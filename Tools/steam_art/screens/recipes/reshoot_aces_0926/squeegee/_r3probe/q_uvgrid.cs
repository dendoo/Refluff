// read-only: 2560x1440 game-camera pixel of a u/v grid (corners, edges)
var sb = new System.Text.StringBuilder();
float[] us = {0f, 0.25f, 0.5f, 0.75f, 1f, 1.15f};
float[] vs = {-0.1f, 0f, 0.15f, 0.3f, 0.5f, 0.7f, 0.85f, 1f, 1.1f};
foreach (var v in vs) { sb.Append("v" + v + ":"); foreach (var u in us) { var j = CarpetCleaning.StoreGifApi.UvPixelJson(u, v, 2560, 1440); var a = j.IndexOf("\"x\":")+4; var b = j.IndexOf(",\"w\""); sb.Append(" " + j.Substring(a, b-a).Replace("\"y\":","")); } sb.Append(" | "); }
return sb.ToString();
