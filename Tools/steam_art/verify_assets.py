"""Check SteamStore/ against the Steam graphical-asset spec (Docs/STORE_ART.md, SteamStore/_work/steam_spec.md).

    python Tools/steam_art/verify_assets.py

Every upload file must have the exact pixel size Steam asks for; a one-pixel error is rejected. Also checks the
format / alpha rules (library logo = transparent PNG, app icon = opaque JPG), screenshot count and aspect, and file
sizes. When SteamStore/description/ exists, also the "About this game" media (make_clips.py): every clip under 11.5 s
(Steam: "under 12 seconds"), at least 780 px wide (the description column), no audio stream, each file <= 5 MB and
all description media together <= 15 MB; header images at least 780 px wide. make_clips.py's *_poster.png and
*_sheet.png previews are not uploaded and are skipped, as is a GIF fallback next to a same-stem MP4 / WebM. When
SteamStore/achievements/source_en.json exists, also the achievement icons (make_achievement_icons.py): every API in it
has achievements/icons/<API>.png and <API>_locked.png, each a 256x256 PNG in RGB mode (no alpha). Prints one line per
check and exits 1 when anything fails.
"""
import json
import sys
from pathlib import Path

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))

ROOT = Path(__file__).resolve().parents[2] / "SteamStore"

# (relative path, width, height, kind) - kind: 'opaque' (no transparency), 'alpha' (must have transparency), 'any'
FIXED = [
    ("store/header_capsule_920x430.png", 920, 430, "opaque"),
    ("store/small_capsule_462x174.png", 462, 174, "opaque"),
    ("store/main_capsule_1232x706.png", 1232, 706, "opaque"),
    ("store/vertical_capsule_748x896.png", 748, 896, "opaque"),
    ("store/page_background_1438x810.png", 1438, 810, "opaque"),
    ("library/library_capsule_600x900.png", 600, 900, "opaque"),
    ("library/library_header_920x430.png", 920, 430, "opaque"),
    ("library/library_hero_3840x1240.png", 3840, 1240, "opaque"),
    ("library/library_logo_1280x720.png", None, None, "alpha"),   # fits 1280x720, touches the width or the height
    ("icons/app_icon_184x184.jpg", 184, 184, "opaque"),
    ("icons/shortcut_icon_512x512.png", 512, 512, "any"),
    ("icons/shortcut_icon_256x256.png", 256, 256, "any"),
    ("events/event_cover_800x450.png", 800, 450, "opaque"),
    ("events/event_header_1920x622.png", 1920, 622, "opaque"),
    ("logo/refluff_logo.png", None, None, "alpha"),
    ("logo/refluff_wordmark.png", None, None, "alpha"),
]
MAX_MB = 5.0
# "About this game" media (Docs/STORE_ART.md, description clips): Steam caps animated media at 12 s; 5 MB per file and
# 15 MB in all are the older per-image / page-weight rules, kept as our own target
DESC_MAX_S = 11.5
DESC_MIN_W = 780
DESC_TOTAL_MB = 15.0
DESC_VIDEO = {".mp4", ".webm"}
DESC_IMAGE = {".png", ".jpg", ".jpeg", ".webp"}
DESC_PREVIEWS = ("_poster.png", "_sheet.png")    # written by make_clips.py next to a clip, never uploaded
# achievement icons (make_achievement_icons.py): achieved + unachieved per API of source_en.json, 256 px, opaque RGB
ACH_SRC = ROOT / "achievements" / "source_en.json"
ACH_DIR = ROOT / "achievements" / "icons"
ACH_SIZE = 256


def has_transparency(im):
    if im.mode in ("RGBA", "LA") or (im.mode == "P" and "transparency" in im.info):
        a = im.convert("RGBA").getchannel("A")
        return a.getextrema()[0] < 255
    return False


def check_fixed(rel, w, h, kind):
    p = ROOT / rel
    if not p.exists():
        return False, f"MISSING  {rel}"
    im = Image.open(p)
    mb = p.stat().st_size / 1e6
    ok, notes = True, []
    if w is not None and im.size != (w, h):
        ok = False
        notes.append(f"size {im.size[0]}x{im.size[1]} != {w}x{h}")
    if rel.endswith("library_logo_1280x720.png"):
        bw, bh = im.size
        if bw > 1280 or bh > 720 or not (bw == 1280 or bh == 720):
            ok = False
            notes.append(f"logo {bw}x{bh} must fit 1280x720 and touch 1280 wide or 720 tall")
    t = has_transparency(im)
    if kind == "opaque" and t:
        ok = False
        notes.append("has transparency (Steam fills it with black)")
    if kind == "alpha" and not t:
        ok = False
        notes.append("needs a transparent background")
    if rel.endswith(".jpg") and im.format != "JPEG":
        ok = False
        notes.append(f"format {im.format}, expected JPEG")
    if mb > MAX_MB:
        notes.append(f"large file {mb:.1f} MB")
    return ok, f"{'OK' if ok else 'FAIL'}     {rel}  {im.size[0]}x{im.size[1]} {im.mode} {mb:.2f} MB" + (("  - " + "; ".join(notes)) if notes else "")


def check_screens():
    lines, ok = [], True
    shots = sorted((ROOT / "screenshots").glob("*.jpg")) + sorted((ROOT / "screenshots").glob("*.png"))
    if len(shots) < 5:
        ok = False
        lines.append(f"FAIL     screenshots: {len(shots)} files, Steam needs at least 5")
    for p in shots:
        im = Image.open(p)
        w, h = im.size
        good = w >= 1920 and h >= 1080 and abs(w / h - 16 / 9) < 1e-3 and not has_transparency(im)
        mb = p.stat().st_size / 1e6
        ok &= good and mb <= MAX_MB
        lines.append(f"{'OK' if good and mb <= MAX_MB else 'FAIL'}     screenshots/{p.name}  {w}x{h} {mb:.2f} MB")
    return ok, lines


def check_description(folder=None):
    folder = folder or (ROOT / "description")
    if not folder.is_dir():
        return True, ["SKIP     description/  (no folder yet: no clips to check)"]
    files = sorted(p for p in folder.iterdir() if p.is_file() and p.suffix.lower() in DESC_VIDEO | DESC_IMAGE | {".gif"})
    lines, ok, total = [], True, 0.0
    for p in files:
        rel = "description/" + p.name
        mb = p.stat().st_size / 1e6
        if p.name.endswith(DESC_PREVIEWS):
            lines.append(f"SKIP     {rel}  (make_clips.py preview, not uploaded)")
            continue
        if p.suffix.lower() == ".gif" and any(p.with_suffix(e).exists() for e in DESC_VIDEO):
            lines.append(f"SKIP     {rel}  (GIF fallback of a clip that has a video, not uploaded)")
            continue
        total += mb
        good, notes = True, []
        if mb > MAX_MB:
            good = False
            notes.append(f"{mb:.2f} MB > {MAX_MB:.0f} MB")
        ext = p.suffix.lower()
        if ext in DESC_IMAGE:
            with Image.open(p) as im:
                w, h = im.size
            if w < DESC_MIN_W:
                good = False
                notes.append(f"width {w} < {DESC_MIN_W}")
            desc = f"{w}x{h} image"
        else:
            try:
                from clip_probe import probe_media
                info = probe_media(p, count_frames=False)
            except Exception as e:  # ffmpeg missing or an unreadable file
                ok = False
                lines.append(f"FAIL     {rel}  - cannot probe: {e}")
                continue
            dur, w, h = info["duration"], info["width"] or 0, info["height"] or 0
            if dur is None or dur >= DESC_MAX_S:
                good = False
                notes.append(f"duration {dur} s, must be < {DESC_MAX_S} s")
            if info["audio"]:
                good = False
                notes.append("has an audio stream")
            if w < DESC_MIN_W:
                notes.append(f"width {w} < {DESC_MIN_W}")
                good = good and ext == ".gif"      # a narrow GIF fallback only gets a note
            if ext in DESC_VIDEO and info["pix_fmt"] != "yuv420p":
                notes.append(f"pix_fmt {info['pix_fmt']} (yuv420p plays everywhere)")
            desc = f"{w}x{h} {info['codec']} {'?' if dur is None else round(dur, 2)} s"
        ok &= good
        lines.append(f"{'OK' if good else 'FAIL'}     {rel}  {desc} {mb:.2f} MB" + (("  - " + "; ".join(notes)) if notes else ""))
    tot_ok = total <= DESC_TOTAL_MB
    ok &= tot_ok
    lines.append(f"{'OK' if tot_ok else 'FAIL'}     description/ media total {total:.2f} MB (limit {DESC_TOTAL_MB:.0f} MB)")
    return ok, lines


def check_achievements():
    if not ACH_SRC.exists():
        return True, ["SKIP     achievements/  (no source_en.json yet: no icons to check)"]
    apis = [a["api"] for a in json.loads(ACH_SRC.read_text(encoding="utf-8"))["achievements"]]
    lines, ok, found = [], True, 0
    for api in apis:
        for name in (f"{api}.png", f"{api}_locked.png"):
            rel = "achievements/icons/" + name
            p = ACH_DIR / name
            if not p.exists():
                ok = False
                lines.append(f"MISSING  {rel}")
                continue
            found += 1
            with Image.open(p) as im:
                size, mode, fmt, t = im.size, im.mode, im.format, has_transparency(im)
            good, notes = True, []
            if size != (ACH_SIZE, ACH_SIZE):
                good = False
                notes.append(f"size {size[0]}x{size[1]} != {ACH_SIZE}x{ACH_SIZE}")
            if mode != "RGB" or t:
                good = False
                notes.append(f"mode {mode}, expected RGB (no alpha)")
            if fmt != "PNG":
                good = False
                notes.append(f"format {fmt}, expected PNG")
            ok &= good
            mb = p.stat().st_size / 1e6
            lines.append(f"{'OK' if good else 'FAIL'}     {rel}  {size[0]}x{size[1]} {mode} {mb:.2f} MB"
                         + (("  - " + "; ".join(notes)) if notes else ""))
    known = {n for api in apis for n in (f"{api}.png", f"{api}_locked.png")}
    for p in (sorted(ACH_DIR.glob("*.png")) if ACH_DIR.is_dir() else []):
        if p.name not in known:
            lines.append(f"NOTE     achievements/icons/{p.name}  (no such API in source_en.json, not uploaded)")
    lines.append(f"{'OK' if ok else 'FAIL'}     achievements: {len(apis)} in source_en.json, {found} of {2 * len(apis)} icons")
    return ok, lines


def main():
    all_ok = True
    for rel, w, h, kind in FIXED:
        ok, line = check_fixed(rel, w, h, kind)
        all_ok &= ok
        print(line)
    ok, lines = check_screens()
    all_ok &= ok
    print("\n".join(lines))
    ok, lines = check_description()
    all_ok &= ok
    print("\n".join(lines))
    ok, lines = check_achievements()
    all_ok &= ok
    print("\n".join(lines))
    print("ALL OK" if all_ok else "SOME CHECKS FAILED")
    return 0 if all_ok else 1


if __name__ == "__main__":
    sys.exit(main())
