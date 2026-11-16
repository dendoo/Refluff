CarpetCleaning.CarpetDebugApi.PinToolHead(4, 0.48f, 0.30f, true);
CarpetCleaning.CarpetDebugApi.RunFrames(10);
UnityEngine.Transform bt = null; foreach (UnityEngine.Transform c in CarpetCleaning.CarpetDebugApi.Heads.transform) if (c.gameObject.activeSelf) bt = c;
bt.localPosition = new UnityEngine.Vector3(bt.localPosition.x, 0.010f + 0.004f, bt.localPosition.z);
var mpb = new UnityEngine.MaterialPropertyBlock(); mpb.SetFloat("roughnessFactor", 0.28f);
foreach (UnityEngine.Transform c in bt) if (c.name != "Spin") foreach (var rr in c.GetComponentsInChildren<UnityEngine.Renderer>(true)) rr.SetPropertyBlock(mpb);
{
var cr = UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.CarpetRenderer>();
var mf = cr.Shells.GetComponent<UnityEngine.MeshFilter>();
var sz = CarpetCleaning.CarpetSimulation.CarpetSizeM;
if (mf.sharedMesh != null && mf.sharedMesh.name == "StoreShotShells") UnityEngine.Object.Destroy(mf.sharedMesh);
var nm = CarpetCleaning.ShellMeshBuilder.BuildShells(sz.x, sz.y, 48, cr.shellSubdivX, cr.shellSubdivZ, cr.pileHeightM);
nm.name = "StoreShotShells"; nm.hideFlags = UnityEngine.HideFlags.DontSave;
mf.sharedMesh = nm;
}
{
var L = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
L.transform.rotation = UnityEngine.Quaternion.Euler(40f, 30f, 0f);
L.intensity = 4.8f; L.colorTemperature = 4800f; L.shadows = UnityEngine.LightShadows.Soft;
UnityEngine.RenderSettings.ambientSkyColor = new UnityEngine.Color(0.62f, 0.65f, 0.72f);
}
CarpetCleaning.StoreShotApi.RenderView("keyart/test3/td_0.png", 480, 720, 0.286f, 0.345f, -0.479f, 40f, -60f, 0f, 36f);
CarpetCleaning.StoreShotApi.RenderView("keyart/test3/td_1.png", 480, 720, 0.313f, 0.36f, -0.39f, 42f, -75f, 0f, 36f);
{
var L = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
L.transform.rotation = UnityEngine.Quaternion.Euler(30f, -30f, 0f);
L.intensity = 5.0f; L.colorTemperature = 4800f; L.shadows = UnityEngine.LightShadows.Soft;
UnityEngine.RenderSettings.ambientSkyColor = new UnityEngine.Color(0.62f, 0.65f, 0.72f);
}
CarpetCleaning.StoreShotApi.RenderView("keyart/test3/td_2.png", 480, 720, 0.286f, 0.345f, -0.479f, 40f, -60f, 0f, 36f);
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
return "ok";
