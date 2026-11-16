"""Writes the TikTok take / staging evals (Docs/TIKTOK.md, order in README.md). Run: python _mk.py (idempotent).
A take = look_on.inc (48 shells, raking sun, DoF volume) + SeqBegin portrait 2160x3840 @60 fps with the audio log +
cam.inc with this take's camera keys + the StoreGifApi queue. Run it with ../../trailer/recipes/common/take.sh, the
staging (p*.cs) with ../../trailer/recipes/common/evp.sh.
World: carpet centred at the origin, x = (u-0.5)*1.2, z = (v-0.5)*1.5 (v = 0 is the game camera's near edge), pile top y ~0.02."""
import os

D = os.path.dirname(os.path.abspath(__file__))
LOOK = open(os.path.join(D, 'look_on.inc'), encoding='utf-8').read()
CAM = open(os.path.join(D, 'cam.inc'), encoding='utf-8').read()
SB = 'var S = CarpetCleaning.StoreGifApi.SeqBegin("%s", 2160, 3840, 0, 0, 0, 0, false, 1, true, 2560, 1440, 60, true);'
G = 'CarpetCleaning.StoreGifApi.'
API = 'CarpetCleaning.CarpetDebugApi.'
PROBE = 'return ' + API + 'SurfaceProbeJson(0.25f, 0.32f, 0.75f, 0.40f) + " " + ' + API + 'StatsJson();\n'


def W(u, v, y=0.0):
    return ((u - 0.5) * 1.2, y, (v - 0.5) * 1.5)


def key(t, pos, tgt, fov, focus=0.0):
    return [t, *pos, *tgt, fov, focus]


def fmt_keys(keys):
    return 'new float[][]{ ' + ', '.join('new[]{' + ', '.join('%.4ff' % x for x in k) + '}' for k in keys) + ' }'


def take(name, comment, keys, items, pre=''):
    return ('// ' + comment.replace('\n', '\n// ') + '\n' + pre + LOOK + (SB % name) + '\n' + CAM.replace('CAMKEYS', fmt_keys(keys))
            + '\n'.join(G + x + ';' for x in items) + '\nreturn S + " " + CarpetCleaning.StoreGifApi.Run(3000);\n')


def rows(tool, v0, v1, n, u0=0.04, u1=0.96, mm=15.0, p=1.0):
    """n horizontal StrokeNow rows from v0 to v1, alternating direction, ~mm per stamp."""
    out = []
    for r in range(n):
        v = v0 + (v1 - v0) * (r / max(1, n - 1))
        a, b = (u0, u1) if r % 2 == 0 else (u1, u0)
        st = max(8, round(abs(b - a) * 1200 / mm))
        out.append(API + 'StrokeNow(%d, %.3ff, %.3ff, %.3ff, %.3ff, %d, %.2ff);' % (tool, a, v, b, v, st, p))
    return '\n'.join(out) + '\n'


F = {}
B = 0.36   # the band: through the lower star's medallion and its cream triangles

F['t1_take.cs'] = take('tt1_hook', 'T1 HOOK: no tool; a forward dolly low over the dusty upper star (v 0.6-0.75, which only the reveal\nsees again), 3.6 s.', [
    key(0.0, (-0.10, 0.16, -0.06), W(0.42, 0.60), 30),
    key(3.6, (0.04, 0.10, 0.10), W(0.56, 0.71), 30),
], ['Away(3.60f)'])

F['t2_take.cs'] = take('tt2_vacuum', 'T2 VACUUM: dry vac R->L along v 0.36 (u 0.74 -> 0.26, 2.8 s); low front camera pans with the ring.', [
    key(0.0, (0.20, 0.16, -0.58), W(0.70, B), 30),
    key(1.6, (0.06, 0.15, -0.57), W(0.52, B), 30),
    key(3.6, (-0.12, 0.15, -0.56), W(0.30, B), 30),
], ['Tool(1)', 'Hover(0.77f, %.3ff, 0.35f)' % B, 'Stroke(0.74f, %.3ff, 0.26f, %.3ff, 2.80f)' % (B, B), 'Up(0.20f)', 'Away(0.30f)'])

# the vacuumed lane widened to the whole band, so the hose lands on combed, still muddy pile
F['p2_vac.cs'] = rows(1, 0.315, 0.405, 3, 0.22, 0.78) + PROBE

F['t3_take.cs'] = take('tt3_hose', 'T3 HOSE: three half-pressure spots along the band (u 0.40, 0.50, 0.60); slow push-in from the near side.', [
    key(0.0, (-0.04, 0.24, -0.62), W(0.45, B), 32),
    key(3.9, (0.06, 0.19, -0.52), W(0.55, B), 30),
], ['Tool(2)', 'Hover(0.40f, %.3ff, 0.30f)' % B, 'Move(0.40f, %.3ff, 0.40f, %.3ff, 0.70f, true, true)' % (B, B),
    'Move(0.40f, %.3ff, 0.50f, %.3ff, 0.20f, false)' % (B, B + 0.005), 'Move(0.50f, %.3ff, 0.50f, %.3ff, 0.70f, true, true)' % (B + 0.005, B + 0.005),
    'Move(0.50f, %.3ff, 0.60f, %.3ff, 0.20f, false)' % (B + 0.005, B), 'Move(0.60f, %.3ff, 0.60f, %.3ff, 0.70f, true, true)' % (B, B),
    'Hover(0.60f, %.3ff, 0.50f)' % B, 'Away(0.40f)'])

# soak the whole band, foam it, brush AREA 6 (the blue skirt stands clear of the foam, trailer #10)
F['p3_soak_foam.cs'] = (rows(2, 0.315, 0.405, 3, 0.22, 0.78, 12.0, 0.5) + API + 'RunFrames(30);\n'
                        + rows(3, 0.325, 0.395, 2, 0.24, 0.76, 6.0, 0.5)
                        + 'CarpetCleaning.EconomyDebugApi.SetLevel(4, 1, 6);\n' + PROBE)

F['t4_take.cs'] = take('tt4_brush', 'T4 SCRUB: the rotary brush (AREA 6) scrubs three legs through the foam; close camera, slight orbit.', [
    key(0.0, (0.22, 0.40, -0.66), W(0.53, B), 30),
    key(3.6, (0.02, 0.36, -0.64), W(0.49, B), 30),
], ['Tool(4)', 'Hover(0.62f, %.3ff, 0.25f)' % B, 'Stroke(0.60f, %.3ff, 0.40f, %.3ff, 0.90f)' % (B, B - 0.012),
    'Move(0.40f, %.3ff, 0.60f, %.3ff, 0.90f, true)' % (B - 0.012, B + 0.015), 'Move(0.60f, %.3ff, 0.42f, %.3ff, 0.90f, true)' % (B + 0.015, B),
    'Up(0.40f)', 'Away(0.20f)'])

F['p4_scrub.cs'] = rows(4, 0.32, 0.40, 3, 0.24, 0.76, 10.0) + API + 'RunFrames(30);\n' + PROBE

F['t5_take.cs'] = take('tt5_squeegee', 'T5 SQUEEGEE: one horizontal pass at v 0.36 off the right rim (2.3 s); the camera waits at the rim and\nturns with the slurry onto the tile.', [
    key(0.0, (0.70, 0.24, -0.66), W(0.70, B), 34),
    key(1.6, (0.74, 0.24, -0.66), W(0.86, B), 34),
    key(3.8, (0.82, 0.22, -0.62), W(1.06, B), 34),
], ['Tool(5)', 'Hover(-0.06f, %.3ff, 0.20f)' % B, 'Stroke(-0.03f, %.3ff, 1.12f, %.3ff, 2.30f)' % (B, B),
    'Move(1.12f, %.3ff, 1.42f, %.3ff, 0.40f, false)' % (B, B + 0.015), 'Away(1.00f)'])

# wet vac lifts what the blade left (damp, pressed, striped pile) and the dryer gets POWER 6 (trailer #13)
F['p5_extract.cs'] = (rows(6, 0.30, 0.42, 3, 0.20, 0.80, 6.5) + 'CarpetCleaning.EconomyDebugApi.SetLevel(7, 0, 6);\n' + PROBE)

F['t6_take.cs'] = take('tt6_dryer', 'T6 REFLUFF: the dryer L->R along the band (u 0.30 -> 0.66, 3.6 s); a low raking camera tracks the ring.', [
    key(0.0, (-0.30, 0.11, -0.47), W(0.30, B), 32),
    key(4.4, (0.10, 0.11, -0.47), W(0.66, B), 32),
], ['Tool(7)', 'Hover(0.28f, %.3ff, 0.20f)' % B, 'Move(0.28f, %.3ff, 0.66f, %.3ff, 3.60f, true)' % (B, B), 'Up(0.30f)', 'Away(0.30f)'])

# the reveal: the whole near half (v 0.02-0.50) cleaned for real, in steps that fit the 5 s eval budget
# cut 1.1 (user: dried-mud chunks on the dirty half of the reveal): s02_reset_crust.cs re-rolls the star with crust 0.3,
# then the scraper clears the near half before the wash (p6s*); the band takes are not replayed (p6 cleans over them)
F['s02_reset_crust.cs'] = ('// T7 retake: the star with crust 0.3 (scattered dried-mud slabs, the pattern still shows), same mud / dust as s01.\n'
                           + API + 'ResetWithProfile(394, 0.75f, 0.3f, 0.45f, 0.85f);\n'
                           + 'CarpetCleaning.EconomyDebugApi.LiveTuning(true);\n' + API + 'SetTool(0);\n'
                           + API + 'RunFrames(3);\nreturn ' + API + 'DirtJson() + " " + ' + API + 'StatsJson();\n')
# with the scraper's crumbs about, p6i no longer fits the 5 s eval budget: the retake runs it as p6i1 + p6i2
F['p6i1_stain.cs'] = rows(6, 0.17, 0.02, 5, mm=8.0) + rows(7, 0.02, 0.17, 5, mm=6.0) + PROBE
F['p6i2_stain.cs'] = rows(7, 0.17, 0.02, 5, mm=6.0) + API + 'RunFrames(180);\n' + PROBE
for h, (v0, v1) in enumerate(((0.02, 0.245), (0.275, 0.50))):
    F['p6s%d_scrape.cs' % (h + 1)] = rows(0, v0, v1, 9, mm=12.0) + API + 'RunFrames(60);\n' + PROBE
P6 = [('vac', 1, 15.0, 1.0), ('hose', 2, 12.0, 0.5), ('wetvac', 6, 8.0, 1.0), ('wetvac2', 6, 8.0, 1.0), ('dry', 7, 6.0, 1.0), ('dry2', 7, 6.0, 1.0)]
for i, (nm, tool, mm, p) in enumerate(P6):
    for h, (v0, v1) in enumerate(((0.02, 0.245), (0.275, 0.50))):
        F['p6%s%d_%s.cs' % ('abcdef'[i], h + 1, nm)] = rows(tool, v0, v1, 8, mm=mm, p=p) + PROBE
F['p6g_settle.cs'] = API + 'RunFrames(180);\n' + PROBE
# the near border keeps a greenish spill stain after the plain wash: foam + brush + wet vac + dryer over v 0.02-0.17
F['p6h_stain.cs'] = (rows(3, 0.02, 0.17, 5, mm=8.0, p=0.5) + rows(4, 0.02, 0.17, 5, mm=10.0) + rows(6, 0.02, 0.17, 5, mm=8.0) + PROBE)
F['p6i_stain.cs'] = (rows(6, 0.17, 0.02, 5, mm=8.0) + rows(7, 0.02, 0.17, 5, mm=6.0) + rows(7, 0.17, 0.02, 5, mm=6.0)
                     + API + 'RunFrames(180);\n' + PROBE)

F['t7_take.cs'] = take('tt7_reveal', 'T7 REVEAL: from macro on the clean / dirty edge (v 0.5) up to the whole rug from above (3.4 s, hold 1.2 s).', [
    key(0.0, (0.02, 0.14, -0.14), W(0.52, 0.53), 32),
    key(1.4, (0.0, 0.55, -0.40), W(0.50, 0.50), 36),
    key(3.4, (0.0, 2.55, -0.02), W(0.50, 0.50), 38),
    key(4.6, (0.0, 2.55, -0.02), W(0.50, 0.50), 38),
], ['Away(4.60f)'])

for n, body in F.items():
    with open(os.path.join(D, n), 'w', encoding='utf-8', newline='\n') as fh:
        fh.write(body)
print(sorted(F))
