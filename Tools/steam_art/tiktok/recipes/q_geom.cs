// Game camera pose + carpet UV corners in world space (for the free-camera paths).
var C = System.Globalization.CultureInfo.InvariantCulture;
var tc = CarpetCleaning.CarpetDebugApi.Tools;
var root = tc.carpetCollider != null ? tc.carpetCollider.transform : tc.transform;
var sz = CarpetCleaning.CarpetSimulation.CarpetSizeM;
var sb = new System.Text.StringBuilder(CarpetCleaning.StoreShotApi.CameraJson());
sb.Append(" root=" + root.name + " pos=" + root.position.ToString("F4") + " rot=" + root.eulerAngles.ToString("F2") + " size=" + sz.ToString("F3"));
var sun = UnityEngine.GameObject.Find("Directional Light").GetComponent<UnityEngine.Light>();
sb.Append(" sun=" + sun.transform.eulerAngles.ToString("F1") + " I=" + sun.intensity.ToString(C) + " K=" + sun.colorTemperature.ToString(C) + " amb=" + UnityEngine.RenderSettings.ambientSkyColor);
return sb.ToString();
