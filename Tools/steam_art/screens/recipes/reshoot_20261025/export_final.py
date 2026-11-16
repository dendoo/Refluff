"""Export the nine 2026-10-25 captures without touching the preserved first shot.

Run after every scene and premultiplied HUD layer in final_order.json exists:
    python Tools/steam_art/screens/recipes/reshoot_20261025/export_final.py

Only files inside SteamStore/screenshots/20261025_reshoot are written. Macro
camera renders are composed directly; the older wide camera/HUD capture's
opaque RGB layer is never used as a base.
"""
from __future__ import annotations

import hashlib
import importlib.util
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageOps


HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[4]
ORDER = HERE / "final_order.json"
SHOTS = ROOT / "SteamStore/_work/shots"
FIRST = ROOT / "SteamStore/screenshots/20261025_current_paused.png"
OUT = ROOT / "SteamStore/screenshots/20261025_reshoot"
SIZE = (2560, 1440)

spec = importlib.util.spec_from_file_location(
    "reshoot_compose_screens", ROOT / "Tools/steam_art/compose_screens.py"
)
compose_utils = importlib.util.module_from_spec(spec)
spec.loader.exec_module(compose_utils)


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def image_record(path: Path) -> dict:
    with Image.open(path) as source:
        source.load()
        rgb = source.convert("RGB")
        # A pixel hash catches identical images in different encoded containers.
        pixel_hash = hashlib.sha256(rgb.tobytes()).hexdigest()
        small = np.asarray(rgb.convert("L").resize((9, 8), Image.Resampling.LANCZOS))
        bits = (small[:, 1:] > small[:, :-1]).ravel()
        dhash = sum(int(value) << bit for bit, value in enumerate(bits))
        return {
            "path": path.relative_to(ROOT).as_posix(),
            "width": rgb.width,
            "height": rgb.height,
            "format": source.format,
            "bytes": path.stat().st_size,
            "sha256": sha256(path),
            "rgb_sha256": pixel_hash,
            "dhash64": f"{dhash:016x}",
        }


def preflight() -> list[dict]:
    order = json.loads(ORDER.read_text(encoding="utf-8-sig"))
    if len(order) != 9 or [x["slot"] for x in order] != list(range(2, 11)):
        raise ValueError("final_order.json must contain nine ordered slots 2..10")
    tags = [x["tag"] for x in order]
    if len(set(tags)) != 9 or any(Path(tag).name != tag for tag in tags):
        raise ValueError("Capture tags must be unique plain filenames")
    missing = [FIRST] if not FIRST.is_file() else []
    for shot in order:
        slot, tag = shot["slot"], shot["tag"]
        expected = "macro" if slot % 2 == 0 else "wide"
        if shot["view"] != expected:
            raise ValueError(f"Slot {slot} must be {expected}")
        for suffix in ("_scene.png", "_hud_ui.png"):
            path = SHOTS / f"{tag}{suffix}"
            if not path.is_file():
                missing.append(path)
    if missing:
        raise FileNotFoundError("No output written; missing capture layers:\n" + "\n".join(map(str, missing)))
    with Image.open(FIRST) as first:
        if first.size != SIZE:
            raise ValueError(f"Preserved first shot is {first.size}, expected {SIZE}")
    for shot in order:
        tag = shot["tag"]
        with Image.open(SHOTS / f"{tag}_scene.png") as scene:
            if scene.width * 9 != scene.height * 16 or scene.width < SIZE[0]:
                raise ValueError(f"{tag}: scene must be 16:9 and at least 2560 px wide")
            scene.verify()
        with Image.open(SHOTS / f"{tag}_hud_ui.png") as ui:
            if ui.size != SIZE or ui.mode != "RGBA":
                raise ValueError(f"{tag}: expected premultiplied RGBA HUD at {SIZE}, got {ui.mode} {ui.size}")
            ui.verify()
    return order


def export(shot: dict) -> dict:
    tag = shot["tag"]
    scene_path = SHOTS / f"{tag}_scene.png"
    ui_path = SHOTS / f"{tag}_hud_ui.png"
    with Image.open(scene_path) as scene:
        scene_rgb = np.asarray(scene.convert("RGB"))
    with Image.open(ui_path) as ui:
        overlay = np.asarray(ui.convert("RGBA")).astype(np.float32) / 255.0
    base = compose_utils.resize_linear(scene_rgb, SIZE)
    # Same sRGB premultiplied-over operation as compose_screens.py. Only the
    # transparent _hud_ui layer is used, including for macro camera renders.
    result = overlay[..., :3] + (1.0 - overlay[..., 3:4]) * base
    master = Image.fromarray((np.clip(result, 0, 1) * 255.0 + 0.5).astype(np.uint8), "RGB")
    path = OUT / f"{tag}.jpg"
    master.save(path, quality=95, subsampling=0, optimize=True)
    record = image_record(path)
    if (record["width"], record["height"]) != SIZE or record["format"] != "JPEG":
        raise ValueError(f"Invalid exported image: {path}")
    record.update({
        "slot": shot["slot"], "tag": tag, "rug": shot["rug"],
        "view": shot["view"], "tool": shot["tool"],
        "category": shot.get("category", "Gameplay"),
        "preserved": False, "jpeg_quality": 95, "jpeg_subsampling": "4:4:4",
        "scene_source": image_record(scene_path),
        "hud_source": image_record(ui_path),
        "hud_composition": "premultiplied RGBA over scene, in sRGB; no RGB HUD capture",
    })
    return record


def contact_sheet(records: list[dict]) -> Path:
    # Every row pairs a wide view with the following macro view.
    thumb_w, thumb_h, pad, caption = 640, 360, 22, 55
    width = pad * 3 + thumb_w * 2
    height = 70 + (thumb_h + caption + pad) * 5 + pad
    sheet = Image.new("RGB", (width, height), "#18212b")
    draw = ImageDraw.Draw(sheet)
    font_path = ROOT / "Tools/steam_art/fonts/Nunito[wght].ttf"
    font = ImageFont.truetype(str(font_path), 21)
    small = ImageFont.truetype(str(font_path), 16)
    title = ImageFont.truetype(str(font_path), 28)
    draw.text((pad, 17), "Refluff - Steam screenshot order - 25 Oct 2026", font=title, fill="#f4f1e9")
    for index, record in enumerate(records):
        col, row = index % 2, index // 2
        x, y = pad + col * (thumb_w + pad), 70 + row * (thumb_h + caption + pad)
        with Image.open(ROOT / record["path"]) as source:
            image = ImageOps.contain(source.convert("RGB"), (thumb_w, thumb_h), Image.Resampling.LANCZOS)
        sheet.paste(image, (x, y))
        label = f"{record['slot']:02d}  {record['view'].upper()}  {record['rug'].removeprefix('rug_')}"
        if record["preserved"]:
            label += "  (preserved)"
        draw.text((x, y + thumb_h + 5), label, font=font, fill="#f4f1e9")
        draw.text((x, y + thumb_h + 31), Path(record["path"]).name, font=small, fill="#b5c0cb")
    path = OUT / "review/contact_sheet.jpg"
    path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(path, quality=94, subsampling=0, optimize=True)
    return path


def main() -> None:
    order = preflight()  # Validate every input before creating output files.
    first_before = sha256(FIRST)
    first = image_record(FIRST)
    first.update({
        "slot": 1, "tag": "20261025_current_paused", "rug": "rug_cartoon_capybara",
        "view": "wide", "preserved": True,
    })
    OUT.mkdir(parents=True, exist_ok=True)
    records = [first] + [export(shot) for shot in order]
    for record in records:
        record["distinct_file_hash"] = sum(x["sha256"] == record["sha256"] for x in records) == 1
        record["distinct_pixel_hash"] = sum(x["rgb_sha256"] == record["rgb_sha256"] for x in records) == 1
        record["dimension_ok"] = (record["width"], record["height"]) == SIZE
    if not all(x["distinct_file_hash"] and x["distinct_pixel_hash"] for x in records):
        raise ValueError("Duplicate screenshot detected; inspect output before upload")
    if sha256(FIRST) != first_before:
        raise ValueError("The preserved first screenshot changed during export")
    sheet = contact_sheet(records)
    manifest = {
        "date": "2026-10-25", "output_size": list(SIZE), "preserved_first_unchanged": True,
        "source_order": ORDER.relative_to(ROOT).as_posix(), "source_order_sha256": sha256(ORDER),
        "checks": {"count": len(records), "new_jpegs": 9, "all_dimensions_ok": True,
                   "all_file_hashes_distinct": True, "all_pixel_hashes_distinct": True,
                   "views_alternate_wide_macro": True},
        "screenshots": records, "contact_sheet": image_record(sheet),
    }
    (OUT / "manifest.json").write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    hash_lines = [f"{record['sha256']}  {record['path']}" for record in records]
    (OUT / "SHA256SUMS.txt").write_text("\n".join(hash_lines) + "\n", encoding="utf-8")
    print(f"ALL OK: 9 new JPEGs, 2560x1440, q95 4:4:4; 10 distinct shots; preserved first unchanged\n{OUT}\n{sheet}")


if __name__ == "__main__":
    main()
