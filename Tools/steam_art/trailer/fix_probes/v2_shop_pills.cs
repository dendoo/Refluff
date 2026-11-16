// Fix (b) probe: open the shop, run real Steps for layout + the entrance, then compare every price label's centre with its
// pill's centre (.ubuy > .ubuy__label, tool rows and the two INSPECTION cards). Before the fix the label hugged the top
// (dy about -6 px on a 30 px pill). Leaves the shop OPEN and the Editor paused for a CaptureHud; close with v2b.
CarpetCleaning.EconomyDebugApi.ShopOpen(true);
UnityEditor.EditorApplication.isPaused = true;
for (int i = 0; i < 40; i++) UnityEditor.EditorApplication.Step();
var hud = UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.CleaningHUD>();
var root = hud.GetComponent<UnityEngine.UIElements.UIDocument>().rootVisualElement;
var c = System.Globalization.CultureInfo.InvariantCulture;
float worstDy = 0f, worstDx = 0f; int n = 0; string sample = "";
foreach (var b in UnityEngine.UIElements.UQueryExtensions.Query<UnityEngine.UIElements.Button>(root, null, "ubuy").ToList())
{
    var l = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Label>(b, null, "ubuy__label");
    if (l == null || float.IsNaN(b.worldBound.height) || b.worldBound.height <= 0f) continue;
    float s = b.worldBound.height / UnityEngine.Mathf.Max(b.layout.height, 1e-3f);   // undo the entrance / panel scale
    float dy = (l.worldBound.center.y - b.worldBound.center.y) / s, dx = (l.worldBound.center.x - b.worldBound.center.x) / s;
    if (UnityEngine.Mathf.Abs(dy) > UnityEngine.Mathf.Abs(worstDy)) worstDy = dy;
    if (UnityEngine.Mathf.Abs(dx) > UnityEngine.Mathf.Abs(worstDx)) worstDx = dx;
    if (n == 0) sample = "'" + l.text + "' pill " + b.layout.height.ToString("0.0", c) + " label " + l.layout.height.ToString("0.0", c) + " top " + l.layout.y.ToString("0.0", c);
    n++;
}
return "pills " + n + " | worst dy " + worstDy.ToString("0.00", c) + " px, dx " + worstDx.ToString("0.00", c) + " px (want |d| < 0.5) | " + sample;
