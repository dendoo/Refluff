var p=UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.FirstPersonPlayer>();
return "frame="+UnityEngine.Time.frameCount+" paused="+UnityEditor.EditorApplication.isPaused+" player="+(p!=null?p.DiagnosticsJson():"missing");
