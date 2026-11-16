// eval_file body. Fresh Play after compile/build; real ToolController.Update -> GPU -> floor water.
// Three water frames and one real far-hose HUD capture at 2560x1440; cropped readbacks. Changes transient floor water.
// No save writes or reset; restores input/pose/selection/pause and disables achievement/economy listeners.
var p=UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.FirstPersonPlayer>();
var t=UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.ToolController>();
var floor=UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.FloorSimulation>();
var mouse=UnityEngine.InputSystem.Mouse.current;
if(!UnityEngine.Application.isPlaying || p==null || t==null || t.sim==null || !t.sim.Ready || floor==null || !floor.Ready || mouse==null)
    return "{\"ok\":false,\"error\":\"Play an initialized FPS scene with a mouse\"}";
var sim=t.sim;var held=p.viewCamera.GetComponent<CarpetCleaning.FirstPersonToolView>();
var handover=UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.CarpetHandover>();
if(sim.CarpetAway || (handover!=null && handover.State!=CarpetCleaning.HandoverState.Idle) || t.UiModal || t.Suspended)
    return "{\"ok\":false,\"error\":\"Close modals and wait for the playable carpet\"}";
var keyboard=UnityEngine.InputSystem.Keyboard.current;
if(keyboard!=null)foreach(var k in keyboard.allKeys)if(k.isPressed || k.wasPressedThisFrame)
    return "{\"ok\":false,\"error\":\"Retry with no real keyboard input\"}";
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var update=t.GetType().GetMethod("Update",flags);
var focus=p.GetType().GetField("_focused",flags);var wait=p.GetType().GetField("_waitForButtonsUp",flags);
var blocked=p.GetType().GetField("_wasBlocked",flags);var owns=p.GetType().GetField("_ownsCursor",flags);
var modalRelease=t.GetType().GetField("_modalRelease",flags);
if(held==null || update==null || focus==null || wait==null || blocked==null || owns==null || modalRelease==null)
    return "{\"ok\":false,\"error\":\"Reflection/view contract changed\"}";
var ci=System.Globalization.CultureInfo.InvariantCulture;
System.Func<double,string> n=x=>x.ToString("0.000000",ci);
System.Func<string,string> q=s=>"\""+s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\n"," ").Replace("\r","")+"\"";
var checks=new System.Collections.Generic.List<string>();bool all=true;
System.Action<string,bool,string> check=(name,ok,detail)=>{all&=ok;checks.Add("{\"name\":"+q(name)+",\"ok\":"+(ok?"true":"false")+",\"detail\":"+q(detail)+"}");};
var pos=p.transform.position;float yaw=p.Yaw,pitch=p.Pitch,timeScale=sim.TimeScale;var selected=t.Current;
bool editorPaused=UnityEditor.EditorApplication.isPaused,simPaused=sim.Paused,uiPaused=sim.UiPaused;
bool input=t.InputEnabled,scripted=t.ScriptedPointer,down=t.ScriptedDown,right=t.ScriptedRight;
var scriptedUv=t.ScriptedUv;var pointerUi=t.PointerOverUI;
bool oldFocus=(bool)focus.GetValue(p),oldWait=(bool)wait.GetValue(p),oldBlocked=(bool)blocked.GetValue(p),oldOwns=(bool)owns.GetValue(p),oldModal=(bool)modalRelease.GetValue(t);
var cursor=UnityEngine.Cursor.lockState;bool cursorVisible=UnityEngine.Cursor.visible;
var achievements=UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.SteamAchievements>();bool achievementsOn=achievements!=null && achievements.enabled;
var economy=UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.Economy>();bool economyOn=economy!=null && economy.enabled;
var originalMouse=new UnityEngine.InputSystem.LowLevel.MouseState {
    position=mouse.position.ReadValue(),delta=mouse.delta.ReadValue(),scroll=mouse.scroll.ReadValue(),
    displayIndex=(ushort)mouse.displayIndex.ReadValue(),clickCount=(ushort)mouse.clickCount.ReadValue()
};
originalMouse=originalMouse.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left,mouse.leftButton.isPressed)
    .WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Right,mouse.rightButton.isPressed)
    .WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Middle,mouse.middleButton.isPressed)
    .WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Forward,mouse.forwardButton.isPressed)
    .WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Back,mouse.backButton.isPressed);
var idleMouse=new UnityEngine.InputSystem.LowLevel.MouseState{position=new UnityEngine.Vector2(17f,23f)};
string savePath=CarpetCleaning.SaveStore.FilePath;
System.Func<string> saveHash=()=>{if(!System.IO.File.Exists(savePath))return "missing";using(var sha=System.Security.Cryptography.SHA256.Create())return System.BitConverter.ToString(sha.ComputeHash(System.IO.File.ReadAllBytes(savePath))).Replace("-","").ToLowerInvariant();};
string beforeHash=saveHash();
System.Action release=()=>{
    UnityEngine.InputSystem.LowLevel.InputState.Change(mouse,idleMouse);
    focus.SetValue(p,true);wait.SetValue(p,false);blocked.SetValue(p,false);owns.SetValue(p,true);
    UnityEngine.Cursor.lockState=UnityEngine.CursorLockMode.Locked;UnityEngine.Cursor.visible=false;
    update.Invoke(t,null);
};
try {
    UnityEditor.EditorApplication.isPaused=true;
    if(achievementsOn)achievements.enabled=false;if(economyOn)economy.enabled=false;
    t.InputEnabled=true;t.ScriptedPointer=false;t.ScriptedDown=false;t.ScriptedRight=false;
    t.PointerOverUI=v=>true; // locked FPS must ignore the unrelated OS cursor/UI position
    sim.Paused=false;sim.UiPaused=false;sim.TimeScale=1f;
    t.SetTool(CarpetCleaning.ToolId.Hose);
    var target=new UnityEngine.Vector3(2.4f,-.003f,5.4f);
    var direction=target-new UnityEngine.Vector3(2.4f,p.floorY+p.eyeHeight,-5.2f);
    p.DebugPose(2.4f,-5.2f,0f,-UnityEngine.Mathf.Atan2(direction.y,direction.z)*UnityEngine.Mathf.Rad2Deg);
    release();check("input_capture_ready",p.CanControl,"CanControl="+p.CanControl);
    if(!p.CanControl)throw new System.InvalidOperationException("Editor did not accept locked cursor");
    UnityEngine.Vector2 uv;
    bool aim=t.TryGetFirstPersonUV(out uv);
    check("far_floor_aim",aim && !t.PointerOnCarpet,"uv="+uv+",distance="+n(UnityEngine.Vector3.Distance(p.viewCamera.transform.position,target)));
    if(!aim)throw new System.InvalidOperationException("Far floor is not reachable");
    CarpetCleaning.CarpetDebugApi.StepAll(1f/60f); // flush pre-test stamps before measuring water
    var cmm=UnityEngine.Vector2.Scale(uv,CarpetCleaning.CarpetSimulation.CarpetSizeMm);
    var floorUv=(cmm-CarpetCleaning.FloorConsts.FloorMinMm)/CarpetCleaning.FloorConsts.FloorSizeMm;
    int cx=UnityEngine.Mathf.FloorToInt(floorUv.x*floor.Fluid.width),cy=UnityEngine.Mathf.FloorToInt(floorUv.y*floor.Fluid.height);
    const int side=64;
    int x0=UnityEngine.Mathf.Clamp(cx-side/2,0,floor.Fluid.width-side),y0=UnityEngine.Mathf.Clamp(cy-side/2,0,floor.Fluid.height-side);
    var beforeRequest=UnityEngine.Rendering.AsyncGPUReadback.Request(floor.Fluid,0,x0,side,y0,side,0,1);beforeRequest.WaitForCompletion();
    if(beforeRequest.hasError)throw new System.InvalidOperationException("Before floor readback failed");
    var beforeNative=beforeRequest.GetData<UnityEngine.Vector4>();var beforeWater=new float[side*side];
    for(int i=0;i<beforeWater.Length;i++)beforeWater[i]=beforeNative[i].x;
    int countBefore=t.StampCount;
    UnityEngine.InputSystem.LowLevel.InputState.Change(mouse,idleMouse.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left,true));
    bool gpuStampCorrect=true;float uvErrorMm=0f;
    for(int frame=0;frame<3;frame++) {
        update.Invoke(t,null);CarpetCleaning.CarpetDebugApi.StepAll(1f/60f);
        if(sim.StampCount<=0){gpuStampCorrect=false;continue;}
        var stamp=new CarpetCleaning.ToolStamp[1];sim.StampBuffer.GetData(stamp,0,0,1);
        uvErrorMm=UnityEngine.Vector2.Scale(stamp[0].uv-uv,CarpetCleaning.CarpetSimulation.CarpetSizeMm).magnitude;
        gpuStampCorrect&=stamp[0].tool==(int)CarpetCleaning.ToolId.Hose && uvErrorMm<.05f && UnityEngine.Mathf.Abs(stamp[0].pressure-1f)<.0001f;
    }
    check("real_update_gpu_stamp",gpuStampCorrect && t.StampCount==countBefore+3 && t.IsStroking && t.HasSurfaceAim,
        "added="+(t.StampCount-countBefore)+",uvErrorMm="+n(uvErrorMm));
    held.SendMessage("LateUpdate");
    var spray=(UnityEngine.LineRenderer)held.GetType().GetField("_spray",flags).GetValue(held);
    var shown=(UnityEngine.Transform)held.GetType().GetField("_shown",flags).GetValue(held);
    CarpetCleaning.FirstPersonToolSet.Entry entry;
    bool marker=held.generatedTools!=null && held.generatedTools.TryGet(CarpetCleaning.ToolId.Hose,out entry);
    // Read the entry again here so definite-assignment remains explicit in the CLI compiler.
    float startError=999f,endError=999f,angle=999f;
    if(spray!=null && spray.enabled && shown!=null && held.generatedTools!=null && held.generatedTools.TryGet(CarpetCleaning.ToolId.Hose,out entry)) {
        startError=UnityEngine.Vector3.Distance(spray.GetPosition(0),shown.TransformPoint(entry.nozzlePosition));
        endError=UnityEngine.Vector3.Distance(spray.GetPosition(spray.positionCount-1),target);
        angle=UnityEngine.Vector3.Angle(shown.forward,spray.GetPosition(spray.positionCount-1)-spray.GetPosition(0));
    }
    check("jet_matches_nozzle_and_far_water",marker && spray!=null && spray.enabled && startError<.001f && endError<.001f && angle<.2f,
        "startM="+n(startError)+",endM="+n(endError)+",barrelAngleDeg="+n(angle));
    var fpHud=UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.FirstPersonHud>();
    if(fpHud!=null)fpHud.SendMessage("Update");
    string capturePath=System.IO.Path.GetFullPath("reports/first_person/interaction_20261115/hose_far.png");
    string capture=CarpetCleaning.EconomyDebugApi.CaptureHud(capturePath,2560,1440);
    check("far_hose_hud_capture",capture.Contains("\"ok\":true"),capture);
    var afterRequest=UnityEngine.Rendering.AsyncGPUReadback.Request(floor.Fluid,0,x0,side,y0,side,0,1);afterRequest.WaitForCompletion();
    if(afterRequest.hasError)throw new System.InvalidOperationException("After floor readback failed");
    var afterNative=afterRequest.GetData<UnityEngine.Vector4>();double added=0,mx=0,my=0;bool finite=true;
    for(int y=0;y<side;y++)for(int x=0;x<side;x++) {
        int i=y*side+x;float value=afterNative[i].x;finite&=!float.IsNaN(value) && !float.IsInfinity(value);
        double delta=System.Math.Max(0,value-beforeWater[i]);added+=delta;mx+=delta*(x0+x+.5);my+=delta*(y0+y+.5);
    }
    double dx=CarpetCleaning.FloorConsts.FloorSizeMm/floor.Fluid.width;
    double volume=added*dx*dx;
    double ex=(mx/System.Math.Max(added,1e-9)-floorUv.x*floor.Fluid.width)*dx;
    double ey=(my/System.Math.Max(added,1e-9)-floorUv.y*floor.Fluid.height)*dx;
    double centerError=System.Math.Sqrt(ex*ex+ey*ey);
    check("actual_water_in_new_floor_band",finite && volume>10 && centerError<dx*2.5,
        "positiveDeltaMm3="+n(volume)+",centroidErrorMm="+n(centerError)+",cellMm="+n(dx));
    release();p.DebugPose(-5.1f,.5f,90f,50f);
    UnityEngine.InputSystem.LowLevel.InputState.Change(mouse,idleMouse.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left,true));
    int blockedCount=t.StampCount;update.Invoke(t,null);held.SendMessage("LateUpdate");
    check("blocked_table_no_stamp_or_jet",t.StampCount==blockedCount && !t.HasSurfaceAim && !spray.enabled,"added="+(t.StampCount-blockedCount)+",spray="+spray.enabled);
    release();p.DebugPose(0f,-3f,0f,-20f);
    UnityEngine.InputSystem.LowLevel.InputState.Change(mouse,idleMouse.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left,true));
    blockedCount=t.StampCount;update.Invoke(t,null);held.SendMessage("LateUpdate");
    check("sky_no_stamp_or_jet",t.StampCount==blockedCount && !t.HasSurfaceAim && !spray.enabled,"added="+(t.StampCount-blockedCount)+",spray="+spray.enabled);
} catch(System.Exception e) {check("exception",false,e.ToString());}
finally {
    UnityEngine.InputSystem.LowLevel.InputState.Change(mouse,idleMouse);t.InputEnabled=false;update.Invoke(t,null);
    p.DebugPose(pos.x,pos.z,yaw,pitch);t.SetTool(selected);
    t.InputEnabled=input;t.ScriptedPointer=scripted;t.ScriptedDown=down;t.ScriptedRight=right;t.ScriptedUv=scriptedUv;t.PointerOverUI=pointerUi;
    sim.Paused=simPaused;sim.UiPaused=uiPaused;sim.TimeScale=timeScale;
    focus.SetValue(p,oldFocus);wait.SetValue(p,oldWait);blocked.SetValue(p,oldBlocked);owns.SetValue(p,oldOwns);modalRelease.SetValue(t,oldModal);
    UnityEngine.Cursor.lockState=cursor;UnityEngine.Cursor.visible=cursorVisible;
    UnityEngine.InputSystem.LowLevel.InputState.Change(mouse,originalMouse);
    if(economy!=null)economy.enabled=economyOn;if(achievements!=null)achievements.enabled=achievementsOn;
    UnityEditor.EditorApplication.isPaused=editorPaused;
}
check("save_bytes_unchanged",beforeHash==saveHash(),beforeHash);
string report="{\"ok\":"+(all?"true":"false")+",\"checks\":["+string.Join(",",checks)+"],\"note\":"+q("Three transient water frames remain in the floor simulation. No save writes. Camera-ray occlusion is tested; independent offset nozzle collision is outside current scope.")+"}";
string dir=System.IO.Path.GetFullPath("reports/first_person/interaction_20261115");System.IO.Directory.CreateDirectory(dir);
System.IO.File.WriteAllText(System.IO.Path.Combine(dir,"hose_far.json"),report);
if(report!=null)return report;

