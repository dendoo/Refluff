UnityEditor.EditorApplication.isPaused=true;
var t=CarpetCleaning.CarpetDebugApi.Tools;
t.ScriptedPointer=true;t.ScriptedUv=new UnityEngine.Vector2(-5,-5);t.ScriptedDown=false;t.ScriptedRight=false;
CarpetCleaning.CarpetDebugApi.SetTool(2);
CarpetCleaning.CarpetDebugApi.PinToolHead(2,0.62000f,0.39000f,true);
CarpetCleaning.CarpetDebugApi.Sim.RefreshStatsSync();
CarpetCleaning.CarpetDebugApi.Sim.Paused=true;
for(int i=0;i<35;i++)UnityEditor.EditorApplication.Step();
CarpetCleaning.CarpetDebugApi.Sim.Paused=false;

return CarpetCleaning.StoreShotApi.Shot("s06_salor_hose")+" stats="+CarpetCleaning.CarpetDebugApi.StatsJson()+" floor="+CarpetCleaning.CarpetDebugApi.FloorStatsJson();
