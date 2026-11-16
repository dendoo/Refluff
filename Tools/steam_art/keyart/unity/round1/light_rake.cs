{
var L = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
L.transform.rotation = UnityEngine.Quaternion.Euler(20f, 250f, 0f);
L.intensity = 3.8f; L.colorTemperature = 4200f; L.shadows = UnityEngine.LightShadows.Soft;
UnityEngine.RenderSettings.ambientSkyColor = new UnityEngine.Color(0.42f, 0.45f, 0.54f);
}
