// clip4 r3 diag: force a FlyOut (Begin(true)) and walk it with real Steps; after each Step (whose Game-view render ran the
// clamp hook, if installed) record the lowest stage corner's world y in mm; then the same for a FlyIn. Snaps back to Idle.
UnityEditor.EditorApplication.isPaused = true;
var H = CarpetCleaning.CarpetDebugApi.Handover;
if (H.State != CarpetCleaning.HandoverState.Idle) CarpetCleaning.CarpetDebugApi.HandoverSnap();
var half = CarpetCleaning.CarpetSimulation.CarpetSizeM * 0.5f;
System.Func<float> lowMm = () => {
    var st = H.view.Stage; float m = 1e9f;
    for (int i = 0; i < 4; i++) m = UnityEngine.Mathf.Min(m, st.TransformPoint(new UnityEngine.Vector3((i & 1) == 0 ? -half.x : half.x, 0f, (i & 2) == 0 ? -half.y : half.y)).y);
    return m * 1000f; };
var sb = new System.Text.StringBuilder();
H.Begin(true);
float worst = 1e9f; int n = 0;
while (H.State == CarpetCleaning.HandoverState.FlyOut && n < 60) { UnityEditor.EditorApplication.Step(); n++; if (H.State == CarpetCleaning.HandoverState.FlyOut) { float y = lowMm(); worst = UnityEngine.Mathf.Min(worst, y); if (n % 4 == 1) sb.Append(y.ToString("0", System.Globalization.CultureInfo.InvariantCulture)).Append(' '); } }
sb.Append("| out worst ").Append(worst.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)).Append(" mm, steps ").Append(n).Append(" state ").Append(H.State).Append(" || in: ");
H.TakeNewCarpet();
worst = 1e9f; n = 0;
while (H.State == CarpetCleaning.HandoverState.FlyIn && n < 60) { UnityEditor.EditorApplication.Step(); n++; if (H.State == CarpetCleaning.HandoverState.FlyIn) { float y = lowMm(); worst = UnityEngine.Mathf.Min(worst, y); if (n % 4 == 1) sb.Append(y.ToString("0", System.Globalization.CultureInfo.InvariantCulture)).Append(' '); } }
sb.Append("| in worst ").Append(worst.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)).Append(" mm, steps ").Append(n).Append(" state ").Append(H.State);
UnityEditor.EditorApplication.isPaused = true;
return sb.ToString();
