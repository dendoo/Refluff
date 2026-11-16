var player = UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.FirstPersonPlayer>();
var sim = UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.CarpetSimulation>();
var hud = UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.EconomyHud>();
var bench = UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.UpgradeWorkbench>();
if (!UnityEngine.Application.isPlaying || player == null || sim == null || hud == null || bench == null)
    return "First-person scene not ready";
var oldPosition = player.transform.position;
float oldYaw = player.Yaw, oldPitch = player.Pitch;
bool oldPaused = UnityEditor.EditorApplication.isPaused, oldSimPaused = sim.Paused;
string oldLanguage = CarpetCleaning.Loc.Code;
string geometry = "";
try
{
    UnityEditor.EditorApplication.isPaused = true;
    sim.Paused = true;
    CarpetCleaning.Loc.SetLanguage("en", false);
    player.DebugPose(0f, -1.8f, 0f, 48f);
    for (int i = 0; i < 3; i++) UnityEditor.EditorApplication.Step();
    CarpetCleaning.EconomyDebugApi.CaptureHud("Screenshots/first_person_ui_review_en.png", 2560, 1440);
    player.DebugPose(-1.5f, .88f, 270f, 20f);
    UnityEngine.Physics.SyncTransforms();
    geometry = bench.DebugJson();
    CarpetCleaning.Loc.SetLanguage("ru", false);
    hud.SetShopOpen(true);
    for (int i = 0; i < 15; i++) UnityEditor.EditorApplication.Step();
    CarpetCleaning.EconomyDebugApi.CaptureHud("Screenshots/first_person_shop_review_ru.png", 2560, 1440);
}
finally
{
    hud.SetShopOpen(false);
    CarpetCleaning.Loc.SetLanguage(oldLanguage, false);
    player.DebugPose(oldPosition.x, oldPosition.z, oldYaw, oldPitch);
    sim.Paused = oldSimPaused;
    UnityEditor.EditorApplication.isPaused = oldPaused;
}
return geometry;
