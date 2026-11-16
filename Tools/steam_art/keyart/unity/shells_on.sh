#!/bin/bash
# shells_on.sh N -> C# snippet swapping in an N-shell pile mesh
cat <<CS
{
var cr = UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.CarpetRenderer>();
var mf = cr.Shells.GetComponent<UnityEngine.MeshFilter>();
var sz = CarpetCleaning.CarpetSimulation.CarpetSizeM;
if (mf.sharedMesh != null && mf.sharedMesh.name == "StoreShotShells") UnityEngine.Object.Destroy(mf.sharedMesh);
var nm = CarpetCleaning.ShellMeshBuilder.BuildShells(sz.x, sz.y, $1, cr.shellSubdivX, cr.shellSubdivZ, cr.pileHeightM);
nm.name = "StoreShotShells"; nm.hideFlags = UnityEngine.HideFlags.DontSave;
mf.sharedMesh = nm;
}
CS
