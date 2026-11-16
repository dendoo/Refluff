// Capture-session setup. Only temporary runtime state; no source/scene edits.
UnityEditor.EditorApplication.isPaused = true;
var ad = System.AppDomain.CurrentDomain;
if (ad.GetData("screens2510Original") != null) throw new System.Exception("Capture setup already active");
var t = CarpetCleaning.CarpetDebugApi.Tools;
var sim = CarpetCleaning.CarpetDebugApi.Sim;
var cam = UnityEngine.Camera.main;
var cr = UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.CarpetRenderer>();
var mat = cr.ShellsRenderer.sharedMaterial;
var ach = UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.SteamAchievements>();
ad.SetData("screens2510Original", new object[] {cam.transform.position,cam.transform.rotation,cam.fieldOfView,cam.nearClipPlane,t.ScriptedPointer,t.ScriptedUv,t.ScriptedDown,t.ScriptedRight,t.InputEnabled,mat.GetTexture("_PictureTex"),mat.GetVector("_PicOffset"),mat.GetVector("_PicScale"),CarpetCleaning.ToolTuning.Neutral,ach != null && ach.enabled,sim.Paused,sim.UiPaused,CarpetCleaning.StoreShotApi.MarketingHudOn()});
var dir = "C:/UnityProjects/CarpetCleaning/SteamStore/_work/screens/reshoot_20261025";
System.IO.Directory.CreateDirectory(dir);
foreach (var n in new[]{"carpet_save_editor.json","carpet_save.json"}) {var p=System.IO.Path.Combine(UnityEngine.Application.persistentDataPath,n);if(System.IO.File.Exists(p))System.IO.File.Copy(p,System.IO.Path.Combine(dir,n+".before"),false);}
System.IO.File.WriteAllText(System.IO.Path.Combine(dir,"save_dir.txt"), UnityEngine.Application.persistentDataPath);
if(ach!=null)ach.enabled=false;
t.ScriptedPointer=true;t.ScriptedUv=new UnityEngine.Vector2(-5,-5);t.ScriptedDown=false;t.ScriptedRight=false;t.InputEnabled=true;
sim.Paused=false;sim.UiPaused=false;
CarpetCleaning.CarpetDebugApi.SetLanguage("en");
CarpetCleaning.StoreShotApi.MarketingHud(true);
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
// Smooth, modest curved pointer paths. A/B/C are positions; seconds is actual simulation time.
System.Action<int,UnityEngine.Vector2[],float> path = (tool,points,seconds) => {
  CarpetCleaning.CarpetDebugApi.SetTool(tool);t.SnapHeadingOnNextMove();
  int n=UnityEngine.Mathf.Max(2,UnityEngine.Mathf.RoundToInt(seconds*60));
  var prev=points[0];
  for(int f=1;f<=n;f++) {
    float q=f/(float)n;float progress=q-.07f*UnityEngine.Mathf.Sin(2*UnityEngine.Mathf.PI*q);
    float p=progress*(points.Length-1);int i=UnityEngine.Mathf.Min(points.Length-2,(int)p);float u=p-i;
    var p0=points[UnityEngine.Mathf.Max(0,i-1)];var p1=points[i];var p2=points[i+1];var p3=points[UnityEngine.Mathf.Min(points.Length-1,i+2)];
    var cur=.5f*((2*p1)+(-p0+p2)*u+(2*p0-5*p1+4*p2-p3)*u*u+(-p0+3*p1-3*p2+p3)*u*u*u);
    t.StrokeSegment((CarpetCleaning.ToolId)tool,prev,cur,1f/60f,.94f+.05f*UnityEngine.Mathf.Sin(q*4.1f));
    CarpetCleaning.CarpetDebugApi.StepAll(1f/60f);prev=cur;
  }
};
ad.SetData("screens2510Path",path);
return "Capture ready; saves="+UnityEngine.Application.persistentDataPath+" camera="+CarpetCleaning.StoreShotApi.CameraJson()+" shells="+cr.shellCount;
