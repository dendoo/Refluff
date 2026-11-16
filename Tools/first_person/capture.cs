var player = UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.FirstPersonPlayer>();
player.DebugPose(0f,-1.8f,0f,48f);
UnityEditor.EditorApplication.isPaused = true;
UnityEditor.EditorApplication.Step();
CarpetCleaning.CarpetDebugApi.RunFrames(3);
UnityEditor.EditorApplication.Step();
CarpetCleaning.EconomyDebugApi.CaptureHud("Screenshots/first_person_initial.png",2560,1440);
return player.DiagnosticsJson();
