var lines = new System.Collections.Generic.List<string>();
foreach(var id in new[]{CarpetCleaning.ToolId.Squeegee,CarpetCleaning.ToolId.Mop,CarpetCleaning.ToolId.WetVacuum,CarpetCleaning.ToolId.Brush}) {
 var go=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/CarpetCleaning/Prefabs/Tools/Tool_"+id+".prefab");
 var verts=new System.Collections.Generic.List<UnityEngine.Vector3>();
 foreach(var mf in go.GetComponentsInChildren<UnityEngine.MeshFilter>()) {
  var m=go.transform.worldToLocalMatrix*mf.transform.localToWorldMatrix;
  foreach(var v in mf.sharedMesh.vertices)verts.Add(m.MultiplyPoint3x4(v));
 }
 var b=new UnityEngine.Bounds(verts[0],UnityEngine.Vector3.zero); foreach(var v in verts)b.Encapsulate(v);
 var top=new UnityEngine.Bounds(); bool first=true; foreach(var v in verts)if(v.y>b.min.y+b.size.y*.85f){if(first){top=new UnityEngine.Bounds(v,UnityEngine.Vector3.zero);first=false;}else top.Encapsulate(v);}
 lines.Add(id+" bounds "+b.ToString("F4")+" top "+top.ToString("F4"));
 System.IO.File.WriteAllText("Tools/first_person/socket_"+id+".json",UnityEngine.JsonUtility.ToJson(new UnityEngine.Bounds(top.center,top.size)));
}
return string.Join("\n",lines);
