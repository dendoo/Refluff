if(UnityEditor.EditorApplication.isPlaying || UnityEditor.BuildPipeline.isBuildingPlayer) throw new System.InvalidOperationException("Idle Editor required");
UnityEditor.EditorUtility.RequestScriptReload();
return "Requested Editor-only domain reload after successful build";
