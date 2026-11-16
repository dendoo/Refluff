// clip3 diagnostic: render px (1920x1080) of a u/v grid (step 0.1) -> "u,v:x,y;" list
var sb = new System.Text.StringBuilder();
for (int iu = 0; iu <= 10; iu++) for (int iv = 0; iv <= 10; iv++) {
  float u = iu * 0.1f, v = iv * 0.1f;
  string j = CarpetCleaning.StoreGifApi.UvPixelJson(u, v, 1920, 1080);
  int a = j.IndexOf("\"x\":") + 4, b = j.IndexOf(",\"y\":"), c = j.IndexOf(",\"w\"");
  sb.Append(u.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)).Append(',').Append(v.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)).Append(':').Append(j.Substring(a, b - a)).Append(',').Append(j.Substring(b + 5, c - b - 5)).Append(';');
}
return sb.ToString();
