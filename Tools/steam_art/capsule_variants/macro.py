"""Refluff Steam capsule set, approach "PLUSH MACRO".

    python Tools/steam_art/capsule_variants/macro.py          (run from the project root)

The tactile close-up is the hero: the blue-bristled rotary brush in glossy pink foam over vivid, fluffy pile
with depth of field. Plates (engine renders, no text) come from SteamStore/_work/keyart (round-2 / v2):
    wide5_macro_brush_v2   header, library header, main capsule, event cover, small capsule
    tallD_macro_brush_v2   vertical capsule, library capsule
    hero1_foam_bank_v2     library hero (no logo), event header
    background_soft_pile_v2 page background (no logo)
Grade: warm, rich clean pile; the grime is pushed a touch further towards dingy grey-olive (so the dirty / clean
story survives the warm grade); soft highlight bloom on the foam gloss; soft elliptical vignette around the
subject. The logo (SteamStore/logo, never redrawn: only scaled and placed) sits large on a calm defocused area
with a soft dark glow behind it. All blending in float, Lanczos resampling, logo resized premultiplied (no halos).

Writes SteamStore/_work/capsules/macro/*.png (opaque RGB, exact Steam sizes) + _sheet.png (review sheet).
Deterministic: no randomness, fixed parameters.
"""
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage as ndi

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
KEY = os.path.join(ROOT, "SteamStore", "_work", "keyart")
LOGO = os.path.join(ROOT, "SteamStore", "logo")
OUT = os.path.join(ROOT, "SteamStore", "_work", "capsules", "macro")
FONT = os.path.join(ROOT, "Tools", "steam_art", "fonts", "Fredoka[wdth,wght].ttf")

SIZES = {
    "header_capsule_920x430": (920, 430),
    "small_capsule_462x174": (462, 174),
    "main_capsule_1232x706": (1232, 706),
    "vertical_capsule_748x896": (748, 896),
    "library_capsule_600x900": (600, 900),
    "library_header_920x430": (920, 430),
    "library_hero_3840x1240": (3840, 1240),
    "page_background_1438x810": (1438, 810),
    "event_cover_800x450": (800, 450),
    "event_header_1920x622": (1920, 622),
}
MARGIN = 0.04          # logo / subject stay out of the outer 4 % of every capsule

_cache = {}


# ----------------------------------------------------------------------------------------------- io
def load_plate(name):
    """Plate as float32 RGB 0..1 (cached)."""
    if name not in _cache:
        im = Image.open(os.path.join(KEY, name + ".png")).convert("RGB")
        _cache[name] = np.asarray(im, np.float32) / 255.0
    return _cache[name]


def resize_f(a, size, box=None):
    """Lanczos resize of a float (H,W[,C]) array, per channel in float ('F' mode), optional source box."""
    if a.ndim == 2:
        return np.asarray(Image.fromarray(np.ascontiguousarray(a, np.float32), "F").resize(size, Image.LANCZOS, box=box))
    return np.stack([resize_f(a[..., i], size, box) for i in range(a.shape[2])], axis=2)


def crop_plate(name, x0, y0, wfrac, size):
    """Crop a window of the plate: left x0 / top y0 as plate fractions, width wfrac of the plate width, height from
    the output aspect. The window must lie inside the plate (no padding, no stretching)."""
    p = load_plate(name)
    H, W = p.shape[:2]
    cw = wfrac * W
    ch = cw * size[1] / size[0]
    bx = (x0 * W, y0 * H, x0 * W + cw, y0 * H + ch)
    assert bx[0] >= -1.0 and bx[1] >= -1.0 and bx[2] <= W + 1.0 and bx[3] <= H + 1.0, (name, bx, (W, H))
    bx = (max(0.0, bx[0]), max(0.0, bx[1]), min(float(W), bx[2]), min(float(H), bx[3]))
    return np.clip(resize_f(p, size, bx), 0.0, 1.0)


def save(a, name):
    os.makedirs(OUT, exist_ok=True)
    im = Image.fromarray((np.clip(a, 0, 1) * 255.0 + 0.5).astype(np.uint8), "RGB")
    path = os.path.join(OUT, name + ".png")
    im.save(path, optimize=True)
    return path


# ----------------------------------------------------------------------------------------------- colour helpers
def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def luma(a):
    return a[..., 0] * 0.2126 + a[..., 1] * 0.7152 + a[..., 2] * 0.0722


def hsv(a):
    """Hue (degrees), saturation, value of a float RGB array."""
    mx = a.max(axis=2)
    mn = a.min(axis=2)
    d = mx - mn + 1e-6
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    h = np.where(mx == r, ((g - b) / d) % 6.0, np.where(mx == g, (b - r) / d + 2.0, (r - g) / d + 4.0)) * 60.0
    return h, d / (mx + 1e-6), mx


def hue_near(h, centre, width):
    """1 at the centre hue, 0 beyond +-width degrees (smooth)."""
    dist = np.abs((h - centre + 180.0) % 360.0 - 180.0)
    return 1.0 - smoothstep(0.35 * width, width, dist)


# ----------------------------------------------------------------------------------------------- grade
def masks(a):
    """Soft region masks on a defocused copy: grime (tan / grey-olive soiled pile), foam (pink-violet), blue
    (bristles, navy wool). The grime mask is spatial (blurred) so single cream fibres of the clean rug are not
    mistaken for soil."""
    H, W = a.shape[:2]
    s = max(1.5, 0.010 * W)
    soft = np.stack([ndi.gaussian_filter(a[..., i], s) for i in range(3)], axis=2)
    h, sat, v = hsv(soft)
    grime = hue_near(h, 31.0, 13.0) * smoothstep(0.20, 0.30, sat) * (1.0 - smoothstep(0.56, 0.66, sat))
    grime *= 1.0 - smoothstep(0.62, 0.74, v)
    grime = np.clip(ndi.gaussian_filter(grime, 0.6 * s), 0.0, 1.0)
    h2, s2, _ = hsv(a)
    foam = hue_near(h2, 300.0, 45.0) * smoothstep(0.12, 0.30, s2)
    blue = hue_near(h2, 212.0, 38.0) * smoothstep(0.10, 0.25, s2)
    return grime, foam, blue


def grade(a, g):
    """Cinematic grade. g: dict with optional keys vig (strength), vc (centre x, y fractions), bloom, warm,
    grime (0..1 how far the soil goes dingy), expo, curve."""
    a = a.copy()
    H, W = a.shape[:2]
    grime, foam, blue = masks(a)
    # soiled pile: a touch dingier (less saturated, olive-grey, slightly darker) -> reads as dirt, not a brown rug
    k = g.get("grime", 0.72) * grime
    L = luma(a)[..., None]
    dingy = (L + (a - L) * 0.55) * np.array([0.94, 1.0, 0.995], np.float32) * 0.95
    a = a + (dingy - a) * k[..., None]
    # clean pile: warm and rich (reds / creams), foam and blues untouched
    wk = g.get("warm", 1.0) * (1.0 - grime) * (1.0 - foam) * (1.0 - blue)
    L = luma(a)[..., None]
    rich = (L + (a - L) * 1.10) * np.array([1.035, 1.0, 0.93], np.float32)
    a = a + (rich - a) * wk[..., None]
    # exposure + gentle filmic S-curve
    ct = g.get("contrast", 1.0)
    if ct != 1.0:
        m0 = float(luma(a).mean())
        a = m0 + (a - m0) * ct
    a = a * g.get("expo", 1.0)
    c = g.get("curve", 0.14)
    a = np.clip(a, 0.0, 1.0)
    a = a + (a * a * (3.0 - 2.0 * a) - a) * c
    # highlight bloom (foam gloss, brush rim): two soft radii
    bl = g.get("bloom", 0.14)
    if bl > 0:
        hl = a * smoothstep(0.68, 1.0, luma(a))[..., None]
        glow = sum(w * np.stack([ndi.gaussian_filter(hl[..., i], r * W) for i in range(3)], axis=2)
                   for w, r in ((0.6, 0.004), (0.4, 0.016)))
        a = 1.0 - (1.0 - a) * (1.0 - bl * glow)          # screen
    # soft elliptical vignette around the subject (neutral-cool, so it never browns the soil)
    vs = g.get("vig", 0.30)
    if vs > 0:
        cx, cy = g.get("vc", (0.5, 0.5))
        yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
        u = (xx / W - cx) / max(cx, 1 - cx)
        v = (yy / H - cy) / max(cy, 1 - cy)
        r = np.sqrt(u * u + v * v) / np.sqrt(2.0)
        m = vs * smoothstep(0.30, 1.05, r) ** 1.15
        a = a * (1.0 - m[..., None] * np.array([1.04, 1.0, 0.97], np.float32))
    return np.clip(a, 0.0, 1.0)


# ----------------------------------------------------------------------------------------------- logo
_logos = {}


def load_logo(name):
    """Final logo PNG (straight alpha) -> premultiplied float RGBA, trimmed to its visible pixels."""
    if name not in _logos:
        a = np.asarray(Image.open(os.path.join(LOGO, name)).convert("RGBA"), np.float32) / 255.0
        a[..., :3] *= a[..., 3:4]
        ys, xs = np.nonzero(a[..., 3] > 2.0 / 255.0)
        _logos[name] = a[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    return _logos[name]


def resize_prem(p, size):
    """Lanczos resize of premultiplied RGBA; ringing outside the true coverage is zeroed, colour <= alpha."""
    out = resize_f(p, size)
    cov = np.asarray(Image.fromarray(np.ascontiguousarray(p[..., 3]), "F").resize(size, Image.BOX))
    out[cov < 0.5 / 255.0] = 0.0
    out[..., 3] = np.clip(out[..., 3], 0.0, 1.0)
    out[..., :3] = np.clip(out[..., :3], 0.0, out[..., 3:4])
    return out


def place_logo(bg, name, w=None, h=None, cx=0.5, cy=0.5, glow=0.66, spread=0.38, tint=(0.03, 0.02, 0.05),
               margin=MARGIN, tag=""):
    """Composite the logo onto bg (float RGB). w / h = visible width / height as a fraction of the canvas,
    (cx, cy) = centre of the logo as canvas fractions. glow = strength of the soft dark glow behind it,
    spread = glow radius as a fraction of the logo height. Returns (image, logo box in px)."""
    H, W = bg.shape[:2]
    lg = load_logo(name)
    lh, lw = lg.shape[:2]
    if w is not None:
        tw = int(round(w * W))
        th = int(round(lh * tw / lw))
    else:
        th = int(round(h * H))
        tw = int(round(lw * th / lh))
    p = resize_prem(lg, (tw, th))
    x0 = int(round(cx * W - tw / 2.0))
    y0 = int(round(cy * H - th / 2.0))
    mx, my = margin * W, margin * H
    if x0 < mx - 0.5 or y0 < my - 0.5 or x0 + tw > W - mx + 0.5 or y0 + th > H - my + 0.5:
        print(f"  WARNING {tag}: logo box ({x0},{y0},{x0 + tw},{y0 + th}) crosses the {margin:.0%} margin of {W}x{H}")
    canvas = np.zeros((H, W, 4), np.float32)
    sx0, sy0 = max(0, -x0), max(0, -y0)
    dx0, dy0 = max(0, x0), max(0, y0)
    dx1, dy1 = min(W, x0 + tw), min(H, y0 + th)
    canvas[dy0:dy1, dx0:dx1] = p[sy0:sy0 + dy1 - dy0, sx0:sx0 + dx1 - dx0]
    out = bg.copy()
    if glow > 0:
        al = canvas[..., 3]
        sig = spread * th
        g = ndi.gaussian_filter(ndi.maximum_filter(al, size=max(3, int(0.08 * th) | 1)), sig)
        g = np.clip(g * 1.6, 0.0, 1.0) ** 0.9
        out = out + (np.array(tint, np.float32) - out) * (glow * g)[..., None]
    out = out * (1.0 - canvas[..., 3:4]) + canvas[..., :3]
    return np.clip(out, 0.0, 1.0), (x0, y0, x0 + tw, y0 + th)


# ----------------------------------------------------------------------------------------------- layouts
# crop = (left, top, width) as fractions of the plate; the height follows the asset's aspect.
# grade = grade() keys; logo = place_logo() keys (None = no logo: library hero, page background).
WORD = "refluff_logo.png"                 # full lockup incl. the official subtitle
SMALL = "refluff_wordmark_small.png"      # dedicated small-size render (< ~600 px wide)
HERO_PLATE = "hero1_foam_bank_v2"
WIDE_PLATE = "wide5_macro_brush_v2"
TALL_PLATE = "tallD_macro_brush_v2"
BG_PLATE = "background_soft_pile_v2"

LAYOUT = {
    # store header: low 3/4 macro, brush right of centre in front of the foam bank, logo on the calm grime
    "header_capsule_920x430": dict(plate=HERO_PLATE, crop=(0.09, 0.03, 0.645),
                                   grade=dict(vc=(0.62, 0.55), vig=0.32),
                                   logo=dict(name=WORD, w=0.47, cx=0.275, cy=0.33)),
    "main_capsule_1232x706": dict(plate=WIDE_PLATE, crop=(0.0, 0.0, 0.9085),
                                  grade=dict(vc=(0.52, 0.60), vig=0.32),
                                  logo=dict(name=WORD, w=0.45, cx=0.27, cy=0.165)),
    "vertical_capsule_748x896": dict(plate=TALL_PLATE, crop=(0.0, 0.07, 1.0),
                                     grade=dict(vc=(0.50, 0.58), vig=0.34),
                                     logo=dict(name=WORD, w=0.84, cx=0.5, cy=0.165)),
    "library_capsule_600x900": dict(plate=TALL_PLATE, crop=(0.0, 0.0, 1.0),
                                    grade=dict(vc=(0.50, 0.58), vig=0.34),
                                    logo=dict(name=WORD, w=0.86, cx=0.5, cy=0.15)),
    "library_hero_3840x1240": dict(plate=HERO_PLATE, crop=(0.0, 0.0, 1.0),
                                   grade=dict(vc=(0.50, 0.53), vig=0.26), logo=None),
    "page_background_1438x810": dict(plate=BG_PLATE, crop=(0.0, 0.0, 1.0),
                                     grade=dict(vc=(0.5, 0.5), vig=0.40, bloom=0.0, expo=0.90, curve=0.0,
                                                grime=0.0, contrast=0.78), logo=None),
    # event cover: shown small in event lists -> header-style layout, logo half the width on the calm grime
    "event_cover_800x450": dict(plate=HERO_PLATE, crop=(0.115, 0.0, 0.5741),
                                grade=dict(vc=(0.62, 0.55), vig=0.32),
                                logo=dict(name=WORD, w=0.47, cx=0.275, cy=0.30)),
    "event_header_1920x622": dict(plate=HERO_PLATE, crop=(0.0, 0.0, 0.9967),
                                  grade=dict(vc=(0.52, 0.55), vig=0.30),
                                  logo=dict(name=WORD, w=0.33, cx=0.215, cy=0.40)),
    # small capsule: the wordmark nearly fills it; behind it the soiled pile meeting the foam bank, defocused
    "small_capsule_462x174": dict(plate=HERO_PLATE, crop=(0.385, 0.0, 0.26), defocus=0.006,
                                  grade=dict(vc=(0.5, 0.5), vig=0.30),
                                  logo=dict(name=SMALL, w=0.88, cx=0.5, cy=0.5, glow=0.60, spread=0.35)),
}
LAYOUT["library_header_920x430"] = LAYOUT["header_capsule_920x430"]      # same art as the store header


def build(key):
    spec = LAYOUT[key]
    size = SIZES[key]
    a = crop_plate(spec["plate"], *spec["crop"], size)
    if spec.get("defocus"):
        s = spec["defocus"] * size[0]
        a = np.stack([ndi.gaussian_filter(a[..., i], s) for i in range(3)], axis=2)
    a = grade(a, spec["grade"])
    box = None
    if spec.get("logo"):
        lp = dict(spec["logo"])
        a, box = place_logo(a, lp.pop("name"), tag=key, **lp)
    assert a.shape[:2] == (size[1], size[0]), (key, a.shape)
    return a, box


# ----------------------------------------------------------------------------------------------- review sheet
SHEET_BG = (0x10 / 255, 0x16 / 255, 0x20 / 255)
LIB_LOGO = "refluff_logo_1280x720.png"


def hero_with_library_logo(hero):
    """The library hero as the Steam client shows it: the library logo (1280x720 PNG, 1:1 at the 3840 master)
    pinned bottom-left, plus the central 860x380 safe area outlined."""
    im = hero.copy()
    lg = np.asarray(Image.open(os.path.join(LOGO, LIB_LOGO)).convert("RGBA"), np.float32) / 255.0
    ys, xs = np.nonzero(lg[..., 3] > 2.0 / 255.0)
    lg = lg[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    H, W = im.shape[:2]
    lh, lw = lg.shape[:2]
    x0, y0 = int(0.045 * W), H - int(0.065 * H) - lh
    al = lg[..., 3:4]
    im[y0:y0 + lh, x0:x0 + lw] = im[y0:y0 + lh, x0:x0 + lw] * (1 - al) + lg[..., :3] * al
    pil = Image.fromarray((np.clip(im, 0, 1) * 255 + 0.5).astype(np.uint8))
    d = ImageDraw.Draw(pil)
    sx0, sy0 = (W - 860) // 2, (H - 380) // 2
    for k in range(4):
        d.rectangle((sx0 - k, sy0 - k, sx0 + 859 + k, sy0 + 379 + k), outline=(255, 214, 64))
    return np.asarray(pil, np.float32) / 255.0


def to_pil(a):
    return Image.fromarray((np.clip(a, 0, 1) * 255.0 + 0.5).astype(np.uint8), "RGB")


def make_sheet(assets):
    """Every asset at its real size + the small-size checks, laid out in rows with labels."""
    ft = ImageFont.truetype(FONT, 30)
    ftb = ImageFont.truetype(FONT, 46)
    small = to_pil(assets["small_capsule_462x174"])
    header = to_pil(assets["header_capsule_920x430"])
    hero = assets["library_hero_3840x1240"]
    rows = [
        [("header_capsule_920x430", header), ("library_header_920x430", to_pil(assets["library_header_920x430"])),
         ("main_capsule_1232x706", to_pil(assets["main_capsule_1232x706"])),
         ("event_cover_800x450", to_pil(assets["event_cover_800x450"]))],
        [("vertical_capsule_748x896", to_pil(assets["vertical_capsule_748x896"])),
         ("library_capsule_600x900", to_pil(assets["library_capsule_600x900"])),
         ("library capsule @300x450", to_pil(assets["library_capsule_600x900"]).resize((300, 450), Image.LANCZOS)),
         ("page_background_1438x810", to_pil(assets["page_background_1438x810"]))],
        [("small_capsule_462x174", small), ("small @231x87", small.resize((231, 87), Image.LANCZOS)),
         ("small @184x69", small.resize((184, 69), Image.LANCZOS)),
         ("small @120x45", small.resize((120, 45), Image.LANCZOS)),
         ("header @460x215", header.resize((460, 215), Image.LANCZOS)),
         ("main @616x353", to_pil(assets["main_capsule_1232x706"]).resize((616, 353), Image.LANCZOS)),
         ("event_header_1920x622", to_pil(assets["event_header_1920x622"]))],
        [("library_hero_3840x1240 (no logo, no text)", to_pil(hero))],
        [("library hero + library logo bottom-left (as Steam shows it), central 860x380 safe area outlined",
          to_pil(hero_with_library_logo(hero)))],
    ]
    gap, lab, pad = 36, 44, 48

    def cell_w(label, im):                                   # a cell is as wide as its image or its label
        return max(im.width, int(ft.getlength(label)) + 8)

    W = max(sum(cell_w(lb, im) for lb, im in r) + gap * (len(r) - 1) for r in rows) + 2 * pad
    H = 90 + sum(max(im.height for _, im in r) + lab + gap for r in rows) + pad
    sheet = Image.new("RGB", (W, H), tuple(int(c * 255) for c in SHEET_BG))
    d = ImageDraw.Draw(sheet)
    d.text((pad, 24), "Refluff capsules - PLUSH MACRO (every asset at real size)", font=ftb, fill=(235, 235, 240))
    y = 90
    for r in rows:
        x = pad
        for label, im in r:
            d.text((x, y + 4), label, font=ft, fill=(190, 200, 215))
            sheet.paste(im, (x, y + lab))
            x += cell_w(label, im) + gap
        y += max(im.height for _, im in r) + lab + gap
    return sheet


def main():
    os.makedirs(OUT, exist_ok=True)
    assets = {}
    for key in SIZES:
        a, box = build(key)
        assets[key] = a
        path = save(a, key)
        print(f"  {key}: {a.shape[1]}x{a.shape[0]}" + (f"  logo box {box}" if box else "") + f"  -> {path}")
    sheet = make_sheet(assets)
    sheet.save(os.path.join(OUT, "_sheet.png"), optimize=False, compress_level=6)
    print(f"  _sheet.png {sheet.width}x{sheet.height}")
    for key, (w, h) in SIZES.items():                       # exact sizes, opaque RGB
        im = Image.open(os.path.join(OUT, key + ".png"))
        assert im.size == (w, h) and im.mode == "RGB", (key, im.size, im.mode)
    print("OK", len(SIZES), "assets")
    return 0


if __name__ == "__main__":
    sys.exit(main())
