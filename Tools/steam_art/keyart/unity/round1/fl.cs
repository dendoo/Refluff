{
var L = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
L.transform.rotation = UnityEngine.Quaternion.Euler(56f, 300f, 0f);
L.intensity = 3.5f; L.colorTemperature = 4900f; L.shadows = UnityEngine.LightShadows.Soft;
}
CarpetCleaning.StoreShotApi.RenderView("keyart/test/fl1.png", 960, 500, 0.0f, 1.4f, -0.547f, 80f, 0f, 0f, 23.1f);
CarpetCleaning.StoreShotApi.RenderView("keyart/test/fl2.png", 960, 500, 0.02f, 1.3f, -0.62f, 74f, 0f, 0f, 24f);
{
var L0 = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
L0.transform.rotation = UnityEngine.Quaternion.Euler(50f, 330f, 0f);
L0.intensity = 3f; L0.colorTemperature = 5000f; L0.shadows = UnityEngine.LightShadows.None;
UnityEngine.RenderSettings.ambientSkyColor = new UnityEngine.Color(0.55f, 0.58f, 0.66f);
}
return "ok";
