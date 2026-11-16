// clip4_rugs r3: runtime stand-in for the CarpetHandover.ApplyPose floor fix (the game's C# could not be edited in this
// capture session). FlyOut eases the position InQuad(t) but the tilt Smooth(t/0.70) around the rug centre, so the near-left
// corner sank up to ~77 mm under the tiles (art director, round 2). This installs a RenderPipelineManager.beginCameraRendering
// hook that runs after CarpetHandover.LateUpdate and before every camera render (Game view + capture): while the stage is
// flying, it lifts the stage just enough that its lowest corner stays at its resting height (y 0 = backing top at rest).
// Exactly what the proposed C# fix does at the end of ApplyPose:
//     float minY = 0f; for (int i = 0; i < 4; i++) minY = Mathf.Min(minY, (rot * new Vector3((i & 1) == 0 ? -hx : hx, 0f, (i & 2) == 0 ? -hz : hz)).y);
//     pos.y = Mathf.Max(pos.y, -minY);       // hx, hz = CarpetSimulation.CarpetSizeM * 0.5
// Idempotent (re-installing replaces the old hook); c4_clamp_off.cs removes it.
const string KEY = "clip4_floor_clamp";
var old = System.AppDomain.CurrentDomain.GetData(KEY) as System.Action<UnityEngine.Rendering.ScriptableRenderContext, UnityEngine.Camera>;
if (old != null) UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= old;
System.Action<UnityEngine.Rendering.ScriptableRenderContext, UnityEngine.Camera> f = (ctx, cam) =>
{
    var h = CarpetCleaning.CarpetDebugApi.Handover;
    if (h == null || !h.IsAnimating || h.view == null) return;
    var st = h.view.Stage;
    if (st == null) return;
    var half = CarpetCleaning.CarpetSimulation.CarpetSizeM * 0.5f;
    var r = st.localRotation;
    float minY = 0f;
    for (int i = 0; i < 4; i++)
        minY = UnityEngine.Mathf.Min(minY, (r * new UnityEngine.Vector3((i & 1) == 0 ? -half.x : half.x, 0f, (i & 2) == 0 ? -half.y : half.y)).y);
    var p = st.localPosition;
    if (p.y < -minY) { p.y = -minY; st.localPosition = p; }
};
UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += f;
System.AppDomain.CurrentDomain.SetData(KEY, f);
return "clamp on (replaced old: " + (old != null) + ")";
