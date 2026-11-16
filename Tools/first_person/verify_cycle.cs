// Focused integration check: physical workbench -> modal UI -> paid hand-over -> new carpet.
// Run in Play from an Idle carpet. The test intentionally leaves a NEW carpet in the scene.
// SaveData and exact save-file bytes are backed up before payment and restored in finally;
// achievements are unsubscribed throughout, and camera/player/input/modal state is restored.
// unity command eval_file --file C:/UnityProjects/CarpetCleaning/Tools/first_person/verify_cycle.cs
var player = UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.FirstPersonPlayer>();
var tools = UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.ToolController>();
var bench = UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.UpgradeWorkbench>();
var hud = UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.EconomyHud>();
var menu = UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.PauseMenu>();
var handover = UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.CarpetHandover>();
var economy = UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.Economy>();
if (!UnityEngine.Application.isPlaying || player == null || tools == null || tools.sim == null || bench == null
    || hud == null || menu == null || handover == null || economy == null || economy.Data == null || !economy.enabled)
    return "{\"ok\":false,\"error\":\"Play the complete first-person scene before this check\"}";
var sim = tools.sim;
if (!sim.Ready || handover.State != CarpetCleaning.HandoverState.Idle)
    return "{\"ok\":false,\"error\":\"An initialized Idle carpet is required\"}";

var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var pt = player.GetType();
var focus = pt.GetField("_focused", flags);
var wait = pt.GetField("_waitForButtonsUp", flags);
var blocked = pt.GetField("_wasBlocked", flags);
var owns = pt.GetField("_ownsCursor", flags);
var playerLate = pt.GetMethod("LateUpdate", flags);
var modalRelease = tools.GetType().GetField("_modalRelease", flags);
var lastPayout = economy.GetType().GetField("<LastPayout>k__BackingField", flags);
if (focus == null || wait == null || blocked == null || owns == null || playerLate == null || modalRelease == null || lastPayout == null)
    return "{\"ok\":false,\"error\":\"Reflection contract changed; update this test\"}";

var checks = new System.Collections.Generic.List<string>();
bool allPassed = true;
System.Func<string, string> quote = value => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ") + "\"";
System.Action<string, bool, string> check = (name, ok, detail) =>
{
    allPassed &= ok;
    checks.Add("{\"name\":" + quote(name) + ",\"ok\":" + (ok ? "true" : "false") + ",\"detail\":" + quote(detail) + "}");
};

string savePath = CarpetCleaning.SaveStore.FilePath;
bool saveExisted = System.IO.File.Exists(savePath);
byte[] saveBytes = saveExisted ? System.IO.File.ReadAllBytes(savePath) : null;
string savedData = UnityEngine.JsonUtility.ToJson(economy.Data);
var savedPayout = economy.LastPayout;
string backupDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath, "../reports/first_person"));
System.IO.Directory.CreateDirectory(backupDir);
// Durable recovery files are written BEFORE any payment, in addition to the in-memory snapshot.
System.IO.File.WriteAllText(System.IO.Path.Combine(backupDir, "cycle_economy_before.json"), savedData);
if (saveExisted) System.IO.File.WriteAllBytes(System.IO.Path.Combine(backupDir, "cycle_save_before.bin"), saveBytes);
System.IO.File.WriteAllText(System.IO.Path.Combine(backupDir, "cycle_save_manifest.json"),
    "{\"existed\":" + (saveExisted ? "true" : "false") + ",\"path\":" + quote(savePath) + "}");

var savedPosition = player.transform.position;
float savedYaw = player.Yaw, savedPitch = player.Pitch;
bool savedFocus = (bool)focus.GetValue(player), savedWait = (bool)wait.GetValue(player);
bool savedBlocked = (bool)blocked.GetValue(player), savedOwns = (bool)owns.GetValue(player);
bool savedRelease = (bool)modalRelease.GetValue(tools);
var savedCursor = UnityEngine.Cursor.lockState;
bool savedCursorVisible = UnityEngine.Cursor.visible;
bool savedUi = tools.UiModal, savedSuspended = tools.Suspended, savedInput = tools.InputEnabled;
bool savedScripted = tools.ScriptedPointer, savedScriptedDown = tools.ScriptedDown, savedScriptedRight = tools.ScriptedRight;
var savedScriptedUv = tools.ScriptedUv;
var savedTool = tools.Current;
bool savedPaused = sim.Paused, savedUiPaused = sim.UiPaused;
bool savedMenu = menu.IsOpen, savedShop = hud.ShopOpen;
var achievement = UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.SteamAchievements>();
bool savedAchievements = achievement != null && achievement.enabled;
bool beganHandover = false;

System.Action capture = () =>
{
    focus.SetValue(player, true); wait.SetValue(player, false); blocked.SetValue(player, false); owns.SetValue(player, true);
    UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.Locked;
    UnityEngine.Cursor.visible = false;
};
System.Action<float, float, UnityEngine.Vector3> aimAt = (x, z, target) =>
{
    var eye = new UnityEngine.Vector3(x, player.floorY + player.eyeHeight, z);
    var direction = target - eye;
    float yaw = UnityEngine.Mathf.Atan2(direction.x, direction.z) * UnityEngine.Mathf.Rad2Deg;
    float pitch = UnityEngine.Mathf.Atan2(-direction.y,
        UnityEngine.Mathf.Sqrt(direction.x * direction.x + direction.z * direction.z)) * UnityEngine.Mathf.Rad2Deg;
    player.DebugPose(x, z, yaw, pitch);
};

try
{
    if (savedAchievements) achievement.enabled = false;
    hud.SetShopOpen(false); menu.SetOpen(false);
    tools.UiModal = false; tools.Suspended = false; tools.InputEnabled = true;
    tools.ScriptedPointer = false; tools.ScriptedDown = false; tools.ScriptedRight = false;
    sim.Paused = false; sim.UiPaused = false;
    capture();

    var terminal = bench.transform.Find("Terminal");
    var terminalCollider = terminal != null ? terminal.GetComponent<UnityEngine.Collider>() : null;
    if (terminalCollider == null) throw new System.InvalidOperationException("Workbench terminal collider is missing.");
    var terminalTarget = terminalCollider.bounds.center;
    aimAt(terminalTarget.x + 3.8f, terminalTarget.z, terminalTarget);
    bool farAimed = bench.IsAimedAt(player.viewCamera);
    bool farOpened = bench.TryInteract();
    check("workbench_out_of_reach", !farAimed && !farOpened && !hud.ShopOpen,
        "aimed=" + farAimed + ",opened=" + farOpened);

    aimAt(terminalTarget.x + 1.1f, terminalTarget.z, terminalTarget);
    capture();
    bool nearAimed = bench.IsAimedAt(player.viewCamera);
    bool nearOpened = bench.TryInteract();
    playerLate.Invoke(player, null);
    check("terminal_opens_actual_shop", nearAimed && nearOpened && hud.ShopOpen && sim.UiPaused && tools.UiModal
        && !player.CanControl && UnityEngine.Cursor.lockState == UnityEngine.CursorLockMode.None,
        "aimed=" + nearAimed + ",opened=" + nearOpened + ",cursor=" + UnityEngine.Cursor.lockState);
    menu.SetOpen(true);
    check("shop_keeps_modal_ownership", !menu.IsOpen && hud.ShopOpen && sim.UiPaused && tools.UiModal, "shop=" + hud.ShopOpen);
    hud.SetShopOpen(false);
    capture();
    check("shop_close_restores_gameplay", !hud.ShopOpen && !sim.UiPaused && !tools.UiModal && player.CanControl,
        "canControl=" + player.CanControl);

    var menuPosition = player.transform.position;
    menu.SetOpen(true);
    playerLate.Invoke(player, null);
    float pausedTime = sim.SimTime;
    sim.SendMessage("LateUpdate");
    bool menuBenchAttempt = bench.TryInteract();
    check("pause_freezes_and_owns_cursor", menu.IsOpen && sim.UiPaused && tools.UiModal && !player.CanControl
        && UnityEngine.Cursor.lockState == UnityEngine.CursorLockMode.None && sim.SimTime == pausedTime
        && UnityEngine.Vector3.Distance(menuPosition, player.transform.position) < 0.0001f && !menuBenchAttempt,
        "menu=" + menu.IsOpen + ",benchOpened=" + menuBenchAttempt + ",cursor=" + UnityEngine.Cursor.lockState);
    menu.SetOpen(false);
    capture();
    check("pause_close_restores_gameplay", !menu.IsOpen && !sim.UiPaused && !tools.UiModal && player.CanControl,
        "canControl=" + player.CanControl);

    player.DebugPose(0f, -3.8f, 0f, 43f);
    var cyclePlayer = player.transform.position;
    var cyclePlayerRotation = player.transform.rotation;
    var cycleCamera = player.viewCamera.transform.position;
    var cycleCameraRotation = player.viewCamera.transform.rotation;
    System.Func<bool> cameraStill = () => UnityEngine.Vector3.Distance(cyclePlayer, player.transform.position) < 0.0001f
        && UnityEngine.Quaternion.Angle(cyclePlayerRotation, player.transform.rotation) < 0.001f
        && UnityEngine.Vector3.Distance(cycleCamera, player.viewCamera.transform.position) < 0.0001f
        && UnityEngine.Quaternion.Angle(cycleCameraRotation, player.viewCamera.transform.rotation) < 0.001f;
    int seedBefore = sim.Seed, moneyBefore = economy.Money, deliveredBefore = economy.Data.carpetsDelivered;
    long earnedBefore = economy.Data.lifetimeEarned;
    string pictureBefore = handover.PictureName;
    string profileBefore = UnityEngine.JsonUtility.ToJson(sim.CarpetDirt);

    beganHandover = handover.Begin(true);
    var payment = economy.LastPayout;
    int expectedMoney = (int)System.Math.Min(int.MaxValue, (long)moneyBefore + payment.total);
    check("handover_begins_without_camera_motion", beganHandover && handover.State == CarpetCleaning.HandoverState.FlyOut
        && !tools.InputEnabled && !tools.carpetCollider.enabled && cameraStill(), "state=" + handover.State);
    check("handover_pays_exactly_once", payment.valid && payment.moneyBefore == moneyBefore && payment.moneyAfter == expectedMoney
        && economy.Money == expectedMoney && economy.Data.carpetsDelivered == deliveredBefore + 1
        && economy.Data.lifetimeEarned == earnedBefore + payment.total,
        "before=" + moneyBefore + ",paid=" + payment.total + ",after=" + economy.Money);
    var paidDisk = System.IO.File.Exists(savePath)
        ? UnityEngine.JsonUtility.FromJson<CarpetCleaning.SaveData>(System.IO.File.ReadAllText(savePath)) : null;
    check("payment_persists_before_animation", paidDisk != null && paidDisk.money == expectedMoney
        && paidDisk.carpetsDelivered == deliveredBefore + 1, "exists=" + (paidDisk != null));

    handover.Tick(handover.flyOutSeconds + 0.01f);
    check("away_keeps_floor_tools_available", handover.State == CarpetCleaning.HandoverState.Away && sim.CarpetAway
        && tools.InputEnabled && !tools.carpetCollider.enabled && cameraStill(), "state=" + handover.State + ",tool=" + tools.Current);
    bool took = handover.TakeNewCarpet();
    check("new_carpet_uses_new_picture_and_soil", took && handover.State == CarpetCleaning.HandoverState.FlyIn
        && sim.Seed == seedBefore + UnityEngine.Mathf.Max(handover.seedStep, 1)
        && UnityEngine.JsonUtility.ToJson(sim.CarpetDirt) != profileBefore
        && (handover.ActivePictures == null || handover.ActivePictures.Length < 2 || handover.PictureName != pictureBefore)
        && cameraStill(), "seed=" + seedBefore + "->" + sim.Seed + ",picture=" + pictureBefore + "->" + handover.PictureName);
    handover.Tick(handover.flyInSeconds + 0.01f);
    check("new_carpet_lands_ready_to_clean", handover.State == CarpetCleaning.HandoverState.Idle && !sim.CarpetAway
        && tools.InputEnabled && tools.carpetCollider.enabled && economy.ClockRunning && cameraStill(), "state=" + handover.State);
    check("cycle_does_not_pay_twice", economy.Data.carpetsDelivered == deliveredBefore + 1 && economy.Money == expectedMoney,
        "delivered=" + economy.Data.carpetsDelivered + ",money=" + economy.Money);
}
catch (System.Exception error)
{
    var cause = error is System.Reflection.TargetInvocationException && error.InnerException != null ? error.InnerException : error;
    check("exception", false, cause.GetType().Name + ": " + cause.Message);
}
finally
{
    // Independent cleanup blocks ensure even a UI/camera cleanup failure cannot skip save restoration.
    try
    {
        hud.SetShopOpen(false); menu.SetOpen(false);
        if (beganHandover && handover.State != CarpetCleaning.HandoverState.Idle) handover.SnapToIdle();
        hud.Payout?.Finish();
        tools.ResetPointerContinuity(); tools.SetTool(savedTool);
        player.DebugPose(savedPosition.x, savedPosition.z, savedYaw, savedPitch);
        var body = player.GetComponent<UnityEngine.CharacterController>();
        bool bodyOn = body != null && body.enabled;
        if (bodyOn) body.enabled = false;
        player.transform.position = savedPosition;
        if (bodyOn) body.enabled = true;
        UnityEngine.Physics.SyncTransforms();
        if (savedShop) hud.SetShopOpen(true);
        else if (savedMenu) menu.SetOpen(true);
        tools.UiModal = savedUi; tools.Suspended = savedSuspended; tools.InputEnabled = savedInput;
        tools.ScriptedPointer = savedScripted; tools.ScriptedDown = savedScriptedDown;
        tools.ScriptedRight = savedScriptedRight; tools.ScriptedUv = savedScriptedUv;
        sim.Paused = savedPaused; sim.UiPaused = savedUiPaused;
        focus.SetValue(player, savedFocus); wait.SetValue(player, savedWait);
        blocked.SetValue(player, savedBlocked); owns.SetValue(player, savedOwns);
        modalRelease.SetValue(tools, savedRelease);
        UnityEngine.Cursor.lockState = savedCursor; UnityEngine.Cursor.visible = savedCursorVisible;
    }
    catch (System.Exception error) { check("restore_scene_controls", false, error.Message); }
    try
    {
        // Overwrite the original object, preserving references held by other gameplay systems.
        UnityEngine.JsonUtility.FromJsonOverwrite(savedData, economy.Data);
        lastPayout.SetValue(economy, savedPayout);
        check("economy_data_restored", UnityEngine.JsonUtility.ToJson(economy.Data) == savedData, "money=" + economy.Money);
    }
    catch (System.Exception error) { check("restore_economy_failed", false, error.Message + "; backup=" + backupDir); }
    try
    {
        if (saveExisted) System.IO.File.WriteAllBytes(savePath, saveBytes);
        else if (System.IO.File.Exists(savePath)) System.IO.File.Delete(savePath);
        bool exact = saveExisted
            ? System.IO.File.Exists(savePath) && System.Linq.Enumerable.SequenceEqual(saveBytes, System.IO.File.ReadAllBytes(savePath))
            : !System.IO.File.Exists(savePath);
        check("save_bytes_restored", exact, "originalExisted=" + saveExisted);
    }
    catch (System.Exception error) { check("restore_save_failed", false, error.Message + "; backup=" + backupDir); }
    if (savedAchievements && achievement != null) achievement.enabled = true;
}
string report = "{\"ok\":" + (allPassed ? "true" : "false") + ",\"checks\":[" + string.Join(",", checks) + "]}";
System.IO.File.WriteAllText("reports/first_person/cycle.json", report);
return report;
