"""Refluff capsule set, approach "THE PROMISE": the before/after seam is the hero of every capsule.

    python Tools/steam_art/capsule_variants/split.py          (run from the project root)

Every capsule shows the same story: soft grey-olive grime on the left, a pink-violet foam band and the blue
rotary brush on the seam, the vivid rug on the right. The vivid side gets a subtle glow grade (lift, colour,
bloom), the grimy side is calmed a little so the logo sits on a quiet zone.

Inputs : SteamStore/_work/keyart/*_v2.png (round-2 plates, see plates.json), SteamStore/logo/*.png (final logo,
         used as-is: only scaled, placed and given a soft dark glow behind it).
Output : SteamStore/_work/capsules/split/<asset>.png (exact Steam sizes, opaque RGB) + _sheet.png (review sheet).
Deterministic: fixed crops, no randomness, LANCZOS resampling in float, premultiplied-alpha logo scaling.
"""
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy.ndimage import gaussian_filter

ROOT = Path(__file__).resolve().parents[3]
KEY = ROOT / "SteamStore/_work/keyart"
LOGO = ROOT / "SteamStore/logo"
OUT = ROOT / "SteamStore/_work/capsules/split"
FONT = ROOT / "Tools/steam_art/fonts/Fredoka[wdth,wght].ttf"

LUMA = np.array([0.2126, 0.7152, 0.0722], np.float32)
SHADOW_RGB = np.array([0.035, 0.03, 0.06], np.float32)   # soft navy-black behind the logo


# ----------------------------------------------------------------------------------------------- basics
_plates, _masks, _logos = {}, {}, {}


def plate(name):
    if name not in _plates:
        _plates[name] = Image.open(KEY / f"{name}.png").convert("RGB")
    return _plates[name]


def to_f(im):
    return np.asarray(im, np.float32) / 255.0


def save_rgb(a, path):
    im = Image.fromarray((np.clip(a, 0, 1) * 255 + 0.5).astype(np.uint8), "RGB")
    im.save(path, optimize=True)
    return im


def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def blur(a, sigma):
    if sigma <= 0:
        return a
    if a.ndim == 3:
        return gaussian_filter(a, (sigma, sigma, 0), mode="nearest")
    return gaussian_filter(a, sigma, mode="nearest")


def resize_f(a, size, box=None):
    """LANCZOS per channel in 32-bit float (PIL mode F): no 8-bit rounding between steps."""
    if a.ndim == 2:
        return np.asarray(Image.fromarray(a).resize(size, Image.LANCZOS, box=box), np.float32)
    ch = [np.asarray(Image.fromarray(np.ascontiguousarray(a[..., c])).resize(size, Image.LANCZOS, box=box),
                     np.float32) for c in range(a.shape[2])]
    return np.stack(ch, -1)


# ----------------------------------------------------------------------------------------------- seam mask
def vivid_mask(name):
    """0 on the grimy pile, 1 on the cleaned rug (and on the foam / brush sitting on the seam).

    The grade in the plates leaves the grime a flat grey-olive: low local chroma spread, chroma near
    (rg 0.09, yb 0.14). The clean rug is red / navy / cream: a large local chroma spread. Built at 1/8 scale
    in plate space so every crop of the same plate shares one mask.
    """
    if name in _masks:
        return _masks[name]
    im = plate(name)
    w, h = im.size
    a = to_f(im.resize((w // 8, h // 8), Image.LANCZOS))
    W = a.shape[1]
    rg = a[..., 0] - a[..., 1]
    yb = 0.5 * (a[..., 0] + a[..., 1]) - a[..., 2]
    s = 0.01 * W
    mrg, myb = blur(rg, s), blur(yb, s)
    spread = np.sqrt(np.maximum(blur(rg * rg, s) - mrg ** 2, 0) + np.maximum(blur(yb * yb, s) - myb ** 2, 0))
    dist = np.sqrt((mrg - 0.09) ** 2 + (myb - 0.14) ** 2)
    m = np.maximum(smoothstep(0.034, 0.075, spread), smoothstep(0.07, 0.16, dist))
    m = smoothstep(0.3, 0.7, blur(m, 0.012 * W))
    m = blur(m, 0.006 * W)
    _masks[name] = np.clip(m, 0, 1).astype(np.float32)
    return _masks[name]


def crop_box(name, cx, cy, wf, aspect):
    """Plate-pixel box centred at (cx, cy) (fractions), width wf * plate width, given aspect (w / h)."""
    pw, ph = plate(name).size
    bw = min(wf * pw, ph * aspect)                  # wf = 1: as wide as the plate allows
    bh = bw / aspect
    x0 = min(max(cx * pw - bw / 2, 0), pw - bw)
    y0 = min(max(cy * ph - bh / 2, 0), ph - bh)
    assert bw <= pw + 1e-3 and bh <= ph + 1e-3, (name, wf, aspect)
    return (x0, y0, x0 + bw, y0 + bh)


def render_art(name, size, cx=0.5, cy=0.5, wf=1.0):
    """Crop + LANCZOS to size; returns (rgb float, vivid mask) for the same framing."""
    W, H = size
    box = crop_box(name, cx, cy, wf, W / H)
    rgb = np.clip(resize_f(to_f(plate(name)), size, box), 0, 1)
    m = vivid_mask(name)
    pw, ph = plate(name).size
    kx, ky = m.shape[1] / pw, m.shape[0] / ph
    mbox = (box[0] * kx, box[1] * ky, box[2] * kx, box[3] * ky)
    mask = np.clip(resize_f(m, size, mbox), 0, 1)
    return rgb, mask


# ----------------------------------------------------------------------------------------------- grade
def grade(rgb, m, glow=0.28, lift=0.08, sat=0.16, calm=0.10, vignette=0.16, bloom_r=0.025, grime=0.25, olive=0.04):
    """The promise grade: vivid side glows (lift + colour + highlight bloom), grimy side calms down.

    m = vivid mask (1 = clean rug). grime = chroma cut on the dirty side, olive = red pulled out of it so the
    soil reads grey-olive (dirty), not as a brown rug. All in float, bounded; bloom is a screen blend.
    """
    H, W, _ = rgb.shape
    m3 = m[..., None]
    g3 = 1 - m3
    lum = (rgb @ LUMA)[..., None]
    out = lum + (rgb - lum) * (1 + sat * m3 - grime * g3)        # colour on the clean side, less on the dirt
    out = out * (1 - olive * g3 * np.array([1.0, 0.0, 0.35], np.float32))
    out = out * (1 + lift * m3) * np.array([1.0 + 0.012, 1.0, 1.0 - 0.012], np.float32) ** m3
    out = out * (1 - calm * (1 - m3))                            # quiet the grime a touch
    chroma = smoothstep(0.08, 0.30, out.max(-1) - out.min(-1))[..., None]   # rug colours bloom, white housing not
    hi = np.clip((out @ LUMA)[..., None] - 0.42, 0, None) * np.clip(out, 0, 1) * m3 * chroma
    bl = blur(hi, bloom_r * W) * 2.2
    out = 1 - (1 - np.clip(out, 0, 1)) * (1 - glow * np.clip(bl, 0, 1))
    if vignette > 0:
        yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
        r = np.sqrt(((xx + 0.5) / W - 0.5) ** 2 * 1.0 + ((yy + 0.5) / H - 0.5) ** 2 * 1.0) / 0.7071
        out = out * (1 - vignette * smoothstep(0.45, 1.05, r)[..., None])
    return np.clip(out, 0, 1)


# ----------------------------------------------------------------------------------------------- logo
def logo_rgba(file):
    """Logo trimmed to its alpha bbox, as premultiplied float RGBA (cached)."""
    if file not in _logos:
        a = to_f(Image.open(LOGO / file).convert("RGBA"))
        ys, xs = np.nonzero(a[..., 3] > 0)
        a = a[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
        a[..., :3] *= a[..., 3:4]
        _logos[file] = a
    return _logos[file]


def scaled_logo(file, width):
    a = logo_rgba(file)
    h = int(round(a.shape[0] * width / a.shape[1]))
    r = np.clip(resize_f(a, (int(width), h)), 0, 1)
    r[..., :3] = np.minimum(r[..., :3], r[..., 3:4])            # keep premultiplied rgb <= alpha
    return r


def place_logo(img, file, cx, cy, width_frac, shadow=0.55, ambient=0.30, drop=0.035, soft=0.045, halo=0.20):
    """Composite the logo centred at (cx, cy) (fractions of the canvas) at width_frac of the canvas width.

    Behind it: a soft drop shadow (blur soft * logo height, offset drop * height) and a wide ambient
    darkening (halo * height) that calms busy art without an outline. Returns (image, logo box in px).
    """
    H, W, _ = img.shape
    lg = scaled_logo(file, round(width_frac * W))
    lh, lw = lg.shape[:2]
    x0 = int(round(cx * W - lw / 2))
    y0 = int(round(cy * H - lh / 2))
    A = np.zeros((H, W), np.float32)
    P = np.zeros((H, W, 3), np.float32)
    ys0, xs0 = max(y0, 0), max(x0, 0)
    ys1, xs1 = min(y0 + lh, H), min(x0 + lw, W)
    A[ys0:ys1, xs0:xs1] = lg[ys0 - y0:ys1 - y0, xs0 - x0:xs1 - x0, 3]
    P[ys0:ys1, xs0:xs1] = lg[ys0 - y0:ys1 - y0, xs0 - x0:xs1 - x0, :3]
    dy = int(round(drop * lh))
    As = np.zeros_like(A)
    As[dy:] = A[:H - dy] if dy > 0 else A
    d1 = np.clip(blur(As, soft * lh), 0, 1) * shadow
    d2 = np.clip(blur(A, halo * lh) * 1.25, 0, 1) * ambient
    dark = (1 - (1 - d1) * (1 - d2))[..., None]
    out = img * (1 - dark) + SHADOW_RGB * dark
    out = P + out * (1 - A[..., None])
    return np.clip(out, 0, 1), (x0, y0, x0 + lw, y0 + lh)


def extend_left(name, frac, src_end=0.26, fade=0.06):
    """Plate widened on the left by frac * width with more of the same grime.

    The grimy pile left of the seam is statistically uniform along x (no vignette; perspective and DoF run
    along y), so a horizontally translated copy of it continues the pile with the tufts leaning the same way
    (a mirror would fold them into visible chevrons). The copy is taken from x < src_end (pure grime) and
    cross-faded into the original over fade * width; the logo sits over most of the extension.
    """
    key = f"{name}@L{frac:.3f}"
    if key not in _plates:
        a = np.asarray(plate(name)).astype(np.float32)
        H, W, _ = a.shape
        e, f, s1 = int(round(frac * W)), int(round(fade * W)), int(round(src_end * W))
        shift = s1 - f                                    # canvas x -> plate x = x - e + shift
        assert shift - e >= 0, "extension wider than the grime source"
        out = np.concatenate([np.zeros((H, e, 3), np.float32), a], 1)
        copy = a[:, shift - e:s1]                         # covers canvas [0, e + f)
        t = np.ones(e + f, np.float32)
        t[e:] = 1 - smoothstep(0.0, 1.0, (np.arange(f, dtype=np.float32) + 0.5) / f)
        out[:, :e + f] = copy * t[None, :, None] + out[:, :e + f] * (1 - t[None, :, None])
        _plates[key] = Image.fromarray(np.clip(out + 0.5, 0, 255).astype(np.uint8))
    return key


# ----------------------------------------------------------------------------------------------- layouts
W2 = "wide2_split_seam_v2"      # 3/4 view along the seam: grime | foam band + brush | vivid medallion
TB = "tallB_brush_seam_v2"      # portrait across the seam: grime top, brush on the seam, vivid foreground
H2 = "hero2_split_brush_v2"     # low wide view: grime left, brush centred on the seam, vivid right
BG = "background_soft_pile_v2"  # defocused clean pile for the store page background

FULL = "refluff_logo.png"               # lockup with the official subtitle
SMALL = "refluff_wordmark_small.png"    # "Refluff" only, bolder render for small sizes

# name: (size, plate, (extend_left, cx, cy, width_frac), grade kwargs, logo or None: (file, cx, cy, width_frac, kwargs))
LAYOUT = {
    "header_capsule_920x430": ((920, 430), W2, (0.18, 0.47, 0.55, 0.86), {},
                               (FULL, 0.265, 0.215, 0.45, {})),
    "small_capsule_462x174": ((462, 174), W2, (0.18, 0.47, 0.0, 0.94), {"vignette": 0.10},
                              (SMALL, 0.47, 0.35, 0.84, {"halo": 0.16, "ambient": 0.25})),
    "main_capsule_1232x706": ((1232, 706), W2, (0.18, 0.473, 0.5, 0.77), {},
                              (FULL, 0.26, 0.22, 0.43, {})),
    "vertical_capsule_748x896": ((748, 896), TB, (0.0, 0.5, 0.45, 1.0), {},
                                 (FULL, 0.5, 0.19, 0.86, {})),
    "library_capsule_600x900": ((600, 900), TB, (0.0, 0.5, 0.5, 1.0), {},
                                (FULL, 0.5, 0.165, 0.86, {})),
    "library_header_920x430": ((920, 430), W2, (0.18, 0.47, 0.55, 0.86), {},
                               (FULL, 0.265, 0.215, 0.45, {})),
    "library_hero_3840x1240": ((3840, 1240), H2, (0.0, 0.5, 0.5, 1.0), {"vignette": 0.10}, None),
    "page_background_1438x810": ((1438, 810), BG, (0.0, 0.5, 0.5, 1.0), None, None),
    "event_cover_800x450": ((800, 450), W2, (0.18, 0.473, 0.5, 0.77), {},
                            (FULL, 0.265, 0.215, 0.45, {})),
    "event_header_1920x622": ((1920, 622), H2, (0.0, 0.5, 0.5, 1.0), {},
                              (FULL, 0.215, 0.52, 0.34, {})),
}


def page_background(size, con=0.72, sat=0.80, exposure=0.90):
    """Calm, dark, low-contrast: the defocused clean pile with less colour and contrast (Steam lays its own
    dark-blue fade over it and the page text sits on it). A "promise" grime tint was tried and read as mud."""
    rgb, _ = render_art(BG, size)
    lum = (rgb @ LUMA)[..., None]
    out = lum + (rgb - lum) * sat
    mu = out.reshape(-1, 3).mean(0)
    out = mu + (out - mu) * con
    return np.clip(out * exposure, 0, 1)


def build(name):
    size, pl, (ext, cx, cy, wf), gk, lg = LAYOUT[name]
    if gk is None:
        return page_background(size), None
    key = extend_left(pl, ext) if ext > 0 else pl
    rgb, m = render_art(key, size, cx, cy, wf)
    img = grade(rgb, m, **gk)
    box = None
    if lg is not None:
        f, lx, ly, lw, lk = lg
        img, box = place_logo(img, f, lx, ly, lw, **lk)
    return img, box


# ----------------------------------------------------------------------------------------------- review sheet
def _font(px, wght=600):
    f = ImageFont.truetype(str(FONT), px)
    try:
        f.set_variation_by_axes([100, wght])
    except Exception:
        pass
    return f


def _label(draw, xy, text, px=34, fill=(214, 222, 232)):
    draw.text(xy, text, font=_font(px), fill=fill)


def hero_with_logo(hero, scale=0.5):
    """How the library shows it: hero (1920x620 copy) with the library logo bottom-left + the 860x380
    centre safe area outlined (red)."""
    W, H = hero.size
    w, h = int(W * scale), int(H * scale)
    im = hero.resize((w, h), Image.LANCZOS).convert("RGBA")
    lg = Image.open(LOGO / "refluff_logo_1280x720.png").convert("RGBA")
    lg = lg.crop(lg.getbbox())
    lw = int(round(w * 0.30))                   # Steam's library logo box ~ 30 % of the hero width
    lg = lg.resize((lw, int(round(lg.height * lw / lg.width))), Image.LANCZOS)
    im.alpha_composite(lg, (int(w * 0.035), h - lg.height - int(h * 0.09)))
    d = ImageDraw.Draw(im)
    sw, sh = 860 * scale, 380 * scale
    x0, y0 = (w - sw) / 2, (h - sh) / 2
    d.rectangle([x0, y0, x0 + sw, y0 + sh], outline=(255, 64, 64, 255), width=3)
    return im.convert("RGB")


def make_sheet(imgs, path):
    pad, lab = 60, 56
    bg = (27, 40, 56)                           # Steam store dark blue
    hero = imgs["library_hero_3840x1240"]
    W = hero.width + 2 * pad
    rows = [
        ["header_capsule_920x430", "library_header_920x430", "main_capsule_1232x706", "event_cover_800x450"],
        ["vertical_capsule_748x896", "library_capsule_600x900", "page_background_1438x810"],
        ["small_capsule_462x174", "small@231x87", "small@184x69", "small@120x45", "header@460x215",
         "event_header_1920x622"],
        ["library_hero_3840x1240"],
        ["hero_in_library"],
    ]
    small = imgs["small_capsule_462x174"]
    extra = {
        "small@231x87": small.resize((231, 87), Image.LANCZOS),
        "small@184x69": small.resize((184, 69), Image.LANCZOS),
        "small@120x45": small.resize((120, 45), Image.LANCZOS),
        "header@460x215": imgs["header_capsule_920x430"].resize((460, 215), Image.LANCZOS),
        "hero_in_library": hero_with_logo(hero),
    }
    get = lambda k: imgs[k] if k in imgs else extra[k]
    probe = ImageDraw.Draw(Image.new("RGB", (8, 8)))
    cell = lambda k: max(get(k).width, int(probe.textlength(k, font=_font(30))))
    W = max(W, max(pad + sum(cell(k) + pad for k in r) for r in rows if r != ["hero_in_library"]))
    Ht = pad
    for r in rows:
        Ht += lab + max(get(k).height for k in r) + pad
    sheet = Image.new("RGB", (W, Ht), bg)
    d = ImageDraw.Draw(sheet)
    y = pad
    for r in rows:
        x = pad
        for k in r:
            im = get(k)
            name = k if k != "hero_in_library" else \
                "library hero as the library shows it (1920x620 copy) + library logo bottom-left + 860x380 safe area"
            fpx = 30 if len(name) < 60 else 28
            _label(d, (x, y), name, fpx)
            sheet.paste(im, (x, y + lab))
            x += max(im.width, int(d.textlength(name, font=_font(fpx)))) + pad
        y += lab + max(get(k).height for k in r) + pad
    sheet.save(path, optimize=True)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    imgs = {}
    for name in LAYOUT:
        a, box = build(name)
        im = save_rgb(a, OUT / f"{name}.png")
        w, h = LAYOUT[name][0]
        assert im.size == (w, h) and im.mode == "RGB", (name, im.size, im.mode)
        imgs[name] = im
        if box is not None:
            x0, y0, x1, y1 = box
            m = min(x0 / w, y0 / h, (w - x1) / w, (h - y1) / h)
            print(f"{name:28s} logo {x1 - x0}x{y1 - y0} at ({x0},{y0}) min margin {m * 100:.1f} %")
        else:
            print(f"{name:28s} (no logo)")
    make_sheet(imgs, OUT / "_sheet.png")
    print("wrote", OUT)


if __name__ == "__main__":
    main()
