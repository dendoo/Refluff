// Runtime physical-frame and GPU conservation checks. Play must be idle, with the rug present.
// Changes transient carpet/floor state, restores the original carpet seed/profile, settings, tuning,
// and exact save bytes. Achievements are disabled throughout. No handover or economy purchase occurs.
var sim = UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.CarpetSimulation>();
var floor = UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.FloorSimulation>();
var tools = UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.ToolController>();
if (!UnityEngine.Application.isPlaying || sim == null || floor == null || !sim.Ready || !floor.Ready || sim.CarpetAway)
    return "{\"ok\":false,\"error\":\"Play an initialized scene with its rug present\"}";
var checks = new System.Collections.Generic.List<string>();
bool all = true;
var ci = System.Globalization.CultureInfo.InvariantCulture;
System.Func<double, string> num = n => n.ToString("0.000000", ci);
System.Func<string, string> quote = s => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", "") + "\"";
System.Action<string, bool, string> check = (name, pass, detail) =>
{
    all &= pass;
    checks.Add("{\"name\":" + quote(name) + ",\"ok\":" + (pass ? "true" : "false") + ",\"detail\":" + quote(detail) + "}");
};
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var achievement = UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.SteamAchievements>();
bool achievementOn = achievement != null && achievement.enabled;
bool neutral = CarpetCleaning.ToolTuning.Neutral;
int originalSeed = sim.Seed;
var originalProfile = sim.CarpetDirt;
float evaporation = sim.settings.E_SURF, pileEvaporation = sim.settings.E_PILE;
float timeScale = sim.TimeScale;
string savePath = CarpetCleaning.SaveStore.FilePath;
bool saveExists = System.IO.File.Exists(savePath);
byte[] save = saveExists ? System.IO.File.ReadAllBytes(savePath) : null;
UnityEngine.Texture2D upload = null;
try
{
    if (achievementOn) achievement.enabled = false;
    CarpetCleaning.ToolTuning.SetNeutral(true);
    sim.TimeScale = 1f;
    sim.settings.E_SURF = 0f; sim.settings.E_PILE = 0f;
    var size = CarpetCleaning.CarpetSimulation.CarpetSizeM;
    var collider = tools != null ? tools.carpetCollider as UnityEngine.BoxCollider : null;
    check("physical_rug_and_collider", UnityEngine.Vector2.Distance(size, new UnityEngine.Vector2(3.6f, 4.5f)) < 1e-5f
        && collider != null && UnityEngine.Mathf.Abs(collider.size.x - size.x) < 1e-5f && UnityEngine.Mathf.Abs(collider.size.z - size.y) < 1e-5f,
        "size=" + size + ",collider=" + (collider != null ? collider.size.ToString() : "missing"));
    check("runtime_rt_dimensions", sim.Fluid.width == 512 && sim.Fluid.height == 640 && floor.Fluid.width == 1280 && floor.Fluid.height == 1280,
        "carpet=" + sim.Fluid.width + "x" + sim.Fluid.height + ",floor=" + floor.Fluid.width + "x" + floor.Fluid.height);
    var floorView = floor.GetComponent<CarpetCleaning.FloorRenderer>();
    var waterMesh = floorView.Water.GetComponent<UnityEngine.MeshFilter>().sharedMesh;
    var foamMesh = floorView.Foam.GetComponent<UnityEngine.MeshFilter>().sharedMesh;
    check("full_room_render_domain", UnityEngine.Mathf.Abs(waterMesh.bounds.size.x - 12f) < 1e-5f
        && UnityEngine.Mathf.Abs(waterMesh.bounds.size.z - 12f) < 1e-5f
        && UnityEngine.Mathf.Abs(foamMesh.bounds.size.x - 12f) < 1e-5f
        && floorView.waterSubdiv == 320 && floorView.foamSubdiv == 640
        && UnityEngine.Mathf.Abs(UnityEngine.Shader.GetGlobalFloat("_FloorFoamPitchMm") - 18.75f) < 1e-5f,
        "water=" + waterMesh.bounds.size + ",foam=" + foamMesh.bounds.size + ",grids=" + floorView.waterSubdiv + "/" + floorView.foamSubdiv);
    check("cell_spacing_asset", UnityEngine.Mathf.Abs(sim.settings.DX - 7.03125f) < 1e-6f
        && UnityEngine.Mathf.Abs(CarpetCleaning.FloorConsts.FloorDxMm - 9.375f) < 1e-6f,
        "carpetDX=" + num(sim.settings.DX) + ",floorDX=" + num(CarpetCleaning.FloorConsts.FloorDxMm));
    var renderer = sim.GetComponent<CarpetCleaning.CarpetRenderer>();
    var mat = renderer.ShellsRenderer.sharedMaterial;
    var strands = mat.GetVector("_StrandGrid"); var clumps = mat.GetVector("_ClumpGrid"); var bend = mat.GetVector("_BendUV");
    check("physical_fiber_dimensions", UnityEngine.Mathf.Abs(strands.x - 720f) < .01f && UnityEngine.Mathf.Abs(strands.y - 900f) < .01f
        && UnityEngine.Mathf.Abs(clumps.x - 300f) < .01f && UnityEngine.Mathf.Abs(clumps.y - 375f) < .01f
        && UnityEngine.Mathf.Abs(bend.x * 3600f - 24f) < .001f && UnityEngine.Mathf.Abs(bend.y * 4500f - 24f) < .001f
        && UnityEngine.Mathf.Abs(mat.GetFloat("_PileHeight") - .02f) < .00001f,
        "strands=" + strands + ",clumps=" + clumps + ",bendMm=" + num(bend.x * 3600f));

    // Explicit off-centre physical target. Water + pile water must remain at the stamp's CMM location.
    sim.ResetCarpet(41, false, 0f);
    var hose = CarpetCleaning.ToolTuning.Effective(CarpetCleaning.ToolId.Hose);
    sim.EnqueueStamp(new CarpetCleaning.ToolStamp {
        uv = new UnityEngine.Vector2(.25f, .3f), dir = UnityEngine.Vector2.right, radiusMm = hose.radiusMm,
        lengthMm = 0f, pressure = 1f, speed = 0f, dtS = 1f / 60f, tool = (int)CarpetCleaning.ToolId.Hose,
        vStroke = UnityEngine.Vector2.zero });
    sim.StepFrame(1f / 60f);
    var fluidRead = UnityEngine.Rendering.AsyncGPUReadback.Request(sim.Fluid); fluidRead.WaitForCompletion();
    var fiberRead = UnityEngine.Rendering.AsyncGPUReadback.Request(sim.Fiber); fiberRead.WaitForCompletion();
    if (fluidRead.hasError || fiberRead.hasError) throw new System.InvalidOperationException("Carpet readback failed");
    var fluid = fluidRead.GetData<UnityEngine.Vector4>(); var fiber = fiberRead.GetData<UnityEngine.Vector4>();
    double volume = 0, mx = 0, my = 0;
    float dx = CarpetCleaning.CarpetSimulation.CellSizeMm;
    for (int y = 0; y < CarpetCleaning.CarpetSimulation.SimH; y++)
        for (int x = 0; x < CarpetCleaning.CarpetSimulation.SimW; x++)
        {
            int i = y * CarpetCleaning.CarpetSimulation.SimW + x;
            double v = (fluid[i].x + fiber[i].w * sim.settings.W_CAP) * dx * dx;
            volume += v; mx += v * (x + .5) * dx; my += v * (y + .5) * dx;
        }
    double cx = mx / System.Math.Max(volume, 1e-9), cy = my / System.Math.Max(volume, 1e-9);
    check("hose_physical_target", volume > 1000 && System.Math.Abs(cx - 900.0) < dx && System.Math.Abs(cy - 1350.0) < dx,
        "volumeMm3=" + num(volume) + ",centroidMm=" + num(cx) + "," + num(cy));

    // Isolate K_FInject: known extensive runoff on every border cell, including all four corners.
    // No transport/chemistry is dispatched, so every payload must be conserved by the edge transfer itself.
    sim.StepFrame(1e-4f); // clear previous stamps before the floor reads the shared buffer
    floor.ResetFloor();
    int w = CarpetCleaning.CarpetSimulation.SimW, h = CarpetCleaning.CarpetSimulation.SimH;
    var payload = new UnityEngine.Color[w * h];
    int boundary = 0;
    for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
        if (x == 0 || y == 0 || x == w - 1 || y == h - 1)
        { payload[y * w + x] = new UnityEngine.Color(100f, .005f, 30f, .002f); boundary++; }
    upload = new UnityEngine.Texture2D(w, h, UnityEngine.TextureFormat.RGBAFloat, false, true);
    upload.SetPixels(payload); upload.Apply(false, false);
    UnityEngine.Graphics.CopyTexture(upload, sim.EdgeOut);
    for (int i = 0; i < payload.Length; i++) payload[i] = UnityEngine.Color.clear;
    upload.SetPixels(payload); upload.Apply(false, false);
    UnityEngine.Graphics.CopyTexture(upload, sim.EdgeOut2);
    var compute = floor.compute; int kernel = compute.FindKernel("K_FInject");
    compute.SetInt("_StampCount", 0); compute.SetInt("_CarpetAway", 0);
    compute.SetTexture(kernel, "_FFluidOut", floor.Fluid); compute.SetTexture(kernel, "_FSoapOut", floor.Soap);
    compute.SetTexture(kernel, "_FDeposit", floor.Deposit);
    compute.SetTexture(kernel, "_FAgit", (UnityEngine.RenderTexture)floor.GetType().GetField("_agit", flags).GetValue(floor));
    compute.SetTexture(kernel, "_FToolFlow", (UnityEngine.RenderTexture)floor.GetType().GetField("_toolFlow", flags).GetValue(floor));
    compute.SetBuffer(kernel, "_FInject", floor.InjectBuffer);
    compute.SetTexture(kernel, "_EdgeOut", sim.EdgeOut); compute.SetTexture(kernel, "_EdgeOut2", sim.EdgeOut2);
    compute.SetBuffer(kernel, "_Stamps", sim.StampBuffer);
    compute.Dispatch(kernel, CarpetCleaning.FloorSimulation.FW / 8, CarpetCleaning.FloorSimulation.FH / 8, 1);
    var floorFluidRead = UnityEngine.Rendering.AsyncGPUReadback.Request(floor.Fluid); floorFluidRead.WaitForCompletion();
    var floorSoapRead = UnityEngine.Rendering.AsyncGPUReadback.Request(floor.Soap); floorSoapRead.WaitForCompletion();
    if (floorFluidRead.hasError || floorSoapRead.hasError) throw new System.InvalidOperationException("Floor readback failed");
    var ff = floorFluidRead.GetData<UnityEngine.Vector4>(); var fs = floorSoapRead.GetData<UnityEngine.Vector4>();
    double water = 0, slurry = 0, foam = 0, foamDirt = 0;
    double cellArea = CarpetCleaning.FloorConsts.FloorDxMm * CarpetCleaning.FloorConsts.FloorDxMm;
    bool finite = true;
    for (int i = 0; i < ff.Length; i++)
    {
        water += ff[i].x * cellArea; slurry += ff[i].w;
        foam += fs[i].y * cellArea; foamDirt += fs[i].z;
        finite &= !float.IsNaN(ff[i].x) && !float.IsInfinity(ff[i].x) && ff[i].x >= 0f;
    }
    double eWater = System.Math.Abs(water / (boundary * 100.0) - 1.0);
    double eSlurry = System.Math.Abs(slurry / (boundary * .005) - 1.0);
    double eFoam = System.Math.Abs(foam / (boundary * 30.0) - 1.0);
    double eDirt = System.Math.Abs(foamDirt / (boundary * .002) - 1.0);
    check("gpu_runoff_water_conserved", finite && eWater < .00001, "expectedMm3=" + boundary * 100 + ",actualMm3=" + num(water) + ",relativeError=" + num(eWater));
    check("gpu_runoff_species_conserved", eSlurry < .00001 && eFoam < .00001 && eDirt < .00001,
        "slurryError=" + num(eSlurry) + ",foamError=" + num(eFoam) + ",foamDirtError=" + num(eDirt));

    // Saturate every dirt histogram sample at its cap16.0 The old1024 scale wraps the uint sum;
    //512 must return2684354560 exactly while all327680 cells land in the top bin.
    for (int i = 0; i < payload.Length; i++) payload[i] = new UnityEngine.Color(0f, 0f, 0f, 32f);
    upload.SetPixels(payload); upload.Apply(false, false);
    UnityEngine.Graphics.CopyTexture(upload, sim.Fluid);
    sim.ReadStatsSync();
    var histogram = (uint[])sim.GetType().GetField("_statsRaw", flags).GetValue(sim);
    int massIndex = 16 + CarpetCleaning.CarpetSimulation.HistBins - 1;
    int countIndex = 16 + 3 * CarpetCleaning.CarpetSimulation.HistBins - 1;
    ulong expectedMass = (ulong)CarpetCleaning.CarpetSimulation.CellCount * 16UL * 512UL;
    check("histogram_cap_does_not_overflow", histogram[massIndex] == expectedMass
        && histogram[countIndex] == CarpetCleaning.CarpetSimulation.CellCount && UnityEngine.Mathf.Abs(sim.HistMean(0) - 16f) < 1e-6f,
        "expected=" + expectedMass + ",actual=" + histogram[massIndex] + ",cells=" + histogram[countIndex] + ",decodedMean=" + num(sim.HistMean(0)));
    // A full 12 mm water/foam layer over the 1280-square floor overflowed the old x256 stats.
    // Test raw accumulation plus the public decoded means, without transport/chemistry changing the fixture.
    UnityEngine.Object.DestroyImmediate(upload);
    int fw = CarpetCleaning.FloorSimulation.FW;
    upload = new UnityEngine.Texture2D(fw, fw, UnityEngine.TextureFormat.RGBAFloat, false, true);
    var floorPayload = new UnityEngine.Color[CarpetCleaning.FloorSimulation.CellCount];
    for (int i = 0; i < floorPayload.Length; i++) floorPayload[i] = new UnityEngine.Color(12f, 0f, 0f, 0f);
    upload.SetPixels(floorPayload); upload.Apply(false, false);
    UnityEngine.Graphics.CopyTexture(upload, floor.Fluid);
    for (int i = 0; i < floorPayload.Length; i++) floorPayload[i] = new UnityEngine.Color(0f, 12f, 0f, 0f);
    upload.SetPixels(floorPayload); upload.Apply(false, false);
    UnityEngine.Graphics.CopyTexture(upload, floor.Soap);
    var floorStats = floor.FloorStatsSync();
    var floorRaw = (uint[])floor.GetType().GetField("_statsRaw", flags).GetValue(floor);
    ulong floorExpected = (ulong)CarpetCleaning.FloorSimulation.CellCount * 12UL * 128UL;
    check("full_floor_stats_do_not_overflow", floorRaw[0] == floorExpected && floorRaw[2] == floorExpected
        && UnityEngine.Mathf.Abs(floorStats.water - 12f) < 1e-5f && UnityEngine.Mathf.Abs(floorStats.foam - 12f) < 1e-5f
        && UnityEngine.Mathf.Abs(floorStats.pooledCells - 1f) < 1e-5f,
        "expected=" + floorExpected + ",waterRaw=" + floorRaw[0] + ",foamRaw=" + floorRaw[2] + ",waterMean=" + num(floorStats.water));

}
catch (System.Exception error) { check("exception", false, error.ToString()); }
finally
{
    if (upload != null) UnityEngine.Object.DestroyImmediate(upload);
    sim.settings.E_SURF = evaporation; sim.settings.E_PILE = pileEvaporation;
    sim.TimeScale = timeScale; CarpetCleaning.ToolTuning.SetNeutral(neutral);
    sim.ResetCarpet(originalSeed, false, originalProfile); floor.ResetFloor();
    if (achievementOn && achievement != null) achievement.enabled = true;
    if (saveExists) System.IO.File.WriteAllBytes(savePath, save);
    else if (System.IO.File.Exists(savePath)) System.IO.File.Delete(savePath);
}
string report = "{\"ok\":" + (all ? "true" : "false") + ",\"checks\":[" + string.Join(",", checks) + "]}";
System.IO.File.WriteAllText(System.IO.Path.GetFullPath("reports/first_person/scale_gpu_full_room.json"), report);
return report;
