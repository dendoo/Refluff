var ad=System.AppDomain.CurrentDomain;
if(System.IO.File.Exists("C:/UnityProjects/CarpetCleaning/SteamStore/_work/clips/between_fibers_20261023_v2_red/seq.json"))
 throw new System.InvalidOperationException("Original take exists. Choose a new take name and folder before recording again.");
var old=ad.GetData("bfCamHook") as System.Action<UnityEngine.Rendering.ScriptableRenderContext,System.Collections.Generic.List<UnityEngine.Camera>>;
if(old!=null)UnityEngine.Rendering.RenderPipelineManager.beginContextRendering-=old;
var cam=UnityEngine.Camera.main;
string begin=CarpetCleaning.StoreGifApi.SeqBegin("between_fibers_20261023_v2_red",1080,1920,0,0,0,0,false,1,true,2560,1440,60,true);
var steps=typeof(CarpetCleaning.StoreGifApi).GetField("s_steps",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
var K=new float[][] {
 new float[]{0,.03f,.019f,-.235f,.03f,.004f,-.210f,50},
 new float[]{2.6f,.03f,.019f,-.235f,.03f,.004f,-.210f,50},
 new float[]{6.2f,.10f,.24f,-.56f,0,.01f,-.075f,45},
 new float[]{9.5f,.10f,.24f,-.56f,0,.01f,-.075f,45},
 new float[]{11.35f,0,2.8f,-2.3f,0,0,0,45},
 new float[]{12,0,2.8f,-2.3f,0,0,0,45}
};
System.Action pose=()=>{
 float t=(int)steps.GetValue(null)/60f;int i=0;while(i<K.Length-2 && t>K[i+1][0])i++;
 float s=UnityEngine.Mathf.Clamp01((t-K[i][0])/(K[i+1][0]-K[i][0]));s=s*s*(3-2*s);
 var a=K[i];var b=K[i+1];
 var ta=new UnityEngine.Vector3(a[4],a[5],a[6]);var tb=new UnityEngine.Vector3(b[4],b[5],b[6]);
 var pa=new UnityEngine.Vector3(a[1],a[2],a[3]);var pb=new UnityEngine.Vector3(b[1],b[2],b[3]);
 var target=UnityEngine.Vector3.Lerp(ta,tb,s);
 var offset=UnityEngine.Vector3.Slerp((pa-ta).normalized,(pb-tb).normalized,s)*UnityEngine.Mathf.Exp(UnityEngine.Mathf.Lerp(UnityEngine.Mathf.Log((pa-ta).magnitude),UnityEngine.Mathf.Log((pb-tb).magnitude),s));
 var pos=target+offset;var fwd=(target-pos).normalized;
 cam.transform.SetPositionAndRotation(pos,UnityEngine.Quaternion.LookRotation(fwd,UnityEngine.Vector3.up));
 cam.fieldOfView=UnityEngine.Mathf.Lerp(a[7],b[7],s);cam.nearClipPlane=.0003f;
};
System.Action<UnityEngine.Rendering.ScriptableRenderContext,System.Collections.Generic.List<UnityEngine.Camera>> hook=(ctx,cams)=>pose();
UnityEngine.Rendering.RenderPipelineManager.beginContextRendering+=hook;ad.SetData("bfCamHook",hook);pose();
CarpetCleaning.StoreGifApi.Away(6.2f);
CarpetCleaning.StoreGifApi.Tool(1);
CarpetCleaning.StoreGifApi.Call(()=>CarpetCleaning.CarpetDebugApi.Tools.SnapHeadingOnNextMove());
CarpetCleaning.StoreGifApi.Move(.66f,.45f,.64f,.45f,.2f,false);
CarpetCleaning.StoreGifApi.Move(.64f,.45f,.36f,.45f,2f,true);
CarpetCleaning.StoreGifApi.Away(3.6f);
return begin+" | "+CarpetCleaning.StoreGifApi.Run(2400);

