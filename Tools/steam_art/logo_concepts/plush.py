"""PLUSH PILE logo for "Refluff: Carpet Cleaning ASMR".

The word is a little rug cut in the shape of the letters: cream wool pile tufts
(thousands of procedural strands, drawn at 3x and downsampled) inlaid in a red
rug field with a bound edge, plus a teal runner with fringe tassels for the
subtitle.  Deterministic (fixed seeds).

    python Tools/steam_art/logo_concepts/plush.py [out_dir]

Writes master.png, wordmark.png and the preview_*.png legibility tests.
"""
import os
import sys
import time

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont
from scipy import ndimage as ndi

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
FONTS = os.path.join(ROOT, "Tools", "steam_art", "fonts")
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "SteamStore", "_work", "logo", "plush")
PROBE = os.path.join(ROOT, "SteamStore", "_work", "probe_1080.png")

SS = 3                      # supersampling factor of the wordmark render
FINAL_W = 2400              # approx. master width (px)
WORD = "Refluff"
SUB = "CARPET CLEANING ASMR"
FONT_FILE = "Fredoka[wdth,wght].ttf"
FONT_AXES = {"Weight": 700, "Width": 112}

# ---- palette (sRGB) -------------------------------------------------------
def hx(s):
    s = s.lstrip("#")
    return np.array([int(s[i:i + 2], 16) for i in (0, 2, 4)], np.float32) / 255.0

WOOL_TIP = hx("#fffaf0")    # sunlit tuft tips
WOOL = hx("#f7e9cd")        # cream wool body
WOOL_ROOT = hx("#c49a6c")   # shaded root of a tuft
WOOL_DEEP = hx("#a88062")   # gaps between tufts
RUG = hx("#c2361f")         # rug-red field
RUG_DEEP = hx("#8c2014")    # red pile shadow
BIND = hx("#5b110b")        # bound (serged) rug edge
SIDE = hx("#6d170f")        # rug thickness / extrusion
SIDE_DARK = hx("#3e0906")
TEAL = hx("#11807a")        # subtitle runner
TEAL_DEEP = hx("#0a5752")
TEAL_SIDE = hx("#073f3c")
CREAM_TXT = hx("#fff5e2")
PALETTE = ["#fffaf0", "#f7e9cd", "#c49a6c", "#a88062", "#c2361f", "#8c2014",
           "#5b110b", "#6d170f", "#11807a", "#0a5752", "#fff5e2"]


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


def signed_distance(mask):
    """+ inside, - outside, in pixels (mask: bool)."""
    din = ndi.distance_transform_edt(mask)
    dout = ndi.distance_transform_edt(~mask)
    return (din - dout + np.where(mask, -0.5, 0.5)).astype(np.float32)


def blur(a, sigma):
    return ndi.gaussian_filter(a, sigma, mode="constant")


def over(dst, src):
    """Premultiplied RGBA 'over' in place: dst, src float32 (H,W,4)."""
    a = src[..., 3:4]
    dst *= (1.0 - a)
    dst += src


def layer(color, alpha):
    """Premultiplied layer from a colour (3,) or (H,W,3) and alpha (H,W)."""
    a = np.clip(alpha, 0.0, 1.0)[..., None].astype(np.float32)
    c = np.broadcast_to(np.asarray(color, np.float32), a.shape[:2] + (3,))
    return np.concatenate([c * a, a], axis=2)


def downsample(prem, factor):
    """Premultiplied float RGBA -> premultiplied float RGBA, LANCZOS per channel."""
    h, w = prem.shape[:2]
    size = (int(round(w / factor)), int(round(h / factor)))
    ch = [np.asarray(Image.fromarray(np.ascontiguousarray(prem[..., i]), "F").resize(size, Image.LANCZOS))
          for i in range(4)]
    out = np.stack(ch, axis=2)
    out[..., 3] = np.clip(out[..., 3], 0.0, 1.0)
    out[..., :3] = np.clip(out[..., :3], 0.0, out[..., 3:4])
    return out


def to_image(prem):
    """Premultiplied float RGBA -> straight-alpha 8-bit PIL image."""
    a = prem[..., 3:4]
    rgb = np.where(a > 1e-4, prem[..., :3] / np.maximum(a, 1e-4), 0.0)
    out = np.concatenate([np.clip(rgb, 0, 1), np.clip(a, 0, 1)], axis=2)
    return Image.fromarray((out * 255.0 + 0.5).astype(np.uint8), "RGBA")


def from_image(im):
    a = np.asarray(im.convert("RGBA"), np.float32) / 255.0
    a[..., :3] *= a[..., 3:4]
    return a


def crop_pad(prem, pad_frac=0.04, thresh=2.0 / 255.0):
    ys, xs = np.nonzero(prem[..., 3] > thresh)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    pad = int(round(pad_frac * max(x1 - x0, y1 - y0)))
    h, w = y1 - y0 + 2 * pad, x1 - x0 + 2 * pad
    out = np.zeros((h, w, 4), np.float32)
    out[pad:pad + y1 - y0, pad:pad + x1 - x0] = prem[y0:y1, x0:x1]
    return out


# ---- wordmark geometry ----------------------------------------------------
# per-letter bounce (dy in em, rotation in degrees) - a plush toy, not a sign
LETTER_FX = [(0.00, -3.0), (0.035, 2.5), (-0.015, -1.5), (0.005, 1.5),
             (0.035, -2.0), (-0.01, 2.0), (0.015, -1.5)]
TRACK = 0.045               # extra letter spacing (em)
BOLD = 0.004                # emboldening before the fur (em)
MARGIN = 0.30               # canvas margin around the letters (em)


def word_mask(size):
    """Anti-aliased coverage of the laid-out word at the working resolution."""
    ft = font(size, FONT_AXES)
    asc, desc = ft.getmetrics()
    m = int(MARGIN * size)
    total = sum(ft.getlength(c) for c in WORD) + TRACK * size * (len(WORD) - 1)
    W = int(total + 2 * m)
    H = int(asc + desc + 2 * m)
    canvas = np.zeros((H, W), np.float32)
    x = float(m)
    for ch, (dy, rot) in zip(WORD, LETTER_FX):
        adv = ft.getlength(ch)
        gw, gh = int(adv + size), int(asc + desc + size)
        g = Image.new("L", (gw, gh), 0)
        ImageDraw.Draw(g).text((size / 2, size / 2), ch, font=ft, fill=255)
        # rotate about the glyph's own centre of mass
        ga = np.asarray(g, np.float32)
        cy, cx = ndi.center_of_mass(ga)
        g = g.rotate(rot, resample=Image.BICUBIC, center=(cx, cy))
        ga = np.asarray(g, np.float32) / 255.0
        ox = int(round(x - size / 2))
        oy = int(round(m - size / 2 + dy * size))
        y0, x0 = max(oy, 0), max(ox, 0)
        y1, x1 = min(oy + gh, H), min(ox + gw, W)
        canvas[y0:y1, x0:x1] = np.maximum(canvas[y0:y1, x0:x1],
                                          ga[y0 - oy:y1 - oy, x0 - ox:x1 - ox])
        x += adv + TRACK * size
    baseline = m + asc
    return canvas, baseline


def size_for_width(target_w):
    ref = 400
    ft = font(ref, FONT_AXES)
    total = sum(ft.getlength(c) for c in WORD) + TRACK * ref * (len(WORD) - 1)
    return ref * target_w / total


def fields(cov, size):
    """Distance fields for the letters and the rug that carries them."""
    letters0 = cov > 0.5
    sd0 = signed_distance(letters0)
    bold = BOLD * size
    sd = sd0 + bold                                   # emboldened letters
    letters = sd > 0
    # rug field: dilate, then close (fills counters, smooths concavities)
    ro, close = 0.075 * size, 0.07 * size
    dil = sd > -(ro + close)
    rug_sd = signed_distance(dil) - close
    rug_sd = ndi.gaussian_filter(rug_sd, 0.012 * size)  # round every corner
    # gap half-width outside the letters (limits fringe inside counters)
    dout = np.maximum(-sd, 0.0)
    gap = ndi.maximum_filter(dout, size=int(0.09 * size) | 1)
    return dict(sd=sd.astype(np.float32), letters=letters, rug_sd=rug_sd.astype(np.float32),
                gap=gap.astype(np.float32))


# ---- plush pile ----------------------------------------------------------
LIGHT = np.array([-0.35, -0.72, 0.80], np.float32)
LIGHT /= np.linalg.norm(LIGHT)


def pillow_shade(sd, radius, sigma):
    """Soft top-left light on a rounded 'pillow' raised from the shape."""
    t = np.clip(sd / radius, 0.0, 1.0)
    h = radius * np.sqrt(1.0 - (1.0 - t) ** 2)
    h = ndi.gaussian_filter(h, sigma)
    gy, gx = np.gradient(h)
    n = np.stack([-gx, -gy, np.ones_like(h)], axis=2)
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    ndl = n @ LIGHT
    wrap = np.clip((ndl + 0.35) / 1.35, 0.0, 1.0)
    return h, wrap.astype(np.float32)


def smooth_noise(shape, scale, rng):
    """Low-frequency noise in [-1,1]-ish, 'scale' = feature size in px."""
    lo = (max(2, int(shape[0] / scale) + 2), max(2, int(shape[1] / scale) + 2))
    n = ndi.gaussian_filter(rng.standard_normal(lo).astype(np.float32), 1.0)
    n /= n.std() + 1e-6
    z = ndi.zoom(n, (shape[0] / lo[0], shape[1] / lo[1]), order=3)
    return z[:shape[0], :shape[1]]


def jitter_grid(mask_fn, H, W, step, rng):
    ys, xs = np.mgrid[0:H:step, 0:W:step].astype(np.float32)
    ys = ys + rng.uniform(0, step, ys.shape)
    xs = xs + rng.uniform(0, step, xs.shape)
    ys, xs = ys.ravel(), xs.ravel()
    ok = (ys < H - 1) & (xs < W - 1)
    ys, xs = ys[ok], xs[ok]
    keep = mask_fn(ys.astype(int), xs.astype(int))
    return ys[keep], xs[keep]


def render_pile(sd, gap, size, seed, wool=(WOOL_ROOT, WOOL, WOOL_TIP, WOOL_DEEP),
                step=0.0105, l_edge=0.032, l_in=0.013, strands=8, reach=1.0):
    """Tufted wool pile filling sd>0, fraying outward.  Returns an RGBA PIL image."""
    rng = np.random.default_rng(seed)
    H, W = sd.shape
    root_c, body_c, tip_c, deep_c = [np.asarray(c, np.float32) for c in wool]
    radius = 0.07 * size
    h, shade = pillow_shade(sd, radius, 0.006 * size)
    ao = np.sqrt(np.clip(blur((sd > 0).astype(np.float32), 0.04 * size) * 1.7, 0.0, 1.0))
    shade = shade * (0.82 + 0.18 * ao)
    # outward normal from a smoothed distance field
    sds = ndi.gaussian_filter(sd, 0.004 * size)
    gy, gx = np.gradient(sds)
    gl = np.sqrt(gx * gx + gy * gy) + 1e-6
    nx, ny = -gx / gl, -gy / gl
    # interior comb: smooth flow field with a slight downward lean
    fx = smooth_noise(sd.shape, 0.09 * size, rng) * 0.55 + 0.30
    fy = smooth_noise(sd.shape, 0.09 * size, rng) * 0.55 + 0.70

    # base: the shaded deep pile colour inside the letters (solid core)
    cov = np.clip(sd + 0.5, 0.0, 1.0)
    base_rgb = deep_c[None, None, :] * (0.50 + 0.62 * shade[..., None])
    base = np.concatenate([np.clip(base_rgb, 0, 1), cov[..., None]], axis=2)
    img = Image.fromarray((base * 255 + 0.5).astype(np.uint8), "RGBA")

    step_px = step * size
    E = 0.05 * size                                   # edge-influence depth
    inside = lambda y, x: sd[y, x] > -0.5
    ty, tx = jitter_grid(inside, H, W, step_px, rng)
    edge_band = lambda y, x: (sd[y, x] > 0.5) & (sd[y, x] < 0.35 * E)
    ey, ex = jitter_grid(edge_band, H, W, step_px * 0.65, rng)
    ty, tx = np.concatenate([ty, ey]), np.concatenate([tx, ex])
    iy, ix = ty.astype(int), tx.astype(int)
    s0 = sd[iy, ix]
    e = 1.0 - smoothstep(0.0, E, s0)                  # 1 at the edge
    dx = nx[iy, ix] * 2.2 * e + fx[iy, ix] * (1.0 - e)
    dy = ny[iy, ix] * 2.2 * e + fy[iy, ix] * (1.0 - e)
    dl = np.sqrt(dx * dx + dy * dy) + 1e-6
    dx, dy = dx / dl, dy / dl
    L = (l_in + (l_edge - l_in) * e ** 1.5) * size * rng.uniform(0.8, 1.2, len(ty))
    # limit the fringe where the outside is a narrow counter / gap
    oy = np.clip((ty - ny[iy, ix] * (s0 + 3)).astype(int), 0, H - 1)
    ox = np.clip((tx - nx[iy, ix] * (s0 + 3)).astype(int), 0, W - 1)
    cosn = np.clip(dx * nx[iy, ix] + dy * ny[iy, ix], 0.25, 1.0)
    L = np.minimum(L, (np.maximum(s0, 0) + reach * 0.45 * gap[oy, ox] + 2.0) / cosn)
    sh = shade[iy, ix]
    rows = np.nonzero((sd > 0).any(axis=1))[0]
    vgrad = np.clip((ty - rows.min()) / (rows.max() - rows.min()), 0, 1)
    order = np.argsort(h[iy, ix] + rng.uniform(0, 0.35 * radius, len(ty)))
    print(f"  pile: {len(ty)} tufts x {strands} strands")

    draw = ImageDraw.Draw(img)
    w_root = max(2, int(round(SS * 1.0)))
    w_tip = max(1, int(round(SS * 0.67)))
    for k in order:
        ang0 = np.arctan2(dy[k], dx[k])
        base_len = L[k]
        ek = e[k]
        warm = rng.uniform(-1, 1)
        tint = np.array([1.0 + 0.02 * warm, 1.0, 1.0 - 0.035 * warm], np.float32)
        # interior tufts are seen from above (mostly tips), edge tufts from the side
        c0 = root_c + (body_c - root_c) * (0.72 * (1 - ek))
        c1 = root_c + (body_c - root_c) * (1.0 - 0.4 * ek)
        c2 = body_c + (tip_c - body_c) * (0.65 - 0.25 * ek)
        cols = (c0, c1, c2)
        lf = (0.47 + 0.72 * sh[k]) * (1.06 - 0.12 * vgrad[k])
        for _ in range(strands):
            r = rng.uniform(0, 0.5 * step_px)
            a = rng.uniform(0, 2 * np.pi)
            x0 = tx[k] + r * np.cos(a)
            y0 = ty[k] + r * np.sin(a)
            ang = ang0 + rng.normal(0, 0.16 + 0.10 * ek)
            ln = base_len * rng.uniform(0.72, 1.12)
            bend = rng.normal(0, 0.10) * ln
            cx_, cy_ = np.cos(ang), np.sin(ang)
            px, py = -cy_, cx_
            pts = []
            for t in (0.0, 1 / 3, 2 / 3, 1.0):
                bb = 4 * t * (1 - t) * bend
                pts.append((x0 + cx_ * ln * t + px * bb, y0 + cy_ * ln * t + py * bb))
            lit = lf * rng.uniform(0.94, 1.05)
            for j in range(3):
                c = cols[j] * tint * lit
                c8 = tuple(int(v) for v in np.clip(c * 255 + 0.5, 0, 255)) + (255,)
                draw.line([pts[j], pts[j + 1]], fill=c8, width=w_root if j < 2 else w_tip)
    return img


# ---- rug field, bound edge and thickness ---------------------------------
def pile_grain(shape, rng, amount):
    """Fine short-pile speckle (multiplicative, around 1)."""
    g = ndi.gaussian_filter(rng.standard_normal(shape).astype(np.float32), 0.45 * SS)
    g /= g.std() + 1e-6
    return 1.0 + amount * g


def render_rug(rug_sd, size, seed, body=RUG, deep=RUG_DEEP, bind=BIND):
    """Flat rug field with a rounded bevel and a bound (serged) outer edge."""
    rng = np.random.default_rng(seed)
    cov = np.clip(rug_sd + 0.5, 0.0, 1.0)
    _, shade = pillow_shade(rug_sd, 0.035 * size, 0.004 * size)
    bw = 0.016 * size
    edge = 1.0 - smoothstep(bw * 0.75, bw * 1.25, rug_sd)          # binding band
    _, bshade = pillow_shade(rug_sd, bw, 0.002 * size)
    grain = pile_grain(rug_sd.shape, rng, 0.06)
    mott = 1.0 + 0.035 * smooth_noise(rug_sd.shape, 0.05 * size, rng)
    field = deep[None, None] + (body - deep)[None, None] * np.clip(0.25 + 0.95 * shade, 0, 1.15)[..., None]
    field = field * (grain * mott)[..., None]
    rim = bind[None, None] * (0.75 + 0.85 * bshade[..., None]) * grain[..., None]
    rgb = field * (1 - edge[..., None]) + rim * edge[..., None]
    return np.clip(rgb, 0, 1).astype(np.float32), cov.astype(np.float32)


def extrude(cov, depth, top, bottom, rng):
    """Thickness below a shape: shifted copies, darker further down."""
    H, W = cov.shape
    acc = np.zeros_like(cov)
    kmap = np.full(cov.shape, float(depth), np.float32)
    for k in range(1, int(depth) + 1):
        sh = np.zeros_like(cov)
        sh[k:] = cov[:-k]
        newly = (sh > 0.5) & (kmap >= depth)
        kmap[newly] = k
        np.maximum(acc, sh, out=acc)
    t = smoothstep(0.0, depth, kmap)[..., None]
    rgb = top[None, None] * (1 - t) + bottom[None, None] * t
    streak = 1.0 + 0.05 * ndi.gaussian_filter(rng.standard_normal((1, W)).astype(np.float32), (0, 1.0 * SS))
    rgb = rgb * streak[..., None]
    return np.clip(rgb, 0, 1).astype(np.float32), acc


def render_wordmark(final_text_w=2150, seed=11):
    t0 = time.time()
    size = size_for_width(final_text_w * SS)
    cov, baseline = word_mask(size)
    F = fields(cov, size)
    print(f"  fields {cov.shape} in {time.time() - t0:.1f}s")
    rng = np.random.default_rng(seed)
    pile = render_pile(F["sd"], F["gap"], size, seed + 1)
    print(f"  pile drawn in {time.time() - t0:.1f}s")
    pile = from_image(pile)
    # rug + cast shadow of the pile onto it
    rug_rgb, rug_cov = render_rug(F["rug_sd"], size, seed + 2)
    fa = pile[..., 3]
    rows = np.nonzero((fa > 0.3).any(axis=1))[0]
    fur_top, fur_bottom = rows.min() / SS, rows.max() / SS
    shadow = blur(fa, 0.010 * size)
    shadow = ndi.shift(shadow, (0.014 * size, 0.004 * size), order=1)
    rug_rgb *= (1.0 - 0.45 * np.clip(shadow, 0, 1))[..., None]
    side_rgb, side_cov = extrude(rug_cov, 0.032 * size, SIDE, SIDE_DARK, rng)
    out = layer(side_rgb, side_cov)
    over(out, layer(rug_rgb, rug_cov))
    over(out, pile)
    del pile, rug_rgb, side_rgb
    out = downsample(out, SS)
    print(f"  wordmark done in {time.time() - t0:.1f}s")
    return out, dict(size=size / SS, baseline=baseline / SS, fur_top=fur_top, fur_bottom=fur_bottom)


# ---- subtitle runner ------------------------------------------------------
SUB_AXES = {"Weight": 700, "Width": 106}
SUB_TRACK = 0.09            # em


def rounded_box_sd(H, W, cx, cy, hw, hh, r):
    ys, xs = np.mgrid[0:H, 0:W].astype(np.float32)
    qx = np.abs(xs - cx) - (hw - r)
    qy = np.abs(ys - cy) - (hh - r)
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


def render_subtitle(word_size, seed=23, rel=0.235):
    """Teal rug runner with fringe tassels carrying the subtitle (final-res prem RGBA)."""
    rng = np.random.default_rng(seed)
    ft = font(rel * word_size * SS, SUB_AXES)
    tm = text_mask(SUB, ft, SUB_TRACK * rel * word_size * SS)
    cap = tm.shape[0]
    h = cap * 2.15
    pad_x = cap * 1.25
    rw = tm.shape[1] + 2 * pad_x
    fr = 0.30 * h                                   # fringe length
    depth = 0.030 * word_size * SS
    m = int(fr + 0.1 * h)
    W, H = int(rw + 2 * m), int(h + depth + 2 * m)
    cx, cy = W / 2.0, m + h / 2.0
    sd = rounded_box_sd(H, W, cx, cy, rw / 2, h / 2, 0.20 * h)
    rgb, cov = render_rug(sd, word_size * SS * 0.85, seed + 1, body=TEAL, deep=TEAL_DEEP, bind=TEAL_SIDE)
    # woven border stripe
    inset, th = 0.17 * h, 0.032 * h
    stripe = smoothstep(-1.2, 1.2, th / 2 - np.abs(sd - inset)) * (sd > 0)
    rgb = rgb * (1 - 0.85 * stripe[..., None]) + WOOL[None, None] * 0.93 * 0.85 * stripe[..., None]
    # text: soft emboss shadow, then cream letters
    tx0, ty0 = int(round(cx - tm.shape[1] / 2)), int(round(cy - cap / 2 + 0.02 * cap))
    full = np.zeros((H, W), np.float32)
    full[ty0:ty0 + cap, tx0:tx0 + tm.shape[1]] = tm
    sh = ndi.shift(blur(full, 0.05 * cap), (0.07 * cap, 0.0), order=1)
    rgb = rgb * (1 - 0.55 * np.clip(sh, 0, 1)[..., None])
    tg = pile_grain(full.shape, rng, 0.035)[..., None]
    tl = 1.0 + 0.05 * np.clip(-(np.gradient(blur(full, 0.04 * cap))[0]) * cap * 0.2, -1, 1)[..., None]
    rgb = rgb * (1 - full[..., None]) + np.clip(CREAM_TXT[None, None] * tg * tl, 0, 1) * full[..., None]
    side_rgb, side_cov = extrude(cov, depth, TEAL_SIDE, SIDE_DARK * 0.5 + TEAL_SIDE * 0.5, rng)
    out = layer(side_rgb, side_cov)
    # fringe tassels on both short ends (behind the runner body)
    fimg = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    fd = ImageDraw.Draw(fimg)
    wid = max(2, int(round(0.021 * h)))
    n_b, per = 6, 4
    ys_b = np.linspace(cy - h / 2 + 0.22 * h, cy + h / 2 - 0.22 * h, n_b)
    for side in (-1, 1):
        for yb in ys_b:
            ex_ = cx + side * (rw / 2 - 0.03 * h)           # threads start under the binding
            kx = cx + side * (rw / 2 + 0.05 * h)            # knot
            tilt = (yb - cy) / h * 0.25
            lit = rng.uniform(0.94, 1.03) * (1.02 - 0.22 * (yb - cy) / h)
            for s_ in range(per):
                u = (s_ - (per - 1) / 2)
                ln = fr * rng.uniform(0.9, 1.04)
                y_k = yb + u * 0.016 * h
                y_e = yb + u * 0.030 * h + tilt * ln + 0.035 * h + rng.normal(0, 0.004 * h)
                x_e = kx + side * ln
                pts = [(ex_, yb + u * 0.022 * h), (kx, y_k), (0.5 * (kx + x_e), 0.5 * (y_k + y_e) + 0.006 * h), (x_e, y_e)]
                for j, c in enumerate((WOOL * 0.80, WOOL * 0.92, WOOL)):
                    c8 = tuple(int(v) for v in np.clip(c * lit * 255, 0, 255)) + (255,)
                    fd.line([pts[j], pts[j + 1]], fill=c8, width=wid)
            kr = 0.030 * h
            kc = tuple(int(v) for v in np.clip(WOOL * 0.78 * lit * 255, 0, 255)) + (255,)
            fd.ellipse([kx - kr * 1.2, yb - kr * 1.25, kx + kr * 1.2, yb + kr * 1.25], fill=kc)
    over(out, from_image(fimg))
    over(out, layer(rgb, cov))
    return downsample(out, SS)


def resize_prem(prem, size):
    ch = [np.asarray(Image.fromarray(np.ascontiguousarray(prem[..., i]), "F").resize(size, Image.LANCZOS))
          for i in range(4)]
    out = np.stack(ch, axis=2)
    out[..., 3] = np.clip(out[..., 3], 0, 1)
    out[..., :3] = np.clip(out[..., :3], 0, out[..., 3:4])
    return out


# ---- lockup, shadow, previews ---------------------------------------------
def drop_shadow(prem, off, sigma, opacity, color=(0.10, 0.03, 0.02)):
    a = ndi.shift(blur(prem[..., 3], sigma), off, order=1) * opacity
    return layer(np.asarray(color, np.float32), a)


def with_shadow(prem, size):
    pad = int(0.12 * size)
    big = np.zeros((prem.shape[0] + 2 * pad, prem.shape[1] + 2 * pad, 4), np.float32)
    big[pad:-pad, pad:-pad] = prem
    out = drop_shadow(big, (0.035 * size, 0.008 * size), 0.028 * size, 0.42)
    over(out, drop_shadow(big, (0.010 * size, 0.0), 0.006 * size, 0.35))
    over(out, big)
    return crop_pad(out, 0.04)


def lockup(word, info, sub):
    size = info["size"]
    wh, ww = word.shape[:2]
    sh, sw = sub.shape[:2]
    # runner sits just under the lowest fur, overlapping the rug like a sewn label
    top = info["fur_bottom"] + 0.012 * size
    # the runner image has a margin of (fringe + 0.1h) around its body: find its body top
    body_rows = np.nonzero((sub[..., 3] > 0.5)[:, sw // 2])[0]
    oy = int(round(top - body_rows.min()))
    ox = int(round((ww - sw) / 2))
    H = max(wh, oy + sh) + 2
    W = max(ww, ox + sw, sw) + 2
    x0 = max(0, -ox)
    canvas = np.zeros((H, W + 2 * x0, 4), np.float32)
    canvas[:wh, x0:x0 + ww] = word
    placed = np.zeros_like(canvas)
    placed[oy:oy + sh, x0 + ox:x0 + ox + sw] = sub
    # contact shadow of the runner on the rug
    cs = ndi.shift(blur(placed[..., 3], 0.012 * size), (0.018 * size, 0), order=1)
    canvas[..., :3] *= (1.0 - 0.55 * np.clip(cs, 0, 1))[..., None]
    over(canvas, placed)
    return canvas


def composite_on(bg_rgb, prem, x, y):
    """bg_rgb float (H,W,3) 0..1; prem placed at integer x,y."""
    out = bg_rgb.copy()
    h, w = prem.shape[:2]
    reg = out[y:y + h, x:x + w]
    reg *= (1.0 - prem[..., 3:4])
    reg += prem[..., :3]
    return out


def save_rgb(a, path):
    Image.fromarray((np.clip(a, 0, 1) * 255 + 0.5).astype(np.uint8), "RGB").save(path)


def fit(prem, width):
    h, w = prem.shape[:2]
    return resize_prem(prem, (int(width), int(round(h * width / w))))


def previews(master, wordmark, out_dir):
    for name, col in (("preview_dark.png", (0x1b, 0x28, 0x38)), ("preview_light.png", (0xf2, 0xf2, 0xf2))):
        bg = np.ones((900, 1600, 3), np.float32) * (np.array(col, np.float32) / 255)
        m = fit(master, 1280)
        if m.shape[0] > 760:
            m = fit(master, 1280 * 760 / m.shape[0])
        save_rgb(composite_on(bg, m, (1600 - m.shape[1]) // 2, (900 - m.shape[0]) // 2),
                 os.path.join(out_dir, name))
    probe = np.asarray(Image.open(PROBE).convert("RGB"), np.float32) / 255
    m = fit(master, 0.55 * probe.shape[1])
    save_rgb(composite_on(probe, m, (probe.shape[1] - m.shape[1]) // 2, int(0.08 * probe.shape[0])),
             os.path.join(out_dir, "preview_game.png"))
    # small capsule legibility card: blurred + darkened crop of the game frame
    card_w, card_h = 462, 174
    src = Image.open(PROBE).convert("RGB")
    crop = src.crop((420, 300, 1500, 300 + int(1080 * card_h / card_w)))
    crop = crop.resize((card_w, card_h), Image.LANCZOS).filter(ImageFilter.GaussianBlur(5))
    bg = np.asarray(crop, np.float32) / 255 * 0.55
    wm = fit(wordmark, 0.85 * card_w)
    card = composite_on(bg, wm, (card_w - wm.shape[1]) // 2, (card_h - wm.shape[0]) // 2)
    save_rgb(card, os.path.join(out_dir, "preview_small.png"))
    Image.open(os.path.join(out_dir, "preview_small.png")).resize((231, 87), Image.LANCZOS).save(
        os.path.join(out_dir, "preview_tiny.png"))


def main():
    os.makedirs(OUT, exist_ok=True)
    t0 = time.time()
    word, info = render_wordmark()
    sub = render_subtitle(info["size"])
    size = info["size"]
    wordmark = with_shadow(word, size)
    master = with_shadow(lockup(word, info, sub), size)
    to_image(master).save(os.path.join(OUT, "master.png"))
    to_image(wordmark).save(os.path.join(OUT, "wordmark.png"))
    previews(master, wordmark, OUT)
    print(f"master {master.shape[1]}x{master.shape[0]}, wordmark {wordmark.shape[1]}x{wordmark.shape[0]}"
          f" -> {OUT}  ({time.time() - t0:.0f}s)")


if __name__ == "__main__":
    main()
