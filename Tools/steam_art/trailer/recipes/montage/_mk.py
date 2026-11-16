"""Writes the #19 montage evals (brief_lamp_montage 2): 4 cuts x 1.33 s, the press on the cut frame, no HUD, full 4K frame.
A rya in a pink foam band (after reshoot_aces_0926/rya up to n_dry2), B Salor Turkmen under the hose (hose_final h1-h6),
C European green under the dryer (scripts_scraper_lamp_spot l_st1-l_st5), D the mop on the tile beside the Transylvanian.
Run: python _mk.py"""
import os

D = os.path.dirname(os.path.abspath(__file__))
SB = 'var S = CarpetCleaning.StoreGifApi.SeqBegin("%s", 3840, 2160, 0, 0, 0, 0, false, 1, true, 2560, 1440, 60, true);'
G = 'CarpetCleaning.StoreGifApi.'


def take(name, comment, pre, items):
    return (comment + '\nCarpetCleaning.CarpetDebugApi.UnpinToolHead();\n' + pre + (SB % name) + '\n'
            + '\n'.join(G + x + ';' for x in items) + '\nreturn S + " " + CarpetCleaning.StoreGifApi.Run(3000);\n')


F = {}
F['a0_band.cs'] = '''// cut A pre: the pink band's right lane laid fast, so the band is there on frame 0
CarpetCleaning.CarpetDebugApi.StrokeNow(3, 0.635f,0.06f, 0.635f,0.95f, 70, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(3, 0.635f,0.95f, 0.635f,0.06f, 70, 1f);
return CarpetCleaning.CarpetDebugApi.SurfaceProbeJson(0.55f, 0.05f, 0.70f, 0.95f);
'''
F['a1_take.cs'] = take('t19a_rya', '// #19 cut A (0:48.00-0:49.33): the foam ring runs down the band\'s left lane toward the camera over the vivid clean half\n'
                       '// of the Finnish rya; the pink mound widens. Press at ~T 0.37 (the cut frame).', '', [
    'Tool(3)', 'Hover(0.595f, 1.00f, 0.35f)', 'Move(0.595f, 1.00f, 0.595f, 0.06f, 1.24f, true)', 'Up(0.25f)',
    'Move(0.595f, 0.06f, 0.70f, -0.06f, 0.20f, false)', 'Away(0.30f)'])
F['b0_settle.cs'] = 'CarpetCleaning.CarpetDebugApi.RunFrames(90);\nreturn CarpetCleaning.CarpetDebugApi.StatsJson();\n'
F['b1_take.cs'] = take('t19b_salor', '// #19 cut B (0:49.33-0:50.67): the blue hose nozzle sprays the Salor Turkmen at half pressure, beads fly, the glossy\n'
                       '// sheet spreads over the dark soaked field. Press at ~T 0.46.', '', [
    'Call(() => CarpetCleaning.CarpetDebugApi.Tools.SnapHeadingOnNextMove())', 'Tool(2)', 'Hover(0.33f, 0.74f, 0.30f)',
    'Move(0.33f, 0.74f, 0.34f, 0.70f, 0.16f, false)', 'Move(0.34f, 0.70f, 0.40f, 0.42f, 0.45f, true, true)',
    'Move(0.40f, 0.42f, 0.42f, 0.29f, 0.40f, true, true)', 'Move(0.42f, 0.29f, 0.42f, 0.29f, 0.30f, true, true)', 'Up(0.35f)', 'Away(0.20f)'])
F['c1_take.cs'] = take('t19c_eu', '// #19 cut C (0:50.67-0:52.00): the dryer ring runs down a damp striped lane of the European green (the left third\n'
                       '// already dried and upright). Press at ~T 0.25.', '', [
    'Tool(7)', 'Hover(0.53f, 1.03f, 0.25f)', 'Move(0.53f, 1.03f, 0.53f, 0.36f, 1.46f, true)', 'Up(0.30f)', 'Away(0.20f)'])
F['d1_stage.cs'] = '''// cut D staging (mop1 seed): the Transylvanian with mud 0.58 and no crust, hosed along the left border so brown runoff lies
// on the tile beside it; factory tools (no LiveTuning), as mop1 / mop4 were shot
var H = CarpetCleaning.CarpetDebugApi.Handover;
if (H.State != CarpetCleaning.HandoverState.Idle) CarpetCleaning.CarpetDebugApi.HandoverSnap();
for (int i = 0; i < 12 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_transylvanian"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
CarpetCleaning.CarpetDebugApi.Reset(20, 0.6f);
CarpetCleaning.CarpetDebugApi.Zigzag(2, 0.03f,0.25f, 0.35f,0.75f, 6, 30, 1f);
CarpetCleaning.CarpetDebugApi.Hold(2, 0.04f,0.40f, 1.5f);
CarpetCleaning.CarpetDebugApi.Hold(2, 0.04f,0.60f, 1.5f);
CarpetCleaning.CarpetDebugApi.RunFrames(120);
return CarpetCleaning.CarpetDebugApi.FloorStatsJson();
'''
F['d2_lane.cs'] = '''// mop4's first lane further out, so a clean lane already lies beyond the one the take wipes
CarpetCleaning.CarpetDebugApi.StrokeNow(9, -0.36f,0.95f, -0.36f,0.05f, 70, 1f);
CarpetCleaning.StoreShotApi.RenderScene("trailer/probe/m19d.png", 1280, 720);
return CarpetCleaning.CarpetDebugApi.FloorStatsJson();
'''
F['d3_take.cs'] = take('t19d_mop', '// #19 cut D (0:52.00-0:53.33): the mop pad wipes a clean lane through the brown runoff on the tile beside the\n'
                       '// Transylvanian\'s left border, away from the camera. Press at ~T 0.41.', '', [
    'Call(() => CarpetCleaning.CarpetDebugApi.Tools.SnapHeadingOnNextMove())', 'Tool(9)', 'Hover(-0.16f, -0.14f, 0.25f)',
    'Move(-0.16f, -0.14f, -0.16f, -0.10f, 0.16f, false)', 'Move(-0.16f, -0.10f, -0.16f, 0.58f, 1.13f, true)', 'Up(0.35f)', 'Away(0.20f)'])
for n, body in F.items():
    with open(os.path.join(D, n), 'w', encoding='utf-8', newline='\n') as fh:
        fh.write(body)
print(sorted(F))
