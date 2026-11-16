"""Read-only textured vertex projections of downloaded GLBs, in glTFast's mirrored-X Unity axes.

This supports axis/marker planning while the Editor is busy. It is not the final solid-surface
acceptance render: use preview_model.cs in the stopped Editor to inspect normals and seams.
"""
from __future__ import annotations
import io
import json
import struct
from pathlib import Path
import numpy as np
from PIL import Image
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt

HERE = Path(__file__).resolve().parent
OUT = HERE.parents[2] / "reports/first_person/regeneration_20261115/models"
OUT.mkdir(parents=True, exist_ok=True)
DTYPES = {5126: "<f4", 5125: "<u4", 5123: "<u2"}
WIDTHS = {"VEC3": 3, "VEC2": 2, "SCALAR": 1}

for path in sorted((HERE / "downloads").glob("*.glb")):
    data = path.read_bytes()
    length = struct.unpack_from("<I", data, 12)[0]
    doc = json.loads(data[20:20 + length])
    offset = 20 + length
    length, kind = struct.unpack_from("<II", data, offset)
    assert kind == 0x004E4942
    binary = data[offset + 8:offset + 8 + length]
    assert len(doc.get("nodes", [])) == 1 and set(doc["nodes"][0]).issubset({"mesh", "name"}), "Inspect nonidentity node hierarchy explicitly"

    def accessor(index):
        value = doc["accessors"][index]
        view = doc["bufferViews"][value["bufferView"]]
        width, dtype = WIDTHS[value["type"]], np.dtype(DTYPES[value["componentType"]])
        return np.ndarray((value["count"], width), dtype, binary,
                          view.get("byteOffset", 0) + value.get("byteOffset", 0),
                          strides=(view.get("byteStride", dtype.itemsize * width), dtype.itemsize)).copy()

    primitive = doc["meshes"][0]["primitives"][0]
    points = accessor(primitive["attributes"]["POSITION"])
    points[:, 0] *= -1
    uv = accessor(primitive["attributes"]["TEXCOORD_0"])
    material = doc["materials"][primitive.get("material", 0)]
    texture = material["pbrMetallicRoughness"]["baseColorTexture"]["index"]
    picture = doc["images"][doc["textures"][texture]["source"]]
    view = doc["bufferViews"][picture["bufferView"]]
    start = view.get("byteOffset", 0)
    pixels = np.asarray(Image.open(io.BytesIO(binary[start:start + view["byteLength"]])).convert("RGB"))
    color = pixels[np.clip((uv[:, 1] * pixels.shape[0]).astype(int), 0, pixels.shape[0] - 1),
                   np.clip((uv[:, 0] * pixels.shape[1]).astype(int), 0, pixels.shape[1] - 1)] / 255
    fig, axes = plt.subplots(1, 3, figsize=(15, 5), layout="constrained")
    fig.suptitle(path.stem + " — Unity mirror X, unrotated")
    for axis, (a, b, depth, title) in zip(axes, [(0, 1, 2, "front: X / Y"), (2, 1, 0, "side: Z / Y"), (0, 2, 1, "top: X / Z")]):
        order = np.argsort(points[:, depth])
        axis.scatter(points[order, a], points[order, b], c=color[order], s=1.2, linewidths=0)
        axis.set_aspect("equal")
        axis.set_title(title)
        axis.grid(alpha=.2)
    fig.savefig(OUT / (path.stem + "_offline_axes.png"), dpi=160)
    plt.close(fig)
    print(path.name)
