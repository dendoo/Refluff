CarpetCleaning.CarpetDebugApi.PinToolHead(4, 0.39f, 0.30f, true);
CarpetCleaning.CarpetDebugApi.RunFrames(40);
UnityEngine.Transform bt = null; foreach (UnityEngine.Transform c in CarpetCleaning.CarpetDebugApi.Heads.transform) if (c.gameObject.activeSelf) bt = c;
bt.localPosition = new UnityEngine.Vector3(bt.localPosition.x, 0.010f + 0.008f, bt.localPosition.z);
var mpb = new UnityEngine.MaterialPropertyBlock(); mpb.SetFloat("roughnessFactor", 0.28f);
foreach (UnityEngine.Transform c in bt) if (c.name != "Spin") foreach (var rr in c.GetComponentsInChildren<UnityEngine.Renderer>(true)) rr.SetPropertyBlock(mpb);
{
var L = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
L.transform.rotation = UnityEngine.Quaternion.Euler(42f, 300f, 0f);
L.intensity = 4.3f; L.colorTemperature = 4900f; L.shadows = UnityEngine.LightShadows.Soft;
UnityEngine.RenderSettings.ambientSkyColor = new UnityEngine.Color(0.62f, 0.65f, 0.72f);
}
CarpetCleaning.StoreShotApi.RenderView("keyart/test3/lt_0.png", 960, 500, -0.111f, 0.445f, -0.774f, 40f, 4f, 0f, 25f);
{
var L = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
L.transform.rotation = UnityEngine.Quaternion.Euler(30f, 160f, 0f);
L.intensity = 4.5f; L.colorTemperature = 4800f; L.shadows = UnityEngine.LightShadows.Soft;
UnityEngine.RenderSettings.ambientSkyColor = new UnityEngine.Color(0.62f, 0.65f, 0.72f);
}
CarpetCleaning.StoreShotApi.RenderView("keyart/test3/lt_1.png", 960, 500, -0.111f, 0.445f, -0.774f, 40f, 4f, 0f, 25f);
{
var L = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
L.transform.rotation = UnityEngine.Quaternion.Euler(25f, 80f, 0f);
L.intensity = 4.5f; L.colorTemperature = 4700f; L.shadows = UnityEngine.LightShadows.Soft;
UnityEngine.RenderSettings.ambientSkyColor = new UnityEngine.Color(0.62f, 0.65f, 0.72f);
}
CarpetCleaning.StoreShotApi.RenderView("keyart/test3/lt_2.png", 960, 500, -0.111f, 0.445f, -0.774f, 40f, 4f, 0f, 25f);
{
var L = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
L.transform.rotation = UnityEngine.Quaternion.Euler(35f, 220f, 0f);
L.intensity = 4.5f; L.colorTemperature = 4800f; L.shadows = UnityEngine.LightShadows.Soft;
UnityEngine.RenderSettings.ambientSkyColor = new UnityEngine.Color(0.62f, 0.65f, 0.72f);
}
CarpetCleaning.StoreShotApi.RenderView("keyart/test3/lt_3.png", 960, 500, -0.111f, 0.445f, -0.774f, 40f, 4f, 0f, 25f);
{
var L0 = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
L0.transform.rotation = UnityEngine.Quaternion.Euler(50f, 330f, 0f);
L0.intensity = 3f; L0.colorTemperature = 5000f; L0.shadows = UnityEngine.LightShadows.None;
UnityEngine.RenderSettings.ambientSkyColor = new UnityEngine.Color(0.55f, 0.58f, 0.66f);
}
return "ok";
