// clip3 diagnostic: render px (3840x2160) of the patch corners, the right rim and the floor beyond it
var sb = new System.Text.StringBuilder();
float[][] P = { new[]{0.55f,0.10f}, new[]{0.55f,0.45f}, new[]{0.60f,0.15f}, new[]{0.60f,0.38f}, new[]{0.95f,0.15f}, new[]{0.95f,0.38f}, new[]{1.0f,0.10f}, new[]{1.0f,0.26f}, new[]{1.0f,0.45f}, new[]{1.25f,0.26f}, new[]{0.78f,0.26f} };
foreach (var p in P) { string j = CarpetCleaning.StoreGifApi.UvPixelJson(p[0], p[1], 3840, 2160); sb.Append(p[0]).Append(",").Append(p[1]).Append(" ").Append(j.Substring(0, j.IndexOf(",\"w\""))).Append(" | "); }
return sb.ToString();
