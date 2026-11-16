// read-only: the UpdateHint ratios against the carpet's starting stats
var sim = CarpetCleaning.CarpetDebugApi.Sim;
var s = sim.RefreshStatsSync();
var fi = typeof(CarpetCleaning.CarpetSimulation).GetField("_initStats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var i0 = (CarpetCleaning.SimStats)fi.GetValue(sim);
System.Func<float,string> F = x => x.ToString("G4", System.Globalization.CultureInfo.InvariantCulture);
return "init mud " + F(i0.mud) + " stain " + F(i0.stain) + " grit " + F(i0.grit) + " crust " + F(i0.crust)
 + " | mudR " + F(s.mud / Mathf.Max(i0.mud,1e-6f)) + " stainR " + F(s.stain / Mathf.Max(i0.stain,1e-6f)) + " gritR " + F(i0.grit > 1e-4f ? s.grit / i0.grit : 0f) + " grit " + F(s.grit)
 + " slurry " + F(s.slurry) + " pooled " + F(s.pooledCells) + " wet " + F(sim.Wetness) + " soapLeft " + F(sim.SoapLeft) + " foam " + F(sim.FoamMean) + " clean " + F(sim.Cleanliness) + " hint=" + sim.Hint;
