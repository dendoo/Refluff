var sb = new System.Text.StringBuilder();
float[][] pts = { new[]{0.58f,0.217f}, new[]{0.92f,0.118f}, new[]{0.62f,0.205f}, new[]{0.90f,0.124f}, new[]{-0.12f,0.42f}, new[]{0.40f,0.269f}, new[]{0.955f,0.108f}, new[]{1.0f,0.207f}, new[]{1.10f,0.065f}, new[]{1.0f,0.0f}, new[]{1.0f,1.0f}, new[]{0f,0f} };
foreach (var p in pts) sb.Append("(" + p[0] + "," + p[1] + ")=" + CarpetCleaning.StoreGifApi.UvPixelJson(p[0], p[1], 3840, 2160).Replace("\"w\":3840,\"h\":2160,", "") + " ");
return sb.ToString();
