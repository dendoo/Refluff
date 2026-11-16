{
var L = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
L.transform.rotation = UnityEngine.Quaternion.Euler(24f, 190f, 0f);
L.intensity = 3.6f; L.colorTemperature = 4500f; L.shadows = UnityEngine.LightShadows.Soft;
}
