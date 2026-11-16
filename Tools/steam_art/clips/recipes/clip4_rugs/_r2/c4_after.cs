// clip4 check after a take: hand-over state, the carpet on the stage, the last payout
return CarpetCleaning.CarpetDebugApi.HandoverJson() + " " + CarpetCleaning.CarpetDebugApi.DirtJson() + " last=" + CarpetCleaning.EconomyDebugApi.Eco.LastPayout.ToJson();
