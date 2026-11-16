var E = CarpetCleaning.EconomyDebugApi.Eco;
CarpetCleaning.EconomyDebugApi.Grant(1960 - E.Data.money);
int[] pw = {3,4,2,2,3,2,3,4,1,1};
int[] ar = {2,3,1,2,2,2,2,3,1,1};
for (int t=0;t<10;t++){ CarpetCleaning.EconomyDebugApi.SetLevel(t,0,pw[t]); CarpetCleaning.EconomyDebugApi.SetLevel(t,1,ar[t]); }
CarpetCleaning.EconomyDebugApi.SetOwned(0, true);
CarpetCleaning.EconomyDebugApi.SetOwned(1, true);
CarpetCleaning.EconomyDebugApi.ShopOpen(false);
CarpetCleaning.CarpetDebugApi.SetHighlight(0, false, true);
CarpetCleaning.CarpetDebugApi.SetHighlight(1, false, true);
CarpetCleaning.CarpetDebugApi.UnpinToolHead();
var sim = CarpetCleaning.CarpetDebugApi.Sim;
sim.UiPaused = false;
UnityEditor.EditorApplication.isPaused = false;
return "paused=" + UnityEditor.EditorApplication.isPaused + " playing=" + UnityEditor.EditorApplication.isPlaying + " uiPaused=" + sim.UiPaused + " " + CarpetCleaning.CarpetDebugApi.HighlightJson().Substring(0,120) + " " + CarpetCleaning.CarpetDebugApi.ToolHeadJson() + " " + CarpetCleaning.EconomyDebugApi.EconomyJson().Substring(0,60);
