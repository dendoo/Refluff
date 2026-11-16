// Fresh Play after the final asset write. One real FPS tool per call, using live effective upgrades.
// Output stays outside Assets. Achievements/economy are unsubscribed and save bytes/data restored exactly.
var player=UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.FirstPersonPlayer>();
if(!UnityEngine.Application.isPlaying||player==null)return "Play the built first-person scene first";
var tools=player.tools;var held=player.viewCamera.GetComponent<CarpetCleaning.FirstPersonToolView>();
var hud=UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.FirstPersonHud>();
var economy=UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.Economy>();
var achievements=UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.SteamAchievements>();
var mouse=UnityEngine.InputSystem.Mouse.current;
var state=new UnityEngine.InputSystem.LowLevel.MouseState {position=mouse.position.ReadValue()};
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var focused=typeof(CarpetCleaning.FirstPersonPlayer).GetField("_focused",flags);
var waiting=typeof(CarpetCleaning.FirstPersonPlayer).GetField("_waitForButtonsUp",flags);
bool oldFocus=(bool)focused.GetValue(player),oldWait=(bool)waiting.GetValue(player);
bool oldPause=UnityEditor.EditorApplication.isPaused,oldEconomy=economy.enabled,oldAchievements=achievements!=null&&achievements.enabled;
bool oldNeutral=CarpetCleaning.ToolTuning.Neutral,oldSimPause=tools.sim.Paused,oldUiPause=tools.sim.UiPaused,oldModal=tools.UiModal,oldSuspended=tools.Suspended;
bool oldInput=tools.InputEnabled,oldScripted=tools.ScriptedPointer;
var oldLock=UnityEngine.Cursor.lockState;bool oldCursorVisible=UnityEngine.Cursor.visible;
var oldPosition=player.transform.position;float oldYaw=player.Yaw,oldPitch=player.Pitch;
var oldTool=tools.Current;string oldLang=CarpetCleaning.Loc.Code;
string savePath=CarpetCleaning.SaveStore.FilePath;bool saveExists=System.IO.File.Exists(savePath);
byte[] save=saveExists?System.IO.File.ReadAllBytes(savePath):null;
string data=UnityEngine.JsonUtility.ToJson(economy.Data);
int id=int.Parse(System.IO.File.ReadAllText("Tools/first_person/regeneration_20261115/capture_tool.txt"));
if(id<0||id>9)throw new System.ArgumentOutOfRangeException("capture tool");
try
{
    if(achievements!=null)achievements.enabled=false;economy.enabled=false;
    CarpetCleaning.ToolTuning.SetNeutral(false);
    UnityEditor.EditorApplication.isPaused=true;
    tools.sim.Paused=false;tools.sim.UiPaused=false;tools.UiModal=false;tools.Suspended=false;tools.InputEnabled=true;tools.ScriptedPointer=false;
    CarpetCleaning.Loc.SetLanguage("ru",false);
    UnityEngine.InputSystem.LowLevel.InputState.Change(mouse,state);
    player.DebugPose(id>=8?2.20f:0f,-2.70f,0f,62f);
    tools.SetTool((CarpetCleaning.ToolId)id);
    UnityEditor.EditorApplication.Step();
    focused.SetValue(player,true);waiting.SetValue(player,false);UnityEngine.Cursor.lockState=UnityEngine.CursorLockMode.Locked;
    tools.SendMessage("Update");
    UnityEngine.InputSystem.LowLevel.InputState.Change(mouse,state.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left,true));
    for(int i=0;i<16;i++){tools.SendMessage("Update");CarpetCleaning.CarpetDebugApi.StepAll(1f/60f);}
    held.SendMessage("LateUpdate");hud.SendMessage("Update");
    string folder=System.IO.Path.GetFullPath("reports/first_person/regeneration_20261115/gameplay");
    System.IO.Directory.CreateDirectory(folder);
    string file=System.IO.Path.Combine(folder,"tool_"+id+".png");
    string capture=CarpetCleaning.EconomyDebugApi.CaptureHud(file,2560,1440);
    var effective=CarpetCleaning.ToolTuning.Effective((CarpetCleaning.ToolId)id);
    var shaft=(UnityEngine.Transform)held.GetType().GetField("_shaft",flags).GetValue(held);
    var shown=(UnityEngine.Transform)held.GetType().GetField("_shown",flags).GetValue(held);
    var foot=(UnityEngine.Transform)held.GetType().GetField("_vacuumFoot",flags).GetValue(held);
    var hose=(UnityEngine.LineRenderer)held.GetType().GetField("_hose",flags).GetValue(held);
    var spray=(UnityEngine.LineRenderer)held.GetType().GetField("_spray",flags).GetValue(held);
    var head=id==1?foot:held.surfaceTools.ShownTransform;
    var ci=System.Globalization.CultureInfo.InvariantCulture;
    string fitting="";
    if(shaft!=null&&shaft.gameObject.activeInHierarchy&&head!=null)
    {
        var marker=head.Find(CarpetCleaning.ToolModelView.SocketNode);
        if(marker!=null)
        {
            float distance=UnityEngine.Mathf.Min(UnityEngine.Vector3.Distance(shaft.position+shaft.up*shaft.lossyScale.y,marker.position),UnityEngine.Vector3.Distance(shaft.position-shaft.up*shaft.lossyScale.y,marker.position));
            fitting+="shaft endpoint/socket distance="+distance.ToString("F7",ci)+"m; diameter="+(shaft.lossyScale.x*1000f).ToString("F2",ci)+"mm\n";
        }
    }
    if(shown!=null&&hose.enabled&&shown.Find("HoseSocket")!=null)fitting+="hose/inlet distance="+UnityEngine.Vector3.Distance(hose.GetPosition(0),shown.Find("HoseSocket").position).ToString("F7",ci)+"m\n";
    if(shown!=null&&spray.enabled)fitting+="nozzle/jet angle="+UnityEngine.Vector3.Angle(shown.forward,spray.GetPosition(spray.positionCount-1)-spray.GetPosition(0)).ToString("F4",ci)+"deg\n";
    float nearest=1000f;
    if(shown!=null)foreach(var mesh in shown.GetComponentsInChildren<UnityEngine.MeshFilter>())if(mesh.sharedMesh!=null)foreach(var v in mesh.sharedMesh.vertices)
        nearest=UnityEngine.Mathf.Min(nearest,player.viewCamera.transform.InverseTransformPoint(mesh.transform.TransformPoint(v)).z);
    if(shown!=null)fitting+="nearest held vertex="+nearest.ToString("F4",ci)+"m; nearClip="+player.viewCamera.nearClipPlane.ToString("F4",ci)+"m\n";
    string diagnostic=player.DiagnosticsJson()+"\n"+capture+"\nheadScale="+CarpetCleaning.ToolTuning.HeadScale((CarpetCleaning.ToolId)id).ToString("F4",ci)+"; effective radius/length mm="+effective.radiusMm.ToString("F3",ci)+"/"+effective.lengthMm.ToString("F3",ci)+"\n"+fitting+CarpetCleaning.CarpetDebugApi.ToolHeadJson();
    System.IO.File.WriteAllText(System.IO.Path.Combine(folder,"tool_"+id+".txt"),diagnostic);
    return diagnostic;
}
finally
{
    UnityEngine.InputSystem.LowLevel.InputState.Change(mouse,state);tools.SendMessage("Update");
    UnityEngine.JsonUtility.FromJsonOverwrite(data,economy.Data);
    if(saveExists)System.IO.File.WriteAllBytes(savePath,save);else if(System.IO.File.Exists(savePath))System.IO.File.Delete(savePath);
    CarpetCleaning.ToolTuning.SetNeutral(oldNeutral);
    player.DebugPose(oldPosition.x,oldPosition.z,oldYaw,oldPitch);tools.SetTool(oldTool);
    tools.sim.Paused=oldSimPause;tools.sim.UiPaused=oldUiPause;tools.UiModal=oldModal;tools.Suspended=oldSuspended;tools.InputEnabled=oldInput;tools.ScriptedPointer=oldScripted;
    focused.SetValue(player,oldFocus);waiting.SetValue(player,oldWait);UnityEngine.Cursor.lockState=oldLock;UnityEngine.Cursor.visible=oldCursorVisible;
    CarpetCleaning.Loc.SetLanguage(oldLang,false);
    economy.enabled=oldEconomy;if(achievements!=null)achievements.enabled=oldAchievements;
    UnityEditor.EditorApplication.isPaused=oldPause;
}
