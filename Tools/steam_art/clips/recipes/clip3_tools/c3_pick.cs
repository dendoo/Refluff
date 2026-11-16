// clip3_tools: NewCarpetNow until the Transylvanian medallion rug is on the stage.
UnityEditor.EditorApplication.isPaused = true;
var sb = new System.Text.StringBuilder();
for (int i = 0; i < 8 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_transylvanian"); i++) { CarpetCleaning.CarpetDebugApi.NewCarpetNow(); var h = CarpetCleaning.CarpetDebugApi.HandoverJson(); int k = h.IndexOf("picName"); sb.Append(h.Substring(k, 40)).Append(" | "); }
return sb.ToString() + CarpetCleaning.CarpetDebugApi.HandoverJson();
