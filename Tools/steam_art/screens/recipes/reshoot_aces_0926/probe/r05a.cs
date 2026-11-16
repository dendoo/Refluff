UnityEditor.EditorApplication.isPaused = true; var sb = new System.Text.StringBuilder();
CarpetCleaning.CarpetDebugApi.Reset(367, 0.9f);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
sb.Append("05_367_l09 ").Append(CarpetCleaning.CarpetDebugApi.DirtJson()).Append(" ").Append(CarpetCleaning.StoreShotApi.RenderScene("test/aces_probe_05_367_l09.png", 1920, 1080)).Append(" | ");
CarpetCleaning.CarpetDebugApi.Reset(958, 0.9f);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
sb.Append("05_958_l09 ").Append(CarpetCleaning.CarpetDebugApi.DirtJson()).Append(" ").Append(CarpetCleaning.StoreShotApi.RenderScene("test/aces_probe_05_958_l09.png", 1920, 1080)).Append(" | ");
return sb.ToString();
