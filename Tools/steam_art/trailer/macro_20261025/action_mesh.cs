// After mf01_intro SeqEnd, keep the microscopic mesh alive for final restoration.
var cr=UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.CarpetRenderer>();
var sz=CarpetCleaning.CarpetSimulation.CarpetSizeM;
var mesh=CarpetCleaning.ShellMeshBuilder.BuildShells(sz.x,sz.y,96,cr.shellSubdivX,cr.shellSubdivZ,cr.pileHeightM);
mesh.name="MacroTrailer96";
mesh.hideFlags=UnityEngine.HideFlags.HideAndDontSave;
cr.Shells.GetComponent<UnityEngine.MeshFilter>().sharedMesh=mesh;
System.AppDomain.CurrentDomain.SetData("mfCaptureMesh96",mesh);
return "96 capture shells for action closeups; gameplay unchanged";
