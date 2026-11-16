// Free camera for a take: keys {t, px,py,pz, tx,ty,tz, fov, focusM} (world metres, game seconds since this eval; focus 0 =
// no DoF), Catmull-Rom through the keys with an ease-in/out time warp per segment. Driven from
// RenderPipelineManager.beginContextRendering (Application.onBeforeRender does NOT fire inside EditorApplication.Step), so it
// follows Time.time per Step while the StoreGifApi queue drives the tool. look_off.cs unhooks it.
{
var adC = System.AppDomain.CurrentDomain;
var oldHook = adC.GetData("ttCamHook") as System.Action<UnityEngine.Rendering.ScriptableRenderContext, System.Collections.Generic.List<UnityEngine.Camera>>;
if (oldHook != null) UnityEngine.Rendering.RenderPipelineManager.beginContextRendering -= oldHook;
float[][] K = CAMKEYS;
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
