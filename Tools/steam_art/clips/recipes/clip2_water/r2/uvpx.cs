// clip2 r2 diagnostic: render px (3840x2160) of carpet points (u, v) near the action, to size the puddle against the crop.
var sb = new System.Text.StringBuilder();
float[] us = {0.3f, 0.4f, 0.5f, 0.6f, 0.7f};
float[] vs = {0.15f, 0.2f, 0.25f, 0.3f, 0.35f, 0.4f, 0.45f, 0.5f, 0.55f};
foreach (var v in vs) { sb.Append("v").Append(v).Append(":"); foreach (var u in us) { var j = CarpetCleaning.StoreGifApi.UvPixelJson(u, v, 3840, 2160); int a = j.IndexOf("\"x\":")+4, b = j.IndexOf(",\"w\""); sb.Append(" ").Append(j.Substring(a, b-a).Replace("\"y\":","")); } sb.Append(" | "); }
return sb.ToString();
