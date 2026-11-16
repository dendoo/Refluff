// ACES re-shoot 01, ROUND 3 (diagnostic): list the live chunks as (u, v, height mm, size mm, state), carpet uv (CMM / 1200, 1500).
UnityEditor.EditorApplication.isPaused = true;
var deb = UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.DebrisSystem>();
var req = UnityEngine.Rendering.AsyncGPUReadback.Request(deb.Particles);
req.WaitForCompletion();
var data = req.GetData<CarpetCleaning.DebrisParticle>();
var sb = new System.Text.StringBuilder();
var ci = System.Globalization.CultureInfo.InvariantCulture;
int n = 0;
for (int i = 0; i < data.Length; i++) {
  uint st = (data[i].kindState >> 8) & 0xFFu; if (st == 0u) continue;
  var p = data[i].pos; n++;
  sb.Append('(').Append((p.x/1200f).ToString("F3", ci)).Append(',').Append((p.z/1500f).ToString("F3", ci)).Append(',')
    .Append(p.y.ToString("F1", ci)).Append(',').Append(data[i].size.ToString("F1", ci)).Append(',').Append(st).Append(") ");
}
return n + " alive: " + sb;
