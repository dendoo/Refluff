"""
Windows exe / taskbar icon for Unity (Player Settings > Icon), cut from the Steam icon art (Docs/STEAM.md 5.1).

Writes Assets/CarpetCleaning/AppIcon/refluff_icon_<n>.png for Unity's eight Standalone icon slots, so the exe, its
window and its taskbar button show the same art as the Steam shortcut icon (SteamStore/icons/refluff.ico):
    1024            SteamStore/logo/refluff_mark_1024.png (the mark: transparent corners, soft shadow)
    512, 256, 128   that mark resized, premultiplied Lanczos (512 / 256 = SteamStore/icons/shortcut_icon_*.png)
    64              refluff_mark_64.png (the tile trimmed to fill the frame, sparkle kept)
    48, 32          refluff_mark_48 / _32.png (small tile: no sparkle, softer locks)
    16              the 16 px frame of refluff_shortcut_icon.ico
SteamBuild.EnsurePlayerSettings assigns them before every build. Run from the project root:
    python Tools/steam_art/export_app_icon.py
"""
import os
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))
LOGO = os.path.join(ROOT, "SteamStore", "logo")
OUT = os.path.join(ROOT, "Assets", "CarpetCleaning", "AppIcon")
sys.path.insert(0, os.path.join(HERE, "capsule_variants"))
from macro import resize_prem  # noqa: E402  (the resize build_store_art.py makes the shortcut icons with)


def mark_resized(mark, n):
    r = resize_prem(mark, (n, n))
    rgb = np.where(r[..., 3:4] > 1e-4, r[..., :3] / np.maximum(r[..., 3:4], 1e-4), 0.0)
    px = np.concatenate([rgb, r[..., 3:4]], axis=2)
    return Image.fromarray(np.clip(np.floor(px * 255.0 + 0.5), 0, 255).astype(np.uint8), "RGBA")


def main():
    os.makedirs(OUT, exist_ok=True)
    src = Image.open(os.path.join(LOGO, "refluff_mark_1024.png")).convert("RGBA")
    mark = np.asarray(src, np.float32) / 255.0
    mark[..., :3] *= mark[..., 3:4]                                    # premultiplied -> no dark fringe
    icons = {1024: src}
    for n in (512, 256, 128):
        icons[n] = mark_resized(mark, n)
    for n in (64, 48, 32):
        icons[n] = Image.open(os.path.join(LOGO, f"refluff_mark_{n}.png")).convert("RGBA")
    icons[16] = Image.open(os.path.join(LOGO, "refluff_shortcut_icon.ico")).ico.getimage((16, 16)).convert("RGBA")
    for n, im in sorted(icons.items(), reverse=True):
        if im.size != (n, n):
            sys.exit(f"refluff_icon_{n}: the source is {im.size[0]}x{im.size[1]}")
        path = os.path.join(OUT, f"refluff_icon_{n}.png")
        im.save(path, optimize=True)
        a = np.asarray(im)[..., 3]
        print(f"  {os.path.relpath(path, ROOT)}  {n}x{n}  alpha {a.min()}..{a.max()}, corner {a[0, 0]}")


if __name__ == "__main__":
    main()
