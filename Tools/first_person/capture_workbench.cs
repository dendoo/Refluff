var p=UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.FirstPersonPlayer>();
var t=p.tools;
var h=UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.EconomyHud>();
var fp=UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.FirstPersonHud>();
var bench=UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.UpgradeWorkbench>();
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var focus=p.GetType().GetField("_focused",flags);
var wait=p.GetType().GetField("_waitForButtonsUp",flags);
bool originalFocus=(bool)focus.GetValue(p),originalWait=(bool)wait.GetValue(p);
bool originalPaused=UnityEditor.EditorApplication.isPaused;
string language=CarpetCleaning.Loc.Code;
try
{
    CarpetCleaning.Loc.SetLanguage("ru",false);
    t.sim.Paused=false;t.sim.UiPaused=false;t.UiModal=false;
    UnityEditor.EditorApplication.isPaused=true;
    p.DebugPose(-1.4f,.88f,270f,20f);
    t.SetTool(CarpetCleaning.ToolId.FoamSprayer);
    for(int i=0;i<4;i++) UnityEditor.EditorApplication.Step();
    focus.SetValue(p,true);wait.SetValue(p,false);
    UnityEngine.Cursor.lockState=UnityEngine.CursorLockMode.Locked;
    t.SendMessage("Update");fp.SendMessage("Update");
    p.viewCamera.GetComponent<CarpetCleaning.FirstPersonToolView>().SendMessage("LateUpdate");
    string first=CarpetCleaning.EconomyDebugApi.CaptureHud("Screenshots/first_person_workbench.png",2560,1440);
    bool opened=bench.TryInteract();
    for(int i=0;i<20;i++) UnityEditor.EditorApplication.Step();
    string second=CarpetCleaning.EconomyDebugApi.CaptureHud("Screenshots/first_person_shop.png",2560,1440);
    return "opened="+opened+"\n"+first+"\n"+second+"\n"+bench.DebugJson()+"\n"+CarpetCleaning.CarpetDebugApi.LocOverflow();
}
finally
{
    h.SetShopOpen(false); focus.SetValue(p,originalFocus);wait.SetValue(p,originalWait);
    CarpetCleaning.Loc.SetLanguage(language,false);
    UnityEngine.Cursor.lockState=UnityEngine.CursorLockMode.None;
    UnityEditor.EditorApplication.isPaused=originalPaused;
}
