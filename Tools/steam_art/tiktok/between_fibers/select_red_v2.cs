var ad=System.AppDomain.CurrentDomain;
var cr=UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.CarpetRenderer>();
var mat=cr.ShellsRenderer.sharedMaterial;
if(ad.GetData("bfRedOriginal")==null)ad.SetData("bfRedOriginal",new object[]{mat,mat.GetTexture("_PictureTex"),mat.GetVector("_PicOffset"),mat.GetVector("_PicScale")});
var tex=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>("Assets/CarpetCleaning/Textures/rug_transylvanian_medallion.jpg");
if(tex==null)throw new System.InvalidOperationException("Red rug texture missing");
CarpetCleaning.CarpetHandover.PictureFit(tex,out var offset,out var scale);
mat.SetTexture("_PictureTex",tex);mat.SetVector("_PicOffset",offset);mat.SetVector("_PicScale",scale);
var cam=UnityEngine.Camera.main;var p=cam.transform.position;var r=cam.transform.rotation;float f=cam.fieldOfView;
try{
 var pos=new UnityEngine.Vector3(0,2.8f,-2.3f);cam.transform.SetPositionAndRotation(pos,UnityEngine.Quaternion.LookRotation(-pos));cam.fieldOfView=45;
 return tex.name+" | "+CarpetCleaning.StoreShotApi.RenderScene("between_fibers/red_v2_full.png",540,960);
}finally{cam.transform.SetPositionAndRotation(p,r);cam.fieldOfView=f;}
