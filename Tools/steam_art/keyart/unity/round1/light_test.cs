var L = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
string info = "useTemp=" + L.useColorTemperature + " shadowStr=" + L.shadowStrength + " bias=" + L.shadowBias;
// baseline
CarpetCleaning.StoreShotApi.RenderView("keyart/test/l0.png", 960, 540, -0.15f, 0.55f, -0.95f, 38f, 10f, 0f, 28f);
// raking from left, low
L.transform.rotation = UnityEngine.Quaternion.Euler(18f, 70f, 0f);
L.intensity = 3.2f;
UnityEngine.RenderSettings.ambientSkyColor = new UnityEngine.Color(0.30f, 0.32f, 0.38f);
CarpetCleaning.StoreShotApi.RenderView("keyart/test/l1.png", 960, 540, -0.15f, 0.55f, -0.95f, 38f, 10f, 0f, 28f);
// raking from back-left (against the view)
L.transform.rotation = UnityEngine.Quaternion.Euler(20f, 150f, 0f);
CarpetCleaning.StoreShotApi.RenderView("keyart/test/l2.png", 960, 540, -0.15f, 0.55f, -0.95f, 38f, 10f, 0f, 28f);
// restore
L.transform.rotation = UnityEngine.Quaternion.Euler(50f, 330f, 0f);
L.intensity = 3f;
UnityEngine.RenderSettings.ambientSkyColor = new UnityEngine.Color(0.55f, 0.58f, 0.66f);
return info;
