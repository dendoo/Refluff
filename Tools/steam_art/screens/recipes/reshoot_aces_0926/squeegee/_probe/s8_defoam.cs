// slow dry-vac pass over the whole rug: lifts the remaining suds, leaves the standing brown water
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Zigzag(1, 0.04f,0.96f, 0.96f,0.04f, 9, 70, 1f);
return "ok";
