// clip2 diagnostic: render px (3840x2160) of carpet points (u, v), to place the crop.
var sb = new System.Text.StringBuilder();
float[] us = {0f, 0.25f, 0.5f, 0.75f, 1f};
float[] vs = {0f, 0.2f, 0.4f, 0.5f, 0.6f, 0.8f, 1f};
foreach (var v in vs) { sb.Append("v").Append(v).Append(":"); foreach (var u in us) { var j = CarpetCleaning.StoreGifApi.UvPixelJson(u, v, 3840, 2160); int a = j.IndexOf("\"x\":")+4, b = j.IndexOf(",\"w\""); sb.Append(" ").Append(j.Substring(a, b-a).Replace("\"y\":","")); } sb.Append(" | "); }
return sb.ToString();
