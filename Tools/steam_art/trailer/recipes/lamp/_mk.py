"""Writes the #18 lamp staging + take evals (brief_lamp_montage 1). Run: python _mk.py
Rug: the Anatolian star with seed 39's light soil at level 0.3 (lamp9.cs dirt): a big patch left, a medium one bottom right,
small spots near the corners. Dust vacuumed, the left patch washed + extracted (a damp blue zone for Z), the rest left as
3-8 amber leftovers for X; the floor mopped clean (the floor has no lamp level, any crumb or runoff would glow)."""
import os

D = os.path.dirname(os.path.abspath(__file__))
F = {}
F['l1_pick.cs'] = '''CarpetCleaning.CarpetDebugApi.UnpinToolHead();
var H = CarpetCleaning.CarpetDebugApi.Handover;
if (H.State != CarpetCleaning.HandoverState.Idle) CarpetCleaning.CarpetDebugApi.HandoverSnap();
for (int i = 0; i < 12 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_anatolian_star"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
CarpetCleaning.CarpetDebugApi.Reset(39, 0.3f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
return CarpetCleaning.CarpetDebugApi.DirtJson() + " " + CarpetCleaning.CarpetDebugApi.StatsJson();
'''
F['l2_probe.cs'] = '''// X / Z probes: instant lamp, 6 real Steps, a 1280x720 render, lamp off
foreach (var w in new[]{1, 0}) {
  CarpetCleaning.CarpetDebugApi.SetHighlight(w, true, true);
  for (int i = 0; i < 6; i++) UnityEditor.EditorApplication.Step();
  CarpetCleaning.StoreShotApi.RenderScene("trailer/probe/l18_" + (w == 1 ? "x" : "z") + ".png", 1280, 720);
  CarpetCleaning.CarpetDebugApi.SetHighlight(w, false, true);
}
UnityEditor.EditorApplication.isPaused = true;
return CarpetCleaning.CarpetDebugApi.HighlightJson();
'''
F['l3a_dust.cs'] = 'CarpetCleaning.CarpetDebugApi.Zigzag(1, 0.04f,0.04f,0.96f,0.96f, 13, 50, 1f);\nreturn CarpetCleaning.CarpetDebugApi.StatsJson();\n'
F['l3b_dust.cs'] = 'CarpetCleaning.CarpetDebugApi.Zigzag(1, 0.96f,0.07f,0.04f,0.93f, 12, 50, 1f);\nreturn CarpetCleaning.CarpetDebugApi.StatsJson();\n'
F['l3c_dust.cs'] = 'CarpetCleaning.CarpetDebugApi.Zigzag(1, 0.04f,0.04f,0.96f,0.96f, 13, 50, 1f);\nreturn CarpetCleaning.CarpetDebugApi.StatsJson();\n'
F['l4_wash.cs'] = '''float[] hu = {0.07f,0.13f,0.19f,0.25f,0.31f,0.37f};
for (int i = 0; i < hu.Length; i++) { if ((i & 1) == 0) CarpetCleaning.CarpetDebugApi.StrokeNow(2, hu[i],0.92f, hu[i],0.30f, 45, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(2, hu[i],0.30f, hu[i],0.92f, 45, 1f); }
float[] bu = {0.075f,0.15f,0.22f,0.29f,0.365f};
for (int r = 0; r < 2; r++) for (int i = 0; i < bu.Length; i++) { if (((i + r) & 1) == 0) CarpetCleaning.CarpetDebugApi.StrokeNow(4, bu[i],0.92f, bu[i],0.30f, 50, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(4, bu[i],0.30f, bu[i],0.92f, 50, 1f); }
return CarpetCleaning.CarpetDebugApi.StatsJson();
'''
F['l5_extract.cs'] = '''float[] wu = {0.10f,0.22f,0.34f};
for (int r = 0; r < 4; r++) for (int i = 0; i < wu.Length; i++) { if (((i + r) & 1) == 0) CarpetCleaning.CarpetDebugApi.StrokeNow(6, wu[i],0.95f, wu[i],0.28f, 60, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(6, wu[i],0.28f, wu[i],0.95f, 60, 1f); }
return CarpetCleaning.CarpetDebugApi.StatsJson();
'''
F['l6_floor.cs'] = '''for (int i=0;i<2;i++){ CarpetCleaning.CarpetDebugApi.StrokeNow(9, -0.08f-0.11f*i,1.12f, -0.08f-0.11f*i,-0.12f, 70, 1f); CarpetCleaning.CarpetDebugApi.StrokeNow(9, 1.08f+0.11f*i,-0.12f, 1.08f+0.11f*i,1.12f, 70, 1f); }
CarpetCleaning.CarpetDebugApi.StrokeNow(9, -0.15f,1.07f, 1.15f,1.07f, 70, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(9, 1.15f,1.16f, -0.15f,1.16f, 70, 1f);
CarpetCleaning.CarpetDebugApi.StrokeNow(9, -0.15f,-0.07f, 1.15f,-0.07f, 70, 1f);
return CarpetCleaning.CarpetDebugApi.FloorStatsJson();
'''
F['l7_check.cs'] = '''CarpetCleaning.CarpetDebugApi.RunFrames(60);
CarpetCleaning.CarpetDebugApi.Sim.RefreshStatsSync();
return CarpetCleaning.CarpetDebugApi.HighlightJson() + " | floor " + CarpetCleaning.CarpetDebugApi.FloorStatsJson();
'''
F['l8_money.cs'] = '''// wallet continuity: #17 ends at $1,885 (1840 + 120 - 75); wet vacuum selected so the tool list matches the damp zone
var E = CarpetCleaning.EconomyDebugApi.Eco;
CarpetCleaning.EconomyDebugApi.Grant(1885 - E.Data.money);
CarpetCleaning.CarpetDebugApi.SetTool(6);
CarpetCleaning.CarpetDebugApi.SetHighlight(0, false, true);
CarpetCleaning.CarpetDebugApi.SetHighlight(1, false, true);
CarpetCleaning.CarpetDebugApi.Sim.RefreshStatsSync();
return "money " + E.Data.money + " " + CarpetCleaning.CarpetDebugApi.HighlightJson();
'''
F['t18_take.cs'] = '''// #18 (0:45.3-0:48.0, HUD): the lamp. X at T 0.40 (the key's own path: click, chip lit, dim + amber leftovers, scan ring),
// Z on top at T 1.75 (damp zone blue). Pointer away (the head and ring are never dimmed). Real Steps = real fades.
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
var S = CarpetCleaning.StoreGifApi.SeqBegin("t18_lamp", 3840, 2160, 0, 0, 0, 0, true, 1, true, 2560, 1440, 60, true);
CarpetCleaning.StoreGifApi.Tool(6);
CarpetCleaning.StoreGifApi.Away(0.40f);
CarpetCleaning.StoreGifApi.Call(() => CarpetCleaning.CarpetDebugApi.ToggleHighlight(1));
CarpetCleaning.StoreGifApi.Away(1.35f);
CarpetCleaning.StoreGifApi.Call(() => CarpetCleaning.CarpetDebugApi.ToggleHighlight(0));
CarpetCleaning.StoreGifApi.Away(1.65f);
return S + " " + CarpetCleaning.StoreGifApi.Run(3000);
'''
F['l9_off.cs'] = '''CarpetCleaning.CarpetDebugApi.SetHighlight(0, false, true);
CarpetCleaning.CarpetDebugApi.SetHighlight(1, false, true);
return CarpetCleaning.CarpetDebugApi.HighlightJson();
'''
for n, body in F.items():
    with open(os.path.join(D, n), 'w', encoding='utf-8', newline='\n') as fh:
        fh.write(body)
print(sorted(F))
