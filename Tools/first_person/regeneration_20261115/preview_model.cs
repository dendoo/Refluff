// Set preview_path.txt to an accepted downloaded GLB or normalized prefab, then run this eval_file while stopped.
string sourcePath = System.IO.File.ReadAllText("Tools/first_person/regeneration_20261115/preview_path.txt").Trim();
if(!sourcePath.StartsWith("Assets/CarpetCleaning/"))throw new System.ArgumentException("Unexpected preview asset path");
UnityEditor.AssetDatabase.ImportAsset(sourcePath,UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var source=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(sourcePath);
if(source==null)return "MISSING "+sourcePath;
var go=UnityEngine.Object.Instantiate(source);
var preview=new UnityEditor.PreviewRenderUtility();
preview.AddSingleGO(go);
try {
 var rs=go.GetComponentsInChildren<UnityEngine.Renderer>();
 if(rs.Length==0)return "No renderers";
 var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
 go.transform.position-=bounds.center;
 float extent=UnityEngine.Mathf.Max(bounds.size.x,UnityEngine.Mathf.Max(bounds.size.y,bounds.size.z));
 preview.cameraFieldOfView=30f;preview.camera.nearClipPlane=.001f;preview.camera.farClipPlane=20f;
 preview.camera.backgroundColor=new UnityEngine.Color(.25f,.28f,.31f);preview.camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;
 preview.lights[0].intensity=1.4f;preview.lights[0].transform.rotation=UnityEngine.Quaternion.Euler(35,-30,0);
 preview.lights[1].intensity=.8f;preview.lights[1].transform.rotation=UnityEngine.Quaternion.Euler(30,140,0);
 preview.ambientColor=new UnityEngine.Color(.5f,.5f,.5f);
 var views=new[]{new UnityEngine.Vector3(1,.5f,-2),new UnityEngine.Vector3(0,0,-2),new UnityEngine.Vector3(2,0,0),new UnityEngine.Vector3(0,0,2)};
 string output="reports/first_person/regeneration_20261115/models/"+(sourcePath.EndsWith(".prefab")?"Normalized_":"")+System.IO.Path.GetFileNameWithoutExtension(sourcePath);
 System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output));
 for(int i=0;i<views.Length;i++) {
  preview.camera.transform.position=views[i].normalized*extent*2.4f;preview.camera.transform.LookAt(UnityEngine.Vector3.zero);
  preview.BeginStaticPreview(new UnityEngine.Rect(0,0,640,640));preview.Render(true);var image=preview.EndStaticPreview();
  System.IO.File.WriteAllBytes(output+"_view"+i+".png",UnityEngine.ImageConversion.EncodeToPNG(image));UnityEngine.Object.DestroyImmediate(image);
 }
 var spin=go.transform.Find(CarpetCleaning.ToolModelView.SpinNode);
 if(spin!=null) {
  preview.camera.transform.position=views[0].normalized*extent*2.4f;preview.camera.transform.LookAt(UnityEngine.Vector3.zero);
  spin.localRotation=UnityEngine.Quaternion.Euler(0,30,0);
  preview.BeginStaticPreview(new UnityEngine.Rect(0,0,640,640));preview.Render(true);var image=preview.EndStaticPreview();
  System.IO.File.WriteAllBytes(output+"_spin30.png",UnityEngine.ImageConversion.EncodeToPNG(image));UnityEngine.Object.DestroyImmediate(image);
 }
 return "Previewed "+sourcePath+"; bounds "+bounds.ToString("F4",System.Globalization.CultureInfo.InvariantCulture)+"; "+output;
}
finally {preview.Cleanup();}
