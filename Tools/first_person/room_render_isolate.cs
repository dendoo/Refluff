var p=UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.FirstPersonPlayer>();
var c=p.viewCamera; var pos=p.transform.position; float yaw=p.Yaw,pitch=p.Pitch;
bool occ=c.useOcclusionCulling;
var beams=new System.Collections.Generic.List<UnityEngine.MeshRenderer>();
foreach(var r in UnityEngine.GameObject.Find("Workshop").GetComponentsInChildren<UnityEngine.MeshRenderer>()) if(r.sharedMaterial.shader.name=="CarpetCleaning/WindowLightVolume") {beams.Add(r);r.enabled=true;}
try {
 c.useOcclusionCulling=false; p.DebugPose(0f,-3.8f,0f,2f);
 return CarpetCleaning.EconomyDebugApi.CaptureHud("Screenshots/room_occlusion_off.png",2560,1440);
} finally { foreach(var r in beams) r.enabled=true; c.useOcclusionCulling=occ; p.DebugPose(pos.x,pos.z,yaw,pitch); }

