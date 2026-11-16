UnityEditor.EditorApplication.isPaused = true; var sb = new System.Text.StringBuilder();
CarpetCleaning.CarpetDebugApi.ResetWithProfile(367, 0.9f, 1f, -1f, 0f, -1f);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
sb.Append("05_367_rwp ").Append(CarpetCleaning.CarpetDebugApi.DirtJson()).Append(" ").Append(CarpetCleaning.StoreShotApi.RenderScene("test/aces_probe_05_367_rwp.png", 1920, 1080)).Append(" | ");
CarpetCleaning.CarpetDebugApi.ResetWithProfile(958, 0.9f, 1f, -1f, 0f, -1f);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
sb.Append("05_958_rwp ").Append(CarpetCleaning.CarpetDebugApi.DirtJson()).Append(" ").Append(CarpetCleaning.StoreShotApi.RenderScene("test/aces_probe_05_958_rwp.png", 1920, 1080)).Append(" | ");
return sb.ToString();
