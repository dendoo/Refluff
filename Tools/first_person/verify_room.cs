// eval_file body; source-only companion for the central compile/build/Play pass.
// Run three times: each invocation repeats room QA and captures one of door/south window/east window.
// Captures are native panel size 2560x1440. Original pose, movement bounds, input and pause state are restored.
// Reset the capture sequence with SessionState.SetInt("Refluff.RoomQA.Capture", 0).
if (!UnityEngine.Application.isPlaying) return "{\"ok\":false,\"error\":\"Enter Play after Build Scene\"}";
var player=UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.FirstPersonPlayer>();
var tools=UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.ToolController>();
var sim=UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.CarpetSimulation>();
var bench=UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.UpgradeWorkbench>();
var workshop=UnityEngine.GameObject.Find("Workshop");
var hud=UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.EconomyHud>();
var menu=UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.PauseMenu>();
if(player==null || tools==null || sim==null || bench==null || workshop==null || hud==null)
    return "{\"ok\":false,\"error\":\"First-person room is not ready\"}";
if(hud.ShopOpen || (menu!=null && menu.IsOpen) || tools.UiModal || tools.Suspended)
    return "{\"ok\":false,\"error\":\"Close shop, menu and overlay before room QA\"}";
var culture=System.Globalization.CultureInfo.InvariantCulture;
System.Func<float,string> num=v=>v.ToString("0.####",culture);
System.Func<string,string> esc=s=>s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\n","\\n").Replace("\r","");
var checks=new System.Collections.Generic.List<string>();
bool allPassed=true;
System.Action<string,bool,string> check=(name,passed,detail)=>
{
    allPassed&=passed;
    checks.Add("{\"name\":\""+esc(name)+"\",\"ok\":"+(passed?"true":"false")+",\"detail\":\""+esc(detail)+"\"}");
};
var originalPosition=player.transform.position;
float originalYaw=player.Yaw, originalPitch=player.Pitch;
var originalBounds=player.movementBounds;
bool originalInput=tools.InputEnabled, originalScripted=tools.ScriptedPointer;
bool originalDown=tools.ScriptedDown, originalRight=tools.ScriptedRight;
bool originalPaused=sim.Paused, originalUiPaused=sim.UiPaused, originalEditorPaused=UnityEditor.EditorApplication.isPaused;
int captureIndex=UnityEditor.SessionState.GetInt("Refluff.RoomQA.Capture",0);
if(captureIndex<0 || captureIndex>=3) captureIndex=0;
string[] captureNames={"first_person_room_door","first_person_south_window","first_person_east_window"};
string capture="{\"ok\":false,\"error\":\"capture did not run\"}";
try
{
    UnityEditor.EditorApplication.isPaused=true;
    tools.InputEnabled=false;
    tools.ScriptedPointer=false;
    tools.ScriptedDown=false; tools.ScriptedRight=false;
    sim.Paused=false; sim.UiPaused=false;
    UnityEngine.Physics.SyncTransforms();
    var north=workshop.transform.Find("NorthWall")?.GetComponent<UnityEngine.BoxCollider>();
    var south=workshop.transform.Find("SouthWall")?.GetComponent<UnityEngine.BoxCollider>();
    var east=workshop.transform.Find("EastWall")?.GetComponent<UnityEngine.BoxCollider>();
    var west=workshop.transform.Find("WestWall")?.GetComponent<UnityEngine.BoxCollider>();
    var floor=workshop.transform.Find("WalkingFloor")?.GetComponent<UnityEngine.BoxCollider>();
    bool walls=north!=null && south!=null && east!=null && west!=null;
    float roomX=walls?east.bounds.min.x-west.bounds.max.x:0f;
    float roomZ=walls?north.bounds.min.z-south.bounds.max.z:0f;
    check("room_interior_12m",walls && UnityEngine.Mathf.Abs(roomX-12f)<.01f && UnityEngine.Mathf.Abs(roomZ-12f)<.01f,
        num(roomX)+" x "+num(roomZ)+" metres");
    check("walking_floor_covers_room",floor!=null && floor.bounds.size.x>=12f && floor.bounds.size.z>=12f && UnityEngine.Mathf.Abs(floor.bounds.max.y+.003f)<.003f,
        floor!=null?floor.bounds.ToString():"missing");
    check("movement_bounds_5_65",UnityEngine.Mathf.Abs(originalBounds.xMin+5.65f)<.001f && UnityEngine.Mathf.Abs(originalBounds.xMax-5.65f)<.001f &&
        UnityEngine.Mathf.Abs(originalBounds.yMin+5.65f)<.001f && UnityEngine.Mathf.Abs(originalBounds.yMax-5.65f)<.001f,originalBounds.ToString());
    check("spawn_view_configuration",UnityEngine.Mathf.Abs(player.initialPitch-43f)<.01f && UnityEngine.Mathf.Abs(player.eyeHeight-1.62f)<.01f,
        "pitch="+num(player.initialPitch)+", eye="+num(player.eyeHeight));

    var door=workshop.transform.Find("ClosedDoor");
    var leaf=door!=null?door.Find("ClosedLeaf")?.GetComponent<UnityEngine.BoxCollider>():null;
    check("closed_door_leaf_is_solid",leaf!=null && leaf.enabled && !leaf.isTrigger,leaf!=null?leaf.bounds.ToString():"missing");
    UnityEngine.RaycastHit hit;
    bool doorRay=UnityEngine.Physics.Raycast(new UnityEngine.Vector3(0f,1.2f,5.2f),UnityEngine.Vector3.forward,out hit,1.3f,
        UnityEngine.Physics.DefaultRaycastLayers,UnityEngine.QueryTriggerInteraction.Ignore);
    check("closed_door_blocks_ray",doorRay && leaf!=null && hit.collider==leaf,doorRay?hit.collider.name+" at "+num(hit.distance):"no hit");
    player.DebugPose(0f,4.8f,0f,0f);
    player.StepMovement(UnityEngine.Vector2.up,1.5f);
    float doorStop=player.transform.position.z;
    check("closed_door_blocks_player",doorStop>5.3f && doorStop<5.625f,"z="+num(doorStop));
    // Temporarily widen only the numerical movement clamp: this tests the actual wall collider itself.
    player.movementBounds=new UnityEngine.Rect(-6.5f,-6.5f,13f,13f);
    player.DebugPose(2f,4.8f,0f,0f);
    player.StepMovement(UnityEngine.Vector2.up,1.5f);
    float wallStop=player.transform.position.z;
    check("world_wall_blocks_player",wallStop>5.6f && wallStop<5.95f,"z="+num(wallStop));
    player.movementBounds=originalBounds;

    player.DebugPose(-3.65f,.98f,270f,20f);
    bool nearBench=bench.IsAimedAt(player.viewCamera);
    check("workbench_reachable",nearBench,bench.DebugJson());
    player.DebugPose(-1.2f,.98f,270f,8f);
    check("workbench_rejects_distance",!bench.IsAimedAt(player.viewCamera),bench.DebugJson());
    var cameraData=player.viewCamera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
    check("camera_requests_opaque_depth",cameraData!=null && cameraData.requiresDepthOption==UnityEngine.Rendering.Universal.CameraOverrideOption.On,
        cameraData!=null?cameraData.requiresDepthOption.ToString():"missing camera data");
    check("generated_room_avoids_legacy_occlusion",!player.viewCamera.useOcclusionCulling,
        "useOcclusionCulling="+player.viewCamera.useOcclusionCulling);

    var materials=new System.Collections.Generic.HashSet<UnityEngine.Material>();
    int volumes=0;
    bool volumeSettings=true;
    foreach(var renderer in workshop.GetComponentsInChildren<UnityEngine.MeshRenderer>())
    {
        var m=renderer.sharedMaterial;
        if(m==null || m.shader==null || m.shader.name!="CarpetCleaning/WindowLightVolume") continue;
        volumes++;
        materials.Add(m);
        var p=m.GetVector("_BeamParams");
        var noise=m.GetVector("_NoiseParams");
        var direction=m.GetVector("_BeamDirection");
        var vector=new UnityEngine.Vector3(direction.x,direction.y,direction.z);
        volumeSettings &= renderer.GetComponent<UnityEngine.Collider>()==null && renderer.gameObject.layer==2 &&
            renderer.shadowCastingMode==UnityEngine.Rendering.ShadowCastingMode.Off && m.renderQueue==3025 &&
            m.shader.isSupported && p.x>0f && p.x<.2f && p.y>0f && p.z>0f &&
            UnityEngine.Mathf.Abs(vector.magnitude-1f)<.001f && direction.w>0f && UnityEngine.Mathf.Abs(noise.w-3.5f)<.001f;
    }
    check("six_window_volumes",volumes==6 && materials.Count==6,"renderers="+volumes+", materials="+materials.Count);
    check("volume_material_and_proxy_settings",volumeSettings && volumes==6,"depth-clipped shader, finite density, normalized direction, visual-only proxy");
    var windowMaterial=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/CarpetCleaning/Materials/Workshop/WindowDaylight.mat");
    float emission=windowMaterial!=null?windowMaterial.GetColor("_EmissionColor").maxColorComponent:0f;
    check("window_hdr_emission",windowMaterial!=null && windowMaterial.IsKeywordEnabled("_EMISSION") && emission>1.2f,"HDR maximum="+num(emission));
    var shader=UnityEngine.Shader.Find("CarpetCleaning/WindowLightVolume");
    int shaderErrors=0;
    if(shader!=null) foreach(var message in UnityEditor.ShaderUtil.GetShaderMessages(shader))
        if(message.severity.ToString()=="Error") shaderErrors++;
    check("volume_shader_has_no_compile_errors",shader!=null && shaderErrors==0,"errors="+shaderErrors);

    sim.Paused=true;
    UnityEngine.Vector3 cameraTarget;
    float captureX,captureZ;
    if(captureIndex==0) { captureX=0f; captureZ=-3.8f; cameraTarget=new UnityEngine.Vector3(0f,1.3f,5.85f); }
    else if(captureIndex==1) { captureX=2.8f; captureZ=-3.3f; cameraTarget=new UnityEngine.Vector3(-1.2f,2.05f,-5.1f); }
    else { captureX=1.5f; captureZ=4.4f; cameraTarget=new UnityEngine.Vector3(5.1f,2.05f,1.8f); }
    var eye=new UnityEngine.Vector3(captureX,player.floorY+player.eyeHeight,captureZ);
    var delta=cameraTarget-eye;
    float yaw=UnityEngine.Mathf.Atan2(delta.x,delta.z)*UnityEngine.Mathf.Rad2Deg;
    float pitch=-UnityEngine.Mathf.Atan2(delta.y,new UnityEngine.Vector2(delta.x,delta.z).magnitude)*UnityEngine.Mathf.Rad2Deg;
    player.DebugPose(captureX,captureZ,yaw,pitch);
    for(int i=0;i<3;i++) UnityEditor.EditorApplication.Step();
    capture=CarpetCleaning.EconomyDebugApi.CaptureHud("Screenshots/"+captureNames[captureIndex]+".png",2560,1440);
    check("hud_capture_succeeded",capture.Contains("\"ok\":true"),captureNames[captureIndex]);
    int renderedErrors=0;
    if(shader!=null) foreach(var message in UnityEditor.ShaderUtil.GetShaderMessages(shader))
        if(message.severity.ToString()=="Error") renderedErrors++;
    check("volume_shader_after_render",shader!=null && renderedErrors==0,"errors="+renderedErrors);
    UnityEditor.SessionState.SetInt("Refluff.RoomQA.Capture",captureIndex+1);
}
finally
{
    player.movementBounds=originalBounds;
    player.DebugPose(originalPosition.x,originalPosition.z,originalYaw,originalPitch);
    tools.InputEnabled=originalInput;
    tools.ScriptedPointer=originalScripted;
    tools.ScriptedDown=originalDown; tools.ScriptedRight=originalRight;
    sim.Paused=originalPaused; sim.UiPaused=originalUiPaused;
    UnityEditor.EditorApplication.isPaused=originalEditorPaused;
}
string report="{\"ok\":"+(allPassed?"true":"false")+",\"captureIndex\":"+captureIndex+",\"capture\":"+capture+
    ",\"checks\":["+string.Join(",",checks)+"]}";
string reportDir=System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(),"reports","first_person");
System.IO.Directory.CreateDirectory(reportDir);
System.IO.File.WriteAllText(System.IO.Path.Combine(reportDir,"room_qa_"+captureIndex+".json"),report);
if(report!=null) return report;
