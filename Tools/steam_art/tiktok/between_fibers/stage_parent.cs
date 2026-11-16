var ad=System.AppDomain.CurrentDomain;
var cam=UnityEngine.Camera.main;
var light=UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
var cr=UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.CarpetRenderer>();
var mf=cr.Shells.GetComponent<UnityEngine.MeshFilter>();
if(ad.GetData("bfOriginal")==null) ad.SetData("bfOriginal", new object[] {
 cam.transform.position,cam.transform.rotation,cam.fieldOfView,cam.nearClipPlane,
 light.transform.rotation,light.intensity,light.colorTemperature,light.shadows,
 UnityEngine.RenderSettings.ambientSkyColor,mf.sharedMesh, UnityEditor.EditorApplication.isPaused
});
UnityEditor.EditorApplication.isPaused=true;
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
CarpetCleaning.CarpetDebugApi.Reset(394,0);
var tools=CarpetCleaning.CarpetDebugApi.Tools;
tools.ScriptedPointer=true; tools.ScriptedUv=new UnityEngine.Vector2(-5,-5); tools.ScriptedDown=false; tools.ScriptedRight=false;
UnityEditor.EditorApplication.Step();
var sz=CarpetCleaning.CarpetSimulation.CarpetSizeM;
var mesh=CarpetCleaning.ShellMeshBuilder.BuildShells(sz.x,sz.y,384,cr.shellSubdivX,cr.shellSubdivZ,cr.pileHeightM);
mesh.name="BetweenFibersCapture384"; mesh.hideFlags=UnityEngine.HideFlags.HideAndDontSave;
mf.sharedMesh=mesh; ad.SetData("bfCaptureMesh",mesh);
cam.nearClipPlane=.0003f;
return "staged clean carpet, 384 capture layers; original runtime view saved";
