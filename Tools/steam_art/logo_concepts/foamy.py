"""Refluff logo - concept "Clean & Foamy".

Chunky rounded wordmark (Fredoka Bold, puffed up by a round dilation) with a mint -> teal
pillow-shaded fill, glossy rim highlight, dark teal extrusion + thick outline, glossy
pink-violet foam bubbles clinging to the letters, one sparkle and a dark pill subtitle.

Everything is rendered at SS x resolution into a premultiplied float RGBA buffer and
downsampled with LANCZOS, then unpremultiplied (no dark / white fringes).
Deterministic: fixed seeds, no time-dependent input.

usage: python foamy.py            -> writes master/wordmark/previews into OUT
"""
import math
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont
from scipy import ndimage as ndi

OUT = "C:/UnityProjects/CarpetCleaning/SteamStore/_work/logo/foamy/"
FONTS = "C:/UnityProjects/CarpetCleaning/Tools/steam_art/fonts/"
PROBE = "C:/UnityProjects/CarpetCleaning/SteamStore/_work/probe_1080.png"

SS = 3                 # supersampling factor
TARGET_W = 2400        # master width (px)
FS1 = 640              # wordmark font size at 1x (before the final fit)
FS = FS1 * SS          # font size at render resolution

WORD = "Refluff"
SUB = "CARPET CLEANING ASMR"


def hexc(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], np.float32) / 255.0


PAL = {
    "outline": "#0e2c38",   # thick outer outline, pill
    "shadow": "#041820",
    "extr_top": "#0d6f70",  # extrusion (3D depth)
    "extr_bot": "#08434d",
    "fill_top": "#c4fcec",  # mint
    "fill_mid": "#3fd6bf",
    "fill_hud": "#14b1a0",  # HUD teal
    "fill_bot": "#0b8a86",
    "rim": "#e9fff9",       # thin light ring just inside the outline
    "foam_hi": "#fbd3f6",
    "suds_top": "#f7e1f8",
    "suds_bot": "#d9a6e6",
    "foam_lit": "#e07ad0",
    "foam_mid": "#c65bd8",
    "foam_dk": "#8a37b4",
    "foam_line": "#6e2b8e",
    "spark": "#fffbea",
    "spark_glow": "#fff1b8",
    "sub_text": "#ffffff",
    "pill_edge": "#14b1a0",
}
C = {k: hexc(v) for k, v in PAL.items()}


def font(path, size, axes=None):
    f = ImageFont.truetype(FONTS + path, size)
    if axes:
        vals = []
        for a in f.get_variation_axes():
            n = a["name"].decode() if isinstance(a["name"], bytes) else a["name"]
            vals.append(axes.get(n, a["default"]))
        f.set_variation_by_axes(vals)
    return f


# ----------------------------------------------------------------------------- field helpers
def sdf(mask):
    """Signed distance (px, render res) of a bool mask: <0 inside, >0 outside."""
    dout = ndi.distance_transform_edt(~mask)
    din = ndi.distance_transform_edt(mask)
    return (dout - din).astype(np.float32)


def cov(sd, r=0.0):
    """Anti-aliased coverage of the offset shape {sd < r}."""
    return np.clip(r - sd + 0.5, 0.0, 1.0).astype(np.float32)


def smoothstep(a, b, x):
    t = np.clip((x - a) / (b - a), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def lerp3(c0, c1, t):
    t = t[..., None] if np.ndim(t) else t
    return c0 * (1.0 - t) + c1 * t


def blur(a, sigma):
    return ndi.gaussian_filter(a, sigma, mode="constant").astype(np.float32)


class Buf:
    """Premultiplied float RGBA buffer with an `over` operator."""

    def __init__(self, h, w):
        self.p = np.zeros((h, w, 4), np.float32)

    def over(self, rgb, a, y0=0, x0=0):
        a = np.clip(a, 0.0, 1.0).astype(np.float32)
        h, w = a.shape
        p = self.p[y0:y0 + h, x0:x0 + w]
        rgb = np.asarray(rgb, np.float32)
        inv = (1.0 - a)[..., None]
        p[..., :3] = rgb * a[..., None] + p[..., :3] * inv
        p[..., 3] = a + p[..., 3] * (1.0 - a)


# ----------------------------------------------------------------------------- layout
WORD_FONT = ("Fredoka[wdth,wght].ttf", {"Weight": 700, "Width": 106})
SUB_FONT = ("Fredoka[wdth,wght].ttf", {"Weight": 700, "Width": 112})
PUFF = 0.020           # round dilation of each glyph (x FS): chunkier, softer letters
TRACK = -0.012         # extra tracking (x FS) after the puff
ARC = 0.035            # centre of the word rises by this (x FS)
ROT = [-5.0, 3.0, -2.0, 3.5, -2.5, 2.0, 5.0]           # per-letter tilt, degrees
BOUNCE = [0.000, -0.012, 0.008, -0.010, 0.012, -0.004, 0.006]  # per-letter y (x FS)


def layout_word():
    """Place each glyph; returns list of dicts {sd, x0, y0} in layout coords (px, render res)."""
    f = font(WORD_FONT[0], FS, WORD_FONT[1])
    M = int(0.32 * FS)
    P = PUFF * FS
    glyphs = []
    pen = 0.0
    for i, ch in enumerate(WORD):
        l, t, r, b = f.getbbox(ch, anchor="ls")
        w, h = r - l + 2 * M, b - t + 2 * M
        im = Image.new("L", (w, h), 0)
        ImageDraw.Draw(im).text((M - l, M - t), ch, font=f, fill=255, anchor="ls")
        cx, cy = M + (r - l) / 2.0, M + (b - t) / 2.0
        im = im.rotate(ROT[i], resample=Image.BICUBIC, center=(cx, cy))
        mask = np.asarray(im) > 127
        sd = sdf(mask) - P                       # puffed glyph
        glyphs.append(dict(ch=ch, sd=sd, l=l, t=t, r=r, b=b, M=M, pen=pen))
        pen += f.getlength(ch) + TRACK * FS + 2 * P * 0.55
    total = pen
    for i, g in enumerate(glyphs):
        cxg = g["pen"] + (g["l"] + g["r"]) / 2.0
        u = cxg / total
        dy = -ARC * FS * (1.0 - (2.0 * u - 1.0) ** 2) + BOUNCE[i] * FS
        g["x0"] = int(round(g["pen"] + g["l"] - g["M"]))
        g["y0"] = int(round(g["t"] - g["M"] + dy))
        g["dy"] = dy
    return glyphs, total


def paste_min(dst, src, x0, y0):
    h, w = src.shape
    sl = dst[y0:y0 + h, x0:x0 + w]
    np.minimum(sl, src, out=sl)


# ----------------------------------------------------------------------------- decoration specs
OL = 0.068             # thick outer outline (x FS)
RING = 0.016           # thin light ring around each letter face (x FS)
SEP = 0.020            # dark separation line drawn with each letter (x FS)
EXD = (0.16, 1.0)      # extrusion direction (screen, y down)
EXL = 0.070            # extrusion length (x FS)
FLUFF_AMP = 0.000      # plush bumps on the outer outline (x FS), 0 = smooth
FLUFF_SIG = 0.010      # bump size (x FS)
BUB_LW = 0.009         # dark purple line around every bubble (x FS)
# foam heaps clinging to letters: (letter, u, v, [(dx, dy, r) x FS]); u/v = anchor in that letter's ink box
HEAPS = [
    (0, 0.20, 0.06, [(0.00, 0.00, 0.100), (-0.14, 0.05, 0.072), (0.145, 0.035, 0.078), (0.055, -0.115, 0.064),
                     (-0.085, -0.085, 0.054), (0.255, 0.075, 0.050), (-0.225, 0.115, 0.044), (0.205, -0.065, 0.040),
                     (-0.03, 0.115, 0.050), (0.105, 0.125, 0.040), (-0.175, -0.020, 0.036), (0.125, -0.175, 0.026),
                     (-0.285, 0.070, 0.022), (0.305, 0.010, 0.020), (0.040, 0.175, 0.020), (-0.130, 0.160, 0.018)]),
    (6, 0.62, 0.95, [(0.00, 0.00, 0.078), (0.125, -0.030, 0.060), (-0.120, 0.025, 0.054), (0.215, 0.035, 0.040),
                     (0.050, -0.095, 0.044), (-0.205, 0.055, 0.034), (0.075, 0.075, 0.040),
                     (0.265, -0.030, 0.020), (-0.060, -0.075, 0.022), (-0.265, 0.020, 0.017)]),
]
# free-floating specks (thin line only, not part of the outlined silhouette)
SPECKS = [(0, 0.78, -0.30, 0.028), (0, 0.98, -0.18, 0.018), (0, 0.62, -0.46, 0.014)]
SPARK = (6, 0.98, 0.04, 0.19)   # letter, u, v, size x FS
SUB_FS = 0.215         # subtitle font size (x FS)
SUB_TRACK = 0.10       # subtitle tracking (x its font size)
PILL_PADX = 0.62       # pill horizontal padding (x subtitle font size)
PILL_H = 1.30          # pill height (x subtitle font size)
PILL_DOWN = 0.55       # pill top sits this far down the extrusion (x extrusion length)


def ink_box(sd, x0, y0):
    ys, xs = np.nonzero(sd < 0)
    return x0 + xs.min(), y0 + ys.min(), x0 + xs.max() + 1, y0 + ys.max() + 1


def disc_sd(h, w, cx, cy, r):
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    return np.hypot(xx - cx, yy - cy) - r


def star_mask(size, s, pw=2.9):
    """Concave 4-point star (astroid-like), bool mask of a size x size crop."""
    t = np.linspace(0, 2 * np.pi, 720, endpoint=False)
    c, sn = np.cos(t), np.sin(t)
    x = np.sign(c) * np.abs(c) ** pw * s
    y = np.sign(sn) * np.abs(sn) ** pw * s * 1.08
    im = Image.new("L", (size, size), 0)
    ImageDraw.Draw(im).polygon(list(zip(size / 2 + x, size / 2 + y)), fill=255)
    return np.asarray(im) > 127


def subtitle_mask(f, track):
    """Render SUB with tracking; returns (bool mask, cap top offset, cap height)."""
    widths = [f.getlength(ch) for ch in SUB]
    total = sum(widths) + track * (len(SUB) - 1)
    asc, desc = f.getmetrics()
    im = Image.new("L", (int(total + 20), asc + desc + 20), 0)
    d = ImageDraw.Draw(im)
    x = 10.0
    for ch, w in zip(SUB, widths):
        d.text((x, 10 + asc), ch, font=f, fill=255, anchor="ls")
        x += w + track
    m = np.asarray(im) > 127
    ys, xs = np.nonzero(m)
    return m[ys.min():ys.max() + 1, xs.min():xs.max() + 1]


# ----------------------------------------------------------------------------- render
def low_blur(a, sigma, shift=(0, 0), q=4):
    """Gaussian blur of a big coverage map at 1/q resolution (shadows), with a shift (px)."""
    h, w = a.shape
    im = Image.fromarray(a.astype(np.float32), "F").resize((w // q, h // q), Image.BOX)
    s = ndi.gaussian_filter(np.asarray(im), sigma / q)
    s = ndi.shift(s, (shift[1] / q, shift[0] / q), order=1, mode="constant")
    return np.asarray(Image.fromarray(s.astype(np.float32), "F").resize((w, h), Image.BICUBIC))


def fluff_noise(h, w, seed=11):
    """Smooth unit-variance noise for the plush outline edge."""
    q = 4
    rng = np.random.default_rng(seed)
    n = rng.standard_normal((h // q + 2, w // q + 2)).astype(np.float32)
    n = ndi.gaussian_filter(n, FLUFF_SIG * FS / q, mode="wrap")
    n /= n.std() + 1e-6
    n = np.asarray(Image.fromarray(n, "F").resize((w, h), Image.BICUBIC))
    return np.clip(n, -2.2, 2.2)


def geometry(with_sub):
    glyphs, _ = layout_word()
    for g in glyphs:
        g["ink"] = ink_box(g["sd"], g["x0"], g["y0"])
    wl = min(g["ink"][0] for g in glyphs)
    wr = max(g["ink"][2] for g in glyphs)
    wt = min(g["ink"][1] for g in glyphs)
    base = np.mean([g["dy"] for g in glyphs])      # mean baseline (y of baseline = dy)
    wb = max(g["ink"][3] for g in glyphs)
    bubs, heaps = [], []
    for li, u, v, heap in HEAPS:
        x0, y0, x1, y1 = glyphs[li]["ink"]
        ax, ay = x0 + u * (x1 - x0), y0 + v * (y1 - y0)
        heaps.append([(ax + dx * FS, ay + dy * FS, r * FS) for dx, dy, r in heap])
        bubs += heaps[-1]
    specks = []
    for li, u, v, rr in SPECKS:
        x0, y0, x1, y1 = glyphs[li]["ink"]
        specks.append((x0 + u * (x1 - x0), y0 + v * (y1 - y0), rr * FS))
    li, u, v, ss = SPARK
    x0, y0, x1, y1 = glyphs[li]["ink"]
    spark = (x0 + u * (x1 - x0), y0 + v * (y1 - y0), ss * FS)
    pill = None
    if with_sub:
        fsz = int(SUB_FS * FS)
        f = font(SUB_FONT[0], fsz, SUB_FONT[1])
        sm = subtitle_mask(f, SUB_TRACK * fsz)
        pw = sm.shape[1] + 2 * PILL_PADX * fsz
        ph = PILL_H * fsz
        pill = dict(cx=(wl + wr) / 2.0, top=wb + (RING + EXL) * FS * PILL_DOWN, w=pw, h=ph, text=sm)
    # canvas bounds in layout coords
    pad = int(0.40 * FS)
    xs0 = [wl] + [b[0] - b[2] for b in bubs + specks] + [spark[0] - spark[2]]
    xs1 = [wr] + [b[0] + b[2] for b in bubs + specks] + [spark[0] + spark[2]]
    ys0 = [wt] + [b[1] - b[2] for b in bubs + specks] + [spark[1] - spark[2]]
    ys1 = [wb] + [b[1] + b[2] for b in bubs]
    if pill:
        xs0.append(pill["cx"] - pill["w"] / 2)
        xs1.append(pill["cx"] + pill["w"] / 2)
        ys1.append(pill["top"] + pill["h"])
    bx0, by0 = int(min(xs0)) - pad, int(min(ys0)) - pad
    bx1, by1 = int(max(xs1)) + pad, int(max(ys1)) + pad
    return dict(glyphs=glyphs, bubs=bubs, heaps=heaps, specks=specks, spark=spark, pill=pill,
                ox=-bx0, oy=-by0, W=bx1 - bx0, H=by1 - by0, wt=wt, wb=wb, base=base)


def pill_sd(h, w, cx, cy, pw, ph):
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    rad = ph / 2.0
    qx = np.abs(xx - cx) - (pw / 2.0 - rad)
    qy = np.abs(yy - cy) - (ph / 2.0 - rad)
    out = np.hypot(np.maximum(qx, 0), np.maximum(qy, 0))
    return out + np.minimum(np.maximum(qx, qy), 0) - rad


def extrusion(sd_plate):
    """Min-sweep of the letter plate along EXD; returns (sd, depth 0..1)."""
    H, W = sd_plate.shape
    L = EXL * FS
    n = max(2, int(L / 2))
    ext = sd_plate.copy()
    depth = np.zeros_like(sd_plate)
    for k in range(1, n + 1):
        t = k / n
        sy = int(round(t * L))
        sx = int(round(t * L * EXD[0]))
        s = np.full_like(sd_plate, 1e4)
        s[sy:, sx:] = sd_plate[:H - sy, :W - sx]
        better = s < ext
        ext[better] = s[better]
        depth[better] = t
    return ext, depth


LIGHT = np.array([-0.45, -0.72, 0.55], np.float32)
LIGHT /= np.linalg.norm(LIGHT)
HALF = LIGHT + np.array([0, 0, 1], np.float32)
HALF /= np.linalg.norm(HALF)


def face_gradient(Y, wt, wb):
    t = np.clip((Y - wt) / (wb - wt), 0.0, 1.0)
    stops = [(0.0, C["fill_top"]), (0.36, C["fill_mid"]), (0.70, C["fill_hud"]), (1.0, C["fill_bot"])]
    out = np.zeros(t.shape + (3,), np.float32)
    for (t0, c0), (t1, c1) in zip(stops[:-1], stops[1:]):
        m = (t >= t0) & (t <= t1)
        k = smoothstep(t0, t1, t[m])[..., None]
        out[m] = c0 * (1 - k) + c1 * k
    return out


def draw_letter(buf, g, G):
    sd = g["sd"]
    x0, y0 = g["x0"] + G["ox"], g["y0"] + G["oy"]
    h, w = sd.shape
    buf.over(C["outline"], cov(sd, (RING + SEP) * FS), y0, x0)       # separation from neighbours
    buf.over(C["rim"], cov(sd, RING * FS), y0, x0)                   # thin light ring
    a = cov(sd, 0.0)
    Y = (np.arange(h, dtype=np.float32) + y0 - G["oy"])[:, None] * np.ones((1, w), np.float32)
    col = face_gradient(Y, G["wt"], G["wb"])
    # pillow shading: rounded height field from the inside distance
    B = 0.085 * FS
    din = np.clip(-sd, 0, B) / B
    hgt = np.sqrt(1.0 - (1.0 - din) ** 2) * B * 0.9
    hgt = ndi.gaussian_filter(hgt, 0.004 * FS)
    gy, gx = np.gradient(hgt)
    nz = 1.0 / np.sqrt(gx * gx + gy * gy + 1.0)
    nx, ny = -gx * nz, -gy * nz
    ndl = (nx * LIGHT[0] + ny * LIGHT[1] + nz * LIGHT[2]) / LIGHT[2]
    shade = np.clip(ndl, 0.15, 1.7)
    col = col * (0.58 + 0.42 * shade[..., None])
    ndh = np.clip(nx * HALF[0] + ny * HALF[1] + nz * HALF[2], 0, 1)
    spec = ndh ** 70 * 0.55
    # glossy crescent along the upper-left inner edge
    e = cov(sd, -0.050 * FS)
    es = ndi.shift(e, (0.050 * FS, 0.030 * FS), order=1, mode="constant", cval=0.0)
    cres = blur(np.clip(e - es, 0, 1), 0.006 * FS)
    x_ink0, y_ink0, x_ink1, y_ink1 = g["ink"]
    vy = (Y - y_ink0) / max(1.0, (y_ink1 - y_ink0))
    cres *= 1.0 - 0.6 * smoothstep(0.35, 0.95, vy)
    hl = np.clip(spec + cres * 0.85, 0, 1)
    col = col * (1 - hl[..., None]) + hl[..., None]
    buf.over(np.clip(col, 0, 1), a, y0, x0)


def draw_suds(buf, bubs):
    """Pale foam mass under a heap: smooth union of its bubbles, so the gaps read as suds."""
    lw = BUB_LW * FS
    m = int(0.08 * FS)
    bubs = [b for b in bubs if b[2] >= 0.032 * FS]    # rim micro-bubbles sit on top, unwebbed
    if not bubs:
        return
    x0 = int(min(b[0] - b[2] for b in bubs)) - m
    y0 = int(min(b[1] - b[2] for b in bubs)) - m
    x1 = int(max(b[0] + b[2] for b in bubs)) + m
    y1 = int(max(b[1] + b[2] for b in bubs)) + m
    sd = None
    kk = 0.075 * FS                                   # polynomial smooth-min blend width
    for cx, cy, r in bubs:
        dd = disc_sd(y1 - y0, x1 - x0, cx - x0, cy - y0, r * 0.88)
        if sd is None:
            sd = dd
            continue
        h = np.clip(0.5 + 0.5 * (dd - sd) / kk, 0, 1)
        sd = dd * (1 - h) + sd * h - kk * h * (1 - h)
    buf.over(C["foam_line"], cov(sd, lw), y0, x0)
    yy = np.arange(y1 - y0, dtype=np.float32)[:, None] * np.ones((1, x1 - x0), np.float32)
    k = smoothstep(0.0, float(y1 - y0), yy)[..., None]
    buf.over(C["suds_top"] * (1 - k) + C["suds_bot"] * k, cov(sd, 0.0), y0, x0)


def draw_bubble(buf, cx, cy, r):
    """Cartoon soap bubble: pale centre, saturated rim, window highlight, inner reflection."""
    lw = BUB_LW * FS
    R = r + lw + 2
    x0, y0 = int(cx - R), int(cy - R)
    n = int(2 * R) + 2
    d = disc_sd(n, n, cx - x0, cy - y0, 0.0)
    buf.over(C["foam_line"], cov(d, r + lw), y0, x0)
    yy, xx = np.mgrid[0:n, 0:n].astype(np.float32)
    nx, ny = (xx - (cx - x0)) / r, (yy - (cy - y0)) / r
    nr = np.clip(np.hypot(nx, ny), 0, 1)
    nz = np.sqrt(1 - nr * nr)
    lit = np.clip(nx * LIGHT[0] + ny * LIGHT[1] + nz * LIGHT[2], 0, 1)
    k = (smoothstep(0.25, 1.0, nr) ** 1.1)[..., None]
    col = C["foam_hi"] * (1 - k) + C["foam_mid"] * k
    k2 = (smoothstep(0.80, 1.0, nr) * 0.55)[..., None]
    col = col * (1 - k2) + C["foam_dk"] * k2
    col = col * (0.80 + 0.28 * lit[..., None])
    # inner reflection crescent, lower right
    dirk = smoothstep(0.35, 0.85, (nx * 0.6 + ny * 0.8) / (nr + 1e-4))
    band = smoothstep(0.66, 0.78, nr) * (1 - smoothstep(0.86, 0.94, nr))
    cr = (dirk * band * 0.65)[..., None]
    col = col * (1 - cr) + C["foam_hi"] * cr
    # window highlight (ellipse along the upper-left arc) + small dot
    sx, sy = nx + 0.34, ny + 0.38
    u, v = (sx - sy) * 0.7071, (sx + sy) * 0.7071
    win = 1 - smoothstep(0.72, 1.0, np.hypot(u / 0.30, v / 0.15))
    dot = 1 - smoothstep(0.6, 1.0, np.hypot(nx + 0.66, ny + 0.02) / 0.075)
    hl = np.clip(win * 0.95 + dot * 0.9, 0, 1)[..., None]
    col = col * (1 - hl) + hl
    buf.over(np.clip(col, 0, 1), cov(d, r), y0, x0)


def draw_spark(buf, cx, cy, size):
    n = int(size * 1.3 + 0.45 * FS)
    x0, y0 = int(cx - n / 2), int(cy - n / 2)
    m = star_mask(n, size / 2.0)
    sd = sdf(m)
    glow = blur(cov(sd, 0.02 * FS), 0.05 * FS)
    buf.over(C["spark_glow"], glow * 0.55, y0, x0)
    buf.over(C["outline"], cov(sd, 0.024 * FS), y0, x0)
    yy, xx = np.mgrid[0:n, 0:n].astype(np.float32)
    rr = np.hypot(xx - n / 2, yy - n / 2) / (size / 2.0)
    k = smoothstep(0.05, 0.75, rr)[..., None]
    col = C["spark"] * (1 - k) + C["spark_glow"] * k
    buf.over(col, cov(sd, 0.0), y0, x0)


def render(with_sub=True):
    G = geometry(with_sub)
    H, W, ox, oy = G["H"], G["W"], G["ox"], G["oy"]
    print(f"  canvas {W}x{H} (render res)", flush=True)
    sd_word = np.full((H, W), 1e4, np.float32)
    for g in G["glyphs"]:
        paste_min(sd_word, g["sd"], g["x0"] + ox, g["y0"] + oy)
    ext, depth = extrusion(sd_word - RING * FS)
    sil = ext.copy()
    for cx, cy, r in G["bubs"]:
        R = int(r + (BUB_LW + OL + FLUFF_AMP * 3) * FS) + 4
        x0, y0 = int(cx + ox - R), int(cy + oy - R)
        d = disc_sd(2 * R, 2 * R, cx + ox - x0, cy + oy - y0, r + BUB_LW * FS)
        paste_min(sil, d, x0, y0)
    P = G["pill"]
    if P:
        pcy = P["top"] + P["h"] / 2 + oy
        psd = pill_sd(H, W, P["cx"] + ox, pcy, P["w"], P["h"])
        np.minimum(sil, psd, out=sil)
        btop = G["base"] + oy - 0.18 * FS            # dark bridge: no holes between word and pill
        bsd = pill_sd(H, W, P["cx"] + ox, (btop + pcy) / 2, P["w"] - 0.5 * P["h"], pcy - btop)
        np.minimum(sil, bsd, out=sil)
        del bsd
    if FLUFF_AMP > 0:
        sil = sil - FLUFF_AMP * FS * fluff_noise(H, W)
    outline = cov(sil, OL * FS)
    del sil
    buf = Buf(H, W)
    sh = low_blur(outline, 0.040 * FS, (0.020 * FS, 0.060 * FS))
    buf.over(C["shadow"], sh * 0.55)
    sh2 = low_blur(outline, 0.010 * FS, (0.008 * FS, 0.024 * FS))
    buf.over(C["shadow"], sh2 * 0.35)
    buf.over(C["outline"], outline)
    if P:
        inset, lw = 0.11 * P["h"], 0.035 * P["h"]
        ring = cov(psd, -inset) - cov(psd, -inset - lw)
        buf.over(C["pill_edge"], ring * 0.95)
        top = cov(psd, -inset - lw) * (1 - smoothstep(pcy - P["h"] * 0.35, pcy + P["h"] * 0.05,
                                                        np.arange(H, dtype=np.float32)[:, None]))
        buf.over(hexc("#2b6f7a"), top * 0.35)
        del psd
        tm = P["text"].astype(np.float32)
        th, tw = tm.shape
        tx0 = int(P["cx"] + ox - tw / 2)
        ty0 = int(pcy - th / 2)
        buf.over(C["sub_text"], tm, ty0, tx0)
    buf.over(lerp3(C["extr_top"], C["extr_bot"], depth), cov(ext, 0.0))
    del ext, depth
    for g in G["glyphs"]:
        draw_letter(buf, g, G)
    for heap in G["heaps"]:
        hb = [(cx + ox, cy + oy, r) for cx, cy, r in heap]
        draw_suds(buf, hb)
        for cx, cy, r in sorted(hb, key=lambda b: b[1]):
            draw_bubble(buf, cx, cy, r)
    for cx, cy, r in G["specks"]:
        draw_bubble(buf, cx + ox, cy + oy, r)
    sx, sy, ss = G["spark"]
    draw_spark(buf, sx + ox, sy + oy, ss)
    return buf.p


def finish(p, pad=0.04, width=TARGET_W):
    """Tight crop + padding, LANCZOS downsample in premultiplied space, unpremultiply."""
    a = p[..., 3]
    ys, xs = np.nonzero(a > 1.0 / 512)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    pw = int((x1 - x0) * pad)
    x0, x1, y0, y1 = x0 - pw, x1 + pw, y0 - pw, y1 + pw
    H, W = a.shape
    crop = np.zeros((y1 - y0, x1 - x0, 4), np.float32)
    sy0, sx0 = max(0, y0), max(0, x0)
    crop[sy0 - y0:min(H, y1) - y0, sx0 - x0:min(W, x1) - x0] = p[sy0:min(H, y1), sx0:min(W, x1)]
    ow = width
    oh = int(round(crop.shape[0] * ow / crop.shape[1]))
    ch = [np.asarray(Image.fromarray(np.ascontiguousarray(crop[..., i]), "F").resize((ow, oh), Image.LANCZOS))
          for i in range(4)]
    al = np.clip(ch[3], 0, 1)
    rgb = np.stack([np.clip(c, 0, None) for c in ch[:3]], -1)
    rgb = np.minimum(rgb, al[..., None])
    out = np.where(al[..., None] > 1e-4, rgb / np.maximum(al[..., None], 1e-4), 0)
    rgba = (np.dstack([np.clip(out, 0, 1), al]) * 255 + 0.5).astype(np.uint8)
    rgba[rgba[..., 3] == 0, :3] = 0                      # clean fully transparent pixels
    return Image.fromarray(rgba, "RGBA")


# ----------------------------------------------------------------------------- previews
def fit(logo, w=None, h=None):
    """Premultiplied-correct LANCZOS resize of an RGBA logo to a width (or height)."""
    if w is None:
        w = int(round(logo.width * h / logo.height))
    if h is None:
        h = int(round(logo.height * w / logo.width))
    return logo.convert("RGBa").resize((w, h), Image.LANCZOS).convert("RGBA")


def comp(bg, logo, x, y):
    bg = bg.convert("RGBA")
    layer = Image.new("RGBA", bg.size, (0, 0, 0, 0))
    layer.paste(logo, (int(x), int(y)))
    return Image.alpha_composite(bg, layer).convert("RGB")


def previews(master, word):
    for name, colr in (("preview_dark.png", (0x1b, 0x28, 0x38)), ("preview_light.png", (0xf2, 0xf2, 0xf2))):
        bg = Image.new("RGB", (1600, 900), colr)
        lg = fit(master, w=1240)
        if lg.height > 760:
            lg = fit(master, h=760)
        comp(bg, lg, (1600 - lg.width) / 2, (900 - lg.height) / 2).save(OUT + name)
    probe = Image.open(PROBE).convert("RGB")
    lg = fit(master, w=int(1920 * 0.55))
    comp(probe, lg, (1920 - lg.width) / 2, 1080 * 0.06).save(OUT + "preview_game.png")
    # small capsule legibility card: blurred + darkened rug crop
    crop = probe.crop((560, 360, 1360, 661)).resize((462, 174), Image.LANCZOS)
    crop = crop.filter(ImageFilter.GaussianBlur(5))
    crop = Image.eval(crop, lambda v: int(v * 0.62))
    lg = fit(word, w=int(462 * 0.85))
    if lg.height > 174 * 0.92:
        lg = fit(word, h=int(174 * 0.92))
    card = comp(crop, lg, (462 - lg.width) / 2, (174 - lg.height) / 2)
    card.save(OUT + "preview_small.png")
    card.resize((231, 87), Image.LANCZOS).save(OUT + "preview_tiny.png")


def main():
    import time
    os.makedirs(OUT, exist_ok=True)
    t = time.time()
    print("master ...", flush=True)
    master = finish(render(True))
    master.save(OUT + "master.png", optimize=True)
    print(f"  {master.size} {time.time() - t:.1f}s", flush=True)
    print("wordmark ...", flush=True)
    word = finish(render(False))
    word.save(OUT + "wordmark.png", optimize=True)
    print(f"  {word.size} {time.time() - t:.1f}s", flush=True)
    previews(master, word)
    print("done", flush=True)


if __name__ == "__main__":
    main()
