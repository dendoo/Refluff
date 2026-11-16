"""Measure the CLEAN / DRY bar fill ends on a 2560x1440 composite: python measure_bars.py <composite.png>
Uses the middle row of each bar (the caps are round): track = the non-white run from the fill start, fill = saturated pixels,
the anti-aliased end pixel counts by its colour coverage."""
import sys
import numpy as np
from PIL import Image

im = np.asarray(Image.open(sys.argv[1]).convert("RGB")).astype(float)
TRACK = np.array([251.0, 251.0, 250.0])


def measure(y0, y1):
    rows = [y for y in range(y0, y1) if (im[y, 70].max() - im[y, 70].min()) > 60]
    y = rows[len(rows) // 2]
    row = im[y]
    sat = row.max(1) - row.min(1)
    x0 = 40
    while row[x0].min() >= 254.5: x0 += 1          # first non-white pixel = track/fill start
    x1 = x0
    while x1 < 440 and row[x1 + 1].min() < 254.5: x1 += 1   # last non-white pixel = track end
    fill_col = row[x0 + 12]
    fe = x0
    while fe + 1 <= x1 and sat[fe + 1] > 0.9 * sat[x0 + 12]: fe += 1
    # coverage of the next (anti-aliased) pixel
    nxt = row[fe + 1]
    cov = float(np.clip(np.dot(nxt - TRACK, fill_col - TRACK) / (np.dot(fill_col - TRACK, fill_col - TRACK) + 1e-6), 0, 1))
    fill_w = fe - x0 + 1 + cov
    track_w = x1 - x0 + 1
    return y, x0, x1, fe + cov, fill_w / track_w


for name, (y0, y1) in (("CLEAN", (80, 105)), ("DRY", (145, 172))):
    y, x0, x1, fe, frac = measure(y0, y1)
    print(f"{name}: mid row y={y} track x {x0}..{x1} fill end x={fe:.1f} -> fill {frac*100:.1f} %")
