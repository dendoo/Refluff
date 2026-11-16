UnityEditor.EditorApplication.isPaused = true;
var sb = new System.Text.StringBuilder();
for (int i = 0; i < 8 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_anatolian_star"); i++) { CarpetCleaning.CarpetDebugApi.NewCarpetNow(); var h = CarpetCleaning.CarpetDebugApi.HandoverJson(); int k = h.IndexOf("picName"); sb.Append(h.Substring(k, 40)).Append(" | "); }
return sb.ToString();
