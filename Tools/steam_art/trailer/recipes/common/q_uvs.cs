var sb = new System.Text.StringBuilder();
float[][] pts = { new[]{0.60f,0.48f}, new[]{0.40f,0.45f}, new[]{0.48f,0.62f}, new[]{0.02f,0.30f}, new[]{0.5f,0.5f}, new[]{0.5f,0.36f}, new[]{-0.16f,0.25f}, new[]{0.42f,0.29f}, new[]{0.53f,0.70f} };
foreach (var p in pts) { var j = CarpetCleaning.StoreGifApi.UvPixelJson(p[0], p[1], 3840, 2160); sb.Append(p[0].ToString(System.Globalization.CultureInfo.InvariantCulture) + "/" + p[1].ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + j.Substring(0, j.IndexOf(",\"w\"")) + "} "); }
return sb.ToString();
