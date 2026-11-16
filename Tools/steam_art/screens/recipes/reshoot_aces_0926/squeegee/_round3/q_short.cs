// read-only: compact sim + floor numbers
var sim = CarpetCleaning.CarpetDebugApi.Sim;
var s = sim.RefreshStatsSync();
var fi = typeof(CarpetCleaning.CarpetSimulation).GetField("_initStats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var i0 = (CarpetCleaning.SimStats)fi.GetValue(sim);
System.Func<float,string> F = x => x.ToString("G3", System.Globalization.CultureInfo.InvariantCulture);
var fl = CarpetCleaning.CarpetDebugApi.Floor.FloorStatsSync();
return "mudR " + F(s.mud / Mathf.Max(i0.mud,1e-6f)) + " stainR " + F(s.stain / Mathf.Max(i0.stain,1e-6f)) + " slurry " + F(s.slurry) + " pooled " + F(s.pooledCells)
 + " wet " + F(sim.Wetness) + " soap " + F(sim.SoapLeft) + " foam " + F(sim.FoamMean) + " clean " + F(sim.Cleanliness) + " | floor water " + F(fl.water) + " dirty " + F(fl.dirtyCells) + " foam " + F(fl.foam) + " | " + sim.Hint;
