// s_spot2 with the in-game cursor parked off the play area (2026-09-26): paused Steps read the OS mouse, which left the
// dryer's outline ring on the tile in the first ACES take. The scripted pointer (ToolController.ScriptedPointer) replaces
// the mouse; a UV outside the floor domain = pointer off the play area = no head and no cursor ring.
UnityEditor.EditorApplication.isPaused = true;
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var eco = CarpetCleaning.EconomyDebugApi.Eco; var sim = CarpetCleaning.CarpetDebugApi.Sim;
var tc = CarpetCleaning.CarpetDebugApi.Tools;
string r;
try
{
    tc.ScriptedPointer = true; tc.ScriptedDown = false; tc.ScriptedRight = false; tc.ScriptedUv = new UnityEngine.Vector2(-20f, -20f);
    typeof(CarpetCleaning.Economy).GetField("_clockAnchor", bf).SetValue(eco, sim.SimTime - 187.6f);
    for (int i = 0; i < 20; i++) UnityEditor.EditorApplication.Step();
    r = CarpetCleaning.StoreShotApi.Shot("08_spotless_handover");
}
finally { tc.ScriptedPointer = false; }
return r + " hint=" + sim.Hint + " clean=" + sim.Cleanliness + " wet=" + sim.Wetness + " " + CarpetCleaning.CarpetDebugApi.HandoverJson() + " " + CarpetCleaning.CarpetDebugApi.ToolHeadJson() + CarpetCleaning.EconomyDebugApi.EconomyJson().Substring(0, 230);
