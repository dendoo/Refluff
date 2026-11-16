"""Steam achievement icons for "Refluff: Carpet Cleaning ASMR" ("rug patch badges").

    python -B Tools/steam_art/make_achievement_icons.py

Reads SteamStore/achievements/source_en.json (api + emblem of every achievement, in AchievementId order) and writes
    SteamStore/achievements/icons/<API>.png         256x256 opaque RGB, the achieved icon
    SteamStore/achievements/icons/<API>_locked.png  256x256 opaque RGB, the unachieved icon (Steam does not derive it)
    SteamStore/_preview/achievements_sheet.png      review only: every icon at 256 / 64 / 32 px on Steam's #1b2838,
                                                    locked beside unlocked, the API name under each
Deterministic (fixed seeds, no timestamps): a rerun writes byte-identical files. Needs make_logo.py (same folder).

Style: a full-bleed square madder-red rug tile (make_logo.render_rug with the App Icon preset P_ICON: indigo serged
binding, cream guard-stitch dots) carries ONE centred, flat, pillow-shaded emblem, ~55-65 % of the side, ~20-26 % of
the tile's area (thin shapes: the bolt, the sparkle, less; printed per icon) and at least 10 px clear of the stitch line at 256, with an
indigo keyline (#222849, 0.020 of the side) and the logo's contact shadow; light from the top left (make_logo.LIGHT).
Palette only from the existing art (cream wool, water blue, gold, teal, amber, brush blue, the in-game foam pink,
make_logo sparkle). No text or digits on the icons.

Art-direction rules (review 2026-09-30): the three textile / box emblems get different aspect ratios so their grey
locked silhouettes differ at 32 px - FIRST_CARPET a tilted landscape rug with one rolled end, REGULARS a portrait
stack of folded slabs, FULL_KIT a toolbox with two tool heads rising above its handle. Both rugs share one rug
layout (cream border band with a zigzag, stepped medallion). Thin shapes (the bolt) shade with a linear top-left
gradient + rim light, not the pillow (its medial-axis crease reads as a fold). Only the two largest foam bubbles carry
specular dots, and the foam has no second (violet) outline.

Locked variant = the same render, not re-drawn: Rec.709 luma; the tile maps to 0.07 + 0.50 y, the emblem's fill to
0.30 + 0.30 y (one stop under the old silver look: a locked gold medal must not read as an earned silver one; dark
accents still clear the tile, and keylines stay dark, so the silhouette survives), secondary mini elements (the small
sparkle) to a dimmer band; then a cool tint (0.93, 0.97, 1.04) toward Steam's #1b2838. No padlock.
"""
import json
import os
import sys
import time

sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import numpy as np                      # noqa: E402
from PIL import Image, ImageDraw        # noqa: E402
from scipy import ndimage as ndi        # noqa: E402

import make_logo as L                   # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
SRC = os.path.join(ROOT, "SteamStore", "achievements", "source_en.json")
OUT = os.path.join(ROOT, "SteamStore", "achievements", "icons")
SHEET = os.path.join(ROOT, "SteamStore", "_preview", "achievements_sheet.png")

SIDE = 256                  # Steam achievement icon size
SS = L.SS                   # supersampling (3)
N = SIDE * SS
RUG_SEED = 32
KEY = 0.020 * N             # indigo keyline width
SHADOW = (0.010, 0.014, 0.004, 0.45)   # contact shadow: blur, dy, dx (of the side), strength (the logo's)

hx = L.hx
C = L.C
INDIGO = C["indigo"]
WHITE = np.ones(3, np.float32)


def mixc(a, b, t):
    return np.asarray(a, np.float32) * (1.0 - t) + np.asarray(b, np.float32) * t


# (lo, top, hi) pillow ramps: lo in the shade, top on the flat face, hi on the lit top-left rim
CREAM = (hx("#d8bf93"), hx("#f4e3c1"), hx("#fff8ea"))
BACKING = (hx("#b08c66"), hx("#d2b088"), hx("#e6cfa6"))       # make_logo wool_deep / wool_root: a rug's back
BLUE = (hx("#2e8cf0") * 0.58, hx("#2e8cf0"), hx("#5aa6f8"))
BRUSH = (hx("#3e7be8") * 0.58, hx("#3e7be8"), hx("#5aa6f8"))
GOLD = (hx("#ac6a00"), hx("#f0a832"), hx("#fbd27a"))
TEAL = (hx("#0f6f69"), hx("#169488"), hx("#14aea0"))
TEAL_BRIGHT = (hx("#169488"), hx("#14aea0"), mixc(hx("#14aea0"), hx("#fff8ea"), 0.35))
AMBER = (hx("#ac6a00"), hx("#f29216"), hx("#faac3c"))
WARM = (hx("#f29216"), hx("#faac3c"), hx("#fbd27a"))          # the dryer's warm air
LID = (hx("#ac6a00") * 0.55, hx("#ac6a00") * 0.86, mixc(hx("#ac6a00"), hx("#f29216"), 0.35))
STEEL = (mixc(C["indigo_hi"], CREAM[1], 0.22), mixc(C["indigo_hi"], CREAM[1], 0.50), mixc(C["indigo_hi"], CREAM[2], 0.78))
# the in-game foam (CarpetFoam.shader): crest hot pink, violet shadow, light-pink rim
FOAM_CREST = np.array([0.98, 0.40, 0.88], np.float32)
FOAM_SHADOW = np.array([0.45, 0.16, 0.72], np.float32)
FOAM_RIM = np.array([1.0, 0.75, 0.95], np.float32)

LUMA = np.array([0.2126, 0.7152, 0.0722], np.float32)
TINT = np.array([0.93, 0.97, 1.04], np.float32)
LOCK_TILE = (0.07, 0.50)    # y' = a + b * y
LOCK_FILL = (0.30, 0.30)    # emblem fill: 0.30 .. 0.60
LOCK_SEC = (0.22, 0.22)     # secondary mini elements: dimmer than the emblem

YY, XX = np.mgrid[0:N, 0:N].astype(np.float32) + 0.5
L2 = L.LIGHT[:2] / np.linalg.norm(L.LIGHT[:2])       # 2-D direction toward the light (screen, y down): top left


# ---- shape helpers (coordinates in fractions of the side) ------------------
def px(v):
    return v * N


def poly(pts):
    im = Image.new("L", (N, N), 0)
    ImageDraw.Draw(im).polygon([(x * N, y * N) for x, y in pts], fill=255)
    return np.asarray(im) > 127


def discs(items):
    """Union of discs [(cx, cy, r)] as a bool mask."""
    im = Image.new("L", (N, N), 0)
    d = ImageDraw.Draw(im)
    for cx, cy, r in items:
        d.ellipse([(cx - r) * N, (cy - r) * N, (cx + r) * N, (cy + r) * N], fill=255)
    return np.asarray(im) > 127


def disc(cx, cy, r):
    return np.hypot(XX - cx * N, YY - cy * N) <= r * N


def seg_dist(ax, ay, bx, by):
    """Distance (px) of every pixel to the segment a-b."""
    ax, ay, bx, by = ax * N, ay * N, bx * N, by * N
    dx, dy = bx - ax, by - ay
    t = np.clip(((XX - ax) * dx + (YY - ay) * dy) / (dx * dx + dy * dy + 1e-9), 0.0, 1.0)
    return np.hypot(XX - ax - t * dx, YY - ay - t * dy)


def capsule(ax, ay, bx, by, r):
    return seg_dist(ax, ay, bx, by) <= r * N


def rrect(x0, y0, x1, y1, r):
    return L.rrect_sd(XX, YY, x0 * N, y0 * N, x1 * N, y1 * N, r * N) > 0


def rrect_pts(x0, y0, x1, y1, r, n=10):
    """Rounded-rectangle outline as a point list (for rotated shapes)."""
    pts = []
    for cx, cy, a0 in ((x1 - r, y0 + r, -90), (x1 - r, y1 - r, 0), (x0 + r, y1 - r, 90), (x0 + r, y0 + r, 180)):
        for k in range(n + 1):
            a = np.radians(a0 + 90.0 * k / n)
            pts.append((cx + r * np.cos(a), cy + r * np.sin(a)))
    return pts


def rot(pts, cx, cy, deg):
    """Local points (around 0, 0) rotated by deg (clockwise on screen) and moved to (cx, cy)."""
    a = np.radians(deg)
    c, s = np.cos(a), np.sin(a)
    return [(cx + x * c - y * s, cy + x * s + y * c) for x, y in pts]


def local(cx, cy, deg):
    """Inverse of rot(): every pixel's local (x, y) (fractions of the side) in a frame at (cx, cy) turned by deg."""
    a = np.radians(deg)
    c, s = np.cos(a), np.sin(a)
    dx, dy = XX / N - cx, YY / N - cy
    return dx * c + dy * s, -dx * s + dy * c


def soft(mask):
    return np.clip(L.signed_distance(mask) + 0.5, 0.0, 1.0)


def band(sd, a, b):
    """Anti-aliased band a <= sd <= b (px)."""
    return np.clip(sd - a + 0.5, 0, 1) * np.clip(b - sd + 0.5, 0, 1)


# ---- badge: emblem layers + the masks the locked variant needs --------------
class Badge:
    """Accumulates the emblem (premultiplied RGBA over a transparent canvas) and three coverage masks:
    fill = primary emblem fill, sec = secondary mini elements, sil = what casts the contact shadow.
    Keylines cover fill / sec (they stay dark in the locked icon, so the silhouette survives)."""

    def __init__(self):
        self.em = np.zeros((N, N, 4), np.float32)
        self.fill = np.zeros((N, N), np.float32)
        self.sec = np.zeros((N, N), np.float32)
        self.sil = np.zeros((N, N), np.float32)

    def mark(self, cover, fill, secondary=False, shadow=True):
        self.fill *= 1.0 - cover
        self.sec *= 1.0 - cover
        if secondary:
            self.sec += fill
        else:
            self.fill += fill
        if shadow:
            np.maximum(self.sil, cover, out=self.sil)

    def keyline(self, mask, width=KEY, secondary=False, shadow=True):
        sd = mask if mask.dtype != bool else L.signed_distance(mask)
        a = np.clip(sd + width + 0.5, 0, 1)
        L.over(self.em, L.layer(INDIGO, a))
        self.mark(a, np.zeros_like(a), secondary, shadow)

    def flat(self, col, alpha, secondary=False):
        L.over(self.em, L.layer(np.clip(col, 0, 1), alpha))
        self.mark(alpha, alpha, secondary, shadow=False)

    def _paint(self, sd, col, key, secondary, shadow):
        if key > 0:
            self.keyline(sd, key, secondary, shadow)
        a = np.clip(sd + 0.5, 0, 1)
        L.over(self.em, L.layer(np.clip(col, 0, 1), a))
        self.mark(a, a, secondary, shadow)

    def pillow(self, mask, pal, radius=0.07, key=KEY, secondary=False, shadow=True):
        """Flat pillow-shaded shape (bool mask, or a signed distance in px) with its own keyline; returns (sd, shade)
        for details on it."""
        sd = L.signed_distance(mask) if mask.dtype == bool else mask
        _, shade = L.pillow_shade(sd, radius * N, 0.004 * N)
        lo, top, hi = pal
        t = np.clip(shade, 0, 1)[..., None]
        col = lo * (1 - t) + top * t + (hi - top) * np.clip(t - 0.85, 0, 1) / 0.15 * 0.6
        self._paint(sd, col, key, secondary, shadow)
        return sd, shade

    def gradient(self, mask, pal, rim=0.012, key=KEY, secondary=False, shadow=True):
        """Thin shapes: a linear top-left gradient over the whole shape + a lit rim on the edges facing the light
        (a pillow on a narrow shape creases along its medial axis). Returns sd."""
        sd = L.signed_distance(mask)
        ys, xs = np.nonzero(mask)
        cx, cy = xs.mean(), ys.mean()
        proj = (XX - cx) * L2[0] + (YY - cy) * L2[1]
        u = np.clip(proj / (np.abs(proj[mask]).max() + 1e-6), -1, 1)[..., None]
        lo, top, hi = pal
        col = top + (hi - top) * np.clip(u, 0, 1) * 0.55 + (lo - top) * np.clip(-u, 0, 1) * 0.60
        gy, gx = np.gradient(sd)
        g = np.hypot(gx, gy) + 1e-6
        face = (-gx * L2[0] - gy * L2[1]) / g                   # outward normal . light
        edge = band(sd, 0.0, rim * N)
        col = mixc(col, hi, (edge * np.clip(face, 0, 1) ** 0.7 * 0.9)[..., None])
        col = mixc(col, lo, (edge * np.clip(-face, 0, 1) ** 0.7 * 0.45)[..., None])
        self._paint(sd, col, key, secondary, shadow)
        return sd


def lit(col, shade, k=0.30):
    """A detail colour lit by the shading of the shape it sits on (flat face ~ 1)."""
    return np.asarray(col, np.float32) * (1.0 - k * 0.78 + k * shade)[..., None]


# ---- rug tile, compose, locked ------------------------------------------------
def rug_tile():
    tile_sd = L.rrect_sd(XX - 0.5, YY - 0.5, -1, -1, N + 1, N + 1, 0.0)
    rgb, _ = L.render_rug(tile_sd, 0.9 * N, RUG_SEED, dict(L.P_ICON))
    return rgb


def stitch_inner():
    """Inner edge of the guard-stitch dots, as a fraction of the side (P_ICON guard, rug size 0.9 N)."""
    inset, r, _ = L.P_ICON["guard"]
    return 0.9 * (inset + r)


def compose(rug, b):
    blur, dy, dx, k = SHADOW
    sh = ndi.shift(L.blur(b.sil, blur * N), (dy * N, dx * N), order=1)
    out = np.concatenate([rug * (1.0 - k * np.clip(sh, 0, 1))[..., None], np.ones((N, N, 1), np.float32)], axis=2)
    L.over(out, b.em)
    return np.clip(out[..., :3], 0, 1)


def locked(rgb, b):
    y = rgb @ LUMA
    f = np.clip(b.fill, 0, 1)
    s = np.clip(b.sec, 0, 1) * (1 - f)
    yt = LOCK_TILE[0] + LOCK_TILE[1] * y
    yf = LOCK_FILL[0] + LOCK_FILL[1] * y
    ys = LOCK_SEC[0] + LOCK_SEC[1] * y
    yl = yt * (1 - f - s) + yf * f + ys * s
    return np.clip(yl[..., None] * TINT, 0, 1)


def reduce3(rgb):
    return L.reduce_ss(np.concatenate([rgb, np.ones(rgb.shape[:2] + (1,), np.float32)], axis=2))[..., :3]


def to_rgb8(rgb):
    return Image.fromarray((np.clip(rgb, 0, 1) * 255.0 + 0.5).astype(np.uint8), "RGB")


# ---- rug parts shared by FIRST_CARPET and REGULARS ------------------------------
def tri(t):
    """Triangle wave 0..1..0 with period 1."""
    f = t - np.floor(t)
    return 1.0 - np.abs(2.0 * f - 1.0)


def stepped(dx, dy, w, h, n=3):
    """Stepped (latch-hook) diamond of half-size w x h in n steps, as a bool mask of local offsets."""
    m = np.zeros(dx.shape, bool)
    ax, ay = np.abs(dx), np.abs(dy)
    for k in range(n):
        m |= (ax <= w * (n - k) / n) & (ay <= h * (k + 1) / n)
    return m


def rug_face(b, lx, ly, rect, clip, shade, field, med, e=(0.018, 0.052), period=0.034, med_at=(0.0, 0.0),
             med_size=(0.085, 0.070), steps=3):
    """The rug layout on a face in local coords (lx, ly): a cream border band e[0]..e[1] in from the edges of rect
    (x0, x1, y0, y1) carrying a zigzag in the field's dark tone, and a stepped medallion (colour med) with a cream
    stepped core and a field-coloured eye. clip = where the face is visible; shade = the face's pillow light."""
    x0, x1, y0, y1 = rect
    dxe = np.minimum(lx - x0, x1 - lx)
    dye = np.minimum(ly - y0, y1 - ly)
    ee = np.minimum(dxe, dye)
    s = np.where(dye <= dxe, lx, ly)                      # along the nearest edge
    inband = clip & (ee >= e[0]) & (ee <= e[1])
    b.flat(lit(CREAM[1], shade), soft(inband))
    zc = e[0] + (e[1] - e[0]) * (0.24 + 0.52 * tri(s / period))
    zig = inband & (np.abs(ee - zc) <= 0.0068)
    b.flat(lit(field[0], shade, 0.2), soft(zig))
    mx, my = med_at
    mw, mh = med_size
    outer = clip & stepped(lx - mx, ly - my, mw, mh, steps)
    inner = clip & stepped(lx - mx, ly - my, mw * 0.56, mh * 0.56, steps)
    b.flat(lit(C["indigo"], shade, 0.1), soft(clip & stepped(lx - mx, ly - my, mw + 0.009, mh + 0.009, steps)))
    b.flat(lit(med[1], shade), soft(outer))
    b.flat(lit(CREAM[1], shade), soft(inner))
    b.flat(lit(field[1], shade), soft(clip & (np.abs(lx - mx) / (mw * 0.20) + np.abs(ly - my) / (mh * 0.20) <= 1)))


def tassel(root, sgn, ln, bend, r0, cx, cy, ang, knot=0.30, curl=0.12, taper=0.35, n=26):
    """One thick fringe tassel in local coords: from root outward (sign sgn along local x) by ln, the tip bent by
    bend along local y (a quadratic curve; curl < 0.5 droops toward the end), a knot a quarter of the way out,
    tapering by taper at the tip. Returns screen discs [(x, y, r)]."""
    (x0, y0) = root
    p0 = np.array([x0, y0])
    p2 = np.array([x0 + sgn * ln, y0 + bend])
    p1 = np.array([x0 + sgn * ln * 0.55, y0 + bend * curl])
    out = []
    for s in np.linspace(0.0, 1.0, n):
        p = (1 - s) ** 2 * p0 + 2 * (1 - s) * s * p1 + s * s * p2
        r = r0 * (1.0 - taper * s) + knot * r0 * np.exp(-((s - 0.24) / 0.08) ** 2)
        (x, y), = rot([tuple(p)], cx, cy, ang)
        out.append((x, y, r))
    return out


# uneven fringe of the loose end: (y along the end, length, bend, root radius) per tassel
FRINGE7 = [(-0.142, 0.056, 0.004, 0.0112), (-0.098, 0.070, 0.016, 0.0126), (-0.050, 0.050, 0.008, 0.0106),
           (-0.008, 0.066, 0.024, 0.0122), (0.040, 0.060, 0.012, 0.0114), (0.086, 0.074, 0.030, 0.0128),
           (0.134, 0.054, 0.018, 0.0110)]


def em_first_carpet(b):
    """A single rug from above, tilted, its right end rolled up: teal field, cream border band with a zigzag, gold
    stepped medallion, seven thick uneven drooping tassels on the loose end; the roll shows the rug's back and, on
    its near end, the spiral of the rolled pile. Landscape and asymmetric (REGULARS is a portrait stack)."""
    cx, cy, ang = 0.486, 0.492, -13.0
    hw, hh = 0.262, 0.168                     # the rug, loose end at -hw
    xa, rr = 0.203, 0.064                     # roll axis (local x) and radius
    lx, ly = local(cx, cy, ang)
    items = []
    for y, ln, bend, r0 in FRINGE7:
        items += tassel((-hw + 0.010, y), -1, ln, bend, r0, cx, cy, ang)
    b.pillow(discs(items), CREAM, radius=0.010, key=0.55 * KEY)
    body = poly(rot(rrect_pts(-hw, -hh, xa, hh, 0.010), cx, cy, ang))
    sd, shade = b.pillow(body, TEAL, radius=0.030)
    rug_face(b, lx, ly, (-hw, hw + 0.08, -hh, hh), sd > 0, shade, TEAL, GOLD, med_at=(-0.040, 0.0),
             med_size=(0.092, 0.078))
    # the roll: the rug's back outside, seen a little from the front so its near end shows
    ek = 0.46                                             # end ellipse: minor / major
    tube = (np.abs(lx - xa) <= rr) & (np.abs(ly) <= hh)
    tube |= ((lx - xa) / rr) ** 2 + ((ly + hh) / (rr * ek)) ** 2 <= 1.0
    endm = ((lx - xa) / rr) ** 2 + ((ly - hh) / (rr * ek)) ** 2 <= 1.0
    sdr, shr = b.pillow(tube | endm, BACKING, radius=0.050)
    b.flat(lit(BACKING[0], shr, 0.2), soft(tube & (ly < hh - 0.004) & (np.abs(lx - (xa + 0.42 * rr)) < 0.0036)) * 0.8)
    sde, she = b.pillow(endm, BACKING, radius=0.022, key=0.6 * KEY, shadow=False)
    u, v = (lx - xa) / rr, (ly - hh) / (rr * ek)
    rho, phi = np.hypot(u, v), np.arctan2(v, u)
    spiral = endm & (sde > px(0.004)) & (rho > 0.16) & (((rho * 2.1 - phi / (2 * np.pi)) % 1.0) < 0.46)
    b.flat(lit(TEAL[1], she, 0.2), soft(spiral))
    b.flat(C["indigo_lo"], soft(endm & (rho <= 0.16)))


def spark_masks(cx, cy, span, lw):
    """Fill / keyline coverage of make_logo.draw_spark(buf, cx, cy, span, lw) (px), same polygon."""
    n = int(span * 1.25 + 4 * lw)
    x0, y0 = int(cx - n / 2), int(cy - n / 2)
    t = np.linspace(0, 2 * np.pi, 1440, endpoint=False)
    c, s = np.cos(t), np.sin(t)
    rr = span / 2.0
    xs, ys = np.sign(c) * np.abs(c) ** 3.1 * rr, np.sign(s) * np.abs(s) ** 3.1 * rr * 1.1
    im = Image.new("L", (N, N), 0)
    ImageDraw.Draw(im).polygon(list(zip(x0 + n / 2 + xs, y0 + n / 2 + ys)), fill=255)
    sd = L.signed_distance(np.asarray(im) > 127)
    return np.clip(sd + 0.5, 0, 1), np.clip(sd + lw + 0.5, 0, 1)


def em_spotless(b):
    """The brand sparkle: a big cream four-point star and a small second one (secondary)."""
    for (cx, cy, span, lw), sec in (((0.470, 0.530, 0.560, 0.022), False), ((0.760, 0.262, 0.155, 0.018), True)):
        fa, cov = spark_masks(px(cx), px(cy), px(span), px(lw))
        L.draw_spark(b.em, px(cx), px(cy), px(span), px(lw))
        b.mark(cov, fa, secondary=sec)


def arc_sd(cx, cy, r, a0, a1, w):
    """Signed distance (px, + inside) of a round-capped arc stroke of half-width w (degrees, clockwise on screen
    from +x, a0 < a1, no wrap through 180, span < 180)."""
    dx, dy = XX - cx * N, YY - cy * N
    ang = np.degrees(np.arctan2(dy, dx))
    ends = [np.hypot(XX - (cx + r * np.cos(np.radians(a))) * N, YY - (cy + r * np.sin(np.radians(a))) * N)
            for a in (a0, a1)]
    d = np.where((ang >= a0) & (ang <= a1), np.abs(np.hypot(dx, dy) - r * N), np.minimum(*ends))
    return w * N - d


def em_bone_dry(b):
    """A dryer fan: three swept bright-teal blades around a cream hub, and two bold keyed warm-air arcs off each of
    two opposite blade tips (primary fill: they carry the 'dry' cue into the locked icon and to 32 px)."""
    cx, cy = 0.5, 0.5
    for r, a0, a1 in ((0.338, -66.0, -16.0), (0.404, -59.0, -31.0), (0.338, 114.0, 164.0), (0.404, 121.0, 149.0)):
        b.pillow(arc_sd(cx, cy, r, a0, a1, 0.0155), WARM, radius=0.012, key=0.6 * KEY)
    items = []
    for k in range(3):
        for s in np.linspace(0.0, 1.0, 40):
            a = np.radians(-100.0 + 120.0 * k + 58.0 * s)
            r = 0.054 + 0.150 * s
            items.append((cx + r * np.cos(a), cy + r * np.sin(a), 0.027 + 0.058 * np.sin(0.5 * np.pi * s) ** 0.8))
    b.pillow(discs(items), TEAL_BRIGHT, radius=0.05)
    sd, shade = b.pillow(disc(cx, cy, 0.074), CREAM, radius=0.05)
    b.flat(lit(TEAL[1], shade), soft(disc(cx, cy, 0.024)))


def ribbon(p0, p1, w, notch):
    (x0, y0), (x1, y1) = p0, p1
    d = np.array([x1 - x0, y1 - y0])
    d /= np.linalg.norm(d)
    n = np.array([-d[1], d[0]]) * w / 2
    return poly([tuple(np.array(p0) + n), tuple(np.array(p1) + n), tuple(np.array(p1) - d * notch),
                 tuple(np.array(p1) - n), tuple(np.array(p0) - n)])


def star_pts(cx, cy, ro, ri):
    t = np.linspace(0, 2 * np.pi, 11)[:-1] - np.pi / 2
    r = np.where(np.arange(10) % 2 == 0, ro, ri)
    return list(zip(cx + r * np.cos(t), cy + r * np.sin(t)))


def em_combo(b):
    """The top prize: a gold coin medal with a cream star and two ribbon tails (teal, blue) below."""
    cx, cy, R = 0.5, 0.430, 0.222
    b.pillow(ribbon((cx - 0.050, cy + 0.10), (cx - 0.150, 0.812), 0.108, 0.050), TEAL, radius=0.035)
    b.pillow(ribbon((cx + 0.050, cy + 0.10), (cx + 0.150, 0.812), 0.108, 0.050), BLUE, radius=0.035)
    sd, shade = b.pillow(disc(cx, cy, R), GOLD, radius=0.05)
    ring = band(sd, px(0.036), px(0.047))
    b.flat(lit(GOLD[0], shade, 0.2), ring * 0.75)
    b.flat(lit(GOLD[2], shade, 0.2), band(sd, px(0.047), px(0.052)) * 0.8)
    b.pillow(poly(star_pts(cx, cy + 0.008, 0.128, 0.054)), CREAM, radius=0.02)


FOAM = [(0.42, 0.58, 0.17), (0.64, 0.52, 0.13), (0.55, 0.33, 0.11), (0.30, 0.40, 0.08), (0.72, 0.72, 0.07)]
FOAM_SCALE = 0.96                                          # set balance: the cluster was the heaviest emblem
FOAM_CORE = mixc(C["foam_mid"], FOAM_CREST, 0.62)          # bubble centre: toward the in-game crest pink
FOAM_BODY = mixc(C["foam_mid"], FOAM_CREST, 0.40)
FOAM_EDGE = mixc(C["foam_rim"], FOAM_CREST, 0.50)


def em_filthy(b):
    """A heap of pink foam bubbles (make_logo.draw_foam's smooth-min suds + bubble shading, re-drawn here so the set's
    rules hold): one indigo keyline and no violet rim around it, thin violet seams only between bubbles, the fill
    pushed toward the in-game crest pink, specular window + dot on the two largest bubbles only."""
    F = [(0.5 + (x - 0.505) * FOAM_SCALE, 0.5 + (y - 0.505) * FOAM_SCALE, r * FOAM_SCALE) for x, y, r in FOAM]
    kk = 0.45 * np.mean([r for _, _, r in F]) * N
    sd, bub = None, None
    for cx, cy, r in F:                                    # the same smooth-min union as draw_foam
        d = np.hypot(XX - cx * N, YY - cy * N)
        dd = d - r * 0.92 * N
        bub = d - r * N if bub is None else np.minimum(bub, d - r * N)
        if sd is None:
            sd = dd
            continue
        hh = np.clip(0.5 + 0.5 * (dd - sd) / kk, 0, 1)
        sd = dd * (1 - hh) + sd * hh - kk * hh * (1 - hh)
    mn = np.minimum(sd, bub)                               # + outside the foam body
    b.keyline(-mn, KEY)
    inside = np.clip(0.5 - mn, 0, 1)
    y0 = min(cy - r for _, cy, r in F) * N
    y1 = max(cy + r for _, cy, r in F) * N
    kv = L.smoothstep(y0, y1, YY)[..., None]
    b.flat(mixc(C["suds_top"], FOAM_RIM, 0.30) * (1 - kv) + mixc(C["suds_bot"], FOAM_CREST, 0.30) * kv,
           np.clip(0.5 - sd, 0, 1))
    big = sorted(F, key=lambda t: -t[2])[:2]
    for cx, cy, r in sorted(F, key=lambda t: t[1] - 0.2 * t[2]):
        d = np.hypot(XX - cx * N, YY - cy * N)
        b.flat(C["foam_line"], np.clip(r * N + KEY / 3 - d + 0.5, 0, 1) * inside)
        nx, ny = (XX - cx * N) / (r * N), (YY - cy * N) / (r * N)
        nr = np.clip(np.hypot(nx, ny), 0, 1)
        nz = np.sqrt(1 - nr * nr)
        li = np.clip(nx * L.LIGHT[0] + ny * L.LIGHT[1] + nz * L.LIGHT[2], 0, 1)[..., None]
        col = L.mix(FOAM_CORE, FOAM_BODY, L.smoothstep(0.0, 0.80, nr))    # a bubble, not a ball: saturated
        col = col * (0.88 + 0.16 * li)                                      # middle, light-pink fresnel rim
        dirk = L.smoothstep(0.35, 0.85, (nx * 0.6 + ny * 0.8) / (nr + 1e-4))
        rim = L.smoothstep(0.76, 0.98, nr)
        col = L.mix(col, FOAM_RIM, rim * (1 - dirk) * 0.85)
        col = L.mix(col, FOAM_EDGE, rim * dirk * 0.7)
        col = L.mix(col, FOAM_SHADOW, dirk * L.smoothstep(0.88, 1.0, nr) * 0.35)
        bandr = L.smoothstep(0.60, 0.72, nr) * (1 - L.smoothstep(0.80, 0.90, nr))
        col = L.mix(col, FOAM_RIM, dirk * bandr * 0.50)          # inner reflection, lower right
        if (cx, cy, r) in big:
            sx, sy = nx + 0.34, ny + 0.38
            u, v = (sx - sy) * 0.7071, (sx + sy) * 0.7071
            win = 1 - L.smoothstep(0.72, 1.0, np.hypot(u / 0.30, v / 0.15))
            dot = 1 - L.smoothstep(0.6, 1.0, np.hypot(nx + 0.66, ny + 0.02) / 0.075)
            col = L.mix(col, WHITE, np.clip(win * 0.95 + dot * 0.9, 0, 1))
        b.flat(np.clip(col, 0, 1), np.clip(r * N - d + 0.5, 0, 1))


def em_express(b):
    """A stopwatch: cream face, indigo ticks, teal hand over a teal elapsed wedge, gold crown and pusher."""
    cx, cy, R = 0.5, 0.550, 0.230
    b.pillow(rrect(cx - 0.030, 0.255, cx + 0.030, cy - R + 0.03, 0.006)
             | rrect(cx - 0.080, 0.196, cx + 0.080, 0.258, 0.024), GOLD, radius=0.03)
    b.pillow(poly(rot(rrect_pts(-0.030, -(R + 0.050), 0.030, -(R - 0.03), 0.010), cx, cy, 45.0)), GOLD, radius=0.03)
    sd, shade = b.pillow(disc(cx, cy, R), CREAM, radius=0.05, key=1.2 * KEY)
    ha = -90.0 + 58.0                              # the hand, 58 degrees past twelve
    ang = np.degrees(np.arctan2(YY - cy * N, XX - cx * N))
    wedge = (np.hypot(XX - cx * N, YY - cy * N) <= 0.162 * N) & (ang >= -90.0) & (ang <= ha)
    b.flat(lit(TEAL[2], shade, 0.2), soft(wedge) * 0.38)
    ticks = np.zeros((N, N), bool)
    for k in range(12):
        a = np.radians(-90.0 + 30.0 * k)
        r0, w = (0.146, 0.0135) if k % 3 == 0 else (0.170, 0.0090)
        ticks |= capsule(cx + r0 * np.cos(a), cy + r0 * np.sin(a), cx + 0.196 * np.cos(a), cy + 0.196 * np.sin(a), w)
    b.flat(lit(INDIGO, shade, 0.15), soft(ticks))
    a = np.radians(ha)
    b.pillow(capsule(cx - 0.03 * np.cos(a), cy - 0.03 * np.sin(a), cx + 0.170 * np.cos(a), cy + 0.170 * np.sin(a), 0.019),
             TEAL, radius=0.02, key=0.5 * KEY, shadow=False)
    sd, shade = b.pillow(disc(cx, cy, 0.034), TEAL, radius=0.02, key=0.5 * KEY, shadow=False)
    b.flat(lit(CREAM[1], shade, 0.2), soft(disc(cx - 0.004, cy - 0.004, 0.011)))


def em_regulars(b):
    """A tall stack of three folded rugs from the side: flat slabs with a crisp square fold on the right (an indigo
    crease, the turned-over end in shade) and the darker underside as a lip along the bottom that steps back at the
    fold; the loose end on the left, its tassels drooping off the edge (the lowest over the rug below); the FIRST_CARPET layout (border band
    + zigzag, stepped medallion) on every face. Portrait, so its locked silhouette is not the rug's or the toolbox's."""
    w, h, fw, lip, step = 0.350, 0.178, 0.030, 0.024, 0.012
    for cy, dx, pal, med in ((0.676, 0.034, BRUSH, GOLD), (0.498, 0.004, GOLD, TEAL), (0.320, 0.038, TEAL, GOLD)):
        scx = 0.5 + dx
        x0, x1, y0, y1 = scx - w / 2, scx + w / 2, cy - h / 2, cy + h / 2
        yu = y1 - lip                                      # the underside lip starts here
        slab = poly([(x0, y0), (x1, y0), (x1, yu), (x1 - step, yu), (x1 - step, y1), (x0, y1)])
        sd, shade = b.pillow(slab, pal, radius=0.024)
        lx, ly = XX / N - scx, YY / N - cy
        inside = sd > 0
        face = inside & (XX / N < x1 - fw) & (YY / N < yu)
        rug_face(b, lx, ly, (-w / 2, w / 2 - fw, -h / 2, h / 2 - lip), face, shade, pal, med, e=(0.012, 0.040),
                 period=0.030, med_at=(-fw / 2, -lip / 2), med_size=(0.068, 0.044))
        b.flat(lit(mixc(pal[0], pal[1], 0.40), shade), soft(inside & (XX / N >= x1 - fw) & (YY / N < yu)))
        b.flat(lit(pal[0] * 0.70, shade, 0.15), soft(inside & (YY / N >= yu)))
        b.flat(INDIGO, soft(inside & (np.abs(XX / N - (x1 - fw)) <= 0.0040) & (YY / N < yu)) * 0.9)
        b.flat(INDIGO, soft(inside & (np.abs(YY / N - yu) <= 0.0032)) * 0.8)
        items = []
        for fy, ln, bend, r0 in ((0.24, 0.050, 0.016, 0.0094), (0.54, 0.058, 0.026, 0.0100),
                                 (0.84, 0.048, 0.030, 0.0092)):
            items += tassel((x0 + 0.008, y0 + fy * h), -1, ln, bend, r0, 0.0, 0.0, 0.0, knot=0.20, curl=0.40,
                            taper=0.15)
        b.pillow(discs(items), CREAM, radius=0.010, key=0.50 * KEY)


def em_full_kit(b):
    """A toolbox: amber body under a clearly darker lid band (an indigo seam keyline between them), two small steel
    latches, a teal bar handle on posts, and two of the game's tools rising behind it (the scraper's steel blade,
    the brush's grey top with blue bristles): the tools make the silhouette taller than a suitcase."""
    k, ox, oy = 0.93, 0.5, 0.488                    # drawn at 1.0, scaled about (ox, oy) for the set's balance

    def P(x, y):
        return ox + (x - ox) * k, oy + (y - oy) * k

    def box(x0, y0, x1, y1, r):
        (a0, b0), (a1, b1) = P(x0, y0), P(x1, y1)
        return rrect(a0, b0, a1, b1, r * k)

    def cap(ax, ay, bx, by, r):
        return capsule(*P(ax, ay), *P(bx, by), r * k)

    def part(pts, cx, cy, deg):
        return poly(rot([(x * k, y * k) for x, y in pts], *P(cx, cy), deg))

    # the scraper, blade up, leaning left
    b.pillow(cap(0.418, 0.47, 0.352, 0.236, 0.017), TEAL, radius=0.014, key=0.6 * KEY)
    b.pillow(part([(-0.036, 0.024), (0.036, 0.024), (0.064, -0.026), (-0.064, -0.026)], 0.338, 0.200, -15.0)
             | part(rrect_pts(-0.020, 0.018, 0.020, 0.046, 0.006), 0.338, 0.200, -15.0), STEEL, radius=0.016,
             key=0.75 * KEY)
    # the brush, head up, leaning right
    b.pillow(cap(0.585, 0.47, 0.645, 0.262, 0.016), BACKING, radius=0.014, key=0.6 * KEY)
    hx_, hy_, ha = 0.664, 0.222, 14.0
    lx, ly = local(*P(hx_, hy_), ha)
    lx, ly = lx / k, ly / k
    teeth = (ly < -0.008) & (ly >= -0.056) & (np.abs(lx) <= 0.054) & ((tri((lx + 0.054) / 0.0216) > 0.28) | (ly > -0.040))
    b.pillow(teeth, BRUSH, radius=0.010, key=0.65 * KEY)
    b.pillow(part(rrect_pts(-0.062, -0.012, 0.062, 0.030, 0.012), hx_, hy_, ha), STEEL, radius=0.016, key=0.75 * KEY)
    # carry handle, body, lid band, latches
    handle = box(0.318, 0.328, 0.682, 0.378, 0.025) | box(0.338, 0.336, 0.386, 0.460, 0.012)         | box(0.614, 0.336, 0.662, 0.460, 0.012)
    b.pillow(handle, TEAL, radius=0.022)
    b.pillow(box(0.232, 0.500, 0.768, 0.792, 0.036), AMBER, radius=0.055)
    b.pillow(box(0.212, 0.432, 0.788, 0.518, 0.026), LID, radius=0.030, key=0.85 * KEY)
    for x in (0.300, 0.700):
        b.pillow(box(x - 0.022, 0.494, x + 0.022, 0.552, 0.010), STEEL, radius=0.012, key=0.5 * KEY)


BOLT = [(0.18, 0.95), (-0.52, -0.06), (-0.06, -0.06), (-0.22, -0.95), (0.52, 0.12), (0.06, 0.12)]   # HUD icon_bolt
BOLT_PAL = (mixc(AMBER[0], AMBER[1], 0.45), AMBER[1], hx("#fbd27a"))


def em_maxed_out(b):
    """The HUD POWER glyph: an amber lightning bolt, strokes 17 % wider than the glyph, shaded as one piece (linear
    top-left gradient + lit rim; a pillow creases a shape this narrow along its middle)."""
    s, wx = 0.322, 1.17
    b.gradient(poly([(0.5 + x * s * wx, 0.5 - y * s) for x, y in BOLT]), BOLT_PAL, rim=0.013)


def em_inspector(b):
    """An eye: cream almond white (the HUD eye's two arcs), blue iris, dark pupil with a white glint."""
    k = 1.04
    cx, cy, s, c, R = 0.5, 0.5, 0.345 * k, 0.56, 1.06
    almond = (np.hypot(XX - cx * N, YY - (cy + c * s) * N) <= R * s * N) & \
             (np.hypot(XX - cx * N, YY - (cy - c * s) * N) <= R * s * N)
    sd, _ = b.pillow(almond, CREAM, radius=0.05, key=1.15 * KEY)
    _, shade = b.pillow(disc(cx, cy, 0.158 * k) & (sd > px(0.012)), BLUE, radius=0.05, key=0.55 * KEY)
    b.flat(lit(C["indigo_lo"], shade, 0.2), soft(disc(cx, cy, 0.068 * k)))
    b.flat(WHITE, soft(disc(cx - 0.036 * k, cy - 0.038 * k, 0.027 * k)))
    b.flat(WHITE, soft(disc(cx + 0.040 * k, cy + 0.042 * k, 0.011 * k)) * 0.8)


EMBLEMS = {
    "ACH_FIRST_CARPET": em_first_carpet,
    "ACH_SPOTLESS": em_spotless,
    "ACH_BONE_DRY": em_bone_dry,
    "ACH_COMBO": em_combo,
    "ACH_FILTHY": em_filthy,
    "ACH_EXPRESS": em_express,
    "ACH_REGULARS": em_regulars,
    "ACH_FULL_KIT": em_full_kit,
    "ACH_MAXED_OUT": em_maxed_out,
    "ACH_INSPECTOR": em_inspector,
}


# ---- review sheet -------------------------------------------------------------
STEAM_BG = (27, 40, 56)       # #1b2838
LABEL = (199, 213, 224)


def sheet(rows, path):
    """Every icon at 256 / 64 / 32 px, locked beside unlocked, the API name under each pair; 2 columns. Below, a
    strip of all ten at 32 px (locked row, then achieved row) to compare the silhouettes side by side."""
    gap, pad, cols = 8, 24, 2
    cw, ch = 256 * 2 + gap + 16 + 64 * 2 + gap, 256 + 26
    nrow = (len(rows) + cols - 1) // cols
    W = pad * 2 + cols * cw + (cols - 1) * 40
    strip_y = pad + nrow * ch + (nrow - 1) * 18 + 16
    H = strip_y + 2 * 32 + gap + pad
    im = Image.new("RGB", (W, H), STEAM_BG)
    d = ImageDraw.Draw(im)
    ft = L.font(15, {"Weight": 600, "Width": 100})
    for i, (api, u, lk) in enumerate(rows):
        x = pad + (i % cols) * (cw + 40)
        y = pad + (i // cols) * (ch + 18)
        im.paste(u, (x, y))
        im.paste(lk, (x + 256 + gap, y))
        xs = x + 2 * 256 + gap + 16
        for j, sz in enumerate((64, 32)):
            yy = y + j * (64 + gap)
            im.paste(u.resize((sz, sz), Image.LANCZOS), (xs, yy))
            im.paste(lk.resize((sz, sz), Image.LANCZOS), (xs + sz + gap, yy))
        tw = d.textlength(api, font=ft)
        d.text((x + (2 * 256 + gap - tw) / 2, y + 256 + 5), api, font=ft, fill=LABEL)
    for i, (api, u, lk) in enumerate(rows):
        im.paste(lk.resize((32, 32), Image.LANCZOS), (pad + i * (32 + gap), strip_y))
        im.paste(u.resize((32, 32), Image.LANCZOS), (pad + i * (32 + gap), strip_y + 32 + gap))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    im.save(path)


def extent(b):
    """Emblem at 256 px -> (clearance of its keyline to the stitch line in px, largest fill side / tile side,
    covered area / tile area)."""
    def box(m):
        ys, xs = np.nonzero(m)
        return xs.min() / SS, ys.min() / SS, (xs.max() + 1) / SS, (ys.max() + 1) / SS
    cov = np.maximum(np.maximum(b.sil, b.fill), b.sec)
    x0, y0, x1, y1 = box(cov > 0.5)
    clear = min(x0, y0, SIDE - x1, SIDE - y1) - stitch_inner() * SIDE
    f0, g0, f1, g1 = box(b.fill > 0.5)
    return clear, max(f1 - f0, g1 - g0) / SIDE, float(cov.mean())


def main():
    t0 = time.time()
    with open(SRC, encoding="utf-8") as f:
        achs = json.load(f)["achievements"]
    missing = [a["api"] for a in achs if a["api"] not in EMBLEMS]
    if missing:
        raise SystemExit("no emblem for " + ", ".join(missing) + " (add an em_* function to EMBLEMS)")
    os.makedirs(OUT, exist_ok=True)
    rug = rug_tile()
    rows = []
    for a in achs:
        api = a["api"]
        b = Badge()
        EMBLEMS[api](b)
        rgb = compose(rug, b)
        u, lk = to_rgb8(reduce3(rgb)), to_rgb8(reduce3(locked(rgb, b)))
        u.save(os.path.join(OUT, api + ".png"))
        lk.save(os.path.join(OUT, api + "_locked.png"))
        rows.append((api, u, lk))
        clear, frac, area = extent(b)
        print(f"{api:18s} emblem {frac * 100:4.1f} % of the side, {area * 100:4.1f} % of the area, keyline "
              f"{clear:5.1f} px clear of the stitch line" + ("" if clear >= 10 else "  <-- TOO CLOSE"))
    sheet(rows, SHEET)
    print(f"{len(rows) * 2} icons -> {os.path.relpath(OUT, ROOT)}, sheet -> {os.path.relpath(SHEET, ROOT)}"
          f"  ({time.time() - t0:.1f} s)")


if __name__ == "__main__":
    main()
