UnityEditor.EditorApplication.isPaused = true; var sb = new System.Text.StringBuilder();
CarpetCleaning.CarpetDebugApi.Reset(242, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
sb.Append("01_242_l1 ").Append(CarpetCleaning.CarpetDebugApi.DirtJson()).Append(" ").Append(CarpetCleaning.StoreShotApi.RenderScene("test/aces_probe_01_242_l1.png", 1920, 1080)).Append(" | ");
CarpetCleaning.CarpetDebugApi.Reset(435, 1f);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
sb.Append("01_435_l1 ").Append(CarpetCleaning.CarpetDebugApi.DirtJson()).Append(" ").Append(CarpetCleaning.StoreShotApi.RenderScene("test/aces_probe_01_435_l1.png", 1920, 1080)).Append(" | ");
return sb.ToString();
