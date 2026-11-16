var p=UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.FirstPersonPlayer>();
var held=p.viewCamera.GetComponent<CarpetCleaning.FirstPersonToolView>();
var bench=UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.UpgradeWorkbench>();
var shader=UnityEngine.Shader.Find("CarpetCleaning/FirstPersonSpray");
var messages=shader!=null?UnityEditor.ShaderUtil.GetShaderMessages(shader):null;
var errors=new System.Collections.Generic.List<string>();
if(messages!=null) foreach(var m in messages) if(m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error) errors.Add(m.message);
int visibleTools=0;
foreach(var def in CarpetCleaning.ToolTable.All)
    if(held.HasGenerated(def.id)||held.surfaceTools.Has(def.id)) visibleTools++;
bool ready=p!=null&&bench!=null&&held!=null&&visibleTools==10&&shader!=null&&shader.isSupported&&errors.Count==0;
string result="{\"ok\":"+(ready?"true":"false")+",\"toolsWithModels\":"+visibleTools+",\"shaderErrors\":"+errors.Count+",\"generatedBodies\":"+held.generatedTools.entries.Length+",\"shellLayers\":"+CarpetCleaning.CarpetRenderer.DefaultShellCount+",\"cameraFov\":"+p.viewCamera.fieldOfView.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}";
System.IO.File.WriteAllText("reports/first_person/final_audit.json",result);
return result;
