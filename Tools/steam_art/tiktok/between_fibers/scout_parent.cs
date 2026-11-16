var cam = UnityEngine.Camera.main;
var p0 = cam.transform.position; var r0 = cam.transform.rotation;
float f0 = cam.fieldOfView, n0 = cam.nearClipPlane;
bool pause0 = UnityEditor.EditorApplication.isPaused;
var tools = CarpetCleaning.CarpetDebugApi.Tools;
bool sp0 = tools.ScriptedPointer; var uv0 = tools.ScriptedUv;
var cr = UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.CarpetRenderer>();
var mf=cr.Shells.GetComponent<UnityEngine.MeshFilter>(); var mesh0=mf.sharedMesh;
UnityEngine.Mesh captureMesh=null;
try {
 UnityEditor.EditorApplication.isPaused = true;
 tools.ScriptedPointer = true; tools.ScriptedUv = new UnityEngine.Vector2(-5,-5); tools.ScriptedDown=false;
 CarpetCleaning.CarpetDebugApi.UnpinToolHead();
 CarpetCleaning.CarpetDebugApi.Reset(394, 0);
 UnityEditor.EditorApplication.Step();
 var sz=CarpetCleaning.CarpetSimulation.CarpetSizeM;
 captureMesh=CarpetCleaning.ShellMeshBuilder.BuildShells(sz.x,sz.y,384,cr.shellSubdivX,cr.shellSubdivZ,cr.pileHeightM);
 captureMesh.hideFlags=UnityEngine.HideFlags.HideAndDontSave; mf.sharedMesh=captureMesh;
 cam.nearClipPlane=.0003f;
 var poses = new [] {
  new [] {.03f,.019f,-.235f, .03f,.004f,-.210f,50f},
  new [] {.10f,.24f,-.56f, 0f,.01f,-.075f,45f},
  new [] {0f,2.8f,-2.3f, 0f,0f,0f,45f}
 };
 string result="";
 for(int i=0;i<poses.Length;i++) {
  var v=poses[i]; var pos=new UnityEngine.Vector3(v[0],v[1],v[2]); var target=new UnityEngine.Vector3(v[3],v[4],v[5]);
  cam.transform.SetPositionAndRotation(pos,UnityEngine.Quaternion.LookRotation(target-pos)); cam.fieldOfView=v[6];
  result += CarpetCleaning.StoreShotApi.RenderScene("between_fibers/scout_dense_"+i+".png",540,960)+"\n";
 }
 return result;
} finally {
 cam.transform.SetPositionAndRotation(p0,r0); cam.fieldOfView=f0; cam.nearClipPlane=n0;
 tools.ScriptedPointer=sp0; tools.ScriptedUv=uv0;
 mf.sharedMesh=mesh0; if(captureMesh!=null) UnityEngine.Object.Destroy(captureMesh);
 UnityEditor.EditorApplication.isPaused=pause0;
}
