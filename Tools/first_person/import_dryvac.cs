// Run with unity command eval_file --file C:/UnityProjects/CarpetCleaning/Tools/first_person/import_generated.cs
// Source orientations were inspected with PreviewRenderUtility, not guessed from glTF bounds.
var ids = new[] { CarpetCleaning.ToolId.DryVacuum };
var lengths = new[] { 0.46f };
var rotations = new[] { new UnityEngine.Vector3(0,-90,0), new UnityEngine.Vector3(0,-90,0), new UnityEngine.Vector3(0,-90,0) };
var results = new System.Collections.Generic.List<string>();
for(int i=0;i<ids.Length;i++) {
    string source = "Assets/CarpetCleaning/Models/FirstPerson/Tool_"+ids[i]+".glb";
    if(!System.IO.File.Exists(source)) continue;
    results.Add(CarpetCleaning.FirstPersonModelImporter.Import(ids[i],source,rotations[i],lengths[i]));
    var set = CarpetCleaning.FirstPersonModelImporter.EnsureSet();
    for(int e=0;e<set.entries.Length;e++) {
        if(set.entries[e].id != ids[i]) continue;
        var entry = set.entries[e];
        var all = new System.Collections.Generic.List<UnityEngine.Vector3>();
        var root = entry.prefab.transform;
        foreach(var mf in entry.prefab.GetComponentsInChildren<UnityEngine.MeshFilter>(true)) {
            var matrix = root.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            foreach(var v in mf.sharedMesh.vertices) all.Add(matrix.MultiplyPoint3x4(v));
        }
        float maxZ = -100f;
        foreach(var v in all) maxZ = UnityEngine.Mathf.Max(maxZ,v.z);
        var bound = new UnityEngine.Bounds(); bool first = true;
        foreach(var v in all) if(v.z > maxZ-0.004f) {
            if(first) { bound = new UnityEngine.Bounds(v,UnityEngine.Vector3.zero); first=false; }
            else bound.Encapsulate(v);
        }
        entry.nozzlePosition = new UnityEngine.Vector3(bound.center.x,bound.center.y,maxZ+0.002f);
        entry.cameraPosition = new UnityEngine.Vector3(0.23f,-0.26f,0.48f);
        set.entries[e] = entry;
        results.Add(ids[i]+" nozzle "+entry.nozzlePosition.ToString("F4",System.Globalization.CultureInfo.InvariantCulture));
    }
    UnityEditor.EditorUtility.SetDirty(set);
}
UnityEditor.AssetDatabase.SaveAssets();
return string.Join("\n",results);

