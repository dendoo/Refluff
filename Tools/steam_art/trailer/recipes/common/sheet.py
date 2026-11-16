"""Quick review sheet of a StoreGifApi take (trailer shoot helper).

    python sheet.py TAKE OUT.jpg [--crop x,y,w,h] [--t0 S --t1 S] [--n 12] [--cols 4] [--w 640] [--ui]

TAKE = folder name under SteamStore/_work/clips or a path. Frames are picked evenly between take seconds t0 and t1
(seq.json frameSteps x stepDt, 0 = SeqBegin); each tile is labelled with its take time and frame number. --crop is in
the take's frame pixels (the saved crop). --ui composites the HUD panel layer (fNNNN_ui.png, premultiplied) when present.
"""
import argparse
import json
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[5]
CLIPS = ROOT / "SteamStore" / "_work" / "clips"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("take"); ap.add_argument("out")
    ap.add_argument("--crop", default=None); ap.add_argument("--t0", type=float, default=None)
    ap.add_argument("--t1", type=float, default=None); ap.add_argument("--n", type=int, default=12)
    ap.add_argument("--cols", type=int, default=4); ap.add_argument("--w", type=int, default=640)
    ap.add_argument("--ui", action="store_true")
    a = ap.parse_args()
    d = Path(a.take)
    if not d.is_dir():
        d = CLIPS / a.take
    seq = json.loads((d / "seq.json").read_text(encoding="utf-8"))
    dt = float(seq["stepDt"]); steps = seq["frameSteps"]
    times = [s * dt for s in steps]
    t0 = times[0] if a.t0 is None else a.t0
    t1 = times[-1] if a.t1 is None else a.t1
    idx = [i for i, t in enumerate(times) if t0 - 1e-6 <= t <= t1 + 1e-6]
    if not idx:
        raise SystemExit("no frames in range")
    n = min(a.n, len(idx))
    pick = [idx[round(k * (len(idx) - 1) / max(1, n - 1))] for k in range(n)]
    crop = [int(v) for v in a.crop.split(",")] if a.crop else None
    tiles = []
    for i in pick:
        im = Image.open(d / ("f%04d.png" % (i + 1))).convert("RGB")
        if a.ui and (d / ("f%04d_ui.png" % (i + 1))).exists():
            ui = Image.open(d / ("f%04d_ui.png" % (i + 1))).convert("RGBA").resize(im.size, Image.LANCZOS)
            base = im.convert("RGBA")
            # premultiplied over
            import numpy as np
            u = np.asarray(ui, np.float32) / 255.0; b = np.asarray(base, np.float32) / 255.0
            o = u[..., :3] + (1 - u[..., 3:4]) * b[..., :3]
            im = Image.fromarray((np.clip(o, 0, 1) * 255 + 0.5).astype("uint8"))
        if crop:
            im = im.crop((crop[0], crop[1], crop[0] + crop[2], crop[1] + crop[3]))
        h = round(im.height * a.w / im.width)
        im = im.resize((a.w, h), Image.LANCZOS)
        dr = ImageDraw.Draw(im)
        lab = "t %.2f  f%d" % (times[i], i + 1)
        dr.rectangle((0, 0, 8 + 7 * len(lab), 18), fill=(0, 0, 0))
        dr.text((4, 3), lab, fill=(255, 255, 0))
        tiles.append(im)
    cols = min(a.cols, len(tiles)); rows = (len(tiles) + cols - 1) // cols
    th = max(t.height for t in tiles)
    sheet = Image.new("RGB", (cols * a.w + (cols - 1) * 4, rows * th + (rows - 1) * 4), (40, 40, 40))
    for k, t in enumerate(tiles):
        sheet.paste(t, ((k % cols) * (a.w + 4), (k // cols) * (th + 4)))
    Path(a.out).parent.mkdir(parents=True, exist_ok=True)
    sheet.save(a.out, quality=90)
    print(a.out, sheet.size, "frames", len(times), "span %.2f-%.2f" % (times[0], times[-1]))


if __name__ == "__main__":
    main()
