// Does beginContextRendering fire per EditorApplication.Step / capture render, with Time.time advancing?
var ad = System.AppDomain.CurrentDomain;
int[] cnt = {0}; float[] last = {0f};
System.Action<UnityEngine.Rendering.ScriptableRenderContext, System.Collections.Generic.List<UnityEngine.Camera>> h = (c, l) => { cnt[0]++; last[0] = UnityEngine.Time.time; };
UnityEngine.Rendering.RenderPipelineManager.beginContextRendering += h;
float t0 = UnityEngine.Time.time;
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 5; i++) UnityEditor.EditorApplication.Step();
UnityEngine.Rendering.RenderPipelineManager.beginContextRendering -= h;
return "calls " + cnt[0] + " dt " + (UnityEngine.Time.time - t0) + " lastRel " + (last[0] - t0);
