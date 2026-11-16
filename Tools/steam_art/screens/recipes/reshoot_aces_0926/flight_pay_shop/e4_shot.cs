UnityEditor.EditorApplication.isPaused = true;
var r = CarpetCleaning.StoreShotApi.Shot("10_payslip");
return r + " " + CarpetCleaning.CarpetDebugApi.HandoverJson() + " hint=" + CarpetCleaning.CarpetDebugApi.Sim.Hint;
