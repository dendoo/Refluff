var sim = CarpetCleaning.CarpetDebugApi.Sim;
string st = CarpetCleaning.CarpetDebugApi.StatsJson();
return "{\"hint\":\"" + sim.Hint + "\",\"clean\":" + sim.Cleanliness.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + ",\"wet\":" + sim.Wetness.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + ",\"ho\":" + CarpetCleaning.CarpetDebugApi.HandoverJson() + ",\"dirt\":" + CarpetCleaning.CarpetDebugApi.DirtJson() + ",\"stats\":" + st + "}";
