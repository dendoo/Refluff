// Fix (a) probe, side-effect free: no Begin() (so no StateChanged / payout / CarpetAway), no carpet swap, no Steps.
// Puts the hand-over into FlyOut / FlyIn by reflection, walks Phase01 with Seek (-> ApplyPose), records the lowest stage
// corner (world y, mm; 0 = backing top at rest), then puts the stage back exactly as it was. Before the fix: out -77.9, in -5.6.
var H = CarpetCleaning.CarpetDebugApi.Handover;
if (H == null || H.view == null) return "no handover";
if (H.State != CarpetCleaning.HandoverState.Idle) return "not Idle (" + H.State + "): run CarpetDebugApi.HandoverSnap() first";
var st = H.view.Stage;
var half = CarpetCleaning.CarpetSimulation.CarpetSizeM * 0.5f;
System.Func<float> low = () => { float m = 1e9f; for (int i = 0; i < 4; i++) m = UnityEngine.Mathf.Min(m, st.TransformPoint(new UnityEngine.Vector3((i & 1) == 0 ? -half.x : half.x, 0f, (i & 2) == 0 ? -half.y : half.y)).y); return m * 1000f; };
System.Func<UnityEngine.Quaternion, float> liftOf = r => { float m = 0f; for (int i = 0; i < 4; i++) m = UnityEngine.Mathf.Min(m, (r * new UnityEngine.Vector3((i & 1) == 0 ? -half.x : half.x, 0f, (i & 2) == 0 ? -half.y : half.y)).y); return -m; };   // = CarpetHandover.CornerLift
var stateProp = typeof(CarpetCleaning.CarpetHandover).GetProperty("State");
var phaseProp = typeof(CarpetCleaning.CarpetHandover).GetProperty("Phase01");
float rest = low();
float[] worst = { 1e9f, 1e9f }; float[] atT = { 0f, 0f }; float[] maxLiftMm = { 0f, 0f };
var legs = new[] { CarpetCleaning.HandoverState.FlyOut, CarpetCleaning.HandoverState.FlyIn };
for (int g = 0; g < 2; g++)
{
    stateProp.SetValue(H, legs[g]);
    for (int k = 0; k <= 200; k++)
    {
        H.Seek(k / 200f);
        float y = low();
        if (y < worst[g]) { worst[g] = y; atT[g] = k / 200f; }
        maxLiftMm[g] = UnityEngine.Mathf.Max(maxLiftMm[g], liftOf(st.localRotation) * 1000f);
    }
}
H.Seek(1f); float inEnd = low();
stateProp.SetValue(H, CarpetCleaning.HandoverState.Idle); phaseProp.SetValue(H, 0f);
st.localPosition = UnityEngine.Vector3.zero; st.localRotation = UnityEngine.Quaternion.identity;
var c = System.Globalization.CultureInfo.InvariantCulture;
return "rest " + rest.ToString("0.0", c) + " mm | out worst " + worst[0].ToString("0.0", c) + " mm @t " + atT[0].ToString("0.00", c)
     + " (lift up to " + maxLiftMm[0].ToString("0", c) + " mm) | in worst " + worst[1].ToString("0.0", c) + " mm @t " + atT[1].ToString("0.00", c)
     + " (lift up to " + maxLiftMm[1].ToString("0.0", c) + " mm) | in t=1 " + inEnd.ToString("0.0", c) + " mm | lift(identity) "
     + liftOf(UnityEngine.Quaternion.identity).ToString("0.000", c) + " | back to " + H.State + " " + low().ToString("0.0", c) + " mm";
