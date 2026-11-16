// let the mopped tiles dry (the mop's damp streaks fade with F_TAU_WET 16 s): 12 s; the rug is dry and does not change
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.RunFrames(720);
return "ok";
