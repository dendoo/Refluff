// Retake of the pay-slip capture: e4_shot ran right after e3_pay with no frames in between, so the floor-strip selection
// (Detergent pill) was mid-fade and the paused Steps had read the OS mouse (floor-detergent head at bottom-left).
// Scripted pointer off the play area = no head; card frozen at 6.7; 40 Steps finish the USS transitions.
UnityEditor.EditorApplication.isPaused = true;
var T = CarpetCleaning.CarpetDebugApi.Tools;
string r = "";
try {
  T.ScriptedPointer = true; T.ScriptedDown = false; T.ScriptedRight = false; T.ScriptedUv = new UnityEngine.Vector2(5f, 5f);
  CarpetCleaning.EconomyDebugApi.PayoutSeek(6.7f);
  for (int i = 0; i < 40; i++) UnityEditor.EditorApplication.Step();
  var pj = CarpetCleaning.EconomyDebugApi.PayoutJson();
  r = CarpetCleaning.StoreShotApi.Shot("10_payslip") + " payout=" + pj.Substring(0, 90);
} finally { T.ScriptedPointer = false; }
return r + " head=" + CarpetCleaning.CarpetDebugApi.ToolHeadJson().Substring(0, 40) + " " + CarpetCleaning.CarpetDebugApi.HandoverJson().Substring(0, 60);
