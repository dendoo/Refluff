var sims = new[] { "CarpetSim", "FloorSim", "Debris" };
var results = new System.Collections.Generic.List<object>();
foreach (var name in sims) {
 var compute = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.ComputeShader>("Assets/CarpetCleaning/Compute/" + name + ".compute");
 var messages = UnityEditor.ShaderUtil.GetComputeShaderMessages(compute);
 results.Add(new { name=name, messages=System.Array.ConvertAll(messages, m=>m.severity+": "+m.message+" @"+m.line) });
}
return new { failed = UnityEditor.EditorUtility.scriptCompilationFailed, computes=results };
