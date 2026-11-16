// clip1 diagnostic: render px (3840x2160) of carpet points near the right rim, to place the off-frame turn.
var sb = new System.Text.StringBuilder();
float[][] P = { new[]{1.0f,0.30f}, new[]{1.0f,0.42f}, new[]{1.06f,0.30f}, new[]{1.10f,0.42f}, new[]{0.0f,0.30f}, new[]{0.0f,0.42f}, new[]{0.90f,0.30f}, new[]{0.5f,0.36f} };
foreach (var p in P) sb.Append(p[0]).Append(",").Append(p[1]).Append(" ").Append(CarpetCleaning.StoreGifApi.UvPixelJson(p[0], p[1], 3840, 2160)).Append(" | ");
return sb.ToString();
