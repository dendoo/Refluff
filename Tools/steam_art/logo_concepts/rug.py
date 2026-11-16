# -*- coding: utf-8 -*-
"""Refluff logo - concept "Heirloom Pile" (direction RUG HERITAGE).

The wordmark is treated like a hand-knotted rug: madder-red field with a cream inner
border line, a cream outer border band, a deep indigo guard with a short tufted pile
fringe on the outer silhouette only (counters stay clean), and the subtitle woven into
a small indigo/teal runner with warp-thread tassels and lozenge medallions.

Deterministic (fixed seeds). Renders at SS x supersampling, downsamples premultiplied
RGBA with LANCZOS.

usage: python rug.py [out_dir]
"""
import os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter
from scipy import ndimage as ndi

HERE = os.path.dirname(os.path.abspath(__file__))
FONTS = os.path.normpath(os.path.join(HERE, "..", "fonts"))
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.normpath(
    os.path.join(HERE, "..", "..", "..", "SteamStore", "_work", "logo", "rug"))
PROBE = os.path.normpath(os.path.join(HERE, "..", "..", "..", "SteamStore", "_work", "probe_1080.png"))

SS = 3                      # supersampling factor
WORD_FONT = "TitanOne-Regular.ttf"
SUB_FONT = "LilitaOne-Regular.ttf"
WORD = "Refluff"
SUB = "CARPET CLEANING ASMR"
WORD_SIZE = 506             # final px font size of the wordmark

def hx(s):
    s = s.lstrip("#")
    return np.array([int(s[i:i + 2], 16) for i in (0, 2, 4)], np.float32) / 255.0

PAL = {
    "madder_hi": hx("#d9553a"),   # sun-faded top of the red field
    "madder":    hx("#b8322b"),   # madder red field
    "madder_lo": hx("#83201f"),   # deep madder (bottom / shade)
    "cream":     hx("#f5e6c4"),   # undyed wool
    "cream_lo":  hx("#d9c095"),
    "teal":      hx("#1f6f6c"),   # deep teal guard stripe
    "teal_lo":   hx("#154b4c"),
    "indigo":    hx("#1d2547"),   # indigo guard / outline
    "indigo_hi": hx("#4a5a92"),
    "indigo_lo": hx("#10142a"),
    "shadow":    hx("#0a0d1a"),
    "gold":      hx("#e9a63a"),   # tassel wrap accent
}

# ----------------------------------------------------------------------------- helpers
def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)

def _hash(ix, iy, seed):
    h = (ix.astype(np.int64) * 374761393 + iy.astype(np.int64) * 668265263
         + seed * 1442695041) & 0xFFFFFFFF
    h = ((h ^ (h >> 13)) * 1274126177) & 0xFFFFFFFF
    h = h ^ (h >> 16)
    return (h & 0xFFFFFF).astype(np.float32) / float(0xFFFFFF)

def valnoise(x, y, seed=0):
    """Smooth value noise, x/y float arrays in lattice units, returns 0..1."""
    x0 = np.floor(x); y0 = np.floor(y)
    fx = (x - x0).astype(np.float32); fy = (y - y0).astype(np.float32)
    fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy)
    ix = x0.astype(np.int64); iy = y0.astype(np.int64)
    a = _hash(ix, iy, seed); b = _hash(ix + 1, iy, seed)
    c = _hash(ix, iy + 1, seed); d = _hash(ix + 1, iy + 1, seed)
    return (a + (b - a) * fx) * (1 - fy) + (c + (d - c) * fx) * fy

def fbm(x, y, seed=0, octaves=3):
    s = 0.0; amp = 0.5; tot = 0.0
    for o in range(octaves):
        s = s + amp * valnoise(x * (2 ** o), y * (2 ** o), seed + 17 * o)
        tot += amp; amp *= 0.5
    return s / tot

def signed_dist(mask):
    """Signed distance (px) of a boolean mask: negative inside, positive outside."""
    din = ndi.distance_transform_edt(mask).astype(np.float32)
    dout = ndi.distance_transform_edt(~mask).astype(np.float32)
    return np.where(mask, -(din - 0.5), dout - 0.5)

def mix(a, b, t):
    t = np.asarray(t, np.float32)
    return a + (b - a) * t[..., None] if t.ndim else a + (b - a) * t

class Canvas:
    """Premultiplied float RGBA canvas."""
    def __init__(self, h, w):
        self.rgb = np.zeros((h, w, 3), np.float32)
        self.a = np.zeros((h, w), np.float32)
    def over(self, col, alpha, y0=0, x0=0):
        """col: (h,w,3) or (3,) straight colour, alpha: (h,w)."""
        h, w = alpha.shape
        sl = (slice(y0, y0 + h), slice(x0, x0 + w))
        al = np.clip(alpha, 0, 1).astype(np.float32)
        c = np.broadcast_to(col, (h, w, 3))
        self.rgb[sl] = c * al[..., None] + self.rgb[sl] * (1 - al[..., None])
        self.a[sl] = al + self.a[sl] * (1 - al)

def extrude_down(a, depth_px):
    """Union of `a` shifted down by 0..depth_px (thickness of the rug)."""
    h = int(depth_px) // 2
    if h < 1:
        return a
    return ndi.maximum_filter1d(a, size=2 * h + 1, axis=0, origin=h, mode="constant")

# ------------------------------------------------------------------ surface textures
def knot_texture(X, Y, kw=5.2, kh=4.6, amp=0.13, jit=0.05, seed=5):
    """Hand-knotted pile: a brick grid of soft knot bumps + per-knot jitter (final px)."""
    row = np.floor(Y / kh)
    sx = X / kw + (row % 2) * 0.5
    col = np.floor(sx)
    fx = sx - col; fy = Y / kh - row
    b = np.sin(np.pi * fx) * np.sin(np.pi * fy)
    j = _hash(col.astype(np.int64), row.astype(np.int64), seed) - 0.5
    return (1.0 - amp * 0.7) + amp * np.power(np.clip(b, 0, 1), 0.8) + jit * j

def pile_streaks(X, Y, seed=9):
    """Fine vertical fibre streaks, 0-centred."""
    return valnoise(X / 1.5, Y / 7.0, seed) - 0.5

def abrash(X, Y, seed=31):
    """Antique dye banding: slow horizontal bands of slightly different red."""
    return fbm(X / 520.0, Y / 30.0, seed, 3) - 0.5

# ------------------------------------------------------------------ wordmark geometry
R_IN0, R_IN1 = 13.0, 18.5   # cream inner border line, inside the letters (final px)
R_CREAM = 16.0              # cream border band outside the letters
R_TEAL = 22.0               # thin deep-teal guard stripe
R_OUT = 42.0                # outer indigo guard edge (before the tufts)
TUFT = 11.0                 # max pile tuft length beyond R_OUT
DEPTH = 14.0                # rug thickness (extrusion down)

LETTER_ROT = [-3.0, 2.0, -1.0, 2.5, -2.0, 1.5, -2.5]      # degrees, per letter
LETTER_DY = [0.000, 0.018, -0.010, 0.006, 0.022, -0.004, 0.012]  # x font size
TRACK = 0.018                                             # extra tracking, em

def glyph_mask(text, size):
    """AA glyph coverage (uint8) at SS resolution, gently bounced letters."""
    f = ImageFont.truetype(os.path.join(FONTS, WORD_FONT), size)
    asc, desc = f.getmetrics()
    pad = int(size * 0.45)
    total = f.getlength(text) + TRACK * size * (len(text) - 1)
    W = int(total + 2 * pad); H = int(asc + desc + 2 * pad)
    img = Image.new("L", (W, H), 0)
    for i, ch in enumerate(text):
        x = f.getlength(text[:i]) + TRACK * size * i
        m = int(size * 0.3)
        li = Image.new("L", (int(f.getlength(ch) + 2 * m), int(asc + desc + 2 * m)), 0)
        ImageDraw.Draw(li).text((m, m), ch, font=f, fill=255)
        pivot = (m + f.getlength(ch) / 2, m + asc * 0.62)
        li = li.rotate(LETTER_ROT[i % len(LETTER_ROT)], resample=Image.BICUBIC, center=pivot)
        layer = Image.new("L", (W, H), 0)
        layer.paste(li, (int(pad + x - m), int(pad + LETTER_DY[i % len(LETTER_DY)] * size - m)))
        img = Image.fromarray(np.maximum(np.asarray(img), np.asarray(layer)))
    return np.asarray(img), f

def build_wordmark():
    """Returns dict with straight rgb, alpha (at SS), solid mask for shadows, cap box."""
    size = WORD_SIZE * SS
    G, f = glyph_mask(WORD, size)
    B = G >= 128
    ys, xs = np.nonzero(B)
    marg = int((R_OUT + TUFT + DEPTH + 10) * SS)
    y0, y1 = ys.min() - marg, ys.max() + marg
    x0, x1 = xs.min() - marg, xs.max() + marg
    pad_img = np.zeros((y1 - y0, x1 - x0), bool)
    sy0, sx0 = max(y0, 0), max(x0, 0)
    sub = B[sy0:y1, sx0:x1]
    pad_img[sy0 - y0:sy0 - y0 + sub.shape[0], sx0 - x0:sx0 - x0 + sub.shape[1]] = sub
    B = pad_img
    H, W = B.shape
    print("wordmark SS grid", W, H)
    Yi, Xi = np.mgrid[0:H, 0:W].astype(np.float32)
    X = Xi / SS; Y = Yi / SS                     # final-px coords
    sd = signed_dist(B) / SS
    wob = (valnoise(X / 2.2, Y / 2.2, 71) - 0.5) * 1.3   # woolly ring boundaries
    sdw = sd + wob * smoothstep(1.5, 4.0, np.abs(sd))    # keep the letter edge crisp
    # --- outer silhouette: indigo guard, counters filled, tufted outer edge only
    S0 = ndi.binary_fill_holes(sd <= R_OUT)
    E, (iy, ix) = ndi.distance_transform_edt(~S0, return_indices=True)
    E = E.astype(np.float32) / SS
    bx = ix.astype(np.float32) / SS; by = iy.astype(np.float32) / SS
    del ix, iy
    n1 = valnoise(bx / 8.5, by / 8.5, 11)          # soft tuft clumps
    n2 = valnoise(bx / 2.0, by / 2.0, 23)          # single fibres
    L = 2.0 + TUFT * smoothstep(0.2, 0.9, n1) + 1.6 * n2
    core = smoothstep(L + 1.0, L - 1.0, E)
    hair = valnoise(X / 1.1, Y / 1.1, 29)          # stray fibre halo just past the clumps
    halo = 0.45 * smoothstep(L + 3.5, L - 0.5, E) * smoothstep(0.45, 0.8, hair)
    tuft = np.maximum(core, halo) * (E > 0)
    tuft = np.clip(ndi.gaussian_filter(tuft, 0.45 * SS), 0, 1)   # soft wool, not torn paper
    holes = S0 & (sd > R_OUT)
    base = np.maximum(smoothstep(R_OUT + 0.9, R_OUT - 0.9, sd), holes.astype(np.float32))
    A = np.maximum(base, tuft)
    fib_t = np.clip(E / (L + 1e-3), 0, 1) * (E > 0)   # 0 at root, 1 at tip
    del S0, n1, n2, L, core, hair, halo, holes
    return dict(B=B, sd=sd, sdw=sdw, A=A, base=base, tuft=tuft, fib_t=fib_t, X=X, Y=Y, f=f,
                capbox=(ys.min() - y0, ys.max() - y0))

LIGHT = np.array([-0.45, -0.72, 0.55], np.float32)
LIGHT /= np.linalg.norm(LIGHT)

def pillow_shade(din, width, height, blur):
    """Lambert factor of a soft cushion that rises `height` px over `width` px from the edge."""
    h = smoothstep(0.0, width, din)
    h = ndi.gaussian_filter(h, blur * SS) * height
    gy, gx = np.gradient(h)
    gx *= SS; gy *= SS                              # slope per final px
    n = 1.0 / np.sqrt(gx * gx + gy * gy + 1.0)
    lam = (-gx * LIGHT[0] - gy * LIGHT[1] + LIGHT[2]) * n
    return lam - LIGHT[2]                           # 0 on flat areas

def shade_wordmark(w, cv, oy=0, ox=0):
    sd, sdw, X, Y, A = w["sd"], w["sdw"], w["X"], w["Y"], w["A"]
    ytop, ybot = w["capbox"][0] / SS, w["capbox"][1] / SS
    kn = knot_texture(X, Y)
    st = pile_streaks(X, Y)
    # 1. rug thickness below the silhouette
    Aext = extrude_down(w["base"], DEPTH * SS)       # thickness of the clean base only (no drips)
    side = PAL["indigo_lo"] * (0.92 + 0.5 * st)[..., None]
    cv.over(side, Aext, oy, ox)
    del Aext
    # 2. indigo guard + tufts (tips catch the light)
    ind = mix(PAL["indigo"], PAL["indigo_hi"], np.clip(w["fib_t"] * 0.95, 0, 1))
    ind = ind * ((kn * 0.6 + 0.4) * (1 + 0.10 * st))[..., None]
    cv.over(ind, A, oy, ox)
    del ind
    # 3. teal guard stripe
    teal = PAL["teal"] * (kn * (1 + 0.08 * st))[..., None]
    cv.over(teal, smoothstep(R_TEAL + 0.6, R_TEAL - 0.6, sdw), oy, ox)
    del teal
    # 4. cream border band, shaded by the raised letter (cast shadow down-right)
    inside = smoothstep(0.4, -0.4, sd)
    cast = np.roll(np.roll(inside, int(4 * SS), 0), int(2 * SS), 1)
    cast = ndi.gaussian_filter(cast, 3.0 * SS)
    cr = mix(PAL["cream"], PAL["cream_lo"], smoothstep(R_CREAM - 7, R_CREAM + 0.5, sdw) * 0.55)
    cr = cr * ((0.93 + 0.07 * kn) * (1 + 0.05 * st) * (1 - 0.30 * cast))[..., None]
    cv.over(cr, smoothstep(R_CREAM + 0.6, R_CREAM - 0.6, sdw), oy, ox)
    del cr, cast
    # 5. madder field inside the letters
    din = np.clip(-sd, 0, None); dinw = np.clip(-sdw, 0, None)
    t = np.clip((Y - ytop) / (ybot - ytop), 0, 1)
    red = mix(PAL["madder_hi"], PAL["madder"], smoothstep(0.0, 0.5, t))
    red = mix(red, PAL["madder_lo"], smoothstep(0.5, 1.05, t) * 0.85)
    red = red * (1 + 0.16 * abrash(X, Y))[..., None]
    line = smoothstep(R_IN0 - 0.7, R_IN0 + 0.7, dinw) * smoothstep(R_IN1 + 0.7, R_IN1 - 0.7, dinw)
    # wool, not plastic: a low wide cushion + fibre occlusion at the very edge
    sh = pillow_shade(din, 44.0, 9.0, 3.0)
    ao = 1.0 - 0.20 * (1.0 - smoothstep(0.0, 5.0, din))
    red = red * ((0.90 + 0.10 * kn) * (1 + 0.09 * st) * (1 + 0.75 * sh) * ao)[..., None]
    crm = PAL["cream"] * ((0.93 + 0.07 * kn) * (1 + 0.35 * sh))[..., None]
    col = mix(red, crm, line)
    del red, crm
    cv.over(np.clip(col, 0, 1), inside, oy, ox)
    del col, sh, line, din, dinw
    return cv

# ------------------------------------------------------------------ subtitle runner
SUB_CAP_RATIO = 0.235       # subtitle cap height / wordmark cap height
SUB_TRACK = 0.11            # em
RUN_H_RATIO = 1.95          # runner height / subtitle cap height
R_EDGE, R_BAND, R_LINE = 5.5, 15.0, 19.0   # runner border rings (inset, final px)
DEPTH_R = 10.0

def rrect_sd(X, Y, x0, y0, x1, y1, r):
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    hx_, hy_ = (x1 - x0) / 2 - r, (y1 - y0) / 2 - r
    qx = np.abs(X - cx) - hx_; qy = np.abs(Y - cy) - hy_
    out = np.sqrt(np.maximum(qx, 0) ** 2 + np.maximum(qy, 0) ** 2)
    return out + np.minimum(np.maximum(qx, qy), 0) - r

def poly_mask(polys, W, H, scale=3):
    img = Image.new("L", (W * scale, H * scale), 0)
    d = ImageDraw.Draw(img)
    for p in polys:
        d.polygon([(px * scale, py * scale) for px, py in p], fill=255)
    return np.asarray(img.resize((W, H), Image.BOX)).astype(np.float32) / 255.0

def diamond(cx, cy, w, h):
    return [(cx - w / 2, cy), (cx, cy - h / 2), (cx + w / 2, cy), (cx, cy + h / 2)]

def medallion_polys(cx, cy, m, facing):
    """Rug lozenge with a pendant pointing away from the text (SS px)."""
    outer = [diamond(cx, cy, m, m * 0.84)]
    stem = [(cx + facing * m * 0.46, cy - m * 0.035), (cx + facing * m * 0.70, cy - m * 0.035),
            (cx + facing * m * 0.70, cy + m * 0.035), (cx + facing * m * 0.46, cy + m * 0.035)]
    tip = diamond(cx + facing * m * 0.78, cy, m * 0.2, m * 0.2)
    back = [diamond(cx - facing * m * 0.62, cy, m * 0.13, m * 0.13)]
    return outer + [stem, tip] + back, [diamond(cx, cy, m * 0.62, m * 0.52)], \
        [diamond(cx, cy, m * 0.24, m * 0.2)]

def bezier(p0, p1, p2, n=24):
    t = np.linspace(0, 1, n)[:, None]
    return (1 - t) ** 2 * np.array(p0) + 2 * (1 - t) * t * np.array(p1) + t ** 2 * np.array(p2)

def draw_tassels(W, H, x_left, x_right, y0, y1, LT, rng, up=2):
    """Warp-thread fringe on both short ends. Returns coverage, shade (0..1), wrap mask (SS res)."""
    sc = SS * up
    cov = Image.new("L", (W * up, H * up), 0); shd = Image.new("L", (W * up, H * up), 0)
    wrap = Image.new("L", (W * up, H * up), 0)
    dc, ds, dw = ImageDraw.Draw(cov), ImageDraw.Draw(shd), ImageDraw.Draw(wrap)
    RH = y1 - y0
    n_t = int(round((RH - 8) / 22.0))
    pitch = (RH - 8) / n_t
    for s, xe in ((-1, x_left), (1, x_right)):
        for k in range(n_t):
            yc = y0 + 4 + (k + 0.5) * pitch
            xk = xe + s * 15.0
            spread = rng.uniform(0.8, 1.2)
            for j in range(4):
                o = j - 1.5
                root = (xe - s * 8.0, yc + o * 4.6)
                knot = (xk, yc + o * 1.4)
                L = LT * rng.uniform(0.78, 1.06)
                tip = (xe + s * L, yc + o * 2.9 * spread + rng.uniform(-2.5, 2.5))
                c1 = (xe + s * 6.0, yc + o * 4.0)
                c2 = ((xk + tip[0]) / 2 + s * rng.uniform(-3, 3), (knot[1] + tip[1]) / 2 + rng.uniform(-2.5, 2.5))
                pts = np.vstack([bezier(root, c1, knot, 10), bezier(knot, c2, tip, 22)[1:]])
                val = int(255 * rng.uniform(0.72, 1.0))
                nseg = len(pts) - 1
                # pass 0: a darker fibre edge (keeps the fringe readable on light art), pass 1: the strand
                for extra, v in ((1.1 * sc, int(val * 0.35)), (0.0, val)):
                    for i in range(nseg):
                        wid = (3.0 - 1.2 * max(0.0, (i / nseg - 0.55) / 0.45)) * sc + extra
                        seg = [(pts[i][0] * sc, pts[i][1] * sc), (pts[i + 1][0] * sc, pts[i + 1][1] * sc)]
                        dc.line(seg, fill=255, width=max(1, int(round(wid))))
                        ds.line(seg, fill=v, width=max(1, int(round(wid))))
                        r = wid / 2
                        for q in seg:
                            dc.ellipse([q[0] - r, q[1] - r, q[0] + r, q[1] + r], fill=255)
                            ds.ellipse([q[0] - r, q[1] - r, q[0] + r, q[1] + r], fill=v)
            ww, wh = 6.5, 2 * 1.4 * 1.5 + 7.5
            dw.rounded_rectangle([(xk - ww / 2) * sc, (yc - wh / 2) * sc, (xk + ww / 2) * sc,
                                  (yc + wh / 2) * sc], radius=2.2 * sc, fill=255)
    red = lambda im: np.asarray(im.resize((W, H), Image.BOX)).astype(np.float32) / 255.0
    c = red(cov); sh = red(shd) / np.maximum(c, 1e-3)
    return c, np.clip(sh, 0, 1), red(wrap)

def build_runner(word_cap):
    """Subtitle runner canvas (SS res). Returns Canvas, shadow alpha, size info (final px)."""
    rng = np.random.default_rng(424242)
    probe = ImageFont.truetype(os.path.join(FONTS, SUB_FONT), 1000)
    cap_ratio = (probe.getbbox("H")[3] - probe.getbbox("H")[1]) / 1000.0
    sub_cap = SUB_CAP_RATIO * word_cap
    fs = sub_cap / cap_ratio
    up = 3
    font = ImageFont.truetype(os.path.join(FONTS, SUB_FONT), int(round(fs * SS * up)))
    track = SUB_TRACK * fs                                   # final px
    tw = sum(font.getlength(ch) for ch in SUB) / (SS * up) + track * (len(SUB) - 1)
    RH = RUN_H_RATIO * sub_cap
    m = 0.60 * RH
    side = R_LINE + 16 + 0.88 * m + 0.69 * m + 18
    RW = tw + 2 * side
    LT = 0.50 * RH
    mx, my = LT + 12, 8
    Wf, Hf = RW + 2 * mx, RH + my + DEPTH_R + 8
    W, H = int(np.ceil(Wf * SS)), int(np.ceil(Hf * SS))
    Yi, Xi = np.mgrid[0:H, 0:W].astype(np.float32)
    X = Xi / SS; Y = Yi / SS
    x0, y0, x1, y1 = mx, my, mx + RW, my + RH
    sd = rrect_sd(X, Y, x0, y0, x1, y1, 5.0)
    wob = (valnoise(X / 2.0, Y / 2.0, 77) - 0.5) * 1.2
    body = smoothstep(0.6, -0.6, sd + wob)
    inset = np.clip(-sd, 0, None); insw = np.clip(-(sd + wob * 0.8), 0, None)
    cv = Canvas(H, W)
    kn = knot_texture(X, Y); st = pile_streaks(X, Y, 12)
    # thickness + tassels (tassels lie on the floor, under the body edge)
    cv.over(PAL["indigo_lo"] * (0.92 + 0.5 * st)[..., None], extrude_down(body, DEPTH_R * SS))
    tc, tsh, twrap = draw_tassels(W, H, x0, x1, y0, y1, LT, rng)
    tcol = mix(PAL["cream_lo"] * 0.85, PAL["cream"], tsh)
    cv.over(tcol, tc)
    wr = PAL["madder"] * (1 + 0.6 * pillow_shade(ndi.distance_transform_edt(twrap > 0.5) / SS, 2.5, 2.0, 0.4))[..., None]
    cv.over(np.clip(wr, 0, 1), twrap)
    # body rings: indigo edge | madder band with cream dots | cream line | teal field
    rim = pillow_shade(inset, 8.0, 3.0, 1.0)
    cv.over(PAL["indigo"] * (kn * (1 + 1.2 * rim))[..., None], body)
    bandc = (R_EDGE + R_BAND) / 2
    horiz = np.minimum(Y - y0, y1 - Y) <= np.minimum(X - x0, x1 - X)
    along = np.where(horiz, X - x0, Y - y0)
    per = 12.5
    dot = np.sqrt((insw - bandc) ** 2 + ((along / per - np.floor(along / per) - 0.5) * per) ** 2)
    band = PAL["madder"] * (kn * (1 + 0.12 * abrash(X, Y)))[..., None]
    band = mix(band, PAL["cream"], smoothstep(2.3, 1.3, dot))
    cv.over(band, smoothstep(R_EDGE - 0.6, R_EDGE + 0.6, insw))
    cv.over(PAL["cream"] * kn[..., None], smoothstep(R_BAND - 0.6, R_BAND + 0.6, insw))
    t = np.clip((Y - y0) / RH, 0, 1)
    field = mix(PAL["teal"] * 1.08, PAL["teal_lo"], smoothstep(0.1, 1.0, t))
    field = field * (kn * (1 + 0.14 * abrash(X, Y, 5)) * (1 + 0.06 * st))[..., None]
    cv.over(field, smoothstep(R_LINE - 0.6, R_LINE + 0.6, insw))
    del rim, band, field, dot, along, horiz
    # medallions
    cy = (y0 + y1) / 2
    for s in (-1, 1):
        cx = (x0 + R_LINE + 16 + 0.88 * m) if s < 0 else (x1 - R_LINE - 16 - 0.88 * m)
        o, i_, c_ = medallion_polys(cx * SS, cy * SS, m * SS, s)
        mo = poly_mask(o, W, H); mi = poly_mask(i_, W, H); mc = poly_mask(c_, W, H)
        sh = ndi.gaussian_filter(np.roll(mo, int(1.5 * SS), 0), 1.2 * SS)
        cv.over(PAL["indigo_lo"], sh * 0.55)
        cv.over(PAL["cream"] * kn[..., None], mo)
        cv.over(PAL["madder"] * kn[..., None], mi)
        cv.over(PAL["gold"], mc)
    # subtitle text, embossed
    asc = font.getbbox("H")[1] / (SS * up)
    tx = (x0 + x1) / 2 - tw / 2
    ty = cy - sub_cap / 2 - asc
    img = Image.new("L", (W * up, H * up), 0); d = ImageDraw.Draw(img); xx = tx
    for ch in SUB:
        d.text((xx * SS * up, ty * SS * up), ch, font=font, fill=255)
        xx += font.getlength(ch) / (SS * up) + track
    tm = np.asarray(img.resize((W, H), Image.BOX)).astype(np.float32) / 255.0
    tsh_ = ndi.gaussian_filter(np.roll(tm, int(2.0 * SS), 0), 1.0 * SS)
    cv.over(PAL["indigo_lo"], np.clip(tsh_ * 0.9, 0, 1))
    tcol = mix(PAL["cream"], PAL["cream_lo"], smoothstep(0.35, 1.0, (Y - (cy - sub_cap / 2)) / sub_cap) * 0.45)
    cv.over(tcol * (0.55 + 0.45 * kn)[..., None], tm)
    info = dict(W=W, H=H, x0=x0, y0=y0, x1=x1, y1=y1, RW=RW, RH=RH, sub_cap=sub_cap, fs=fs)
    return cv, info

# ------------------------------------------------------------------ compositing / output
def over_premult(dst, src, oy, ox):
    h, w = src.a.shape
    sl = (slice(oy, oy + h), slice(ox, ox + w))
    dst.rgb[sl] = src.rgb + dst.rgb[sl] * (1 - src.a[..., None])
    dst.a[sl] = src.a + dst.a[sl] * (1 - src.a)

def finish(cv, pad_frac=0.04, shadow=True):
    """SS canvas -> final straight-alpha RGBA PIL image, soft shadow, tight crop + padding."""
    H, W = cv.a.shape
    Hf, Wf = H // SS, W // SS
    def rs(ch):
        return np.asarray(Image.fromarray(np.ascontiguousarray(ch[:Hf * SS, :Wf * SS]), "F")
                          .resize((Wf, Hf), Image.LANCZOS), np.float32)
    a = np.clip(rs(cv.a), 0, 1)
    rgb = np.stack([rs(cv.rgb[..., c]) for c in range(3)], -1)
    rgb = np.clip(rgb, 0, a[..., None])
    if shadow:
        P = 48                                   # pad so the shifted shadow never wraps
        a = np.pad(a, P); rgb = np.pad(rgb, ((P, P), (P, P), (0, 0)))
        sa = ndi.gaussian_filter(a, 9.0)
        sa = np.vstack([np.zeros((12, sa.shape[1]), np.float32), sa[:-12]]) * 0.5
        rgb = rgb + PAL["shadow"] * (sa * (1 - a))[..., None]
        a = a + sa * (1 - a)
    ys, xs = np.nonzero(a > 0.012)
    bw = xs.max() - xs.min() + 1
    pad = int(round(pad_frac * bw))
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    Ho, Wo = y1 - y0 + 2 * pad, x1 - x0 + 2 * pad
    A = np.zeros((Ho, Wo), np.float32); C = np.zeros((Ho, Wo, 3), np.float32)
    A[pad:pad + y1 - y0, pad:pad + x1 - x0] = a[y0:y1, x0:x1]
    C[pad:pad + y1 - y0, pad:pad + x1 - x0] = rgb[y0:y1, x0:x1]
    A[A < 1.0 / 512] = 0
    col = np.where(A[..., None] > 1e-6, C / np.maximum(A[..., None], 1e-6), 0)
    out = np.dstack([np.clip(col, 0, 1), A])
    return Image.fromarray((out * 255 + 0.5).astype(np.uint8), "RGBA")

def scale_rgba(im, w):
    h = int(round(im.height * w / im.width))
    return im.convert("RGBa").resize((w, h), Image.LANCZOS).convert("RGBA")

def place(bg, logo, cx, top):
    bg = bg.convert("RGBA")
    layer = Image.new("RGBA", bg.size, (0, 0, 0, 0))
    layer.paste(logo, (int(round(cx - logo.width / 2)), int(round(top))))
    return Image.alpha_composite(bg, layer)

def previews(master, wordmark):
    for name, colr in (("preview_dark.png", (0x1b, 0x28, 0x38)), ("preview_light.png", (0xf2, 0xf2, 0xf2))):
        bg = Image.new("RGBA", (1600, 900), colr + (255,))
        lg = scale_rgba(master, 1280)
        place(bg, lg, 800, 450 - lg.height / 2).convert("RGB").save(os.path.join(OUT, name))
    game = Image.open(PROBE).convert("RGBA")
    lg = scale_rgba(master, int(game.width * 0.55))
    place(game, lg, game.width / 2, game.height * 0.06).convert("RGB").save(os.path.join(OUT, "preview_game.png"))
    # small capsule test: 462x174, blurred + darkened carpet crop behind the wordmark
    g = Image.open(PROBE).convert("RGB")
    crop = g.crop((480, 300, 1440, 662)).resize((462, 174), Image.LANCZOS)
    crop = crop.filter(ImageFilter.GaussianBlur(5))
    crop = Image.fromarray((np.asarray(crop, np.float32) * 0.6).astype(np.uint8))
    lg = scale_rgba(wordmark, int(462 * 0.85))
    card = place(crop, lg, 231, 87 - lg.height / 2).convert("RGB")
    card.save(os.path.join(OUT, "preview_small.png"))
    card.resize((231, 87), Image.LANCZOS).save(os.path.join(OUT, "preview_tiny.png"))

def main():
    os.makedirs(OUT, exist_ok=True)
    w = build_wordmark()
    Hw, Ww = w["A"].shape
    word_cap = (w["f"].getbbox("R")[3] - w["f"].getbbox("R")[1]) / SS
    cvw = Canvas(Hw, Ww)
    shade_wordmark(w, cvw)
    glyph_bottom = w["capbox"][1] / SS
    glyph_cx = Ww / SS / 2
    for k in ("sd", "sdw", "tuft", "fib_t", "X", "Y", "B"):
        w.pop(k, None)
    rcv, ri = build_runner(word_cap)
    print("word cap %.1f px, sub cap %.1f px (%.0f%%), runner %.0fx%.0f" %
          (word_cap, ri["sub_cap"], 100 * ri["sub_cap"] / word_cap, ri["RW"], ri["RH"]))
    ry = int(round((glyph_bottom + R_OUT * 0.42 - ri["y0"]) * SS))
    rx = int(round(glyph_cx * SS - ri["W"] / 2))
    X0, Y0 = min(0, rx), 0
    X1, Y1 = max(Ww, rx + ri["W"]), max(Hw, ry + ri["H"])
    Wt = ((X1 - X0 + SS - 1) // SS) * SS; Ht = ((Y1 - Y0 + SS - 1) // SS) * SS
    tot = Canvas(Ht, Wt)
    over_premult(tot, cvw, -Y0, -X0)
    over_premult(tot, rcv, ry - Y0, rx - X0)
    del rcv
    master = finish(tot); del tot
    wordmark = finish(cvw); del cvw
    master.save(os.path.join(OUT, "master.png"))
    wordmark.save(os.path.join(OUT, "wordmark.png"))
    print("master", master.size, "wordmark", wordmark.size)
    previews(master, wordmark)

if __name__ == "__main__":
    main()
