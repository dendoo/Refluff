// eval_file body. One review frame at the real eye height and existing FOV; no simulation/save reset.
// Run only in the exclusive room QA slot after tool import/fitting. Restores pose, selection and locale.
if (!UnityEngine.Application.isPlaying) return "{\"ok\":false,\"error\":\"Enter Play first\"}";
var player=UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.FirstPersonPlayer>();
var tools=UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.ToolController>();
var sim=UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.CarpetSimulation>();
var hud=UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.EconomyHud>();
var menu=UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.PauseMenu>();
if(player==null || tools==null || sim==null || hud==null)
    return "{\"ok\":false,\"error\":\"First-person scene is missing\"}";
if(hud.ShopOpen || (menu!=null && menu.IsOpen) || tools.UiModal || tools.Suspended)
    return "{\"ok\":false,\"error\":\"Close modals before overview capture\"}";
var position=player.transform.position;
float yaw=player.Yaw,pitch=player.Pitch;
var selected=tools.Current;
string language=CarpetCleaning.Loc.Code;
bool input=tools.InputEnabled,scripted=tools.ScriptedPointer,down=tools.ScriptedDown,right=tools.ScriptedRight;
bool simPaused=sim.Paused,uiPaused=sim.UiPaused,editorPaused=UnityEditor.EditorApplication.isPaused;
string result=null; var volumeMats=new System.Collections.Generic.Dictionary<UnityEngine.Material,UnityEngine.Vector4>();
try
{
    foreach(var r in UnityEngine.GameObject.Find("Workshop").GetComponentsInChildren<UnityEngine.MeshRenderer>()) { var m=r.sharedMaterial; if(m!=null && m.shader.name=="CarpetCleaning/WindowLightVolume" && !volumeMats.ContainsKey(m)) { var old=m.GetVector("_NoiseParams"); volumeMats.Add(m,old); var next=old; next.w=3.5f;m.SetVector("_NoiseParams",next); } }
    UnityEditor.EditorApplication.isPaused=true;
    tools.InputEnabled=false;tools.ScriptedPointer=false;tools.ScriptedDown=false;tools.ScriptedRight=false;
    sim.Paused=true;sim.UiPaused=false;
    CarpetCleaning.Loc.SetLanguage("ru",false);
    tools.SetTool(CarpetCleaning.ToolId.DryVacuum);
    // Northern door, eastern window/shafts, central carpet and western workbench share one view.
    player.DebugPose(-2.6f,-4.4f,22f,13f);
    for(int i=0;i<3;i++) UnityEditor.EditorApplication.Step();
    result=CarpetCleaning.EconomyDebugApi.CaptureHud("Screenshots/first_person_room_overview_light35.png",2560,1440);
}
finally
{
    foreach(var pair in volumeMats) pair.Key.SetVector("_NoiseParams",pair.Value);
    player.DebugPose(position.x,position.z,yaw,pitch);
    tools.SetTool(selected);
    CarpetCleaning.Loc.SetLanguage(language,false);
    tools.InputEnabled=input;tools.ScriptedPointer=scripted;tools.ScriptedDown=down;tools.ScriptedRight=right;
    sim.Paused=simPaused;sim.UiPaused=uiPaused;UnityEditor.EditorApplication.isPaused=editorPaused;
}
if(result!=null) return result;


