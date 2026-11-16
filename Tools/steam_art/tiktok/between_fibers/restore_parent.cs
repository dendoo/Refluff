CarpetCleaning.StoreGifApi.SeqAbort();
var ad=System.AppDomain.CurrentDomain;
var hook=ad.GetData("bfCamHook") as System.Action<UnityEngine.Rendering.ScriptableRenderContext,System.Collections.Generic.List<UnityEngine.Camera>>;
if(hook!=null)UnityEngine.Rendering.RenderPipelineManager.beginContextRendering-=hook;
ad.SetData("bfCamHook",null);
var a=ad.GetData("bfOriginal") as object[];
if(a!=null){
 var cam=UnityEngine.Camera.main;cam.transform.SetPositionAndRotation((UnityEngine.Vector3)a[0],(UnityEngine.Quaternion)a[1]);cam.fieldOfView=(float)a[2];cam.nearClipPlane=(float)a[3];
 var light=UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();light.transform.rotation=(UnityEngine.Quaternion)a[4];light.intensity=(float)a[5];light.colorTemperature=(float)a[6];light.shadows=(UnityEngine.LightShadows)a[7];
 UnityEngine.RenderSettings.ambientSkyColor=(UnityEngine.Color)a[8];
 var cr=UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.CarpetRenderer>();cr.Shells.GetComponent<UnityEngine.MeshFilter>().sharedMesh=(UnityEngine.Mesh)a[9];
 UnityEditor.EditorApplication.isPaused=(bool)a[10];
}
var mesh=ad.GetData("bfCaptureMesh") as UnityEngine.Mesh;if(mesh!=null)UnityEngine.Object.Destroy(mesh);
ad.SetData("bfCaptureMesh",null);ad.SetData("bfOriginal",null);
return "restored original camera/light/mesh";
