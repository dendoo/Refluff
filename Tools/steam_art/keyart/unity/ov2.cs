var r = "";
int[] sds = {14, 102, 240, 325, 102, 240};
float[] lv = {0.99f, 0.75f, 0.75f, 0.75f, 0.9f, 0.9f};
{
var L = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
L.transform.rotation = UnityEngine.Quaternion.Euler(42f, 300f, 0f);
L.intensity = 3.5f; L.colorTemperature = 4700f; L.shadows = UnityEngine.LightShadows.Soft;
}
for (int i = 0; i < sds.Length; i++) {
  CarpetCleaning.CarpetDebugApi.Reset(sds[i], lv[i]);
  CarpetCleaning.CarpetDebugApi.RunFrames(2);
  r += CarpetCleaning.StoreShotApi.RenderView("keyart/test3/d_" + i + ".png", 640, 333, 0.12f, 0.60f, -0.95f, 38f, -6f, 0f, 25f);
}
{
var L0 = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
L0.transform.rotation = UnityEngine.Quaternion.Euler(50f, 330f, 0f);
L0.intensity = 3f; L0.colorTemperature = 5000f; L0.shadows = UnityEngine.LightShadows.None;
UnityEngine.RenderSettings.ambientSkyColor = new UnityEngine.Color(0.55f, 0.58f, 0.66f);
}
return "ok";
