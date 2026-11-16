// let the touched-up tiles dry (the mop's damp streaks fade with F_TAU_WET 16 s): 12 s
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.RunFrames(720);
return "ok";
