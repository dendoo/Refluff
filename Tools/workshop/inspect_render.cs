var lines=new System.Collections.Generic.List<string>();
foreach(var name in new[]{"CarpetCleaning/FloorSurface","CarpetCleaning/WindowLightVolume"})
{var sh=UnityEngine.Shader.Find(name);lines.Add(name+": "+string.Join(" | ",System.Array.ConvertAll(UnityEditor.ShaderUtil.GetShaderMessages(sh),m=>m.severity+" "+m.message+" @"+m.line)));}
foreach(var name in new[]{"_F_TILE","_F_GROUT_W","_F_GROUT_DEPTH"})lines.Add(name+"="+UnityEngine.Shader.GetGlobalFloat(name));
var atm=UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.WorkshopAtmosphere>();
var p=UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.FirstPersonPlayer>();
var old=p.transform.position;var yaw=p.Yaw;var pitch=p.Pitch;bool paused=UnityEditor.EditorApplication.isPaused;
try{
 UnityEditor.EditorApplication.isPaused=true;p.DebugPose(-2.6f,-4.4f,22f,13f);
 foreach(var s in atm.shafts)s.enabled=false;
 UnityEditor.EditorApplication.Step();
 lines.Add(CarpetCleaning.StoreShotApi.RenderScene("C:/UnityProjects/CarpetCleaning/reports/workshop_20261116/beam_off.png",1920,1080));
}finally{foreach(var s in atm.shafts)s.enabled=true;p.DebugPose(old.x,old.z,yaw,pitch);UnityEditor.EditorApplication.isPaused=paused;}
return string.Join("\n",lines);
