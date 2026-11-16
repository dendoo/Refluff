"""Refluff: Carpet Cleaning ASMR - FINAL Steam store / library / event art, one reproducible build.

    python Tools/steam_art/build_store_art.py            (run from the project root; ~1-2 min)
    python Tools/steam_art/verify_assets.py              (Steam size / format checks)

Picks (three judges over the "split", "macro" and "director" capsule sets, SteamStore/_work/capsules):
    macro   (PLUSH MACRO)  header, library header (= header), main, vertical, library capsule, event cover,
                           event header, page background
    director               small capsule (wide5 plate, painterly-simplified, wordmark_small nearly fills it)
    split   (THE PROMISE)  library hero (hero2 plate, exact 1.5x downscale, no logo / text)
and the fixes the judges asked for, all through ONE grade and ONE logo treatment so the family matches:
  * grime: every plate's soiled pile is recoloured to one grey-olive target (hue ~40, HSV sat ~0.28, value ~0.35)
    through a plate-space soil mask, so it never reads as a tan / brown rug; the orange "feather" where the grime
    meets the clean rug is pulled into the grime look (a crisp dirty | clean edge instead of a stain-like glow);
  * clean side warmed and enriched, foam and blues untouched, filmic S-curve, two-radius highlight bloom;
  * soft contact occlusion under the bristle skirt (the brush no longer looks pasted on);
  * light vignette (0.20), faded out over the outer 5 % where the art is dark, and the logo glow fades out before
    the frame edge, so the tiles do not dissolve into Steam's dark UI (#1b2838; edges now >= 1.6:1);
  * logo (SteamStore/logo, never redrawn: only scaled premultiplied + placed) over one soft dark glow built from
    its blurred alpha; the glow deepens automatically where pink foam sits behind the pink bubbles;
  * seeded +-0.5 LSB dither on every 8-bit export (no contour banding in the soft gradients).
Art-director round (after verify_assets passed):
  * brush cap: the magenta / maroon crescent on the cap's inner ring (violet smear on hero2) was blurred foam
    showing through holes in finish2's head silhouette; every plate with raw passes gets those holes refilled with
    the head's own render (cap_fix), so every capsule, event asset and the hero show a clean white / grey cap;
  * library hero: hero2 re-finished from its raw passes with the brush scaled x0.74 and centred on the 860x380
    safe area (it fits now, with its shadows following it);
  * library logo: trimmed tightly (1280 x ~373: letterforms + shadow + 3 px), a pure crop of the official PNG;
  * screenshots (ACES re-shoot 2026-09-26): final_order.json's order per the steam-izdat guide (1 striking hero
    reveal, 2 gameplay scraper, 3 progression pay slip, 4 striking rya, then gameplay / striking alternating, no two
    neighbouring shots on the same rug); SHOT_MOVE is empty. The hero reveal and the rya are the two no-HUD beauty shots
    (both HUD hints read 'Wet it down' under another tool), each a tighter crop of its supersampled 3840x2160 scene
    layer (hero 1:1 at 1.5x: rug ~75 % of the frame, wand readable; rya 1.25x since its 2026-09-26 retake: rug top
    edge in frame, less empty tile); the pay slip's terraced ray-glow halo is smoothed in its HUD layer.
Also writes the library logo / icons (copies and resizes of SteamStore/logo), the final screenshot set
(SteamStore/_work/screens/final_order.json) and three review mocks in SteamStore/_preview/.
Deterministic: no clock, dither noise seeded from the asset name, fixed parameters -> byte-identical output.
"""
import json
import os
import shutil
import sys
import time
import zlib

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage as ndi
from scipy.spatial import ConvexHull

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "capsule_variants"))
sys.path.insert(0, os.path.join(HERE, "keyart"))
sys.path.insert(0, HERE)
from macro import resize_f, resize_prem, load_logo, smoothstep, luma, hsv, hue_near   # noqa: E402
from director import simplify                                                        # noqa: E402
from finish2 import dof as f2_dof, gblur as f2_gblur, fbm as f2_fbm                  # noqa: E402  (key-art finisher)
from lensdof import depth_map, unity_rot                                             # noqa: E402
from compose_screens import resize_linear                                            # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
KEY = os.path.join(ROOT, "SteamStore", "_work", "keyart")
LOGO = os.path.join(ROOT, "SteamStore", "logo")
SCREENS = os.path.join(ROOT, "SteamStore", "_work", "screens")
OUT = os.path.join(ROOT, "SteamStore")
FONT = os.path.join(ROOT, "Tools", "steam_art", "fonts", "Fredoka[wdth,wght].ttf")
MARGIN = 0.04                        # logo stays out of the outer 4 % of every capsule
STEAM_UI = np.array([0x1b, 0x28, 0x38], np.float32) / 255.0

# ------------------------------------------------------------------------------------------------ io
_plates, _pmasks = {}, {}


def plate(name):
    """Key-art plate as float32 RGB 0..1 (cached); plates with finish2 raw passes get the brush-cap retouch."""
    if name not in _plates:
        p = np.asarray(Image.open(os.path.join(KEY, name + ".png")).convert("RGB"), np.float32) / 255.0
        if name in RAW:
            p = cap_fix(p, RAW[name])
        _plates[name] = p
    return _plates[name]


def crop_box(name, x0, y0, wfrac, size):
    """Plate-pixel box: left x0 / top y0 as plate fractions, width wfrac of the plate, height from the aspect."""
    H, W = plate(name).shape[:2]
    cw = wfrac * W
    ch = cw * size[1] / size[0]
    bx = (x0 * W, y0 * H, x0 * W + cw, y0 * H + ch)
    assert bx[0] >= -1.0 and bx[1] >= -1.0 and bx[2] <= W + 1.0 and bx[3] <= H + 1.0, (name, bx, (W, H))
    return (max(0.0, bx[0]), max(0.0, bx[1]), min(float(W), bx[2]), min(float(H), bx[3]))


def blur3(a, s):
    if s <= 0:
        return a
    if a.ndim == 2:
        return ndi.gaussian_filter(a, s, mode="nearest")
    return np.stack([ndi.gaussian_filter(a[..., i], s, mode="nearest") for i in range(a.shape[2])], axis=2)


def to_img(a, seed_name):
    """Float RGB -> 8-bit PIL with seeded +-0.5 LSB dither where the art is smooth (defocused gradients would
    otherwise quantise into 1-LSB contour rings under Steam's JPEG + dark fade); textured areas need none, which
    keeps the PNGs small. Deterministic: the noise is seeded from the asset name."""
    a = np.clip(a, 0, 1)
    L = luma(a) * 255.0
    mu = ndi.uniform_filter(L, 5)
    sd = np.sqrt(np.maximum(ndi.uniform_filter(L * L, 5) - mu * mu, 0.0))
    smooth = (1.0 - smoothstep(1.5, 4.0, sd))[..., None]
    rng = np.random.default_rng(zlib.crc32(seed_name.encode()))
    n = (rng.random(a.shape[:2], np.float32)[..., None] - 0.5) * smooth
    return Image.fromarray(np.clip(np.floor(a * 255.0 + 0.5 + n), 0, 255).astype(np.uint8), "RGB")


def save_image(im, path, **kw):
    """Write via a temp file + os.replace, retrying briefly: on Windows a viewer / indexer that just opened the
    previous file can make a direct overwrite fail with EINVAL / EACCES."""
    os.makedirs(os.path.dirname(path), exist_ok=True)
    fmt = "JPEG" if path.lower().endswith((".jpg", ".jpeg")) else "PNG"
    tmp = path + ".tmp"
    im.save(tmp, format=fmt, **kw)
    for i in range(20):
        try:
            os.replace(tmp, path)
            return path
        except OSError:
            time.sleep(0.25 * (i + 1))
    raise OSError(f"could not replace {path}")


def copy_file(src, dst):
    """Byte copy with the same temp + replace + retry pattern as save_image."""
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    shutil.copyfile(src, dst + ".tmp")
    for i in range(20):
        try:
            os.replace(dst + ".tmp", dst)
            return dst
        except OSError:
            time.sleep(0.25 * (i + 1))
    raise OSError(f"could not replace {dst}")


def save_png(im, rel):
    return save_image(im, os.path.join(OUT, rel), optimize=True)


# ------------------------------------------------------------------------------------------------ plate-space masks
MS = 4                               # masks are built once per plate at 1/MS scale, so every crop shares them


def _largest(b):
    lab, n = ndi.label(b)
    if n == 0:
        return b
    sizes = ndi.sum(b, lab, index=np.arange(1, n + 1))
    return lab == (1 + int(np.argmax(sizes)))


def plate_masks(name):
    """Soft masks in plate space (1/MS scale), cached:
    soil  - soiled pile incl. the orange feather band where it meets the clean rug (1 = grime look wanted)
    core  - the flat grime itself (used for the per-plate grime statistics)
    foam  - pink-violet foam;  brush - bristles + housing (one solid silhouette);  bristle - blue skirt only
    plus 'g0' (mean grime RGB of the plate). Colour tests run on a slightly defocused copy so single cream / gold
    fibres are not mistaken for soil; the band only counts next to real grime (spatial proximity)."""
    if name in _pmasks:
        return _pmasks[name]
    p = plate(name)
    H, W = p.shape[:2]
    a = resize_f(p, (W // MS, H // MS))
    w4 = a.shape[1]
    soft = np.clip(blur3(a, 0.006 * w4), 0, 1)
    h, s, v = hsv(soft)
    hp, sp, vp = hsv(np.clip(a, 0, 1))
    foam = hue_near(hp, 300.0, 45.0) * smoothstep(0.12, 0.30, sp)
    foam = smoothstep(0.30, 0.60, blur3(foam, 0.004 * w4))          # the mound, not stray magenta rug fibres
    # brush: the largest bright-blue blob (bristles) + the low-saturation housing touching it
    bri = hue_near(h, 208.0, 16.0) * smoothstep(0.30, 0.40, s) * smoothstep(0.45, 0.55, v) > 0.5
    bri = ndi.binary_fill_holes(_largest(ndi.binary_opening(bri, iterations=2)))
    ys, xs = np.nonzero(bri)
    bh = ys.max() - ys.min() + 1
    hous = (s < 0.28) & (v > 0.55)
    lab, n = ndi.label(hous)
    touch = np.unique(lab[ndi.binary_dilation(bri, iterations=max(2, int(0.06 * bh))) & (lab > 0)])
    brush = bri | np.isin(lab, touch[touch > 0])
    brush = ndi.binary_fill_holes(ndi.binary_closing(brush, iterations=6))
    brush_f = np.clip(blur3(brush.astype(np.float32), 0.8), 0, 1)
    bri_f = np.clip(blur3(bri.astype(np.float32), 0.8), 0, 1)
    free = (1.0 - foam) * (1.0 - brush_f)
    core = hue_near(h, 31.0, 8.0) * smoothstep(0.30, 0.36, s) * (1.0 - smoothstep(0.52, 0.60, s)) \
        * (1.0 - smoothstep(0.58, 0.66, v)) * free
    core = np.clip(blur3(core, 0.004 * w4), 0, 1)
    near = smoothstep(0.03, 0.30, blur3(core, 0.05 * w4))          # the feather runs up to ~0.2 W wide (DoF)
    band = hue_near(h, 21.0, 13.0) * smoothstep(0.28, 0.36, s) * (1.0 - smoothstep(0.84, 0.92, s)) \
        * (1.0 - smoothstep(0.60, 0.70, v)) * near * free
    soil = np.clip(blur3(np.maximum(core, band), 0.003 * w4), 0, 1) * free
    sel = core > 0.8
    g0 = a[sel].mean(axis=0) if sel.sum() > 100 else np.array([0.44, 0.35, 0.25], np.float32)
    band = np.clip(soil - core, 0, 1)
    _pmasks[name] = dict(soil=soil.astype(np.float32), core=core.astype(np.float32), band=band.astype(np.float32),
                         foam=foam.astype(np.float32),
                         brush=brush_f.astype(np.float32), bristle=bri_f.astype(np.float32), g0=g0.astype(np.float32))
    return _pmasks[name]


def crop_masks(name, box, size):
    """The plate masks for the same crop box, resampled to the asset size."""
    pm = plate_masks(name)
    H, W = plate(name).shape[:2]
    mh, mw = pm["soil"].shape
    kx, ky = mw / W, mh / H
    mb = (box[0] * kx, box[1] * ky, box[2] * kx, box[3] * ky)
    out = {k: np.clip(resize_f(pm[k], size, mb), 0, 1) for k in ("soil", "core", "band", "foam", "brush", "bristle")}
    out["g0"] = pm["g0"]
    return out


def render(name, x0, y0, wfrac, size, masks=True):
    """Crop + Lanczos (float) of plate `name` and (optionally) its masks, same framing."""
    box = crop_box(name, x0, y0, wfrac, size)
    rgb = np.clip(resize_f(plate(name), size, box), 0, 1)
    return rgb, (crop_masks(name, box, size) if masks else None)


# ------------------------------------------------------------------------------------------------ plate retouch
# finish2 passes (SteamStore/_work/keyart/raw3, same camera as the plate): <raw>_full (1.5x), _nohead (1.5x),
# _obj (head only) and _empty (nothing) at plate size. No plate was cropped after finish2, so pixels line up.
RAW = {"hero1_foam_bank_v2": "hero1v2", "hero2_split_brush_v2": "hero2v2",
       "wide5_macro_brush_v2": "wide5v2", "tallD_macro_brush_v2": "tallDv2"}


def _load_rgb(path, size=None):
    im = Image.open(path).convert("RGB")
    if size is not None and im.size != size:
        im = im.resize(size, Image.LANCZOS)
    return np.asarray(im, np.float32) / 255.0


def head_passes(raw, size):
    """The head layer of one plate: the full render with finish2's foam-fringe fix, finish2's own silhouette m
    (|head-only - empty|, which has HOLES where the white / grey cap matches the empty pass's grey), and m_fix =
    the same silhouette with those interior holes closed (outer anti-aliased edge untouched)."""
    pre = os.path.join(KEY, "raw3", raw)
    full = _load_rgb(pre + "_full.png", size)
    obj = _load_rgb(pre + "_obj.png", size)
    emp = _load_rgb(pre + "_empty.png", size)
    m = f2_gblur(np.clip((np.abs(obj - emp).sum(-1) - 0.03) / 0.06, 0, 1), 0.7)
    sil = _largest(m > 0.5)
    ys, _ = np.nonzero(sil)
    r = max(3, int(0.03 * (ys.max() - ys.min())))
    yy, xx = np.mgrid[-r:r + 1, -r:r + 1]
    pad = r + 2
    cl = ndi.binary_closing(np.pad(sil, pad), structure=(xx * xx + yy * yy) <= r * r)[pad:-pad, pad:-pad]
    inner = ndi.binary_erosion(ndi.binary_fill_holes(cl), iterations=2)
    # the cap is convex (disc + rim band): its hull also closes notches that open onto the outline (hero2's rim)
    h, s, _ = hsv(obj)
    blue = (hue_near(h, 208.0, 20.0) * smoothstep(0.25, 0.35, s)) > 0.5
    cap = _largest(ndi.binary_erosion(sil & ~blue, iterations=2))
    edge = cap & ~ndi.binary_erosion(cap)
    py, px = np.nonzero(edge)
    hv = ConvexHull(np.column_stack([px, py]).astype(np.float64)).vertices
    hull = Image.new("L", (size[0], size[1]), 0)
    ImageDraw.Draw(hull).polygon([(int(px[i]), int(py[i])) for i in hv], fill=255)
    inner = inner | (np.asarray(hull) > 0)                    # 2 px inside the outline (cap was eroded by 2)
    mfix = np.maximum(m, inner.astype(np.float32))
    # finish2's foam-fringe fix (foam drawn over the head -> the head-only pass there), over the whole hole-free
    # silhouette: inside the old holes a foam bubble in front of the rim would otherwise stay as a pink stain
    pk_f = np.minimum(full[..., 0] - full[..., 1], full[..., 2] - full[..., 1])
    pk_o = np.minimum(obj[..., 0] - obj[..., 1], obj[..., 2] - obj[..., 1])
    wf = f2_gblur(smoothstep(0.015, 0.06, pk_f - pk_o) * (mfix > 0.5), 1.0)[..., None]
    return full + (obj - full) * wf, m, mfix


def cap_fix(p, raw):
    """Brush-cap retouch (art director): finish2 laid the sharp head over the defocused background through the
    holed silhouette, so blurred pink foam showed through the cap as a magenta / maroon crescent on its inner ring
    (a violet smear on hero2's rim). Refill exactly those holes with the head's own render, graded to the plate by a
    per-channel LUT fitted on the solid head pixels (residual < 1 level): the cap goes back to its white and grey."""
    H, W = p.shape[:2]
    full, m, mfix = head_passes(raw, (W, H))
    hole = np.clip(mfix - m, 0.0, 1.0)
    solid = (m > 0.995) & (mfix > 0.5)
    bins = np.linspace(0.0, 1.0, 65)
    ctr = 0.5 * (bins[:-1] + bins[1:])
    g = np.empty_like(p)
    for c in range(3):
        x, y = full[..., c][solid], p[..., c][solid]
        idx = np.clip(np.digitize(x, bins) - 1, 0, 63)
        med = np.array([np.median(y[idx == i]) if (idx == i).sum() > 20 else np.nan for i in range(64)])
        ok = ~np.isnan(med)
        g[..., c] = np.interp(full[..., c], ctr[ok], med[ok])
    return p + (g - p) * hole[..., None]


# Library hero: hero2 re-finished from its raw passes (finish2 settings recovered by matching the plate: mean
# difference 0.6 of 255 away from the head) with the head scaled so it fits Steam's centre safe area.
HERO2_CAM = (0.003, 0.48, -1.059, 30.0, -2.0, 0.0, 15.0)
HERO2_F2 = dict(head=(-0.024, -0.30), head_r=0.054, head_h=0.030, light=(25.0, 80.0), ao=0.42, ao_w=0.012,
                cast=0.30, focus_world=(-0.024, 0.01, -0.30), blur=0.02, near=0.8, cap=1.5, seam=0.462,
                feather=(0.035, -0.004), desat=0.48, dark=0.85, blotch=0.3, expo=1.2, curve=0.18, sat=1.04)
HERO_BRUSH_SCALE = 0.74               # brush 480 -> ~355 px tall at 3840x1240 (safe area 380)
HERO_SAFE = (1490, 430, 2350, 810)    # 860x380 centred on (1920, 620) of the 3840x1240 hero
HERO2S = "hero2_split_brush_v2_safe"  # the re-finished plate's cache name


def refinish_hero2(scale):
    """finish2's pipeline for hero2 (grime grade in carpet UV, contact + cast shadow, plane DoF, sharp head laid
    over, global grade) with the head layer scaled by `scale` about its footprint and centred on the safe area; the
    shadows follow the smaller head (radius, height and the new footprint on the pile). Registers the plate as
    HERO2S and returns the head's box in 3840x1240 px."""
    f = HERO2_F2
    W, H = 5760, 1860
    full, _, mfix = head_passes(RAW[HERO2], (W, H))
    bg = _load_rgb(os.path.join(KEY, "raw3", RAW[HERO2] + "_nohead.png"), (W, H))
    z, hx, hz = depth_map(W, H, HERO2_CAM, 0.01, want_xz=True)
    # head layer (premultiplied), scaled about the footprint centre's pixel, then centred on the safe area
    cx, cz = f["head"]
    py, px = np.unravel_index(np.argmin((hx - cx) ** 2 + (hz - cz) ** 2), hx.shape)
    ys, xs = np.nonzero(mfix > 0.01)
    bx0, by0, bx1, by1 = xs.min() - 8, ys.min() - 8, xs.max() + 9, ys.max() + 9
    lay = np.concatenate([full * mfix[..., None], mfix[..., None]], axis=2)[by0:by1, bx0:bx1]
    tw, th = int(round((bx1 - bx0) * scale)), int(round((by1 - by0) * scale))
    lay = np.clip(resize_prem(lay, (tw, th)), 0.0, 1.0)
    a = lay[..., 3] > 0.5
    ly, lx = np.nonzero(a)
    sx0 = px + (bx0 - px) * scale                          # scaled layer origin (anchor stays put) ...
    sy0 = py + (by0 - py) * scale
    tcx, tcy = 1.5 * 0.5 * (HERO_SAFE[0] + HERO_SAFE[2]), 1.5 * 0.5 * (HERO_SAFE[1] + HERO_SAFE[3])
    ox = int(round(tcx - (sx0 + 0.5 * (lx.min() + lx.max() + 1))))  # ... then shifted onto the safe-area centre
    oy = int(round(tcy - (sy0 + 0.5 * (ly.min() + ly.max() + 1))))
    X0, Y0 = int(round(sx0)) + ox, int(round(sy0)) + oy
    layer = np.zeros((H, W, 4), np.float32)
    layer[Y0:Y0 + th, X0:X0 + tw] = lay
    npx, npy = int(px + ox), int(py + oy)                # the anchor scales onto itself, then moves by the shift
    ncx, ncz = float(hx[npy, npx]), float(hz[npy, npx])  # new footprint centre on the pile (world m)
    # 1. grime grade in carpet UV (finish2)
    u = (hx + 0.6) / 1.2
    d = f["seam"] - u
    gw = smoothstep(f["feather"][1], f["feather"][0], d)
    r, g, b = bg[..., 0], bg[..., 1], bg[..., 2]
    foam = smoothstep(0.05, 0.14, np.minimum(b - g, r - g + 0.02)) * smoothstep(0.02, 0.08, b - r * 0.55)
    gw = gw * (1 - f2_gblur(foam, 3))
    lum = (bg * np.array([0.2126, 0.7152, 0.0722], np.float32)).sum(-1, keepdims=True)
    graded = (bg + (lum * np.array([1.04, 1.0, 0.80], np.float32) - bg) * f["desat"]) * f["dark"]
    n1, n2 = f2_fbm(hx / 0.11, hz / 0.11, 3.0), f2_fbm(hx / 0.035, hz / 0.035, 9.0)
    bl = smoothstep(0.52, 0.72, n1 * 0.8 + n2 * 0.2)
    stain = graded * np.array([0.78, 0.74, 0.66], np.float32)
    graded = graded + (stain - graded) * (f["blotch"] / 0.22 * 0.9 * bl)[..., None]
    bg = bg + (graded - bg) * gw[..., None]
    # 2. contact + cast shadow of the (smaller) head on the pile
    R, hh = f["head_r"] * scale, f["head_h"] * scale
    dd = np.sqrt((hx - ncx) ** 2 + (hz - ncz) ** 2)
    ao = f["ao"] * np.exp(-np.maximum(dd - R, 0) / f["ao_w"]) * smoothstep(R * 0.6, R * 0.95, dd)
    p_, y_ = np.radians(f["light"][0]), np.radians(f["light"][1])
    L = np.array([np.sin(y_) * np.cos(p_), -np.sin(p_), np.cos(y_) * np.cos(p_)])
    off = hh * np.array([L[0], L[2]]) / max(-L[1], 0.15)
    ssx, ssz = hx - ncx, hz - ncz
    t = np.clip((ssx * off[0] + ssz * off[1]) / (off @ off + 1e-9), 0, 1)
    dc = np.sqrt((ssx - t * off[0]) ** 2 + (ssz - t * off[1]) ** 2)
    soft = R * (0.25 + 0.5 * t)
    cast = f["cast"] * (1 - smoothstep(R - soft * 0.5, R + soft, dc)) * (1 - 0.5 * t)
    sh = f2_gblur((1 - (1 - ao) * (1 - cast)).astype(np.float32), 2.0)
    bg = bg * (1 - sh[..., None])
    # 3. plane DoF (focus on the original head position), sharp head over it, finish2's global grade
    fwd = unity_rot(*HERO2_CAM[3:6]) @ np.array([0.0, 0.0, 1.0])
    focus = float(np.dot(np.array(f["focus_world"]) - np.array(HERO2_CAM[:3]), fwd))
    bg, _ = f2_dof(bg, z, focus, f["blur"] * W, f["near"], f["cap"])
    out = bg * (1 - layer[..., 3:4]) + layer[..., :3]
    x = out * f["expo"]
    x = x / (1 + np.maximum(x - 0.85, 0) * 0.9)
    s = np.clip(x, 0, 1)
    x = x + (s * s * (3 - 2 * s) - x) * f["curve"]
    lum = (x * np.array([0.2126, 0.7152, 0.0722], np.float32)).sum(-1, keepdims=True)
    x = np.clip(lum + (x - lum) * f["sat"], 0.0, 1.0)
    _plates[HERO2S] = x.astype(np.float32)
    hy, hxs = np.nonzero(layer[..., 3] > 0.5)
    return tuple(v / 1.5 for v in (hxs.min(), hy.min(), hxs.max() + 1, hy.max() + 1))
# Grime target (judges: hue 35-45, HSV sat <= 0.30, value ~0.32-0.36; the plates measure hue 31, sat 0.43-0.46).
GRIME_RGB = np.array([0.405, 0.374, 0.297], np.float32)      # grime tone before the S-curve (hue ~43, sat ~0.27)
GRIME_KEEP = 0.45                                            # share of each fibre's own chroma deviation kept
GRADE = dict(grime=1.0, warm=1.0, contrast=1.0, expo=1.0, curve=0.14, bloom=0.14, vig=0.20, vc=(0.5, 0.5),
             edge=0.75, occl=0.22)


def contact_occlusion(m):
    """Soft darkening of the pile just below / around the bottom of the bristle skirt (0..1 field)."""
    B = m["bristle"]
    ys, xs = np.nonzero(B > 0.5)
    if len(ys) < 50:
        return np.zeros_like(B)
    bh = float(ys.max() - ys.min() + 1)
    cy = ys.mean()
    d = int(round(0.07 * bh))
    Bs = np.zeros_like(B)
    Bs[d:] = B[:-d] if d > 0 else B
    S = np.clip(blur3(Bs, 0.09 * bh) * 1.4, 0, 1)
    H = B.shape[0]
    yy = np.arange(H, dtype=np.float32)[:, None]
    S = S * smoothstep(cy, cy + 0.45 * bh, yy) * (1.0 - m["brush"]) * (1.0 - 0.7 * m["foam"])
    return np.clip(S, 0, 1)


def edge_band(H, W, width=0.05):
    """1 at the frame edge, 0 further in than `width` of the shorter side (smooth)."""
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    d = np.minimum(np.minimum(xx + 0.5, W - xx - 0.5), np.minimum(yy + 0.5, H - yy - 0.5)) / min(H, W)
    return 1.0 - smoothstep(0.0, width, d)


def grade(a, m, **kw):
    """The single Refluff finish. a = float RGB crop, m = crop_masks() of the same framing."""
    g = dict(GRADE)
    g.update(kw)
    a = a.copy()
    H, W = a.shape[:2]
    soil, foam, brush, bri = m["soil"], m["foam"], m["brush"], m["bristle"]
    # 1. soiled pile -> one grey-olive (per-plate gain so every plate's grime lands on the same tone)
    g0 = m["g0"]
    L0 = float(g0 @ np.array([0.2126, 0.7152, 0.0722], np.float32))
    dir0, dir1 = g0 / L0, GRIME_RGB / float(luma(GRIME_RGB[None, None])[0, 0])
    gain = float(luma(GRIME_RGB[None, None])[0, 0]) / L0
    L = luma(a)[..., None]
    keep = (GRIME_KEEP * (1.0 - 0.65 * m["band"]))[..., None]      # the orange feather keeps less of its own tint
    recol = gain * (L * dir1 + (a - L * dir0) * keep)
    k = (g["grime"] * soil)[..., None]
    a = a + (recol - a) * k
    # 2. clean pile warm + rich (reds / creams); foam, blues and the white housing untouched
    wk = (g["warm"] * (1.0 - soil) * (1.0 - foam) * (1.0 - np.maximum(brush, bri)))[..., None]
    L = luma(a)[..., None]
    rich = (L + (a - L) * 1.10) * np.array([1.035, 1.0, 0.93], np.float32)
    a = a + (rich - a) * wk
    # 3. contact occlusion under the skirt
    if g["occl"] > 0:
        a = a * (1.0 - g["occl"] * contact_occlusion(m))[..., None]
    # 4. exposure / contrast / filmic S-curve
    if g["contrast"] != 1.0:
        m0 = float(luma(a).mean())
        a = m0 + (a - m0) * g["contrast"]
    a = np.clip(a * g["expo"], 0.0, 1.0)
    a = a + (a * a * (3.0 - 2.0 * a) - a) * g["curve"]
    # 5. two-radius highlight bloom (foam gloss, housing rim)
    if g["bloom"] > 0:
        hl = a * smoothstep(0.68, 1.0, luma(a))[..., None]
        glow = sum(w * blur3(hl, r * W) for w, r in ((0.6, 0.004), (0.4, 0.016)))
        a = 1.0 - (1.0 - a) * (1.0 - g["bloom"] * glow)
    # 6. light neutral-cool vignette around the subject, faded out over the outer 5 % (tiles keep their edge
    #    against Steam's dark UI instead of dissolving into it)
    if g["vig"] > 0:
        cx, cy = g["vc"]
        yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
        u = (xx / W - cx) / max(cx, 1 - cx)
        v = (yy / H - cy) / max(cy, 1 - cy)
        r = np.sqrt(u * u + v * v) / np.sqrt(2.0)
        dark = 1.0 - smoothstep(0.35, 0.60, luma(a))              # only the dark grime edge is spared
        vm = g["vig"] * smoothstep(0.30, 1.05, r) ** 1.15 * (1.0 - g["edge"] * edge_band(H, W) * dark)
        a = a * (1.0 - vm[..., None] * np.array([1.04, 1.0, 0.97], np.float32))
    return np.clip(a, 0.0, 1.0)


# ------------------------------------------------------------------------------------------------ the one logo treatment
WORD = "refluff_logo.png"                 # full lockup incl. the official subtitle "CARPET CLEANING ASMR"
SMALL = "refluff_wordmark_small.png"      # "Refluff" only, the softer / bolder render made for < ~600 px
GLOW_TINT = np.array([0.03, 0.02, 0.05], np.float32)


def place_logo(bg, m, name, w, cx, cy, glow=0.66, spread=0.38, foam_boost=0.7, edge_fade=0.7, spare_brush=0.0,
               rug_boost=0.0, tag=""):
    """Composite the logo (width w of the canvas, centred at (cx, cy) canvas fractions) over float RGB bg.
    Behind it one soft dark glow from its dilated + blurred alpha (spread = radius / logo height); where pink foam
    sits behind the lockup the glow deepens (x (1 + foam_boost * foam)) so the logo's own pink bubbles keep their
    separation. The logo pixels are only scaled (Lanczos, premultiplied) - never redrawn.
    Returns (image, logo box px, logo alpha canvas)."""
    H, W = bg.shape[:2]
    lg = load_logo(name)
    lh, lw = lg.shape[:2]
    tw = int(round(w * W))
    th = int(round(lh * tw / lw))
    p = resize_prem(lg, (tw, th))
    x0 = int(round(cx * W - tw / 2.0))
    y0 = int(round(cy * H - th / 2.0))
    canvas = np.zeros((H, W, 4), np.float32)
    sx0, sy0 = max(0, -x0), max(0, -y0)
    dx0, dy0, dx1, dy1 = max(0, x0), max(0, y0), min(W, x0 + tw), min(H, y0 + th)
    canvas[dy0:dy1, dx0:dx1] = p[sy0:sy0 + dy1 - dy0, sx0:sx0 + dx1 - dx0]
    al = canvas[..., 3]
    gl = ndi.gaussian_filter(ndi.maximum_filter(al, size=max(3, int(0.08 * th) | 1)), spread * th)
    gl = np.clip(gl * 1.6, 0.0, 1.0) ** 0.9
    rug = (1.0 - m["soil"]) * (1.0 - m["foam"]) * (1.0 - m["brush"])      # vivid rug (red, like the logo plate)
    k = np.clip(glow * gl * (1.0 + foam_boost * m["foam"] + rug_boost * rug), 0.0, 0.93)
    k = k * (1.0 - edge_fade * edge_band(H, W, 0.045))            # the frame edge stays art, not a dark halo
    k = k * (1.0 - spare_brush * m["brush"])
    out = bg + (GLOW_TINT - bg) * k[..., None]
    out = out * (1.0 - canvas[..., 3:4]) + canvas[..., :3]
    return np.clip(out, 0.0, 1.0), (x0, y0, x0 + tw, y0 + th), al


# ------------------------------------------------------------------------------------------------ measurements
def srgb_lin(c):
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def rel_lum(a):
    li = srgb_lin(a)
    return li[..., 0] * 0.2126 + li[..., 1] * 0.7152 + li[..., 2] * 0.0722


def edge_contrast(a, frac=0.04):
    """Contrast ratio (WCAG) of the mean edge strip luminance against Steam's #1b2838, per side L T R B."""
    H, W = a.shape[:2]
    e = max(2, int(round(frac * min(H, W))))
    Lui = float(rel_lum(STEAM_UI[None, None])[0, 0])
    sides = (a[:, :e], a[:e, :], a[:, W - e:], a[H - e:, :])
    return [(float(rel_lum(s).mean()) + 0.05) / (Lui + 0.05) for s in sides]


def grime_hsv(a, m, al):
    """Mean hue / HSV sat / value of the final pixels on the flat grime, away from the logo and its glow."""
    away = ndi.maximum_filter(al, size=41) < 0.02
    sel = (m["core"] > 0.85) & away
    if sel.sum() < 200:
        return None
    px = a[sel]
    mx, mn = px.max(1), px.min(1)
    r, g, b = px[:, 0], px[:, 1], px[:, 2]
    d = mx - mn + 1e-6
    h = np.where(mx == r, ((g - b) / d) % 6, np.where(mx == g, (b - r) / d + 2, (r - g) / d + 4)) * 60.0
    hm = np.degrees(np.arctan2(np.sin(np.radians(h)).mean(), np.cos(np.radians(h)).mean())) % 360
    return hm, float((d / (mx + 1e-6)).mean()), float(mx.mean())


def logo_report(key, a, m, box, al):
    H, W = a.shape[:2]
    x0, y0, x1, y1 = box
    mg = (x0 / W, y0 / H, 1 - x1 / W, 1 - y1 / H)
    solid = al > 0.5
    bm = m["brush"] > 0.5
    gap = float(ndi.distance_transform_edt(~bm)[solid].min()) if bm.any() and solid.any() else float("inf")
    rx = x0 + int(0.85 * (x1 - x0))
    ring = ndi.maximum_filter(solid, size=9)
    ring[:, :rx] = False
    foam_r = float(m["foam"][ring].mean()) if ring.any() else 0.0
    flag = "" if min(mg) >= MARGIN - 1e-6 else "  <-- inside the outer 4 %"
    return (f"logo {x1 - x0}x{y1 - y0} ({(x1 - x0) / W:.0%} W)  margins L{mg[0]:.3f} T{mg[1]:.3f} R{mg[2]:.3f} "
            f"B{mg[3]:.3f}  gap to brush {gap:.0f}px  foam behind right 15% {foam_r:.2f}{flag}")


# ------------------------------------------------------------------------------------------------ layouts
# crop = (left, top, width) as plate fractions (height from the asset aspect); grade = grade() overrides;
# logo = place_logo() keys. Plates: SteamStore/_work/keyart/plates.json (round-2 / v2 engine renders, no text).
HERO1 = "hero1_foam_bank_v2"      # front-right 3/4: grime left, brush centre, glossy foam bank behind, vivid right
HERO2 = "hero2_split_brush_v2"    # low view along the seam: grime left, brush centred on the seam, vivid right
WIDE5 = "wide5_macro_brush_v2"    # macro: big brush on the seam, foam mound behind, vivid pile bottom-right
TALLD = "tallD_macro_brush_v2"    # portrait macro: big brush, foam mound right, grime above, vivid below
BGP = "background_soft_pile_v2"   # evenly defocused clean pile

LAYOUT = {
    "header_capsule_920x430": dict(size=(920, 430), plate=HERO1, crop=(0.049, 0.066, 0.645),
                                   grade=dict(vc=(0.62, 0.55)),
                                   logo=dict(name=WORD, w=0.50, cx=0.29, cy=0.31)),
    "main_capsule_1232x706": dict(size=(1232, 706), plate=WIDE5, crop=(0.0, 0.04, 0.84),
                                  grade=dict(vc=(0.55, 0.60)),
                                  logo=dict(name=WORD, w=0.44, cx=0.265, cy=0.18)),
    "vertical_capsule_748x896": dict(size=(748, 896), plate=TALLD, crop=(0.0, 0.0, 1.0),
                                     grade=dict(vc=(0.50, 0.60)), lift=((0.18, 0.92, 0.36, 0.20, 0.07),),
                                     logo=dict(name=WORD, w=0.79, cx=0.475, cy=0.15)),
    "library_capsule_600x900": dict(size=(600, 900), plate=TALLD, crop=(0.0, 0.0, 1.0),
                                    grade=dict(vc=(0.50, 0.58)), lift=((0.07, 0.98, 0.26, 0.13, -0.22),),
                                    logo=dict(name=WORD, w=0.87, cx=0.49, cy=0.135, foam_boost=1.1)),
    "event_cover_800x450": dict(size=(800, 450), plate=HERO1, crop=(0.124, 0.04, 0.55),
                                grade=dict(vc=(0.62, 0.55)),
                                logo=dict(name=WORD, w=0.47, cx=0.275, cy=0.30)),
    "event_header_1920x622": dict(size=(1920, 622), plate=HERO1, crop=(0.0, 0.0, 0.9967),
                                  grade=dict(vc=(0.52, 0.55)),
                                  logo=dict(name=WORD, w=0.31, cx=0.245, cy=0.40)),
}


def build_capsule(key, report):
    spec = LAYOUT[key]
    a, m = render(spec["plate"], *spec["crop"], spec["size"])
    a = grade(a, m, **spec["grade"])
    if spec.get("lift"):
        a = soft_lift(a, spec["lift"])
    lp = dict(spec["logo"])
    a2, box, al = place_logo(a, m, lp.pop("name"), tag=key, **lp)
    report.append(f"{key:26s} {logo_report(key, a2, m, box, al)}")
    report.append(f"{'':26s} {stats_line(a2, m, al)}")
    return a2


def stats_line(a, m, al):
    gh = grime_hsv(a, m, al)
    ec = edge_contrast(a)
    g = f"grime hue {gh[0]:.0f} sat {gh[1]:.2f} val {gh[2]:.2f}" if gh else "grime n/a"
    return g + "  edge contrast vs #1b2838 L{:.2f} T{:.2f} R{:.2f} B{:.2f}".format(*ec)


def soft_lift(a, spots):
    """Soft elliptical exposure changes: spots = ((cx, cy, rx, ry, amount), ...) in canvas fractions."""
    H, W = a.shape[:2]
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    f = np.ones((H, W), np.float32)
    for cx, cy, rx, ry, amt in spots:
        d = np.sqrt(((xx + 0.5) / W - cx) ** 2 / rx ** 2 + ((yy + 0.5) / H - cy) ** 2 / ry ** 2)
        f = f * (1.0 + amt * (1.0 - smoothstep(0.0, 1.0, d)))
    return np.clip(a * f[..., None], 0, 1)


# ------------------------------------------------------------------------------------------------ small capsule
SMALL_SPEC = dict(size=(462, 174), plate=WIDE5, crop=(0.0, 0.099, 1.0), med=5, sigma=2.4,
                  logo=dict(name=SMALL, w=0.91, cx=0.5, cy=0.37, glow=0.62, spread=0.13, foam_boost=0.2,
                            rug_boost=0.5, spare_brush=0.75))


def build_small(report):
    """Director's small capsule, regraded: the wordmark_small nearly fills it (91 % wide, ~4.5 % side margins);
    behind it the macro seam (wide5) painterly-simplified (median + soft Gaussian: pile texture turns to mush at
    120x45) EXCEPT the brush, which stays sharp so its bristle streaks and a sliver of the white rim show under
    the word; grime left (family grey-olive), pink foam top right, vivid rug bottom right."""
    sp = SMALL_SPEC
    a, m = render(sp["plate"], *sp["crop"], sp["size"])
    simp = simplify(a, sp["med"], sp["sigma"])
    bm = np.clip(blur3(ndi.maximum_filter(m["brush"], size=3), 1.0) * 1.2, 0, 1)[..., None]
    a = simp * (1.0 - bm) + a * bm
    a = grade(a, m, vc=(0.5, 0.5), vig=0.16, bloom=0.10)
    lp = dict(sp["logo"])
    a, box, al = place_logo(a, m, lp.pop("name"), tag="small", **lp)
    report.append(f"{'small_capsule_462x174':26s} {logo_report('small', a, m, box, al)}")
    report.append(f"{'':26s} {stats_line(a, m, al)}")
    return a


# ------------------------------------------------------------------------------------------------ library hero
def build_hero(report):
    """Split's pick (hero2: the seam story, calm grime bottom-left), no logo, no text, the family grade.
    Art director: the brush (cap y ~411 .. bristles ~890 at 3840x1240) overflowed Steam's 860x380 centre safe area,
    so hero2 is re-finished from its raw passes with the head scaled x HERO_BRUSH_SCALE about its footprint and
    centred on (1920, 620), its contact / cast shadow following it (refinish_hero2); the cap needs no retouch there
    (the silhouette is hole-free). Judges' fixes: the sharp cream / red chevrons of the far-right ~20 % lose ~10 %
    contrast and colour; the bottom-left (~35 % W x 40 % H, under Steam's library logo) is darkened ~12 %."""
    size = (3840, 1240)
    hb = refinish_hero2(HERO_BRUSH_SCALE)
    sx0, sy0, sx1, sy1 = HERO_SAFE
    inside = hb[0] >= sx0 and hb[1] >= sy0 and hb[2] <= sx1 and hb[3] <= sy1
    report.append(f"{'library_hero_3840x1240':26s} brush x {hb[0]:.0f}-{hb[2]:.0f} y {hb[1]:.0f}-{hb[3]:.0f} "
                  f"({hb[2] - hb[0]:.0f}x{hb[3] - hb[1]:.0f}) vs safe area x {sx0}-{sx1} y {sy0}-{sy1}: "
                  + ("INSIDE" if inside else "OUTSIDE  <--"))
    a, m = render(HERO2S, 0.0, 0.0, 1.0, size)
    a = grade(a, m, vc=(0.50, 0.53), vig=0.18)
    H, W = a.shape[:2]
    xx = (np.arange(W, dtype=np.float32) + 0.5) / W
    yy = (np.arange(H, dtype=np.float32) + 0.5) / H
    rw = smoothstep(0.76, 0.92, xx)[None, :, None] * np.ones((H, 1, 1), np.float32)
    L = luma(a)[..., None]
    mu = float(luma(a[:, int(0.8 * W):]).mean())
    calm = mu + (L + (a - L) * 0.90 - mu) * 0.90                   # -10 % colour, -10 % contrast
    a = a + (calm - a) * rw
    bl = (1.0 - smoothstep(0.18, 0.42, xx))[None, :] * smoothstep(0.52, 0.80, yy)[:, None]
    a = a * (1.0 - 0.12 * bl)[..., None]
    report.append(f"{'library_hero_3840x1240':26s} no logo / text   {stats_line(np.clip(a, 0, 1), m, np.zeros((H, W), np.float32))}")
    return np.clip(a, 0, 1)


# ------------------------------------------------------------------------------------------------ page background
def build_page_background(report):
    """Macro's pick (the darkest, calmest): the defocused clean pile, flattened (contrast x0.76, exposure x0.89),
    cream highlights soft-kneed (p99 luma ~97 -> <= 80 of 255, so they do not glow through Steam's fade), the
    violet foam hint top-left kept but 20 % quieter, vignette 0.40; dithered on export (no contour rings)."""
    a, _ = render(BGP, 0.0, 0.0, 1.0, (1438, 810), masks=False)
    H, W = a.shape[:2]
    h, s, v = hsv(a)
    vio = hue_near(h, 285.0, 40.0) * smoothstep(0.08, 0.20, s)
    L = luma(a)[..., None]
    a = a + ((L + (a - L) * 0.8) - a) * vio[..., None]
    L = luma(a)
    mu = float(L.mean())
    a = mu + (a - mu) * 0.76
    a = np.clip(a * 0.89, 0, 1)
    L = luma(a)
    k, r = 0.22, 0.12                                              # soft knee: L -> k + (L-k) / (1 + (L-k)/r)
    Lc = np.where(L > k, k + (L - k) / (1.0 + (L - k) / r), L)
    a = a * (Lc / np.maximum(L, 1e-4))[..., None]
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    rr = np.sqrt(((xx / W - 0.5) / 0.5) ** 2 + ((yy / H - 0.5) / 0.5) ** 2) / np.sqrt(2.0)
    a = a * (1.0 - 0.40 * smoothstep(0.30, 1.05, rr) ** 1.15)[..., None] * np.ones(3, np.float32)
    a = np.clip(a, 0, 1)
    L8 = luma(a) * 255.0
    report.append(f"{'page_background_1438x810':26s} no text   mean luma {L8.mean():.0f}  p99 {np.percentile(L8, 99):.0f}")
    return a


# ------------------------------------------------------------------------------------------------ outputs
ART = {   # asset key -> output path under SteamStore/
    "header_capsule_920x430": "store/header_capsule_920x430.png",
    "small_capsule_462x174": "store/small_capsule_462x174.png",
    "main_capsule_1232x706": "store/main_capsule_1232x706.png",
    "vertical_capsule_748x896": "store/vertical_capsule_748x896.png",
    "page_background_1438x810": "store/page_background_1438x810.png",
    "library_capsule_600x900": "library/library_capsule_600x900.png",
    "library_header_920x430": "library/library_header_920x430.png",
    "library_hero_3840x1240": "library/library_hero_3840x1240.png",
    "event_cover_800x450": "events/event_cover_800x450.png",
    "event_header_1920x622": "events/event_header_1920x622.png",
}


def build_art(report):
    imgs = {}
    for key in ("header_capsule_920x430", "main_capsule_1232x706", "vertical_capsule_748x896",
                "library_capsule_600x900", "event_cover_800x450", "event_header_1920x622"):
        imgs[key] = to_img(build_capsule(key, report), key)
    imgs["library_header_920x430"] = imgs["header_capsule_920x430"]          # the same art as the store header
    report.append(f"{'library_header_920x430':26s} = header_capsule_920x430 (identical file)")
    imgs["small_capsule_462x174"] = to_img(build_small(report), "small_capsule_462x174")
    imgs["library_hero_3840x1240"] = to_img(build_hero(report), "library_hero_3840x1240")
    imgs["page_background_1438x810"] = to_img(build_page_background(report), "page_background_1438x810")
    for key, rel in ART.items():
        im = imgs[key]
        w, h = (int(v) for v in key.rsplit("_", 1)[1].split("x"))
        assert im.size == (w, h) and im.mode == "RGB", (key, im.size, im.mode)
        save_png(im, rel)
    return imgs


def build_logo_icons(report):
    """Library logo + icons: copies / resizes of the final files in SteamStore/logo (never redrawn)."""
    os.makedirs(os.path.join(OUT, "library"), exist_ok=True)
    os.makedirs(os.path.join(OUT, "icons"), exist_ok=True)
    # library logo: refluff_logo_1280x720 trimmed tightly (spec 5; Steam anchors the logo by the image edges, so
    # the 176 / 177 px of empty rows above / below would float it off the chosen anchor). Pure crop of the
    # official pixels: full width kept (the lockup already spans 3..1276, so the file still touches 1280 wide),
    # rows cut to the first / last non-transparent row (shadow included) + 3 px.
    lg = Image.open(os.path.join(LOGO, "refluff_logo_1280x720.png")).convert("RGBA")
    ys = np.nonzero(np.asarray(lg)[..., 3].max(axis=1) > 0)[0]
    lib = lg.crop((0, max(0, int(ys.min()) - 3), lg.width, min(lg.height, int(ys.max()) + 4)))
    save_image(lib, os.path.join(OUT, "library", "library_logo_1280x720.png"), optimize=True)
    copy_file(os.path.join(LOGO, "refluff_shortcut_icon.ico"), os.path.join(OUT, "icons", "refluff.ico"))
    src = Image.open(os.path.join(LOGO, "refluff_app_icon_184.jpg"))
    q0 = list(src.quantization[0])[:8] if hasattr(src, "quantization") else None
    if src.format == "JPEG" and src.size == (184, 184) and src.mode == "RGB" and q0 == [2, 1, 1, 2, 2, 4, 5, 6]:
        copy_file(src.filename, os.path.join(OUT, "icons", "app_icon_184x184.jpg"))   # already opaque q95 4:4:4
        how = "copied (already opaque JPG q95 4:4:4)"
    else:
        save_image(src.convert("RGB").resize((184, 184), Image.LANCZOS), os.path.join(OUT, "icons", "app_icon_184x184.jpg"),
                   quality=95, subsampling=0)
        how = "re-encoded JPG q95 4:4:4"
    mark = np.asarray(Image.open(os.path.join(LOGO, "refluff_mark_1024.png")).convert("RGBA"), np.float32) / 255.0
    mark[..., :3] *= mark[..., 3:4]                                    # premultiplied -> no dark fringe
    for n in (512, 256):
        r = resize_prem(mark, (n, n))
        rgb = np.where(r[..., 3:4] > 1e-4, r[..., :3] / np.maximum(r[..., 3:4], 1e-4), 0.0)
        px = np.concatenate([rgb, r[..., 3:4]], axis=2)
        save_image(Image.fromarray(np.clip(np.floor(px * 255.0 + 0.5), 0, 255).astype(np.uint8), "RGBA"),
                   os.path.join(OUT, "icons", f"shortcut_icon_{n}x{n}.png"), optimize=True)
    report.append(f"library_logo_1280x720.png = refluff_logo_1280x720 trimmed to {lib.width}x{lib.height}; refluff.ico copied; app_icon_184x184.jpg {how}; "
                  "shortcut_icon_512/256 = refluff_mark_1024 resized (premultiplied Lanczos, transparent)")


SHOTS = os.path.join(ROOT, "SteamStore", "_work", "shots")      # StoreShotApi layers: <name>_scene / _hud_ui
# Upload order = final_order.json as it stands (ACES re-shoot 2026-09-26, steam-izdat guide: 1 striking, 2 gameplay,
# 3 progression, 4 striking, then gameplay / striking alternating, no two neighbours with the same rug):
#   01 hero reveal (Anatolian, no HUD) | 02 scraper crust (European green) | 03 pay slip (progression) |
#   04 rya half clean (Finnish rya, no HUD) | 05 hose spill (Salor) | 06 moisture lamp (European green) |
#   07 hand-over flight (Anatolian) | 08 upgrades shop (progression, European green dimmed) | 09 spotless (Transylvanian).
# SHOT_MOVE (name -> slot, applied after the json order) stays available for one-off moves; empty = the json decides.
SHOT_MOVE = {}
SHOT_VARIANT = {
    # The two no-HUD beauty shots (spec 3.3: 1-2 without UI), each a crop of the supersampled 3840x2160 scene layer
    # (x0, y0, w, h in scene px, exactly 16:9): real frames of the game camera, only framed tighter, never painted.
    # (ACES re-stage 2026-09-26: both dirty parts are caked mud plates now; both windows re-checked, unchanged.)
    # hero reveal: its HUD hint read 'Wet it down' while the wet vac extracted (art director: mismatched; the game
    # hides the whole chrome incl. the hint while a tool strokes, so a mid-stroke frame shows no hint anyway). A 1:1
    # native window (1.5x): the rug fills ~75 % instead of ~37 %, the wand on the band's leading edge reads at the
    # store's 600 px viewer, the far rug edge keeps a tile margin, only dirty near pile is cut.
    "01_hero_reveal": ("nohud", (670, 250, 2560, 1440)),
    # rya: HUD off as before (same hint mismatch); clean | foam + brush | grime. The 2026-09-26 retake mopped the grey
    # floor streaks above the rug and moved the brush beside the foam, nearer the camera (scene y ~1590-1785), so the
    # old 1.35x window (640, 330, 2848, 1602), kept below the streaks, only clipped the rug's top fringe and the foam's
    # rounded top end. 1.25x: rug top edge + foam top with a tile margin (rug from y 305), brush ~120 px (2560) above
    # the bottom edge, the near rug border and foam run out of frame; same centre x as before.
    "12_rya_half_clean": ("nohud", (528, 232, 3072, 1728)),
    # pay slip: the ray glow's faint halo is quantised in the captured HUD layer (premultiplied alpha 1, 2, 3 ...
    # /255 with ~8-level colour jumps -> 4-5 hard terraces); smooth that halo in the layer before compositing
    "10_payslip": ("deband", None),
}


def deband_glow(ui):
    """Smooth the additive glow halo of a premultiplied HUD layer (float RGBA 0..1). The glow is where the colour
    exceeds alpha (cards, text and shadows never do); inside it a normalised Gaussian (sigma 28 px at 2560, about
    one terrace) over the glow AND the clear layer around it replaces the stepped values (so the outermost 0 -> 1/255
    step softens too), fully where alpha is low, fading out by alpha 0.16 so the ray cores keep their shape. Opaque
    UI and shadows are excluded from the average, so nothing bleeds."""
    a = ui[..., 3]
    glow = smoothstep(1.0 / 255, 4.0 / 255, ui[..., :3].max(-1) - a)
    clear = ((a <= 0.0) & (ui[..., :3].max(-1) <= 0.0)).astype(np.float32)     # untouched scene around the halo
    w = np.maximum(glow, clear) * (ndi.maximum_filter(a, size=7) < 0.5)
    num = np.stack([ndi.gaussian_filter(ui[..., c] * w, 28.0) for c in range(4)], axis=2)
    den = ndi.gaussian_filter(w, 28.0)[..., None]
    sm = num / np.maximum(den, 1e-6)
    near = np.clip(ndi.gaussian_filter(glow, 28.0) * 8.0, 0.0, 1.0)
    k = (w * near * (1.0 - smoothstep(0.06, 0.16, ndi.gaussian_filter(a, 6.0))))[..., None]
    k = k * np.clip(den * 4.0, 0.0, 1.0)
    return ui + (sm - ui) * k


def shot_master(name, variant, crop=None):
    """2560x1440 master (float sRGB) for the variants, composed like compose_screens.py (scene downscaled in linear
    light, the HUD laid over premultiplied in sRGB). crop = (x0, y0, w, h) window of the 3840x2160 scene layer (no-HUD
    shots only: the HUD belongs to the full game-camera frame); a 2560x1440 window is used 1:1, unresampled."""
    scene = np.asarray(Image.open(os.path.join(SHOTS, name + "_scene.png")).convert("RGB"))
    if crop is not None:
        assert variant == "nohud", (name, variant)
        x0, y0, w, h = crop
        assert w * 9 == h * 16 and x0 >= 0 and y0 >= 0 and x0 + w <= scene.shape[1] and y0 + h <= scene.shape[0], crop
        scene = scene[y0:y0 + h, x0:x0 + w]
        if (w, h) == (2560, 1440):
            return scene.astype(np.float32) / 255.0
    base = resize_linear(scene, (2560, 1440))
    if variant == "nohud":
        return base
    ui = np.asarray(Image.open(os.path.join(SHOTS, name + "_hud_ui.png")).convert("RGBA"), np.float32) / 255.0
    if variant == "deband":
        ui = deband_glow(ui)
    return np.clip(ui[..., :3] + (1.0 - ui[..., 3:4]) * base, 0.0, 1.0)


def build_screenshots(report):
    """The final upload set: NN_<short>.jpg at 2560x1440 (JPG q95, 4:4:4; quality stepped down only if a file would
    pass 5 MB) + the 1920x1080 PNGs in screenshots/1920x1080/. Order = final_order.json with the SHOT_MOVE shots
    moved to their slots; SHOT_VARIANT shots are re-composed from their capture layers (the 1920 PNG then from that
    master, in linear light as compose_screens.py does), the rest use the composed files as they are. Stale files
    are removed."""
    with open(os.path.join(SCREENS, "final_order.json"), encoding="utf-8") as fh:
        order = sorted(json.load(fh), key=lambda e: e["n"])
    moved = sorted(((SHOT_MOVE[e["name"]], e) for e in order if e["name"] in SHOT_MOVE), key=lambda t: t[0])
    order = [e for e in order if e["name"] not in SHOT_MOVE]
    for slot, e in moved:
        order.insert(slot - 1, e)
    d, d2 = os.path.join(OUT, "screenshots"), os.path.join(OUT, "screenshots", "1920x1080")
    os.makedirs(d2, exist_ok=True)
    keep, shots = set(), []
    for n, e in enumerate(order, 1):
        base = f"{n:02d}_{e['name'].split('_', 1)[1]}"
        var, crop = SHOT_VARIANT.get(e["name"], (None, None))
        if var:
            m = shot_master(e["name"], var, crop)
            im = to_img(m, base)
            small = Image.fromarray((resize_linear(np.asarray(im), (1920, 1080)) * 255.0 + 0.5).astype(np.uint8), "RGB")
            save_image(small, os.path.join(d2, base + ".png"), optimize=True)
        else:
            im = Image.open(e["file2560"]).convert("RGB")
            s1920 = Image.open(e["file"])
            assert s1920.size == (1920, 1080) and s1920.format == "PNG", (e["file"], s1920.size)
            copy_file(e["file"], os.path.join(d2, base + ".png"))
        assert im.size == (2560, 1440), (e["name"], im.size)
        path = os.path.join(d, base + ".jpg")
        for q in (95, 93, 91, 89, 87, 85):
            save_image(im, path, quality=q, subsampling=0, optimize=True)
            if os.path.getsize(path) <= 5_000_000:
                break
        keep |= {base + ".jpg", base + ".png"}
        shots.append((base, im))
        how = ""
        if var:
            how = f"   [{var}" + (f", scene crop x{crop[0]} y{crop[1]} {crop[2]}x{crop[3]} = {3840 / crop[2]:.2f}x" if crop else "") + "]"
        report.append(f"  screenshots/{base}.jpg  q{q}  {os.path.getsize(path) / 1e6:.2f} MB   (+ 1920x1080/{base}.png){how}")
    for folder in (d, d2):
        for f in sorted(os.listdir(folder)):
            if os.path.isfile(os.path.join(folder, f)) and f not in keep:
                os.remove(os.path.join(folder, f))
                report.append(f"  removed stale {os.path.relpath(os.path.join(folder, f), OUT)}")
    return shots


# ------------------------------------------------------------------------------------------------ review mocks
UI_BG, UI_TXT, UI_DIM = (27, 40, 56), (220, 222, 223), (143, 152, 160)


def _font(px, wght=400, face="Rubik[wght].ttf"):
    f = ImageFont.truetype(os.path.join(ROOT, "Tools", "steam_art", "fonts", face), px)
    try:
        f.set_variation_by_axes([wght])
    except Exception:
        pass
    return f


def _fit(im, size):
    return im.resize(size, Image.LANCZOS)


def _bars(d, x, y, widths, h=10, gap=10, fill=(58, 72, 90)):
    for i, w in enumerate(widths):
        d.rounded_rectangle((x, y + i * (h + gap), x + w, y + i * (h + gap) + h), radius=h // 2, fill=fill)


def _placeholder(size, seed):
    """Neutral stand-in capsule for other games (flat gradient, no art)."""
    w, h = size
    rng = np.random.default_rng(seed)
    c0, c1 = rng.uniform(40, 90, 3), rng.uniform(40, 90, 3)
    t = np.linspace(0, 1, h, dtype=np.float32)[:, None, None]
    a = c0 * (1 - t) + c1 * t
    return Image.fromarray(np.broadcast_to(a, (h, w, 3)).astype(np.uint8), "RGB")


def store_page_mock(imgs, shots):
    """Steam store page mock: page background under a dark blue fade, title, big screenshot + header capsule,
    screenshot strip, search-results rows (small capsule at 120x45 and 184x69) and the front-page carousel with
    the main capsule at 616x353."""
    W, H = 1600, 2060
    ui = np.array(UI_BG, np.float32) / 255.0
    canvas = np.broadcast_to(ui, (H, W, 3)).copy()
    bg = np.asarray(imgs["page_background_1438x810"], np.float32) / 255.0
    bx = (W - 1438) // 2
    yy = (np.arange(810, dtype=np.float32) + 0.5) / 810
    xx = (np.arange(1438, dtype=np.float32) + 0.5) / 1438
    t = 0.30 + 0.70 * smoothstep(0.30, 1.0, yy)[:, None]            # Steam's blue tint + fade to #1b2838
    t = np.maximum(t, 1.0 - smoothstep(0.0, 0.10, np.minimum(xx, 1 - xx))[None, :])
    canvas[:810, bx:bx + 1438] = bg * (1 - t[..., None]) + ui * t[..., None]
    im = Image.fromarray((canvas * 255 + 0.5).astype(np.uint8), "RGB")
    d = ImageDraw.Draw(im)
    d.rectangle((0, 0, W, 56), fill=(23, 26, 33))
    d.text((330, 16), "STORE      LIBRARY      COMMUNITY", font=_font(20, 500), fill=UI_DIM)
    cx0 = (W - 940) // 2
    d.text((cx0, 84), "Refluff: Carpet Cleaning ASMR", font=_font(34, 400), fill=(255, 255, 255))
    y = 140
    im.paste(_fit(shots[0][1], (600, 338)), (cx0, y))
    im.paste(_fit(imgs["header_capsule_920x430"], (324, 151)), (cx0 + 616, y))
    _bars(d, cx0 + 616, y + 166, (316, 300, 310, 220), h=8, gap=10)            # short description (placeholder)
    for i, k in enumerate(("ALL REVIEWS:", "RELEASE DATE:", "DEVELOPER:")):
        d.text((cx0 + 616, y + 262 + i * 24), k, font=_font(13, 500), fill=(85, 106, 128))
        _bars(d, cx0 + 740, y + 266 + i * 24, (90 - 14 * i,), h=7, fill=(52, 102, 140))
    y += 350
    tw = (940 - 8 * 5) // 9
    for i, (base, s) in enumerate(shots):
        th = int(round(tw * 9 / 16))
        im.paste(_fit(s, (tw, th)), (cx0 + i * (tw + 5), y))
        if i == 0:
            d.rectangle((cx0 - 2, y - 2, cx0 + tw + 1, y + th + 1), outline=(255, 255, 255), width=2)
    d.text((cx0, y + 64), "screenshots in upload order (the first 4 feed the hover tooltip)", font=_font(13), fill=UI_DIM)
    return im, d, cx0, y + 100


def store_page_mock_rest(imgs, shots, im, d, cx0, y):
    small = imgs["small_capsule_462x174"]
    d.text((cx0, y), "SEARCH RESULTS  (small capsule at 120x45)", font=_font(16, 500), fill=UI_TXT)
    y += 30
    rows = [("Refluff: Carpet Cleaning ASMR", _fit(small, (120, 45)), True)] + \
           [(f"Another Game {i}", _placeholder((120, 45), 10 + i), False) for i in (1, 2)]
    for title, cap, ours in rows:
        d.rectangle((cx0, y, cx0 + 940, y + 53), fill=(22, 32, 45) if not ours else (32, 48, 66))
        im.paste(cap, (cx0 + 4, y + 4))
        d.text((cx0 + 140, y + 8), title, font=_font(17), fill=(199, 213, 224) if ours else UI_DIM)
        _bars(d, cx0 + 140, y + 34, (60,), h=6, fill=(62, 82, 104))
        d.text((cx0 + 860, y + 17), "$—", font=_font(14), fill=UI_DIM)
        y += 57
    y += 24
    d.text((cx0, y), "WISHLIST / TOP SELLERS  (small capsule at 184x69)", font=_font(16, 500), fill=UI_TXT)
    y += 30
    for i in range(3):
        cap = _fit(small, (184, 69)) if i == 0 else _placeholder((184, 69), 20 + i)
        d.rectangle((cx0, y, cx0 + 940, y + 81), fill=(32, 48, 66) if i == 0 else (22, 32, 45))
        im.paste(cap, (cx0 + 6, y + 6))
        d.text((cx0 + 206, y + 12), "Refluff: Carpet Cleaning ASMR" if i == 0 else f"Another Game {i + 2}",
               font=_font(18), fill=(199, 213, 224) if i == 0 else UI_DIM)
        _bars(d, cx0 + 206, y + 46, (90, 70), h=7, gap=8, fill=(62, 82, 104))
        y += 87
    y += 30
    d.text((cx0, y), "FEATURED & RECOMMENDED  (main capsule at 616x353)", font=_font(16, 500), fill=UI_TXT)
    y += 30
    fx = cx0
    d.rectangle((fx - 4, y - 4, fx + 944, y + 357), fill=(15, 22, 33))
    im.paste(_fit(imgs["main_capsule_1232x706"], (616, 353)), (fx, y))
    px = fx + 616
    d.rectangle((px, y, px + 324, y + 353), fill=(33, 50, 70))
    d.text((px + 14, y + 12), "Refluff: Carpet Cleaning ASMR", font=_font(18), fill=(255, 255, 255))
    for i in range(4):
        s = shots[i][1]
        im.paste(_fit(s, (143, 80)), (px + 14 + (i % 2) * 153, y + 50 + (i // 2) * 90))
    d.text((px + 14, y + 240), "Coming soon", font=_font(16), fill=(255, 255, 255))
    _bars(d, px + 14, y + 272, (120, 180), h=8, gap=10, fill=(70, 95, 120))
    for sx in (fx - 34, fx + 952):
        d.polygon([(sx + 12, y + 150), (sx + 12, y + 200), (sx + (-4 if sx < fx else 28), y + 175)], fill=(90, 110, 130))
    for i in range(6):
        d.rounded_rectangle((cx0 + 380 + i * 32, y + 372, cx0 + 402 + i * 32, y + 380), radius=3,
                            fill=(200, 210, 220) if i == 0 else (60, 75, 95))
    return im.crop((0, 0, im.width, y + 410))


def library_mock(imgs):
    """Steam library details page mock: the hero at its 1920x620 copy with the library logo bottom-left (30 % of
    the width, Steam adds no shadow), the 860x380 safe area outlined (thin, at copy scale), a play bar, and the
    capsule grid at 300x450."""
    W = 1920
    hero = _fit(imgs["library_hero_3840x1240"], (1920, 620)).convert("RGBA")
    lg = Image.open(os.path.join(OUT, "library", "library_logo_1280x720.png")).convert("RGBA")
    lg = lg.crop(lg.getchannel("A").point(lambda v: 255 if v > 2 else 0).getbbox())
    lw = int(0.30 * W)
    lg = lg.resize((lw, int(round(lg.height * lw / lg.width))), Image.LANCZOS)
    hero.alpha_composite(lg, (int(0.035 * W), 620 - int(0.08 * 620) - lg.height))
    hero = hero.convert("RGB")
    d0 = ImageDraw.Draw(hero)
    sx, sy = (W - 430) // 2, (620 - 190) // 2
    for i in range(sx, sx + 430, 16):
        d0.line((i, sy, min(i + 8, sx + 430), sy), fill=(255, 214, 64), width=2)
        d0.line((i, sy + 190, min(i + 8, sx + 430), sy + 190), fill=(255, 214, 64), width=2)
    for j in range(sy, sy + 190, 16):
        d0.line((sx, j, sx, min(j + 8, sy + 190)), fill=(255, 214, 64), width=2)
        d0.line((sx + 430, j, sx + 430, min(j + 8, sy + 190)), fill=(255, 214, 64), width=2)
    H = 620 + 90 + 70 + 450 + 60
    im = Image.new("RGB", (W, H), (27, 30, 36))
    im.paste(hero, (0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle((0, 620, W, 710), fill=(35, 40, 49))
    d.rounded_rectangle((40, 640, 220, 690), radius=4, fill=(90, 184, 72))
    d.text((92, 650), "PLAY", font=_font(24, 600), fill=(255, 255, 255))
    _bars(d, 260, 650, (120, 90), h=9, gap=12, fill=(70, 78, 92))
    d.text((1380, 655), "dashed box = 860x380 safe area (at the 1920x620 copy)", font=_font(16), fill=(255, 214, 64))
    d.text((40, 730), "ALL GAMES  (library capsule at 300x450)", font=_font(18, 500), fill=(190, 196, 204))
    x = 40
    for i in range(5):
        cap = _fit(imgs["library_capsule_600x900"], (300, 450)) if i == 1 else _placeholder((300, 450), 40 + i)
        im.paste(cap, (x, 770))
        x += 300 + 24
    return im


def icons_mock():
    """App icon (184 JPG) at 184 / 64 / 32 and the 256 shortcut icon, on Steam-dark and light grounds."""
    app = Image.open(os.path.join(OUT, "icons", "app_icon_184x184.jpg")).convert("RGB")
    sc = Image.open(os.path.join(OUT, "icons", "shortcut_icon_256x256.png")).convert("RGBA")
    im = Image.new("RGB", (1180, 640), UI_BG)
    d = ImageDraw.Draw(im)
    d.rectangle((0, 320, 1180, 640), fill=(236, 238, 241))
    for row, (y0, col) in enumerate(((0, UI_TXT), (320, (40, 44, 52)))):
        x = 40
        for n in (184, 64, 32):
            im.paste(_fit(app, (n, n)), (x, y0 + 60 + (184 - n)))
            d.text((x, y0 + 256), f"app icon {n}", font=_font(16), fill=col)
            x += n + 60
        sub = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
        sub.alpha_composite(sc)
        im.paste(sub, (x + 40, y0 + 30), sub)
        d.text((x + 40, y0 + 290), "shortcut icon 256 (transparent)", font=_font(16), fill=col)
        sm = sc.resize((48, 48), Image.LANCZOS)
        im.paste(sm, (x + 360, y0 + 150), sm)
        d.text((x + 330, y0 + 210), "shortcut @48", font=_font(16), fill=col)
    return im


# ------------------------------------------------------------------------------------------------ main
def main():
    report = []
    print("building store / library / event art ...")
    imgs = build_art(report)
    build_logo_icons(report)
    print("writing screenshots ...")
    report.append("screenshots (final upload order):")
    shots = build_screenshots(report)
    print("rendering review mocks ...")
    im, d, cx0, y = store_page_mock(imgs, shots)
    prev = os.path.join(OUT, "_preview")
    os.makedirs(prev, exist_ok=True)
    save_image(store_page_mock_rest(imgs, shots, im, d, cx0, y), os.path.join(prev, "store_page_mock.png"), optimize=True)
    save_image(library_mock(imgs), os.path.join(prev, "library_mock.png"), optimize=True)
    save_image(icons_mock(), os.path.join(prev, "icons_mock.png"), optimize=True)
    print("\n".join(report))
    print(f"done -> {OUT}  (+ _preview/store_page_mock.png, library_mock.png, icons_mock.png)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
