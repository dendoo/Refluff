"""Inspect the accepted Tripo GLB before importing it into Unity.

Usage: python Tools/first_person/inspect_glb.py path/to/model.glb [--json]
Does not change the model. Run on the high-quality original and simplified export
to record the actual reduction, finite bounds, degenerate faces and texture sizes.
"""
from __future__ import annotations

import argparse
import io
import json
import struct
from pathlib import Path

import numpy as np
from PIL import Image

DTYPES = {5120: "i1", 5121: "u1", 5122: "<i2", 5123: "<u2", 5125: "<u4", 5126: "<f4"}
WIDTHS = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4, "MAT4": 16}


def inspect(path: Path) -> dict:
    data = path.read_bytes()
    magic, version, length = struct.unpack_from("<III", data)
    if magic != 0x46546C67 or version != 2 or length != len(data):
        raise ValueError("Invalid GLB 2 header or byte length")
    offset, doc, binary = 12, None, None
    while offset < len(data):
        size, kind = struct.unpack_from("<II", data, offset)
        chunk = data[offset + 8:offset + 8 + size]
        if len(chunk) != size:
            raise ValueError("Truncated GLB chunk")
        if kind == 0x4E4F534A:
            doc = json.loads(chunk)
        elif kind == 0x004E4942:
            binary = chunk
        offset += 8 + size
    if doc is None or binary is None:
        raise ValueError("GLB must embed its JSON and mesh binary")

    def accessor(index: int) -> np.ndarray:
        a = doc["accessors"][index]
        if "sparse" in a:
            raise ValueError("Sparse accessor needs an explicit review")
        bv = doc["bufferViews"][a["bufferView"]]
        if bv.get("buffer", 0) != 0:
            raise ValueError("External mesh buffer needs an explicit review")
        dtype = np.dtype(DTYPES[a["componentType"]])
        width = WIDTHS[a["type"]]
        start = bv.get("byteOffset", 0) + a.get("byteOffset", 0)
        stride = bv.get("byteStride", dtype.itemsize * width)
        return np.ndarray((a["count"], width), dtype, buffer=binary, offset=start,
                          strides=(stride, dtype.itemsize))

    report = {"path": str(path), "bytes": len(data), "meshes": len(doc.get("meshes", [])),
              "materials": len(doc.get("materials", [])), "vertices": 0, "triangles": 0,
              "non_finite_vertices": 0, "degenerate_faces": 0, "invalid_indices": 0,
              "opposed_normal_faces": 0, "zero_length_normals": 0,
              "non_triangle_primitives": 0, "textures": [], "primitive_bounds": []}
    for mesh in doc.get("meshes", []):
        for primitive in mesh.get("primitives", []):
            vertices = accessor(primitive["attributes"]["POSITION"])
            report["vertices"] += len(vertices)
            report["non_finite_vertices"] += int((~np.isfinite(vertices).all(axis=1)).sum())
            report["primitive_bounds"].append({"mesh": mesh.get("name", ""),
                                               "min": vertices.min(axis=0).astype(float).tolist(),
                                               "max": vertices.max(axis=0).astype(float).tolist()})
            if primitive.get("mode", 4) != 4:
                report["non_triangle_primitives"] += 1
                continue
            indices = (accessor(primitive["indices"]).reshape(-1)
                       if "indices" in primitive else np.arange(len(vertices)))
            if len(indices) % 3:
                raise ValueError("Triangle index count is not divisible by 3")
            report["triangles"] += len(indices) // 3
            bad = (indices >= len(vertices)) | (indices < 0)
            report["invalid_indices"] += int(bad.sum())
            if not bad.any():
                faces = vertices[indices.reshape(-1, 3)].astype(np.float64)
                face_normals = np.cross(faces[:, 1] - faces[:, 0], faces[:, 2] - faces[:, 0])
                twice_area = np.linalg.norm(face_normals, axis=1)
                report["degenerate_faces"] += int((twice_area <= 1e-12).sum())
                if "NORMAL" in primitive["attributes"]:
                    normals = accessor(primitive["attributes"]["NORMAL"])
                    report["zero_length_normals"] += int((np.linalg.norm(normals, axis=1) < 1e-6).sum())
                    average = normals[indices.reshape(-1, 3)].mean(axis=1)
                    report["opposed_normal_faces"] += int(((face_normals * average).sum(axis=1) < -1e-12).sum())
    for img in doc.get("images", []):
        if "bufferView" not in img:
            report["textures"].append({"uri": img.get("uri"), "external": True})
            continue
        bv = doc["bufferViews"][img["bufferView"]]
        start = bv.get("byteOffset", 0)
        encoded = binary[start:start + bv["byteLength"]]
        with Image.open(io.BytesIO(encoded)) as image:
            report["textures"].append({"size": list(image.size), "mode": image.mode, "bytes": len(encoded)})
    report["valid_geometry"] = not any(report[key] for key in
        ("non_finite_vertices", "invalid_indices", "non_triangle_primitives"))
    return report


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("model", type=Path)
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args()
    report = inspect(args.model)
    if args.json:
        print(json.dumps(report, indent=2, ensure_ascii=False))
    else:
        print(f"{args.model.name}: {report['triangles']:,} triangles, {report['vertices']:,} vertices, "
              f"{report['meshes']} meshes, {report['materials']} materials")
        print(f"finite/index check: {'PASS' if report['valid_geometry'] else 'FAIL'}; "
              f"degenerate faces: {report['degenerate_faces']}")
        print(f"faces opposing supplied normals: {report['opposed_normal_faces']}; "
              f"zero-length normals: {report['zero_length_normals']}; visual inspection still required")
        print("textures:", ", ".join(str(t.get("size", "external")) for t in report["textures"]))
    return 0 if report["valid_geometry"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
