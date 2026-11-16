// Run in Play after CarpetCleaning/Build Scene:
// unity command eval_file --file C:/UnityProjects/CarpetCleaning/Tools/first_person/verify_controls.cs
// This exercises real movement/aim paths. It enqueues one harmless scraper hold stamp; it does not
// reset the carpet, wallet or save. Camera pose and all modal/script-input flags are restored.
var player = UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.FirstPersonPlayer>();
var tools = UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.ToolController>();
if (!UnityEngine.Application.isPlaying || player == null || tools == null)
    return "{\"ok\":false,\"error\":\"Play the first-person scene before running this check\"}";
var culture = System.Globalization.CultureInfo.InvariantCulture;
var checks = new System.Collections.Generic.List<string>();
bool allPassed = true;
System.Action<string, bool, string> check = (name, passed, detail) =>
{
    allPassed &= passed;
    checks.Add("{\"name\":\"" + name + "\",\"ok\":" + (passed ? "true" : "false") + ",\"detail\":\"" + detail + "\"}");
};
System.Func<float, string> number = x => x.ToString("0.0000", culture);
var oldPosition = player.transform.position;
float oldYaw = player.Yaw, oldPitch = player.Pitch, oldReach = player.toolReach;
bool oldInput = tools.InputEnabled, oldSuspended = tools.Suspended, oldModal = tools.UiModal;
bool oldScripted = tools.ScriptedPointer, oldDown = tools.ScriptedDown, oldRight = tools.ScriptedRight;
var oldUv = tools.ScriptedUv;
var oldTool = tools.Current;
var sim = tools.sim;
bool oldPaused = sim != null && sim.Paused, oldUiPaused = sim != null && sim.UiPaused;
try
{
    tools.InputEnabled = true; tools.Suspended = false; tools.UiModal = false;
    tools.ScriptedPointer = true; tools.ScriptedDown = false; tools.ScriptedRight = false;
    // Clear the existing modal-release latch through its real Update path.
    tools.SendMessage("Update");
    tools.ScriptedPointer = false;
    if (sim != null) { sim.Paused = false; sim.UiPaused = false; }
    player.toolReach = 2f;

    tools.SetTool(CarpetCleaning.ToolId.Scraper);
    player.DebugPose(0f, -2.7f, 0f, 60f);
    UnityEngine.Vector2 uv;
    bool near = tools.TryGetFirstPersonUV(out uv);
    check("near_carpet_contact", near && tools.PointerOnCarpet && uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f,
        "uv=" + number(uv.x) + "," + number(uv.y));

    player.DebugPose(0f, -2.7f, 0f, 35f);
    var camera = player.viewCamera;
    var center = new UnityEngine.Vector2(camera.pixelRect.center.x, camera.pixelRect.center.y);
    bool farPlane = tools.TryGetCarpetUV(center, out uv);
    bool far = tools.TryGetFirstPersonUV(out uv);
    check("reach_rejects_distant_contact", farPlane && !far, "plane=" + farPlane + ",fps=" + far);

    player.DebugPose(0f, -1.8f, 0f, -20f);
    bool sky = tools.TryGetFirstPersonUV(out uv);
    check("sky_has_no_contact", !sky, "fps=" + sky);

    // Unlimited hose isolates furniture occlusion from the close-contact reach check above.
    tools.SetTool(CarpetCleaning.ToolId.Hose);
    // The floor target lies inside the enlarged simulation, behind the workbench.
    player.DebugPose(-5.1f, 0.5f, 90f, 50f);
    bool throughTablePlane = tools.TryGetCarpetUV(center, out uv);
    bool throughTable = tools.TryGetFirstPersonUV(out uv);
    check("table_occludes_cleaning", throughTablePlane && !throughTable,
        "plane=" + throughTablePlane + ",fps=" + throughTable);

    player.DebugPose(-3.65f, 0.5f, 270f, 0f);
    player.StepMovement(UnityEngine.Vector2.up, 1f);
    float tableStop = player.transform.position.x;
    check("table_blocks_body", tableStop > -4.1f && tableStop < -3.8f, "x=" + number(tableStop));

    player.DebugPose(1f, -2f, 90f, 0f);
    player.StepMovement(UnityEngine.Vector2.up, 10f);
    float eastStop = player.transform.position.x;
    check("east_movement_bound", UnityEngine.Mathf.Abs(eastStop - player.movementBounds.xMax) < 0.025f,
        "x=" + number(eastStop));

    player.DebugPose(0f, -2f, 180f, 0f);
    player.StepMovement(UnityEngine.Vector2.up, 10f);
    float southStop = player.transform.position.z;
    check("south_movement_bound", UnityEngine.Mathf.Abs(southStop - player.movementBounds.yMin) < 0.025f,
        "z=" + number(southStop));

    player.DebugPose(0f, -2.2f, 0f, 48f);
    var start = player.transform.position;
    player.StepMovement(UnityEngine.Vector2.right, 0.3f);
    float straight = UnityEngine.Vector2.Distance(new UnityEngine.Vector2(start.x, start.z),
        new UnityEngine.Vector2(player.transform.position.x, player.transform.position.z));
    player.DebugPose(0f, -2.2f, 0f, 48f);
    player.StepMovement(UnityEngine.Vector2.one, 0.3f);
    float diagonal = UnityEngine.Vector2.Distance(new UnityEngine.Vector2(start.x, start.z),
        new UnityEngine.Vector2(player.transform.position.x, player.transform.position.z));
    check("diagonal_speed_normalized", UnityEngine.Mathf.Abs(straight - diagonal) < 0.005f,
        "straight=" + number(straight) + ",diagonal=" + number(diagonal));

    player.DebugPose(0f, -2.7f, 0f, 60f);
    tools.SetTool(CarpetCleaning.ToolId.Scraper);
    tools.ScriptedPointer = true; tools.ScriptedUv = new UnityEngine.Vector2(0.5f, 0.5f);
    tools.ScriptedDown = true;
    int beforeStamp = tools.StampCount;
    tools.SendMessage("Update");
    check("scripted_pointer_preserved", tools.StampCount > beforeStamp && tools.HasSurfaceAim && tools.IsStroking,
        "newStamps=" + (tools.StampCount - beforeStamp));

    tools.UiModal = true;
    if (sim != null) sim.UiPaused = true;
    beforeStamp = tools.StampCount;
    var modalPosition = player.transform.position;
    tools.ScriptedPointer = false;
    player.StepMovement(UnityEngine.Vector2.one, 0.5f);
    player.SendMessage("Update");
    tools.ScriptedPointer = true;
    tools.SendMessage("Update");
    check("modal_blocks_player_and_tools", !player.CanControl && tools.StampCount == beforeStamp && !tools.IsStroking
          && UnityEngine.Vector3.Distance(modalPosition, player.transform.position) < 0.0001f,
        "canControl=" + player.CanControl + ",newStamps=" + (tools.StampCount - beforeStamp));

    tools.UiModal = false; tools.ScriptedDown = false;
    if (sim != null) sim.UiPaused = false;
    tools.SendMessage("Update");
    tools.Suspended = true;
    tools.ScriptedDown = true;
    beforeStamp = tools.StampCount;
    tools.SendMessage("Update");
    check("overlay_blocks_stamps", tools.StampCount == beforeStamp && !tools.IsStroking,
        "newStamps=" + (tools.StampCount - beforeStamp));
}
finally
{
    tools.ScriptedDown = false; tools.ScriptedRight = false; tools.Suspended = false; tools.UiModal = false;
    tools.ScriptedPointer = true; tools.SendMessage("Update");
    tools.SetTool(oldTool);
    tools.InputEnabled = oldInput; tools.Suspended = oldSuspended; tools.UiModal = oldModal;
    tools.ScriptedPointer = oldScripted; tools.ScriptedUv = oldUv; tools.ScriptedDown = oldDown; tools.ScriptedRight = oldRight;
    if (sim != null) { sim.Paused = oldPaused; sim.UiPaused = oldUiPaused; }
    player.toolReach = oldReach;
    player.DebugPose(oldPosition.x, oldPosition.z, oldYaw, oldPitch);
    var body = player.GetComponent<UnityEngine.CharacterController>();
    bool enabled = body != null && body.enabled;
    if (enabled) body.enabled = false;
    player.transform.position = oldPosition;
    if (enabled) body.enabled = true;
    UnityEngine.Physics.SyncTransforms();
}
return "{\"ok\":" + (allPassed ? "true" : "false") + ",\"checks\":[" + string.Join(",", checks) + "]}";
