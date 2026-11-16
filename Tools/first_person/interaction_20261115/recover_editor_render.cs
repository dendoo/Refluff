if (UnityEditor.EditorApplication.isPlaying || UnityEditor.BuildPipeline.isBuildingPlayer) throw new System.InvalidOperationException("Stopped idle Editor required");
if (UnityEngine.Rendering.RenderPipelineManager.currentPipeline == null) UnityEngine.Rendering.Blitter.Cleanup();
UnityEditor.SceneView.RepaintAll();
UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
return "Stale post-build blitter cleared only while no render pipeline exists";
