// probe: the star with crust for the reveal's dirty half; camera over the far half (v 0.5-1)
CarpetCleaning.CarpetDebugApi.ResetWithProfile(394, 0.75f, 0.3f, 0.45f, 0.85f);
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
var cam = UnityEngine.Camera.main;
cam.transform.SetPositionAndRotation(new UnityEngine.Vector3(0f, 1.1f, 0.05f), UnityEngine.Quaternion.LookRotation(new UnityEngine.Vector3(0f, 0f, 0.42f) - new UnityEngine.Vector3(0f, 1.1f, 0.05f)));
cam.fieldOfView = 38f;
return CarpetCleaning.CarpetDebugApi.DirtJson() + " " + CarpetCleaning.CarpetDebugApi.StatsJson();
