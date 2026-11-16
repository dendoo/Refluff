// let the mopped tiles dry (the mop's damp streaks fade with F_TAU_WET 16 s): 10 s
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.RunFrames(600);
return "ok";
