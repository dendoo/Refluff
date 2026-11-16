// clip4 diagnostic: framing of the flight. Spotless Transylvanian medallion (Reset(14, 0f)), a REAL hand-over (pays),
// seeks through FlyOut, Away, then the Anatolian star (seed 15 @0.95) seeks through FlyIn -> SteamStore/_work/test/c4_fp_*.png
UnityEditor.EditorApplication.isPaused = true;
var H = CarpetCleaning.CarpetDebugApi.Handover;
var sim = CarpetCleaning.CarpetDebugApi.Sim;
CarpetCleaning.EconomyDebugApi.PayoutResume();
if (H.State != CarpetCleaning.HandoverState.Idle) CarpetCleaning.CarpetDebugApi.HandoverSnap();
for (int i = 0; i < 8 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_transylvanian"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
CarpetCleaning.CarpetDebugApi.HandoverSnap();
CarpetCleaning.CarpetDebugApi.Reset(14, 0f);
// the next picture = the Anatolian star
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var bag = (System.Collections.Generic.List<int>)typeof(CarpetCleaning.CarpetHandover).GetField("_picBag", bf).GetValue(H);
int ai = System.Array.FindIndex(H.pictures, p => p != null && p.name == "rug_anatolian_star");
bag.Remove(ai); bag.Insert(0, ai);
for (int i = 0; i < 10; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = true;
var sb = new System.Text.StringBuilder();
sb.Append(CarpetCleaning.CarpetDebugApi.HandoverJson()).Append(" begin=").Append(CarpetCleaning.CarpetDebugApi.HandoverNow(false));
float[] ts = { 0.3f, 0.6f, 0.8f, 0.9f, 1.0f };
foreach (var t in ts) { CarpetCleaning.CarpetDebugApi.HandoverSeek(t); CarpetCleaning.StoreShotApi.RenderScene("test/c4_fp_out" + (int)(t * 100) + ".png", 1280, 720); }
CarpetCleaning.CarpetDebugApi.HandoverStep(1f);
sb.Append(" | ").Append(CarpetCleaning.CarpetDebugApi.HandoverJson());
CarpetCleaning.StoreShotApi.RenderScene("test/c4_fp_away.png", 1280, 720);
sb.Append(" take=").Append(CarpetCleaning.CarpetDebugApi.TakeNewCarpetNow());
sim.ResetCarpet(sim.Seed, false, 0.95f);
float[] ti = { 0.0f, 0.15f, 0.3f, 0.5f, 0.7f, 0.85f };
foreach (var t in ti) { CarpetCleaning.CarpetDebugApi.HandoverSeek(t); CarpetCleaning.StoreShotApi.RenderScene("test/c4_fp_in" + (int)(t * 100) + ".png", 1280, 720); }
sb.Append(" | ").Append(CarpetCleaning.CarpetDebugApi.HandoverJson()).Append(' ').Append(CarpetCleaning.CarpetDebugApi.DirtJson());
CarpetCleaning.CarpetDebugApi.HandoverStep(2f);
sb.Append(" | ").Append(CarpetCleaning.CarpetDebugApi.HandoverJson());
return sb.ToString();
