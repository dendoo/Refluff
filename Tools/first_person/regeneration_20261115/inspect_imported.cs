// Read-only prefab/geometry audit after imports. No scene objects, refresh, Play transition or saved-state changes.
var lines=new System.Collections.Generic.List<string>();int errors=0;
var contacts=new[]{CarpetCleaning.ToolId.Scraper,CarpetCleaning.ToolId.Brush,CarpetCleaning.ToolId.Squeegee,CarpetCleaning.ToolId.WetVacuum,CarpetCleaning.ToolId.Mop};
foreach(var id in contacts) {
 string path="Assets/CarpetCleaning/Prefabs/Tools/Tool_"+id+".prefab";
 var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
 if(prefab==null){lines.Add("FAIL missing "+path);errors++;continue;}
 var root=prefab.transform;var points=new System.Collections.Generic.List<UnityEngine.Vector3>();int triangles=0,bad=0;
 foreach(var mf in prefab.GetComponentsInChildren<UnityEngine.MeshFilter>(true)) {
  var mesh=mf.sharedMesh;if(mesh==null){bad++;continue;}
  var matrix=root.worldToLocalMatrix*mf.transform.localToWorldMatrix;
  foreach(var v in mesh.vertices){var p=matrix.MultiplyPoint3x4(v);points.Add(p);if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.x)||float.IsInfinity(p.y)||float.IsInfinity(p.z))bad++;}
  triangles+=mesh.triangles.Length/3;
 }
 if(points.Count==0){lines.Add("FAIL no readable mesh "+id);errors++;continue;}
 var bounds=new UnityEngine.Bounds(points[0],UnityEngine.Vector3.zero);foreach(var p in points)bounds.Encapsulate(p);
 bool first=true;var working=new UnityEngine.Bounds();
 foreach(var p in points)if(p.y<=bounds.min.y+bounds.size.y*.15f+.000001f){if(first){working=new UnityEngine.Bounds(p,UnityEngine.Vector3.zero);first=false;}else working.Encapsulate(p);}
 float expected=CarpetCleaning.ToolModelPostprocess.TargetSpanM(id);
 bool span=UnityEngine.Mathf.Abs(working.size.z-expected)<.002f;
 bool baseAligned=UnityEngine.Mathf.Abs(bounds.min.y)<.0001f;
 var socket=root.Find(CarpetCleaning.ToolModelView.SocketNode);
 float closest=1000f;if(socket!=null)foreach(var p in points)closest=UnityEngine.Mathf.Min(closest,UnityEngine.Vector3.Distance(socket.localPosition,p));
 bool socketNear=socket!=null&&closest<.025f;
 foreach(var renderer in prefab.GetComponentsInChildren<UnityEngine.Renderer>(true))foreach(var material in renderer.sharedMaterials)
  if(material==null||material.shader==null||material.shader.name=="Hidden/InternalErrorShader")bad++;
 if(!span||!baseAligned||!socketNear||bad>0)errors++;
 lines.Add((span&&baseAligned&&socketNear&&bad==0?"PASS ":"FAIL ")+id+" triangles="+triangles+" span="+working.size.z.ToString("F4",System.Globalization.CultureInfo.InvariantCulture)+" expected="+expected.ToString("F4",System.Globalization.CultureInfo.InvariantCulture)+" base="+bounds.min.y.ToString("F5",System.Globalization.CultureInfo.InvariantCulture)+" socket="+(socket!=null?socket.localPosition.ToString("F4",System.Globalization.CultureInfo.InvariantCulture):"MISSING")+" nearestVertex="+closest.ToString("F4",System.Globalization.CultureInfo.InvariantCulture)+" bad="+bad);
 if(id==CarpetCleaning.ToolId.Brush) {
  var spin=root.Find(CarpetCleaning.ToolModelView.SpinNode);int bodyTriangles=0,spinTriangles=0;
  foreach(var mf in prefab.GetComponentsInChildren<UnityEngine.MeshFilter>(true)){if(mf.sharedMesh==null)continue;if(spin!=null&&mf.transform.IsChildOf(spin))spinTriangles+=mf.sharedMesh.triangles.Length/3;else bodyTriangles+=mf.sharedMesh.triangles.Length/3;}
  var modelSet=UnityEditor.AssetDatabase.LoadAssetAtPath<CarpetCleaning.ToolModelSet>("Assets/CarpetCleaning/Settings/ToolModelSet.asset");
  var settings=UnityEditor.AssetDatabase.LoadAssetAtPath<CarpetCleaning.SimSettings>("Assets/CarpetCleaning/Settings/SimSettings.asset");
  bool tuning=modelSet!=null&&modelSet.TryGet(id,out var entry)&&UnityEngine.Mathf.Approximately(entry.spinRpm,350f)&&UnityEngine.Mathf.Approximately(entry.pileSinkM,.008f)&&settings!=null&&UnityEngine.Mathf.Approximately(settings.BRUSH_RPM,350f);
  bool valid=spin!=null&&spin.childCount>0&&bodyTriangles>0&&spinTriangles>0&&tuning;
  if(!valid)errors++;
  lines.Add((valid?"PASS":"FAIL")+" brush static="+bodyTriangles+" rotating="+spinTriangles+" tuning350rpm/.008m="+tuning+"; compare preview idle and spin30 before acceptance");
 }
}
var held=UnityEditor.AssetDatabase.LoadAssetAtPath<CarpetCleaning.FirstPersonToolSet>(CarpetCleaning.FirstPersonModelImporter.SetPath);
foreach(var id in new[]{CarpetCleaning.ToolId.Hose,CarpetCleaning.ToolId.FloorCleaner}) {
 if(held==null||!held.TryGet(id,out var entry)){errors++;lines.Add("FAIL missing held "+id);continue;}
 string path=UnityEditor.AssetDatabase.GetAssetPath(entry.prefab);bool own=path.EndsWith("/FP_"+id+".prefab");
 if(!own)errors++;
 lines.Add((own?"PASS":"FAIL")+" held "+id+" ownPrefab="+path+" nozzle="+entry.nozzlePosition.ToString("F4",System.Globalization.CultureInfo.InvariantCulture)+" scale="+entry.scale.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+" hoseMarker="+(entry.prefab.transform.Find("HoseSocket")!=null));
}
string output="errors="+errors+"\n"+string.Join("\n",lines);
System.IO.Directory.CreateDirectory("reports/first_person/regeneration_20261115");
System.IO.File.WriteAllText("reports/first_person/regeneration_20261115/unity_geometry.txt",output);
return output;
