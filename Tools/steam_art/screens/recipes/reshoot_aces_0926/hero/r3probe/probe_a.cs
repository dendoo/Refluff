UnityEditor.EditorApplication.isPaused = true; var sb = new System.Text.StringBuilder();
CarpetCleaning.CarpetDebugApi.Reset(394, 0.9f);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
sb.Append("394 ").Append(CarpetCleaning.CarpetDebugApi.DirtJson()).Append(" ").Append(CarpetCleaning.StoreShotApi.RenderScene("test/r3probe_394.png", 3840, 2160)).Append(" | ");
CarpetCleaning.CarpetDebugApi.Reset(174, 0.9f);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
sb.Append("174 ").Append(CarpetCleaning.CarpetDebugApi.DirtJson()).Append(" ").Append(CarpetCleaning.StoreShotApi.RenderScene("test/r3probe_174.png", 3840, 2160)).Append(" | ");
return sb.ToString();
