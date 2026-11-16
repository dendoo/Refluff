"""Final logo for "Refluff: Carpet Cleaning ASMR" (Steam store / library / community art).

"Rug-Cut Refluff": the word is a little rug cut in the shape of the letters. The letters are cream wool pile
(procedural clumped strands, drawn supersampled), inlaid in a madder-red rug field with an indigo serged
binding, a dotted cream guard line and a visible rug thickness. A few pink-violet foam bubbles sit at the foot
of the last f and a cream sparkle glints on its hook (the cleaning cue). Under the word hangs a woven teal runner
with "CARPET CLEANING ASMR", a dotted madder guard, lozenge medallions and knotted fringe on its square ends.

    python Tools/steam_art/make_logo.py [--quick]

Deterministic (fixed seeds). Needs Pillow, numpy, scipy, scikit-image. Fonts: Tools/steam_art/fonts (Fredoka, OFL).
Writes SteamStore/logo/:
    refluff_logo.png            full lockup, transparent, ~3000 px wide
    refluff_wordmark.png        "Refluff" only, transparent, ~3000 px wide
    refluff_wordmark_small.png  dedicated small-size render (bigger, softer tufts, thicker edge) for capsules < 600 px
    refluff_logo_1280x720.png   Steam library logo (transparent, lockup touches the canvas width)
    refluff_mark_1024.png       square icon mark: fluffy R on a rounded, extruded rug tile, transparent corners
    refluff_mark_64.png         the tile trimmed to fill the frame, tight shadow, sparkle kept
    refluff_mark_48.png / _32   small-size tile (48 px and below): no sparkle, softer locks, fills ~95 % of the frame
    refluff_app_icon_184.jpg    Steam App (community) icon: the tile edge to edge, opaque, sparkle in the free corner
    refluff_shortcut_icon.ico   shortcut icon 256..16 px, RGBA with transparent rounded corners: 256 = the mark,
                                64 = the tight tile (sparkle), 48..16 = the small tile (no sparkle, no drop shadow)
and the legibility previews into SteamStore/_work/logo/final/.
--quick renders at 40 % scale into _work/logo/final/quick/ for iteration.

Letters: each glyph is rasterised into ONE flattened mask (Fredoka's overlapping stem / crossbar contours merged,
seam-gated) before any height field, distance field or fur is built, so an f is one continuous pile surface.
Runner guard dots are placed per side with one whole dot centred on every corner (no clipped corner slivers).
The rug and the runner share one die-cut silhouette: a navy backing fills the pockets between the rug's wavy
underside and the runner top (pocket_report() gates it). Fringe strands are single tapered spline strokes.
The icon sparkle is placed from the stitch geometry (spark_corner): an even gap to the stitched guard line.
"""
import os
import sys
import time

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont
from scipy import ndimage as ndi
from skimage import measure

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
FONTS = os.path.join(HERE, "fonts")
QUICK = "--quick" in sys.argv
OUT = os.path.join(ROOT, "SteamStore", "logo")
PREV = os.path.join(ROOT, "SteamStore", "_work", "logo", "final")
if QUICK:
    OUT = PREV = os.path.join(PREV, "quick")
PROBE = os.path.join(ROOT, "SteamStore", "_work", "probe_1080.png")
RUG_ART = os.path.join(ROOT, "Assets", "Screenshots", "backing_final1920_full.png")

SCALE = 0.4 if QUICK else 1.0
SS = 3                      # supersampling of every render
WORD = "Refluff"
SUB = "CARPET CLEANING ASMR"
FONT_FILE = "Fredoka[wdth,wght].ttf"
FONT_AXES = {"Weight": 700, "Width": 112}
SUB_AXES = {"Weight": 700, "Width": 106}


# ---- palette (sRGB) -------------------------------------------------------
def hx(s):
    s = s.lstrip("#")
    return np.array([int(s[i:i + 2], 16) for i in (0, 2, 4)], np.float32) / 255.0


PAL = {
    "wool_tip":  "#fff8ea",   # sunlit tuft tips (ivory)
    "wool":      "#f4e3c1",   # wheat-ivory wool body
    "wool_root": "#d2b088",   # shaded root of an edge tuft
    "wool_deep": "#b08c66",   # gaps between edge tufts
    "madder_hi": "#c23a2a",   # madder-red rug field (lit)
    "madder":    "#a8291f",   # madder-red rug field
    "madder_lo": "#6e1914",   # red pile shadow
    "indigo_hi": "#3d4777",   # serged binding (lit)
    "indigo":    "#222849",   # serged binding / keyline
    "indigo_lo": "#11152c",   # rug thickness / deep shadow
    "cream":     "#f6e7c6",   # guard dots, runner line, fringe
    "cream_lo":  "#d8bf93",
    "teal_hi":   "#169488",   # runner field (HUD teal family)
    "teal":      "#0f6f69",
    "teal_lo":   "#0a4d4b",
    "gold":      "#f0a832",   # HUD amber: medallion core
    "foam_hi":   "#fff3fc",   # soap bubbles: white-pink body
    "foam_mid":  "#f1b6ea",
    "foam_rim":  "#d77ee0",   # pink-violet rim (#c65bd8 family)
    "foam_line": "#8e45ad",   # thin violet keyline
    "suds_top":  "#fbeefb",
    "suds_bot":  "#e6bdef",
    "spark":     "#fffbea",
    "shadow":    "#05070d",   # cool near-black drop shadow
}
C = {k: hx(v) for k, v in PAL.items()}


# ---- helpers --------------------------------------------------------------
def font(size, axes=None, file=FONT_FILE):
    ft = ImageFont.truetype(os.path.join(FONTS, file), int(round(size)))
    if axes:
        vals = []
        for a in ft.get_variation_axes():
            nm = a["name"].decode() if isinstance(a["name"], bytes) else a["name"]
            vals.append(axes.get(nm, a["default"]))
        ft.set_variation_by_axes(vals)
    return ft


def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def mix(a, b, t):
    t = np.asarray(t, np.float32)
    return a + (b - a) * (t[..., None] if t.ndim else t)


def signed_distance(mask):
    """+ inside, - outside, in pixels (mask: bool)."""
    din = ndi.distance_transform_edt(mask)
    dout = ndi.distance_transform_edt(~mask)
    return (din - dout + np.where(mask, -0.5, 0.5)).astype(np.float32)


def blur(a, sigma):
    return ndi.gaussian_filter(a, sigma, mode="constant")


def over(dst, src):
    """Premultiplied RGBA 'over' in place: dst, src float32 (H,W,4)."""
    dst *= (1.0 - src[..., 3:4])
    dst += src


def layer(color, alpha):
    """Premultiplied layer from a colour (3,) or (H,W,3) and alpha (H,W)."""
    a = np.clip(alpha, 0.0, 1.0)[..., None].astype(np.float32)
    c = np.broadcast_to(np.asarray(color, np.float32), a.shape[:2] + (3,))
    return np.concatenate([c * a, a], axis=2)


def resize_prem(prem, size, method=Image.LANCZOS):
    """Resize premultiplied float RGBA (per channel), keep colour <= alpha."""
    ch = [np.asarray(Image.fromarray(np.ascontiguousarray(prem[..., i]), "F").resize(size, method))
          for i in range(4)]
    out = np.stack(ch, axis=2)
    if method != Image.BOX:                   # Lanczos rings: zero every pixel with no true area coverage
        cov = np.asarray(Image.fromarray(np.ascontiguousarray(prem[..., 3]), "F").resize(size, Image.BOX))
        out[cov < 0.5 / 255.0] = 0.0
    out[..., 3] = np.clip(out[..., 3], 0.0, 1.0)
    out[..., :3] = np.clip(out[..., :3], 0.0, out[..., 3:4])
    return out


def reduce_ss(prem, factor=SS):
    """Exact area average of the supersampled render (no ringing at alpha edges)."""
    h, w = prem.shape[:2]
    h2, w2 = h // factor, w // factor
    p = prem[:h2 * factor, :w2 * factor]
    return p.reshape(h2, factor, w2, factor, 4).mean(axis=(1, 3)).astype(np.float32)


def fit(prem, width=None, height=None):
    h, w = prem.shape[:2]
    if width is None:
        width = w * height / h
    return resize_prem(prem, (int(round(width)), max(1, int(round(h * width / w)))))


def to_image(prem):
    """Premultiplied float RGBA -> straight-alpha 8-bit PIL image (no colour under alpha 0)."""
    a = prem[..., 3:4]
    rgb = np.where(a >= 0.5 / 255.0, prem[..., :3] / np.maximum(a, 1e-4), 0.0)   # alpha 0 after rounding: black
    out = np.concatenate([np.clip(rgb, 0, 1), np.clip(a, 0, 1)], axis=2)
    return Image.fromarray((out * 255.0 + 0.5).astype(np.uint8), "RGBA")


def from_image(im):
    a = np.asarray(im.convert("RGBA"), np.float32) / 255.0
    a[..., :3] *= a[..., 3:4]
    return a


def crop_pad(prem, pad_px, thresh=1.0 / 255.0, ret_off=False):
    """Tight crop to alpha > thresh plus pad_px; ret_off also returns the (dy, dx) that maps input -> output."""
    ys, xs = np.nonzero(prem[..., 3] > thresh)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    pad = int(round(pad_px))
    out = np.zeros((y1 - y0 + 2 * pad, x1 - x0 + 2 * pad, 4), np.float32)
    out[pad:pad + y1 - y0, pad:pad + x1 - x0] = prem[y0:y1, x0:x1]
    return (out, (pad - y0, pad - x0)) if ret_off else out


def _hash(ix, iy, seed):
    h = (ix.astype(np.int64) * 374761393 + iy.astype(np.int64) * 668265263 + seed * 1442695041) & 0xFFFFFFFF
    h = ((h ^ (h >> 13)) * 1274126177) & 0xFFFFFFFF
    h = h ^ (h >> 16)
    return (h & 0xFFFFFF).astype(np.float32) / float(0xFFFFFF)


def valnoise(x, y, seed=0):
    """Smooth value noise, x/y in lattice units, 0..1."""
    x0, y0 = np.floor(x), np.floor(y)
    fx, fy = (x - x0).astype(np.float32), (y - y0).astype(np.float32)
    fx, fy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
    ix, iy = x0.astype(np.int64), y0.astype(np.int64)
    a, b = _hash(ix, iy, seed), _hash(ix + 1, iy, seed)
    c, d = _hash(ix, iy + 1, seed), _hash(ix + 1, iy + 1, seed)
    return (a + (b - a) * fx) * (1 - fy) + (c + (d - c) * fx) * fy


def fbm(x, y, seed=0, octaves=3):
    s, amp, tot = 0.0, 0.5, 0.0
    for o in range(octaves):
        s = s + amp * valnoise(x * (2 ** o), y * (2 ** o), seed + 17 * o)
        tot += amp
        amp *= 0.5
    return s / tot


def knot_texture(X, Y, kw, kh, amp=0.12, jit=0.05, seed=5):
    """Hand-knotted pile: brick grid of soft knot bumps + per-knot jitter, ~1 on average."""
    row = np.floor(Y / kh)
    sx = X / kw + (row % 2) * 0.5
    col = np.floor(sx)
    b = np.sin(np.pi * (sx - col)) * np.sin(np.pi * (Y / kh - row))
    j = _hash(col.astype(np.int64), row.astype(np.int64), seed) - 0.5
    return (1.0 - amp * 0.7) + amp * np.power(np.clip(b, 0, 1), 0.8) + jit * j


def smooth_noise(shape, scale, rng):
    """Low-frequency noise ~N(0,1), 'scale' = feature size in px."""
    lo = (max(2, int(shape[0] / scale) + 2), max(2, int(shape[1] / scale) + 2))
    n = ndi.gaussian_filter(rng.standard_normal(lo).astype(np.float32), 1.0)
    n /= n.std() + 1e-6
    z = ndi.zoom(n, (shape[0] / lo[0], shape[1] / lo[1]), order=3)
    return z[:shape[0], :shape[1]]


LIGHT = np.array([-0.35, -0.72, 0.80], np.float32)
LIGHT /= np.linalg.norm(LIGHT)


def pillow_shade(sd, radius, sigma):
    """Soft top-left light on a rounded 'pillow' raised from a signed-distance shape."""
    t = np.clip(sd / radius, 0.0, 1.0)
    h = radius * np.sqrt(1.0 - (1.0 - t) ** 2)
    h = ndi.gaussian_filter(h, sigma)
    gy, gx = np.gradient(h)
    n = np.stack([-gx, -gy, np.ones_like(h)], axis=2)
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    wrap = np.clip((n @ LIGHT + 0.35) / 1.35, 0.0, 1.0)
    return h.astype(np.float32), wrap.astype(np.float32)


# ---- wordmark geometry ----------------------------------------------------
# per-letter bounce (dy in em, rotation in degrees): a plush rug, not a sign
LETTER_FX = [(0.00, -3.0), (0.035, 2.5), (-0.015, -1.5), (0.005, 1.5),
             (0.035, -2.0), (-0.01, 2.0), (0.015, -1.5)]
TRACK = 0.045               # extra letter spacing (em)
PAIR = {("f", "f"): 0.035}  # opens the ff pair so the crossbars never fuse at 231 px
BOLD = 0.004                # emboldening before the fur (em)
MARGIN = 0.34               # canvas margin around the letters (em): > ro + close, or the rug clips
# foam at the foot of the last f: (u right of its pen, v up from its baseline, r) in em
FOAM = [(0.445, 0.100, 0.088), (0.572, 0.082, 0.064), (0.515, 0.205, 0.058),
        (0.358, 0.058, 0.050), (0.640, 0.170, 0.032), (0.395, 0.205, 0.028)]
SPARK = (0.550, 0.720, 0.20)  # sparkle glinting on the last f's hook terminal: u, v, span (em)


def glyph_mask(ch, ft, gw, gh, size):
    """One flattened coverage mask per glyph (glyph image coords, 0..1).

    Fredoka's variable outlines overlap (an f is a stem contour plus a crossbar contour). FreeType fills every
    contour of the glyph into the same raster with the non-zero rule; this mask is then thresholded and closed
    into ONE solid shape and only its edge is re-antialiased, so the height field, the distance field and the
    fur all see a single continuous surface: no seam, groove or second rim where a crossbar crosses its stem.
    """
    g = Image.new("L", (gw, gh), 0)
    ImageDraw.Draw(g).text((size / 2, size / 2), ch, font=ft, fill=255)
    a = np.asarray(g)
    m0 = a > 127
    # seam gate: an overlap artefact would leave partly covered pixels deep inside the filled letter
    assert not ((a < 250) & ndi.binary_erosion(m0, iterations=2)).any(), f"glyph {ch!r}: seam inside the letter"
    m = ndi.binary_closing(m0, np.ones((3, 3), bool), iterations=1) | m0   # heal any sliver between contours
    sd = signed_distance(m)
    return np.clip(sd + 0.5, 0.0, 1.0)                                   # 1 px anti-aliased edge of the union


def word_layout(size):
    """Coverage of the laid-out word (working res, one merged mask per glyph) and per-letter transforms."""
    ft = font(size, FONT_AXES)
    asc, desc = ft.getmetrics()
    m = int(MARGIN * size)
    advs = [ft.getlength(c) for c in WORD]
    kern = [PAIR.get((a, b), 0.0) for a, b in zip(WORD, WORD[1:])] + [0.0]
    total = sum(advs) + TRACK * size * (len(WORD) - 1) + sum(kern) * size
    W, H = int(total + 2 * m), int(asc + desc + 2 * m)
    cov = np.zeros((H, W), np.float32)
    letters = []
    x = float(m)
    for i, (ch, (dy, rot)) in enumerate(zip(WORD, LETTER_FX)):
        gw, gh = int(advs[i] + size), int(asc + desc + size)
        ga = glyph_mask(ch, ft, gw, gh, size)
        cy, cx = ndi.center_of_mass(ga)
        ga = np.asarray(Image.fromarray(ga, "F").rotate(rot, resample=Image.BICUBIC, center=(cx, cy)), np.float32)
        ox, oy = int(round(x - size / 2)), int(round(m - size / 2 + dy * size))
        y0, x0, y1, x1 = max(oy, 0), max(ox, 0), min(oy + gh, H), min(ox + gw, W)
        sl = (slice(y0, y1), slice(x0, x1))
        gsl = (slice(y0 - oy, y1 - oy), slice(x0 - ox, x1 - ox))
        cov[sl] = np.maximum(cov[sl], np.clip(ga[gsl], 0, 1))
        th = np.radians(rot)

        def to_canvas(u, v, cx=cx, cy=cy, ox=ox, oy=oy, th=th):
            px, py = size / 2 + u * size, size / 2 + asc - v * size
            rx = cx + (px - cx) * np.cos(th) + (py - cy) * np.sin(th)
            ry = cy - (px - cx) * np.sin(th) + (py - cy) * np.cos(th)
            return rx + ox, ry + oy
        letters.append(dict(ch=ch, to_canvas=to_canvas))
        x += advs[i] + TRACK * size + kern[i] * size
    return cov, letters, m + asc


def size_for_width(target_w):
    ref = 400
    ft = font(ref, FONT_AXES)
    kern = sum(PAIR.get((a, b), 0.0) for a, b in zip(WORD, WORD[1:]))
    total = sum(ft.getlength(c) for c in WORD) + (TRACK * (len(WORD) - 1) + kern) * ref
    return ref * target_w / total


def disc_field(shape, discs, reach):
    """max over discs of (r - distance) in px: + inside (exact within 'reach' px of each disc)."""
    H, W = shape
    out = np.full(shape, -1e4, np.float32)
    for cx, cy, r in discs:
        x0, x1 = max(0, int(cx - r - reach)), min(W, int(cx + r + reach))
        y0, y1 = max(0, int(cy - r - reach)), min(H, int(cy + r + reach))
        yy, xx = np.mgrid[y0:y1, x0:x1].astype(np.float32)
        out[y0:y1, x0:x1] = np.maximum(out[y0:y1, x0:x1], r - np.hypot(xx - cx, yy - cy))
    return out


def fields(cov, size, P, foam_discs):
    """Distance fields for the letters and the rug that carries them (and the foam)."""
    sd = signed_distance(cov > 0.5) + BOLD * size
    ro, close = P["ro"] * size, P["close"] * size
    union = sd + ro
    if foam_discs:
        union = np.maximum(union, disc_field(sd.shape, foam_discs, ro + close + 4) + 0.55 * ro)
    dil = union > -close
    rug_sd = signed_distance(dil) - close
    rug_sd = ndi.gaussian_filter(rug_sd, 0.014 * size)            # round every corner
    gap = ndi.maximum_filter(np.maximum(-sd, 0.0), size=int(0.09 * size) | 1)
    return dict(sd=sd.astype(np.float32), rug_sd=rug_sd.astype(np.float32), gap=gap.astype(np.float32))


# ---- plush pile -----------------------------------------------------------
def jitter_grid(mask_fn, H, W, step, rng):
    ys, xs = np.mgrid[0:H:step, 0:W:step].astype(np.float32)
    ys = (ys + rng.uniform(0, step, ys.shape)).ravel()
    xs = (xs + rng.uniform(0, step, xs.shape)).ravel()
    ok = (ys < H - 1) & (xs < W - 1)
    ys, xs = ys[ok], xs[ok]
    keep = mask_fn(ys.astype(int), xs.astype(int))
    return ys[keep], xs[keep]


def render_pile(F, size, seed, P):
    """Clumped, curled wool locks filling sd > 0 and fraying softly outward. Premultiplied RGBA."""
    rng = np.random.default_rng(seed)
    sd, gap = F["sd"], F["gap"]
    H, W = sd.shape
    k = P["contrast"]
    body_c, tip_c = C["wool"], C["wool_tip"]
    root_c = mix(body_c, C["wool_root"], k)
    deep_c = mix(body_c * 0.9, C["wool_deep"], k)
    radius = 0.07 * size
    h, shade = pillow_shade(sd, radius, 0.006 * size)
    ao = np.sqrt(np.clip(blur((sd > 0).astype(np.float32), 0.04 * size) * 1.7, 0.0, 1.0))
    shade = shade * (0.82 + 0.18 * ao)
    sds = ndi.gaussian_filter(sd, 0.004 * size)
    gy, gx = np.gradient(sds)
    gl = np.sqrt(gx * gx + gy * gy) + 1e-6
    nx, ny = -gx / gl, -gy / gl                               # outward normal
    fx = smooth_noise(sd.shape, 0.09 * size, rng) * 0.55 + 0.30  # interior comb, leaning down
    fy = smooth_noise(sd.shape, 0.09 * size, rng) * 0.55 + 0.70
    E = 0.05 * size                                           # edge-influence depth
    # base under the locks: light in the core, deeper only near the rim and in the f grooves
    epx = 1.0 - smoothstep(0.0, E, sd)
    basec = mix(mix(root_c, body_c, 0.6), deep_c, epx)
    base_rgb = basec * (0.55 + 0.55 * shade)[..., None]
    cov = np.clip(sd + 0.5, 0.0, 1.0)
    base = np.concatenate([np.clip(base_rgb, 0, 1), cov[..., None]], axis=2)
    img = Image.fromarray((base * 255 + 0.5).astype(np.uint8), "RGBA")
    del base, base_rgb, basec, epx, sds, gy, gx, gl, ao

    step_px = P["step"] * size
    ty, tx = jitter_grid(lambda y, x: sd[y, x] > -0.5, H, W, step_px, rng)
    ey, ex = jitter_grid(lambda y, x: (sd[y, x] > 0.5) & (sd[y, x] < 0.35 * E), H, W, step_px * 0.65, rng)
    ty, tx = np.concatenate([ty, ey]), np.concatenate([tx, ex])
    iy, ix = ty.astype(int), tx.astype(int)
    s0 = sd[iy, ix]
    e = 1.0 - smoothstep(0.0, E, s0)                          # 1 at the edge
    nxs, nys = nx[iy, ix], ny[iy, ix]
    dx = nxs * P["out"] * e + fx[iy, ix] * (1.0 - e)
    dy = nys * P["out"] * e + fy[iy, ix] * (1.0 - e)
    dl = np.sqrt(dx * dx + dy * dy) + 1e-6
    dx, dy = dx / dl, dy / dl
    L = (P["l_in"] + (P["l_edge"] - P["l_in"]) * e ** 1.5) * size * rng.uniform(0.8, 1.2, len(ty))
    oy = np.clip((ty - nys * (s0 + 3)).astype(int), 0, H - 1)
    ox = np.clip((tx - nxs * (s0 + 3)).astype(int), 0, W - 1)
    cosn = np.clip(dx * nxs + dy * nys, 0.25, 1.0)
    L = np.minimum(L, (np.maximum(s0, 0) + 0.45 * gap[oy, ox] + 2.0) / cosn)
    rows = np.nonzero((sd > 0).any(axis=1))[0]
    vgrad = np.clip((ty - rows.min()) / (rows.max() - rows.min()), 0, 1)
    lf = (0.47 + 0.72 * shade[iy, ix]) * (1.06 - 0.12 * vgrad)
    lf = 0.97 + (lf - 0.97) * P["shade_k"]
    order = np.argsort(h[iy, ix] + rng.uniform(0, 0.35 * radius, len(ty)))
    ty, tx, dx, dy, L, e, lf = (a[order] for a in (ty, tx, dx, dy, L, e, lf))
    nt, S = len(ty), P["strands"]
    print(f"  pile: {nt} tufts x {S} strands")
    rep = lambda a: np.repeat(a, S)
    Tx, Ty, Dx, Dy, Lt, Et, LF = map(rep, (tx, ty, dx, dy, L, e, lf))
    Curl, Warm = rep(rng.normal(0, P["curl"], nt)), rep(rng.uniform(-1, 1, nt))
    n = nt * S
    r = 0.5 * step_px * np.sqrt(rng.uniform(0, 1, n))
    a = rng.uniform(0, 2 * np.pi, n)
    x0, y0 = Tx + r * np.cos(a), Ty + r * np.sin(a)
    ang = np.arctan2(Dy, Dx) + rng.normal(0, 1, n) * (0.14 + 0.12 * Et)
    ln = Lt * rng.uniform(0.75, 1.1, n)
    cx_, cy_ = np.cos(ang), np.sin(ang)
    cl = np.clip(P["clump"] * (1.0 + 0.4 * Et), 0, 0.85) * rng.uniform(0.7, 1.0, n)  # strands of a lock meet
    tipx = (x0 + cx_ * ln) * (1 - cl) + (Tx + Dx * Lt) * cl
    tipy = (y0 + cy_ * ln) * (1 - cl) + (Ty + Dy * Lt) * cl
    bend = (Curl * (1.0 + P["edge_curl"] * Et) + rng.normal(0, 0.05, n)) * ln   # locks curl, more at the rim
    px, py = -cy_, cx_
    ts = (0.0, 0.3, 0.58, 0.8, 1.0)
    pts = np.empty((n, len(ts), 2), np.float32)
    for j, t in enumerate(ts):
        bow = 4 * t * (1 - t) * bend
        pts[:, j, 0] = x0 + (tipx - x0) * t + px * bow
        pts[:, j, 1] = y0 + (tipy - y0) * t + py * bow
    root_c = mix(body_c, C["wool_root"], k)
    ins = [mix(root_c, body_c, 0.80), mix(root_c, body_c, 0.95), mix(body_c, tip_c, 0.45), mix(body_c, tip_c, 0.8)]
    edg = [root_c, mix(root_c, body_c, 0.55), mix(body_c, tip_c, 0.25), mix(body_c, tip_c, 0.55)]
    tint = np.stack([1.0 + 0.02 * Warm, np.ones(n, np.float32), 1.0 - 0.035 * Warm], axis=1)
    lit = (LF * rng.uniform(0.94, 1.05, n))[:, None]
    cols = np.empty((n, 4, 4), np.uint8)
    cols[..., 3] = 255
    for j in range(4):
        c = (ins[j][None] * (1 - Et[:, None]) + edg[j][None] * Et[:, None]) * tint * lit
        cols[:, j, :3] = np.clip(c * 255 + 0.5, 0, 255).astype(np.uint8)
    widths = [SS, SS, max(1, int(round(0.67 * SS))), max(1, int(round(0.6 * SS)))]
    flat = pts.reshape(n, -1).tolist()
    cl8 = [[tuple(c) for c in row] for row in cols.tolist()]
    draw = ImageDraw.Draw(img)
    line = draw.line
    for p, c in zip(flat, cl8):
        line(p[0:4], fill=c[0], width=widths[0])
        line(p[2:6], fill=c[1], width=widths[1])
        line(p[4:8], fill=c[2], width=widths[2])
        line(p[6:10], fill=c[3], width=widths[3])
    prem = from_image(img)
    if P["pile_blur"] > 0:
        prem = ndi.gaussian_filter(prem, (P["pile_blur"] * size, P["pile_blur"] * size, 0))
    return prem


# ---- rug field, serged binding, guard dots, thickness ----------------------
def contour_dots(sd, level, pitch, min_len):
    """Evenly spaced points along the iso-line sd == level (px coords)."""
    pts = []
    for c in measure.find_contours(sd, level):
        seg = np.hypot(*np.diff(c, axis=0).T)
        s = np.concatenate([[0.0], np.cumsum(seg)])
        if s[-1] < min_len:
            continue
        n = max(1, int(round(s[-1] / pitch)))
        q = np.linspace(0, s[-1], n, endpoint=False)
        pts.append(np.stack([np.interp(q, s, c[:, 1]), np.interp(q, s, c[:, 0])], axis=1))
    return np.concatenate(pts) if pts else np.zeros((0, 2))


def dots_mask(shape, pts, r):
    im = Image.new("L", (shape[1], shape[0]), 0)
    d = ImageDraw.Draw(im)
    for x, y in pts:
        d.ellipse([x - r, y - r, x + r, y + r], fill=255)
    return np.asarray(im, np.float32) / 255.0


def render_rug(rug_sd, size, seed, P):
    """Madder rug field (knots, abrash, vacuum-nap stripes), indigo serged binding, cream guard dots."""
    rng = np.random.default_rng(seed)
    H, W = rug_sd.shape
    cov = np.clip(rug_sd + 0.5, 0.0, 1.0)
    Y, X = np.mgrid[0:H, 0:W].astype(np.float32) / size          # em units
    _, shade = pillow_shade(rug_sd, 0.035 * size, 0.004 * size)
    bw = P["bind"] * size
    bind = 1.0 - smoothstep(bw - 0.6 * SS, bw + 0.6 * SS, rug_sd)
    _, bshade = pillow_shade(rug_sd, bw, 0.002 * size)
    kn = knot_texture(X, Y, 0.013, 0.011, amp=0.10 * P["tex"], jit=0.05 * P["tex"], seed=seed)
    abr = fbm(X / 1.4, Y / 0.08, seed + 3, 3) - 0.5
    a = np.radians(78.0)
    wave = np.sin(2 * np.pi * (X * np.cos(a) + Y * np.sin(a)) / 0.46)
    nap = smoothstep(-0.35, 0.35, wave) * 2.0 - 1.0
    del Y, X, wave
    t = np.clip(0.25 + 0.95 * shade, 0, 1.15)
    field = C["madder_lo"] + (C["madder"] - C["madder_lo"]) * t[..., None]
    field = field + (C["madder_hi"] - C["madder"]) * np.clip(t - 0.9, 0, 1)[..., None] * 2.0
    field = field * (kn * (1.0 + 0.10 * P["tex"] * abr) * (1.0 + P["nap"] * nap))[..., None]
    rim = C["indigo"] * (0.55 + 1.0 * bshade[..., None]) * (1.0 + 0.8 * (kn - 1.0))[..., None]
    rim = rim + (C["indigo_hi"] - C["indigo"]) * smoothstep(0.75, 1.0, bshade)[..., None]
    rgb = field * (1 - bind[..., None]) + rim * bind[..., None]
    if P["guard"]:
        inset, r, pitch = (v * size for v in P["guard"])
        pts = contour_dots(rug_sd, inset, pitch, 12 * pitch)
        dm = dots_mask(rug_sd.shape, pts, r) * (1.0 - bind)
        dcol = mix(C["cream"], C["madder"], 0.30) * (0.80 + 0.28 * shade[..., None]) * kn[..., None]
        rgb = rgb * (1 - 0.8 * dm[..., None]) + 0.8 * dcol * dm[..., None]
    return np.clip(rgb, 0, 1).astype(np.float32), cov.astype(np.float32)


def extrude(cov, depth, top, bottom, rng):
    """Thickness below a shape: shifted copies, darker further down."""
    acc = np.zeros_like(cov)
    kmap = np.full(cov.shape, float(depth), np.float32)
    for kk in range(1, int(depth) + 1):
        sh = np.zeros_like(cov)
        sh[kk:] = cov[:-kk]
        kmap[(sh > 0.5) & (kmap >= depth)] = kk
        np.maximum(acc, sh, out=acc)
    t = smoothstep(0.0, depth, kmap)[..., None]
    rgb = top[None, None] * (1 - t) + bottom[None, None] * t
    streak = 1.0 + 0.06 * ndi.gaussian_filter(rng.standard_normal((1, cov.shape[1])).astype(np.float32), (0, SS))
    return np.clip(rgb * streak[..., None], 0, 1).astype(np.float32), acc


# ---- foam bubbles and sparkle (the cleaning cue) ---------------------------
def paint(dst, col, alpha, y0, x0):
    """Premultiplied 'over' of a straight colour + alpha patch at (y0, x0), clipped to dst."""
    h, w = alpha.shape
    Y0, X0 = max(0, y0), max(0, x0)
    Y1, X1 = min(dst.shape[0], y0 + h), min(dst.shape[1], x0 + w)
    if Y1 <= Y0 or X1 <= X0:
        return
    a = np.clip(alpha[Y0 - y0:Y1 - y0, X0 - x0:X1 - x0], 0, 1)[..., None]
    c = np.broadcast_to(col, alpha.shape + (3,))[Y0 - y0:Y1 - y0, X0 - x0:X1 - x0]
    reg = dst[Y0:Y1, X0:X1]
    reg *= (1 - a)
    reg[..., :3] += c * a
    reg[..., 3:4] += a


def disc_dist(n_y, n_x, cx, cy):
    yy, xx = np.mgrid[0:n_y, 0:n_x].astype(np.float32)
    return np.hypot(xx - cx, yy - cy)


def draw_foam(buf, discs, lw):
    """Soft suds mound + glossy pink-violet bubbles; discs = [(cx, cy, r)] in buf px."""
    m = int(max(r for _, _, r in discs) * 0.6 + 3 * lw)
    x0 = int(min(cx - r for cx, _, r in discs)) - m
    y0 = int(min(cy - r for _, cy, r in discs)) - m
    x1 = int(max(cx + r for cx, _, r in discs)) + m
    y1 = int(max(cy + r for _, cy, r in discs)) + m
    kk = 0.45 * np.mean([r for _, _, r in discs])
    sd = None
    for cx, cy, r in discs:                                  # polynomial smooth-min union
        dd = disc_dist(y1 - y0, x1 - x0, cx - x0, cy - y0) - r * 0.92
        if sd is None:
            sd = dd
            continue
        hh = np.clip(0.5 + 0.5 * (dd - sd) / kk, 0, 1)
        sd = dd * (1 - hh) + sd * hh - kk * hh * (1 - hh)
    sh = ndi.shift(blur(np.clip(0.5 - sd, 0, 1), 1.2 * lw), (0.9 * lw, 0.5 * lw), order=1)
    paint(buf, C["indigo_lo"], sh * 0.45, y0, x0)            # contact shadow on the rug
    paint(buf, C["foam_line"], np.clip(lw - sd + 0.5, 0, 1), y0, x0)
    yy = np.arange(y1 - y0, dtype=np.float32)[:, None] * np.ones((1, x1 - x0), np.float32)
    kv = smoothstep(0.0, float(y1 - y0), yy)[..., None]
    paint(buf, C["suds_top"] * (1 - kv) + C["suds_bot"] * kv, np.clip(0.5 - sd, 0, 1), y0, x0)
    for cx, cy, r in sorted(discs, key=lambda d: d[1] - 0.2 * d[2]):
        R = r + lw + 2
        bx, by = int(cx - R), int(cy - R)
        n = int(2 * R) + 2
        d = disc_dist(n, n, cx - bx, cy - by)
        paint(buf, C["foam_line"], np.clip(r + lw - d + 0.5, 0, 1), by, bx)
        yy, xx = np.mgrid[0:n, 0:n].astype(np.float32)
        nx, ny = (xx - (cx - bx)) / r, (yy - (cy - by)) / r
        nr = np.clip(np.hypot(nx, ny), 0, 1)
        nz = np.sqrt(1 - nr * nr)
        lit = np.clip(nx * LIGHT[0] + ny * LIGHT[1] + nz * LIGHT[2], 0, 1)[..., None]
        col = mix(C["foam_hi"], C["foam_mid"], smoothstep(0.2, 0.95, nr) ** 1.2)
        col = mix(col, C["foam_rim"], smoothstep(0.78, 1.0, nr) * 0.75)
        col = col * (0.84 + 0.22 * lit)
        dirk = smoothstep(0.35, 0.85, (nx * 0.6 + ny * 0.8) / (nr + 1e-4))
        band = smoothstep(0.64, 0.76, nr) * (1 - smoothstep(0.86, 0.94, nr))
        col = mix(col, C["foam_hi"], dirk * band * 0.6)          # inner reflection, lower right
        sx, sy = nx + 0.34, ny + 0.38
        u, v = (sx - sy) * 0.7071, (sx + sy) * 0.7071
        win = 1 - smoothstep(0.72, 1.0, np.hypot(u / 0.30, v / 0.15))
        dot = 1 - smoothstep(0.6, 1.0, np.hypot(nx + 0.66, ny + 0.02) / 0.075)
        col = mix(col, np.ones(3, np.float32), np.clip(win * 0.95 + dot * 0.9, 0, 1))
        paint(buf, np.clip(col, 0, 1), np.clip(r - d + 0.5, 0, 1), by, bx)


def draw_spark(buf, cx, cy, span, lw):
    """Cream four-point sparkle with a thin indigo keyline (no glow: no smudge on dark)."""
    n = int(span * 1.25 + 4 * lw)
    x0, y0 = int(cx - n / 2), int(cy - n / 2)
    t = np.linspace(0, 2 * np.pi, 1440, endpoint=False)
    c, s = np.cos(t), np.sin(t)
    pw, rr = 3.1, span / 2.0
    xs, ys = np.sign(c) * np.abs(c) ** pw * rr, np.sign(s) * np.abs(s) ** pw * rr * 1.1
    im = Image.new("L", (n, n), 0)
    ImageDraw.Draw(im).polygon(list(zip(n / 2 + xs, n / 2 + ys)), fill=255)
    sd = signed_distance(np.asarray(im) > 127)
    paint(buf, C["indigo"], np.clip(sd + lw + 0.5, 0, 1), y0, x0)
    rd = disc_dist(n, n, n / 2, n / 2) / rr
    col = mix(np.ones(3, np.float32), C["spark"], smoothstep(0.05, 0.5, rd))
    col = mix(col, C["gold"], smoothstep(0.35, 1.0, rd) * 0.35)
    paint(buf, col, np.clip(sd + 0.5, 0, 1), y0, x0)


# ---- wordmark ---------------------------------------------------------------
def render_wordmark(P, seed=11):
    """Premultiplied RGBA of the rug-cut word at final resolution + layout info."""
    t0 = time.time()
    size = size_for_width(P["text_w"] * SCALE * SS)
    cov, letters, baseline = word_layout(size)
    last = letters[-1]["to_canvas"]
    foam = [(*last(u, v), r * size * P["foam_scale"]) for u, v, r in FOAM] if P["foam"] else []
    F = fields(cov, size, P, foam)
    print(f"  fields {cov.shape} in {time.time() - t0:.1f}s")
    pile = render_pile(F, size, seed + 1, P)
    print(f"  pile drawn in {time.time() - t0:.1f}s")
    rng = np.random.default_rng(seed)
    rug_rgb, rug_cov = render_rug(F["rug_sd"], size, seed + 2, P)
    fa = pile[..., 3]
    rows = np.nonzero((fa > 0.3).any(axis=1))[0]
    fur_bottom = rows.max() / SS
    shadow = ndi.shift(blur(fa, 0.010 * size), (0.014 * size, 0.004 * size), order=1)
    rug_rgb *= (1.0 - 0.45 * np.clip(shadow, 0, 1))[..., None]
    del shadow, fa
    side_rgb, side_cov = extrude(rug_cov, P["extrude"] * size, C["indigo"] * 0.85, C["indigo_lo"] * 0.8, rng)
    out = layer(side_rgb, side_cov)
    del side_rgb, side_cov
    over(out, layer(rug_rgb, rug_cov))
    del rug_rgb, rug_cov
    over(out, pile)
    del pile
    if foam:
        draw_foam(out, foam, P["foam_lw"] * size)
    if P["spark"]:
        sx, sy = last(SPARK[0], SPARK[1])
        draw_spark(out, sx, sy, SPARK[2] * size * P["spark_scale"], P["spark_lw"] * size)
    out = reduce_ss(out)
    print(f"  wordmark done in {time.time() - t0:.1f}s")
    return out, dict(size=size / SS, fur_bottom=fur_bottom)


# ---- subtitle runner ------------------------------------------------------
SUB_CAP = 0.215             # subtitle cap height / wordmark cap height
SUB_TRACK = 0.055           # subtitle tracking (em of the subtitle font)
RUN_H = 2.05                # runner height / subtitle cap height


def rrect_sd(X, Y, x0, y0, x1, y1, r):
    """Signed distance of a rounded rectangle, + inside."""
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    qx = np.abs(X - cx) - ((x1 - x0) / 2 - r)
    qy = np.abs(Y - cy) - ((y1 - y0) / 2 - r)
    out = np.sqrt(np.maximum(qx, 0) ** 2 + np.maximum(qy, 0) ** 2) + np.minimum(np.maximum(qx, qy), 0) - r
    return -out


def text_mask(text, ft, track_px):
    asc, desc = ft.getmetrics()
    w = int(sum(ft.getlength(c) for c in text) + track_px * (len(text) - 1)) + 4
    im = Image.new("L", (w, asc + desc + 4), 0)
    d = ImageDraw.Draw(im)
    x = 2.0
    for c in text:
        d.text((x, 2), c, font=ft, fill=255)
        x += ft.getlength(c) + track_px
    a = np.asarray(im, np.float32) / 255.0
    ys, xs = np.nonzero(a > 0.02)
    return a[ys.min():ys.max() + 1, xs.min():xs.max() + 1]


def poly_mask(polys, W, H):
    im = Image.new("L", (W, H), 0)
    d = ImageDraw.Draw(im)
    for p in polys:
        d.polygon([tuple(q) for q in p], fill=255)
    return np.asarray(im, np.float32) / 255.0


def diamond(cx, cy, w, h):
    return [(cx - w / 2, cy), (cx, cy - h / 2), (cx + w / 2, cy), (cx, cy + h / 2)]


def guard_dots(x0, y0, x1, y1, inset, pitch):
    """Dot centres of the runner's guard line: one whole dot centred on each corner, the rest evenly spaced
    per side (no dot is ever split by a mitre, so the corners carry no clipped slivers)."""
    ax0, ay0, ax1, ay1 = x0 + inset, y0 + inset, x1 - inset, y1 - inset
    corners = [(ax0, ay0), (ax1, ay0), (ax1, ay1), (ax0, ay1)]
    pts = []
    for (px0, py0), (px1, py1) in zip(corners, corners[1:] + corners[:1]):
        n = max(1, int(round(np.hypot(px1 - px0, py1 - py0) / pitch)))
        pts += [(px0 + (px1 - px0) * k / n, py0 + (py1 - py0) * k / n) for k in range(n)]
    return pts


def spline(pts, n=40):
    """Centripetal Catmull-Rom through pts (C1-smooth, no cusps or overshoot on uneven spacing) -> dense polyline."""
    P = np.asarray(pts, np.float64)
    P = np.vstack([2 * P[0] - P[1], P, 2 * P[-1] - P[-2]])
    out = []
    for i in range(1, len(P) - 2):
        p0, p1, p2, p3 = P[i - 1], P[i], P[i + 1], P[i + 2]
        t0 = 0.0
        t1 = t0 + np.linalg.norm(p1 - p0) ** 0.5 + 1e-6
        t2 = t1 + np.linalg.norm(p2 - p1) ** 0.5 + 1e-6
        t3 = t2 + np.linalg.norm(p3 - p2) ** 0.5 + 1e-6
        t = np.linspace(t1, t2, n, endpoint=False)[:, None]
        a1 = (t1 - t) / (t1 - t0) * p0 + (t - t0) / (t1 - t0) * p1
        a2 = (t2 - t) / (t2 - t1) * p1 + (t - t1) / (t2 - t1) * p2
        a3 = (t3 - t) / (t3 - t2) * p2 + (t - t2) / (t3 - t2) * p3
        b1 = (t2 - t) / (t2 - t0) * a1 + (t - t0) / (t2 - t0) * a2
        b2 = (t3 - t) / (t3 - t1) * a2 + (t - t1) / (t3 - t1) * a3
        out.append((t2 - t) / (t2 - t1) * b1 + (t - t1) / (t2 - t1) * b2)
    out.append(P[-2][None])
    return np.vstack(out)


def stroke_polygon(line, half, shift=None):
    """Outline of a variable-width stroke along a dense polyline: half-width per point (px), optional sideways
    shift per point (+ = left of travel). The tip gets a round cap; the start is left open (hidden in the knot)."""
    d = np.gradient(line, axis=0)
    d /= np.linalg.norm(d, axis=1, keepdims=True) + 1e-9
    nrm = np.stack([d[:, 1], -d[:, 0]], axis=1)
    c = line + (nrm * shift[:, None] if shift is not None else 0.0)
    left, right = c + nrm * half[:, None], c - nrm * half[:, None]
    a0 = np.arctan2(nrm[-1, 1], nrm[-1, 0])
    cap = [c[-1] + half[-1] * np.array([np.cos(a0 + k * np.pi / 10), np.sin(a0 + k * np.pi / 10)])
           for k in range(1, 10)]
    ring = np.vstack([left, cap, right[::-1]])
    return [tuple(p) for p in ring.tolist()]


def draw_tassels(W, H, xl, xr, y0, y1, RH, rng):
    """Knotted warp fringe on both square ends, spanning the full end height (SS px). Prem RGBA.

    Every strand is ONE continuous tapered stroke: a centripetal Catmull-Rom curve from its root under the runner
    end, through the knot, sagging gently to the tip; full width up to the knot, then narrowing smoothly to ~35 %
    with a round tip. It is drawn as three nested polygons along the same curve (dark fibre outline of constant
    thickness, cream body, a lit stripe on the upper side), so the outline stays even from knot to tip.
    """
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    nb, per = 6, 4
    LT = 0.62 * RH
    hw = 0.017 * RH                                             # strand half-width at the root
    ow = 0.0075 * RH                                            # outline thickness
    ys = np.linspace(y0 + 0.11 * RH, y1 - 0.11 * RH, nb)
    rgb8 = lambda c: tuple(int(q) for q in np.clip(np.asarray(c) * 255 + 0.5, 0, 255)) + (255,)
    knots = []
    for side, xe in ((-1, xl), (1, xr)):
        for yb in ys:
            xk = xe + side * 0.13 * RH                          # knot just outside the end edge
            lit = rng.uniform(0.93, 1.03) * (1.04 - 0.14 * (yb - y0) / RH)
            for j in rng.permutation(per):
                o = j - (per - 1) / 2
                root = (xe - side * 0.06 * RH, yb + o * 0.040 * RH)
                knot = (xk, yb + o * 0.011 * RH)
                ln = LT * rng.uniform(0.84, 1.04)
                tip = (xe + side * ln, yb + o * 0.027 * RH + 0.050 * RH + rng.normal(0, 0.008 * RH))
                f = 0.55 + rng.normal(0, 0.03)
                sag = 0.020 * RH + rng.normal(0, 0.006 * RH)
                mid = (knot[0] + (tip[0] - knot[0]) * f, knot[1] + (tip[1] - knot[1]) * f + sag)
                line = spline([root, knot, mid, tip])
                seg = np.hypot(*np.diff(line, axis=0).T)
                s = np.concatenate([[0.0], np.cumsum(seg)])
                sk = s[np.argmin(np.hypot(line[:, 0] - knot[0], line[:, 1] - knot[1]))]
                t = np.clip((s - sk) / (s[-1] - sk), 0.0, 1.0)
                w = hw * rng.uniform(0.9, 1.08) * (1.0 - 0.65 * smoothstep(0.0, 1.0, t) ** 0.85)
                up = side                                        # + shift (normal = travel turned -90) is up on the right end
                d.polygon(stroke_polygon(line, w + ow), fill=rgb8(C["cream_lo"] * lit * 0.42))
                d.polygon(stroke_polygon(line, w), fill=rgb8(mix(C["cream_lo"], C["cream"], 0.72) * lit))
                n0 = int(0.06 * len(line))
                d.polygon(stroke_polygon(line[n0:], 0.42 * w[n0:], up * 0.40 * w[n0:]),
                          fill=rgb8(np.minimum(C["cream"] * lit * 1.05, 1.0)))
            knots.append((xk, yb))
    kw, kh = 0.075 * RH, 0.105 * RH
    for xk, yb in knots:                                         # madder wraps over the knots
        box = [xk - kw / 2, yb - kh / 2, xk + kw / 2, yb + kh / 2]
        d.rounded_rectangle(box, radius=kw * 0.35, fill=rgb8(C["indigo_lo"]))
        inner = [box[0] + 0.012 * RH, box[1] + 0.012 * RH, box[2] - 0.012 * RH, box[3] - 0.012 * RH]
        d.rounded_rectangle(inner, radius=kw * 0.3, fill=rgb8(C["madder_hi"]))
    return from_image(im)


def render_runner(word_size, seed=23):
    """Woven teal runner with the subtitle; returns final-res prem RGBA and the body box (x0, y0, x1, y1)."""
    rng = np.random.default_rng(seed)
    probe = font(1000, SUB_AXES)
    bb = probe.getbbox("H")
    word_cap = 0.70 * word_size
    sub_cap = SUB_CAP * word_cap * SS
    ft = font(sub_cap / ((bb[3] - bb[1]) / 1000.0), SUB_AXES)
    tm = text_mask(SUB, ft, SUB_TRACK * ft.size)
    cap = tm.shape[0]
    RH = RUN_H * cap
    med = 0.60 * RH
    side = 0.34 * RH + med + 0.30 * RH
    RW = tm.shape[1] + 2 * side
    mx, my, depth = 0.72 * RH, 0.10 * RH, 0.06 * RH
    W, H = int(RW + 2 * mx), int(RH + 2 * my + depth)
    x0, y0, x1, y1 = mx, my, mx + RW, my + RH
    Y, X = np.mgrid[0:H, 0:W].astype(np.float32)
    sd = rrect_sd(X, Y, x0, y0, x1, y1, 0.035 * RH)
    wob = (valnoise(X / (0.012 * RH), Y / (0.012 * RH), 77) - 0.5) * 0.9 * SS
    body = np.clip(sd + wob * 0.5 + 0.5, 0, 1)
    ins = np.maximum(sd + wob * 0.4, 0)
    E = word_size * SS                                          # em of the word (knot scale)
    kn = knot_texture(X / E, Y / E, 0.0105, 0.0095, amp=0.12, jit=0.05, seed=seed)
    _, rim = pillow_shade(sd, 0.05 * RH, 0.01 * RH)
    abr = fbm(X / (3.0 * RH), Y / (0.25 * RH), seed + 1, 3) - 0.5
    e1, e2, e3 = 0.050 * RH, 0.135 * RH, 0.165 * RH
    aa = 0.6 * SS
    dots = guard_dots(x0, y0, x1, y1, (e1 + e2) / 2, 0.11 * RH)
    dot = disc_field((H, W), [(dx_, dy_, 0.029 * RH) for dx_, dy_ in dots], 2 * aa)   # + inside a dot
    rgb = C["indigo"] * (kn * (0.7 + 0.7 * rim))[..., None]
    band = C["madder"] * (kn * (1 + 0.10 * abr))[..., None]
    band = mix(band, mix(C["cream"], C["madder"], 0.12) * kn[..., None], smoothstep(-aa, aa, dot))
    rgb = mix(rgb, band, smoothstep(e1 - aa, e1 + aa, ins))
    rgb = mix(rgb, C["cream"] * kn[..., None], smoothstep(e2 - aa, e2 + aa, ins))
    t = np.clip((Y - y0) / RH, 0, 1)
    field = mix(C["teal_hi"], C["teal_lo"], smoothstep(0.05, 1.0, t))
    field = field * (kn * (1 + 0.12 * abr))[..., None]
    rgb = mix(rgb, field, smoothstep(e3 - aa, e3 + aa, ins))
    del dot, band, field, wob, ins, abr
    cy = (y0 + y1) / 2
    for s in (-1, 1):                                            # lozenge medallions
        cx = x0 + 0.30 * RH + med / 2 if s < 0 else x1 - 0.30 * RH - med / 2
        outer = [diamond(cx, cy, med, med * 0.80),
                 diamond(cx - s * med * 0.62, cy, med * 0.16, med * 0.16),
                 diamond(cx + s * med * 0.62, cy, med * 0.16, med * 0.16)]
        mo = poly_mask(outer, W, H)
        shd = ndi.gaussian_filter(np.roll(mo, int(0.03 * RH), 0), 0.02 * RH)
        rgb = mix(rgb, C["indigo_lo"], shd * 0.5)
        rgb = mix(rgb, C["cream"] * kn[..., None], mo)
        rgb = mix(rgb, C["madder"] * kn[..., None], poly_mask([diamond(cx, cy, med * 0.64, med * 0.51)], W, H))
        rgb = mix(rgb, C["gold"], poly_mask([diamond(cx, cy, med * 0.26, med * 0.21)], W, H))
    full = np.zeros((H, W), np.float32)
    tx0, ty0 = int(round((W - tm.shape[1]) / 2)), int(round(cy - cap / 2))
    full[ty0:ty0 + cap, tx0:tx0 + tm.shape[1]] = tm
    tsh = ndi.shift(blur(full, 0.05 * cap), (0.07 * cap, 0.0), order=1)
    rgb = mix(rgb, C["indigo_lo"], np.clip(tsh, 0, 1) * 0.75)
    tcol = mix(C["cream"] * 1.03, C["cream_lo"], smoothstep(ty0 + 0.35 * cap, ty0 + cap, Y) * 0.45)
    rgb = mix(rgb, np.clip(tcol * (0.86 + 0.14 * kn)[..., None], 0, 1), full)
    side_rgb, side_cov = extrude(body, depth, C["indigo"] * 0.8, C["indigo_lo"] * 0.7, rng)
    out = layer(side_rgb, side_cov)
    over(out, draw_tassels(W, H, x0, x1, y0, y1, RH, rng))
    over(out, layer(np.clip(rgb, 0, 1), body))
    return reduce_ss(out), (x0 / SS, y0 / SS, x1 / SS, y1 / SS)


# ---- lockup, shadow, icon mark -----------------------------------------------
def with_shadow(prem, size, pad_out, ret_off=False):
    """Cool, tight drop shadow (blurred over the whole silhouette: no holes) + contact shadow."""
    pad = int(0.10 * size)
    big = np.zeros((prem.shape[0] + 2 * pad, prem.shape[1] + 2 * pad, 4), np.float32)
    big[pad:-pad, pad:-pad] = prem
    a = big[..., 3]
    drop = ndi.shift(blur(a, 0.017 * size), (0.024 * size, 0.006 * size), order=1)
    contact = ndi.shift(blur(a, 0.005 * size), (0.009 * size, 0.0), order=1)
    out = layer(C["shadow"], np.clip(drop, 0, 1) * 0.42)
    over(out, layer(C["shadow"], np.clip(contact, 0, 1) * 0.30))
    over(out, big)
    res, (dy, dx) = crop_pad(out, pad_out, ret_off=True)
    return (res, (dy + pad, dx + pad)) if ret_off else res


def lockup(word, info, sub, box):
    """Runner hung under the word, overlapping the rug's lower edge like a sewn label."""
    size = info["size"]
    sub_top = box[1]
    wh, ww = word.shape[:2]
    sh, sw = sub.shape[:2]
    cols = np.nonzero((word[..., 3] > 0.5).any(axis=0))[0]
    cx = 0.5 * (cols.min() + cols.max())
    oy = int(round(info["fur_bottom"] + 0.010 * size - sub_top))
    ox = int(round(cx - sw / 2))
    x_shift = max(0, -ox)
    H = max(wh, oy + sh) + 2
    W = max(ww, ox + sw) + 2 + x_shift
    canvas = np.zeros((H, W, 4), np.float32)
    canvas[:wh, x_shift:x_shift + ww] = word
    placed = np.zeros_like(canvas)
    placed[oy:oy + sh, x_shift + ox:x_shift + ox + sw] = sub
    bx = x_shift + ox
    RUNNER_BOX["box"] = rbox = (bx + box[0], oy + box[1], bx + box[2], oy + box[3])
    back = backing(canvas[..., 3], rbox, size)                   # one die-cut silhouette: no pockets
    over(back, canvas)
    canvas = back
    cs = ndi.shift(blur(placed[..., 3], 0.012 * size), (0.016 * size, 0), order=1)
    canvas[..., :3] *= (1.0 - 0.50 * np.clip(cs, 0, 1))[..., None]
    over(canvas, placed)
    return canvas


FRINGE_REACH = 0.62 * 1.04 + 0.03   # fringe length past the runner end / runner height (see draw_tassels)
LEDGE = 0.10                        # backing ledge over the fringe: depth below the runner top / runner height


def backing(word_a, rbox, size):
    """Navy backing between the rug's wavy underside and the runner's straight top edge (prem RGBA layer).

    The rug is a dilation of the letters, so its underside dips under every letter and rises between them; with
    the runner sewn on below, those rises enclose transparent pockets. The backing continues the rug's navy
    thickness straight down to the runner across the runner's width (flush with its square ends), and a closing
    rounds the joins and seals the notch above the fringe, so the lockup has ONE outer contour, like a die-cut
    sticker. It sits under the rug and the runner; its edge is pulled 1 px inside theirs (no dark rim peeks out).
    """
    H, W = word_a.shape
    x0, y0, x1, y1 = rbox
    RH = y1 - y0
    yt = int(round(y0))
    xi0, xi1 = int(np.ceil(x0)) + 1, int(np.floor(x1)) - 1
    wa = word_a > 0.5
    above = wa[:int(yt + LEDGE * RH) + 3]
    has = above.any(axis=0)
    last = above.shape[0] - 1 - np.argmax(above[::-1], axis=0)    # rug's lowest opaque row per column
    # fill depth per column: down to the runner top across the runner, and over the fringe down to a ledge
    # just under the top knot, so the top tassel lies on the backing instead of touching the rug's edge
    reach, ledge = FRINGE_REACH * RH, yt + LEDGE * RH
    xs = np.arange(W, dtype=np.float32)
    out_d = np.maximum(np.maximum(x0 - xs, xs - x1), 0.0)        # distance outside the runner's ends
    w = 1.0 - smoothstep(0.92 * reach, 1.30 * reach, out_d)       # past the tips: rounds up into the rug
    last = np.where(has, last, H).astype(np.float32)
    bottom = last + (ledge - last) * w
    bottom[xi0:xi1] = yt + 3
    rows = np.arange(H)[:, None]
    fill = (rows >= (last - 3)[None]) & (rows <= bottom[None]) & (last < H)[None]
    body = np.zeros((H, W), bool)
    body[yt:int(y1), xi0 - 1:xi1 + 1] = True
    S = wa | body | fill
    r = int(round(0.035 * size))
    yy, xx = np.mgrid[-r:r + 1, -r:r + 1]
    pad = r + 2
    cl = ndi.binary_closing(np.pad(S, pad), structure=(xx * xx + yy * yy) <= r * r)[pad:-pad, pad:-pad]
    region = np.zeros((H, W), bool)
    region[max(0, yt - int(0.6 * size)):yt + int(0.12 * RH), max(0, int(x0 - 0.8 * RH)):int(x1 + 0.8 * RH)] = True
    M = (cl & region) | fill
    ro = max(1, r // 2)                                           # round the ledge's outer corners
    yy, xx = np.mgrid[-ro:ro + 1, -ro:ro + 1]
    M = ndi.binary_opening(np.pad(M, pad), structure=(xx * xx + yy * yy) <= ro * ro)[pad:-pad, pad:-pad] | (M & S)
    alpha = np.clip(ndi.gaussian_filter(signed_distance(M), 1.0) - 0.5, 0.0, 1.0)
    ao = ndi.shift(blur(word_a, 0.015 * size), (0.012 * size, 0.0), order=1)   # the rug shades the backing
    k = np.clip(ao, 0, 1)[..., None]
    rgb = C["indigo_lo"] * 0.95 * (1.0 - 0.35 * k) + C["indigo"] * 0.10 * (1.0 - k)
    added = int((M & ~(wa | body)).sum())
    print(f"  backing: {added} px of pockets / joins closed (closing r {r} px)")
    return layer(np.clip(rgb, 0, 1), alpha)


def render_mark(P, n_final=1024, seed=31):
    """Square icon: a fluffy R on a small square rug tile.

    P["bleed"] False: the tile floats on a transparent canvas with a drop shadow (community / store mark).
    P["bleed"] True: the tile fills the square edge to edge on an opaque ground, no shadow and no alpha
    (Steam turns alpha into black on the App Icon JPG and shows the shortcut icon on any colour).
    """
    N = int(round(n_final * SCALE)) * SS
    bleed = P.get("bleed", False)
    side = N + 2.0 if bleed else P["fill"] * N
    ext = 0.0 if bleed else P["ext"] * N                        # tile thickness below the face
    x0 = (N - side) / 2
    if bleed:
        y0 = x0
    elif P["shadow"]:
        y0 = x0 - 0.02 * N                                       # room for the drop shadow below
    else:
        y0 = (N - side - ext) / 2                                # tight: face + thickness centred
    Y, X = np.mgrid[0:N, 0:N].astype(np.float32)
    tile_sd = rrect_sd(X, Y, x0, y0, x0 + side, y0 + side, P["corner"] * side)
    del Y, X
    size = P["cap"] * side / 0.70                                # em so that the R cap = cap x side
    ft = font(size, FONT_AXES)
    bb = ft.getbbox("R")
    gx = x0 + side / 2 - (bb[0] + bb[2]) / 2 + P["dx"] * side
    gy = y0 + side / 2 - (bb[1] + bb[3]) / 2 + P["dy"] * side
    g = Image.new("L", (N, N), 0)
    ImageDraw.Draw(g).text((gx, gy), "R", font=ft, fill=255)
    m = ndi.binary_closing(np.asarray(g) > 127, np.ones((3, 3), bool)) | (np.asarray(g) > 127)
    ga = np.clip(signed_distance(m) + 0.5, 0.0, 1.0)             # one merged mask, as in the wordmark
    cov = np.asarray(Image.fromarray(ga, "F").rotate(-2.0, resample=Image.BICUBIC, center=(N / 2, N / 2)), np.float32)
    sd = signed_distance(cov > 0.5) + BOLD * size
    gap = ndi.maximum_filter(np.maximum(-sd, 0.0), size=int(0.09 * size) | 1)
    F = dict(sd=sd, gap=gap)
    pile = render_pile(F, size, seed, P)
    rug_rgb, rug_cov = render_rug(tile_sd, size, seed + 1, P)
    shadow = ndi.shift(blur(pile[..., 3], 0.010 * size), (0.014 * size, 0.004 * size), order=1)
    rug_rgb *= (1.0 - 0.45 * np.clip(shadow, 0, 1))[..., None]
    if bleed:
        out = layer(C["indigo_lo"], np.ones((N, N), np.float32))
    else:
        rng = np.random.default_rng(seed)
        side_rgb, side_cov = extrude(rug_cov, ext, C["indigo"] * 0.85, C["indigo_lo"] * 0.8, rng)
        out = layer(side_rgb, side_cov)
    over(out, layer(rug_rgb, rug_cov))
    over(out, pile)
    if P["spark"]:
        gap, span = P["spark"]
        lw = 0.011 * side
        inset, dot_r, pitch = (v * size for v in P["guard"])
        cx, cy = spark_corner(x0, y0, side, inset, dot_r, gap * side, span * side, lw)
        sp = np.zeros_like(out)
        draw_spark(sp, cx, cy, span * side, lw)
        dots = dots_mask(tile_sd.shape, contour_dots(tile_sd, inset, pitch, 12 * pitch), dot_r) > 0.5
        free = ndi.distance_transform_edt(~(sp[..., 3] > 0.5))
        fur = pile[..., 3] > 0.25
        print(f"  sparkle clearance: stitch {free[dots].min() / SS:.1f} px, fur {free[fur].min() / SS:.1f} px"
              f" (final px, tile {side / SS:.0f})")
        over(out, sp)
        del sp, dots, free, fur
    if not bleed and P["shadow"]:
        sb, sdy, sop = P["shadow"]
        drop = ndi.shift(blur(out[..., 3], sb * N), (sdy * N, 0.25 * sdy * N), order=1)
        res = layer(C["shadow"], np.clip(drop, 0, 1) * sop)
        over(res, out)
        out = res
    return reduce_ss(out)


def spark_corner(x0, y0, side, inset, dot_r, gap, span, lw):
    """Sparkle centre in the tile's top-right corner: its top tip and its right tip (keyline included) clear
    the stitched guard line by the same gap, so they never look tangent to it by accident."""
    rr = span / 2.0
    cx = x0 + side - inset - dot_r - gap - rr - lw
    cy = y0 + inset + dot_r + gap + 1.1 * rr + lw
    return cx, cy


# ---- presets ----------------------------------------------------------------
P_MASTER = dict(text_w=2780, ro=0.095, close=0.15, step=0.0105, l_in=0.012, l_edge=0.023, strands=8, out=1.2,
                edge_curl=1.0, curl=0.16, clump=0.45, contrast=1.0, shade_k=1.0, pile_blur=0.0,
                bind=0.026, tex=1.0, nap=0.045, guard=(0.048, 0.0050, 0.025), extrude=0.032,
                foam=True, foam_scale=1.0, foam_lw=0.007, spark=True, spark_scale=1.0, spark_lw=0.010)
# small capsules (< ~600 px): bigger, softer, lower-contrast locks, thicker binding, no guard dots / nap stripes
P_SMALL = dict(P_MASTER, text_w=1000, ro=0.10, step=0.017, l_in=0.013, l_edge=0.019, strands=12, out=1.2,
               curl=0.10, clump=0.35, contrast=0.45, shade_k=0.75, pile_blur=0.0025,
               bind=0.042, tex=0.5, nap=0.0, guard=None, extrude=0.042, foam_scale=1.15, foam_lw=0.010,
               spark_scale=1.2, spark_lw=0.016)
# icon mark: one big R on a rounded, extruded rug tile (transparent corners, soft drop shadow).
# spark = (gap to the stitched guard line, span), both as fractions of the tile side: placed in the free
# top-right corner by spark_corner(), clear of the stitch by the same gap on its top and right tips.
P_MARK = dict(P_MASTER, cap=0.63, dx=-0.02, dy=0.025, step=0.012, l_in=0.012, l_edge=0.022, strands=9,
              contrast=0.75, shade_k=0.9, bind=0.038, guard=(0.080, 0.0085, 0.034), nap=0.0,
              corner=0.17, bleed=False, fill=0.86, ext=0.022, shadow=(0.012, 0.016, 0.40),
              spark=(0.030, 0.128))
# 64 px use (shortcut .ico frame): same tile, trimmed padding and a tight shadow so it fills the frame
P_MARK_TIGHT = dict(P_MARK, fill=0.925, ext=0.026, shadow=(0.004, 0.006, 0.35), bind=0.044, spark=(0.026, 0.126))
# 48 px and below: no sparkle (it collapses to a speck), no guard dots, softer bigger locks, heavier binding,
# the tile fills ~95 % of the frame (no drop shadow, only its own thickness)
P_MARK_SMALL = dict(P_MARK, cap=0.66, dx=-0.005, dy=0.020, step=0.016, l_in=0.013, l_edge=0.018, strands=12,
                    curl=0.10, contrast=0.45, shade_k=0.75, pile_blur=0.002, bind=0.060, tex=0.5, guard=None,
                    fill=0.935, ext=0.030, shadow=None, corner=0.19, spark=None)
# App Icon (184 JPG): the same tile edge to edge, opaque; the sparkle sits in the free top-right corner
P_ICON = dict(P_MARK, bleed=True, corner=0.0, cap=0.60, dx=-0.025, dy=0.030, bind=0.036,
              guard=(0.078, 0.0085, 0.034), spark=(0.030, 0.130))


# ---- previews -----------------------------------------------------------------
def solid(w, h, hexcol):
    return np.ones((h, w, 3), np.float32) * hx(hexcol)


def comp(bg, prem, x, y):
    """bg float RGB (H,W,3); prem placed with its top-left at integer x, y (clipped)."""
    out = bg.copy()
    h, w = prem.shape[:2]
    X0, Y0 = max(0, x), max(0, y)
    X1, Y1 = min(out.shape[1], x + w), min(out.shape[0], y + h)
    p = prem[Y0 - y:Y1 - y, X0 - x:X1 - x]
    reg = out[Y0:Y1, X0:X1]
    reg *= (1.0 - p[..., 3:4])
    reg += p[..., :3]
    return out


def centred(bg, prem, dy=0):
    return comp(bg, prem, (bg.shape[1] - prem.shape[1]) // 2, (bg.shape[0] - prem.shape[0]) // 2 + dy)


def load_rgb(path, box=None, size=None, blur_px=0, dim=1.0):
    im = Image.open(path).convert("RGB")
    if box:
        im = im.crop(box)
    if size:
        im = im.resize(size, Image.LANCZOS)
    if blur_px:
        im = im.filter(ImageFilter.GaussianBlur(blur_px))
    return np.asarray(im, np.float32) / 255.0 * dim


def save_rgb(a, name):
    Image.fromarray((np.clip(a, 0, 1) * 255 + 0.5).astype(np.uint8), "RGB").save(os.path.join(PREV, name))


def grid(tiles, gap=12, bg="#808080", zoom=1):
    """Rows of float RGB tiles -> one sheet (optionally nearest-neighbour zoomed)."""
    rows = []
    for row in tiles:
        if zoom > 1:
            row = [np.repeat(np.repeat(t, zoom, 0), zoom, 1) for t in row]
        h = max(t.shape[0] for t in row)
        w = sum(t.shape[1] for t in row) + gap * (len(row) + 1)
        r = solid(w, h + gap, bg)
        x = gap
        for t in row:
            r[gap:gap + t.shape[0], x:x + t.shape[1]] = t
            x += t.shape[1] + gap
        rows.append(r)
    W = max(r.shape[1] for r in rows)
    out = solid(W, sum(r.shape[0] for r in rows) + gap, bg)
    y = 0
    for r in rows:
        out[y:y + r.shape[0], :r.shape[1]] = r
        y += r.shape[0]
    return out


def grey(a):
    g = a @ np.array([0.2126, 0.7152, 0.0722], np.float32)
    return np.repeat(g[..., None], 3, axis=2)


def previews(logo, word, small, lib, mark, icon, tight, small_m, rb=None):
    for name, col in (("preview_dark.png", "#1b2838"), ("preview_light.png", "#f2f2f2")):
        save_rgb(centred(solid(1600, 900, col), fit(logo, 1280)), name)
    game = load_rgb(PROBE)
    lg = fit(logo, 0.50 * 1920)
    save_rgb(comp(game, lg, (1920 - lg.shape[1]) // 2 + 60, int(0.38 * 1080)), "preview_game.png")
    rug = load_rgb(RUG_ART)
    save_rgb(comp(rug, fit(logo, 0.55 * 1920), int(0.225 * 1920), int(0.30 * 1080)), "preview_rugart.png")
    save_rgb(centred(load_rgb(RUG_ART, (0, 0, 1920, 1080), (1280, 720), 0, 0.75), lib), "preview_library_1280x720.png")
    # capsule legibility: dedicated small render; backgrounds = game frame, red rug art, Steam dark
    tiles = {}
    for w, h in ((462, 174), (231, 87)):
        bgs = [load_rgb(PROBE, (420, 300, 1500, 300 + int(1080 * h / w)), (w, h), 5 * w / 462, 0.55),
               load_rgb(RUG_ART, (560, 330, 1360, 330 + int(800 * h / w)), (w, h)),
               solid(w, h, "#1b2838")]
        sm = fit(small, 0.88 * w)
        if sm.shape[0] > 0.86 * h:
            sm = fit(small, height=0.86 * h)
        tiles[w] = [centred(b, sm) for b in bgs]
        for b, t in zip(("game", "rug", "dark"), tiles[w]):
            save_rgb(t, f"capsule_{w}x{h}_{b}.png")
    save_rgb(grid([tiles[462], tiles[231] + [grey(t) for t in tiles[231]]]), "capsules_sheet.png")
    save_rgb(grid([tiles[231] + [grey(tiles[231][1])]], zoom=3), "capsules_231_zoom3.png")
    # header capsule with the full lockup ~300 px wide (store header shows at 460x215)
    hdr = load_rgb(RUG_ART, (380, 120, 1540, 120 + int(1160 * 215 / 460)), (460, 215))
    save_rgb(grid([[centred(hdr, fit(logo, 300)), centred(solid(460, 215, "#1b2838"), fit(logo, 300))]]),
             "header_460x215_lockup300.png")
    # icon mark at store / community sizes: 184 = the mark, 64 = tight tile, 48 / 32 = small tile (no sparkle)
    src = {184: mark, 64: tight, 48: small_m, 32: small_m}
    rows = []
    for col in ("#1b2838", "#f2f2f2", "#c4452c"):
        row = []
        for s in (184, 64, 48, 32):
            m = resize_prem(src[s], (s, s), Image.LANCZOS)
            Image.fromarray((np.clip(centred(solid(s, s, col), m), 0, 1) * 255 + .5).astype(np.uint8)).save(
                os.path.join(PREV, f"mark_{s}_{col[1:]}.png"))
            row.append(centred(solid(s + 16, s + 16, col), m))
        rows.append(row)
    save_rgb(grid(rows), "mark_sizes.png")
    save_rgb(grid([[r[3] for r in rows] + [r[2] for r in rows]], zoom=4), "mark_32_48_zoom4.png")
    # shipped icons read back from disk: App Icon JPG, then every .ico frame over Steam dark and light grey
    jpg = load_rgb(os.path.join(OUT, "refluff_app_icon_184.jpg"))
    ico = Image.open(os.path.join(OUT, "refluff_shortcut_icon.ico"))
    fr = {}
    for n in (256, 64, 48, 32, 24, 16):
        ico.size = (n, n)
        fr[n] = from_image(ico.convert("RGBA"))
    ic_rows = []
    for col in ("#1b2838", "#f2f2f2"):
        ic_rows.append([centred(solid(n, n, col), fr[n]) for n in (256, 64, 48, 32, 24, 16)])
    save_rgb(grid([[jpg] + ic_rows[0], ic_rows[1]], bg="#808080", gap=16), "icon_sizes.png")
    save_rgb(grid([r[1:] for r in ic_rows], bg="#808080", gap=4, zoom=5), "icon_small_zoom5.png")
    # alpha-edge fringe check: 400 % crops over black and white (plate edge top-left of R, runner end)
    a = logo[..., 3]
    ys, xs = np.nonzero(a > 0.5)
    y_mid = int(np.percentile(ys, 20))
    x_l = xs[ys == y_mid].min()
    crops = []
    for (cy_, cx_) in ((y_mid, x_l), (int(np.percentile(ys, 97)), int(np.percentile(xs, 12)))):
        c = logo[max(0, cy_ - 50):cy_ + 50, max(0, cx_ - 60):cx_ + 60]
        crops += [centred(solid(c.shape[1], c.shape[0], "#000000"), c),
                  centred(solid(c.shape[1], c.shape[0], "#ffffff"), c)]
    save_rgb(grid([crops[:2], crops[2:]], zoom=4), "fringe_check_400.png")
    # the same 400 % check on the new edges: backing join at the runner corner, the ledge end past the right
    # fringe, and the left tassel tips (continuous tapered strokes)
    if rb:
        x0, y0, x1, y1 = rb
        RH = y1 - y0
        crops = []
        for cx_, cy_ in ((x1 - 0.05 * RH, y0 - 0.10 * RH), (x1 + 0.75 * RH, y0 + 0.02 * RH),
                         (x0 - 0.55 * RH, y0 + 0.45 * RH)):
            cx_, cy_ = int(round(cx_)), int(round(cy_))
            c = logo[max(0, cy_ - 40):cy_ + 40, max(0, cx_ - 55):cx_ + 55]
            crops += [centred(solid(c.shape[1], c.shape[0], "#000000"), c),
                      centred(solid(c.shape[1], c.shape[0], "#ffffff"), c)]
        save_rgb(grid([crops[0::2], crops[1::2]], zoom=4), "backing_fringe_check_400.png")


def pocket_report(prem, name, fringe=None, seal_px=None):
    """Regression gate: transparent pockets enclosed by the silhouette (after sealing gaps narrower than
    2 x seal_px). A die-cut lockup must have none. fringe = (x_left_end, x_right_end, y_ledge): holes lying
    wholly outside the runner's ends and below the backing ledge are the see-through gaps between tassel
    bundles (by design) and are counted apart."""
    a = prem[..., 3] > 0.5
    r = seal_px or max(3, int(round(0.004 * a.shape[1])))
    yy, xx = np.mgrid[-r:r + 1, -r:r + 1]
    pad = r + 2
    cl = ndi.binary_closing(np.pad(a, pad), structure=(xx * xx + yy * yy) <= r * r)[pad:-pad, pad:-pad]
    holes = ndi.binary_fill_holes(cl) & ~a
    lab, n = ndi.label(holes)
    big, gaps = [], 0
    for i, sl in enumerate(ndi.find_objects(lab), 1):
        area = int((lab[sl] == i).sum())
        if area < 4 * r * r:
            continue
        if fringe and sl[0].start >= fringe[2] and (sl[1].stop <= fringe[0] or sl[1].start >= fringe[1]):
            gaps += 1
            continue
        big.append((area, sl[1].start, sl[1].stop, sl[0].start, sl[0].stop))
    print(f"  pockets in {name}: {len(big)}  (fringe gaps between tassel bundles: {gaps})")
    for b in big:
        print(f"    area {b[0]} px at x {b[1]}-{b[2]}, y {b[3]}-{b[4]}")
    return big


RUNNER_BOX = {}              # runner body corners in lockup px, filled by lockup() for the corner check


def corner_check(logo):
    """400 % crops of the four runner corners over black and white: whole corner dots, no slivers."""
    x0, y0, x1, y1 = RUNNER_BOX["box"]
    tiles = []
    for cx_, cy_ in ((x0, y0), (x1, y0), (x0, y1), (x1, y1)):
        cx_, cy_ = int(round(cx_)), int(round(cy_))
        c = logo[max(0, cy_ - 30):cy_ + 30, max(0, cx_ - 30):cx_ + 30]
        tiles += [centred(solid(c.shape[1], c.shape[0], "#000000"), c), centred(solid(c.shape[1], c.shape[0], "#ffffff"), c)]
    save_rgb(grid([tiles[:4], tiles[4:]], zoom=4), "runner_corners_400.png")


def save_png(prem, name):
    path = os.path.join(OUT, name)
    to_image(prem).save(path, optimize=True)
    print(f"  wrote {path}  {prem.shape[1]}x{prem.shape[0]}")


def main():
    os.makedirs(OUT, exist_ok=True)
    os.makedirs(PREV, exist_ok=True)
    t0 = time.time()
    word, info = render_wordmark(P_MASTER)
    size = info["size"]
    sub, box = render_runner(size)
    pad = 0.02 * size
    lk = lockup(word, info, sub, box)
    corner_check(lk)
    logo, (ody, odx) = with_shadow(lk, size, pad, ret_off=True)
    rx0, ry0, rx1, ry1 = RUNNER_BOX["box"]
    fz = (rx0 + odx, rx1 + odx, ry0 + ody + LEDGE * (ry1 - ry0) - 2)     # fringe zone in logo px
    del lk
    wordmark = with_shadow(word, size, pad)
    del word, sub
    save_png(logo, "refluff_logo.png")
    save_png(wordmark, "refluff_wordmark.png")
    sw, sinfo = render_wordmark(P_SMALL, seed=41)
    small = with_shadow(sw, sinfo["size"], 0.03 * sinfo["size"])
    save_png(small, "refluff_wordmark_small.png")
    L = int(round(1280 * SCALE)), int(round(720 * SCALE))
    edge = max(1, int(round(3 * SCALE)))                             # lockup spans the width minus 3 px
    tl, (tdy, tdx) = crop_pad(logo, 0, ret_off=True)
    lf = fit(tl, L[0] - 2 * edge)
    if lf.shape[0] > L[1] - 2 * edge:
        lf = fit(tl, height=L[1] - 2 * edge)
    lib = np.zeros((L[1], L[0], 4), np.float32)
    y, x = (L[1] - lf.shape[0]) // 2, (L[0] - lf.shape[1]) // 2
    lib[y:y + lf.shape[0], x:x + lf.shape[1]] = lf
    k = lf.shape[1] / tl.shape[1]
    fz_lib = ((fz[0] + tdx) * k + x, (fz[1] + tdx) * k + x, (fz[2] + tdy) * k + y)
    save_png(lib, "refluff_logo_1280x720.png")
    mark = render_mark(P_MARK)
    save_png(mark, "refluff_mark_1024.png")
    tight = render_mark(P_MARK_TIGHT, n_final=512, seed=31)          # 64 px: sparkle, trimmed padding
    small_m = render_mark(P_MARK_SMALL, n_final=512, seed=35)        # <= 48 px: no sparkle, fills ~95 %
    for n, src in ((64, tight), (48, small_m), (32, small_m)):
        save_png(resize_prem(src, (n, n)), f"refluff_mark_{n}.png")
    icon = render_mark(P_ICON, seed=33)                             # opaque, edge to edge
    assert icon[..., 3].min() > 0.999, "app icon must be fully opaque"
    icon184 = resize_prem(icon, (184, 184))
    Image.fromarray((np.clip(icon184[..., :3], 0, 1) * 255 + 0.5).astype(np.uint8), "RGB").save(
        os.path.join(OUT, "refluff_app_icon_184.jpg"), quality=95, subsampling=0)
    print(f"  wrote {os.path.join(OUT, 'refluff_app_icon_184.jpg')}  184x184")
    # shortcut icon (.ico): the rounded die-cut tile with transparent corners, as in refluff_mark_1024.
    # 256 = the mark itself (sparkle, soft shadow); 64 = the tight tile with its sparkle; 48..16 = the small tile
    # (no sparkle, no drop shadow, fills the frame).
    sizes = (256, 64, 48, 32, 24, 16)
    srcs = {256: mark, 64: tight}
    frames = [to_image(resize_prem(srcs.get(n, small_m), (n, n))) for n in sizes]
    frames[0].save(os.path.join(OUT, "refluff_shortcut_icon.ico"), format="ICO", sizes=[(n, n) for n in sizes],
                   append_images=frames[1:])
    ico = Image.open(os.path.join(OUT, "refluff_shortcut_icon.ico"))
    for n in sizes:                                                 # gate: every frame is RGBA with clear corners
        ico.size = (n, n)
        fr = np.asarray(ico.convert("RGBA"))
        assert fr.shape[:2] == (n, n) and fr[0, 0, 3] == 0 and fr[-1, -1, 3] == 0, f"ico frame {n}: opaque corner"
    print(f"  wrote {os.path.join(OUT, 'refluff_shortcut_icon.ico')}  {sizes} (transparent corners checked)")
    assert not pocket_report(logo, "refluff_logo", fz), "refluff_logo: enclosed transparent pocket"
    assert not pocket_report(lib, "refluff_logo_1280x720", fz_lib), "1280x720: enclosed transparent pocket"
    previews(logo, wordmark, small, lib, mark, icon, tight, small_m, (rx0 + odx, ry0 + ody, rx1 + odx, ry1 + ody))
    print(f"done in {time.time() - t0:.0f}s -> {OUT}")


if __name__ == "__main__":
    main()
