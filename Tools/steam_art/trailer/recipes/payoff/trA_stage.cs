// Take A staging (#04 arrival): kit, a forced $0 hand-over of a spotless rug so the stage is Away (bare tile, "Take a new
// carpet"), the card killed, $1,840 / 6 delivered, the Anatolian star at the front of the picture bag.
var H = CarpetCleaning.CarpetDebugApi.Handover;
if (H.State != CarpetCleaning.HandoverState.Idle) CarpetCleaning.CarpetDebugApi.HandoverSnap();
CarpetCleaning.CarpetDebugApi.Reset(394, 0f);
CarpetCleaning.CarpetDebugApi.HandoverNow(true);
int n = 0;
for (; n < 150 && H.State != CarpetCleaning.HandoverState.Away; n++) UnityEditor.EditorApplication.Step();
if (H.State != CarpetCleaning.HandoverState.Away) return "ERROR not Away after " + n + " steps: " + CarpetCleaning.CarpetDebugApi.HandoverJson();
CarpetCleaning.EconomyDebugApi.Hud.Payout.Finish();
CarpetCleaning.EconomyDebugApi.PayoutResume();
var E = CarpetCleaning.EconomyDebugApi.Eco;
CarpetCleaning.EconomyDebugApi.Grant(1840 - E.Data.money);
E.Data.carpetsDelivered = 6;
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var bag = (System.Collections.Generic.List<int>)typeof(CarpetCleaning.CarpetHandover).GetField("_picBag", bf).GetValue(H);
int ai = System.Array.FindIndex(H.pictures, p => p != null && p.name == "rug_anatolian_star");
bag.Remove(ai); bag.Insert(0, ai);
for (int i = 0; i < 30; i++) UnityEditor.EditorApplication.Step();
UnityEditor.EditorApplication.isPaused = true;
return "away after " + n + " | money " + E.Data.money + " delivered " + E.Data.carpetsDelivered + " bag " + string.Join(",", bag) + " | " + CarpetCleaning.CarpetDebugApi.HandoverJson().Substring(0, 90);
