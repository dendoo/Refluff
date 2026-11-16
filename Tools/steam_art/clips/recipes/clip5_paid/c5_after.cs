// clip5_paid after the take: close the shop (the take ends with it open), stay paused; report what the take paid and bought
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.EconomyDebugApi.ShopOpen(false);
var E = CarpetCleaning.EconomyDebugApi.Eco;
return "money=" + E.Data.money + " carpets=" + E.Data.carpetsDelivered + " " + CarpetCleaning.CarpetDebugApi.HandoverJson() + " last=" + E.LastPayout.ToJson().Substring(0, 240);
