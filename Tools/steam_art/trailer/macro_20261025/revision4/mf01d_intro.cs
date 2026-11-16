if(System.IO.File.Exists("C:/UnityProjects/CarpetCleaning/SteamStore/_work/clips/mf01d_intro/seq.json"))throw new System.InvalidOperationException("Take already exists");
var begin=CarpetCleaning.StoreGifApi.SeqBegin("mf01d_intro",1920,1080,0,0,0,0,false,1,true,2560,1440,60,true);

var ad=System.AppDomain.CurrentDomain;
var old=ad.GetData("mfCamHook") as System.Action<UnityEngine.Rendering.ScriptableRenderContext,System.Collections.Generic.List<UnityEngine.Camera>>;
if(old!=null)UnityEngine.Rendering.RenderPipelineManager.beginContextRendering-=old;
var cam=UnityEngine.Camera.main;
var steps=typeof(CarpetCleaning.StoreGifApi).GetField("s_steps",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
var K=new float[][]{new float[]{0.00000f,0.03000f,0.01900f,-0.23500f,0.03000f,0.00400f,-0.21000f,50.00000f},new float[]{2.00000f,0.03000f,0.01900f,-0.22600f,0.03000f,0.00400f,-0.20100f,50.00000f},new float[]{5.05000f,0.00000f,1.70000f,-1.70000f,0.00000f,0.01000f,0.00000f,43.00000f},new float[]{5.75000f,0.00000f,1.70000f,-1.70000f,0.00000f,0.01000f,0.00000f,43.00000f},new float[]{10.45000f,0.06000f,0.10000f,-0.43000f,0.00000f,0.01000f,-0.21000f,43.00000f},new float[]{10.75000f,0.06000f,0.10000f,-0.43000f,0.00000f,0.01000f,-0.21000f,43.00000f}};
var cameraLog=new System.Text.StringBuilder("frame,t,px,py,pz,tx,ty,tz,fov\n");ad.SetData("mfCameraLog",cameraLog);var lastCameraFrame=new int[]{-1};
System.Action pose=()=>{
float t=(int)steps.GetValue(null)/60f;int i=0;while(i<K.Length-2 && t>K[i+1][0])i++;
float s=UnityEngine.Mathf.Clamp01((t-K[i][0])/UnityEngine.Mathf.Max(.0001f,K[i+1][0]-K[i][0]));s=s*s*(3-2*s);
var a=K[i];var b=K[i+1];var ta=new UnityEngine.Vector3(a[4],a[5],a[6]);var tb=new UnityEngine.Vector3(b[4],b[5],b[6]);
var pa=new UnityEngine.Vector3(a[1],a[2],a[3]);var pb=new UnityEngine.Vector3(b[1],b[2],b[3]);var target=UnityEngine.Vector3.Lerp(ta,tb,s);
var offset=UnityEngine.Vector3.Slerp((pa-ta).normalized,(pb-tb).normalized,s)*UnityEngine.Mathf.Exp(UnityEngine.Mathf.Lerp(UnityEngine.Mathf.Log((pa-ta).magnitude),UnityEngine.Mathf.Log((pb-tb).magnitude),s));
var pos=target+offset;var fwd=(target-pos).normalized;
float upW=UnityEngine.Mathf.SmoothStep(0,1,UnityEngine.Mathf.InverseLerp(.9f,.999f,UnityEngine.Mathf.Abs(fwd.y)));
cam.transform.SetPositionAndRotation(pos,UnityEngine.Quaternion.LookRotation(fwd,UnityEngine.Vector3.Slerp(UnityEngine.Vector3.up,UnityEngine.Vector3.forward,upW)));
cam.fieldOfView=UnityEngine.Mathf.Lerp(a[7],b[7],s);cam.nearClipPlane=.0003f;
int frame=(int)steps.GetValue(null);if(frame>lastCameraFrame[0]){lastCameraFrame[0]=frame;var ci=System.Globalization.CultureInfo.InvariantCulture;cameraLog.AppendLine(string.Join(",",new string[]{frame.ToString(ci),t.ToString("F6",ci),pos.x.ToString("F9",ci),pos.y.ToString("F9",ci),pos.z.ToString("F9",ci),target.x.ToString("F9",ci),target.y.ToString("F9",ci),target.z.ToString("F9",ci),cam.fieldOfView.ToString("F6",ci)}));}
};
System.Action<UnityEngine.Rendering.ScriptableRenderContext,System.Collections.Generic.List<UnityEngine.Camera>> hook=(ctx,cams)=>pose();
UnityEngine.Rendering.RenderPipelineManager.beginContextRendering+=hook;ad.SetData("mfCamHook",hook);pose();
var targetDirt=(UnityEngine.Color[])ad.GetData("mfDirtTarget");var dirtPixels=(UnityEngine.Color[])ad.GetData("mfDirtPixels");var dirtTex=(UnityEngine.Texture2D)ad.GetData("mfDirtTex");var dirtRt=CarpetCleaning.CarpetDebugApi.Sim.Dirt;
var cr=UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.CarpetRenderer>();var mf=cr.Shells.GetComponent<UnityEngine.MeshFilter>();
var nearMesh=(UnityEngine.Mesh)ad.GetData("mfCaptureMesh");var farMesh=(UnityEngine.Mesh)ad.GetData("mfCaptureMesh96");mf.sharedMesh=nearMesh;
var log=new System.Text.StringBuilder();ad.SetData("mfDirtRampLog",log);
CarpetCleaning.StoreGifApi.Each(10.75f,unused=>{
float t=((int)steps.GetValue(null)+1)/60f;
float u=UnityEngine.Mathf.InverseLerp(5.95f,10.25f,t);float soil=u*u*(3-2*u);
for(int j=0;j<dirtPixels.Length;j++)dirtPixels[j]=targetDirt[j]*soil;
dirtTex.SetPixels(dirtPixels);dirtTex.Apply(false,false);UnityEngine.Graphics.Blit(dirtTex,dirtRt);
var mesh=t<3.95f?nearMesh:farMesh;if(mf.sharedMesh!=mesh)mf.sharedMesh=mesh;
log.Append(((int)steps.GetValue(null)+1).ToString(System.Globalization.CultureInfo.InvariantCulture));log.Append(",");log.Append(t.ToString("F6",System.Globalization.CultureInfo.InvariantCulture));log.Append(",");log.AppendLine(soil.ToString("F9",System.Globalization.CultureInfo.InvariantCulture));
});
return begin+" | "+CarpetCleaning.StoreGifApi.Run(1800);
