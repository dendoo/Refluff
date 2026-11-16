"""Rotate a StoreGifApi take so the clip starts (and loops) at frame X, with the end -> start dissolve moved inside.

    python rotate_take.py SRC_TAKE DST_TAKE X [--fade 0.5]

make_clips --fade-loop always starts the clip at the take's start (+ fade), so its poster is the take's first frame. For
clip 2 the story starts on the DRY rug (the hook: add water, brown comes out), which is a weak still. This writes a new take
folder whose frames run X .. N-F, then F frames that dissolve the take's end into its start (blended in linear light, the
same weights as make_clips' loop fade: (j+1)/(F+1)), then F+1 .. X-1. The clip's last frame (X-1) leads straight into its
first (X): a natural loop, and the poster is frame X. Encode the result with make_clips --fade-loop 0.
Unchanged frames are hard links (copies if the file system refuses); seq.json gets a uniform cadence and a "rotated" note.
"""
import argparse
import json
import os
import shutil
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[6]          # the project folder
sys.path.insert(0, str(ROOT / "Tools" / "steam_art"))
from make_clips import CLIPS, srgb_u8_to_linear, linear_to_srgb_u8   # noqa: E402


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("src")
    ap.add_argument("dst")
    ap.add_argument("x", type=int, help="1-based take frame the clip starts on (the poster)")
    ap.add_argument("--fade", type=float, default=0.5)
    a = ap.parse_args()
    src = Path(a.src) if Path(a.src).is_dir() else CLIPS / a.src
    dst = CLIPS / a.dst
    seq = json.loads((src / "seq.json").read_text(encoding="utf-8"))
    steps = seq["frameSteps"]
    n = len(steps)
    every = seq["cadence"][0]["every"]
    if len(seq["cadence"]) != 1 or any(steps[i] != every * (i + 1) for i in range(n)):
        raise SystemExit("rotate_take needs a take with one uniform cadence")
    fps = 1.0 / (every * seq["stepDt"])
    F = int(round(a.fade * fps))
    X = a.x
    if not (F + 1 < X <= n - F):
        raise SystemExit("X must lie in (F+1, N-F] = (%d, %d]" % (F + 1, n - F))
    if dst.exists():
        shutil.rmtree(dst)
    dst.mkdir(parents=True)

    def f(i):
        return src / ("f%04d.png" % i)

    order = [("copy", i) for i in range(X, n - F + 1)]
    order += [("blend", n - F + 1 + j, 1 + j, (j + 1) / float(F + 1)) for j in range(F)]
    order += [("copy", i) for i in range(F + 1, X)]
    for k, item in enumerate(order, 1):
        out = dst / ("f%04d.png" % k)
        if item[0] == "copy":
            try:
                os.link(f(item[1]), out)
            except OSError:
                shutil.copyfile(f(item[1]), out)
        else:
            _, ia, ib, w = item
            A = srgb_u8_to_linear(np.asarray(Image.open(f(ia)).convert("RGB")))
            B = srgb_u8_to_linear(np.asarray(Image.open(f(ib)).convert("RGB")))
            Image.fromarray(linear_to_srgb_u8(A * (1.0 - w) + B * w)).save(out)
    m = len(order)
    seq2 = dict(seq)
    seq2["name"] = a.dst
    seq2["frames"] = m
    seq2["steps"] = every * m
    seq2["frameSteps"] = [every * (k + 1) for k in range(m)]
    seq2["rotated"] = {"from": src.name, "startFrame": X, "fadeFrames": F,
                       "order": "%d..%d, dissolve %d..%d -> 1..%d, %d..%d" % (X, n - F, n - F + 1, n, F, F + 1, X - 1)}
    (dst / "seq.json").write_text(json.dumps(seq2), encoding="utf-8")
    print(json.dumps({"dst": str(dst).replace("\\", "/"), "frames": m, "seconds": round(m / fps, 3), "fadeFrames": F,
                      "order": seq2["rotated"]["order"]}))


if __name__ == "__main__":
    main()
