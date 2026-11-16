"""Trailer captions, logo animation and end card (Docs/TRAILER.md 6), drawn over finished sRGB frames.

Used by make_trailer.py (drawing) and check_trailer.py (geometry). Every size below is at 1080p and scales with the
output height (a 960x540 preview draws everything at half size).

Styles (TRAILER.md 6, the "HUD pill" look; colours are read from the `.hud` tokens of Assets/CarpetCleaning/UI/CarpetHud.uss
so a HUD restyle carries over; the constants in TOKENS_FALLBACK are only used when the USS cannot be read):
  verb        white --surface capsule, --ink text, teal --accent square chip with a white digit (hotkey), 60 px
  line        white capsule, --ink text, 60 px
  refluff     inverted: teal capsule, white text, white chip with a teal digit, 1.3x = 78 px
  headphones  white capsule with a headphones glyph, 54 px (45 in the first draft: ~8 px caps at 600 px)
  wishlist    teal capsule, white text, 60 px (end card call to action)
  date        text only, white, soft shadow (end card; its own layer: change only this when the date moves)
  studio      text only, small, white (end card)
A title may set "shadow": "shade-strong" (the HUD CTA shadow) where a white capsule sits on the white tile.
Positions: bottom (bottom centre, capsule bottom 66 px above the frame edge), br (bottom right), under_logo (the #03
plaque under the logo), or {"x": 0..1, "y": 0..1} (capsule centre, fractions of the frame).
Appearance: on the cut, 0.15 s scale 0.92 -> 1 and alpha 0 -> 1 (ease-out cubic), then held.
Rendering: capsules, chips and glyphs are drawn at 4x and box-filtered down (PIL shapes are not antialiased); the result
is a straight-alpha RGBA sprite composited premultiplied in 8-bit sRGB, the way the game composites its UI Toolkit panel.
"""
import math
import re
import threading
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[3]
FONTS = ROOT / "Tools" / "steam_art" / "fonts"
USS = ROOT / "Assets" / "CarpetCleaning" / "UI" / "CarpetHud.uss"
LOGO = ROOT / "SteamStore" / "logo" / "refluff_logo.png"
FONT_FILE = "Fredoka[wdth,wght].ttf"
SS = 4                                  # supersampling of shapes

TOKENS_FALLBACK = {
    "surface": (255, 255, 255, 0.92), "ink": (26, 33, 43, 1.0), "shade": (16, 24, 36, 0.16),
    "shade-strong": (16, 24, 36, 0.24), "accent": (20, 174, 160, 1.0), "rim": (18, 26, 38, 0.08),
    "on-accent": (255, 255, 255, 1.0),
}


def hud_tokens(path=USS):
    """{"surface": (r, g, b, a), ...} from the `.hud { --name: rgb()/rgba() }` block of CarpetHud.uss."""
    tok = dict(TOKENS_FALLBACK)
    try:
        txt = path.read_text(encoding="utf-8")
    except OSError:
        return tok
    m = re.search(r"\.hud\s*\{(.*?)\n\}", txt, re.S)
    body = m.group(1) if m else txt
    for name, fn, args in re.findall(r"--([a-z0-9-]+)\s*:\s*(rgba?)\(([^)]*)\)", body):
        v = [float(x) for x in args.split(",")]
        tok[name] = (int(v[0]), int(v[1]), int(v[2]), v[3] if fn == "rgba" and len(v) > 3 else 1.0)
    return tok


TOK = hud_tokens()


def rgba8(name, alpha_mul=1.0):
    r, g, b, a = TOK[name]
    return (r, g, b, int(round(255 * max(0.0, min(1.0, a * alpha_mul)))))


WHITE = (255, 255, 255, 255)

# 1080p geometry (TRAILER.md 6)
STYLES = {
    #              font px  capsule fill  text       chip fill   chip text  glyph
    "verb":       (60, "surface", "ink", "accent", "on-accent", None),
    "line":       (60, "surface", "ink", None, None, None),
    "refluff":    (78, "accent", "on-accent", "on-accent", "accent", None),
    "headphones": (54, "surface", "ink", None, None, "headphones"),
    "wishlist":   (60, "accent", "on-accent", None, None, None),
}
TEXT_STYLES = {"date": 51, "studio": 30}
BOTTOM_PX = 66                          # capsule bottom above the frame's bottom edge (1080p)
SAFE = 0.05                             # title-safe margin, fraction of each dimension
APPEAR_S = 0.15
APPEAR_SCALE = 0.92

_lock = threading.Lock()
_fonts, _sprites, _logos = {}, {}, {}


def font(px, weight="SemiBold"):
    key = (px, weight)
    with _lock:
        f = _fonts.get(key)
        if f is None:
            f = ImageFont.truetype(str(FONTS / FONT_FILE), px)
            f.set_variation_by_name(weight)
            _fonts[key] = f
        return f


def ease_out_cubic(u):
    u = min(max(u, 0.0), 1.0)
    return 1.0 - (1.0 - u) ** 3


def smoothstep(u):
    u = min(max(u, 0.0), 1.0)
    return u * u * (3.0 - 2.0 * u)


def appear(tl, dur=APPEAR_S, s0=APPEAR_SCALE):
    """(scale, alpha) of a title tl seconds after it appears."""
    if tl >= dur:
        return 1.0, 1.0
    e = ease_out_cubic(tl / dur)
    return s0 + (1.0 - s0) * e, e


# ------------------------------------------------------------------------------------------------ sprites
class Sprite:
    """Straight-alpha RGBA image plus the box (x0, y0, x1, y1, sprite px) that positions it: the capsule, or the text."""

    def __init__(self, img, box):
        self.img, self.box = img, box
        self._np = None

    def array(self):
        if self._np is None:
            self._np = np.asarray(self.img, dtype=np.float32) / 255.0
        return self._np


def _layer(size):
    return Image.new("RGBA", size, (0, 0, 0, 0))


def _down(img):
    """SS x image -> 1x, box filter on premultiplied alpha (a plain reduce would darken the antialiased edges)."""
    return img.convert("RGBa").reduce(SS).convert("RGBA")


def _text_top_left(f, text, cx=None, left=None, cy=0.0):
    """Anchor 'ls' origin for text whose cap height is centred on cy (horizontally centred on cx, or starting at left)."""
    capb = f.getbbox("H", anchor="ls")
    cap = capb[3] - capb[1]
    tb = f.getbbox(text, anchor="ls")
    x = (cx - (tb[0] + tb[2]) / 2.0) if cx is not None else (left - tb[0])
    return x, cy + cap / 2.0


def headphones_glyph(d, cx, cy, s, col):
    """Headphones (band arc + two cups), s = glyph height in px, drawn onto an ImageDraw at the current scale."""
    r = s * 0.46
    d.arc((cx - r, cy - s * 0.52, cx + r, cy - s * 0.52 + 2 * r), 180, 360, fill=col, width=max(2, round(s * 0.11)))
    cw, ch = s * 0.24, s * 0.42
    for sx in (-1, 1):
        x = cx + sx * (r - cw * 0.35)
        d.rounded_rectangle((x - cw / 2, cy - ch * 0.15, x + cw / 2, cy + ch * 0.85), round(cw * 0.35), fill=col)


def _shadow(size, box, radius, h, token="shade"):
    """--shade drop shadow (HudArtBaker's soft rounded rect): solid under the capsule, inset at the top, gaussian tail
    that shows mostly below it."""
    x0, y0, x1, y1 = box
    m = _layer((size[0] * SS, size[1] * SS))
    ins_x, ins_top, ins_bot = 0.10 * h, 0.22 * h, -0.02 * h
    ImageDraw.Draw(m).rounded_rectangle(((x0 + ins_x) * SS, (y0 + ins_top) * SS, (x1 - ins_x) * SS, (y1 - ins_bot) * SS),
                                        max(1, (radius - ins_x) * SS), fill=(255, 255, 255, 255))
    a = _down(m).getchannel("A").filter(ImageFilter.GaussianBlur(0.18 * h))
    r, g, b, sa = TOK[token]
    a = a.point(lambda v: int(round(v * sa)))
    sh = Image.new("RGBA", size, (r, g, b, 0))
    sh.putalpha(a)
    return sh


def capsule_sprite(text, style, num=None, k=1.0, shadow="shade"):
    """The HUD pill caption at output scale k (= frame height / 1080); shadow = a USS shade token."""
    key = ("cap", text, style, num, round(k, 4), shadow)
    with _lock:
        if key in _sprites:
            return _sprites[key]
    px, fill, ink, chip_fill, chip_ink, glyph = STYLES[style]
    F = px * k
    h = float(round(1.55 * F))
    f = font(max(4, round(F * SS)))
    tw = f.getlength(text) / SS
    inset = 0.13 * h
    cs = h - 2 * inset
    has_chip = bool(chip_fill) and num is not None and str(num) != ""
    lead = inset + cs + 0.20 * h if has_chip else 0.42 * h
    gs = 0.62 * h if glyph else 0.0
    if glyph:
        lead = 0.36 * h + gs + 0.22 * h
    w = float(round(lead + tw + 0.42 * h))
    marg = int(math.ceil(0.18 * h * 3 + 0.1 * h))
    size = (int(w) + 2 * marg, int(h) + 2 * marg)
    box = (marg, marg, marg + w, marg + h)
    big = _layer((size[0] * SS, size[1] * SS))
    bx0, by0, bx1, by1 = [v * SS for v in box]
    base = _layer(big.size)
    bd = ImageDraw.Draw(base)
    bd.rounded_rectangle((bx0, by0, bx1, by1), h * SS / 2, fill=rgba8(fill))
    if fill == "surface":   # the HUD's hairline rim
        bd.rounded_rectangle((bx0, by0, bx1, by1), h * SS / 2, outline=rgba8("rim"), width=max(1, round(1.5 * k * SS)))
    big = Image.alpha_composite(big, base)
    top = _layer(big.size)
    td = ImageDraw.Draw(top)
    x = bx0 + lead * SS
    cy = (by0 + by1) / 2.0
    if has_chip:
        c0x, c0y = bx0 + inset * SS, by0 + inset * SS
        td.rounded_rectangle((c0x, c0y, c0x + cs * SS, c0y + cs * SS), 0.26 * cs * SS, fill=rgba8(chip_fill))
        cf = font(max(4, round(cs * 0.64 * SS)), "Bold")
        db = cf.getbbox(str(num), anchor="ls")
        td.text((c0x + cs * SS / 2 - (db[0] + db[2]) / 2, c0y + cs * SS / 2 - (db[1] + db[3]) / 2), str(num),
                font=cf, fill=rgba8(chip_ink), anchor="ls")
    if glyph:
        headphones_glyph(td, bx0 + (0.36 * h + gs / 2) * SS, cy - 0.02 * h * SS, gs * SS, rgba8(ink))
    tx, ty = _text_top_left(f, text, left=x, cy=cy)
    td.text((tx, ty), text, font=f, fill=rgba8(ink), anchor="ls")
    big = Image.alpha_composite(big, top)
    img = Image.alpha_composite(_shadow(size, box, h / 2, h, shadow), _down(big))
    sp = Sprite(img, box)
    with _lock:
        _sprites[key] = sp
    return sp


def text_sprite(text, style, k=1.0):
    """Plain white text with a soft --shade-strong shadow (end card date / studio)."""
    key = ("txt", text, style, round(k, 4))
    with _lock:
        if key in _sprites:
            return _sprites[key]
    px = TEXT_STYLES[style] * k
    f = font(max(4, round(px * SS)), "SemiBold" if style == "date" else "Medium")
    tw = f.getlength(text) / SS
    h = px * 1.3
    marg = int(math.ceil(px * 0.5))
    size = (int(math.ceil(tw)) + 2 * marg, int(math.ceil(h)) + 2 * marg)
    box = (marg, marg, marg + tw, marg + h)
    big = _layer((size[0] * SS, size[1] * SS))
    tx, ty = _text_top_left(f, text, left=marg * SS, cy=(marg + h / 2) * SS)
    ImageDraw.Draw(big).text((tx, ty), text, font=f, fill=WHITE, anchor="ls")
    fg = _down(big)
    r, g, b, sa = TOK["shade-strong"]
    a = fg.getchannel("A").filter(ImageFilter.GaussianBlur(px * 0.12)).point(lambda v: int(round(min(255, v * sa * 2.5))))
    sh = Image.new("RGBA", size, (r, g, b, 0))
    sh.putalpha(a)
    sh = ImageChops_offset(sh, 0, max(1, round(px * 0.05)))
    sp = Sprite(Image.alpha_composite(sh, fg), box)
    with _lock:
        _sprites[key] = sp
    return sp


def ImageChops_offset(img, dx, dy):
    out = _layer(img.size)
    out.paste(img, (dx, dy))
    return out


def sprite_for(spec, k):
    style = spec.get("style", "line")
    if style in STYLES:
        return capsule_sprite(spec["text"], style, spec.get("num"), k, spec.get("shadow", "shade"))
    if style in TEXT_STYLES:
        return text_sprite(spec["text"], style, k)
    raise ValueError("unknown title style %r (have %s)" % (style, ", ".join(list(STYLES) + list(TEXT_STYLES))))


# ------------------------------------------------------------------------------------------------ placement
def place_box(pos, bw, bh, W, H, ctx=None):
    """Top-left of a bw x bh box (the capsule) for a position name; ctx carries the logo box for under_logo."""
    k = min(W, H) / 1080.0
    if isinstance(pos, dict):
        return pos.get("x", 0.5) * W - bw / 2, pos.get("y", 0.5) * H - bh / 2
    if pos == "under_logo" and ctx and ctx.get("logo_box"):
        lx0, ly0, lx1, ly1 = ctx["logo_box"]
        return (W - bw) / 2, ly1 + 51 * k
    y = H - BOTTOM_PX * k - bh
    if pos == "br":
        return W - SAFE * W - 12 * k - bw, y
    if pos == "bl":
        return SAFE * W + 12 * k, y
    return (W - bw) / 2, y


def title_scale(spec, W, H):
    """Output scale of a caption: the frame's short side / 1080 (a 9:16 frame sizes like 16:9), times its "scale"."""
    return min(W, H) / 1080.0 * float(spec.get("scale", 1.0))


def title_box(spec, W, H, ctx=None):
    """The final (scale 1) box of a title in frame px: (x0, y0, x1, y1). check_trailer's safe-zone test uses it."""
    sp = sprite_for(spec, title_scale(spec, W, H))
    bw, bh = sp.box[2] - sp.box[0], sp.box[3] - sp.box[1]
    x, y = place_box(spec.get("pos", "bottom"), bw, bh, W, H, ctx)
    return (x, y, x + bw, y + bh)


def over(frame, img_arr, x, y, alpha=1.0):
    """Straight-alpha float RGBA (0..1) over a uint8 sRGB frame, in place, at integer top-left (x, y)."""
    H, W = frame.shape[:2]
    h, w = img_arr.shape[:2]
    x, y = int(round(x)), int(round(y))
    x0, y0, x1, y1 = max(0, x), max(0, y), min(W, x + w), min(H, y + h)
    if x1 <= x0 or y1 <= y0 or alpha <= 0.0:
        return
    s = img_arr[y0 - y:y1 - y, x0 - x:x1 - x]
    a = s[..., 3:4] * alpha
    reg = frame[y0:y1, x0:x1].astype(np.float32) * (1.0 / 255.0)
    out = s[..., :3] * a + reg * (1.0 - a)
    frame[y0:y1, x0:x1] = (np.clip(out, 0.0, 1.0) * 255.0 + 0.5).astype(np.uint8)


def draw_sprite(frame, sp, box_x, box_y, alpha=1.0, scale=1.0):
    """Draws sp so that its box's top-left lands at (box_x, box_y) at scale 1; scale works about the box centre."""
    bx0, by0, bx1, by1 = sp.box
    if abs(scale - 1.0) < 1e-4:
        over(frame, sp.array(), box_x - bx0, box_y - by0, alpha)
        return
    w, h = sp.img.size
    nw, nh = max(1, int(round(w * scale))), max(1, int(round(h * scale)))
    im = sp.img.resize((nw, nh), Image.Resampling.LANCZOS)
    cx, cy = box_x + (bx1 - bx0) / 2.0, box_y + (by1 - by0) / 2.0
    lx, ly = (bx0 + bx1) / 2.0 * nw / w, (by0 + by1) / 2.0 * nh / h
    over(frame, np.asarray(im, dtype=np.float32) / 255.0, cx - lx, cy - ly, alpha)


def draw_title(frame, spec, tl, ctx=None, anim=None):
    """One caption tl seconds after it appeared (anim = (dur, scale0), default the cut-in 0.15 s / 0.92)."""
    H, W = frame.shape[:2]
    sp = sprite_for(spec, title_scale(spec, W, H))
    bw, bh = sp.box[2] - sp.box[0], sp.box[3] - sp.box[1]
    x, y = place_box(spec.get("pos", "bottom"), bw, bh, W, H, ctx)
    dur, s0 = anim or (APPEAR_S, APPEAR_SCALE)
    s, a = appear(tl, dur, s0)
    draw_sprite(frame, sp, x, y, a * spec.get("alpha", 1.0), s)


# ------------------------------------------------------------------------------------------------ logo
_logo_src = None


def _logo_source():
    global _logo_src
    with _lock:
        if _logo_src is None:
            _logo_src = Image.open(LOGO).convert("RGBA")
        return _logo_src


def logo_image(width):
    """refluff_logo.png at `width` px (Lanczos; PIL resizes RGBA premultiplied). Only resting sizes are cached."""
    width = max(8, int(round(width)))
    key = ("logo", width)
    with _lock:
        if key in _logos:
            return _logos[key]
    src = _logo_source()
    im = src.resize((width, max(1, int(round(src.height * width / src.width)))), Image.Resampling.LANCZOS)
    arr = np.asarray(im, dtype=np.float32) / 255.0
    with _lock:
        _logos[key] = (im, arr)
    return im, arr


def logo_rest_box(cfg, W, H, width_def, cy_def):
    src = _logo_source()
    w = cfg.get("width", width_def) * W
    h = w * src.height / src.width
    cx, cy = cfg.get("cx", 0.5) * W, cfg.get("cy", cy_def) * H
    return (cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2)


def draw_logo(frame, box, scale=1.0, alpha=1.0):
    x0, y0, x1, y1 = box
    w = x1 - x0
    if abs(scale - 1.0) < 1e-4:
        im, arr = logo_image(w)
    else:
        base, _ = logo_image(w * 1.1)       # a cached work copy near the size: cheaper than resampling the 3015 px master
        nw = max(8, int(round(w * scale)))
        arr = np.asarray(base.resize((nw, max(1, int(round(base.height * nw / base.width)))), Image.Resampling.LANCZOS),
                         dtype=np.float32) / 255.0
    h, wi = arr.shape[:2]
    over(frame, arr, (x0 + x1) / 2 - wi / 2, (y0 + y1) / 2 - h / 2, alpha)


# ------------------------------------------------------------------------------------------------ foam bubbles (#03)
def bubble_sprite(r):
    """A pink-violet foam bubble like the ones on the logo: lavender body, violet rim, white highlight."""
    r = max(3, int(round(r)))
    key = ("bubble", r)
    with _lock:
        if key in _sprites:
            return _sprites[key]
    R = r * SS
    n = 2 * R + 4 * SS
    yy, xx = np.mgrid[0:n, 0:n].astype(np.float32)
    c = n / 2.0
    d = np.sqrt((xx - c) ** 2 + (yy - c) ** 2) / R
    inner, edge, rim = np.array([255, 238, 252.]), np.array([218, 146, 238.]), np.array([150, 84, 206.])
    t = np.clip(d, 0, 1)[..., None] ** 2
    col = inner * (1 - t) + edge * t
    rt = np.clip((d - 0.80) / 0.20, 0, 1)[..., None]
    col = col * (1 - rt) + rim * rt
    alpha = np.where(d <= 1.0, 0.94, 0.0)
    # specular highlights (upper left big, lower right small)
    h1 = np.clip(1.0 - np.sqrt(((xx - (c - 0.36 * R)) / (0.26 * R)) ** 2 + ((yy - (c - 0.40 * R)) / (0.17 * R)) ** 2), 0, 1)
    h2 = np.clip(1.0 - np.sqrt(((xx - (c + 0.34 * R)) / (0.10 * R)) ** 2 + ((yy - (c + 0.38 * R)) / (0.08 * R)) ** 2), 0, 1)
    hl = np.clip(np.minimum(h1 * 3.0, 1.0) * 0.95 + np.minimum(h2 * 3.0, 1.0) * 0.6, 0, 1)[..., None] * (d <= 1.0)[..., None]
    col = col * (1 - hl) + 255.0 * hl
    rgba = np.dstack([col, alpha * 255.0]).clip(0, 255).astype(np.uint8)
    img = _down(Image.fromarray(rgba, "RGBA"))
    sp = Sprite(img, (0, 0, img.width, img.height))
    with _lock:
        _sprites[key] = sp
    return sp


# (delay s, angle deg (0 = right, -90 = up), distance in logo widths, radius in logo heights, life s)
BUBBLES = [(0.05, -35.0, 0.20, 0.070, 1.00), (0.09, 10.0, 0.19, 0.056, 1.12),
           (0.13, -70.0, 0.16, 0.048, 1.24), (0.17, 45.0, 0.15, 0.042, 1.36)]
BUBBLE_EMIT = (0.885, 0.38)             # behind the last "f" of the logo, fractions of the logo image


def draw_bubbles(frame, box, scale, tl):
    x0, y0, x1, y1 = box
    lw, lh = (x1 - x0) * scale, (y1 - y0) * scale
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    ex, ey = cx + (BUBBLE_EMIT[0] - 0.5) * lw, cy + (BUBBLE_EMIT[1] - 0.5) * lh
    for delay, ang, dist, rad, life in BUBBLES:
        t = tl - delay
        if t <= 0 or t >= life + 0.14:
            continue
        e = ease_out_cubic(t / 0.55)
        a = math.radians(ang)
        px = ex + math.cos(a) * dist * lw * e
        py = ey + math.sin(a) * dist * lw * e - 0.03 * lw * t
        s = 0.35 + 0.65 * ease_out_cubic(t / 0.25)
        alpha = 1.0
        if t > life:
            p = (t - life) / 0.14
            s *= 1.0 + 0.3 * p
            alpha = 1.0 - p
        sp = bubble_sprite(rad * lh * s)
        w, h = sp.img.size
        over(frame, sp.array(), px - w / 2, py - h / 2, alpha)


# ------------------------------------------------------------------------------------------------ #03 logo whip
WHIP_WIDTH, WHIP_CY = 0.60, 0.43


def whip_scale(tl):
    """Logo scale 0.9 -> 1.03 -> 1 in 0.4 s (TRAILER.md 6) and its alpha (in over 0.1 s)."""
    if tl <= 0:
        return 0.9, 0.0
    if tl < 0.24:
        s = 0.9 + 0.13 * ease_out_cubic(tl / 0.24)
    else:
        s = 1.03 - 0.03 * smoothstep((tl - 0.24) / 0.16)
    return s, smoothstep(tl / 0.10)


def draw_logo_whip(frame, tl, cfg, ctx):
    """The logo "whipping up" over the shot (bubbles fly out from behind the f). Sets ctx["logo_box"] (rest box)."""
    H, W = frame.shape[:2]
    box = logo_rest_box(cfg, W, H, WHIP_WIDTH, WHIP_CY)
    ctx["logo_box"] = box
    tl -= cfg.get("at", 0.0)
    if tl < 0:
        return
    s, a = whip_scale(tl)
    if cfg.get("bubbles", True):
        draw_bubbles(frame, box, s, tl)
    draw_logo(frame, box, s, a)


def whip_boxes(cfg, W, H):
    x0, y0, x1, y1 = logo_rest_box(cfg, W, H, WHIP_WIDTH, WHIP_CY)
    cx, cy, w, h = (x0 + x1) / 2, (y0 + y1) / 2, (x1 - x0) * 1.03, (y1 - y0) * 1.03
    out = {"logo": (cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2)}
    # the far edge of every bubble path (at its pop size) must stay title-safe too
    ex, ey = x0 + BUBBLE_EMIT[0] * (x1 - x0), y0 + BUBBLE_EMIT[1] * (y1 - y0)
    bx = []
    for _, ang, dist, rad, life in BUBBLES:
        a = math.radians(ang)
        r = rad * (y1 - y0) * 1.3
        px = ex + math.cos(a) * dist * (x1 - x0)
        py = ey + math.sin(a) * dist * (x1 - x0) - 0.03 * (x1 - x0) * (life + 0.14)
        bx.append((px - r, py - r, px + r, py + r))
    out["bubbles"] = (min(b[0] for b in bx), min(b[1] for b in bx), max(b[2] for b in bx), max(b[3] for b in bx))
    return out


# ------------------------------------------------------------------------------------------------ #21 end card
END_WIDTH, END_CY = 0.56, 0.36
END_DEFAULTS = {"cta": "Wishlist now on Steam", "date": "October 2026", "studio": "Vahov Games",
                "logo_in": 0.0, "cta_in": 0.45, "date_in": 0.70, "studio_in": 0.90}


def endcard_layout(cfg, W, H):
    """{"logo": box, "cta": (spec, box), "date": (spec, box), "studio": (spec, box)} at rest."""
    k = min(W, H) / 1080.0
    c = dict(END_DEFAULTS, **cfg)
    logo = logo_rest_box(cfg, W, H, END_WIDTH, END_CY)
    out = {"logo": logo}
    cta = {"text": c["cta"], "style": "wishlist"}
    sp = sprite_for(cta, k * float(c.get("cta_scale", 1.0)))
    bw, bh = sp.box[2] - sp.box[0], sp.box[3] - sp.box[1]
    cy0 = c.get("cta_y", 0.63) * H
    out["cta"] = (cta, sp, ((W - bw) / 2, cy0, (W + bw) / 2, cy0 + bh))
    date = {"text": c["date"], "style": "date"}
    sp = sprite_for(date, k)
    bw, bh = sp.box[2] - sp.box[0], sp.box[3] - sp.box[1]
    dy = cy0 + (out["cta"][2][3] - cy0) + 66 * k - bh / 2
    out["date"] = (date, sp, ((W - bw) / 2, dy, (W + bw) / 2, dy + bh))
    studio = {"text": c["studio"], "style": "studio"}
    sp = sprite_for(studio, k)
    bw, bh = sp.box[2] - sp.box[0], sp.box[3] - sp.box[1]
    sy = H - 84 * k - bh / 2
    out["studio"] = (studio, sp, ((W - bw) / 2, sy, (W + bw) / 2, sy + bh))
    return out


def draw_endcard(frame, tl, cfg):
    """Logo fades in calmly (alpha 0.8 s, scale 0.97 -> 1), then the call to action, the date layer, the studio."""
    H, W = frame.shape[:2]
    c = dict(END_DEFAULTS, **cfg)
    lay = endcard_layout(cfg, W, H)
    t = tl - c["logo_in"]
    if t > 0:
        draw_logo(frame, lay["logo"], 0.97 + 0.03 * ease_out_cubic(t / 1.2), smoothstep(t / 0.8))
    for name in ("cta", "date", "studio"):
        t = tl - c[name + "_in"]
        if t <= 0 or not c.get(name):
            continue
        spec, sp, box = lay[name]
        s, a = appear(t, 0.35, 0.94) if name == "cta" else (1.0, smoothstep(t / 0.4))
        draw_sprite(frame, sp, box[0], box[1], a, s)


def endcard_boxes(cfg, W, H):
    lay = endcard_layout(cfg, W, H)
    return {"logo": lay["logo"], "cta": lay["cta"][2], "date": lay["date"][2], "studio": lay["studio"][2]}


def safe_rect(W, H):
    return (SAFE * W, SAFE * H, (1 - SAFE) * W, (1 - SAFE) * H)


def inside(box, rect, tol=0.5):
    return box[0] >= rect[0] - tol and box[1] >= rect[1] - tol and box[2] <= rect[2] + tol and box[3] <= rect[3] + tol


if __name__ == "__main__":   # quick look: python titles.py OUT.png [bg.png]
    import sys
    W, H = 1920, 1080
    bg = np.asarray(Image.open(sys.argv[2]).convert("RGB").resize((W, H))) if len(sys.argv) > 2 else \
        np.full((H, W, 3), (120, 60, 50), np.uint8)
    fr = bg.copy()
    ctx = {}
    draw_logo_whip(fr, 0.5, {}, ctx)
    draw_title(fr, {"text": "Headphones on", "style": "headphones", "pos": "under_logo"}, 1.0, ctx)
    draw_title(fr, {"text": "SCRAPE", "style": "verb", "num": 1, "pos": {"x": 0.25, "y": 0.9}}, 1.0)
    draw_title(fr, {"text": "REFLUFF", "style": "refluff", "num": 8, "pos": {"x": 0.72, "y": 0.9}}, 1.0)
    draw_title(fr, {"text": "Get paid.", "style": "line", "pos": "br"}, 1.0)
    Image.fromarray(fr).save(sys.argv[1] if len(sys.argv) > 1 else "titles_demo.png")
