UnityEditor.EditorApplication.isPaused=true;
var t=CarpetCleaning.CarpetDebugApi.Tools;
t.ScriptedPointer=true;t.ScriptedUv=new UnityEngine.Vector2(-5,-5);t.ScriptedDown=false;t.ScriptedRight=false;
CarpetCleaning.CarpetDebugApi.SetTool(6);
CarpetCleaning.CarpetDebugApi.PinToolHead(6,0.39000f,0.24000f,true);
CarpetCleaning.CarpetDebugApi.Sim.RefreshStatsSync();
CarpetCleaning.CarpetDebugApi.Sim.Paused=true;
for(int i=0;i<35;i++)UnityEditor.EditorApplication.Step();
CarpetCleaning.CarpetDebugApi.Sim.Paused=false;

var cp=new UnityEngine.Vector3(-0.12000f,0.27000f,-1.03500f);var ce=UnityEngine.Quaternion.LookRotation(new UnityEngine.Vector3(-0.12000f,0.01200f,-0.64000f)-cp).eulerAngles;
var sr=CarpetCleaning.StoreShotApi.RenderView("shots/r02_anatolian_extract_scene.png",3840,2160,cp.x,cp.y,cp.z,ce.x,ce.y,ce.z,51.00000f);
var hr=CarpetCleaning.EconomyDebugApi.CaptureHud("C:/UnityProjects/CarpetCleaning/SteamStore/_work/shots/r02_anatolian_extract_hud.png",2560,1440);
return sr+hr+" stats="+CarpetCleaning.CarpetDebugApi.StatsJson()+" floor="+CarpetCleaning.CarpetDebugApi.FloorStatsJson();
