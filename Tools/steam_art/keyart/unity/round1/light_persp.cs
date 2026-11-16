{
var L = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
L.transform.rotation = UnityEngine.Quaternion.Euler(42f, 300f, 0f);
L.intensity = 3.5f; L.colorTemperature = 4700f; L.shadows = UnityEngine.LightShadows.Soft;
}
