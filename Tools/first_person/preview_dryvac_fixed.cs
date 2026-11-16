string sourcePath = "Assets/CarpetCleaning/Models/FirstPerson/Tool_DryVacuum.glb";
UnityEditor.AssetDatabase.ImportAsset(sourcePath, UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var source = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(sourcePath);
var go = UnityEngine.Object.Instantiate(source);
foreach(var mf in go.GetComponentsInChildren<UnityEngine.MeshFilter>()) {
 var mesh=UnityEngine.Object.Instantiate(mf.sharedMesh); var verts=mesh.vertices; var normals=mesh.normals; var tris=mesh.triangles;
 var good=new System.Collections.Generic.List<int>();
 for(int i=0;i<tris.Length;i+=3) {
  int a=tris[i],b=tris[i+1],c=tris[i+2]; var face=UnityEngine.Vector3.Cross(verts[b]-verts[a],verts[c]-verts[a]);
  if(face.sqrMagnitude<1e-20f) continue;
  if(UnityEngine.Vector3.Dot(face,normals[a]+normals[b]+normals[c])<0f) { int swap=b;b=c;c=swap; }
  good.Add(a);good.Add(b);good.Add(c);
 }
 mesh.SetTriangles(good,0); mesh.RecalculateBounds(); mf.sharedMesh=mesh;
}
var renderers = go.GetComponentsInChildren<UnityEngine.Renderer>();
var bounds = renderers[0].bounds;
foreach(var r in renderers) bounds.Encapsulate(r.bounds);
go.transform.position -= bounds.center;
float extent = UnityEngine.Mathf.Max(bounds.size.x, UnityEngine.Mathf.Max(bounds.size.y, bounds.size.z));
var directions = new UnityEngine.Vector3[] { new UnityEngine.Vector3(1,0.5f,-2), new UnityEngine.Vector3(0,0, -2), new UnityEngine.Vector3(2,0,0), new UnityEngine.Vector3(0,0,2) };
var preview = new UnityEditor.PreviewRenderUtility();
preview.AddSingleGO(go);
preview.cameraFieldOfView = 30f;
preview.camera.nearClipPlane = 0.01f; preview.camera.farClipPlane = 20f;
preview.camera.backgroundColor = new UnityEngine.Color(0.25f,0.28f,0.31f);
preview.camera.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
preview.lights[0].intensity = 1.4f; preview.lights[0].transform.rotation = UnityEngine.Quaternion.Euler(35,-30,0);
preview.lights[1].intensity = 0.8f; preview.lights[1].transform.rotation = UnityEngine.Quaternion.Euler(30,140,0);
preview.ambientColor = new UnityEngine.Color(0.5f,0.5f,0.5f);
for(int i=0;i<directions.Length;i++) {
    preview.camera.transform.position = directions[i].normalized * extent * 2.4f;
    preview.camera.transform.LookAt(UnityEngine.Vector3.zero);
    preview.BeginStaticPreview(new UnityEngine.Rect(0,0,640,640));
    preview.Render(true);
    var image = preview.EndStaticPreview();
    System.IO.File.WriteAllBytes("Tools/first_person/dryvac_fixed_"+i+".png", UnityEngine.ImageConversion.EncodeToPNG(image));
    UnityEngine.Object.DestroyImmediate(image);
}
preview.Cleanup();
return bounds.ToString();


