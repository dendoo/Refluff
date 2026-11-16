if (UnityEditor.EditorApplication.isPlaying || UnityEditor.BuildPipeline.isBuildingPlayer) throw new System.InvalidOperationException("Idle Editor required");
if (UnityEngine.Rendering.RenderPipelineManager.currentPipeline == null) UnityEngine.Rendering.Blitter.Cleanup();
var cam = UnityEngine.Camera.main;
var rt = new UnityEngine.RenderTexture(320,180,24,UnityEngine.RenderTextureFormat.ARGBHalf);
var previous = cam.targetTexture;
try { rt.Create(); cam.targetTexture=rt; cam.Render(); return new { pipeline=UnityEngine.Rendering.RenderPipelineManager.currentPipeline!=null }; }
finally { cam.targetTexture=previous; rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
