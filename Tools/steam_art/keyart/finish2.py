"""Round-2 finisher for the Refluff key-art plates (layered depth of field, contact shadow, grime grade).

Inputs (StoreShotApi.RenderView from the same camera, see scratchpad plate.sh):
    <raw>_full.png    whole scene incl. the tool head (any integer multiple of --size, Lanczos-downsampled)
    <raw>_nohead.png  same without the tool head (what is behind it)
    <raw>_obj.png     tool head only, every other renderer off (--size)
    <raw>_empty.png   nothing at all (--size): obj vs empty = the head's silhouette

    python finish2.py raw3/wide2v2 out.png --size 3840 2000 --cam px py pz rx ry rz fov --head x z
           [--focus-world x y z] [--blur 0.018] [--seam 0.465] [--expo 1.18] ...

Layers: the background (nohead) gets the grimy-side grade, the head's contact + cast shadow and the plane depth
of field; the head silhouette is then laid over it from the sharp full render, so no head colour bleeds into the
blur and the pile keeps the plane depth right up to the hem. The grime grade works in carpet UV (the plane
intersection of every pixel), so it follows the seam in perspective, and it leaves the foam alone.
"""
import argparse
import os

import numpy as np
from PIL import Image, ImageFilter

from lensdof import depth_map, unity_rot

LUMA = np.array([0.2126, 0.7152, 0.0722], dtype=np.float32)


def load(path, size):
    im = Image.open(path).convert("RGB")
    if im.size != size:
        im = im.resize(size, Image.LANCZOS)
    return np.asarray(im, dtype=np.float32) / 255.0


def gblur(x, rad):
    """Gaussian blur of a float image (H, W[, C]) in 0..1 through PIL (8-bit per level is fine for masks/plates)."""
    if rad <= 0:
        return x
    if x.ndim == 2:
        im = Image.fromarray(np.clip(x * 255 + 0.5, 0, 255).astype(np.uint8))
        return np.asarray(im.filter(ImageFilter.GaussianBlur(rad)), dtype=np.float32) / 255.0
    im = Image.fromarray(np.clip(x * 255 + 0.5, 0, 255).astype(np.uint8))
    return np.asarray(im.filter(ImageFilter.GaussianBlur(rad)), dtype=np.float32) / 255.0


def smoothstep(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def value_noise(x, y, seed):
    xi, yi = np.floor(x), np.floor(y)
    xf, yf = x - xi, y - yi

    def h(i, j):
        n = np.sin(i * 127.1 + j * 311.7 + seed * 74.7) * 43758.5453
        return n - np.floor(n)
    u, v = xf * xf * (3 - 2 * xf), yf * yf * (3 - 2 * yf)
    a, b, c, d = h(xi, yi), h(xi + 1, yi), h(xi, yi + 1), h(xi + 1, yi + 1)
    return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v


def fbm(x, y, seed, oct=4):
    s, amp, tot = 0.0, 0.5, 0.0
    for o in range(oct):
        s = s + amp * value_noise(x, y, seed + o * 13)
        tot += amp
        x, y, amp = x * 2.03, y * 2.03, amp * 0.5
    return s / tot


def dof(src, z, focus, blur_px, near, cap=1.5):
    r = blur_px * (1.0 - focus / z)
    r = np.where(r < 0, -r * near, r)
    r = np.clip(r, 0, blur_px * cap)
    levels = [0.0]
    while levels[-1] < r.max():
        levels.append(1.5 if levels[-1] == 0 else levels[-1] * 2)
    idx = np.interp(r, np.array(levels), np.arange(len(levels)))
    acc = np.zeros_like(src)
    for k, rad in enumerate(levels):
        wk = np.clip(1.0 - np.abs(idx - k), 0, 1)[..., None]
        if not wk.any():
            continue
        acc += (src if rad == 0 else gblur(src, rad * 0.5)) * wk
    return acc, float(r.max())


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("raw", help="prefix: <raw>_full.png etc.")
    ap.add_argument("out")
    ap.add_argument("--size", type=int, nargs=2, required=True)
    ap.add_argument("--cam", type=float, nargs=7, required=True)
    ap.add_argument("--head", type=float, nargs=2, default=None, help="head centre on the carpet, world x z (m)")
    ap.add_argument("--head-r", type=float, default=0.054, help="head footprint radius (m)")
    ap.add_argument("--light", type=float, nargs=2, default=(25, 80), help="sun pitch yaw (deg): cast shadow direction")
    ap.add_argument("--head-h", type=float, default=0.030, help="height used for the cast shadow length (m)")
    ap.add_argument("--ao", type=float, default=0.42, help="contact shadow strength at the hem")
    ap.add_argument("--ao-w", type=float, default=0.012, help="contact shadow falloff (m)")
    ap.add_argument("--cast", type=float, default=0.30, help="cast shadow strength")
    ap.add_argument("--focus-world", type=float, nargs=3, default=None)
    ap.add_argument("--focus", type=float, default=0.0)
    ap.add_argument("--blur", type=float, default=0.018, help="CoC radius at infinity, fraction of the plate width")
    ap.add_argument("--near", type=float, default=0.8)
    ap.add_argument("--cap", type=float, default=1.5)
    ap.add_argument("--seam", type=float, default=0.465, help="carpet u of the dirty/clean seam; <0 = no grime grade")
    ap.add_argument("--seam-axis", default="u", choices=["u", "v"])
    ap.add_argument("--dirty-below", action="store_true", help="grime is where u (or v) is ABOVE the seam instead")
    ap.add_argument("--feather", type=float, nargs=2, default=(0.13, 0.035), help="grime weight 1 at seam-a .. 0 at seam-b")
    ap.add_argument("--grime-desat", type=float, default=0.48)
    ap.add_argument("--grime-dark", type=float, default=0.85)
    ap.add_argument("--blotch", type=float, default=0.22, help="darker blotchy stains on the grimy side")
    ap.add_argument("--expo", type=float, default=1.18)
    ap.add_argument("--curve", type=float, default=0.18)
    ap.add_argument("--sat", type=float, default=1.04)
    ap.add_argument("--lift", type=float, default=0.0)
    ap.add_argument("--hi", type=float, default=0.0, help="soft highlight boost on the clean side's bright tips")
    ap.add_argument("--crop", type=int, nargs=4, default=None, help="x y w h crop of the finished plate (before --final)")
    ap.add_argument("--final", type=int, nargs=2, default=None, help="resize the (cropped) plate to this size")
    ap.add_argument("--foam-soften", type=float, nargs=5, default=None, help="x0 y0 x1 y1 amt: fade the foam in this box (fractions)")
    ap.add_argument("--preview", type=int, default=960)
    a = ap.parse_args()

    W, H = a.size
    full = load(a.raw + "_full.png", (W, H))
    nohead = load(a.raw + "_nohead.png", (W, H)) if os.path.exists(a.raw + "_nohead.png") else full.copy()
    have_obj = os.path.exists(a.raw + "_obj.png")
    if have_obj:
        obj = load(a.raw + "_obj.png", (W, H))
        emp = load(a.raw + "_empty.png", (W, H))
        m = np.clip((np.abs(obj - emp).sum(-1) - 0.03) / 0.06, 0, 1)
        m = gblur(m, 0.7)
        # foam drawn over the head (its translucent fringe): take the head-only pass there, i.e. the head in front
        # of the foam. The head-only pass shades the head identically (median |diff| 0-0.016 measured).
        pk_f = np.minimum(full[..., 0] - full[..., 1], full[..., 2] - full[..., 1])
        pk_o = np.minimum(obj[..., 0] - obj[..., 1], obj[..., 2] - obj[..., 1])
        wf = gblur(smoothstep(0.015, 0.06, pk_f - pk_o) * (m > 0.5), 1.0)[..., None]
        full = full + (obj - full) * wf
    else:
        m = np.zeros((H, W), np.float32)

    z, hx, hz = depth_map(W, H, a.cam, 0.01, want_xz=True)
    u = (hx + 0.6) / 1.2
    v = (hz + 0.75) / 1.5

    bg = nohead.copy()
    # ---- grime grade (carpet UV space), foam excluded by colour
    if a.seam >= 0:
        coord = u if a.seam_axis == "u" else v
        d = (coord - a.seam) if a.dirty_below else (a.seam - coord)       # >0 on the dirty side
        gw = smoothstep(a.feather[1], a.feather[0], d)
        r, g, b = bg[..., 0], bg[..., 1], bg[..., 2]
        foam = smoothstep(0.05, 0.14, np.minimum(b - g, r - g + 0.02)) * smoothstep(0.02, 0.08, b - r * 0.55)
        foam = gblur(foam, 3)
        gw = gw * (1 - foam)
        lum = (bg * LUMA).sum(-1, keepdims=True)
        olive = lum * np.array([1.04, 1.0, 0.80], np.float32)
        graded = (bg + (olive - bg) * a.grime_desat) * a.grime_dark
        if a.blotch > 0:
            n1 = fbm(hx / 0.11, hz / 0.11, 3.0)
            n2 = fbm(hx / 0.035, hz / 0.035, 9.0)
            bl = smoothstep(0.52, 0.72, n1 * 0.8 + n2 * 0.2)
            stain = graded * np.array([0.78, 0.74, 0.66], np.float32)
            graded = graded + (stain - graded) * (a.blotch / 0.22 * 0.9 * bl)[..., None]
        bg = bg + (graded - bg) * gw[..., None]
    # ---- contact + cast shadow of the head on the pile
    if a.head is not None:
        cx, cz = a.head
        R = a.head_r
        dd = np.sqrt((hx - cx) ** 2 + (hz - cz) ** 2)
        ao = a.ao * np.exp(-np.maximum(dd - R, 0) / a.ao_w) * smoothstep(R * 0.6, R * 0.95, dd)
        p, y = np.radians(a.light[0]), np.radians(a.light[1])
        L = np.array([np.sin(y) * np.cos(p), -np.sin(p), np.cos(y) * np.cos(p)])
        off = a.head_h * np.array([L[0], L[2]]) / max(-L[1], 0.15)
        ox, oz = cx + off[0], cz + off[1]
        sx, sz = hx - cx, hz - cz
        t = np.clip((sx * off[0] + sz * off[1]) / (off @ off + 1e-9), 0, 1)
        dc = np.sqrt((sx - t * off[0]) ** 2 + (sz - t * off[1]) ** 2)
        soft = R * (0.25 + 0.5 * t)
        cast = a.cast * (1 - smoothstep(R - soft * 0.5, R + soft, dc)) * (1 - 0.5 * t)
        sh = 1 - (1 - ao) * (1 - cast)
        sh = gblur(sh.astype(np.float32), 2.0)
        bg = bg * (1 - sh[..., None])
    # ---- plane depth of field on the background, head laid over sharp
    focus = a.focus
    if a.focus_world is not None:
        fwd = unity_rot(*a.cam[3:6]) @ np.array([0.0, 0.0, 1.0])
        focus = float(np.dot(np.array(a.focus_world) - np.array(a.cam[:3]), fwd))
    rmax = 0.0
    if a.blur > 0:
        bg, rmax = dof(bg, z, focus, a.blur * W, a.near, a.cap)
    out = bg * (1 - m[..., None]) + full * m[..., None]
    if a.foam_soften:
        x0, y0, x1, y1, amt = a.foam_soften
        box = np.zeros((H, W), np.float32)
        box[int(y0 * H):int(y1 * H), int(x0 * W):int(x1 * W)] = 1
        box = gblur(box, 0.03 * W)
        r, g, b = out[..., 0], out[..., 1], out[..., 2]
        foam = smoothstep(0.05, 0.14, np.minimum(b - g, r - g + 0.02))
        lum = (out * LUMA).sum(-1, keepdims=True)
        k = (box * foam * amt)[..., None]
        out = out + (lum * np.array([1.0, 0.92, 0.95], np.float32) * 0.9 - out) * k
    # ---- global grade: exposure, S-curve, saturation, lift
    x = out * a.expo
    x = x / (1 + np.maximum(x - 0.85, 0) * 0.9)                 # soft shoulder for the brightest cream tips
    s = np.clip(x, 0, 1)
    s = s * s * (3 - 2 * s)
    x = x + (s - x) * a.curve
    lum = (x * LUMA).sum(-1, keepdims=True)
    x = lum + (x - lum) * a.sat
    if a.lift:
        x = a.lift + x * (1 - a.lift)
    img = Image.fromarray(np.clip(x * 255 + 0.5, 0, 255).astype(np.uint8))
    if a.crop:
        cx0, cy0, cw, ch = a.crop
        img = img.crop((cx0, cy0, cx0 + cw, cy0 + ch))
    if a.final:
        img = img.resize(tuple(a.final), Image.LANCZOS)
    img.save(a.out, optimize=True)
    arr = np.asarray(img, dtype=np.float32) / 255.0
    L2 = (arr * LUMA).sum(-1)
    if a.preview:
        pdir = os.path.join(os.path.dirname(os.path.abspath(a.out)), "preview")
        os.makedirs(pdir, exist_ok=True)
        pw = a.preview
        img.resize((pw, round(img.size[1] * pw / img.size[0])), Image.LANCZOS).save(os.path.join(pdir, os.path.basename(a.out)))
    print(f"{a.out}: {img.size[0]}x{img.size[1]} focus {focus:.3f} m, max blur {rmax:.1f} px, "
          f"lum mean {L2.mean():.3f} p5 {np.percentile(L2, 5):.3f} p95 {np.percentile(L2, 95):.3f}")


if __name__ == "__main__":
    main()
