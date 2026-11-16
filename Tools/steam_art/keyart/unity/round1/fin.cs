CarpetCleaning.CarpetDebugApi.UnpinToolHead();
UnityEditor.EditorApplication.isPaused = false;
var L = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
var vols = UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Volume>(UnityEngine.FindObjectsInactive.Include);
string v = ""; foreach (var x in vols) v += x.name + ";";
return "euler=" + L.transform.eulerAngles + " int=" + L.intensity + " temp=" + L.colorTemperature + " shadows=" + L.shadows + " sky=" + UnityEngine.RenderSettings.ambientSkyColor + " vols=" + v + " paused=" + UnityEditor.EditorApplication.isPaused + " " + CarpetCleaning.StoreShotApi.CameraJson();
