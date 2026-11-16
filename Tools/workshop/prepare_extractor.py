"""Give the imported root a stable readable name; preserve optimized geometry/textures byte-for-byte."""
import json
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
source = ROOT / "Tools/workshop_20261116/refluff_workshop_extractor_17k.glb"
target = ROOT / "Assets/CarpetCleaning/Models/Workshop/extractor.glb"
raw = source.read_bytes()
magic, version, total = struct.unpack_from("<III", raw)
assert magic == 0x46546C67 and version == 2 and total == len(raw)
chunks, offset = [], 12
while offset < len(raw):
    length, kind = struct.unpack_from("<II", raw, offset)
    payload = raw[offset + 8:offset + 8 + length]
    if kind == 0x4E4F534A:
        data = json.loads(payload)
        scene = data["scenes"][data.get("scene", 0)]
        assert len(scene["nodes"]) == 1
        data["nodes"][scene["nodes"][0]]["name"] = "extractor"
        scene["name"] = "extractor"
        payload = json.dumps(data, separators=(",", ":")).encode()
        payload += b" " * (-len(payload) % 4)
    chunks.append(struct.pack("<II", len(payload), kind) + payload)
    offset += 8 + length
body = b"".join(chunks)
target.write_bytes(struct.pack("<III", magic, version, len(body) + 12) + body)
print(f"Normalized root name: {target}; binary mesh/texture chunk unchanged")
