{
var L = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
L.transform.rotation = UnityEngine.Quaternion.Euler(56f, 300f, 0f);
L.intensity = 3.5f; L.colorTemperature = 4900f; L.shadows = UnityEngine.LightShadows.Soft;
}
CarpetCleaning.StoreShotApi.RenderView("keyart/test/ta1.png", 400, 600, 0.0f, 2.0f, -0.30f, 90f, -90f, 0f, 36f);
CarpetCleaning.StoreShotApi.RenderView("keyart/test/ta2.png", 400, 600, 0.25f, 1.9f, -0.30f, 80f, -90f, 0f, 36f);
{
var L = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
L.transform.rotation = UnityEngine.Quaternion.Euler(42f, 300f, 0f);
L.intensity = 3.5f; L.colorTemperature = 4700f; L.shadows = UnityEngine.LightShadows.Soft;
}
CarpetCleaning.StoreShotApi.RenderView("keyart/test/tb1.png", 400, 600, 0.346f, 0.59f, -0.425f, 53f, -82f, 0f, 40f);
CarpetCleaning.StoreShotApi.RenderView("keyart/test/tb2.png", 400, 600, 0.477f, 0.65f, -0.461f, 48f, -80f, 0f, 32f);
{
var L0 = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
L0.transform.rotation = UnityEngine.Quaternion.Euler(50f, 330f, 0f);
L0.intensity = 3f; L0.colorTemperature = 5000f; L0.shadows = UnityEngine.LightShadows.None;
UnityEngine.RenderSettings.ambientSkyColor = new UnityEngine.Color(0.55f, 0.58f, 0.66f);
}
return "ok";
