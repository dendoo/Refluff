// eval_file body. Run in Play after the builder and at least two real EditorApplication.Step frames.
// Each invocation checks five languages (HUD + visible workbench prompt + shop); repeat seven times.
// Cursor: SessionState "Refluff.FpsUiVerify.Next". Set it to 0 to restart. No save/economy mutations.
// The prompt is temporarily presented directly to test layout while the Editor lacks mouse focus;
// actual focus/reach authorization is separately testable through UpgradeWorkbench.IsAimedAt.
if (!UnityEngine.Application.isPlaying) return "{\"error\":\"Enter Play before UI verification\"}";
var hud = UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.EconomyHud>();
var fp = UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.FirstPersonHud>();
var tools = UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.ToolController>();
var sim = UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.CarpetSimulation>();
var menu = UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.PauseMenu>();
if (hud == null || fp == null || tools == null || sim == null || !tools.FirstPersonActive)
    return "{\"error\":\"First-person scene is not ready\"}";
if (hud.ShopOpen || (menu != null && menu.IsOpen) || tools.UiModal || tools.Suspended)
    return "{\"error\":\"Close modals and the Steam overlay before UI verification\"}";
var document = hud.GetComponent<UnityEngine.UIElements.UIDocument>();
var tree = document.rootVisualElement;
System.Func<string, UnityEngine.UIElements.VisualElement> element = n => UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(tree, n);
var root = element("hud");
var overlay = element("fp-overlay");
var prompt = element("fp-workbench");
var toolName = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Label>(tree, "fp-tool-name");
if (root == null || overlay == null || prompt == null || toolName == null || tree.panel == null)
    return "{\"error\":\"HUD has not built\"}";
const string cursorKey = "Refluff.FpsUiVerify.Next";
int first = UnityEditor.SessionState.GetInt(cursorKey, 0);
if (first >= CarpetCleaning.Loc.Languages.Length) first = 0;
int last = System.Math.Min(first + 5, CarpetCleaning.Loc.Languages.Length);
string dir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "reports", "first_person");
System.IO.Directory.CreateDirectory(dir);
bool editorPaused = UnityEditor.EditorApplication.isPaused;
bool simPaused = sim.Paused, uiPaused = sim.UiPaused, uiModal = tools.UiModal, fpEnabled = fp.enabled;
string language = CarpetCleaning.Loc.Code;
string oldName = toolName.text;
var oldOverlayDisplay = overlay.style.display;
var oldPromptDisplay = prompt.style.display;
bool hadClass = root.ClassListContains("first-person");
var reports = new System.Collections.Generic.List<string>();
bool modalIsolation = true;
try
{
    UnityEditor.EditorApplication.isPaused = true;
    sim.Paused = true;
    fp.enabled = false;
    root.AddToClassList("first-person");
    for (int i = first; i < last; i++)
    {
        string code = CarpetCleaning.Loc.Languages[i].code;
        CarpetCleaning.Loc.SetLanguage(code, false);
        hud.SetShopOpen(false);
        overlay.style.display = UnityEngine.UIElements.DisplayStyle.Flex;
        prompt.style.display = UnityEngine.UIElements.DisplayStyle.Flex;
        UnityEditor.EditorApplication.Step();
        // Exercise the widest translated held-tool name instead of only the currently selected tool.
        float widest = -1f;
        foreach (var tool in CarpetCleaning.ToolTable.All)
        {
            string label = CarpetCleaning.Loc.T(tool.name);
            float width = toolName.MeasureTextSize(label, 0f, UnityEngine.UIElements.VisualElement.MeasureMode.Undefined,
                0f, UnityEngine.UIElements.VisualElement.MeasureMode.Undefined).x;
            if (width > widest) { widest = width; toolName.text = label; }
        }
        UnityEditor.EditorApplication.Step();
        UnityEditor.EditorApplication.Step();
        string hudResult = CarpetCleaning.CarpetDebugApi.LocOverflow();
        overlay.style.display = UnityEngine.UIElements.DisplayStyle.None;
        hud.SetShopOpen(true);
        UnityEditor.EditorApplication.Step();
        UnityEditor.EditorApplication.Step();
        UnityEditor.EditorApplication.Step();
        string shopResult = CarpetCleaning.CarpetDebugApi.LocOverflow();
        string report = "{\"code\":\"" + code + "\",\"hud\":" + hudResult + ",\"shop\":" + shopResult + "}";
        System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "ui_" + code + ".json"), report);
        reports.Add(report);
        hud.SetShopOpen(false);
    }
    if (first == 0 && menu != null)
    {
        hud.SetShopOpen(true);
        menu.SetOpen(true);
        modalIsolation &= hud.ShopOpen && !menu.IsOpen && sim.UiPaused && tools.UiModal;
        hud.SetShopOpen(false);
        menu.SetOpen(true);
        hud.SetShopOpen(true);
        modalIsolation &= menu.IsOpen && !hud.ShopOpen && sim.UiPaused && tools.UiModal;
        menu.SetOpen(false);
        modalIsolation &= !hud.ShopOpen && !menu.IsOpen && !sim.UiPaused && !tools.UiModal;
    }
    UnityEditor.SessionState.SetInt(cursorKey, last);
}
finally
{
    if (menu != null && menu.IsOpen) menu.SetOpen(false);
    hud.SetShopOpen(false);
    fp.enabled = fpEnabled;
    CarpetCleaning.Loc.SetLanguage(language, false);
    toolName.text = oldName;
    overlay.style.display = oldOverlayDisplay;
    prompt.style.display = oldPromptDisplay;
    root.EnableInClassList("first-person", hadClass);
    sim.Paused = simPaused;
    sim.UiPaused = uiPaused;
    tools.UiModal = uiModal;
    UnityEditor.EditorApplication.isPaused = editorPaused;
}
return "{\"first\":" + first + ",\"next\":" + last + ",\"done\":" + (last == CarpetCleaning.Loc.Languages.Length ? "true" : "false") +
    ",\"modalIsolation\":" + (modalIsolation ? "true" : "false") + ",\"languages\":[" + string.Join(",", reports) + "]}";
