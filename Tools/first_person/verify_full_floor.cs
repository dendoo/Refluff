// Run eight times in Play: one near-wall/corner patch per call (outside the former 9 m domain).
// Calls real carpet stamp upload and floor simulation kernels. No player-input bypass or production hook.
// The floor is reset before/after each sample. Save bytes, achievements and tool tuning are preserved.
// Optional restart: UnityEditor.SessionState.SetInt("Refluff.FullFloorQA.Sample", 0).
var sim = UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.CarpetSimulation>();
var floor = UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.FloorSimulation>();
if (!UnityEngine.Application.isPlaying || sim == null || floor == null || !sim.Ready || !floor.Ready)
    return "{\"ok\":false,\"error\":\"Play the initialized full-floor scene\"}";
var points = new[] {
    new UnityEngine.Vector2(-5.8f, 0f), new UnityEngine.Vector2(5.8f, 0f),
    new UnityEngine.Vector2(0f, -5.8f), new UnityEngine.Vector2(0f, 5.8f),
    new UnityEngine.Vector2(-5.8f, -5.8f), new UnityEngine.Vector2(5.8f, -5.8f),
    new UnityEngine.Vector2(-5.8f, 5.8f), new UnityEngine.Vector2(5.8f, 5.8f)
};
int sample = UnityEditor.SessionState.GetInt("Refluff.FullFloorQA.Sample", 0) % points.Length;
var point = points[sample];
var cmm = point * 1000f + CarpetCleaning.CarpetSimulation.CarpetSizeMm * .5f;
var uv = new UnityEngine.Vector2(cmm.x / CarpetCleaning.CarpetSimulation.CarpetWidthMm,
    cmm.y / CarpetCleaning.CarpetSimulation.CarpetHeightMm);
var grid = (cmm - CarpetCleaning.FloorConsts.FloorMinMm) / CarpetCleaning.FloorConsts.FloorDxMm;
int cx = UnityEngine.Mathf.FloorToInt(grid.x), cy = UnityEngine.Mathf.FloorToInt(grid.y);
if (cx < 1 || cy < 1 || cx >= floor.Fluid.width - 1 || cy >= floor.Fluid.height - 1)
    return "{\"ok\":false,\"error\":\"Near-wall sample lies outside the active RT\",\"sample\":" + sample + "}";
var checks = new System.Collections.Generic.List<string>();
bool all = true;
var ci = System.Globalization.CultureInfo.InvariantCulture;
System.Func<float, string> n = f => f.ToString("0.000000", ci);
System.Func<string, string> quote = s => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ") + "\"";
System.Action<string, bool, string> check = (name, ok, detail) => {
    all &= ok;
    checks.Add("{\"name\":" + quote(name) + ",\"ok\":" + (ok ? "true" : "false") + ",\"detail\":" + quote(detail) + "}");
};
System.Func<UnityEngine.RenderTexture, UnityEngine.Vector4> patch = rt => {
    var read = UnityEngine.Rendering.AsyncGPUReadback.Request(rt, 0, cx - 1, 3, cy - 1, 3, 0, 1);
    read.WaitForCompletion();
    if (read.hasError) throw new System.InvalidOperationException("Floor patch readback failed: " + rt.name);
    var data = read.GetData<UnityEngine.Vector4>();
    var sum = UnityEngine.Vector4.zero;
    for (int i = 0; i < data.Length; i++) sum += data[i];
    return sum / data.Length;
};
System.Action<CarpetCleaning.ToolId> apply = tool => {
    var def = CarpetCleaning.ToolTuning.Effective(tool);
    sim.EnqueueStamp(new CarpetCleaning.ToolStamp {
        uv = uv, dir = UnityEngine.Vector2.up, radiusMm = def.radiusMm,
        lengthMm = def.isBlade ? def.lengthMm : 0f, pressure = 1f, speed = 0f,
        dtS = 1f / 30f, tool = (int)tool, vStroke = UnityEngine.Vector2.zero });
    sim.StepFrame(1f / 30f);
    floor.StepFrame(1f / 30f);
};
var achievement = UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.SteamAchievements>();
bool achievementsOn = achievement != null && achievement.enabled;
bool neutral = CarpetCleaning.ToolTuning.Neutral;
string savePath = CarpetCleaning.SaveStore.FilePath;
bool saveExists = System.IO.File.Exists(savePath);
byte[] originalSave = saveExists ? System.IO.File.ReadAllBytes(savePath) : null;
UnityEngine.Texture2D seed = null;
try
{
    if (achievementsOn) achievement.enabled = false;
    CarpetCleaning.ToolTuning.SetNeutral(true);
    floor.ResetFloor();
    // A physical 28.125 mm patch of dust/residue supplies visible material for the cleaning assertion.
    seed = new UnityEngine.Texture2D(3, 3, UnityEngine.TextureFormat.RGBAFloat, false, true);
    var dirt = new UnityEngine.Color[9];
    for (int i = 0; i < dirt.Length; i++) dirt[i] = new UnityEngine.Color(.4f, .4f, 0f, 0f);
    seed.SetPixels(dirt); seed.Apply(false, false);
    UnityEngine.Graphics.CopyTexture(seed, 0, 0, 0, 0, 3, 3, floor.Deposit, 0, 0, cx - 1, cy - 1);
    check("sample_outside_old_domain", UnityEngine.Mathf.Abs(point.x) > 4.5f || UnityEngine.Mathf.Abs(point.y) > 4.5f,
        "world=" + point + ",cell=" + cx + "," + cy);
    apply(CarpetCleaning.ToolId.Hose);
    var wet = patch(floor.Fluid);
    // The hose pushes inflow radially during the same frame; a centre patch is a wet film, not the whole dose.
    // Require a visible >0.05 mm film after transport instead of expecting the 5 mm pre-transport source depth.
    check("hose_wets_near_wall", wet.x > .05f && !float.IsNaN(wet.x), "waterMm=" + n(wet.x));
    apply(CarpetCleaning.ToolId.FoamSprayer);
    var foamed = patch(floor.Soap); var beforeVac = patch(floor.Fluid);
    check("foam_exists_near_wall", foamed.y > .1f, "foam=" + n(foamed.y));
    apply(CarpetCleaning.ToolId.WetVacuum);
    var vacuumed = patch(floor.Fluid); var vacuumFoam = patch(floor.Soap);
    check("vacuum_removes_water_and_foam", vacuumed.x < beforeVac.x * .25f && vacuumFoam.y < foamed.y * .25f,
        "water=" + n(beforeVac.x) + "->" + n(vacuumed.x) + ",foam=" + n(foamed.y) + "->" + n(vacuumFoam.y));
    apply(CarpetCleaning.ToolId.FloorCleaner);
    var treated = patch(floor.Deposit); var treatedFluid = patch(floor.Fluid); var treatedSoap = patch(floor.Soap);
    float soilBefore = treated.x + treated.y + treatedFluid.w + treatedSoap.z;
    check("detergent_treats_near_wall", treated.w > .1f && soilBefore > .01f,
        "detergent=" + n(treated.w) + ",soil=" + n(soilBefore));
    apply(CarpetCleaning.ToolId.Mop);
    var cleaned = patch(floor.Deposit); var cleanedFluid = patch(floor.Fluid); var cleanedSoap = patch(floor.Soap);
    float soilAfter = cleaned.x + cleaned.y + cleanedFluid.w + cleanedSoap.z;
    check("mop_cleans_and_drains_near_wall", soilAfter < soilBefore * .1f && cleanedFluid.x < .05f,
        "soil=" + n(soilBefore) + "->" + n(soilAfter) + ",waterMm=" + n(cleanedFluid.x));
}
catch (System.Exception error) { check("exception", false, error.ToString()); }
finally
{
    if (seed != null) UnityEngine.Object.DestroyImmediate(seed);
    floor.ResetFloor();
    CarpetCleaning.ToolTuning.SetNeutral(neutral);
    if (achievementsOn && achievement != null) achievement.enabled = true;
    if (saveExists) System.IO.File.WriteAllBytes(savePath, originalSave);
    else if (System.IO.File.Exists(savePath)) System.IO.File.Delete(savePath);
}
string report = "{\"ok\":" + (all ? "true" : "false") + ",\"sample\":" + sample
    + ",\"world\":[" + n(point.x) + "," + n(point.y) + "],\"checks\":[" + string.Join(",", checks) + "]}";
string reportDir = System.IO.Path.GetFullPath("reports/first_person");
System.IO.Directory.CreateDirectory(reportDir);
System.IO.File.WriteAllText(System.IO.Path.Combine(reportDir, "full_floor_" + sample + ".json"), report);
if (all) UnityEditor.SessionState.SetInt("Refluff.FullFloorQA.Sample", sample + 1);
return report;
