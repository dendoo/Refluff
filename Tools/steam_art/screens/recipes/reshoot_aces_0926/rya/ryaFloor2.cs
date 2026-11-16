// ACES re-shoot 05: copy of reshoot_d/rya/ryaFloor2 (its dead unused path string removed) with the near (bottom) mop strips carried
// to u 0.90 (was 0.65), so the scraper chunks on the near tile at u 0.60-0.80 are mopped up too.
UnityEditor.EditorApplication.isPaused = true;
for (int p=0;p<2;p++){
float[] tv = {1.05f,1.15f,1.24f};
for (int i=0;i<tv.Length;i++){ if(((i+p)&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(9, -0.30f,tv[i], 0.90f,tv[i], 60, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(9, 0.90f,tv[i], -0.30f,tv[i], 60, 1f); }
float[] bv = {-0.05f,-0.15f,-0.23f};
for (int i=0;i<bv.Length;i++){ if(((i+p)&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(9, -0.30f,bv[i], 0.90f,bv[i], 70, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(9, 0.90f,bv[i], -0.30f,bv[i], 70, 1f); }
float[] lu = {-0.08f,-0.20f,-0.32f};
for (int i=0;i<lu.Length;i++){ if(((i+p)&1)==0) CarpetCleaning.CarpetDebugApi.StrokeNow(9, lu[i],-0.28f, lu[i],1.28f, 70, 1f); else CarpetCleaning.CarpetDebugApi.StrokeNow(9, lu[i],1.28f, lu[i],-0.28f, 70, 1f); }
}
return CarpetCleaning.CarpetDebugApi.FloorStatsJson() + CarpetCleaning.CarpetDebugApi.Sim.Hint;
