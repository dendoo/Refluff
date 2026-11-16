"""Writes the chain's eval scripts (#06-#13 on the Anatolian star, brief: TRAILER.md 8.2 + recipes/README.md).
Run: python _mk.py  (idempotent). Files ending in .body get clip 1's lane outline (_lanes.inc) prepended."""
import os

D = os.path.dirname(os.path.abspath(__file__))
SB = 'var S = CarpetCleaning.StoreGifApi.SeqBegin("%s", 3840, 2160, 480, 360, 3360, 1800, false, 1, true, 2560, 1440, 60, true);'
G = 'CarpetCleaning.StoreGifApi.'
F = {}
F['c00_reset.cs'] = '''// Chain S0: the Anatolian star (take A left it on the stage) with Reset(394, 0.9f) = the landed rug of #04 (same K_Init,
// clean floor); hero kit set by payoff/kit.inc; LiveTuning on (Reset switches ToolTuning to Neutral).
for (int i = 0; i < 12 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_anatolian_star"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
var H0 = CarpetCleaning.CarpetDebugApi.Handover;
if (H0.State != CarpetCleaning.HandoverState.Idle) CarpetCleaning.CarpetDebugApi.HandoverSnap();
CarpetCleaning.CarpetDebugApi.Reset(394, 0.9f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.SetTool(0);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
var hj = CarpetCleaning.CarpetDebugApi.HandoverJson(); int k = hj.IndexOf("picName");
return hj.Substring(k, 36) + " " + CarpetCleaning.CarpetDebugApi.DirtJson();
'''


def take(name, comment, items):
    return (comment + '\n' + (SB % name) + '\n' + '\n'.join(G + x + ';' for x in items)
            + '\nreturn S + " " + CarpetCleaning.StoreGifApi.Run(3000);\n')


F['tr06_take.cs'] = take('tr06_scrape', '// #06 SCRAPE (0:13.3-0:16.0): 4 pressed scraper legs over the dried-mud slabs of the band centre (clip 3 pattern, '
                         '+-0.002 v\n// wobble so the heading never swings), 870 mm/s; shards hop onto the right tile. Edit window ~0.30-2.97 s.', [
    'Tool(0)', 'Hover(0.43f, 0.345f, 0.34f)', 'Stroke(0.46f, 0.345f, 0.86f, 0.345f, 0.55f)',
    'Move(0.86f, 0.345f, 0.46f, 0.347f, 0.55f, true)', 'Move(0.46f, 0.347f, 0.86f, 0.343f, 0.55f, true)',
    'Move(0.86f, 0.343f, 0.46f, 0.345f, 0.55f, true)', 'Up(0.12f)', 'Move(0.46f, 0.345f, 0.30f, 0.36f, 0.30f, false)', 'Away(0.40f)'])
F['p06b_settle.cs'] = 'CarpetCleaning.CarpetDebugApi.RunFrames(60);\nreturn CarpetCleaning.CarpetDebugApi.StatsJson();\n'
F['p07a_dv.body'] = '''int i = 0;
foreach (float v in new[]{0.28f, 0.32f}) { if (!Cover(v, 0.035f, out float ul, out float ur)) continue; Lane(1, new[]{ul - 0.01f, v, ur + 0.01f, v}, (i++ & 1) == 1, 15.4f, 1f); }
return CarpetCleaning.CarpetDebugApi.StatsJson() + " " + CarpetCleaning.CarpetDebugApi.DebrisAliveJson();
'''
F['tr07_take.cs'] = take('tr07_vacuum', '// #07 VACUUM (0:16.0-0:18.7): one dry-vac row at the band centre R->L between the vacuumed lower third and the grey,\n'
                         '// crumb-strewn upper third; crumbs vanish at the ring (GPU capture -> tube rattle). Ring only. 332 mm/s.', [
    'Tool(1)', 'Hover(0.93f, 0.36f, 0.30f)', 'Stroke(0.905f, 0.36f, 0.13f, 0.36f, 2.80f)', 'Up(0.15f)', 'Away(0.35f)'])
F['p07b_dv.body'] = '''int i = 1;
foreach (float v in new[]{0.40f, 0.44f}) { if (!Cover(v, 0.035f, out float ul, out float ur)) continue; Lane(1, new[]{ul - 0.01f, v, ur + 0.01f, v}, (i++ & 1) == 1, 15.4f, 1f); }
return CarpetCleaning.CarpetDebugApi.StatsJson();
'''
F['tr08_take.cs'] = take('tr08_soak', '// #08 SOAK (0:18.7-0:21.3): three half-pressure hose spots along the band centre spread and join, the dust darkens\n'
                         '// to mud, beads fly round the nozzle (right button = half pressure).', [
    'Tool(2)', 'Hover(0.36f, 0.36f, 0.30f)', 'Move(0.36f, 0.36f, 0.36f, 0.36f, 0.55f, true, true)',
    'Move(0.36f, 0.36f, 0.50f, 0.365f, 0.20f, false)', 'Move(0.50f, 0.365f, 0.50f, 0.365f, 0.55f, true, true)',
    'Move(0.50f, 0.365f, 0.64f, 0.36f, 0.20f, false)', 'Move(0.64f, 0.36f, 0.64f, 0.36f, 0.55f, true, true)',
    'Hover(0.64f, 0.36f, 0.40f)', 'Away(0.35f)'])
F['p08_settle.cs'] = 'CarpetCleaning.CarpetDebugApi.RunFrames(60);\nreturn CarpetCleaning.CarpetDebugApi.SurfaceProbeJson(0.12f, 0.28f, 0.89f, 0.45f);\n'
F['tr09_take.cs'] = take('tr09_foam', '// #09 FOAM (0:21.3-0:24.0): one half-pressure foam lane L->R along the band centre, 330 mm/s (1.05x the dose clip 3\n'
                         '// validated). Ring only.', [
    'Tool(3)', 'Hover(0.10f, 0.365f, 0.25f)', 'Stroke(0.13f, 0.365f, 0.87f, 0.365f, 2.70f, 40f, true)', 'Up(0.20f)', 'Away(0.40f)'])
F['p09_foam.cs'] = '''CarpetCleaning.CarpetDebugApi.StrokeNow(3, 0.14f,0.30f, 0.86f,0.30f, 160, 0.5f);
CarpetCleaning.CarpetDebugApi.StrokeNow(3, 0.86f,0.43f, 0.14f,0.43f, 160, 0.5f);
CarpetCleaning.EconomyDebugApi.SetLevel(4, 1, 6);   // brush AREA 6 for #10 (pad r 78 mm: the blue skirt stands clear of the foam)
return CarpetCleaning.CarpetDebugApi.SurfaceProbeJson(0.12f, 0.28f, 0.89f, 0.45f);
'''
F['tr10_take.cs'] = take('tr10_scrub', '// #10 SCRUB (0:24.0-0:26.7): the rotary brush (AREA 6) scrubs 4 legs inside the band: foam swirls, greys to brown mush.', [
    'Tool(4)', 'Hover(0.68f, 0.365f, 0.30f)', 'Stroke(0.66f, 0.365f, 0.36f, 0.345f, 0.66f)', 'Move(0.36f, 0.345f, 0.64f, 0.385f, 0.66f, true)',
    'Move(0.64f, 0.385f, 0.38f, 0.365f, 0.66f, true)', 'Move(0.38f, 0.365f, 0.62f, 0.35f, 0.66f, true)', 'Up(0.35f)', 'Away(0.20f)'])
F['q_band.cs'] = ('return "band " + CarpetCleaning.CarpetDebugApi.SurfaceProbeJson(0.12f, 0.28f, 0.89f, 0.45f) + " | far " + '
                  'CarpetCleaning.CarpetDebugApi.SurfaceProbeJson(0.12f, 0.39f, 0.89f, 0.45f) + " | spill " + CarpetCleaning.CarpetDebugApi.PlowSpillJson();\n')
F['tr11_take.cs'] = take('tr11_squeegee', '// #11 SQUEEGEE (0:26.7-0:29.3, wider "how" frame): one horizontal pass at v 0.30 (blade 264 mm covers v 0.21-0.39)\n'
                         '// off the right rim; the far strip v 0.39-0.45 keeps its slurry for #12. Touch-down outside the left rim.', [
    'Tool(5)', 'Hover(-0.06f, 0.30f, 0.20f)', 'Stroke(-0.03f, 0.30f, 1.12f, 0.30f, 2.30f)', 'Move(1.12f, 0.30f, 1.42f, 0.315f, 0.40f, false)', 'Away(1.00f)'])
F['p12_row1.cs'] = '''CarpetCleaning.CarpetDebugApi.StrokeNow(6, 0.08f,0.30f, 0.90f,0.30f, 179, 1f);
return CarpetCleaning.CarpetDebugApi.SurfaceProbeJson(0.12f, 0.39f, 0.89f, 0.45f);
'''
F['tr12_take.cs'] = take('tr12_extract', '// #12 EXTRACT (0:29.3-0:32.0, wider): wet-vac row 2 at v 0.42 R->L swallows the far strip slurry; behind it the\n'
                         '// pattern bright, the pile dark with moisture and striped; the lift runs ALONG the row (a lift across swings the wand).', [
    'Tool(6)', 'Hover(0.935f, 0.42f, 0.30f)', 'Stroke(0.905f, 0.42f, 0.10f, 0.42f, 2.93f)', 'Move(0.10f, 0.42f, 0.085f, 0.422f, 0.16f, false)',
    'Move(0.085f, 0.422f, -0.35f, 0.46f, 0.56f, false)', 'Away(0.60f)'])
F['tr13_take.cs'] = ('// dryer POWER 6 (P 2.31) for #13 (brief_chain 5)\nCarpetCleaning.EconomyDebugApi.SetLevel(7, 0, 6);\n'
                     + take('tr13_refluff', '// #13 REFLUFF (0:32.0-0:34.7): the dryer along the band centre L->R at 171 mm/s: the damp dark striped pile dries,\n'
                            '// lightens and stands up at the ring trailing edge. Ring only.', [
                                'Tool(7)', 'Hover(0.05f, 0.36f, 0.20f)', 'Move(0.05f, 0.36f, 0.62f, 0.36f, 4.00f, true)', 'Up(0.30f)', 'Away(0.50f)']))

lanes = open(os.path.join(D, '_lanes.inc'), encoding='utf-8').read()
for n, body in F.items():
    if n.endswith('.body'):
        n = n[:-5] + '.cs'
        body = lanes + body
    with open(os.path.join(D, n), 'w', encoding='utf-8', newline='\n') as fh:
        fh.write(body)
print(sorted(f for f in os.listdir(D) if f.endswith('.cs')))
