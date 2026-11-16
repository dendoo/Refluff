CarpetCleaning.CarpetDebugApi.PinToolHead(4, 0.39f, 0.30f, true);
var hv = CarpetCleaning.CarpetDebugApi.Heads.transform;
var sb = new System.Text.StringBuilder();
foreach (UnityEngine.Transform c in hv) if (c.gameObject.activeSelf) {
  sb.Append(c.name + " pos=" + c.localPosition.ToString("F4") + " world=" + c.position.ToString("F4") + " scale=" + c.localScale.ToString("F3") + "\n");
  var seen = new System.Collections.Generic.HashSet<UnityEngine.Material>();
  foreach (var r in c.GetComponentsInChildren<UnityEngine.Renderer>(true)) foreach (var m in r.sharedMaterials) if (m != null && seen.Add(m)) {
    sb.Append(" mat " + m.name + " sh=" + m.shader.name + " q=" + m.renderQueue);
    foreach (var p in new string[]{"_Smoothness","_Glossiness","_Metallic","_BaseColor","_roughnessFactor","_metallicFactor","baseColorFactor"}) if (m.HasProperty(p)) sb.Append(" " + p + "=" + (p.Contains("olor") ? m.GetColor(p).ToString() : m.GetFloat(p).ToString(System.Globalization.CultureInfo.InvariantCulture)));
    sb.Append("\n");
  }
  sb.Append(" renderers=" + c.GetComponentsInChildren<UnityEngine.Renderer>(true).Length + "\n");
}
return sb.ToString();
