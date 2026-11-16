// clip4_rugs r3: removes the floor-clamp render hook installed by c4_clamp_on.cs
const string KEY = "clip4_floor_clamp";
var old = System.AppDomain.CurrentDomain.GetData(KEY) as System.Action<UnityEngine.Rendering.ScriptableRenderContext, UnityEngine.Camera>;
if (old != null) UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= old;
System.AppDomain.CurrentDomain.SetData(KEY, null);
return "clamp off (was on: " + (old != null) + ")";
