CarpetCleaning.CarpetDebugApi.PinToolHead(4, 0.48f, 0.30f, true);
CarpetCleaning.CarpetDebugApi.RunFrames(40);
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
var sw=System.Diagnostics.Stopwatch.StartNew(); CarpetCleaning.StoreShotApi.RenderView("keyart/raw3/hero1v2_full.png", 8640, 2790, 0.417f, 0.485f, -0.79f, 34f, -42f, 0f, 15f);
return "A ms=" + sw.ElapsedMilliseconds;
