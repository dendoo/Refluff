// Write one ToolId (0..9) to capture_tool.txt; captures the real FPS input/view path unfocused.
var p = UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.FirstPersonPlayer>();
var t = p.tools;
var held = p.viewCamera.GetComponent<CarpetCleaning.FirstPersonToolView>();
var hud = UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.FirstPersonHud>();
var mouse = UnityEngine.InputSystem.Mouse.current;
var mouseState = new UnityEngine.InputSystem.LowLevel.MouseState { position=mouse.position.ReadValue() };
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var focused = typeof(CarpetCleaning.FirstPersonPlayer).GetField("_focused", flags);
var waiting = typeof(CarpetCleaning.FirstPersonPlayer).GetField("_waitForButtonsUp", flags);
int id = int.Parse(System.IO.File.ReadAllText("Tools/first_person/capture_tool.txt"));
bool oldFocus=(bool)focused.GetValue(p), oldWait=(bool)waiting.GetValue(p);
bool oldPause=UnityEditor.EditorApplication.isPaused;
var oldLock=UnityEngine.Cursor.lockState;
string oldLang=CarpetCleaning.Loc.Code;
try
{
    UnityEditor.EditorApplication.isPaused=true;
    t.sim.Paused=false; t.sim.UiPaused=false; t.UiModal=false; t.Suspended=false;
    t.InputEnabled=true; t.ScriptedPointer=false;
    CarpetCleaning.Loc.SetLanguage("ru",false);
    UnityEngine.InputSystem.LowLevel.InputState.Change(mouse,mouseState);
    p.DebugPose(id>=8 ? 1.15f : 0f,-1.25f,0f,58f);
    t.SetTool((CarpetCleaning.ToolId)id);
    UnityEditor.EditorApplication.Step();
    focused.SetValue(p,true); waiting.SetValue(p,false); UnityEngine.Cursor.lockState=UnityEngine.CursorLockMode.Locked;
    t.SendMessage("Update"); // clear the modal-release latch before pressing
    UnityEngine.InputSystem.LowLevel.InputState.Change(mouse,mouseState.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left,true));
    for(int i=0;i<16;i++) { t.SendMessage("Update"); CarpetCleaning.CarpetDebugApi.StepAll(1f/60f); }
    held.SendMessage("LateUpdate"); hud.SendMessage("Update");
    string file="Screenshots/first_person_tool_"+id+".png";
    string capture=CarpetCleaning.EconomyDebugApi.CaptureHud(file,2560,1440);
    return p.DiagnosticsJson()+"\n"+capture;
}
finally
{
    UnityEngine.InputSystem.LowLevel.InputState.Change(mouse,mouseState);
    t.SendMessage("Update");
    focused.SetValue(p,oldFocus); waiting.SetValue(p,oldWait);
    UnityEngine.Cursor.lockState=oldLock;
    CarpetCleaning.Loc.SetLanguage(oldLang,false);
    UnityEditor.EditorApplication.isPaused=oldPause;
}
