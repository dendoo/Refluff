var E = CarpetCleaning.EconomyDebugApi.Eco;
int[] pw = {1,2,1,1,2,0,1,0,0,0};
int[] ar = {1,1,0,1,1,1,1,0,0,0};
for (int t=0;t<10;t++){ CarpetCleaning.EconomyDebugApi.SetLevel(t,0,pw[t]); CarpetCleaning.EconomyDebugApi.SetLevel(t,1,ar[t]); }
CarpetCleaning.EconomyDebugApi.LiveTuning(false);
CarpetCleaning.EconomyDebugApi.Grant(1840 - E.Data.money);
E.Data.carpetsDelivered = 6;
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
for (int i = 0; i < 6 && !CarpetCleaning.CarpetDebugApi.HandoverJson().Contains("rug_transylvanian"); i++) CarpetCleaning.CarpetDebugApi.NewCarpetNow();
CarpetCleaning.CarpetDebugApi.HandoverSnap();
CarpetCleaning.CarpetDebugApi.Reset(14, 0f);
CarpetCleaning.CarpetDebugApi.SetTool(7);
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 30; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = false;
return CarpetCleaning.CarpetDebugApi.HandoverJson() + " money=" + E.Data.money + " carpets=" + E.Data.carpetsDelivered;
