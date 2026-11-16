foreach (var rr in CarpetCleaning.CarpetDebugApi.Heads.GetComponentsInChildren<UnityEngine.Renderer>(true)) rr.SetPropertyBlock(null);
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
{
var cr = UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.CarpetRenderer>();
var mf = cr.Shells.GetComponent<UnityEngine.MeshFilter>();
var orig = (UnityEngine.Mesh)typeof(CarpetCleaning.CarpetRenderer).GetField("_shellMesh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(cr);
if (mf.sharedMesh != orig) { var cur = mf.sharedMesh; mf.sharedMesh = orig; if (cur != null && cur.name == "StoreShotShells") UnityEngine.Object.Destroy(cur); }
}
{
var L0 = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
L0.transform.rotation = UnityEngine.Quaternion.Euler(50f, 330f, 0f);
L0.intensity = 3f; L0.colorTemperature = 5000f; L0.shadows = UnityEngine.LightShadows.None;
UnityEngine.RenderSettings.ambientSkyColor = new UnityEngine.Color(0.55f, 0.58f, 0.66f);
}
UnityEditor.EditorApplication.isPaused = false;


var L = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
var vols = UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Volume>(UnityEngine.FindObjectsInactive.Include);
string v = ""; foreach (var x in vols) v += x.name + ";";
var mf2 = UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.CarpetRenderer>().Shells.GetComponent<UnityEngine.MeshFilter>(); return "shellsVerts=" + mf2.sharedMesh.vertexCount + " mesh=" + mf2.sharedMesh.name + " euler=" + L.transform.eulerAngles + " int=" + L.intensity + " temp=" + L.colorTemperature + " shadows=" + L.shadows + " sky=" + UnityEngine.RenderSettings.ambientSkyColor + " vols=" + v + " paused=" + UnityEditor.EditorApplication.isPaused + " " + CarpetCleaning.StoreShotApi.CameraJson();
