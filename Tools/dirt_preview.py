"""Offline preview of a new carpet's dirt (Docs/DIRT_LEVEL.md) - no Unity, no Play mode.

Ports, in float32 numpy:
  - CarpetSimulation.RollDirt (the per-carpet roll from the seed: level, spread, dust, mud, crust, spill, offset),
  - ResetCarpet's gameplay coverage floor (at least 50% visible dust / 20% crust),
  - SimCommon.hlsl Hash1/Hash2/ValueNoise/Fbm/Voronoi and CarpetSim.compute SoilNoise/SoilMask/CrustPatch/K_Init layers,
  - CarpetFibers.shader's albedo compositing (stain -> mud -> crumbs, crust plate, dust film), averaged over the strand
    tips the camera sees and over strands. No lighting, no shells: judge colours and the spread of states, not shading.
Keep it in step with those three files when they change (the SimSettings values sit in TUNE below).

Usage:  python Tools/dirt_preview.py <out.png> <rug.jpg> [seed ...]
        seed -1 renders the fully soiled carpet (CarpetDebugApi.Reset's level 1) as a reference.
Prints each carpet's roll and its layer means (the same means StatsJson reports) and saves a contact sheet.
"""
import sys
import numpy as np
from PIL import Image

f32 = np.float32
W, H = 512, 640                               # CarpetSimulation.SimW / SimH; carpet 3600 x 4500 mm
TUNE = dict(MASK_MM=500.0, WARP=1.5, GRAIN=0.3, GRAIN_MM=50.0, CRUST_MM=220.0, DUST=0.35,   # SimSettings "Dirt of a new carpet"
            VEIL=0.45)                                                                          # CarpetFibers _DustVeil


def frac(x): return x - np.floor(x)
def sat(x): return np.clip(x, 0.0, 1.0)
def lerp(a, b, t): return a + (b - a) * t
def smoothstep(a, b, x):
    t = sat((x - a) / (b - a)); return t * t * (3.0 - 2.0 * t)


# ------------------------------------------------------------------ SimCommon.hlsl noise
def hash1(px, py):
    px = frac(px * f32(0.1031)); py = frac(py * f32(0.1030))
    d = px * (py + f32(33.33)) + py * (px + f32(33.33))
    px = px + d; py = py + d
    return frac((px + py) * px)

def hash2(px, py):
    x = frac(px * f32(0.1031)); y = frac(py * f32(0.1030)); z = frac(px * f32(0.0973))
    d = x * (y + f32(33.33)) + y * (z + f32(33.33)) + z * (x + f32(33.33))
    x = x + d; y = y + d; z = z + d
    return frac((x + y) * z), frac((x + z) * y)

def value_noise(px, py):
    ix = np.floor(px); iy = np.floor(py); fx = px - ix; fy = py - iy
    ux = fx * fx * (3 - 2 * fx); uy = fy * fy * (3 - 2 * fy)
    a = hash1(ix, iy); b = hash1(ix + 1, iy); c = hash1(ix, iy + 1); d = hash1(ix + 1, iy + 1)
    return lerp(lerp(a, b, ux), lerp(c, d, ux), uy)

def fbm(px, py):
    s = 0.0; amp = 0.5; norm = 0.0
    for _ in range(4):
        s = s + amp * value_noise(px, py); norm += amp
        px = px * f32(2.03) + f32(17.1); py = py * f32(2.03) + f32(9.7); amp *= 0.5
    return (s / norm).astype(f32)

def voronoi(px, py):
    ix = np.floor(px); iy = np.floor(py); fx = px - ix; fy = py - iy
    f1 = np.full(px.shape, 8.0, f32); f2 = np.full(px.shape, 8.0, f32); ch = np.zeros(px.shape, f32)
    for gy in (-1, 0, 1):
        for gx in (-1, 0, 1):
            ox, oy = hash2(ix + gx, iy + gy)
            rx = gx + ox - fx; ry = gy + oy - fy; d = rx * rx + ry * ry
            h = hash1(ix + gx + 0.5, iy + gy + 0.5)
            closer = d < f1
            f2 = np.where(closer, f1, np.where(d < f2, d, f2))
            ch = np.where(closer, h, ch); f1 = np.where(closer, d, f1)
    return np.sqrt(f1), np.sqrt(f2), ch


# ------------------------------------------------------------------ CarpetSimulation.RollDirt
def h01(seed, salt):
    x = ((seed * 0x9E3779B9) & 0xFFFFFFFF) ^ salt
    x ^= x >> 16; x = (x * 0x7FEB352D) & 0xFFFFFFFF; x ^= x >> 15; x = (x * 0x846CA68B) & 0xFFFFFFFF; x ^= x >> 16
    return (x >> 8) / 16777216.0

def ss(a, b, x):
    t = min(max((x - a) / (b - a), 0.0), 1.0); return t * t * (3 - 2 * t)

FULLY_SOILED = dict(level=1.0, spread=0.5, dust=0.0, mud=1.0, crust=1.0, spill=1.0, offset=(0.0, 0.0))

def roll_dirt(seed, forced_level=-1.0):
    if forced_level >= 1.0: return dict(FULLY_SOILED)
    p = dict(level=forced_level if forced_level >= 0.0 else h01(seed, 0x68E31DA4),
             spread=lerp(0.45, 1.3, h01(seed, 0x2C1B3C6D)),
             dust=lerp(0.3, 1.0, h01(seed, 0x297A2D39)),
             mud=0.2 + 0.8 * h01(seed, 0x6A09E667) ** 0.5,
             offset=(h01(seed, 0xB5297A4D) * 211.0, h01(seed, 0x1B56C4E9) * 211.0))
    crusty = h01(seed, 0xBB67AE85) < 0.4
    p['crust'] = lerp(0.25, 0.9, h01(seed, 0x3C6EF372)) * ss(0.25, 0.75, p['level']) if crusty else 0.0
    p['spill'] = p['level'] * lerp(0.15, 1.0, h01(seed, 0xA54FF53A))
    if forced_level < 0.0:
        p['dust'] = max(0.5, p['dust'])
        p['crust'] = max(0.2, p['crust'])
    return p


# ------------------------------------------------------------------ CarpetSim.compute K_Init
def soil_noise(pxm, pym, cell, off):
    qx = pxm / f32(cell) + f32(off[0]); qy = pym / f32(cell) + f32(off[1])
    wx = fbm(qx * f32(0.7) + f32(5.2), qy * f32(0.7) + f32(1.3)) - f32(0.5)
    wy = fbm(qx * f32(0.7) + f32(9.7), qy * f32(0.7) + f32(19.1)) - f32(0.5)
    return smoothstep(0.25, 0.75, fbm(qx + f32(TUNE['WARP']) * wx, qy + f32(TUNE['WARP']) * wy))

def soil_mask(pxm, pym, p):
    level = p['level']
    if level <= 0: return np.zeros_like(pxm)
    if level >= 1: return np.ones_like(pxm)
    spread = max(p['spread'], 1e-3); a = 1.0 - level * (1.0 + spread)
    S = smoothstep(a, a + spread, soil_noise(pxm, pym, TUNE['MASK_MM'], p['offset']))
    g = fbm(pxm / f32(TUNE['GRAIN_MM']) + f32(p['offset'][1] + 91.7), pym / f32(TUNE['GRAIN_MM']) + f32(p['offset'][0] + 13.3))
    return sat(S * (1.0 + TUNE['GRAIN'] * (g * 2.0 - 1.0)))

def crust_patch(pxm, pym, p):
    k = p['crust']
    if k <= 0: return np.zeros_like(pxm)
    if k >= 1: return np.ones_like(pxm)
    a = 1.0 - k * 1.25
    return smoothstep(a, a + 0.25, soil_noise(pxm, pym, TUNE['CRUST_MM'], (p['offset'][0] + 57.3, p['offset'][1] + 23.9)))

def k_init(seed, p):
    ys, xs = np.mgrid[0:H, 0:W].astype(f32)
    u = (xs + 0.5) / W; v = (ys + 0.5) / H
    sd = f32(seed * 0.6180339 + 7.31); s2x, s2y = sd, f32(sd * 1.37)
    pxm = u * f32(3600.0); pym = v * f32(4500.0)
    # dried-mud slabs (K_Init 2026-09-26): warped ~40 mm slabs + thin partial crazing along the ~16 mm cells
    wx = fbm(u * 9.0 + s2x * 0.37 + 2.1, v * 9.0 + s2y * 0.37 + 2.1) - 0.5
    wy = fbm(u * 9.0 + s2x * 0.53 + 17.9, v * 9.0 + s2y * 0.53 + 17.9) - 0.5
    l1, l2, lh = voronoi(u * 30.0 + wx * 0.9 + s2x * 0.71 + 5.3, v * 37.5 + wy * 0.9 + s2y * 0.71 + 5.3)
    f1, f2, sh = voronoi(u * 75.0 + wx * 1.4 + s2x, v * 94.0 + wy * 1.4 + s2y)
    main_mm = (l2 - l1) * 40.0 * (0.72 + 0.5 * fbm(u * 17.0 + s2x * 0.21 + 41.0, v * 17.0 + s2y * 0.21 + 41.0))
    craze = (hash1(lh * 91.3, np.full_like(lh, 7.7)) >= 0.4).astype(f32)
    sub_mm = (f2 - f1) * 16.0 + 1.1 + 3.0 * sat(fbm(u * 23.0 + s2x * 1.3, v * 23.0 + s2y * 1.3) * 2.0 - 0.7) + 40.0 * (1.0 - craze)
    e = sat(np.minimum(main_mm, sub_mm) / 8.0)
    plate = sat(0.82 * lh + 0.18 * sh)
    hue = hash1(np.floor(u * 4.0) + s2x, np.floor(v * 5.0) + s2y)
    S = soil_mask(pxm, pym, p)
    grit = p['dust'] * TUNE['DUST'] * smoothstep(0.0, 0.35, S) * lerp(1.0, 0.4, smoothstep(0.55, 1.0, S))
    grit = grit * smoothstep(0.04, 0.05, grit)
    mud = (0.5 + 0.5 * sat(fbm(u * 11.0 + 3.7 + s2x, v * 11.0 + 3.7 + s2y))) * p['mud'] * S * S * (0.8 + 0.2 * S)
    mud = mud * smoothstep(0.02, 0.03, mud)
    gc = smoothstep(0.45, 0.90, crust_patch(pxm, pym, p) + 0.15 * (plate - 0.5)) * smoothstep(0.35, 0.75, S)
    crust = (0.75 + 0.25 * sat(fbm(u * 6.0 + s2x, v * 6.0 + s2y))) * gc
    stain = 0.8 * smoothstep(0.55, 0.75, sat(voronoi(u * 4.0 + s2x, v * 4.0 + s2y)[0])) * smoothstep(0.3, 0.8, S)
    for k in range(12):
        if not hash1(f32(k * 1.7 + 0.3), f32(sd * 0.37 + 11.1)) < p['spill']: continue
        hx, hy = hash2(f32(k * 3.1 + 1.0), f32(sd + k * 0.77))
        cx = (0.05 + 0.9 * hx) * 3600.0; cy = (0.05 + 0.9 * hy) * 4500.0
        r = lerp(30.0, 80.0, hash1(f32(sd + k * 2.3), f32(k * 5.1 + 0.5)))
        stain = stain + 0.6 * smoothstep(r, r * 0.6, np.sqrt((pxm - cx) ** 2 + (pym - cy) ** 2))
    return dict(S=S, crust=crust, mud=mud, stain=sat(stain), grit=grit, plate=plate, e=e, hue=hue)


def new_carpet(seed, forced_level=-1.0):
    """ResetCarpet + EnsureStartingDirtCoverage. Explicit levels preserve the historical/debug look."""
    rolled = roll_dirt(seed, forced_level)
    p = dict(rolled)
    d = k_init(seed, p)
    if forced_level < 0.0:
        for i in range(1, 9):
            if (d['grit'] > 0.04).mean() >= 0.5 and (d['crust'] > 0.1).mean() >= 0.2:
                break
            t = i / 8.0
            p = dict(rolled, level=1.0 if i == 8 else lerp(rolled['level'], 1.0, t),
                     crust=1.0 if i == 8 else lerp(rolled['crust'], 1.0, t))
            d = k_init(seed, p)
    return p, d


# ------------------------------------------------------------------ CarpetFibers.shader albedo (linear space)
def srgb2lin(c): c = np.asarray(c, f32); return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
def lin2srgb(c): c = np.clip(c, 0, 1); return np.where(c <= 0.0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - 0.055)
MUD = srgb2lin([0.24, 0.15, 0.08]); CRUST = srgb2lin([0.50, 0.45, 0.38]); GRIT = srgb2lin([0.55, 0.48, 0.40])
DUST = srgb2lin([0.60, 0.56, 0.50])
STAIN = [srgb2lin(c) for c in ([0.55, 0.38, 0.20], [0.50, 0.18, 0.25], [0.35, 0.35, 0.33], [0.40, 0.50, 0.22])]

def albedo(d, pic):
    hue = d['hue'][..., None]
    tint = np.where(hue < 0.25, STAIN[0], np.where(hue < 0.5, STAIN[1], np.where(hue < 0.75, STAIN[2], STAIN[3])))
    out = np.zeros_like(pic)
    ts = (0.6, 0.75, 0.9)                                       # the part of the strands the 55 deg camera sees
    for t in ts:
        b = lerp(pic, pic * tint, (1 - np.exp(-4 * d['stain']))[..., None])
        b = lerp(b, MUD, (1 - np.exp(-d['mud'] * lerp(8.0, 3.0, t)))[..., None])
        b = lerp(b, GRIT, (0.45 * sat((d['grit'] - 0.2) * 2.5) * smoothstep(0.4, 0.8, t))[..., None])   # crumbs on 45 % of strands
        out += b / len(ts)
    cc = (1 - np.exp(-3.5 * d['crust']))[..., None]
    plate = lerp(CRUST * (0.76 + 0.48 * d['plate'][..., None]), MUD * 0.45, (1 - smoothstep(0.26, 0.34, d['e']))[..., None])
    out = lerp(out, plate, cc)
    dust = TUNE['VEIL'] * (1 - np.exp(-3.5 * d['grit'])) * smoothstep(0.1, 0.8, 0.75)            # strand mean of (0.55 + 0.9 hs.y) = 1
    return lerp(out, DUST, dust[..., None])

def load_pic(path):
    a = np.asarray(Image.open(path).convert('RGB').resize((W, H), Image.BILINEAR), f32) / 255.0
    return srgb2lin(a[::-1])                                    # carpet v = 0 is the texture's bottom row

def render(seed, pic, p):
    d = k_init(seed, p)
    return (lin2srgb(albedo(d, pic) * 1.35)[::-1] * 255).astype(np.uint8), d


if __name__ == '__main__':
    if len(sys.argv) < 3: sys.exit(__doc__)
    out, rug = sys.argv[1], sys.argv[2]
    seeds = [int(s) for s in sys.argv[3:]] or [1, 2, 3, 4]
    pic = load_pic(rug)
    tiles = []
    for sd in seeds:
        p, d = new_carpet(abs(sd), 1.0 if sd < 0 else -1.0)
        img = (lin2srgb(albedo(d, pic) * 1.35)[::-1] * 255).astype(np.uint8)
        score = d['crust'].mean() + 0.8 * d['mud'].mean() + 0.6 * d['stain'].mean() + 0.9 * d['grit'].mean()   # CarpetSimulation.DirtScore
        print(f"seed {sd}: level {p['level']:.2f} spread {p['spread']:.2f} dust {p['dust']:.2f} mud {p['mud']:.2f} "
              f"crust {p['crust']:.2f} spill {p['spill']:.2f} | crust {d['crust'].mean():.3f} mud {d['mud'].mean():.3f} "
              f"stain {d['stain'].mean():.3f} grit {d['grit'].mean():.3f} DirtScore {score:.4f} "
              f"dust area {(d['grit'] > 0.04).mean():.1%} crust area {(d['crust'] > 0.1).mean():.1%}")
        tiles.append(img)
    cols = min(4, len(tiles)); rows = (len(tiles) + cols - 1) // cols
    sheet = np.full((rows * (H + 8), cols * (W + 8), 3), 30, np.uint8)
    for i, t in enumerate(tiles):
        r, c = divmod(i, cols); sheet[r * (H + 8):r * (H + 8) + H, c * (W + 8):c * (W + 8) + W] = t
    Image.fromarray(sheet).save(out)
    print('saved', out)
