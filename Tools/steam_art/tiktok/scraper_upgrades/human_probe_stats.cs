CarpetCleaning.CarpetDebugApi.SaveDebugView("dirt","SteamStore/_work/tiktok_scraper/retake/qa/replay_dirt.png");
var t = CarpetCleaning.ToolId.Scraper;
return "power="+CarpetCleaning.ToolTuning.Power(t).ToString(System.Globalization.CultureInfo.InvariantCulture)+" area="+CarpetCleaning.ToolTuning.Area(t).ToString(System.Globalization.CultureInfo.InvariantCulture)+" "+CarpetCleaning.CarpetDebugApi.StatsJson();
