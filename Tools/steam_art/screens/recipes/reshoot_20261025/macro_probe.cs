CarpetCleaning.EconomyDebugApi.ShopOpen(false);
var poses=new UnityEngine.Vector3[]{new UnityEngine.Vector3(-.12f,.22f,-.91f),new UnityEngine.Vector3(-.10f,.13f,-.83f),new UnityEngine.Vector3(.55f,.12f,-.69f)};
var targets=new UnityEngine.Vector3[]{new UnityEngine.Vector3(-.12f,.012f,-.56f),new UnityEngine.Vector3(-.10f,.012f,-.64f),new UnityEngine.Vector3(.43f,.012f,-.55f)};
string r="";
for(int i=0;i<poses.Length;i++){var p=poses[i];var e=UnityEngine.Quaternion.LookRotation(targets[i]-p).eulerAngles;r+=CarpetCleaning.StoreShotApi.RenderView("screens/reshoot_20261025/macro_probe_"+i+".png",1920,1080,p.x,p.y,p.z,e.x,e.y,e.z,50);}
return r;
