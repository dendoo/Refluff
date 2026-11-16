// EXPERIMENT: initial dirt for profile variants (seed 17)
UnityEditor.EditorApplication.isPaused = true;
string r = "";
float[][] P = { new float[]{1f,1f,1f,0f,0f}, new float[]{1f,0.5f,1f,0f,0f}, new float[]{1f,1f,1f,1f,0f} };
foreach (var p in P) { CarpetCleaning.CarpetDebugApi.ResetWithProfile(17, p[0], p[1], p[2], p[3], p[4]); CarpetCleaning.CarpetDebugApi.RunFrames(1); r += "L" + p[0] + " c" + p[1] + " m" + p[2] + " d" + p[3] + ": " + CarpetCleaning.CarpetDebugApi.StatsJson().Substring(0, 110) + " || "; }
return r;
