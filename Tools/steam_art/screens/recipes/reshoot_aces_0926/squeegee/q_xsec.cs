// read-only: film h + murk across the lanes (perpendicular to heading -33.45 deg) at two stations
UnityEditor.EditorApplication.isPaused = true;
var sim = CarpetCleaning.CarpetDebugApi.Sim;
var rd = typeof(CarpetCleaning.CarpetDebugApi).GetMethod("ReadFloatRT", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
var flu = (UnityEngine.Color[])rd.Invoke(null, new object[]{ sim.Fluid });
var fib = (UnityEngine.Color[])rd.Invoke(null, new object[]{ sim.Fiber });
var dir = (UnityEngine.Color[])rd.Invoke(null, new object[]{ sim.Dirt });
int W = sim.Fluid.width, H = sim.Fluid.height;
System.Func<float,string> F = x => x.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
string o = "";
float[][] st = { new float[]{0.35f,0.612f}, new float[]{0.6f,0.4793f} };
foreach (var c in st) {
  o += "@(" + F(c[0]) + "," + F(c[1]) + "): ";
  for (int k = -8; k <= 18; k++) {
    float s = k * 0.25f; // units of half blade (158.5 mm), + = toward lane B / upper-right
    float u = c[0] + 0.0728f * s, v = c[1] + 0.0881f * s;
    if (u < 0 || u >= 1 || v < 0 || v >= 1) { o += "x "; continue; }
    int x = (int)(u * W), y = (int)(v * H); int i = y * W + x;
    float h = flu[i].r, sl = flu[i].a, w = fib[i].a; var d = dir[i]; float pd = d.g + 0.5f*d.r + 0.5f*d.b + 0.5f*d.a;
    float wt = h + w * 6f; float murk = Mathf.Clamp01((sl/(wt+1e-3f) + pd/(wt+1e-3f))/0.4f);
    o += F(s) + ":" + F(h) + "/" + F(murk) + " ";
  }
  o += " || ";
}
return o;
