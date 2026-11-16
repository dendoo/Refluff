// Run only in the agreed stopped Editor slot. Changes one marker, no mesh import/regeneration or settings write.
if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Stop Play before fitting the brush socket.");
const string path="Assets/CarpetCleaning/Prefabs/Tools/Tool_Brush.prefab";
var root=UnityEditor.PrefabUtility.LoadPrefabContents(path);
try
{
    var socket=root.transform.Find(CarpetCleaning.ToolModelView.SocketNode);
    var spin=root.transform.Find(CarpetCleaning.ToolModelView.SpinNode);
    if(socket==null||spin==null)throw new System.InvalidOperationException("Expected existing brush HandleSocket and Spin nodes.");
    var before=socket.localPosition;
    // The silver centre hub's rim lies at 33.81–34.58mm. Its 6.02mm bore admits the existing 6mm shaft.
    socket.localPosition=new UnityEngine.Vector3(spin.localPosition.x,.0342f,spin.localPosition.z);
    if(socket.IsChildOf(spin))throw new System.InvalidOperationException("The handle must remain on the static housing.");
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,path);
    var set=UnityEditor.AssetDatabase.LoadAssetAtPath<CarpetCleaning.ToolModelSet>("Assets/CarpetCleaning/Settings/ToolModelSet.asset");
    if(!set.TryGet(CarpetCleaning.ToolId.Brush,out var entry))throw new System.InvalidOperationException("Brush tuning missing.");
    string result="Brush centre socket "+socket.localPosition.ToString("F6",System.Globalization.CultureInfo.InvariantCulture)
        +"; before "+before.ToString("F6",System.Globalization.CultureInfo.InvariantCulture)
        +"; socket under static root="+(socket.parent==root.transform)
        +"; spin rpm="+entry.spinRpm.ToString(System.Globalization.CultureInfo.InvariantCulture)
        +"; pile sink="+entry.pileSinkM.ToString(System.Globalization.CultureInfo.InvariantCulture);
    System.IO.Directory.CreateDirectory("reports/first_person/brush_center");
    System.IO.File.WriteAllText("reports/first_person/brush_center/socket_fit.txt",result);
    return result;
}
finally {UnityEditor.PrefabUtility.UnloadPrefabContents(root);}
