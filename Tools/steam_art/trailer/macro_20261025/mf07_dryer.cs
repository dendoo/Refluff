
if(System.IO.File.Exists("C:/UnityProjects/CarpetCleaning/SteamStore/_work/clips/mf07_dryer/seq.json"))throw new System.InvalidOperationException("Take already exists");
var begin=CarpetCleaning.StoreGifApi.SeqBegin("mf07_dryer",1920,1080,0,0,0,0,false,1,true,2560,1440,60,true);

var ad=System.AppDomain.CurrentDomain;
var old=ad.GetData("mfCamHook") as System.Action<UnityEngine.Rendering.ScriptableRenderContext,System.Collections.Generic.List<UnityEngine.Camera>>;
if(old!=null)UnityEngine.Rendering.RenderPipelineManager.beginContextRendering-=old;
var cam=UnityEngine.Camera.main;
var steps=typeof(CarpetCleaning.StoreGifApi).GetField("s_steps",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
var K=new float[][]{new float[]{0.00000f,0.04500f,0.07500f,-0.40000f,0.00000f,0.00800f,-0.21000f,43.00000f},new float[]{9.00000f,0.04500f,0.07500f,-0.40000f,0.00000f,0.00800f,-0.21000f,43.00000f}};
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
};
System.Action<UnityEngine.Rendering.ScriptableRenderContext,System.Collections.Generic.List<UnityEngine.Camera>> hook=(ctx,cams)=>pose();
UnityEngine.Rendering.RenderPipelineManager.beginContextRendering+=hook;ad.SetData("mfCamHook",hook);pose();CarpetCleaning.StoreGifApi.Tool(7);CarpetCleaning.StoreGifApi.Move(.23f,.36f,.25f,.36f,.2f,false);CarpetCleaning.StoreGifApi.Move(.25f,.36f,.75f,.36f,4.8f,true);CarpetCleaning.StoreGifApi.Move(.75f,.36f,.86f,.36f,.3f,false);CarpetCleaning.StoreGifApi.Away(3.7f);
return begin+" | "+CarpetCleaning.StoreGifApi.Run(1800);
