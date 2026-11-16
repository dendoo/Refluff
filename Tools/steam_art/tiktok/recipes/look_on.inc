// TikTok macro look (key-art rules, STORE_ART.md 4): 48-shell pile mesh (the game uses 24), warm raking sun with soft
// shadows, cooler ambient, and a global Bokeh DoF volume (focus driven per frame by cam.inc). Undo: look_off.cs.
{
var cr = UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.CarpetRenderer>();
var mf = cr.Shells.GetComponent<UnityEngine.MeshFilter>();
var szL = CarpetCleaning.CarpetSimulation.CarpetSizeM;
if (mf.sharedMesh == null || mf.sharedMesh.name != "StoreShotShells") {
  var nm = CarpetCleaning.ShellMeshBuilder.BuildShells(szL.x, szL.y, 48, cr.shellSubdivX, cr.shellSubdivZ, cr.pileHeightM);
  nm.name = "StoreShotShells"; nm.hideFlags = UnityEngine.HideFlags.DontSave; mf.sharedMesh = nm; }
var L = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
L.transform.rotation = UnityEngine.Quaternion.Euler(40f, 30f, 0f);
L.intensity = 4.8f; L.colorTemperature = 4800f; L.shadows = UnityEngine.LightShadows.Soft;
UnityEngine.RenderSettings.ambientSkyColor = new UnityEngine.Color(0.62f, 0.65f, 0.72f);
if (System.AppDomain.CurrentDomain.GetData("ttDofGo") == null) {
  var prof = UnityEngine.ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>(); prof.hideFlags = UnityEngine.HideFlags.DontSave;
  var dof = prof.Add<UnityEngine.Rendering.Universal.DepthOfField>(true);
  dof.mode.Override(UnityEngine.Rendering.Universal.DepthOfFieldMode.Bokeh);
  dof.focusDistance.Override(0.35f); dof.aperture.Override(5.6f); dof.focalLength.Override(24f);
  var go = new UnityEngine.GameObject("TikTok DoF") { hideFlags = UnityEngine.HideFlags.DontSave };
  var vol = go.AddComponent<UnityEngine.Rendering.Volume>(); vol.isGlobal = true; vol.priority = 1000f; vol.sharedProfile = prof;
  System.AppDomain.CurrentDomain.SetData("ttDofGo", go); System.AppDomain.CurrentDomain.SetData("ttDof", dof);
}
}
