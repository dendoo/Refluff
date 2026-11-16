"""Refluff Steam "About This Game" section headers + a preview of the description column.

Per the steam-izdat store-page guide every feature block of the description opens with an IMAGE header (then a
looping clip, then 1-2 lines of text). Each header here is one transparent strip, 1560 x 120 RGBA, shown at
780 x 60 in Steam's 780 px description column and at ~390 x 30 on a phone:

  * the feature name in sentence case, cream Fredoka 700 with a soft pillow roll and a short wool pile
    (the logo's letters, trimmed so the counters of e / a / g stay open at 390 px), flush left with the text;
  * a soft cool drop shadow that lifts the cream off Steam's #1b2838 and the faded page background;
  * a thin rounded underline in the runner's brand teal with a faint cream running stitch (the logo's stitched
    edge), a small madder / gold lozenge as its start cap and the logo's pink foam bubbles at its end.
No frame, no buttons, no prices, no URLs (Valve's rules for description images). Alt text = the header text.

Outputs (deterministic, byte-identical on every run):
  SteamStore/description/<name>.png           English headers (upload to the description as [img] extras)
  SteamStore/description/<lang>/<name>.png    other languages, when HEADERS has them
  SteamStore/_preview/description_mock.png    the description column at 780 px (Read More fold at 850 px drawn
                                              in red) next to the same column at 390 px (phone)
Usage:  python Tools/steam_art/make_headers.py [--lang en] [--no-mock] [--check DIR]
        --check DIR also writes a 2x mock (headers at native size) and a strip sheet to DIR for review.
Needs Python 3.12 + numpy + Pillow + scipy + fontTools. Fonts ONLY from Tools/steam_art/fonts (OFL / Apache).
"""
import argparse
import os
import time

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage as ndi

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
FONTS = os.path.join(HERE, "fonts")
SHOTS = os.path.join(ROOT, "SteamStore", "screenshots")
PAGE_BG = os.path.join(ROOT, "SteamStore", "store", "page_background_1438x810.png")
OUT = os.path.join(ROOT, "SteamStore", "description")
MOCK = os.path.join(ROOT, "SteamStore", "_preview", "description_mock.png")

# ---- header texts: add a header or a language here ------------------------------------------------------
# Keys are the output file names (alt text on Steam = the text). English goes to SteamStore/description/,
# any other language to SteamStore/description/<lang>/ (Steam lets each language have its own description).
# NOTE: Fredoka covers Latin only (no Cyrillic, no CJK). A localised header needs another OFL font with those
# scripts, added to Tools/steam_art/fonts with its licence file and named in FONT_BY_LANG; the script refuses
# to render a text the chosen font has no glyphs for.
HEADERS = {
    "en": {
        "hdr_reveal": "Watch the pattern come back",
        "hdr_water": "Real water, real mud",
        "hdr_rugs": "A new rug every job",
        "hdr_paid": "Get paid, upgrade your kit",
    },
}
FONT_BY_LANG = {"en": ("Fredoka[wdth,wght].ttf", {"Weight": 700, "Width": 106})}

# ---- localised headers (2026-09-28, Docs/STORE_LOCALIZATION.md) ---------------------------------------------------
# The texts come from the hdr_* fields of SteamStore/localization/<code>.json (store_loc.py). They are shaped with
# HarfBuzz and rasterised with FreeType (Pillow here has no raqm: it would stack Thai tone marks on the vowels and drop
# kerning). English keeps the Pillow path above, byte-identical to the uploaded strips.
# Font per language: Fredoka where it has every glyph (the logo's face), else Nunito (Latin Extended, Vietnamese,
# Cyrillic), else Inter (Greek); the Noto families of the game's own UI fonts for CJK and Thai (Tools/loc/fonts_src).
FONT_DIRS = [FONTS, os.path.join(ROOT, "Tools", "loc", "fonts_src")]
LOC_LATIN_CHAIN = [("Fredoka[wdth,wght].ttf", {"wght": 700, "wdth": 106}), ("Nunito[wght].ttf", {"wght": 900}),
                   ("Inter-VF.ttf", {"wght": 800, "opsz": 32})]
LOC_SCRIPT_FONT = {"ja": ("NotoSansJP-VF.ttf", {"wght": 800}), "zh-Hans": ("NotoSansSC-VF.ttf", {"wght": 800}),
                   "zh-Hant": ("NotoSansTC-VF.ttf", {"wght": 800}), "ko": ("NotoSansKR-VF.ttf", {"wght": 800}),
                   "th": ("NotoSansThai-VF.ttf", {"wght": 800, "wdth": 100})}
LOC_SIZE = {"ja": 0.84, "zh-Hans": 0.84, "zh-Hant": 0.84, "ko": 0.84, "th": 0.92}   # ideographs fill the em
INK_MAX = 1496             # ink right edge limit (px): underline pad + foam bubbles + shadow must stay on the strip
INK_TOP, INK_BOT = 7, 95   # ink rows allowed for shaped text: the pile fringe needs ~6 px above, the underline sits at LINE_Y
MIN_FIT = 0.74             # smallest auto-fit scale before the script refuses a too-long header


def loc_headers():
    """{code: {name: text}} for every translated language (SteamStore/localization/<code>.json)."""
    import store_loc
    out = {}
    for code, steam, _ in store_loc.LANGS:
        p = os.path.join(store_loc.LOCDIR, code + ".json")
        if os.path.exists(p):
            f = store_loc.load(p)["fields"]
            out[code] = {store_loc.header_asset(h, code): f[h] for h in store_loc.HEADERS}
    return out


def font_path(file):
    for d in FONT_DIRS:
        p = os.path.join(d, file)
        if os.path.exists(p):
            return p
    raise SystemExit(f"font {file} not found in {FONT_DIRS}")


def loc_font(lang, texts):
    if lang in LOC_SCRIPT_FONT:
        return LOC_SCRIPT_FONT[lang]
    for ff in LOC_LATIN_CHAIN:
        if not any(missing_glyphs(ff[0], t) for t in texts):
            return ff
    raise SystemExit(f"no header font covers {lang}: {texts}")

# ---- strip geometry (source px; Steam shows everything at half size) -----------------------------------
W, H = 1560, 120
SS = 3                     # supersampling of every render
EM = 86                    # em in source px: cap ~60 -> ~30 CSS px caps at 780, ~15 at 390
TRACK = 0.022              # extra letter spacing (em): air for the pile, open counters at 390 px
X0 = 4                     # left edge of the ink (the pile's fringe and the shadow need a few px)
BASE = 74                  # baseline y (ascender ink from ~11, descender ink to ~93)
LINE_Y = 101.5             # underline centre y: clears the descenders of g / p / y and their pile
LINE_T = 7.0               # underline thickness (3.5 CSS px)
LINE_PAD = 22              # underline overshoot past the ink, before the bubbles
LOZENGE = (21.0, 17.0)     # start-cap lozenge w, h (px); the underline starts under its centre
BUBBLES = [(4, -3, 10.5), (19, -13, 7.0), (21, 2.5, 4.0)]      # (dx, dy, r) from the underline end: suds
SHADOW = [(2.2, 2.0, 0.8, 0.55), (0.9, 1.0, 0.3, 0.30)]        # (sigma, dy, dx, alpha): drop + contact
# short wool pile (lengths in em): the logo's locks trimmed to ~1 CSS px at 780 so the strip stays crisp at 390
PILE = dict(step=0.024, l_in=0.018, l_edge=0.030, strands=7, out=1.1, curl=0.12, edge_curl=0.7, clump=0.30,
            contrast=0.55, width=1.0, gapk=0.30, blur=0.25)

# ---- brand colours (Tools/steam_art/make_logo.py PAL) ------------------------------------------------------
PAL = {
    "wool_tip": "#fff8ea", "wool": "#f4e3c1", "wool_root": "#d2b088", "cream": "#f6e7c6", "cream_lo": "#d8bf93",
    "madder_hi": "#c23a2a", "madder": "#a8291f", "madder_lo": "#6e1914",
    "teal_hi": "#169488", "teal": "#0f6f69", "teal_lo": "#0a4d4b",
    "indigo": "#222849", "indigo_lo": "#11152c", "gold": "#f0a832",
    "foam_hi": "#fff3fc", "foam_mid": "#f1b6ea", "foam_rim": "#d77ee0", "foam_line": "#8e45ad",
    "shadow": "#05070d", "steam_bg": "#1b2838", "steam_text": "#acb2b8",
}


def hx(s):
    s = s.lstrip("#")
    return np.array([int(s[i:i + 2], 16) for i in (0, 2, 4)], np.float32) / 255.0


C = {k: hx(v) for k, v in PAL.items()}
LIGHT = np.array([-0.35, -0.72, 0.80], np.float32)
LIGHT /= np.linalg.norm(LIGHT)


# ---- helpers -----------------------------------------------------------------------------------------------
def font(size, axes=None, file="Fredoka[wdth,wght].ttf"):
    ft = ImageFont.truetype(font_path(file), int(round(size)))
    if axes:
        vals = []
        for a in ft.get_variation_axes():
            nm = a["name"].decode() if isinstance(a["name"], bytes) else a["name"]
            vals.append(axes.get(nm, a["default"]))
        ft.set_variation_by_axes(vals)
    return ft


def missing_glyphs(font_file, text):
    """Characters of text the font has no glyph for (fontTools cmap lookup)."""
    from fontTools.ttLib import TTFont
    cmap = TTFont(font_path(font_file), lazy=True).getBestCmap()
    return sorted({ch for ch in text if not ch.isspace() and ord(ch) not in cmap})


def smoothstep(e0, e1, x):
    t = np.clip((np.asarray(x, np.float32) - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def mix(a, b, t):
    t = np.asarray(t, np.float32)
    return a + (b - a) * (t[..., None] if t.ndim else t)


def blur(a, s):
    return ndi.gaussian_filter(a, s, mode="constant") if s > 0 else a


def signed_distance(mask):
    """+ inside, - outside, in pixels (mask: bool)."""
    din = ndi.distance_transform_edt(mask)
    dout = ndi.distance_transform_edt(~mask)
    return (din - dout + np.where(mask, -0.5, 0.5)).astype(np.float32)


def dot3(nx, ny, nz, v):
    return nx * v[0] + ny * v[1] + nz * v[2]       # elementwise: no BLAS, bit-stable on every machine


def over(dst, src):
    """Premultiplied RGBA 'over' in place."""
    dst *= (1.0 - src[..., 3:4])
    dst += src


def layer(color, alpha):
    a = np.clip(alpha, 0.0, 1.0)[..., None].astype(np.float32)
    c = np.broadcast_to(np.asarray(color, np.float32), a.shape[:2] + (3,))
    return np.concatenate([c * a, a], axis=2)


def paint(dst, col, alpha, y0=0, x0=0):
    """Premultiplied 'over' of a colour (or per-pixel colours) with coverage alpha, patch placed at (y0, x0)."""
    h, w = alpha.shape
    Y0, X0_ = max(0, y0), max(0, x0)
    Y1, X1 = min(dst.shape[0], y0 + h), min(dst.shape[1], x0 + w)
    if Y1 <= Y0 or X1 <= X0_:
        return
    a = np.clip(alpha[Y0 - y0:Y1 - y0, X0_ - x0:X1 - x0], 0, 1)[..., None]
    c = np.broadcast_to(np.asarray(col, np.float32), alpha.shape + (3,))[Y0 - y0:Y1 - y0, X0_ - x0:X1 - x0]
    reg = dst[Y0:Y1, X0_:X1]
    reg *= (1 - a)
    reg[..., :3] += c * a
    reg[..., 3:4] += a


def reduce_ss(prem, f=SS):
    h, w = prem.shape[0] // f, prem.shape[1] // f
    return prem[:h * f, :w * f].reshape(h, f, w, f, 4).mean(axis=(1, 3)).astype(np.float32)


def to_image(prem):
    a = prem[..., 3:4]
    rgb = np.where(a >= 0.5 / 255.0, prem[..., :3] / np.maximum(a, 1e-4), 0.0)
    out = np.concatenate([np.clip(rgb, 0, 1), np.clip(a, 0, 1)], axis=2)
    return Image.fromarray((out * 255.0 + 0.5).astype(np.uint8), "RGBA")


def from_image(im):
    a = np.asarray(im.convert("RGBA"), np.float32) / 255.0
    a[..., :3] *= a[..., 3:4]
    return a


def pillow_shade(sd, radius, sigma):
    """Soft top-left light on a rounded 'pillow' raised from a signed-distance shape (make_logo recipe)."""
    t = np.clip(sd / radius, 0.0, 1.0)
    h = ndi.gaussian_filter(radius * np.sqrt(1.0 - (1.0 - t) ** 2), sigma)
    gy, gx = np.gradient(h)
    n = np.sqrt(gx * gx + gy * gy + 1.0)
    wrap = np.clip((dot3(-gx / n, -gy / n, 1.0 / n, LIGHT) + 0.35) / 1.35, 0.0, 1.0)
    return h.astype(np.float32), wrap.astype(np.float32)


def smooth_noise(shape, scale, rng):
    """Low-frequency noise ~N(0,1), 'scale' = feature size in px."""
    lo = (max(2, int(shape[0] / scale) + 2), max(2, int(shape[1] / scale) + 2))
    n = ndi.gaussian_filter(rng.standard_normal(lo).astype(np.float32), 1.0)
    n /= n.std() + 1e-6
    return ndi.zoom(n, (shape[0] / lo[0], shape[1] / lo[1]), order=3)[:shape[0], :shape[1]]


# ---- letters ------------------------------------------------------------------------------------------------
def text_sd(text, lang):
    """Signed distance (SS res, + inside) of the laid-out text, ink flush at X0, baseline at BASE, TRACK em of
    extra spacing; and the ink's right edge in final px."""
    if lang != "en":
        return text_sd_shaped(text, lang)
    ffile, axes = FONT_BY_LANG[lang]
    miss = missing_glyphs(ffile, text)
    if miss:
        raise SystemExit(f"{ffile} has no glyphs for {''.join(miss)!r} in {text!r}: add an OFL font with that "
                         f"script to Tools/steam_art/fonts and name it in FONT_BY_LANG[{lang!r}]")
    em = EM * SS
    ft = font(em, axes, ffile)

    def draw(x):
        im = Image.new("L", (W * SS, H * SS), 0)
        d = ImageDraw.Draw(im)
        for i, ch in enumerate(text):
            if not ch.isspace():
                d.text((x + ft.getlength(text[:i]) + i * TRACK * em, BASE * SS), ch, font=ft, fill=255, anchor="ls")
        m = np.asarray(im) > 127              # one solid shape: Fredoka's overlapping contours merge
        return m, np.nonzero(m.any(axis=0))[0]

    m, cols = draw(X0 * SS)
    m, cols = draw(2 * X0 * SS - cols.min())  # getbbox() ignores the side bearing: measure the ink instead
    return signed_distance(m), (cols.max() + 1) / SS


def shape_text(text, ffile, var):
    """HarfBuzz run of text: (hb font, [(glyph id, pen x in font units, y offset)], upem). TRACK em of extra
    spacing goes after every spacing glyph (marks and cluster continuations get none)."""
    import uharfbuzz as hb
    face = hb.Face(hb.Blob.from_file_path(font_path(ffile)))
    f = hb.Font(face)
    f.set_variations(var)
    buf = hb.Buffer()
    buf.add_str(text)
    buf.guess_segment_properties()
    hb.shape(f, buf, {"kern": True, "liga": True})
    run, x = [], 0.0
    for inf, pos in zip(buf.glyph_infos, buf.glyph_positions):
        run.append((inf.codepoint, x + pos.x_offset, pos.y_offset))
        x += pos.x_advance
        if pos.x_advance:
            x += TRACK * face.upem
    return f, run, face.upem


def draw_run(pen, shaped, em, x0, y0):
    """Draws a shaped run into a y-up pen at em px, pen start x0, baseline y0."""
    from fontTools.pens.transformPen import TransformPen
    f, run, upem = shaped
    s = em / upem
    for gid, x, dy in run:
        f.draw_glyph_with_pen(gid, TransformPen(pen, (s, 0, 0, s, x0 + x * s, y0 + dy * s)))


def shaped_mask(shaped, em, x0, base):
    """Ink mask (SS res) of a shaped run at em px, pen start x0, baseline base (SS px from the top)."""
    from fontTools.pens.freetypePen import FreeTypePen
    f, run, upem = shaped
    m = np.zeros((H * SS, W * SS), bool)
    for g in run:                       # one glyph per raster: a whole line can overflow FreeType's raster pool
        pen = FreeTypePen(None)
        draw_run(pen, (f, [g], upem), em, x0, H * SS - base)        # FreeTypePen: y up from the bottom row
        m |= pen.array(width=W * SS, height=H * SS) > 0.5
    return m


def shaped_bounds(shaped, em):
    """Exact ink bounds (x0, top, x1, bottom) in SS px of a run at em px, pen at x 0, baseline at y 0, y down."""
    from fontTools.pens.boundsPen import BoundsPen
    pen = BoundsPen(None)
    draw_run(pen, shaped, em, 0, 0)
    xmin, ymin, xmax, ymax = pen.bounds
    return xmin, -ymax, xmax, -ymin


_LANG_FIT = {}


def lang_fit(lang, runs):
    """One type size and one baseline (SS px) for all headers of a language: the largest scale <= the nominal
    size at which every run keeps its ink within rows INK_TOP..INK_BOT and left of INK_MAX (an accented capital
    over a descender or a long phrase decides it), and the baseline that clears the tallest run (CJK / Thai: their
    tallest ink starts at row 11 like Latin caps). So the headers of one page never change size or height."""
    if lang in _LANG_FIT:
        return _LANG_FIT[lang]
    nominal = LOC_SIZE.get(lang, 1.0)
    scale = nominal
    for _ in range(24):
        b = [shaped_bounds(r, EM * SS * scale) for r in runs]           # (left, top, right, bot) per run
        fit = min(min((INK_MAX - X0) * SS / (r - l), (INK_BOT - INK_TOP) * SS / (bo - t + SS)) for l, t, r, bo in b)
        if fit < 0.999:
            scale *= fit * 0.995
        else:
            top, bot = min(x[1] for x in b), max(x[3] for x in b)
            base = max(BASE * SS, INK_TOP * SS - top, 11 * SS - top if lang in LOC_SIZE else 0)
            base = min(base, INK_BOT * SS - bot)
            if base + top >= INK_TOP * SS - 0.5:
                _LANG_FIT[lang] = (scale, base)
                return _LANG_FIT[lang]
            scale *= 0.98
        if scale < MIN_FIT * nominal:
            break
    raise SystemExit(f"the {lang} headers need less than {MIN_FIT} of the nominal size: shorten the longest")


def text_sd_shaped(text, lang):
    """text_sd for a localised header: HarfBuzz-shaped, at the language's shared size and baseline (lang_fit)."""
    texts = list(loc_headers()[lang].values())
    ffile, var = loc_font(lang, texts)
    miss = missing_glyphs(ffile, text)
    if miss:
        raise SystemExit(f"{ffile} has no glyphs for {''.join(miss)!r} in {text!r}")
    scale, base = lang_fit(lang, [shape_text(t, ffile, var) for t in texts])
    shaped = shape_text(text, ffile, var)
    left = shaped_bounds(shaped, EM * SS * scale)[0]
    m = shaped_mask(shaped, EM * SS * scale, X0 * SS - left, base)
    cols = np.nonzero(m.any(axis=0))[0]
    return signed_distance(m), (cols.max() + 1) / SS


def jitter_grid(mask_fn, h, w, step, rng):
    ys, xs = np.mgrid[0:h:step, 0:w:step].astype(np.float32)
    ys = (ys + rng.uniform(0, step, ys.shape)).ravel()
    xs = (xs + rng.uniform(0, step, xs.shape)).ravel()
    ok = (ys < h - 1) & (xs < w - 1)
    ys, xs = ys[ok], xs[ok]
    keep = mask_fn(ys.astype(int), xs.astype(int))
    return ys[keep], xs[keep]


def render_letters(sd, seed, P=PILE):
    """Cream wool letters (make_logo.render_pile, trimmed): a pillow-shaded base fill under clumped, curled
    short locks that comb down-right inside and fray a little outward at the rim. Premultiplied RGBA, SS res."""
    rng = np.random.default_rng(seed)
    size = EM * SS
    h, w = sd.shape
    k = P["contrast"]
    body_c, tip_c = C["wool"], C["wool_tip"]
    root_c = mix(body_c, C["wool_root"], k)
    radius = 0.07 * size
    hgt, shade = pillow_shade(sd, radius, 0.006 * size)
    sds = ndi.gaussian_filter(sd, 0.004 * size)
    gy, gx = np.gradient(sds)
    gl = np.sqrt(gx * gx + gy * gy) + 1e-6
    nx, ny = -gx / gl, -gy / gl                                    # outward normal
    fx = smooth_noise(sd.shape, 0.09 * size, rng) * 0.55 + 0.30     # interior comb, leaning down-right
    fy = smooth_noise(sd.shape, 0.09 * size, rng) * 0.55 + 0.70
    gap = ndi.maximum_filter(np.maximum(-sd, 0.0), size=int(0.09 * size) | 1)
    E = 0.05 * size                                                # edge-influence depth
    yy = np.arange(h, dtype=np.float32)[:, None]
    vgrad = smoothstep((BASE - 0.74 * EM) * SS, (BASE + 0.05 * EM) * SS, yy)   # 0 at the ascenders, 1 at base
    # base under the locks: bright top, warmer toward the baseline, deeper at the rim
    basec = mix(mix(tip_c, body_c, 0.5 + 0.5 * vgrad), root_c, 1.0 - smoothstep(0.0, E, sd))
    base = np.concatenate([np.clip(basec * (0.62 + 0.46 * shade)[..., None], 0, 1),
                           np.clip(sd + 0.5, 0.0, 1.0)[..., None]], axis=2)
    img = Image.fromarray((base * 255 + 0.5).astype(np.uint8), "RGBA")
    del base, basec, sds, gx, gy, gl

    step = P["step"] * size
    ty, tx = jitter_grid(lambda y, x: sd[y, x] > -0.5, h, w, step, rng)
    ey, ex = jitter_grid(lambda y, x: (sd[y, x] > 0.5) & (sd[y, x] < 0.35 * E), h, w, step * 0.65, rng)
    ty, tx = np.concatenate([ty, ey]), np.concatenate([tx, ex])
    iy, ix = ty.astype(int), tx.astype(int)
    s0 = sd[iy, ix]
    e = 1.0 - smoothstep(0.0, E, s0)                               # 1 at the edge
    nxs, nys = nx[iy, ix], ny[iy, ix]
    dx = nxs * P["out"] * e + fx[iy, ix] * (1.0 - e)
    dy = nys * P["out"] * e + fy[iy, ix] * (1.0 - e)
    dl = np.sqrt(dx * dx + dy * dy) + 1e-6
    dx, dy = dx / dl, dy / dl
    L = (P["l_in"] + (P["l_edge"] - P["l_in"]) * e ** 1.5) * size * rng.uniform(0.8, 1.2, len(ty))
    oy = np.clip((ty - nys * (s0 + 3)).astype(int), 0, h - 1)
    ox = np.clip((tx - nxs * (s0 + 3)).astype(int), 0, w - 1)
    cosn = np.clip(dx * nxs + dy * nys, 0.25, 1.0)
    L = np.minimum(L, (np.maximum(s0, 0) + P["gapk"] * gap[oy, ox] + 2.0) / cosn)   # counters stay open
    lf = (0.52 + 0.62 * shade[iy, ix]) * (1.05 - 0.10 * vgrad[iy, 0])
    order = np.argsort(hgt[iy, ix] + rng.uniform(0, 0.35 * radius, len(ty)), kind="stable")
    ty, tx, dx, dy, L, e, lf = (a[order] for a in (ty, tx, dx, dy, L, e, lf))
    nt, S = len(ty), P["strands"]
    rep = lambda a: np.repeat(a, S)
    Tx, Ty, Dx, Dy, Lt, Et, LF = map(rep, (tx, ty, dx, dy, L, e, lf))
    Curl, Warm = rep(rng.normal(0, P["curl"], nt)), rep(rng.uniform(-1, 1, nt))
    n = nt * S
    r = 0.5 * step * np.sqrt(rng.uniform(0, 1, n))
    a = rng.uniform(0, 2 * np.pi, n)
    x0, y0 = Tx + r * np.cos(a), Ty + r * np.sin(a)
    ang = np.arctan2(Dy, Dx) + rng.normal(0, 1, n) * (0.14 + 0.12 * Et)
    ln = Lt * rng.uniform(0.75, 1.1, n)
    cx_, cy_ = np.cos(ang), np.sin(ang)
    cl = np.clip(P["clump"] * (1.0 + 0.4 * Et), 0, 0.85) * rng.uniform(0.7, 1.0, n)   # strands of a lock meet
    tipx = (x0 + cx_ * ln) * (1 - cl) + (Tx + Dx * Lt) * cl
    tipy = (y0 + cy_ * ln) * (1 - cl) + (Ty + Dy * Lt) * cl
    bend = (Curl * (1.0 + P["edge_curl"] * Et) + rng.normal(0, 0.05, n)) * ln
    px, py = -cy_, cx_
    ts = (0.0, 0.3, 0.58, 0.8, 1.0)
    pts = np.empty((n, len(ts), 2), np.float32)
    for j, t in enumerate(ts):
        bow = 4 * t * (1 - t) * bend
        pts[:, j, 0] = x0 + (tipx - x0) * t + px * bow
        pts[:, j, 1] = y0 + (tipy - y0) * t + py * bow
    ins = [mix(root_c, body_c, 0.80), mix(root_c, body_c, 0.95), mix(body_c, tip_c, 0.45), mix(body_c, tip_c, 0.8)]
    edg = [root_c, mix(root_c, body_c, 0.55), mix(body_c, tip_c, 0.25), mix(body_c, tip_c, 0.55)]
    tint = np.stack([1.0 + 0.02 * Warm, np.ones(n, np.float32), 1.0 - 0.035 * Warm], axis=1)
    lit = (LF * rng.uniform(0.94, 1.05, n))[:, None]
    cols = np.empty((n, 4, 4), np.uint8)
    cols[..., 3] = 255
    for j in range(4):
        c = (ins[j][None] * (1 - Et[:, None]) + edg[j][None] * Et[:, None]) * tint * lit
        cols[:, j, :3] = np.clip(c * 255 + 0.5, 0, 255).astype(np.uint8)
    w0 = P["width"] * SS
    widths = [max(1, int(round(w0))), max(1, int(round(w0))), max(1, int(round(0.7 * w0))),
              max(1, int(round(0.55 * w0)))]
    line = ImageDraw.Draw(img).line
    for p, c in zip(pts.reshape(n, -1).tolist(), [[tuple(c) for c in row] for row in cols.tolist()]):
        line(p[0:4], fill=c[0], width=widths[0])
        line(p[2:6], fill=c[1], width=widths[1])
        line(p[4:8], fill=c[2], width=widths[2])
        line(p[6:10], fill=c[3], width=widths[3])
    prem = from_image(img)
    if P["blur"] > 0:                                              # soften the locks (final px)
        prem = ndi.gaussian_filter(prem, (P["blur"] * SS, P["blur"] * SS, 0))
    return prem, n


# ---- underline: teal runner thread with a running stitch, lozenge start cap, foam bubbles at the end ----------
def capsule_sd(xx, yy, xa, xb, y, t):
    """Signed distance (+ inside) of a horizontal rounded bar, cap centres xa..xb, centre y, thickness t."""
    return t / 2 - np.hypot(xx - np.clip(xx, xa, xb), yy - y)


def render_underline(x_end):
    """Premultiplied RGBA (SS res) of the underline from the lozenge to x_end (final px)."""
    s = SS
    out = np.zeros((H * s, W * s, 4), np.float32)
    lw, lh = LOZENGE
    xa, xb, y, t = (X0 + lw / 2) * s, x_end * s, LINE_Y * s, LINE_T * s
    y0, y1 = int(y - t), int(y + t) + 1                           # the bar's band of rows
    yy, xx = np.mgrid[y0:y1, 0:W * s].astype(np.float32) + 0.5
    bar = np.clip(capsule_sd(xx, yy, xa, xb, y, t) + 0.5, 0, 1)
    v = np.clip((yy - (y - t / 2)) / t, 0, 1)                      # 0 top .. 1 bottom of the bar
    col = mix(C["teal_hi"] * 1.10, C["teal"], smoothstep(0.1, 1.0, v))
    col = mix(col, C["teal_lo"], smoothstep(0.75, 1.0, v) * 0.6)
    paint(out, col, bar, y0, 0)
    # cream running stitch down the middle of the bar: the logo's stitched rug edge, faint
    pitch, dash, rt = 9.0 * s, 4.6 * s, 0.75 * s
    u = np.mod(xx - xa - 2.0 * s, pitch)
    dxd = np.maximum(np.abs(u - dash / 2) - (dash / 2 - rt), 0)
    st = np.clip(rt - np.hypot(dxd, yy - y + 0.25 * s) + 0.5, 0, 1)
    st *= smoothstep(xa + lw * 0.6 * s, xa + lw * 1.0 * s, xx) * (1 - smoothstep(xb - 8 * s, xb - 3 * s, xx))
    paint(out, C["cream"], st * 0.62, y0, 0)
    # lozenge start cap: indigo keyline, madder field, gold heart (the runner's medallion, in miniature)
    cx, cy = xa, y
    im = Image.new("L", (W * s, H * s), 0)
    for key, (fw, fh) in (("indigo_lo", (lw, lh)), ("madder_hi", (lw - 4.2, lh - 4.2)), ("gold", (4.6, 4.6))):
        im.paste(0, (0, 0, W * s, H * s))
        hw, hh = fw * s / 2, fh * s / 2
        ImageDraw.Draw(im).polygon([(cx - hw, cy), (cx, cy - hh), (cx + hw, cy), (cx, cy + hh)], fill=255)
        paint(out, C[key], np.asarray(im, np.float32) / 255.0)
    for dx, dy, r in sorted(BUBBLES, key=lambda b: b[1] - 0.2 * b[2]):     # back (upper) bubbles first
        draw_bubble(out, (x_end + dx) * s, (LINE_Y + dy) * s, r * s, 1.7 * s)
    return out


def draw_bubble(buf, cx, cy, r, lw):
    """One glossy pink-violet soap bubble (the logo's foam): violet keyline, window glint upper left."""
    R = r + lw + 2
    bx, by = int(cx - R), int(cy - R)
    n = int(2 * R) + 3
    yy, xx = np.mgrid[0:n, 0:n].astype(np.float32) + 0.5
    d = np.hypot(xx - (cx - bx), yy - (cy - by))
    paint(buf, C["foam_line"], np.clip(r + lw - d + 0.5, 0, 1), by, bx)
    nx, ny = (xx - (cx - bx)) / r, (yy - (cy - by)) / r
    nr = np.clip(np.hypot(nx, ny), 0, 1)
    nz = np.sqrt(1 - nr * nr)
    lit = np.clip(dot3(nx, ny, nz, LIGHT), 0, 1)
    col = mix(C["foam_hi"], C["foam_mid"], smoothstep(0.15, 0.95, nr) ** 1.2)
    col = mix(col, C["foam_rim"], smoothstep(0.72, 1.0, nr) * 0.8)
    col = col * (0.82 + 0.24 * lit)[..., None]
    dirk = smoothstep(0.35, 0.85, (nx * 0.6 + ny * 0.8) / (nr + 1e-4))
    band = smoothstep(0.62, 0.74, nr) * (1 - smoothstep(0.86, 0.94, nr))
    col = mix(col, C["foam_hi"], dirk * band * 0.6)                     # inner reflection, lower right
    sx, sy = nx + 0.34, ny + 0.38
    u, v = (sx - sy) * 0.7071, (sx + sy) * 0.7071
    win = 1 - smoothstep(0.72, 1.0, np.hypot(u / 0.32, v / 0.17))      # window glint, upper left
    col = mix(col, np.ones(3, np.float32), np.clip(win * 0.95, 0, 1))
    paint(buf, np.clip(col, 0, 1), np.clip(r - d + 0.5, 0, 1), by, bx)


# ---- one header strip ------------------------------------------------------------------------------------------
def render_header(text, lang, seed):
    """Final 1560 x 120 premultiplied RGBA strip; returns (prem, info string)."""
    sd, ink_r = text_sd(text, lang)
    letters, n = render_letters(sd, seed)
    x_end = ink_r + LINE_PAD
    fg = render_underline(x_end)
    over(fg, letters)                                              # letters above the underline's ends
    out = np.zeros_like(fg)
    for sig, dy, dx, a in SHADOW:                                  # soft cool drop shadow + contact shadow
        s = ndi.shift(blur(fg[..., 3], sig * SS), (dy * SS, dx * SS), order=1)
        over(out, layer(C["shadow"], np.clip(s, 0, 1) * a))
    over(out, fg)
    fin = reduce_ss(out)
    a = fin[..., 3]
    rows, cols = np.nonzero(a > 1 / 255)
    assert rows.min() > 0 and rows.max() < H - 1 and cols.max() < W - 1,         f"{text!r} clips its strip: ink rows {rows.min()}..{rows.max()}, cols ..{cols.max()}"
    return fin, f"ink x {cols.min()}..{cols.max()} y {rows.min()}..{rows.max()}, {n} strands"


# ---- Steam description-column mock -------------------------------------------------------------------------------
# (header, clip poster, bold lead, text) per block, in the page order (2026-09-26: the tool relay opens the page with no
# header; the reveal follows, so both 2.4:1 clips sit above the fold). The posters are the clips' first frames.
BLOCKS = [
    (None, "refluff_clip3_tools_poster.png", "Ten tools, one right order.",
     " Chip the dried crust with the scraper, foam the grime, scrub it in with the rotary brush, then plow the slurry "
     "off the rug with the squeegee."),
    ("hdr_reveal", "refluff_clip1_reveal_poster.png", "",
     "The wet vacuum pulls the murky slurry out of the pile and the pattern comes back, one satisfying pass at a time."),
    ("hdr_water", "refluff_clip2_water_poster.png", "",
     "Water soaks in and wicks through the pile, dry dust turns to mud the moment it gets wet, and foam heaps up and "
     "slowly settles. Nothing is a mask being erased: a fluid simulation runs under every fibre."),
    ("hdr_rugs", "refluff_clip4_rugs_poster.png", "",
     "Hand the clean rug over and the next one flies in. Every job rolls its own mix of dust, grime, crust and "
     "spills over a real museum carpet."),
    ("hdr_paid", "refluff_clip5_paid_poster.png", "",
     "The pay slip prints line by line: speed bonus, spotless, bone dry, combo. Spend it on POWER and AREA upgrades "
     "for any tool, or on inspection lamps that show the moisture and dirt your eyes miss."),
]
FOLD = 850          # "Read More" fold, CSS px from the top of the description block (its h2 included)
LINE_H = 20         # body line height, CSS px (14 px text)
IMG_GAP = 4         # descender gap under each inline [img] line, CSS px
BODY_FONT = "Rubik[wght].ttf"   # OFL stand-in for Motiva Sans / Arial


def clip_poster(name, w):
    """A clip's poster (its first frame, written by make_clips.py) scaled to w px, keeping the clip's aspect."""
    im = Image.open(os.path.join(OUT, name)).convert("RGB")
    return im.resize((w, int(round(im.height * w / im.width))), Image.LANCZOS)


def scaled(img, w):
    """Resize an RGBA strip in premultiplied space (no dark fringe), like a browser does."""
    h = int(round(img.height * w / img.width))
    return img.convert("RGBa").resize((w, h), Image.LANCZOS).convert("RGBA")


def wrap(runs, fonts, width):
    """Greedy word wrap of [(bold, text)] runs -> lines of [(bold, word)]."""
    words = [(b, wd) for b, t in runs for wd in t.split(" ") if wd]
    lines, cur, cw = [], [], 0.0
    for b, wd in words:
        add = (fonts[False].getlength(" ") if cur else 0.0) + fonts[b].getlength(wd)
        if cur and cw + add > width:
            lines.append(cur)
            cur, cw, add = [], 0.0, fonts[b].getlength(wd)
        cur.append((b, wd))
        cw += add
    if cur:
        lines.append(cur)
    return lines


def column(strips, col, s=1, fold=True):
    """The About This Game column, col CSS px wide at device pixel ratio s, on a transparent canvas."""
    rub = lambda px, wt: font(px * s, {"Weight": wt}, BODY_FONT)
    fonts, h2f, lab = {False: rub(14, 400), True: rub(14, 700)}, rub(14, 400), rub(11, 500)
    ops, y = [], 0.0

    def h2(title):
        nonlocal y
        ops.append(("h2", y, title))
        y += 34

    def text(runs):
        nonlocal y
        for ln in wrap(runs, fonts, col * s):
            ops.append(("text", y, ln))
            y += LINE_H

    def image(img, label=None):
        nonlocal y
        ops.append(("img", y, (img, label)))
        y += img.height / s + IMG_GAP

    h2("ABOUT THIS GAME")
    for i, (hdr, poster, lead, body) in enumerate(BLOCKS):
        if hdr:
            y += LINE_H                                            # the blank line between blocks
            image(scaled(strips[hdr], col * s))
        image(clip_poster(poster, col * s))
        text([(True, lead), (False, body)])
    y += LINE_H
    h2("HEADPHONES ON")
    text([(False, "Every machine is a recording of a real one: motors spool up and wind down, the vacuum tube "
                  "rattles the moment it swallows a crumb, water hisses, foam fizzes, bristles drag.")])
    page = Image.new("RGBA", (col * s, int((y + 8) * s)), (0, 0, 0, 0))
    dr = ImageDraw.Draw(page)
    for kind, yc, p in ops:
        py = int(round(yc * s))
        if kind == "h2":                                           # Steam h2: caps, 2 px tracking, blue rule
            x = 0
            for ch in p:
                dr.text((x, py + 4 * s), ch, font=h2f, fill="#ffffff")
                x += h2f.getlength(ch) + 2 * s
            rule = np.zeros((s, col * s, 4), np.uint8)
            rule[..., :3] = (59, 110, 140)
            rule[..., 3] = (np.clip(1 - np.linspace(0, 1, col * s) / 0.85, 0, 1) * 255).astype(np.uint8)
            page.alpha_composite(Image.fromarray(rule, "RGBA"), (0, py + 25 * s))
        elif kind == "text":
            x = 0.0
            for j, (b, wd) in enumerate(p):
                x += fonts[False].getlength(" ") if j else 0.0
                dr.text((x, py + LINE_H * s / 2), wd, font=fonts[b], fill="#ffffff" if b else PAL["steam_text"],
                        anchor="lm")
                x += fonts[b].getlength(wd)
        else:
            img, label = p
            page.alpha_composite(img.convert("RGBA"), (0, py))
            if label:
                tw = lab.getlength(label) + 12 * s
                dr.rounded_rectangle([8 * s, py + 8 * s, 8 * s + tw, py + 26 * s], 4 * s, fill=(10, 14, 20, 170))
                dr.text((14 * s, py + 17 * s), label, font=lab, fill=(220, 224, 228), anchor="lm")
    if fold:
        fy = FOLD * s
        dr.rectangle([0, fy, col * s, fy + s - 1], fill="#ff3b3b")
        t = f"Read More fold, {FOLD} px"
        dr.text((col * s, fy - 3 * s), t, font=lab, fill="#ff5a5a", anchor="rb")
    return page


def steam_page(cols, labels, margin=40, gap=64):
    """Columns side by side on Steam's #1b2838 with the game's page background faded in behind the top."""
    top = 34
    w = margin * 2 + sum(c.width for c in cols) + gap * (len(cols) - 1)
    h = top + margin + max(c.height for c in cols)
    page = Image.new("RGB", (w, h), PAL["steam_bg"])
    bg = Image.open(PAGE_BG).convert("RGB")
    bg = bg.resize((w, int(round(bg.height * w / bg.width))), Image.LANCZOS)
    fade = (0.22 * (1 - smoothstep(0.0, 0.85, np.linspace(0, 1, bg.height))))[:, None, None]
    base = np.asarray(page, np.float32)
    base[:bg.height] = base[:bg.height] * (1 - fade) + np.asarray(bg, np.float32) * fade
    page = Image.fromarray(np.round(base).astype(np.uint8), "RGB").convert("RGBA")
    dr = ImageDraw.Draw(page)
    lab = font(13, {"Weight": 500}, BODY_FONT)
    x = margin
    for c, t in zip(cols, labels):
        dr.text((x, 12), t, font=lab, fill="#66c0f4")
        page.alpha_composite(c, (x, top))
        x += c.width + gap
    return page.convert("RGB")


def sheet(strips):
    """Every strip at native size (2x), at 780 and at 390 px, on #1b2838: the review sheet for --check."""
    rows = [(im, 1560) for im in strips.values()] + [(im, 780) for im in strips.values()] + \
           [(im, 390) for im in strips.values()]
    hs = [int(round(H * w / W)) + 10 for _, w in rows]
    page = Image.new("RGBA", (W + 40, sum(hs) + 30), PAL["steam_bg"])
    y = 15
    for (im, w), hh in zip(rows, hs):
        page.alpha_composite(im if w == W else scaled(im, w), (20, y))
        y += hh
    return page.convert("RGB")


def main():
    import zlib
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    headers = dict(HEADERS)
    headers.update(loc_headers())
    ap.add_argument("--lang", nargs="*", default=sorted(headers), help="languages to render (default: all)")
    ap.add_argument("--no-mock", action="store_true", help="headers only")
    ap.add_argument("--check", metavar="DIR", help="also write mock2x.png and sheet.png to DIR for review")
    ap.add_argument("--sheet", metavar="PNG", help="write every rendered language's strips at 780 px to PNG")
    a = ap.parse_args()
    t0 = time.time()
    english, rendered = {}, {}
    for lang in a.lang:
        out = OUT if lang == "en" else os.path.join(OUT, lang)
        os.makedirs(out, exist_ok=True)
        for name, text in headers[lang].items():
            fin, info = render_header(text, lang, zlib.crc32(name.encode()) & 0xFFFF)
            img = to_image(fin)
            path = os.path.join(out, name + ".png")
            img.save(path, optimize=True)
            print(f"{os.path.relpath(path, ROOT)}  {img.width}x{img.height}  {os.path.getsize(path) / 1024:.0f} KB"
                  f"  {info}  '{text}'")
            if lang == "en":
                english[name] = img
            rendered.setdefault(lang, []).append(img)
    if a.sheet:
        lab = font(15, {"Weight": 500}, BODY_FONT)
        rowh = H // 2 + 8
        page = Image.new("RGBA", (W // 2 + 110, 16 + sum(len(v) for v in rendered.values()) * rowh), PAL["steam_bg"])
        dr = ImageDraw.Draw(page)
        y = 8
        for lang, imgs in rendered.items():
            dr.text((10, y + 18), lang, font=lab, fill="#66c0f4")
            for im in imgs:
                page.alpha_composite(scaled(im, W // 2), (100, y))
                y += rowh
        page.convert("RGB").save(a.sheet)
        print("sheet ->", a.sheet)
    if english and not a.no_mock:
        os.makedirs(os.path.dirname(MOCK), exist_ok=True)
        page = steam_page([column(english, 780), column(english, 390, fold=False)],
                          ["Steam description column, 780 px (desktop)", "390 px (phone)"])
        page.save(MOCK, optimize=True)
        print(f"{os.path.relpath(MOCK, ROOT)}  {page.width}x{page.height}")
        if a.check:
            os.makedirs(a.check, exist_ok=True)
            steam_page([column(english, 780, 2)], ["2x"], 80).save(os.path.join(a.check, "mock2x.png"))
            sheet(english).save(os.path.join(a.check, "sheet.png"))
            print("review files written to", a.check)
    print(f"done in {time.time() - t0:.1f} s")


if __name__ == "__main__":
    main()
