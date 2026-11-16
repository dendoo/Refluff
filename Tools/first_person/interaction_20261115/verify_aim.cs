// eval_file body; geometry-only, no Unity frames/stamps/save writes. Run after the central compile/build.
if(!UnityEngine.Application.isPlaying) return "{\"ok\":false,\"error\":\"Enter Play first\"}";
var p=UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.FirstPersonPlayer>();
var t=UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.ToolController>();
if(p==null || t==null || t.carpetCollider==null || !t.carpetCollider.enabled)
    return "{\"ok\":false,\"error\":\"A playable carpet and FPS player are required\"}";
var ci=System.Globalization.CultureInfo.InvariantCulture;
System.Func<float,string> n=x=>x.ToString("0.00000",ci);
System.Func<string,string> q=s=>"\""+s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\n"," ").Replace("\r","")+"\"";
var checks=new System.Collections.Generic.List<string>();bool all=true;
System.Action<string,bool,string> check=(name,ok,detail)=>{all&=ok;checks.Add("{\"name\":"+q(name)+",\"ok\":"+(ok?"true":"false")+",\"detail\":"+q(detail)+"}");};
var pos=p.transform.position;float yaw=p.Yaw,pitch=p.Pitch;var tool=t.Current;
bool paused=UnityEditor.EditorApplication.isPaused;
string savePath=CarpetCleaning.SaveStore.FilePath;
System.Func<string> saveHash=()=>{if(!System.IO.File.Exists(savePath))return "missing";using(var sha=System.Security.Cryptography.SHA256.Create())return System.BitConverter.ToString(sha.ComputeHash(System.IO.File.ReadAllBytes(savePath))).Replace("-","").ToLowerInvariant();};
string beforeHash=saveHash();
// A real eye-height ray to a specified world point; DebugPose resets pointer/height continuity.
System.Action<float,float,UnityEngine.Vector3> aim=(x,z,target)=>{
    var d=target-new UnityEngine.Vector3(x,p.floorY+p.eyeHeight,z);
    p.DebugPose(x,z,UnityEngine.Mathf.Atan2(d.x,d.z)*UnityEngine.Mathf.Rad2Deg,
        -UnityEngine.Mathf.Atan2(d.y,new UnityEngine.Vector2(d.x,d.z).magnitude)*UnityEngine.Mathf.Rad2Deg);
};
System.Action<string,float,float,UnityEngine.Vector3,bool> expectHit=(name,x,z,target,onCarpet)=>{
    aim(x,z,target);UnityEngine.Vector2 uv;bool hit=t.TryGetFirstPersonUV(out uv);
    var local=t.carpetCollider.transform.InverseTransformPoint(target);var size=CarpetCleaning.CarpetSimulation.CarpetSizeM;
    var wanted=new UnityEngine.Vector2(local.x/size.x+.5f,local.z/size.y+.5f);
    float error=UnityEngine.Vector2.Scale(uv-wanted,CarpetCleaning.CarpetSimulation.CarpetSizeMm).magnitude;
    check(name,hit && t.HasSurfaceAim && t.PointerOnCarpet==onCarpet && error<.25f,
        "hit="+hit+",carpet="+t.PointerOnCarpet+",uv="+n(uv.x)+","+n(uv.y)+",errorMm="+n(error));
};
try {
    UnityEditor.EditorApplication.isPaused=true;
    check("configured_close_reach_2m",UnityEngine.Mathf.Abs(p.toolReach-2f)<.001f,"reach="+n(p.toolReach));
    check("floor_covers_12m_room",CarpetCleaning.FloorConsts.FloorSizeMm>=12000f,"floorMm="+n(CarpetCleaning.FloorConsts.FloorSizeMm));
    for(int id=0;id<CarpetCleaning.ToolTable.All.Length;id++) {
        if(id==(int)CarpetCleaning.ToolId.Hose)continue;
        t.SetTool((CarpetCleaning.ToolId)id);UnityEngine.Vector2 uv;
        p.DebugPose(0f,-2.7f,0f,60f);bool near=t.TryGetFirstPersonUV(out uv);
        check("tool_"+id+"_near_carpet_aim",near && t.PointerOnCarpet,"near="+near+",uv="+n(uv.x)+","+n(uv.y));
        p.DebugPose(0f,-2.7f,0f,35f);bool far=t.TryGetFirstPersonUV(out uv);
        check("tool_"+id+"_far_carpet_rejected",!far && !t.HasSurfaceAim,"far="+far);
    }
    t.SetTool(CarpetCleaning.ToolId.Hose);
    expectHit("hose_far_carpet",0f,-5.2f,new UnityEngine.Vector3(0f,.02f,1.8f),true);
    expectHit("hose_new_north_floor_band",2.4f,-5.2f,new UnityEngine.Vector3(2.4f,-.003f,5.4f),false);
    expectHit("hose_new_east_floor_band",-5.2f,-2.8f,new UnityEngine.Vector3(5.4f,-.003f,-2.8f),false);
    UnityEngine.Vector2 blockedUv;UnityEngine.RaycastHit obstacle;
    p.DebugPose(-5.1f,.5f,90f,50f);
    bool tableRay=UnityEngine.Physics.Raycast(p.InteractionRay,out obstacle,3f,UnityEngine.Physics.DefaultRaycastLayers,UnityEngine.QueryTriggerInteraction.Ignore);
    bool tableAim=t.TryGetFirstPersonUV(out blockedUv);
    check("hose_camera_ray_table_blocks",tableRay && obstacle.transform.IsChildOf(UnityEngine.GameObject.Find("Workshop/UpgradeWorkbench").transform) && !tableAim,
        "first="+(tableRay?obstacle.collider.name:"none")+",aim="+tableAim);
    p.DebugPose(2.4f,5.4f,0f,10f);
    bool wallRay=UnityEngine.Physics.Raycast(p.InteractionRay,out obstacle,2f,UnityEngine.Physics.DefaultRaycastLayers,UnityEngine.QueryTriggerInteraction.Ignore);
    bool wallAim=t.TryGetFirstPersonUV(out blockedUv);
    check("hose_camera_ray_wall_blocks",wallRay && obstacle.collider.name=="NorthWall" && !wallAim,
        "first="+(wallRay?obstacle.collider.name:"none")+",aim="+wallAim+"; floor target is also outside domain");
    p.DebugPose(0f,-3f,0f,-20f);check("hose_sky_no_surface",!t.TryGetFirstPersonUV(out blockedUv),"pitch=-20");
    p.DebugPose(0f,-3f,0f,0f);check("hose_horizontal_no_surface",!t.TryGetFirstPersonUV(out blockedUv),"pitch=0");
    p.DebugPose(0f,-3f,0f,.01f);check("hose_near_horizontal_beyond_room",!t.TryGetFirstPersonUV(out blockedUv),"pitch=.01; no infinite-distance stamp");
} catch(System.Exception e) {check("exception",false,e.ToString());}
finally {p.DebugPose(pos.x,pos.z,yaw,pitch);t.SetTool(tool);UnityEditor.EditorApplication.isPaused=paused;}
check("save_bytes_unchanged",beforeHash==saveHash(),beforeHash);
string report="{\"ok\":"+(all?"true":"false")+",\"checks\":["+string.Join(",",checks)+"],\"knownLimitations\":["+
    q("Occlusion checks camera-to-surface. The offset nozzle-to-target segment is not independently collided; near a furniture edge it can intersect furniture while the reticle ray remains clear.")+
    "],\"saveSha256\":"+q(saveHash())+"}";
string dir=System.IO.Path.GetFullPath("reports/first_person/interaction_20261115");System.IO.Directory.CreateDirectory(dir);
System.IO.File.WriteAllText(System.IO.Path.Combine(dir,"aim.json"),report);
if(report!=null)return report;
