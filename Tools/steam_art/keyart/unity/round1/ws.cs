{
var L = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
L.transform.rotation = UnityEngine.Quaternion.Euler(24f, 190f, 0f);
L.intensity = 3.6f; L.colorTemperature = 4500f; L.shadows = UnityEngine.LightShadows.Soft;
}
CarpetCleaning.StoreShotApi.RenderView("keyart/test/ws1.png", 960, 500, 0.30f, 0.30f, -0.25f, 30f, 0f, 0f, 30f);
{
var L = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
L.transform.rotation = UnityEngine.Quaternion.Euler(42f, 300f, 0f);
L.intensity = 3.5f; L.colorTemperature = 4700f; L.shadows = UnityEngine.LightShadows.Soft;
}
CarpetCleaning.StoreShotApi.RenderView("keyart/test/ws2.png", 960, 500, 0.30f, 0.30f, -0.25f, 30f, 0f, 0f, 30f);
{
var L0 = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
L0.transform.rotation = UnityEngine.Quaternion.Euler(50f, 330f, 0f);
L0.intensity = 3f; L0.colorTemperature = 5000f; L0.shadows = UnityEngine.LightShadows.None;
UnityEngine.RenderSettings.ambientSkyColor = new UnityEngine.Color(0.55f, 0.58f, 0.66f);
}
return "ok";
