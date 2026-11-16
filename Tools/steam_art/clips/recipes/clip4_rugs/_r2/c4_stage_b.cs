// clip4_rugs r2, staging for take B (after take A, which left the dirty Anatolian star, seed 15, landed): the SAME rug made
// spotless in place (Reset(15, 0f): same picture, same seed, clean + dry, a real Spotless + Bone dry hand-over again) = the hard
// cut "job done"; card dismissed; the Transylvanian medallion moved to the front of the picture bag; 10 warm-up Steps.
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.EconomyDebugApi.PayoutResume();
CarpetCleaning.CarpetDebugApi.Sim.UiPaused = false;
var H = CarpetCleaning.CarpetDebugApi.Handover;
if (H.State != CarpetCleaning.HandoverState.Idle) CarpetCleaning.CarpetDebugApi.HandoverSnap();
if (!CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_anatolian_star")) return "ERROR not the Anatolian star: " + CarpetCleaning.CarpetDebugApi.HandoverJson();
CarpetCleaning.CarpetDebugApi.Reset(15, 0f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.SetTool(7);
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var bag = (System.Collections.Generic.List<int>)typeof(CarpetCleaning.CarpetHandover).GetField("_picBag", bf).GetValue(H);
int ti = System.Array.FindIndex(H.pictures, p => p != null && p.name == "rug_transylvanian_medallion");
bag.Remove(ti); bag.Insert(0, ti);
for (int i = 0; i < 10; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = true;
return CarpetCleaning.CarpetDebugApi.HandoverJson() + " bag=" + string.Join(",", bag) + " " + CarpetCleaning.CarpetDebugApi.DirtJson();
