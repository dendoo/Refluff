UnityEditor.EditorApplication.isPaused = true; var sb = new System.Text.StringBuilder();
CarpetCleaning.CarpetDebugApi.Reset(101, 0.9f);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
sb.Append("101 ").Append(CarpetCleaning.CarpetDebugApi.DirtJson()).Append(" ").Append(CarpetCleaning.StoreShotApi.RenderScene("test/r3probe_101.png", 3840, 2160)).Append(" | ");
CarpetCleaning.CarpetDebugApi.Reset(242, 0.9f);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
sb.Append("242 ").Append(CarpetCleaning.CarpetDebugApi.DirtJson()).Append(" ").Append(CarpetCleaning.StoreShotApi.RenderScene("test/r3probe_242.png", 3840, 2160)).Append(" | ");
return sb.ToString();
