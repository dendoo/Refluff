UnityEditor.EditorApplication.isPaused=true;
var t=CarpetCleaning.CarpetDebugApi.Tools;
t.ScriptedPointer=true;t.ScriptedUv=new UnityEngine.Vector2(-5,-5);t.ScriptedDown=false;t.ScriptedRight=false;
CarpetCleaning.CarpetDebugApi.SetTool(2);
CarpetCleaning.CarpetDebugApi.PinToolHead(2,0.84000f,0.12000f,true);
CarpetCleaning.CarpetDebugApi.Sim.RefreshStatsSync();
CarpetCleaning.CarpetDebugApi.Sim.Paused=true;
for(int i=0;i<35;i++)UnityEditor.EditorApplication.Step();
CarpetCleaning.CarpetDebugApi.Sim.Paused=false;

var cp=new UnityEngine.Vector3(0.43000f,0.22000f,-0.96000f);var ce=UnityEngine.Quaternion.LookRotation(new UnityEngine.Vector3(0.43000f,0.01200f,-0.65000f)-cp).eulerAngles;
var sr=CarpetCleaning.StoreShotApi.RenderView("shots/r06_salor_hose_scene.png",3840,2160,cp.x,cp.y,cp.z,ce.x,ce.y,ce.z,52.00000f);
var hr=CarpetCleaning.EconomyDebugApi.CaptureHud("C:/UnityProjects/CarpetCleaning/SteamStore/_work/shots/r06_salor_hose_hud.png",2560,1440);
return sr+hr+" stats="+CarpetCleaning.CarpetDebugApi.StatsJson()+" floor="+CarpetCleaning.CarpetDebugApi.FloorStatsJson();
