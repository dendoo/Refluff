// Fix (c) probe: the key chips the HUD strips and the shop cards actually built, the ToolTable slots and the key
// ToolController maps each slot to (keys cannot be faked from an eval, so the mapping is read by reflection).
var hud = UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.CleaningHUD>();
var root = hud.GetComponent<UnityEngine.UIElements.UIDocument>().rootVisualElement;
System.Func<string, string> chips = name => {
    var list = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(root, name);
    if (list == null) return name + ": missing";
    var sb = new System.Text.StringBuilder(name + ": ");
    foreach (var l in UnityEngine.UIElements.UQueryExtensions.Query<UnityEngine.UIElements.Label>(list, null, "kbd").ToList()) sb.Append('[').Append(l.text).Append(']');
    sb.Append(" plain=").Append(UnityEngine.UIElements.UQueryExtensions.Query<UnityEngine.UIElements.VisualElement>(list, null, "tool--plain").ToList().Count
                             + UnityEngine.UIElements.UQueryExtensions.Query<UnityEngine.UIElements.VisualElement>(list, null, "ucard__head--plain").ToList().Count);   // strip rows / shop card heads without a chip
    return sb.ToString(); };
var row = (UnityEngine.InputSystem.Key[])typeof(CarpetCleaning.ToolController).GetField("NumberRow", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(null);
var map = new System.Text.StringBuilder();
foreach (var d in CarpetCleaning.ToolTable.All)
    map.Append(d.name).Append('=').Append(d.hotkey == 10 ? "0" : d.hotkey.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append("->").Append(d.hotkey >= 1 && d.hotkey < row.Length ? row[d.hotkey].ToString() : "none").Append(' ');
return chips("carpet-tools") + " | " + chips("floor-tools") + " | " + chips("shop-carpet-tools") + " | " + chips("shop-floor-tools") + " || " + map;
