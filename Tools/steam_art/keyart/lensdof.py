"""Plane-aware depth of field for the key-art plates (Refluff Steam capsules).

The carpet shells are drawn without depth that URP's Bokeh DoF can use (the whole rug blurs as if it were
infinitely far, only the opaque tool head stays sharp), so the plates get their lens blur here instead: every
pixel's depth is the distance along the camera axis to the carpet plane (y = PLANE m), computed from the same
camera StoreShotApi.RenderView used (position, Unity Euler angles, vertical FOV). The foam and the tool head stand
only a few cm above the plane, so they take the depth of the pile under them, which is right when they sit near
the focus distance (the usual case: the subject is what is in focus).

    python lensdof.py in.png out.png --cam px py pz rx ry rz fov --focus 0.6 --blur 40 [--near 1.0] [--plane 0.01]

--blur is the circle-of-confusion radius in px for a point at infinity; a point at depth z gets
blur * |1 - focus / z| (near points, z < focus, are scaled by --near). A soft tilt-shift look: 0.8-1.5 % of the width.
"""
import argparse
import math

import numpy as np
from PIL import Image, ImageFilter


def unity_rot(rx, ry, rz):
    ax, ay, az = (math.radians(a) for a in (rx, ry, rz))
    Rx = np.array([[1, 0, 0], [0, math.cos(ax), -math.sin(ax)], [0, math.sin(ax), math.cos(ax)]])
    Ry = np.array([[math.cos(ay), 0, math.sin(ay)], [0, 1, 0], [-math.sin(ay), 0, math.cos(ay)]])
    Rz = np.array([[math.cos(az), -math.sin(az), 0], [math.sin(az), math.cos(az), 0], [0, 0, 1]])
    return Ry @ Rx @ Rz          # Unity applies Z, then X, then Y


def depth_map(w, h, cam, plane, want_xz=False):
    px, py, pz, rx, ry, rz, fov = cam
    t = math.tan(math.radians(fov) * 0.5)
    xs = ((np.arange(w) + 0.5) / w * 2 - 1) * t * (w / h)
    ys = (1 - (np.arange(h) + 0.5) / h * 2) * t
    X, Y = np.meshgrid(xs, ys)
    d = np.stack([X, Y, np.ones_like(X)], -1) @ unity_rot(rx, ry, rz).T   # world ray per pixel, camera-z = 1
    dy = d[..., 1]
    with np.errstate(divide="ignore", invalid="ignore"):
        z = (plane - py) / dy                                               # camera-axis depth (dir has z_cam = 1)
    ok = (dy < -1e-6) & (z > 0)
    z = np.where(ok, z, 1e3)
    if want_xz:
        hx = np.where(ok, px + z * d[..., 0], 1e3)
        hz = np.where(ok, pz + z * d[..., 2], 1e3)
        return z, hx, hz
    return z


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("inp")
    ap.add_argument("out")
    ap.add_argument("--cam", type=float, nargs=7, required=True)
    ap.add_argument("--focus", type=float, default=0.0, help="focus depth along the camera axis (m)")
    ap.add_argument("--focus-world", type=float, nargs=3, default=None, help="focus on this world point instead")
    ap.add_argument("--blur", type=float, required=True)
    ap.add_argument("--near", type=float, default=1.0)
    ap.add_argument("--plane", type=float, default=0.01)
    ap.add_argument("--max", type=float, default=0.0, help="cap on the blur radius (px), 0 = 1.5 x --blur")
    a = ap.parse_args()

    im = Image.open(a.inp).convert("RGB")
    w, h = im.size
    z = depth_map(w, h, a.cam, a.plane)
    if a.focus_world is not None:
        R = unity_rot(*a.cam[3:6])
        fwd = R @ np.array([0.0, 0.0, 1.0])
        a.focus = float(np.dot(np.array(a.focus_world) - np.array(a.cam[:3]), fwd))
    r = a.blur * (1.0 - a.focus / z)
    r = np.where(r < 0, -r * a.near, r)
    r = np.clip(r, 0, a.max if a.max > 0 else a.blur * 1.5)

    levels = [0.0]
    while levels[-1] < r.max():
        levels.append(1.5 if levels[-1] == 0 else levels[-1] * 2)
    lv = np.array(levels)
    # fractional level index of every pixel
    idx = np.interp(r, lv, np.arange(len(lv)))
    src = np.asarray(im, dtype=np.float32)
    acc = np.zeros_like(src)
    for k, rad in enumerate(levels):
        wk = np.clip(1.0 - np.abs(idx - k), 0, 1)[..., None]
        if not wk.any():
            continue
        img = src if rad == 0 else np.asarray(im.filter(ImageFilter.GaussianBlur(rad * 0.5)), dtype=np.float32)
        acc += img * wk
    Image.fromarray(np.clip(acc + 0.5, 0, 255).astype(np.uint8)).save(a.out)
    print(f"{a.out}: {w}x{h} focus {a.focus:.3f} m, blur px min {r.min():.1f} max {r.max():.1f}, levels {levels}")


if __name__ == "__main__":
    main()
