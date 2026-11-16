{
var L = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
L.transform.rotation = UnityEngine.Quaternion.Euler(32f, 300f, 0f);
L.intensity = 3.4f; L.colorTemperature = 4600f; L.shadows = UnityEngine.LightShadows.Soft;
}
