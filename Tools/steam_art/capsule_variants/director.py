#!/usr/bin/env python3
"""Refluff Steam capsule set - approach "ART DIRECTOR".

Per asset it picks the key-art plate and crop that read best at that size and aspect. One brand
treatment runs through all of them:
  * the final logo, never redrawn: only scaled (Lanczos, premultiplied), placed, and given the
    same soft dark glow + drop shadow (sized from the logo height), and
  * the same finish: a calm "bed" behind the logo (slightly darker and defocused art), then a gentle
    vignette, a light S-curve and a touch of saturation, identical numbers everywhere.
The small capsule is its own design: the soft-render wordmark nearly fills it, over a heavily
simplified crop of the before/after seam (grime | pink foam | vivid rug) with the blue brush kept
as a readable shape.

Run from the project root:   python Tools/steam_art/capsule_variants/director.py
Writes SteamStore/_work/capsules/director/<asset>.png and _sheet.png (review sheet).
Deterministic: no clock, dithering noise is seeded from the asset name.
"""
import os
import zlib

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy.ndimage import gaussian_filter

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..'))
KEYART = os.path.join(ROOT, 'SteamStore', '_work', 'keyart')
LOGODIR = os.path.join(ROOT, 'SteamStore', 'logo')
FONTDIR = os.path.join(ROOT, 'Tools', 'steam_art', 'fonts')
OUT = os.path.join(ROOT, 'SteamStore', '_work', 'capsules', 'director')

LANCZOS = Image.Resampling.LANCZOS
LUMA = np.array([0.2126, 0.7152, 0.0722], np.float32)
SHADOW_RGB = np.array([0.055, 0.035, 0.045], np.float32)   # warm near-black, not grey
_cache = {}


# ---------------------------------------------------------------- sources
def plate(name):
    """Key-art plate (PIL RGB), cached."""
    if name not in _cache:
        _cache[name] = Image.open(os.path.join(KEYART, name + '.png')).convert('RGB')
    return _cache[name]


def logo(name):
    """Logo PNG trimmed to its visible pixels (PIL RGBA), cached."""
    key = 'logo:' + name
    if key not in _cache:
        im = Image.open(os.path.join(LOGODIR, name + '.png')).convert('RGBA')
        _cache[key] = im.crop(im.getchannel('A').point(lambda v: 255 if v > 2 else 0).getbbox())
    return _cache[key]


def frame(name, size, cx, cy, wf):
    """Crop plate `name` to the aspect of `size` and resample it there.
    (cx, cy) = crop centre in plate fractions, wf = crop width / plate width. The box is clamped
    inside the plate (shrunk if the aspect needs more than the plate has). Returns (PIL image,
    (x0, y0, w, h) of the box in plate fractions) so plate-space bboxes can be mapped into it."""
    im = plate(name)
    W, H = im.size
    tw, th = size
    cw = wf * W
    ch = cw * th / tw
    if ch > H:
        ch = H
        cw = ch * tw / th
    if cw > W:
        cw = W
        ch = cw * th / tw
    x0 = min(max(cx * W - cw / 2, 0.0), W - cw)
    y0 = min(max(cy * H - ch / 2, 0.0), H - ch)
    out = im.resize(size, LANCZOS, box=(x0, y0, x0 + cw, y0 + ch))
    return out, (x0 / W, y0 / H, cw / W, ch / H)


def to_f(im):
    return np.asarray(im, np.float32) / 255.0


def to_img(a, seed_name):
    """Float RGB -> 8-bit PIL with seeded +-0.5 LSB dither (no banding in the soft gradients)."""
    rng = np.random.default_rng(zlib.crc32(seed_name.encode()))
    n = rng.random(a.shape[:2], np.float32)[..., None] - 0.5
    return Image.fromarray(np.clip(np.floor(a * 255.0 + 0.5 + n), 0, 255).astype(np.uint8), 'RGB')


def blur(a, sigma):
    if sigma <= 0:
        return a
    if a.ndim == 2:
        return gaussian_filter(a, sigma, mode='nearest')
    return np.stack([gaussian_filter(a[..., c], sigma, mode='nearest') for c in range(a.shape[2])], -1)


# ---------------------------------------------------------------- grade (one set of numbers for the brand)
def finish(a, vignette=0.20, curve=0.10, sat=1.04):
    """Gentle vignette (elliptical, frame-relative), light S-curve, a touch of saturation."""
    h, w = a.shape[:2]
    y, x = np.mgrid[0:h, 0:w].astype(np.float32)
    nx = (x + 0.5) / w * 2 - 1
    ny = (y + 0.5) / h * 2 - 1
    r = np.sqrt(nx * nx + ny * ny) / np.sqrt(2.0)
    v = 1.0 - vignette * np.clip((r - 0.40) / 0.60, 0, 1) ** 1.7
    a = a * v[..., None]
    s = a * a * (3 - 2 * a)
    a = a + curve * (s - a)
    L = (a @ LUMA)[..., None]
    a = L + sat * (a - L)
    return np.clip(a, 0, 1)


# ---------------------------------------------------------------- logo (scale, place, glow/shadow only)
def scaled_logo(name, width):
    """Logo resampled to `width` px. Pillow resamples RGBA premultiplied, so no dark fringe."""
    lg = logo(name)
    return lg.resize((int(width), max(1, int(round(lg.height * width / lg.width)))), LANCZOS)


def _paste(dst, src, x0, y0):
    """dst[y0:, x0:] = src with clipping at the canvas border (both float arrays)."""
    h, w = dst.shape[:2]
    sh, sw = src.shape[:2]
    ax, ay = max(x0, 0), max(y0, 0)
    bx, by = min(x0 + sw, w), min(y0 + sh, h)
    if bx > ax and by > ay:
        dst[ay:by, ax:bx] = src[ay - y0:by - y0, ax - x0:bx - x0]


def _shift_down(m, dy):
    if dy <= 0:
        return m
    out = np.zeros_like(m)
    out[dy:] = m[:-dy]
    return out


def place_logo(a, name, width, cx, cy, bed=0.22, bed_blur=0.0, glow=1.0):
    """Lay logo `name` (`width` px wide, centred at (cx, cy) in canvas fractions) over float RGB `a`.
    Order: the bed (art behind the logo darkened by `bed` and defocused by `bed_blur` x logo height
    under a broad soft mask), then the soft dark glow and the tight drop shadow (both sized from the
    logo height, strength x `glow`), then the logo pixels themselves, untouched.
    Returns (array, (x0, y0, x1, y1) px bbox of the logo)."""
    h, w = a.shape[:2]
    lg = scaled_logo(name, width)
    lw, lh = lg.size
    x0 = int(round(cx * w - lw / 2))
    y0 = int(round(cy * h - lh / 2))
    la = np.asarray(lg, np.float32) / 255.0
    A = np.zeros((h, w), np.float32)
    C = np.zeros((h, w, 3), np.float32)
    _paste(A, la[..., 3], x0, y0)
    _paste(C, la[..., :3], x0, y0)

    if bed > 0 or bed_blur > 0:
        M = np.clip(blur(A, 0.22 * lh) * 1.7, 0, 1)[..., None]
        if bed_blur > 0:
            a = a + (blur(a, bed_blur * lh) - a) * M
        a = a * (1.0 - bed * M)
    g1 = blur(A, 0.075 * lh)                                    # wide soft glow
    g2 = _shift_down(blur(A, 0.020 * lh), int(round(0.030 * lh)))  # tight drop shadow
    for g, k in ((g1, 0.50), (g2, 0.60)):
        s = np.clip(g * k * glow, 0, 1)[..., None]
        a = a + (SHADOW_RGB - a) * s
    a = a + (C - a) * A[..., None]
    return np.clip(a, 0, 1), (x0, y0, x0 + lw, y0 + lh)


def margin_report(name, size, bb, frac=0.04):
    """Warn when the logo enters the outer `frac` of the frame (Steam rounds corners/overlays badges)."""
    w, h = size
    x0, y0, x1, y1 = bb
    m = (x0 / w, y0 / h, 1 - x1 / w, 1 - y1 / h)
    flag = '' if min(m) >= frac - 1e-6 else '  <-- inside the outer %d %%' % round(frac * 100)
    print('  %-26s logo %4dx%-4d margins L%.3f T%.3f R%.3f B%.3f%s'
          % (name, x1 - x0, y1 - y0, m[0], m[1], m[2], m[3], flag))


def build(name, size, plate_name, cx, cy, wf, logo_name=None, logo_w=0.0, logo_c=(0.5, 0.5),
          bed=0.22, bed_blur=0.0, glow=1.0, pre=None, grade=True, margin=0.04):
    """One asset: plate crop -> optional `pre` (float array fn) -> brand finish -> logo."""
    im, _ = frame(plate_name, size, cx, cy, wf)
    a = to_f(im)
    if pre is not None:
        a = pre(a)
    if grade:
        a = finish(a)
    if logo_name:
        a, bb = place_logo(a, logo_name, round(logo_w * size[0]), logo_c[0], logo_c[1],
                           bed=bed, bed_blur=bed_blur, glow=glow)
        margin_report(name, size, bb, margin)
    return to_img(a, name)


# ---------------------------------------------------------------- small capsule: simplified art
def simplify(a, med=7, sigma=2.0):
    """Painterly simplification: a median (keeps the big shapes - brush, foam, seam - with clean
    edges, drops the pile texture that turns to noise at 120x45), then a soft Gaussian."""
    from scipy.ndimage import median_filter
    m = np.stack([median_filter(a[..., c], size=med, mode='nearest') for c in range(3)], -1)
    return blur(m, sigma)


# ---------------------------------------------------------------- the set (art direction per asset)
# plate, crop centre (cx, cy) + crop width fraction wf, logo, logo width / canvas width, logo centre.
# Plate bboxes quoted from plates.json. Every logo keeps >= 4 % from the frame edges.
SPECS = {
    # Store header: the low seam view (hero2). Big lockup on the calm grime at left, brush on the
    # seam at 0.59-0.81, the foam band trailing behind it, vivid rug on the right = the whole story
    # left to right, the way the eye reads. Brush centre (plate x 0.495) lands at 0.70 of the frame.
    'header_capsule_920x430': dict(size=(920, 430), plate='hero2_split_brush_v2',
                                   cx=0.357, cy=0.50, wf=0.691, logo='refluff_logo',
                                   logo_w=0.53, logo_c=(0.305, 0.47)),
    # Main capsule (carousel, next to screenshots): the 3/4 seam view (wide2) - the foam band
    # recedes up the seam, the medallion is vivid at the right. Lockup top-left over grime/foam.
    'main_capsule_1232x706': dict(size=(1232, 706), plate='wide2_split_seam_v2',
                                  cx=0.40, cy=0.53, wf=0.76, logo='refluff_logo',
                                  logo_w=0.46, logo_c=(0.275, 0.175)),
    # Vertical capsule (sale pages, shown large): portrait across the seam (tallB), lockup in the
    # calm out-of-focus grime of the top third, brush centred, medallion in the foreground.
    'vertical_capsule_748x896': dict(size=(748, 896), plate='tallB_brush_seam_v2',
                                     cx=0.50, cy=0.45, wf=1.0, logo='refluff_logo',
                                     logo_w=0.86, logo_c=(0.50, 0.165)),
    # Library capsule (grid, often 300x450 or less): the macro portrait (tallD) - the brush is 44 %
    # of the width and the foam mound is huge, so it still reads when small; grime top, vivid bottom.
    'library_capsule_600x900': dict(size=(600, 900), plate='tallD_macro_brush_v2',
                                    cx=0.50, cy=0.50, wf=1.0, logo='refluff_logo',
                                    logo_w=0.86, logo_c=(0.50, 0.15)),
    # Library header (Recent Games shelf): same layout as the store header but the front-right view
    # with the glossy foam bank (hero1), so the library side of the brand has its own frame. Tighter
    # crop (brush 27 % wide) pulled down so the vivid rug fills the lower right.
    'library_header_920x430': dict(size=(920, 430), plate='hero1_foam_bank_v2',
                                   cx=0.380, cy=0.56, wf=0.60, logo='refluff_logo',
                                   logo_w=0.50, logo_c=(0.29, 0.45)),
    # Event cover: the macro (wide5) - most tactile plate, still dirty | clean across the brush.
    'event_cover_800x450': dict(size=(800, 450), plate='wide5_macro_brush_v2',
                                cx=0.50, cy=0.50, wf=1.0, logo='refluff_logo',
                                logo_w=0.46, logo_c=(0.27, 0.165)),
    # Event header: the whole hero2 frame; lockup left-centre on the grime (6 % in from the left:
    # the event page trims the sides on narrow windows), brush at the centre.
    'event_header_1920x622': dict(size=(1920, 622), plate='hero2_split_brush_v2',
                                  cx=0.50, cy=0.50, wf=1.0, logo='refluff_logo',
                                  logo_w=0.33, logo_c=(0.225, 0.50)),
    # Small capsule: the soft-render wordmark nearly fills it (92 % wide). Behind it the macro seam
    # (wide5, whole width) framed so the brush cap hides behind the word and only the blue skirt
    # shows under it: grime left, pink foam top right, vivid rug bottom right - the full story as
    # colour, painterly-simplified so nothing competes with the letters. (Tried: plain blur - reads
    # as a generic colour field; tallD - the white cap pokes out above the word as a blob.)
    'small_capsule_462x174': dict(size=(462, 174), plate='wide5_macro_brush_v2',
                                  cx=0.47, cy=0.475, wf=1.0, logo='refluff_wordmark_small',
                                  logo_w=0.92, logo_c=(0.50, 0.385), bed=0.20, glow=1.15,
                                  pre=lambda a: simplify(a, 7, 2.0), margin=0.035),
}

HERO_SAFE = (860, 380)   # centred safe area of the 3840x1240 library hero


def build_hero():
    """Library hero: hero2 downscaled exactly 1.5x (5760x1860 -> 3840x1240), same grade, no logo,
    no text. The brush (plate bbox x 0.42-0.57) sits on the centre line; bottom-left is calm grime
    for Steam's own logo overlay."""
    im, _ = frame('hero2_split_brush_v2', (3840, 1240), 0.5, 0.5, 1.0)
    return to_img(finish(to_f(im)), 'library_hero_3840x1240')


def build_page_background():
    """Store page background: the evenly defocused, desaturated pile plate (exactly 2x), with only
    the brand vignette (no S-curve, no saturation: it has to stay calm and low contrast)."""
    im, _ = frame('background_soft_pile_v2', (1438, 810), 0.5, 0.5, 1.0)
    a = finish(to_f(im), vignette=0.14, curve=0.0, sat=1.0)
    L = (a @ LUMA).mean()
    a = L + 0.82 * (a - L)            # flatter still: page text and Steam's fade go on top
    return to_img(np.clip(a * 0.92, 0, 1), 'page_background_1438x810')


def build_all():
    os.makedirs(OUT, exist_ok=True)
    out = {}
    print('logo placement:')
    for name, s in SPECS.items():
        out[name] = build(name, s['size'], s['plate'], s['cx'], s['cy'], s['wf'], s['logo'],
                          s['logo_w'], s['logo_c'], bed=s.get('bed', 0.22), glow=s.get('glow', 1.0),
                          pre=s.get('pre'), margin=s.get('margin', 0.04))
    out['library_hero_3840x1240'] = build_hero()
    out['page_background_1438x810'] = build_page_background()
    for name, im in out.items():
        exp = tuple(int(v) for v in name.rsplit('_', 1)[1].split('x'))
        assert im.size == exp and im.mode == 'RGB', (name, im.size, im.mode)
        im.save(os.path.join(OUT, name + '.png'), optimize=False)
    return out


# ---------------------------------------------------------------- review sheet
def _font(px):
    for f in ('Fredoka[wdth,wght].ttf', 'Baloo2[wght].ttf'):
        p = os.path.join(FONTDIR, f)
        if os.path.exists(p):
            return ImageFont.truetype(p, px)
    return ImageFont.load_default()


def hero_with_library_logo(hero):
    """The hero as the Steam library shows it: library logo pinned bottom-left (trimmed, 1:1 at
    its 1280 px width budget, 4 % padding), plus the centred 860x380 safe area outlined."""
    im = hero.convert('RGBA')
    lg = logo('refluff_logo_1280x720')
    W, H = im.size
    pad = int(0.04 * W)
    im.alpha_composite(lg, (pad, H - int(0.06 * H) - lg.height))
    d = ImageDraw.Draw(im)
    sx, sy = (W - HERO_SAFE[0]) // 2, (H - HERO_SAFE[1]) // 2
    d.rectangle([sx, sy, sx + HERO_SAFE[0] - 1, sy + HERO_SAFE[1] - 1], outline=(60, 255, 120, 255), width=4)
    d.text((sx + 8, sy + 6), 'safe 860x380', fill=(60, 255, 120, 255), font=_font(30))
    return im.convert('RGB')


def build_sheet(out):
    """Every asset at its real size, plus the small capsule at 231x87 / 184x69 / 120x45 (and the
    120x45 blown up 3x, nearest, to judge it), the header at 460x215, and the hero with the library
    logo bottom-left and the safe area outlined."""
    G, LBL = 40, 44            # gap, label band
    f = _font(28)
    small = out['small_capsule_462x174']
    header = out['header_capsule_920x430']
    tiny = small.resize((120, 45), LANCZOS)
    rows = [
        [('library_hero_3840x1240 + library logo (bottom-left) + safe area', hero_with_library_logo(out['library_hero_3840x1240']))],
        [('main_capsule_1232x706', out['main_capsule_1232x706']),
         ('header_capsule_920x430', header),
         ('library_header_920x430', out['library_header_920x430']),
         ('header @460x215', header.resize((460, 215), LANCZOS))],
        [('vertical_capsule_748x896', out['vertical_capsule_748x896']),
         ('library_capsule_600x900', out['library_capsule_600x900']),
         ('library_capsule @300x450', out['library_capsule_600x900'].resize((300, 450), LANCZOS)),
         ('page_background_1438x810', out['page_background_1438x810']),
         ('event_cover_800x450', out['event_cover_800x450'])],
        [('event_header_1920x622', out['event_header_1920x622']),
         ('small_capsule_462x174', small),
         ('small @231x87', small.resize((231, 87), LANCZOS)),
         ('small @184x69', small.resize((184, 69), LANCZOS)),
         ('small @120x45', tiny),
         ('120x45 x3 (nearest)', tiny.resize((360, 135), Image.Resampling.NEAREST))],
    ]
    probe = ImageDraw.Draw(Image.new('RGB', (8, 8)))
    colw = lambda label, im: max(im.width, int(probe.textlength(label, font=f)) + 12)
    W = max(G + sum(colw(l, im) + G for l, im in r) for r in rows)
    H = G + sum(LBL + max(im.height for _, im in r) + G for r in rows)
    sheet = Image.new('RGB', (W, H), (22, 25, 30))
    d = ImageDraw.Draw(sheet)
    y = G
    for r in rows:
        x = G
        for label, im in r:
            d.text((x, y + 6), label, fill=(205, 210, 218), font=f)
            sheet.paste(im, (x, y + LBL))
            x += colw(label, im) + G
        y += LBL + max(im.height for _, im in r) + G
    sheet.save(os.path.join(OUT, '_sheet.png'))
    return sheet


if __name__ == '__main__':
    assets = build_all()
    s = build_sheet(assets)
    print('wrote %d assets + _sheet.png (%dx%d) to %s' % (len(assets), s.width, s.height, OUT))
