// Read-only live audit, run with unity command eval_file --file <this path>.
var root=UnityEngine.GameObject.Find("Workshop/WorkshopDressing");
if(root==null) return "FAIL: WorkshopDressing missing";
var errors=new System.Collections.Generic.List<string>();
int renderers=0,vertices=0,triangles=0,colliders=0;
var protectedArea=new UnityEngine.Bounds(new UnityEngine.Vector3(0,.5f,0),new UnityEngine.Vector3(4.2f,1.0f,5.1f));
foreach(var r in root.GetComponentsInChildren<UnityEngine.MeshRenderer>())
{
    renderers++;
    var mesh=r.GetComponent<UnityEngine.MeshFilter>().sharedMesh;
    if(mesh==null){errors.Add("missing mesh: "+r.name);continue;}
    if(!UnityEditor.AssetDatabase.Contains(mesh))errors.Add("nonpersistent mesh: "+r.name);
    vertices+=mesh.vertexCount;triangles+=mesh.triangles.Length/3;
    if(r.shadowCastingMode!=UnityEngine.Rendering.ShadowCastingMode.On)errors.Add("no casting: "+r.name);
    if(!r.receiveShadows)errors.Add("no receiving: "+r.name);
    if(r.sharedMaterial==null||r.sharedMaterial.shader==null||!r.sharedMaterial.shader.isSupported)errors.Add("unsupported material: "+r.name);
    foreach(var v in mesh.vertices)if(float.IsNaN(v.x)||float.IsInfinity(v.x)||float.IsNaN(v.y)||float.IsInfinity(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.z)){errors.Add("nonfinite geometry: "+r.name);break;}
}
foreach(var c in root.GetComponentsInChildren<UnityEngine.Collider>())
{
    colliders++;if(c.bounds.Intersects(protectedArea))errors.Add("collision in protected work area: "+c.name);
}
if(renderers>160)errors.Add("unexpected renderer count: "+renderers);
if(root.transform.childCount!=8)errors.Add("expected eight dressing zones");
return "Workshop dressing: "+renderers+" renderers, "+vertices+" vertices, "+triangles+" triangles, "+colliders+" colliders; "+errors.Count+" errors. "+string.Join(" | ",errors);
