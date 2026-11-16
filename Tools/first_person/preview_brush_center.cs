// Isolated preview of the actual saved brush plus a short copy of its runtime shaft/cuff. No import/refresh.
var source=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/CarpetCleaning/Prefabs/Tools/Tool_Brush.prefab");
if(source==null)throw new System.InvalidOperationException("Brush prefab missing.");
var clone=UnityEngine.Object.Instantiate(source);
var socket=clone.transform.Find(CarpetCleaning.ToolModelView.SocketNode);
var spin=clone.transform.Find(CarpetCleaning.ToolModelView.SpinNode);
var metal=new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit"));
metal.SetColor("_BaseColor",new UnityEngine.Color(.46f,.51f,.55f));metal.SetFloat("_Metallic",.76f);metal.SetFloat("_Smoothness",.55f);
var rubber=new UnityEngine.Material(metal);rubber.SetColor("_BaseColor",new UnityEngine.Color(.035f,.045f,.052f));rubber.SetFloat("_Metallic",0f);
var preview=new UnityEditor.PreviewRenderUtility();
try
{
    var position=socket.localPosition;
    var direction=new UnityEngine.Vector3(-.70f,1f,-.20f).normalized;
    System.Action<string,float,float,UnityEngine.Material> connector=(name,length,radius,material)=>
    {
        var go=UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cylinder);
        go.name=name;go.transform.SetParent(clone.transform,false);
        go.transform.localPosition=position+direction*length*.5f;
        go.transform.localRotation=UnityEngine.Quaternion.FromToRotation(UnityEngine.Vector3.up,direction);
        go.transform.localScale=new UnityEngine.Vector3(radius*2f,length*.5f,radius*2f);
        go.GetComponent<UnityEngine.Renderer>().sharedMaterial=material;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<UnityEngine.Collider>());
    };
    connector("Preview of runtime shaft",.12f,.006f,metal);
    connector("Preview of runtime cuff",.018f,.008f,rubber);
    preview.AddSingleGO(clone);
    preview.cameraFieldOfView=30f;preview.camera.nearClipPlane=.001f;preview.camera.farClipPlane=10f;
    preview.camera.backgroundColor=new UnityEngine.Color(.25f,.28f,.31f);preview.camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;
    preview.lights[0].intensity=1.4f;preview.lights[0].transform.rotation=UnityEngine.Quaternion.Euler(35,-30,0);
    preview.lights[1].intensity=.8f;preview.lights[1].transform.rotation=UnityEngine.Quaternion.Euler(30,140,0);
    preview.ambientColor=new UnityEngine.Color(.5f,.5f,.5f);
    var target=new UnityEngine.Vector3(-.018f,.047f,0f);
    var views=new[]{new UnityEngine.Vector3(-1f,1.1f,-1.7f),new UnityEngine.Vector3(.2f,1.6f,-1.7f)};
    System.IO.Directory.CreateDirectory("reports/first_person/brush_center");
    for(int i=0;i<3;i++)
    {
        spin.localRotation=UnityEngine.Quaternion.Euler(0,i==2?30f:0f,0);
        preview.camera.transform.position=target+views[i==1?1:0].normalized*.36f;preview.camera.transform.LookAt(target);
        preview.BeginStaticPreview(new UnityEngine.Rect(0,0,800,800));preview.Render(true);
        var image=preview.EndStaticPreview();
        System.IO.File.WriteAllBytes("reports/first_person/brush_center/attachment_"+(i==2?"spin30":i.ToString())+".png",UnityEngine.ImageConversion.EncodeToPNG(image));
        UnityEngine.Object.DestroyImmediate(image);
    }
    return "Rendered central brush attachment; the static shaft/cuff are a preview-only copy of existing runtime dimensions. Spin preview 0/30 degrees.";
}
finally {preview.Cleanup();UnityEngine.Object.DestroyImmediate(metal);UnityEngine.Object.DestroyImmediate(rubber);}
