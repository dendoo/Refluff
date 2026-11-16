"""Stack StoreShotApi capture layers into Steam screenshots (Docs/STORE_ART.md).

    python Tools/steam_art/compose_screens.py [name ...] [--nohud]

For every <name> (default: every *_scene.png in SteamStore/_work/shots):
  <name>_scene.png   game camera, 3840x2160 (supersampled scene layer)
  <name>_hud_ui.png  UI Toolkit HUD, premultiplied RGBA at the Game view layout size (2560x1440), optional
-> SteamStore/_work/screens/<name>_2560.png  (2560x1440 master: scene downscaled, HUD laid over at its native size)
   SteamStore/_work/screens/<name>.png       (1920x1080, the Steam upload size)
Downscaling is done in linear light (Lanczos), so thin bright fibres do not darken; the HUD is composited in sRGB,
exactly as EconomyHud.DebugCapture does in the game (over = ui + (1 - ui.a) * scene).
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SHOTS = ROOT / "SteamStore" / "_work" / "shots"
OUT = ROOT / "SteamStore" / "_work" / "screens"


def srgb_to_linear(c):
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def linear_to_srgb(c):
    c = np.clip(c, 0.0, 1.0)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(c, 1 / 2.4) - 0.055)


def resize_linear(rgb8, size):
    """Lanczos resize of an sRGB uint8 HxWx3 array in linear light; returns float sRGB 0..1."""
    lin = srgb_to_linear(rgb8.astype(np.float32) / 255.0)
    chans = [np.asarray(Image.fromarray(lin[..., i].astype(np.float32), "F").resize(size, Image.LANCZOS)) for i in range(3)]
    return linear_to_srgb(np.stack(chans, -1))


def compose(name, use_hud=True):
    scene_path = SHOTS / f"{name}_scene.png"
    ui_path = SHOTS / f"{name}_hud_ui.png"
    scene = np.asarray(Image.open(scene_path).convert("RGB"))
    ui = np.asarray(Image.open(ui_path).convert("RGBA")).astype(np.float32) / 255.0 if (use_hud and ui_path.exists()) else None
    size = (ui.shape[1], ui.shape[0]) if ui is not None else (2560, 1440)
    base = resize_linear(scene, size)
    if ui is not None:
        base = ui[..., :3] + (1.0 - ui[..., 3:4]) * base      # premultiplied over, sRGB
    master = Image.fromarray((np.clip(base, 0, 1) * 255.0 + 0.5).astype(np.uint8), "RGB")
    OUT.mkdir(parents=True, exist_ok=True)
    master.save(OUT / f"{name}_2560.png", optimize=True)
    small = resize_linear(np.asarray(master), (1920, 1080))
    Image.fromarray((small * 255.0 + 0.5).astype(np.uint8), "RGB").save(OUT / f"{name}.png", optimize=True)
    return f"{name}: scene {scene.shape[1]}x{scene.shape[0]}, hud {'yes' if ui is not None else 'no'} -> {OUT / (name + '.png')}"


def main(argv):
    use_hud = "--nohud" not in argv
    names = [a for a in argv if not a.startswith("--")]
    if not names:
        names = sorted(p.name[: -len("_scene.png")] for p in SHOTS.glob("*_scene.png"))
    for n in names:
        print(compose(n, use_hud))


if __name__ == "__main__":
    main(sys.argv[1:])
