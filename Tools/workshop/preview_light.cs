var room=UnityEngine.GameObject.Find("Workshop").transform;
foreach(var name in new[]{"Plaster","Paint"}){
var m=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/CarpetCleaning/Materials/Workshop/"+name+".mat");
m.SetColor("_BaseColor",name=="Plaster"?new UnityEngine.Color(.87f,.83f,.74f):new UnityEngine.Color(.37f,.48f,.41f));}
for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2){
string name="IndirectFill"+x+"_"+z;var go=UnityEngine.GameObject.Find(name);if(go==null)go=new UnityEngine.GameObject(name);
go.transform.SetParent(room,false);go.transform.localPosition=new UnityEngine.Vector3(x*3.2f,2.75f,z*3.15f);
var l=go.GetComponent<UnityEngine.Light>();if(l==null)l=go.AddComponent<UnityEngine.Light>();l.type=UnityEngine.LightType.Point;l.color=new UnityEngine.Color(1f,.94f,.82f);l.intensity=2.2f;l.range=8f;l.shadows=UnityEngine.LightShadows.None;
}
var atmosphere=room.GetComponent<CarpetCleaning.WorkshopAtmosphere>();
foreach(var r in atmosphere.shafts){var m=r.sharedMaterial;var n=m.GetVector("_NoiseParams");n.y=.65f;n.w=2.55f;m.SetVector("_NoiseParams",n);}
UnityEditor.SessionState.SetInt("WorkshopReview",0);return "preview lighting applied";
