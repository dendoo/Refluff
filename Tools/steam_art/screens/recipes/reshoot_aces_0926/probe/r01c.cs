UnityEditor.EditorApplication.isPaused = true; var sb = new System.Text.StringBuilder();
CarpetCleaning.CarpetDebugApi.ResetWithProfile(242, 0.9f, 1f, -1f, 0f, -1f);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
sb.Append("01_242_rwp ").Append(CarpetCleaning.CarpetDebugApi.DirtJson()).Append(" ").Append(CarpetCleaning.StoreShotApi.RenderScene("test/aces_probe_01_242_rwp.png", 1920, 1080)).Append(" | ");
CarpetCleaning.CarpetDebugApi.ResetWithProfile(435, 0.9f, 1f, -1f, 0f, -1f);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
sb.Append("01_435_rwp ").Append(CarpetCleaning.CarpetDebugApi.DirtJson()).Append(" ").Append(CarpetCleaning.StoreShotApi.RenderScene("test/aces_probe_01_435_rwp.png", 1920, 1080)).Append(" | ");
return sb.ToString();
