// T2 VACUUM: dry vac R->L along v 0.36 (u 0.74 -> 0.26, 2.8 s); low front camera pans with the ring.
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
var S = CarpetCleaning.StoreGifApi.SeqBegin("tt2_vacuum", 2160, 3840, 0, 0, 0, 0, false, 1, true, 2560, 1440, 60, true);
// Free camera for a take: keys {t, px,py,pz, tx,ty,tz, fov, focusM} (world metres, game seconds since this eval; focus 0 =
// no DoF), Catmull-Rom through the keys with an ease-in/out time warp per segment. Driven from
// RenderPipelineManager.beginContextRendering (Application.onBeforeRender does NOT fire inside EditorApplication.Step), so it
// follows Time.time per Step while the StoreGifApi queue drives the tool. look_off.cs unhooks it.
{
var adC = System.AppDomain.CurrentDomain;
var oldHook = adC.GetData("ttCamHook") as System.Action<UnityEngine.Rendering.ScriptableRenderContext, System.Collections.Generic.List<UnityEngine.Camera>>;
if (oldHook != null) UnityEngine.Rendering.RenderPipelineManager.beginContextRendering -= oldHook;
float[][] K = new float[][]{ new[]{0.0000f, 0.2000f, 0.1600f, -0.5800f, 0.2400f, 0.0000f, -0.2100f, 30.0000f, 0.0000f}, new[]{1.6000f, 0.0600f, 0.1500f, -0.5700f, 0.0240f, 0.0000f, -0.2100f, 30.0000f, 0.0000f}, new[]{3.6000f, -0.1200f, 0.1500f, -0.5600f, -0.2400f, 0.0000f, -0.2100f, 30.0000f, 0.0000f} };
float t0C = UnityEngine.Time.time;
var camC = UnityEngine.Camera.main; camC.nearClipPlane = 0.01f;
System.Func<int, int, UnityEngine.Vector3> P = (i, o) => new UnityEngine.Vector3(K[i][o], K[i][o + 1], K[i][o + 2]);
System.Action pose = () => {
  float t = UnityEngine.Time.time - t0C;
  int n = K.Length, i = 0;
  while (i < n - 2 && t > K[i + 1][0]) i++;
  float s = n == 1 ? 0f : UnityEngine.Mathf.Clamp01((t - K[i][0]) / UnityEngine.Mathf.Max(1e-4f, K[i + 1 < n ? i + 1 : i][0] - K[i][0]));
  s = s * s * (3f - 2f * s);
  int a = UnityEngine.Mathf.Max(0, i - 1), b = i, c = UnityEngine.Mathf.Min(n - 1, i + 1), d = UnityEngine.Mathf.Min(n - 1, i + 2);
  System.Func<int, UnityEngine.Vector3> CR = o => {
    UnityEngine.Vector3 p0 = P(a, o), p1 = P(b, o), p2 = P(c, o), p3 = P(d, o);
    if ((p2 - p1).sqrMagnitude < 1e-8f) return p1;      // a hold: Catmull-Rom would overshoot past the key and back
    return 0.5f * (2f * p1 + (-p0 + p2) * s + (2f * p0 - 5f * p1 + 4f * p2 - p3) * s * s + (-p0 + 3f * p1 - 3f * p2 + p3) * s * s * s); };
  var pos = CR(1); var tgt = CR(4);
  var fwd = (tgt - pos).normalized;
  // looking straight down, world up is degenerate and the yaw flips 180 deg: lean "up" toward +z (the far edge) there
  float wUp = UnityEngine.Mathf.SmoothStep(0f, 1f, UnityEngine.Mathf.InverseLerp(0.9f, 0.999f, UnityEngine.Mathf.Abs(fwd.y)));
  var up = UnityEngine.Vector3.Slerp(UnityEngine.Vector3.up, UnityEngine.Vector3.forward, wUp);
  camC.transform.SetPositionAndRotation(pos, UnityEngine.Quaternion.LookRotation(fwd, up));
  camC.fieldOfView = UnityEngine.Mathf.Lerp(K[b][7], K[c][7], s);
  var dofC = adC.GetData("ttDof") as UnityEngine.Rendering.Universal.DepthOfField;
  if (dofC != null) { float f = UnityEngine.Mathf.Lerp(K[b][8], K[c][8], s); dofC.active = f > 0f; if (f > 0f) dofC.focusDistance.value = f; }
};
System.Action<UnityEngine.Rendering.ScriptableRenderContext, System.Collections.Generic.List<UnityEngine.Camera>> hook = (ctx, cams) => pose();
UnityEngine.Rendering.RenderPipelineManager.beginContextRendering += hook; adC.SetData("ttCamHook", hook);
pose();
}
CarpetCleaning.StoreGifApi.Tool(1);
CarpetCleaning.StoreGifApi.Hover(0.77f, 0.360f, 0.35f);
CarpetCleaning.StoreGifApi.Stroke(0.74f, 0.360f, 0.26f, 0.360f, 2.80f);
CarpetCleaning.StoreGifApi.Up(0.20f);
CarpetCleaning.StoreGifApi.Away(0.30f);
return S + " " + CarpetCleaning.StoreGifApi.Run(3000);
