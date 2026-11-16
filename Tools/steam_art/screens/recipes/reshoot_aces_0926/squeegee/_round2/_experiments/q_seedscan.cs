// read-only scan (debug, not part of the take): initial dirt stats for seeds x levels with a dust + grime mix, no stain wanted
var sim = CarpetCleaning.CarpetDebugApi.Sim;
var fi = typeof(CarpetCleaning.CarpetSimulation).GetField("_initStats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
System.Func<float,string> F = x => x.ToString("G3", System.Globalization.CultureInfo.InvariantCulture);
var sb = new System.Text.StringBuilder();
float[] levels = {0.18f, 0.24f, 0.30f};
for (int seed = 1; seed <= 30; seed++) {
  var d = CarpetCleaning.CarpetSimulation.RollDirt(seed, 0.5f);
  if (d.spread < 1.05f) continue;
  foreach (var L in levels) {
    CarpetCleaning.CarpetDebugApi.ResetWithProfile(seed, L, 0f, 1f, 1f, 0f);
    var i0 = (CarpetCleaning.SimStats)fi.GetValue(sim);
    sb.Append(seed).Append(" sp ").Append(F(d.spread)).Append(" L ").Append(F(L)).Append(" lvl ").Append(F(sim.CarpetDirt.level)).Append(": stain ").Append(F(i0.stain)).Append(" grit ").Append(F(i0.grit)).Append(" mud ").Append(F(i0.mud)).Append(" | ");
  }
}
return sb.ToString();
