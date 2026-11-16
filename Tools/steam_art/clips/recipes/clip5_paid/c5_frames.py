"""clip5_paid: the virtual camera over a full-frame HUD take (Steam description clip 5, "Get paid, upgrade your kit").

The take (c5_take.cs) is captured at the full 2560x1440 Game view size (scene + UI Toolkit panel, StoreGifApi). This script
composites every frame exactly like make_clips.py (premultiplied panel over the scene, 8-bit sRGB), then cuts a keyframed
16:9 window out of it and resamples it straight to the clip size (Lanczos in linear light, sub-pixel window, so a moving window
does not jitter). It writes a scene-only take folder <take>_cam/ (fNNNN.png + seq.json with the original frameSteps) that
make_clips.py encodes as is (its downscale is a no-op at the same size).

    python c5_frames.py [take] [--out-take NAME] [--size 1560x878] [--jobs 8]

Camera (2560x1440 panel pixels, take time T = Steps x 0.02 s; the hand-over is at T 0.80):
    T 0.00-0.98   FULL  (0,0,2560,1440): the spotless rug + the whole HUD (the clip starts at T 0.40 = the poster); the rug
                  lifts off at T 0.80 in the full frame
    T 0.98-1.66   push in (smootherstep, size geometric) while the rug flies out and the card drops (c 0.30 = T 1.10)
    T 1.66-5.94   CARD  (640,122,1280,720): the pay slip prints line by line, COMBO stamp, the total counts up to $120
    T 5.94-6.50   pull out to WALLET while the 14 coins burst from the total (T 6.0) and fly to the wallet
    T 6.50-7.36   WALLET (0,0,1828,1028): stats panel + message pill + card + tool list; the wallet counts $1,840 -> $1,960
                  (T 6.4-7.44), so the "get paid" payoff is on screen and the HUD stays in shot through the card exit
    T 7.36-8.06   zoom out to FULL while the card exits (Skip at T 7.30) and the shop enters (ShopOpen at T 7.66)
    T 8.06-end    FULL: the whole upgrade screen, two buys (T 8.30, 9.20); a tighter window always sliced the dimmed HUD
                  (the top-left wallet pill or the Upgrades button), and FULL makes the loop dissolve a plain one
Round 1 held CARD to T 7.24: the coins flew out of frame into nothing and the card's exit left 3 blank frames (f0189-f0191).
"""
import argparse
import json
import math
import sys
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[5]
sys.path.insert(0, str(ROOT / "Tools" / "steam_art"))
import make_clips as M  # noqa: E402

FULL = (0.0, 0.0, 2560.0, 1440.0)
CARD = (640.0, 122.0, 1280.0, 720.0)
WALLET = (0.0, 0.0, 1828.0, 1028.0)  # stats (32,32)-(432,268), tool list to y 996, card + stamp to x 1641; 16:9
# (T, rect): between two keys with different rects the window moves, with equal rects it holds
KEYS = [(0.00, FULL), (0.98, FULL), (1.66, CARD), (5.94, CARD), (6.50, WALLET), (7.36, WALLET), (8.06, FULL), (99.0, FULL)]


def smootherstep(s):
    s = min(max(s, 0.0), 1.0)
    return s * s * s * (s * (s * 6 - 15) + 10)


def window(T):
    """The crop (x, y, w, h) at take time T: centre linear, size geometric in the eased parameter (an even zoom speed)."""
    for (t0, a), (t1, b) in zip(KEYS, KEYS[1:]):
        if T <= t1:
            if a == b or T <= t0:
                return a
            s = smootherstep((T - t0) / (t1 - t0))
            w = a[2] * math.pow(b[2] / a[2], s)
            h = a[3] * math.pow(b[3] / a[3], s)
            cx = (a[0] + a[2] / 2) * (1 - s) + (b[0] + b[2] / 2) * s
            cy = (a[1] + a[3] / 2) * (1 - s) + (b[1] + b[3] / 2) * s
            return (cx - w / 2, cy - h / 2, w, h)
    return KEYS[-1][1]


def render(src, dst, T, W, H):
    sc = np.asarray(Image.open(src).convert("RGB"))
    up = src.with_name(src.stem + "_ui.png")
    if up.exists():
        ui = np.asarray(Image.open(up).convert("RGBA"))
        img = M.composite_ui(M.resize_linear(sc, ui.shape[1], ui.shape[0]), ui)
    else:
        img = sc
    x, y, w, h = window(T)
    lin = M.srgb_u8_to_linear(img)
    out = np.empty((H, W, 3), np.float32)
    box = (x, y, x + w, y + h)
    for c in range(3):
        ch = Image.fromarray(np.ascontiguousarray(lin[..., c]), "F")
        out[..., c] = np.asarray(ch.resize((W, H), Image.Resampling.LANCZOS, box=box))
    Image.fromarray(M.linear_to_srgb_u8(out)).save(dst, compress_level=3)
    return dst.name


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("take", nargs="?", default="c5_paid_r1")
    ap.add_argument("--out-take", default="")
    ap.add_argument("--size", default="1560x878")
    ap.add_argument("--jobs", type=int, default=8)
    a = ap.parse_args()
    src = M.CLIPS / a.take
    seq = json.loads((src / "seq.json").read_text(encoding="utf-8"))
    W, H = (int(v) for v in a.size.split("x"))
    dst = M.CLIPS / (a.out_take or a.take + "_cam")
    dst.mkdir(parents=True, exist_ok=True)
    for f in dst.glob("f*.png"):
        f.unlink()
    step_dt = float(seq.get("stepDt", 0.02))
    jobs = []
    for i, s in enumerate(seq["frameSteps"]):
        f = src / ("f%04d.png" % (i + 1))
        if f.exists():
            jobs.append((f, dst / f.name, s * step_dt))
    with ThreadPoolExecutor(max_workers=a.jobs) as pool:
        list(pool.map(lambda j: render(j[0], j[1], j[2], W, H), jobs))
    out = dict(seq)
    out.update({"name": dst.name, "render": [W, H], "crop": [0, 0, W, H], "cropAsked": [0, 0, W, H], "hud": False,
                "virtualCamera": {"from": a.take, "keys": [[t, list(r)] for t, r in KEYS]}})
    for k in ("ui", "uiCrop", "uiPng"):
        out.pop(k, None)
    (dst / "seq.json").write_text(json.dumps(out), encoding="utf-8")
    print(json.dumps({"take": str(dst).replace("\\", "/"), "frames": len(jobs), "size": [W, H]}))


if __name__ == "__main__":
    main()
