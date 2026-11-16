// Undo look_on.inc + cam.inc: the game's 24-shell mesh, sun, ambient and camera pose; DoF volume and camera hook removed.
{
var ad = System.AppDomain.CurrentDomain;
var hook = ad.GetData("ttCamHook") as System.Action<UnityEngine.Rendering.ScriptableRenderContext, System.Collections.Generic.List<UnityEngine.Camera>>;
if (hook != null) { UnityEngine.Rendering.RenderPipelineManager.beginContextRendering -= hook; ad.SetData("ttCamHook", null); }
var go = ad.GetData("ttDofGo") as UnityEngine.GameObject;
if (go != null) { go.GetComponent<UnityEngine.Rendering.Volume>().enabled = false; UnityEngine.Object.Destroy(go); }
ad.SetData("ttDofGo", null); ad.SetData("ttDof", null);
var cr = UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.CarpetRenderer>();
var mf = cr.Shells.GetComponent<UnityEngine.MeshFilter>();
var orig = (UnityEngine.Mesh)typeof(CarpetCleaning.CarpetRenderer).GetField("_shellMesh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(cr);
if (mf.sharedMesh != orig) { var cur = mf.sharedMesh; mf.sharedMesh = orig; if (cur != null && cur.name == "StoreShotShells") UnityEngine.Object.Destroy(cur); }
var L0 = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
L0.transform.rotation = UnityEngine.Quaternion.Euler(50f, 330f, 0f);
L0.intensity = 3f; L0.colorTemperature = 5000f; L0.shadows = UnityEngine.LightShadows.None;
UnityEngine.RenderSettings.ambientSkyColor = new UnityEngine.Color(0.55f, 0.58f, 0.66f);
var cam = UnityEngine.Camera.main;
cam.transform.SetPositionAndRotation(new UnityEngine.Vector3(0f, 1.5f, -1.12f), UnityEngine.Quaternion.Euler(55f, 0f, 0f));
cam.fieldOfView = 45f; cam.nearClipPlane = 0.05f;
}
return "look off";
