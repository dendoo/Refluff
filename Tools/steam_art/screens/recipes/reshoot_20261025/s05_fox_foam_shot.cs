UnityEditor.EditorApplication.isPaused=true;
var t=CarpetCleaning.CarpetDebugApi.Tools;
t.ScriptedPointer=true;t.ScriptedUv=new UnityEngine.Vector2(-5,-5);t.ScriptedDown=false;t.ScriptedRight=false;
CarpetCleaning.CarpetDebugApi.SetTool(3);
CarpetCleaning.CarpetDebugApi.PinToolHead(3,0.78000f,0.37000f,true);
CarpetCleaning.CarpetDebugApi.Sim.RefreshStatsSync();
CarpetCleaning.CarpetDebugApi.Sim.Paused=true;
for(int i=0;i<35;i++)UnityEditor.EditorApplication.Step();
CarpetCleaning.CarpetDebugApi.Sim.Paused=false;

return CarpetCleaning.StoreShotApi.Shot("s05_fox_foam")+" stats="+CarpetCleaning.CarpetDebugApi.StatsJson()+" floor="+CarpetCleaning.CarpetDebugApi.FloorStatsJson();
