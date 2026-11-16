var sb = new System.Text.StringBuilder();
foreach (var l in UnityEngine.Object.FindObjectsByType<UnityEngine.Light>(UnityEngine.FindObjectsInactive.Include)) {
  sb.Append(l.name).Append(" type=").Append(l.type).Append(" en=").Append(l.enabled).Append(" int=").Append(l.intensity.ToString(System.Globalization.CultureInfo.InvariantCulture))
    .Append(" col=").Append(l.color).Append(" euler=").Append(l.transform.eulerAngles).Append(" pos=").Append(l.transform.position).Append(" shadows=").Append(l.shadows).Append(" temp=").Append(l.colorTemperature).Append("\n");
}
foreach (var v in UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Volume>(UnityEngine.FindObjectsInactive.Include)) {
  sb.Append("VOL ").Append(v.name).Append(" global=").Append(v.isGlobal).Append(" prio=").Append(v.priority).Append(" w=").Append(v.weight).Append(" prof=").Append(v.sharedProfile ? v.sharedProfile.name : "null").Append(" comps=");
  if (v.sharedProfile) foreach (var c in v.sharedProfile.components) sb.Append(c.GetType().Name).Append(c.active?"+":"-").Append(",");
  sb.Append("\n");
}
sb.Append("ambient mode=").Append(UnityEngine.RenderSettings.ambientMode).Append(" int=").Append(UnityEngine.RenderSettings.ambientIntensity).Append(" sky=").Append(UnityEngine.RenderSettings.ambientSkyColor).Append(" eq=").Append(UnityEngine.RenderSettings.ambientEquatorColor).Append(" gr=").Append(UnityEngine.RenderSettings.ambientGroundColor).Append(" fog=").Append(UnityEngine.RenderSettings.fog).Append("\n");
sb.Append(CarpetCleaning.CarpetDebugApi.HandoverJson()).Append("\n").Append(CarpetCleaning.StoreShotApi.CameraJson());
return sb.ToString();
