var c=UnityEngine.Camera.main;
var all=UnityEngine.Object.FindObjectsByType<UnityEngine.Camera>(UnityEngine.FindObjectsSortMode.None);
var w=UnityEngine.GameObject.Find("Workshop");
var door=w.transform.Find("ClosedDoor/ClosedLeaf").GetComponent<UnityEngine.MeshRenderer>();
var glass=w.transform.Find("SouthWindow/Glass0_0").GetComponent<UnityEngine.MeshRenderer>();
return new {camera=c.name,campos=c.transform.position.ToString(),culling=c.cullingMask,occlusion=c.useOcclusionCulling,fog=UnityEngine.RenderSettings.fog,fogDensity=UnityEngine.RenderSettings.fogDensity,distances=c.layerCullDistances,doorPos=door.bounds.ToString(),doorEnabled=door.enabled,doorActive=door.gameObject.activeInHierarchy,doorLayer=door.gameObject.layer,doorShader=door.sharedMaterial.shader.name,doorpass=door.sharedMaterial.GetShaderPassEnabled("UniversalForward"),glassEmission=glass.sharedMaterial.GetColor("_EmissionColor").ToString(),glassKeywords=glass.sharedMaterial.shaderKeywords, cameras=System.Array.ConvertAll(all,x=>x.name+" "+x.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>()?.renderType.ToString())};
