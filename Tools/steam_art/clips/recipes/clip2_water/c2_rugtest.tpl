// clip2 diagnostic: rug RUG with the clip2 dust profile (seed SEED, level LEVEL), 4K render -> SteamStore/_work/test/c2_RUG_SEED.png
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 8 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("RUG"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
CarpetCleaning.CarpetDebugApi.ResetWithProfile(SEED, LEVEL, 0f, 0f, 1f, 0f);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
CarpetCleaning.StoreShotApi.RenderScene("test/c2_RUG_SEED.png", 3840, 2160);
return CarpetCleaning.CarpetDebugApi.HandoverJson() + " " + CarpetCleaning.CarpetDebugApi.StatsJson();
