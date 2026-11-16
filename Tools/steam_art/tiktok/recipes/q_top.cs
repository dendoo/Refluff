// probe: the reveal's end pose (whole rug from above, far edge up)
var cam = UnityEngine.Camera.main;
var p = new UnityEngine.Vector3(0f, 2.55f, -0.02f);
cam.transform.SetPositionAndRotation(p, UnityEngine.Quaternion.LookRotation(-p, UnityEngine.Vector3.forward));
cam.fieldOfView = 38f;
return "top";
