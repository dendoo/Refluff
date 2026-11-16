"""Finish a raw key-art render into a plate: supersample down, plane depth of field, light grade.

    python finish.py raw.png out.png --size 3840 2000 --cam px py pz rx ry rz fov
                     [--focus-world x y z | --focus d] [--blur 0.04] [--near 0.7] [--sat 1.10] [--curve 0.22]
                     [--lift 0.0] [--warm 0.0]

--blur is the circle-of-confusion radius at infinity as a fraction of the plate width (0 = no DoF).
The raw render may be any integer multiple of --size (StoreShotApi.RenderView at 1.5x or 2x): it is
Lanczos-downsampled first, so the fibres are anti-aliased before the blur. The grade is deliberately light:
a small S-curve (--curve), saturation (--sat), optional warm shift. No vignette: the capsule crops differ.
"""
import argparse

import numpy as np
from PIL import Image, ImageFilter

from lensdof import depth_map, unity_rot


def dof(im, cam, focus, blur_px, near, plane=0.01, cap=1.5, z=None):
    w, h = im.size
    if z is None:
        z = depth_map(w, h, cam, plane)
    r = blur_px * (1.0 - focus / z)
    r = np.where(r < 0, -r * near, r)
    r = np.clip(r, 0, blur_px * cap)
    levels = [0.0]
    while levels[-1] < r.max():
        levels.append(1.5 if levels[-1] == 0 else levels[-1] * 2)
    idx = np.interp(r, np.array(levels), np.arange(len(levels)))
    src = np.asarray(im, dtype=np.float32)
    acc = np.zeros_like(src)
    for k, rad in enumerate(levels):
        wk = np.clip(1.0 - np.abs(idx - k), 0, 1)[..., None]
        if not wk.any():
            continue
        img = src if rad == 0 else np.asarray(im.filter(ImageFilter.GaussianBlur(rad * 0.5)), dtype=np.float32)
        acc += img * wk
    return Image.fromarray(np.clip(acc + 0.5, 0, 255).astype(np.uint8)), float(r.max())


def grade(im, sat, curve, lift, warm):
    x = np.asarray(im, dtype=np.float32) / 255.0
    s = x * x * (3 - 2 * x)
    x = x + (s - x) * curve
    lum = (x * np.array([0.2126, 0.7152, 0.0722], dtype=np.float32)).sum(-1, keepdims=True)
    x = lum + (x - lum) * sat
    if warm:
        x = x * np.array([1 + warm, 1.0, 1 - warm], dtype=np.float32)
    if lift:
        x = lift + x * (1 - lift)
    return Image.fromarray(np.clip(x * 255 + 0.5, 0, 255).astype(np.uint8))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("raw")
    ap.add_argument("out")
    ap.add_argument("--size", type=int, nargs=2, required=True)
    ap.add_argument("--cam", type=float, nargs=7, required=True)
    ap.add_argument("--focus", type=float, default=0.0)
    ap.add_argument("--focus-world", type=float, nargs=3, default=None)
    ap.add_argument("--blur", type=float, default=0.04)
    ap.add_argument("--near", type=float, default=0.7)
    ap.add_argument("--cap", type=float, default=1.5)
    ap.add_argument("--sat", type=float, default=1.10)
    ap.add_argument("--curve", type=float, default=0.22)
    ap.add_argument("--lift", type=float, default=0.0)
    ap.add_argument("--warm", type=float, default=0.0)
    ap.add_argument("--mask", nargs=2, default=None, help="obj.png bg.png: tool-head-only pass and empty pass (StoreShot mask)")
    ap.add_argument("--obj-world", type=float, nargs=3, default=None, help="depth of the masked object = this point")
    ap.add_argument("--ao", type=float, nargs=5, default=None, help="cx cz R width strength: soft contact shadow on the pile")
    ap.add_argument("--preview", type=int, default=960, help="also write preview/<out name> this wide (0 = none)")
    a = ap.parse_args()

    im = Image.open(a.raw).convert("RGB")
    W, H = a.size
    if im.size != (W, H):
        im = im.resize((W, H), Image.LANCZOS)
    focus = a.focus
    if a.focus_world is not None:
        fwd = unity_rot(*a.cam[3:6]) @ np.array([0.0, 0.0, 1.0])
        focus = float(np.dot(np.array(a.focus_world) - np.array(a.cam[:3]), fwd))
    rmax = 0.0
    z, hx, hz = depth_map(W, H, a.cam, 0.01, want_xz=True)
    m = None
    if a.mask:
        ob = np.asarray(Image.open(a.mask[0]).convert("RGB").resize((W, H), Image.BILINEAR), dtype=np.int32)
        bg = np.asarray(Image.open(a.mask[1]).convert("RGB").resize((W, H), Image.BILINEAR), dtype=np.int32)
        m = (np.abs(ob - bg).sum(-1) > 12).astype(np.float32)
        m = np.asarray(Image.fromarray((m * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.0)), dtype=np.float32) / 255
        if a.obj_world is not None:
            fwd = unity_rot(*a.cam[3:6]) @ np.array([0.0, 0.0, 1.0])
            zo = float(np.dot(np.array(a.obj_world) - np.array(a.cam[:3]), fwd))
            z = np.where(m > 0.5, zo, z)
    if a.ao is not None:
        cx, cz, R, wd, st = a.ao
        dd = np.sqrt((hx - cx) ** 2 + (hz - cz) ** 2)
        ao = st * np.exp(-np.maximum(dd - R, 0) / wd) * (dd < 5)
        if m is not None:
            ao = ao * (1 - m)
        x = np.asarray(im, dtype=np.float32) * (1 - ao[..., None])
        im = Image.fromarray(np.clip(x + 0.5, 0, 255).astype(np.uint8))
    if a.blur > 0:
        im, rmax = dof(im, a.cam, focus, a.blur * W, a.near, cap=a.cap, z=z)
    im = grade(im, a.sat, a.curve, a.lift, a.warm)
    im.save(a.out, optimize=True)
    if a.preview:
        pw = a.preview
        import os
        pdir = os.path.join(os.path.dirname(os.path.abspath(a.out)), "preview")
        os.makedirs(pdir, exist_ok=True)
        im.resize((pw, round(H * pw / W)), Image.LANCZOS).save(os.path.join(pdir, os.path.basename(a.out)))
    print(f"{a.out}: {W}x{H} focus {focus:.3f} m, max blur {rmax:.1f} px")


if __name__ == "__main__":
    main()
