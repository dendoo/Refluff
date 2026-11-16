CarpetCleaning.CarpetDebugApi.PinToolHead(4, 0.35f, 0.31f, true);
CarpetCleaning.CarpetDebugApi.RunFrames(40);
UnityEngine.Transform bt = null; foreach (UnityEngine.Transform c in CarpetCleaning.CarpetDebugApi.Heads.transform) if (c.gameObject.activeSelf) bt = c;
bt.localPosition = new UnityEngine.Vector3(bt.localPosition.x, 0.010f + 0.006f, bt.localPosition.z);
var mpb = new UnityEngine.MaterialPropertyBlock(); mpb.SetFloat("roughnessFactor", 0.28f);
foreach (UnityEngine.Transform c in bt) if (c.name != "Spin") foreach (var rr in c.GetComponentsInChildren<UnityEngine.Renderer>(true)) rr.SetPropertyBlock(mpb);
{
var L = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
L.transform.rotation = UnityEngine.Quaternion.Euler(25f, 80f, 0f);
L.intensity = 5.0f; L.colorTemperature = 4800f; L.shadows = UnityEngine.LightShadows.Soft;
UnityEngine.RenderSettings.ambientSkyColor = new UnityEngine.Color(0.62f, 0.65f, 0.72f);
}
CarpetCleaning.StoreShotApi.RenderView("keyart/test3/wc_0.png", 960, 500, -0.216f, 0.485f, -0.841f, 38f, 10f, 0f, 25f);
CarpetCleaning.StoreShotApi.RenderView("keyart/test3/wc_1.png", 960, 500, -0.293f, 0.485f, -0.83f, 38f, 18f, 0f, 25f);
CarpetCleaning.StoreShotApi.RenderView("keyart/test3/wc_2.png", 960, 500, -0.262f, 0.445f, -0.867f, 34f, 14f, 0f, 25f);
CarpetCleaning.StoreShotApi.RenderView("keyart/test3/wc_3.png", 960, 500, -0.246f, 0.525f, -0.807f, 42f, 14f, 0f, 25f);
{
var L0 = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
L0.transform.rotation = UnityEngine.Quaternion.Euler(50f, 330f, 0f);
L0.intensity = 3f; L0.colorTemperature = 5000f; L0.shadows = UnityEngine.LightShadows.None;
UnityEngine.RenderSettings.ambientSkyColor = new UnityEngine.Color(0.55f, 0.58f, 0.66f);
}
return "ok";
