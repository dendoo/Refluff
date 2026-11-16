// eval_file; each call captures the next player-height review. Original play/input/camera state is restored.
if(!UnityEngine.Application.isPlaying) return "Enter Play first";
var p=UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.FirstPersonPlayer>();
var tools=p.tools; var sim=p.sim; var atmosphere=UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.WorkshopAtmosphere>();
int index=UnityEditor.SessionState.GetInt("WorkshopReview",0)%7;
string[] names={"overview","south_light","workbench","wash_station","spawn","rays_t0","rays_t8"};
float[,] poses={{-2.6f,-4.4f,22f,13f},{2.5f,2.6f,208f,4f},{-2.5f,-1.8f,310f,6f},{.3f,2.1f,41f,7f},{0f,-3.8f,0f,28f},{.3f,-1.0f,82f,2f},{.3f,-1.0f,82f,2f}};
var old=p.transform.position;float yaw=p.Yaw,pitch=p.Pitch;bool paused=UnityEditor.EditorApplication.isPaused;
bool sp=sim.Paused,ui=sim.UiPaused,input=tools.InputEnabled,scripted=tools.ScriptedPointer,down=tools.ScriptedDown;
string result="";
try
{
    UnityEditor.EditorApplication.isPaused=true;
    sim.Paused=true;sim.UiPaused=false;tools.InputEnabled=false;tools.ScriptedPointer=false;tools.ScriptedDown=false;
    p.DebugPose(poses[index,0],poses[index,1],poses[index,2],poses[index,3]);
    if(atmosphere!=null) atmosphere.SetPreviewTime(index==6?8f:0f);
    for(int i=0;i<2;i++)UnityEditor.EditorApplication.Step();
    string path="C:/UnityProjects/CarpetCleaning/reports/workshop_20261116/"+names[index]+".png";
    result=CarpetCleaning.StoreShotApi.RenderScene(path,1920,1080);
    if(index==0) CarpetCleaning.EconomyDebugApi.CaptureHud("C:/UnityProjects/CarpetCleaning/reports/workshop_20261116/overview_hud.png",2560,1440);
    UnityEditor.SessionState.SetInt("WorkshopReview",index+1);
}
finally
{
    p.DebugPose(old.x,old.z,yaw,pitch);sim.Paused=sp;sim.UiPaused=ui;
    tools.InputEnabled=input;tools.ScriptedPointer=scripted;tools.ScriptedDown=down;
    if(atmosphere!=null) atmosphere.ResumeTime();
    UnityEditor.EditorApplication.isPaused=paused;
}
return result;
