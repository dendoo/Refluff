// Targeted shader/compute import and lazy-error verification. Safe in Play (no C# refresh).
foreach (var include in new[] { "SimulationDomain.hlsl", "SimCommon.hlsl", "FloorCommon.hlsl" })
    UnityEditor.AssetDatabase.ImportAsset("Assets/CarpetCleaning/Compute/" + include, UnityEditor.ImportAssetOptions.ForceSynchronousImport | UnityEditor.ImportAssetOptions.ForceUpdate);
string[] names = { "CarpetSim", "FloorSim", "Debris" };
foreach (var name in names)
    UnityEditor.AssetDatabase.ImportAsset("Assets/CarpetCleaning/Compute/" + name + ".compute", UnityEditor.ImportAssetOptions.ForceSynchronousImport | UnityEditor.ImportAssetOptions.ForceUpdate);
if (UnityEngine.Application.isPlaying)
    for (int i = 0; i < 3; i++) CarpetCleaning.CarpetDebugApi.StepAll(1f / 60f);
var lines = new System.Collections.Generic.List<string>();
int errors = 0;
foreach (var name in names)
{
    var compute = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.ComputeShader>("Assets/CarpetCleaning/Compute/" + name + ".compute");
    var messages = UnityEditor.ShaderUtil.GetComputeShaderMessages(compute);
    foreach (var m in messages)
    {
        if (m.severity.ToString() == "Error") errors++;
        lines.Add(name + ": " + m.severity + " " + m.message + " @" + m.line);
    }
}
return "{\"ok\":" + (errors == 0 ? "true" : "false") + ",\"errors\":" + errors + ",\"messages\":"
    + UnityEngine.JsonUtility.ToJson(new UnityEngine.Vector2(lines.Count, errors)) + ",\"details\":\""
    + string.Join(" | ", lines).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", "") + "\"}";
