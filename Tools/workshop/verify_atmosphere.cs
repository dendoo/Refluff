// eval_file three times: camera inside the volume in both directions, then opaque foreground rejection A/B.
if(!UnityEngine.Application.isPlaying)return "Enter Play first";
var p=UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.FirstPersonPlayer>();
var atmosphere=UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.WorkshopAtmosphere>();
int index=UnityEditor.SessionState.GetInt("WorkshopAtmosphereQA",0)%3;
var position=p.transform.position;float yaw=p.Yaw,pitch=p.Pitch;bool paused=UnityEditor.EditorApplication.isPaused;
bool simPaused=p.sim.Paused,input=p.tools.InputEnabled;var oldScale=UnityEngine.Time.timeScale;
UnityEngine.GameObject blocker=null;UnityEngine.Material blockerMat=null;
string result="";
try{
    UnityEditor.EditorApplication.isPaused=true;p.sim.Paused=true;p.tools.InputEnabled=false;atmosphere.SetPreviewTime(0);
    if(index<2){
        p.DebugPose(-2.086f,-4.325f,index==0?150f:-30f,index==0?-38f:38f);
        UnityEditor.EditorApplication.Step();
        result=CarpetCleaning.StoreShotApi.RenderScene("C:/UnityProjects/CarpetCleaning/reports/workshop_20261116/inside_volume_"+index+".png",1920,1080);
    }else{
        p.DebugPose(.3f,-1f,82f,2f);UnityEditor.EditorApplication.Step();
        blocker=UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube);blocker.name="TemporaryAtmosphereQA";
        var cam=p.viewCamera;blocker.transform.SetPositionAndRotation(cam.transform.position+cam.transform.forward*.35f,cam.transform.rotation);
        blocker.transform.localScale=new UnityEngine.Vector3(1.4f,1.0f,.02f);
        blockerMat=new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Unlit"));blockerMat.SetColor("_BaseColor",new UnityEngine.Color(.2f,.09f,.06f));
        blocker.GetComponent<UnityEngine.MeshRenderer>().sharedMaterial=blockerMat;
        UnityEngine.Physics.SyncTransforms();UnityEditor.EditorApplication.Step();
        // Warm the new renderer/material and this capture target size before comparing the two states.
        CarpetCleaning.StoreShotApi.RenderScene("C:/UnityProjects/CarpetCleaning/reports/workshop_20261116/blocked_warmup.png",960,540);
        UnityEditor.EditorApplication.Step();
        result=CarpetCleaning.StoreShotApi.RenderScene("C:/UnityProjects/CarpetCleaning/reports/workshop_20261116/blocked_rays_on.png",960,540);
        foreach(var r in atmosphere.shafts)r.enabled=false;
        CarpetCleaning.StoreShotApi.RenderScene("C:/UnityProjects/CarpetCleaning/reports/workshop_20261116/blocked_rays_off.png",960,540);
    }
    UnityEditor.SessionState.SetInt("WorkshopAtmosphereQA",index+1);
}finally{
    foreach(var r in atmosphere.shafts)r.enabled=true;
    if(blocker!=null)UnityEngine.Object.DestroyImmediate(blocker);if(blockerMat!=null)UnityEngine.Object.DestroyImmediate(blockerMat);
    p.DebugPose(position.x,position.z,yaw,pitch);p.sim.Paused=simPaused;p.tools.InputEnabled=input;
    atmosphere.ResumeTime();UnityEngine.Time.timeScale=oldScale;UnityEditor.EditorApplication.isPaused=paused;
}
return result;
