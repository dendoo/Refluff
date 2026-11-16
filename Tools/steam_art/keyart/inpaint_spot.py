"""Fill small dark holes in a plate: inpaint_spot.py img cx cy r lumThr -> in place (normalized-convolution fill)."""
import sys
import numpy as np
from PIL import Image, ImageFilter
p, cx, cy, r, thr = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), int(sys.argv[4]), float(sys.argv[5])
im = Image.open(p).convert("RGB")
a = np.asarray(im, dtype=np.float32) / 255
x0, y0, x1, y1 = cx - 3 * r, cy - 3 * r, cx + 3 * r, cy + 3 * r
sub = a[y0:y1, x0:x1].copy()
L = (sub * [0.2126, 0.7152, 0.0722]).sum(-1)
yy, xx = np.mgrid[y0:y1, x0:x1]
hole = (L < thr) & ((xx - cx) ** 2 + (yy - cy) ** 2 < r * r)
hm = Image.fromarray((hole * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.GaussianBlur(3))
hm = np.asarray(hm, dtype=np.float32) / 255
known = 1 - (hm > 0.05)
num = Image.fromarray(np.clip(sub * known[..., None] * 255, 0, 255).astype(np.uint8))
den = Image.fromarray((known * 255).astype(np.uint8))
fill = None
for rad in (r * 0.6,):
    n = np.asarray(num.filter(ImageFilter.GaussianBlur(rad)), dtype=np.float32) / 255
    d = np.asarray(den.filter(ImageFilter.GaussianBlur(rad)), dtype=np.float32)[..., None] / 255
    fill = n / np.maximum(d, 1e-3)
sub = sub * (1 - hm[..., None]) + fill * hm[..., None]
a[y0:y1, x0:x1] = sub
Image.fromarray(np.clip(a * 255 + 0.5, 0, 255).astype(np.uint8)).save(p, optimize=True)
print("filled", int(hole.sum()), "px")
