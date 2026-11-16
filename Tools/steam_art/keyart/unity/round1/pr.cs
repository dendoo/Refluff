{
var L = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
L.transform.rotation = UnityEngine.Quaternion.Euler(20f, 250f, 0f);
L.intensity = 3.8f; L.colorTemperature = 4200f; L.shadows = UnityEngine.LightShadows.Soft;
UnityEngine.RenderSettings.ambientSkyColor = new UnityEngine.Color(0.42f, 0.45f, 0.54f);
}
CarpetCleaning.StoreShotApi.RenderView("keyart/test/pr1.png", 960, 500, 0.30f, 0.28f, -0.50f, 30f, 0f, 0f, 26f);
{
var L = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
L.transform.rotation = UnityEngine.Quaternion.Euler(42f, 300f, 0f);
L.intensity = 3.5f; L.colorTemperature = 4700f; L.shadows = UnityEngine.LightShadows.Soft;
}
CarpetCleaning.StoreShotApi.RenderView("keyart/test/pr0.png", 960, 500, 0.30f, 0.28f, -0.50f, 30f, 0f, 0f, 26f);
{
var L0 = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
L0.transform.rotation = UnityEngine.Quaternion.Euler(50f, 330f, 0f);
L0.intensity = 3f; L0.colorTemperature = 5000f; L0.shadows = UnityEngine.LightShadows.None;
UnityEngine.RenderSettings.ambientSkyColor = new UnityEngine.Color(0.55f, 0.58f, 0.66f);
}
return "ok";
