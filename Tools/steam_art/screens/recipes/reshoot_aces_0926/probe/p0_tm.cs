var sb = new System.Text.StringBuilder();
foreach (var v in UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Volume>(UnityEngine.FindObjectsSortMode.None)) {
  var p = v.sharedProfile; if (p == null) continue;
  UnityEngine.Rendering.Universal.Tonemapping tm;
  if (p.TryGet(out tm)) sb.Append(v.name + ": tm active=" + tm.active + " mode=" + tm.mode.value + " overridden=" + tm.mode.overrideState + "; ");
  UnityEngine.Rendering.Universal.ColorAdjustments ca;
  if (p.TryGet(out ca)) sb.Append("postExp=" + ca.postExposure.value + " contrast=" + ca.contrast.value + " sat=" + ca.saturation.value + "; ");
}
var st = UnityEngine.Rendering.VolumeManager.instance.stack;
var t2 = st.GetComponent<UnityEngine.Rendering.Universal.Tonemapping>();
sb.Append("stack mode=" + t2.mode.value);
return sb.ToString();
