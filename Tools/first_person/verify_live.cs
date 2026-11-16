// Actual ToolController.Update -> centre ray -> ToolStamp -> GPU upload, for all ten tools.
// Run in Play with a carpet present. This changes the live carpet/floor by short tool strokes.
// It temporarily sets only private player focus flags and Input System state for an unfocused
// Editor, restores them in finally, and disables achievement listening so the test cannot unlock
// achievements or write tools-used history. No production debug/input override is installed.
// unity command eval_file --file C:/UnityProjects/CarpetCleaning/Tools/first_person/verify_live.cs
var player = UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.FirstPersonPlayer>();
var tools = UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.ToolController>();
var mouse = UnityEngine.InputSystem.Mouse.current;
if (!UnityEngine.Application.isPlaying || player == null || tools == null || tools.sim == null || mouse == null)
    return "{\"ok\":false,\"error\":\"Play the FPS scene with an Input System mouse before this check\"}";
var sim = tools.sim;
var handover = UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.CarpetHandover>();
if (!sim.Ready || sim.CarpetAway || (handover != null && handover.State != CarpetCleaning.HandoverState.Idle))
    return "{\"ok\":false,\"error\":\"Wait for a playable carpet before this check\"}";
// Avoid interpreting a real user's simultaneous number/R key as part of the injected mouse test.
var keyboard = UnityEngine.InputSystem.Keyboard.current;
if (keyboard != null)
    foreach (var key in keyboard.allKeys)
        if (key.isPressed || key.wasPressedThisFrame)
            return "{\"ok\":false,\"error\":\"A real keyboard key is active; retry on an idle input frame\"}";

var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var playerType = player.GetType();
var toolType = tools.GetType();
var playerUpdate = playerType.GetMethod("Update", flags);
var toolUpdate = toolType.GetMethod("Update", flags);
var focus = playerType.GetField("_focused", flags);
var waitButtons = playerType.GetField("_waitForButtonsUp", flags);
var wasBlocked = playerType.GetField("_wasBlocked", flags);
var ownsCursor = playerType.GetField("_ownsCursor", flags);
var modalRelease = toolType.GetField("_modalRelease", flags);
if (playerUpdate == null || toolUpdate == null || focus == null || waitButtons == null || wasBlocked == null || ownsCursor == null || modalRelease == null)
    return "{\"ok\":false,\"error\":\"Controller reflection contract changed; update this test\"}";

var culture = System.Globalization.CultureInfo.InvariantCulture;
var checks = new System.Collections.Generic.List<string>();
bool passedAll = true;
System.Func<string, string> quote = s => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ") + "\"";
System.Func<float, string> num = value => value.ToString("0.0000", culture);
System.Action<string, bool, string> check = (name, passed, detail) =>
{
    passedAll &= passed;
    checks.Add("{\"name\":" + quote(name) + ",\"ok\":" + (passed ? "true" : "false") + ",\"detail\":" + quote(detail) + "}");
};

var savedPosition = player.transform.position;
float savedYaw = player.Yaw, savedPitch = player.Pitch, savedReach = player.toolReach, savedTimeScale = sim.TimeScale;
bool savedFocused = (bool)focus.GetValue(player), savedWait = (bool)waitButtons.GetValue(player);
bool savedBlocked = (bool)wasBlocked.GetValue(player), savedOwnedCursor = (bool)ownsCursor.GetValue(player);
bool savedModalRelease = (bool)modalRelease.GetValue(tools);
var savedCursor = UnityEngine.Cursor.lockState;
bool savedCursorVisible = UnityEngine.Cursor.visible;
bool savedInput = tools.InputEnabled, savedSuspended = tools.Suspended, savedModal = tools.UiModal;
bool savedScripted = tools.ScriptedPointer, savedDown = tools.ScriptedDown, savedRight = tools.ScriptedRight;
bool savedPaused = sim.Paused, savedUiPaused = sim.UiPaused;
var savedUv = tools.ScriptedUv;
var savedTool = tools.Current;
var savedPointerUi = tools.PointerOverUI;
var achievement = UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.SteamAchievements>();
bool savedAchievementEnabled = achievement != null && achievement.enabled;
var originalMouse = new UnityEngine.InputSystem.LowLevel.MouseState
{
    position = mouse.position.ReadValue(), delta = mouse.delta.ReadValue(), scroll = mouse.scroll.ReadValue(),
    displayIndex = (ushort)mouse.displayIndex.ReadValue(), clickCount = (ushort)mouse.clickCount.ReadValue()
};
originalMouse = originalMouse.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left, mouse.leftButton.isPressed)
    .WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Right, mouse.rightButton.isPressed)
    .WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Middle, mouse.middleButton.isPressed)
    .WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Forward, mouse.forwardButton.isPressed)
    .WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Back, mouse.backButton.isPressed);

var fpHud = UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.FirstPersonHud>();
var hudUpdate = fpHud != null ? fpHud.GetType().GetMethod("Update", flags) : null;
var chromeField = fpHud != null ? fpHud.GetType().GetField("_chromeBlockers", flags) : null;
var chrome = chromeField != null ? chromeField.GetValue(fpHud) as System.Collections.Generic.List<UnityEngine.UIElements.VisualElement> : null;
var savedPicking = new System.Collections.Generic.List<UnityEngine.UIElements.PickingMode>();
if (chrome != null) foreach (var element in chrome) savedPicking.Add(element != null ? element.pickingMode : UnityEngine.UIElements.PickingMode.Ignore);

// Mouse buttons are bitfields: InputState.Change(buttonControl, float) is unsupported.
// Replace a whole MouseState while retaining every other current mouse field instead.
System.Action<UnityEngine.InputSystem.LowLevel.MouseButton, bool> setButton = (button, down) =>
{
    var state = new UnityEngine.InputSystem.LowLevel.MouseState
    {
        position = mouse.position.ReadValue(), delta = mouse.delta.ReadValue(), scroll = mouse.scroll.ReadValue(),
        displayIndex = (ushort)mouse.displayIndex.ReadValue(), clickCount = (ushort)mouse.clickCount.ReadValue()
    };
    state = state.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left, mouse.leftButton.isPressed)
        .WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Right, mouse.rightButton.isPressed)
        .WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Middle, mouse.middleButton.isPressed)
        .WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Forward, mouse.forwardButton.isPressed)
        .WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Back, mouse.backButton.isPressed)
        .WithButton(button, down);
    UnityEngine.InputSystem.LowLevel.InputState.Change(mouse, state);
};

System.Action release = () =>
{
    setButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left, false);
    setButton(UnityEngine.InputSystem.LowLevel.MouseButton.Right, false);
    UnityEngine.InputSystem.LowLevel.InputState.Change(mouse.delta, UnityEngine.Vector2.zero);
    tools.InputEnabled = true; tools.Suspended = false; tools.UiModal = false;
    tools.ScriptedPointer = false; sim.Paused = false; sim.UiPaused = false;
    focus.SetValue(player, true); waitButtons.SetValue(player, false); wasBlocked.SetValue(player, false);
    ownsCursor.SetValue(player, true);
    UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.Locked;
    UnityEngine.Cursor.visible = false;
    toolUpdate.Invoke(tools, null); // real release path clears the modal-release latch
};
System.Action press = () => setButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left, true);
System.Action step = () => CarpetCleaning.CarpetDebugApi.StepAll(1f / 60f);
System.Func<UnityEngine.Vector2> expectedUv = () =>
{
    var ray = player.InteractionRay;
    var root = tools.carpetCollider != null ? tools.carpetCollider.transform : tools.transform;
    UnityEngine.Vector3 point;
    UnityEngine.RaycastHit hit;
    if (tools.carpetCollider != null && tools.carpetCollider.enabled && tools.carpetCollider.Raycast(ray, out hit, 100f)) point = hit.point;
    else
    {
        var floorPoint = root.TransformPoint(new UnityEngine.Vector3(0f, CarpetCleaning.FloorConsts.FloorYMm * .001f, 0f));
        var plane = new UnityEngine.Plane(root.up, floorPoint);
        float enter;
        if (!plane.Raycast(ray, out enter)) return new UnityEngine.Vector2(float.NaN, float.NaN);
        point = ray.GetPoint(enter);
    }
    var local = root.InverseTransformPoint(point);
    var size = CarpetCleaning.CarpetSimulation.CarpetSizeM;
    return new UnityEngine.Vector2(local.x / size.x + 0.5f, local.z / size.y + 0.5f);
};
System.Func<UnityEngine.Vector2, UnityEngine.Vector2, float> errorMm = (a, b) =>
    UnityEngine.Vector2.Scale(a - b, CarpetCleaning.CarpetSimulation.CarpetSizeMm).magnitude;
System.Action<string> expectBlocked = name =>
{
    int before = tools.StampCount;
    toolUpdate.Invoke(tools, null);
    check(name, tools.StampCount == before && !tools.IsStroking && !tools.HasSurfaceAim,
        "added=" + (tools.StampCount - before) + ",aim=" + tools.HasSurfaceAim + ",working=" + tools.IsStroking);
};

try
{
    if (savedAchievementEnabled) achievement.enabled = false;
    sim.TimeScale = 1f; player.toolReach = 2f;
    // Deliberately make the OS-style pointer unrelated to the centre. A regression to mouse-position
    // picking would miss the intended target. Also prove non-modal HUD hit testing cannot eat FPS strokes.
    tools.PointerOverUI = p => true;
    UnityEngine.InputSystem.LowLevel.InputState.Change(mouse, new UnityEngine.InputSystem.LowLevel.MouseState
    { position = new UnityEngine.Vector2(17f, 23f), delta = UnityEngine.Vector2.zero, scroll = UnityEngine.Vector2.zero });
    release();
    check("test_input_capture", player.CanControl, "focused=" + focus.GetValue(player) + ",lock=" + UnityEngine.Cursor.lockState);
    if (!player.CanControl) throw new System.InvalidOperationException("The Editor did not accept Locked cursor; no production bypass was used.");
    step(); // flush any pre-test pending stamps before measuring each real input frame

    for (int i = 0; i < CarpetCleaning.ToolTable.All.Length; i++)
    {
        release();
        bool floorTool = CarpetCleaning.ToolTable.All[i].floorTool;
        // Eye-to-contact distance is about 1.84m; floor-only tools aim beside the carpet.
        player.DebugPose(floorTool ? 2.2f : 0f, -2.7f, 0f, 60f);
        tools.SetTool((CarpetCleaning.ToolId)i);
        int before = tools.StampCount;
        press();
        toolUpdate.Invoke(tools, null);
        var firstUv = tools.SurfaceAimUV;
        var rayUv = expectedUv();
        float aimError = errorMm(firstUv, rayUv);
        var carpetRoot = tools.carpetCollider != null ? tools.carpetCollider.transform : tools.transform;
        var carpetSize = CarpetCleaning.CarpetSimulation.CarpetSizeM;
        var expectedWorld = carpetRoot.TransformPoint(new UnityEngine.Vector3((rayUv.x - 0.5f) * carpetSize.x,
            0f, (rayUv.y - 0.5f) * carpetSize.y));
        float worldError = UnityEngine.Vector3.ProjectOnPlane(tools.SurfaceAimWorld - expectedWorld, carpetRoot.up).magnitude * 1000f;
        bool onCorrectSurface = tools.PointerOnCarpet != floorTool;
        bool contact = tools.HasSurfaceAim && tools.IsStroking && tools.StampCount == before + 1;
        step();
        bool uploaded = sim.StampCount > 0;
        var gpu = new CarpetCleaning.ToolStamp[UnityEngine.Mathf.Max(sim.StampCount, 1)];
        if (uploaded) sim.StampBuffer.GetData(gpu, 0, 0, sim.StampCount);
        var def = CarpetCleaning.ToolTuning.Effective((CarpetCleaning.ToolId)i);
        bool stamp = uploaded && gpu[0].tool == i && errorMm(gpu[0].uv, firstUv) < 0.01f
            && UnityEngine.Mathf.Abs(gpu[0].radiusMm - def.radiusMm) < 0.001f
            && UnityEngine.Mathf.Abs(gpu[0].lengthMm - (def.isBlade ? def.lengthMm : 0f)) < 0.001f
            && UnityEngine.Mathf.Abs(gpu[0].pressure - 1f) < 0.0001f;
        // Real mouse-look Update, then the next tool Update. This makes an actual segment/plow,
        // instead of teleporting the camera through DebugPose and accidentally testing only HOLD.
        UnityEngine.InputSystem.LowLevel.InputState.Change(mouse.delta, new UnityEngine.Vector2(16f, 0f));
        playerUpdate.Invoke(player, null);
        UnityEngine.InputSystem.LowLevel.InputState.Change(mouse.delta, UnityEngine.Vector2.zero);
        toolUpdate.Invoke(tools, null);
        float travel = errorMm(tools.SurfaceAimUV, firstUv);
        float nextError = errorMm(tools.SurfaceAimUV, expectedUv());
        step();
        check("tool_" + i + "_real_update", contact && onCorrectSurface && aimError < 0.05f && worldError < 0.05f && stamp
            && tools.StampCount == before + 2 && travel > 1f && nextError < 0.05f && sim.StampCount > 0,
            "surface=" + (floorTool ? "floor" : "carpet") + ",added=" + (tools.StampCount - before)
            + ",gpu=" + sim.StampCount + ",aimErrorMm=" + num(aimError) + ",worldErrorMm=" + num(worldError) + ",travelMm=" + num(travel)
            + ",radiusMm=" + num(def.radiusMm));
    }

    release(); player.DebugPose(0f, -2.7f, 0f, 60f); tools.SetTool(CarpetCleaning.ToolId.Hose);
    setButton(UnityEngine.InputSystem.LowLevel.MouseButton.Right, true);
    int pressureBefore = tools.StampCount;
    toolUpdate.Invoke(tools, null); step();
    var pressureGpu = new CarpetCleaning.ToolStamp[1];
    if (sim.StampCount > 0) sim.StampBuffer.GetData(pressureGpu, 0, 0, 1);
    check("right_button_half_pressure", tools.StampCount == pressureBefore + 1 && sim.StampCount > 0
        && UnityEngine.Mathf.Abs(pressureGpu[0].pressure - 0.5f) < 0.0001f, "pressure=" + num(pressureGpu[0].pressure));

    // Hose is deliberately unlimited; only a close-contact tool should reject this 2.78m carpet ray.
    release(); tools.SetTool(CarpetCleaning.ToolId.Scraper); player.DebugPose(0f, -2.7f, 0f, 35f); press(); expectBlocked("reach_blocks_real_update");
    release(); tools.SetTool(CarpetCleaning.ToolId.Hose); player.DebugPose(-5.1f, 0.5f, 90f, 50f); press(); expectBlocked("table_blocks_real_update");
    release(); player.DebugPose(0f, -1.8f, 0f, -20f); press(); expectBlocked("sky_blocks_real_update");

    release(); player.DebugPose(0f, -2.7f, 0f, 60f); press();
    tools.UiModal = true; sim.UiPaused = true; expectBlocked("modal_blocks_real_update");
    float pausedClock = sim.SimTime;
    sim.SendMessage("LateUpdate");
    check("modal_freezes_simulation", sim.SimTime == pausedClock, "dt=" + num(sim.SimTime - pausedClock));
    tools.UiModal = false; sim.UiPaused = false;
    expectBlocked("closing_click_waits_for_release");
    release(); press();
    int resumedBefore = tools.StampCount;
    toolUpdate.Invoke(tools, null); step();
    check("release_restores_cleaning", tools.StampCount == resumedBefore + 1 && tools.IsStroking,
        "added=" + (tools.StampCount - resumedBefore));

    release(); press(); tools.Suspended = true; expectBlocked("overlay_blocks_real_update");
    release(); press(); sim.Paused = true; expectBlocked("pause_blocks_real_update");
    release(); press(); focus.SetValue(player, false); expectBlocked("focus_loss_blocks_real_update");
    release(); press(); tools.InputEnabled = false; expectBlocked("handover_gate_blocks_real_update");

    release();
    if (fpHud != null && hudUpdate != null && chrome != null && chrome.Count > 0)
    {
        // Emulate CleaningHUD's delayed post-stroke restore; the later FPS HUD update must win.
        foreach (var element in chrome) if (element != null) element.pickingMode = UnityEngine.UIElements.PickingMode.Position;
        hudUpdate.Invoke(fpHud, null);
        bool allIgnored = true;
        foreach (var element in chrome) if (element != null && element.pickingMode != UnityEngine.UIElements.PickingMode.Ignore) allIgnored = false;
        check("locked_cursor_blocks_chrome_clicks", allIgnored, "elements=" + chrome.Count);
        tools.UiModal = true;
        UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.None;
        hudUpdate.Invoke(fpHud, null);
        bool restored = true;
        foreach (var element in chrome) if (element != null && element.pickingMode != UnityEngine.UIElements.PickingMode.Position) restored = false;
        check("modal_restores_chrome_picking", restored, "elements=" + chrome.Count);
    }
    else check("hud_picking_ready", false, "FirstPersonHud has not built its chrome list");
}
catch (System.Exception error)
{
    var cause = error is System.Reflection.TargetInvocationException && error.InnerException != null ? error.InnerException : error;
    check("exception", false, cause.GetType().Name + ": " + cause.Message);
}
finally
{
    // Stop the test stroke before restoring physical device state, so a held OS button cannot
    // create a final unintended stamp during cleanup. No Unity frames are stepped in this block.
    setButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left, false);
    setButton(UnityEngine.InputSystem.LowLevel.MouseButton.Right, false);
    tools.ResetPointerContinuity();
    tools.SetTool(savedTool); tools.PointerOverUI = savedPointerUi;
    tools.InputEnabled = savedInput; tools.Suspended = savedSuspended; tools.UiModal = savedModal;
    tools.ScriptedPointer = savedScripted; tools.ScriptedDown = savedDown; tools.ScriptedRight = savedRight; tools.ScriptedUv = savedUv;
    sim.Paused = savedPaused; sim.UiPaused = savedUiPaused; sim.TimeScale = savedTimeScale;
    player.toolReach = savedReach;
    player.DebugPose(savedPosition.x, savedPosition.z, savedYaw, savedPitch);
    var body = player.GetComponent<UnityEngine.CharacterController>();
    bool bodyEnabled = body != null && body.enabled;
    if (bodyEnabled) body.enabled = false;
    player.transform.position = savedPosition;
    if (bodyEnabled) body.enabled = true;
    UnityEngine.Physics.SyncTransforms();
    focus.SetValue(player, savedFocused); waitButtons.SetValue(player, savedWait);
    wasBlocked.SetValue(player, savedBlocked); ownsCursor.SetValue(player, savedOwnedCursor);
    modalRelease.SetValue(tools, savedModalRelease);
    UnityEngine.Cursor.lockState = savedCursor; UnityEngine.Cursor.visible = savedCursorVisible;
    UnityEngine.InputSystem.LowLevel.InputState.Change(mouse, originalMouse);
    if (fpHud != null && hudUpdate != null) hudUpdate.Invoke(fpHud, null);
    if (chrome != null)
        for (int i = 0; i < chrome.Count && i < savedPicking.Count; i++)
            if (chrome[i] != null) chrome[i].pickingMode = savedPicking[i];
    if (savedAchievementEnabled && achievement != null) achievement.enabled = true;
}
return "{\"ok\":" + (passedAll ? "true" : "false") + ",\"checks\":[" + string.Join(",", checks) + "]}";
