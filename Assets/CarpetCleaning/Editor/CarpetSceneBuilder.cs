using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace CarpetCleaning
{
    /// <summary>Idempotent scene builder (section 8). Menu: CarpetCleaning/Build Scene.</summary>
    public static class CarpetSceneBuilder
    {
        const string Root = "Assets/CarpetCleaning";
        const string MatDir = Root + "/Materials";
        const string SetDir = Root + "/Settings";
        const string PicPath = Root + "/Textures/rug_transylvanian_medallion.jpg";   // default first carpet when no picture override is active
        const string UiDir = Root + "/UI";
        const string HudUxmlPath = UiDir + "/CarpetHud.uxml";
        public const string CreditsPath = UiDir + "/Credits.txt";
        const string HudThemePath = UiDir + "/UnityDefaultRuntimeTheme.tss";
        const string HudPanelPath = UiDir + "/CarpetHudPanel.asset";
        const string ComputePath = Root + "/Compute/CarpetSim.compute";
        const string FloorComputePath = Root + "/Compute/FloorSim.compute";
        const string DebrisComputePath = Root + "/Compute/Debris.compute";

        [MenuItem("CarpetCleaning/Build Scene")]
        public static void Build()
        {
            try
            {
                EnsureFolders();
                var fiberMat = BuildFiberMaterial();
                var waterMat = BuildWaterMaterial();
                var cursorMat = BuildCursorMaterial();
                var floorSurfaceMat = BuildFloorSurfaceMaterial();
                var floorWaterMat = BuildFloorWaterMaterial();
                var foamMat = BuildFoamMaterial();
                var floorFoamMat = BuildFloorFoamMaterial();
                BuildDebrisMaterials(out var chunkMat, out var dustMat, out var dropMat);
                BuildToolModelSet();
                var settings = BuildSettings();
                var floorSettings = BuildFloorSettings();
                var soundBank = BuildSoundBank();
                var economySettings = BuildEconomySettings();
                UiSoundBaker.Bake(false);                 // ui_*.wav + Settings/UiSoundBank.asset (Docs/AUDIO.md 10)
                var sparkleMat = BuildSparkleMaterial();
                var compute = AssetDatabase.LoadAssetAtPath<ComputeShader>(ComputePath);
                if (compute == null) Debug.LogError("[CarpetSceneBuilder] compute shader not found at " + ComputePath);
                var floorCompute = AssetDatabase.LoadAssetAtPath<ComputeShader>(FloorComputePath);
                if (floorCompute == null) Debug.LogError("[CarpetSceneBuilder] floor compute shader not found at " + FloorComputePath);
                var debrisCompute = AssetDatabase.LoadAssetAtPath<ComputeShader>(DebrisComputePath);
                if (debrisCompute == null) Debug.LogError("[CarpetSceneBuilder] debris compute shader not found at " + DebrisComputePath);
                AssetDatabase.SaveAssets();

                var scene = SceneManager.GetActiveScene();
                var root = BuildCarpetRoot(fiberMat, waterMat, foamMat, cursorMat, sparkleMat, settings, compute);
                var carpetSim = root.GetComponent<CarpetSimulation>();
                if (carpetSim != null) carpetSim.floorSettings = floorSettings;
                // the floor needs the carpet refs, so it is built after BuildCarpetRoot
                var floorRoot = BuildFloorRoot(root, floorSettings, floorCompute, debrisCompute, floorSurfaceMat, floorWaterMat, floorFoamMat, chunkMat, dustMat, dropMat);
                var handover = root.GetComponent<CarpetHandover>();
                if (handover != null && floorRoot != null) handover.debris = floorRoot.GetComponent<DebrisSystem>();
                var cam = SetupCamera();
                SetupLight();
                SetupVolume();
                SetupPipeline();
                BuildEconomy(root, economySettings);      // before BuildUI: the HUD wires itself to it
                BuildUI();
                BuildEventSystem();

                var tc = root.GetComponent<ToolController>();
                if (tc != null) tc.cam = cam;
                FirstPersonSceneBuilder.Build(root, cam);

                // audio last: it only needs the tool controller and the bank (Docs/AUDIO.md 4)
                var audio = Ensure<ToolAudio>(root);
                audio.tools = tc;
                audio.bank = soundBank;
                audio.debris = floorRoot != null ? floorRoot.GetComponent<DebrisSystem>() : null;   // crumb capture events

                BuildSteam(root);

                EditorSceneManager.MarkSceneDirty(scene);
                if (!string.IsNullOrEmpty(scene.path)) EditorSceneManager.SaveScene(scene);
                else EditorSceneManager.SaveScene(scene, "Assets/Scenes/SampleScene.unity");
                Debug.Log("[CarpetSceneBuilder] scene built");
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }

        // ------------------------------------------------------------------ assets
        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder("Assets", "CarpetCleaning");
            if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder(Root, "Materials");
            if (!AssetDatabase.IsValidFolder(SetDir)) AssetDatabase.CreateFolder(Root, "Settings");
            if (!AssetDatabase.IsValidFolder(Root + "/Models")) AssetDatabase.CreateFolder(Root, "Models");
            if (!AssetDatabase.IsValidFolder(Root + "/Models/Tools")) AssetDatabase.CreateFolder(Root + "/Models", "Tools");
            if (!AssetDatabase.IsValidFolder(Root + "/Prefabs")) AssetDatabase.CreateFolder(Root, "Prefabs");
            if (!AssetDatabase.IsValidFolder(Root + "/Prefabs/Tools")) AssetDatabase.CreateFolder(Root + "/Prefabs", "Tools");
        }

        static Material LoadOrCreateMaterial(string path, string shaderName)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null) { Debug.LogError("[CarpetSceneBuilder] shader not found: " + shaderName); shader = Shader.Find("Universal Render Pipeline/Unlit"); }
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (shader != null && mat.shader != shader) mat.shader = shader;
            return mat;
        }

        static Material BuildFiberMaterial()
        {
            var m = LoadOrCreateMaterial(MatDir + "/CarpetFibers.mat", "CarpetCleaning/CarpetFibers");
            var overrides = CarpetHandover.LoadPictureOverrides();
            var pic = LoadPicture(overrides.Length > 0 ? AssetDatabase.GetAssetPath(overrides[0]) : PicPath);
            if (pic == null) Debug.LogWarning("[CarpetSceneBuilder] picture not found at " + PicPath);
            m.SetTexture("_PictureTex", pic);
            CarpetHandover.PictureFit(pic, out var picOffset, out var picScale);   // same fit the hand-over applies to every next carpet
            m.SetColor("_CarpetColor", new Color(0.20f, 0.36f, 0.62f, 1f));
            m.SetColor("_MudColor", new Color(0.24f, 0.15f, 0.08f, 1f));
            m.SetColor("_CrustColor", new Color(0.50f, 0.45f, 0.38f, 1f));
            m.SetColor("_GritColor", new Color(0.55f, 0.48f, 0.40f, 1f));
            m.SetColor("_DustColor", new Color(0.60f, 0.56f, 0.50f, 1f));   // the grey film loose dust leaves on the tips (Docs/DIRT_LEVEL.md)
            m.SetColor("_StainA", new Color(0.55f, 0.38f, 0.20f, 1f));
            m.SetColor("_StainB", new Color(0.50f, 0.18f, 0.25f, 1f));
            m.SetColor("_StainC", new Color(0.35f, 0.35f, 0.33f, 1f));
            m.SetColor("_StainD", new Color(0.40f, 0.50f, 0.22f, 1f));
            m.SetFloat("_PileHeight", 0.02f);
            m.SetFloat("_ShellCount", CarpetRenderer.DefaultShellCount);
            m.SetVector("_StrandGrid", new Vector4(CarpetSimulation.CarpetWidthMm / 5f, CarpetSimulation.CarpetHeightMm / 5f, 0f, 0f));
            m.SetVector("_ClumpGrid", new Vector4(CarpetSimulation.CarpetWidthMm / 12f, CarpetSimulation.CarpetHeightMm / 12f, 0f, 0f));
            m.SetVector("_BendUV", new Vector4(24f / CarpetSimulation.CarpetWidthMm, 24f / CarpetSimulation.CarpetHeightMm, 0f, 0f));
            m.SetVector("_PicOffset", picOffset);
            m.SetVector("_PicScale", picScale);
            m.SetFloat("_StrandRadius", 0.40f);
            m.SetFloat("_WetDarken", 0.5f);
            m.SetFloat("_DustVeil", 0.45f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material BuildWaterMaterial()
        {
            var m = LoadOrCreateMaterial(MatDir + "/CarpetWater.mat", "CarpetCleaning/CarpetWater");
            m.SetColor("_SkyColor", new Color(0.62f, 0.68f, 0.76f, 1f));
            m.SetColor("_SiltColor", new Color(0.26f, 0.18f, 0.10f, 1f));     // suspended-dirt albedo (sRGB, converted to linear): dirty water is brown, not blue, about as light as the wet pile around it
            m.SetColor("_AbsorbClean", new Color(0.02f, 0.03f, 0.05f, 1f));
            m.SetColor("_AbsorbDirty", new Color(0.15f, 0.30f, 0.60f, 1f));  // dirty water absorbs blue most (was red-most = blue cast)
            m.SetFloat("_MurkDensity", 0.8f);
            m.SetFloat("_MurkCRef", 0.4f);
            m.SetFloat("_MurkCarpet", 1.0f);
            m.SetFloat("_RefrStrength", 0.02f);
            m.SetFloat("_PileHeight", 0.02f);
            // film vs free surface (Docs/WET_LOOK.md 5): water under _FilmLo mm is held in the pile and drawn by CarpetFibers only
            // (0.3 = K_Reduce's pooledCells threshold); the free surface fades in up to _FilmHi. Shallow sheets drape over the tufts
            // (_Drape mm relief at _DrapeMm scale, _DrapeTilt meniscus tilt) and keep _ShallowSky of the sky + rim sheen below 2.5 mm.
            m.SetFloat("_FilmLo", 0.3f);
            m.SetFloat("_FilmHi", 0.9f);
            m.SetFloat("_Drape", 0.5f);
            m.SetFloat("_DrapeMm", 6f);
            m.SetFloat("_DrapeTilt", 1.5f);
            m.SetFloat("_ShallowSky", 0.35f);
            EditorUtility.SetDirty(m);
            return m;
        }

        // ---- foam (Docs/FOAM_LOOK.md): one shader, two materials; the floor variant enables _FLOOR_DOMAIN. Colours are sRGB (converted to linear).
        static void ApplyFoamLook(Material m)
        {
            m.SetColor("_FoamColor", new Color(0.98f, 0.40f, 0.88f, 1f));        // crest: hot pink
            m.SetColor("_FoamShadowColor", new Color(0.45f, 0.16f, 0.72f, 1f));  // shadow side / foot: violet, never grey
            m.SetColor("_FoamDirtColor", new Color(0.55f, 0.30f, 0.45f, 1f));    // dirty foam: mauve, still magenta-hued
            m.SetColor("_FoamRimColor", new Color(1.0f, 0.75f, 0.95f, 1f));      // fresnel rim: light pink
            m.SetFloat("_FoamHeight", 45f);       // mm; F 1/4/12 -> 13/33/44 mm. Keep _FoamHeight + _LumpMm + _CapLiftMm < 90 mm (mesh bounds)
            m.SetFloat("_FoamKnee", 3f);
            m.SetFloat("_LumpMm", 8f);            // +-8 mm dollops: the rim of a mound is a row of bumps, not a smooth ellipse
            m.SetVector("_BubbleMm", new Vector4(16f, 7f, 0f, 0f));
            m.SetFloat("_CapLiftMm", 5f);
            m.SetFloat("_SunScale", 0.5f);        // lit crest ~1.26 linear: under the 1.52 bloom threshold, only sparkles bloom
            m.SetFloat("_AmbientScale", 1.0f);
            m.SetFloat("_WrapK", 0.35f);
            m.SetFloat("_FormK", 0.5f);
            m.SetFloat("_ShadowStrength", 0.55f);
            m.SetFloat("_AlphaKnee", 0.25f);
            m.SetFloat("_SparklePx", 6f);
            m.renderQueue = 3001;                 // Transparent+1: after the water (3000), before droplets (3005) and dust (3010)
        }

        static Material BuildFoamMaterial()
        {
            var m = LoadOrCreateMaterial(MatDir + "/CarpetFoam.mat", "CarpetCleaning/CarpetFoam");
            ApplyFoamLook(m);
            m.DisableKeyword("_FLOOR_DOMAIN");
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material BuildFloorFoamMaterial()
        {
            var m = LoadOrCreateMaterial(MatDir + "/FloorFoam.mat", "CarpetCleaning/CarpetFoam");
            ApplyFoamLook(m);
            m.EnableKeyword("_FLOOR_DOMAIN");
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material BuildCursorMaterial()
        {
            var m = LoadOrCreateMaterial(MatDir + "/ToolCursor.mat", "CarpetCleaning/ToolCursor");
            m.SetColor("_Color", new Color(1f, 1f, 1f, 0.6f));
            EditorUtility.SetDirty(m);
            return m;
        }

        // ---- floor + debris materials (FLOOR_SYSTEM.md 5 / 6). LoadOrCreateMaterial falls back to URP/Unlit while a shader is missing.
        static Material BuildFloorSurfaceMaterial()
        {
            var m = LoadOrCreateMaterial(MatDir + "/FloorSurface.mat", "CarpetCleaning/FloorSurface");
            // Bright white porcelain on purpose: a clean tile must read as WHITE and glossy under the 3x sun, so the
            // contrast against a dusty/residued tile (which drops to ~0.40 albedo and near-zero gloss) is unmistakable.
            m.SetColor("_TileColor", new Color(0.82f, 0.83f, 0.85f, 1f));
            m.SetColor("_GroutColor", new Color(0.60f, 0.60f, 0.62f, 1f));
            m.SetColor("_DustColor", new Color(0.40f, 0.33f, 0.23f, 1f));       // darker + browner than the tile: dirt must not read as "slightly grey"
            m.SetColor("_ResidueColor", new Color(0.19f, 0.13f, 0.07f, 1f));
            m.SetColor("_SkyColor", new Color(0.62f, 0.68f, 0.76f, 1f));
            m.SetColor("_DetTint", new Color(0.86f, 0.94f, 0.98f, 1f));   // pale cool sheen: detergent must not read as a coloured overlay
            m.SetFloat("_DetSparkle", 0.35f);
            m.SetFloat("_CleanGloss", 0.74f);      // smoothness of a spotless dry tile (dust pulls it to 0.08)
            m.SetFloat("_CleanSheen", 0.45f);      // strength of the fresnel sheen + sun glint + micro-twinkle
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material BuildFloorWaterMaterial()
        {
            var m = LoadOrCreateMaterial(MatDir + "/FloorWater.mat", "CarpetCleaning/FloorWater");
            // same look as CarpetWater.mat
            m.SetColor("_SkyColor", new Color(0.62f, 0.68f, 0.76f, 1f));
            m.SetColor("_SiltColor", new Color(0.26f, 0.18f, 0.10f, 1f));
            m.SetColor("_AbsorbClean", new Color(0.02f, 0.03f, 0.05f, 1f));
            m.SetColor("_AbsorbDirty", new Color(0.15f, 0.30f, 0.60f, 1f));
            m.SetFloat("_MurkDensity", 0.8f);
            m.SetFloat("_MurkCRef", 0.4f);
            m.SetFloat("_RefrStrength", 0.02f);
            EditorUtility.SetDirty(m);
            return m;
        }

        // splash drop streak look (DebrisDrop.shader); tuned 2026-09-25 against the 1080p game camera, re-tuned 2026-09-26 for
        // ONE draw per frame, then "Drop look v2" (2026-09-26): the art director read every drop as a sewing pin (white glint
        // head, opaque grey shaft, barely tapered 1/60 s rod), so: a short exposure with a minimum length, a teardrop fading to
        // nothing, a nearly clear body with a darker refraction band, a small glint that flashes somewhere along the streak,
        // and per-drop variation (Docs/DEBRIS_SYSTEM.md "Drop look v2"). "Drop look v3" (2026-09-26): v2 had lost a third of
        // its contrast over the lit tile (thin dark ring + near-clear centre + white core averaged back to the tile), so: a
        // lens that passes on part of the real scene behind it (opaque texture, upside down), denser on a few-pixel drop and
        // clear on a near one, a wider darker band, a faint tail, a narrower brightness jitter, a 0.75 px glint floor
        const float DropShutter = 0.011f, DropMinStreakPx = 11f, DropMaxStreakPx = 20f, DropMinRadiusPx = 1.7f, DropSizeBoost = 1.3f;
        const float DropStreakFade = 0.2f, DropVary = 1f, DropBrightMin = 0.8f;
        const float DropBodyAlpha = 0.5f, DropBodyAlphaNear = 0.15f, DropRimAlpha = 0.9f, DropRimShade = 0.28f;
        const float DropBandInner = 0.12f, DropBandOuter = 0.82f, DropTailAlpha = 0.15f, DropTailWidth = 0.3f;
        const float DropLensTransmit = 0.35f, DropLensInvert = 1.5f;
        const float DropCaustic = 0.12f, DropGlintHDR = 3.2f, DropGlintSize = 0.15f, DropGlintMinPx = 0.75f, DropGlintLine = 0.35f;

        static void BuildDebrisMaterials(out Material chunk, out Material dust, out Material drop)
        {
            chunk = LoadOrCreateMaterial(MatDir + "/DebrisChunk.mat", "CarpetCleaning/DebrisChunk");
            chunk.SetColor("_CrustColor", new Color(0.34f, 0.29f, 0.23f, 1f));   // darker than the pile crust: shards show their broken underside
            chunk.SetColor("_MudColor", new Color(0.18f, 0.12f, 0.07f, 1f));
            EditorUtility.SetDirty(chunk);

            dust = LoadOrCreateMaterial(MatDir + "/DebrisDust.mat", "CarpetCleaning/DebrisDust");
            // pale powder, lighter than the crust plates it comes from; optical depth 0.7 at a newborn puff's centre, falling as
            // 1/area and settling out at 0.6/s; the lowest 12 mm above the pile fade out (Docs/DEBRIS_SYSTEM.md, dust puff look)
            dust.SetColor("_DustColor", new Color(0.68f, 0.64f, 0.58f, 1f));
            dust.SetFloat("_Density", 0.7f);
            dust.SetFloat("_Settle", 0.6f);
            dust.SetFloat("_SoftMm", 12f);
            EditorUtility.SetDirty(dust);

            drop = LoadOrCreateMaterial(MatDir + "/DebrisDrop.mat", "CarpetCleaning/DebrisDrop");
            // splash drops are drawn as screen-space motion streaks (Docs/DEBRIS_SYSTEM.md, splash drop look + drop look v2): a
            // short exposure (11 ms, x0.55-1.45 per drop) but never under 9 px while the drop moves that far in 1/60 s, a soft
            // knee at 20 px, at least 3 px wide at 1080p (px values scale with the target height), 1.3x the physical radius; a
            // teardrop whose width eases to 0.25 and alpha to 0 at the tail; a nearly clear centre (0.2), a darker refraction band
            // (0.9 x ambient 0.45) + a Fresnel rim in the puddles' sky colour, a faint caustic; the sun's glint is a small sparkle
            // (HDR 3.2 x the light x a skewed per-drop 0.2-1) where the drop flashed along the streak (40-100 % toward the head),
            // with a weaker smear around it; per-drop length, width and brightness vary
            drop.SetColor("_SkyColor", new Color(0.62f, 0.68f, 0.76f, 1f));
            drop.SetColor("_MudColor", new Color(0.24f, 0.15f, 0.08f, 1f));
            drop.SetFloat("_Shutter", DropShutter);
            drop.SetFloat("_MinStreakPx", DropMinStreakPx);
            drop.SetFloat("_MaxStreakPx", DropMaxStreakPx);
            drop.SetFloat("_MinRadiusPx", DropMinRadiusPx);
            drop.SetFloat("_SizeBoost", DropSizeBoost);
            drop.SetFloat("_BodyAlpha", DropBodyAlpha);
            drop.SetFloat("_BodyAlphaNear", DropBodyAlphaNear);
            drop.SetFloat("_RimAlpha", DropRimAlpha);
            drop.SetFloat("_RimShade", DropRimShade);
            drop.SetFloat("_BandInner", DropBandInner);
            drop.SetFloat("_BandOuter", DropBandOuter);
            drop.SetFloat("_TailAlpha", DropTailAlpha);
            drop.SetFloat("_TailWidth", DropTailWidth);
            drop.SetFloat("_StreakFade", DropStreakFade);
            drop.SetFloat("_LensTransmit", DropLensTransmit);
            drop.SetFloat("_LensInvert", DropLensInvert);
            drop.SetFloat("_Caustic", DropCaustic);
            drop.SetFloat("_GlintHDR", DropGlintHDR);
            drop.SetFloat("_GlintSize", DropGlintSize);
            drop.SetFloat("_GlintMinPx", DropGlintMinPx);
            drop.SetFloat("_GlintLine", DropGlintLine);
            drop.SetFloat("_Vary", DropVary);
            drop.SetFloat("_BrightMin", DropBrightMin);
            EditorUtility.SetDirty(drop);
        }

        /// <summary>
        /// The rotary brush's speed (Docs/TOOL_MODELS.md 7): twice a low-speed floor machine's 175 rpm, because the user
        /// wanted it to read faster (2026-09-24). SimSettings.BRUSH_RPM is its physics twin.
        /// </summary>
        const float DefaultSpinRpm = 350f;
        /// <summary>
        /// The machine presses its brush into the pile: the bristle tips sit 10 mm above the backing, half way into the
        /// 20 mm pile (hoverMarginM alone leaves them 2 mm in, like every other head). Docs/TOOL_MODELS.md 7.
        /// </summary>
        const float DefaultPileSinkM = 0.008f;

        /// <summary>
        /// Keeps Settings/ToolModelSet.asset in step with Prefabs/Tools: one entry per tool whose head
        /// prefab exists. Tools without a prefab are left out and keep the outline cursor (Docs/TOOL_MODELS.md).
        /// </summary>
        static ToolModelSet BuildToolModelSet()
        {
            string path = SetDir + "/ToolModelSet.asset";
            var set = AssetDatabase.LoadAssetAtPath<ToolModelSet>(path);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<ToolModelSet>();
                AssetDatabase.CreateAsset(set, path);
            }
            var keep = new System.Collections.Generic.List<ToolModelSet.Entry>();
            for (int i = 0; i < ToolTable.All.Length; i++)
            {
                var id = ToolTable.All[i].id;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Tools/Tool_" + id + ".prefab");
                if (prefab == null) continue;
                var e = new ToolModelSet.Entry { id = id, prefab = prefab, extraScale = 1f };
                for (int k = 0; k < set.entries.Length; k++)      // preserve any hand-tuned offsets
                    if (set.entries[k].id == id)
                    {
                        e.posOffsetM = set.entries[k].posOffsetM;
                        e.eulerOffset = set.entries[k].eulerOffset;
                        if (set.entries[k].extraScale > 0f) e.extraScale = set.entries[k].extraScale;
                        e.spinRpm = set.entries[k].spinRpm;
                        e.pileSinkM = set.entries[k].pileSinkM;
                    }
                // a head cut for spinning (ToolModelPostprocess.SpinSplit) is the rotary brush: it spins, and the machine's
                // weight presses it into the pile. 0 there means "not set yet".
                bool spins = prefab.transform.Find(ToolModelView.SpinNode) != null;
                if (spins && e.spinRpm == 0f) e.spinRpm = DefaultSpinRpm;
                if (spins && e.pileSinkM == 0f) e.pileSinkM = DefaultPileSinkM;
                keep.Add(e);
            }
            set.entries = keep.ToArray();
            EditorUtility.SetDirty(set);
            return set;
        }

        static SimSettings BuildSettings()
        {
            string path = SetDir + "/SimSettings.asset";
            var s = AssetDatabase.LoadAssetAtPath<SimSettings>(path);
            if (s == null)
            {
                s = ScriptableObject.CreateInstance<SimSettings>();
                AssetDatabase.CreateAsset(s, path);
            }
            s.DX = CarpetSimulation.CellSizeMm;
            EditorUtility.SetDirty(s);
            return s;
        }

        static FloorSettings BuildFloorSettings()
        {
            string path = SetDir + "/FloorSettings.asset";
            var s = AssetDatabase.LoadAssetAtPath<FloorSettings>(path);
            if (s == null)
            {
                s = ScriptableObject.CreateInstance<FloorSettings>();
                AssetDatabase.CreateAsset(s, path);
            }
            return s;
        }

        /// <summary>
        /// Settings/ToolSoundBank.asset (Docs/AUDIO.md 2). Defaults are written ONLY when the asset is created,
        /// so hand-tuned volumes and any recordings dropped into the clip slots survive a re-run of the builder.
        /// A new bank also gets the shipped recordings (Docs/AUDIO.md 11), which its default volumes are set for.
        /// </summary>
        static ToolSoundBank BuildSoundBank()
        {
            string path = SetDir + "/ToolSoundBank.asset";
            var s = AssetDatabase.LoadAssetAtPath<ToolSoundBank>(path);
            if (s == null)
            {
                s = ScriptableObject.CreateInstance<ToolSoundBank>();
                s.ResetToDefaults();
                AssetDatabase.CreateAsset(s, path);
                EditorUtility.SetDirty(s);
                AssetDatabase.SaveAssets();
                Debug.Log(ToolSoundBaker.ImportShippedRecordings());
            }
            else if (s.entries == null || s.entries.Length == 0)
            {
                s.ResetToDefaults();
                EditorUtility.SetDirty(s);
            }
            return s;
        }

        // ------------------------------------------------------------------ scene objects
        static GameObject FindOrCreate(string name, out bool created)
        {
            var go = GameObject.Find(name);
            created = go == null;
            if (created) go = new GameObject(name);
            return go;
        }

        static T Ensure<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        static GameObject BuildCarpetRoot(Material fiber, Material water, Material foam, Material cursor, Material sparkle, SimSettings settings, ComputeShader compute)
        {
            var old = GameObject.Find("CarpetRoot");
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
            var root = new GameObject("CarpetRoot");
            root.transform.position = Vector3.zero; root.transform.rotation = Quaternion.identity;

            var box = Ensure<BoxCollider>(root);
            box.center = new Vector3(0f, 0.01f, 0f);
            box.size = new Vector3(CarpetSimulation.CarpetSizeM.x, 0.02f, CarpetSimulation.CarpetSizeM.y);

            var sim = Ensure<CarpetSimulation>(root);
            sim.compute = compute; sim.settings = settings; sim.seed = 1;

            var view = Ensure<CarpetRenderer>(root);
            view.fiberMaterial = fiber; view.waterMaterial = water; view.foamMaterial = foam; view.cursorMaterial = cursor;
            view.shellCount = CarpetRenderer.DefaultShellCount; view.shellSubdivX = CarpetSimulation.SimW / 4; view.shellSubdivZ = CarpetSimulation.SimH / 4;
            view.waterSubdivX = CarpetSimulation.SimW / 2; view.waterSubdivZ = CarpetSimulation.SimH / 2;
            view.foamSubdivX = CarpetSimulation.SimW; view.foamSubdivZ = CarpetSimulation.SimH;

            var tc = Ensure<ToolController>(root);
            tc.sim = sim; tc.view = view; tc.carpetCollider = box;

            // CarpetStage: the visual-only child the hand-over animates (Docs/HANDOVER.md). The collider, the cursor,
            // the simulation and the debris frame stay on CarpetRoot at the origin.
            var stage = new GameObject("CarpetStage");
            stage.transform.SetParent(root.transform, false);

            var ho = Ensure<CarpetHandover>(root);
            ho.sim = sim; ho.view = view; ho.tools = tc;
            ho.pictures = LoadPictures();

            // child placeholders (meshes are built at Awake; materials assigned here so the scene shows them in the inspector)
            MakeChild(stage.transform, "CarpetShells", fiber);
            MakeChild(stage.transform, "WaterSurface", water);
            MakeChild(stage.transform, "FoamSurface", foam);
            MakeChild(root.transform, "ToolCursor", cursor);

            // "sparkling clean" glints that ride on the rug during the hand-over flight (Docs/ECONOMY.md 4.3): under the
            // stage, local space; CelebrationFx.Configure owns the particle setup, nothing emits on its own
            var glints = new GameObject("CleanSparkles");
            glints.transform.SetParent(stage.transform, false);
            var ps = glints.AddComponent<ParticleSystem>();
            CelebrationFx.Configure(ps, sparkle);
            var fx = Ensure<CelebrationFx>(root);
            fx.sparkles = ps; fx.handover = ho;

            // Tool heads hang off CarpetRoot for the same reason the cursor does: the hand-over animates CarpetStage.
            var models = new GameObject("ToolModels");
            models.transform.SetParent(root.transform, false);
            var mv = Ensure<ToolModelView>(models);
            mv.set = AssetDatabase.LoadAssetAtPath<ToolModelSet>(SetDir + "/ToolModelSet.asset");
            mv.tools = tc;   // a spinning head turns while the tool works
            view.toolModels = mv;
            return root;
        }

        /// <summary>Every picture asset in Textures/, name-sorted so the order is the same on every machine (Docs/HANDOVER.md).</summary>
        static Texture2D[] LoadPictures()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Root + "/Textures" });
            var paths = new System.Collections.Generic.List<string>();
            foreach (var g in guids) paths.Add(AssetDatabase.GUIDToAssetPath(g));
            paths.Sort(System.StringComparer.Ordinal);
            var list = new System.Collections.Generic.List<Texture2D>();
            foreach (var path in paths)
            {
                var t = LoadPicture(path);
                if (t != null) list.Add(t);
            }
            if (list.Count == 0) Debug.LogWarning("[CarpetSceneBuilder] no carpet pictures found in " + Root + "/Textures");
            return list.ToArray();
        }

        static Texture2D LoadPicture(string path)
        {
            // Texture2D.width/height report the IMPORTED size, and the default NPOT scaling would resize a 1024x1280 rug
            // to 1024x1024; CarpetHandover.PictureFit would then fit the wrong aspect and crop the rug's ends.
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null && imp.npotScale != TextureImporterNPOTScale.None)
            {
                imp.npotScale = TextureImporterNPOTScale.None;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void MakeChild(Transform parent, string name, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mr = go.AddComponent<MeshRenderer>();
            go.AddComponent<MeshFilter>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        /// <summary>FloorRoot (identity, destroyed and recreated like CarpetRoot): FloorSimulation + FloorRenderer + DebrisSystem, children FloorSurface / FloorWater / FloorFoam.</summary>
        static GameObject BuildFloorRoot(GameObject carpetRoot, FloorSettings floorSettings, ComputeShader floorCompute, ComputeShader debrisCompute,
                                         Material surface, Material water, Material foam, Material chunk, Material dust, Material drop)
        {
            var oldQuad = GameObject.Find("Floor");            // pre-floor-system placeholder quad
            if (oldQuad != null) UnityEngine.Object.DestroyImmediate(oldQuad);
            var old = GameObject.Find("FloorRoot");
            if (old != null) UnityEngine.Object.DestroyImmediate(old);

            var root = new GameObject("FloorRoot");
            root.transform.position = Vector3.zero; root.transform.rotation = Quaternion.identity;
            var carpet = carpetRoot != null ? carpetRoot.GetComponent<CarpetSimulation>() : null;

            var floor = Ensure<FloorSimulation>(root);
            floor.compute = floorCompute; floor.settings = floorSettings; floor.carpet = carpet;

            var view = Ensure<FloorRenderer>(root);
            view.surfaceMaterial = surface; view.waterMaterial = water; view.foamMaterial = foam; view.foamSubdiv = FloorConsts.FloorW / 2;
            view.waterSubdiv = FloorConsts.FloorW / 4;

            var debris = Ensure<DebrisSystem>(root);
            debris.compute = debrisCompute; debris.settings = floorSettings; debris.carpet = carpet; debris.floor = floor;
            debris.carpetRoot = carpetRoot != null ? carpetRoot.transform : null;
            debris.chunkMaterial = chunk; debris.dustMaterial = dust; debris.dropMaterial = drop;

            // child placeholders (meshes are built by FloorRenderer at Awake; materials assigned here for the inspector)
            MakeChild(root.transform, "FloorSurface", surface);
            MakeChild(root.transform, "FloorWater", water);
            MakeChild(root.transform, "FloorFoam", foam);
            return root;
        }

        static Camera SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = FindOrCreate("Main Camera", out _);
                go.tag = "MainCamera";
                cam = Ensure<Camera>(go);
            }
            // outside the if: a scene that already had a camera may still have no listener, and then ToolAudio
            // plays into nothing (Docs/AUDIO.md 4)
            Ensure<AudioListener>(cam.gameObject);
            cam.transform.position = new Vector3(0f, 1.5f, -1.12f);
            cam.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            cam.fieldOfView = 45f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 20f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.58f, 0.62f, 1f);
            cam.allowHDR = true;
            cam.allowMSAA = true;
            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null)
            {
                data.requiresColorOption = CameraOverrideOption.On;
                data.requiresDepthOption = CameraOverrideOption.Off;
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.None;
            }
            EditorUtility.SetDirty(cam);
            return cam;
        }

        static void SetupLight()
        {
            Light light = null;
            var go = GameObject.Find("Directional Light");
            if (go != null) light = go.GetComponent<Light>();
            if (light == null)
            {
                foreach (var l in UnityEngine.Object.FindObjectsByType<Light>())
                    if (l.type == LightType.Directional) { light = l; break; }
            }
            if (light == null)
            {
                go = new GameObject("Directional Light");
                light = go.AddComponent<Light>();
                light.type = LightType.Directional;
            }
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            light.intensity = 3f;
            light.color = new Color(1.0f, 0.98f, 0.95f, 1f);
            light.shadows = LightShadows.None;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.66f, 1f);
            RenderSettings.sun = light;
            EditorUtility.SetDirty(light);
        }

        static void SetupVolume()
        {
            Volume vol = null;
            var go = GameObject.Find("Global Volume");
            if (go != null) vol = go.GetComponent<Volume>();
            if (vol == null)
            {
                foreach (var v in UnityEngine.Object.FindObjectsByType<Volume>())
                    if (v.isGlobal) { vol = v; break; }
            }
            if (vol == null)
            {
                go = new GameObject("Global Volume");
                vol = go.AddComponent<Volume>();
                vol.isGlobal = true;
            }
            var profile = vol.sharedProfile;
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, SetDir + "/CarpetVolumeProfile.asset");
                vol.sharedProfile = profile;
            }
            if (!profile.TryGet<Tonemapping>(out var tone)) tone = profile.Add<Tonemapping>(true);
            tone.active = true;
            tone.mode.overrideState = true; tone.mode.value = TonemappingMode.ACES;

            if (!profile.TryGet<ColorAdjustments>(out var ca)) ca = profile.Add<ColorAdjustments>(true);
            ca.active = true;
            ca.postExposure.overrideState = true; ca.postExposure.value = 0.0f;
            ca.saturation.overrideState = true; ca.saturation.value = 12f;

            if (!profile.TryGet<Bloom>(out var bloom)) bloom = profile.Add<Bloom>(true);
            bloom.active = true;
            bloom.threshold.overrideState = true; bloom.threshold.value = 1.2f;
            bloom.intensity.overrideState = true; bloom.intensity.value = 0.25f;

            if (!profile.TryGet<Vignette>(out var vig)) vig = profile.Add<Vignette>(true);
            vig.active = true;
            vig.intensity.overrideState = true; vig.intensity.value = 0.2f;

            EditorUtility.SetDirty(profile);
            EditorUtility.SetDirty(vol);
        }

        static void SetupPipeline()
        {
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp == null) urp = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            if (urp == null) { Debug.LogWarning("[CarpetSceneBuilder] no UniversalRenderPipelineAsset active; MSAA not set"); return; }
            urp.msaaSampleCount = 4;
            urp.supportsHDR = true;
            urp.supportsCameraOpaqueTexture = true;
            EditorUtility.SetDirty(urp);
        }

        /// <summary>
        /// UI Toolkit HUD: one UIDocument fed by UI/CarpetHud.uxml + UI/CarpetHud.uss (Docs/HUD.md).
        /// The PanelSettings asset is created here so the scene never depends on a hand-made asset;
        /// its theme is the stock runtime theme, imported from a one-line .tss next to the UXML.
        /// </summary>
        static void BuildUI()
        {
            HudArtBaker.Bake();

            var panel = BuildPanelSettings();
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(HudUxmlPath);
            if (tree == null) Debug.LogError("[CarpetSceneBuilder] HUD UXML not found at " + HudUxmlPath);

            var old = GameObject.Find("CarpetUI");
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
            var ui = new GameObject("CarpetUI");
            var doc = ui.AddComponent<UIDocument>();
            doc.panelSettings = panel;
            doc.visualTreeAsset = tree;
            // localization (Docs/LOCALIZATION.md): texts, the per-script Noto fonts (UI/Fonts), RTL; order -50, before the views
            var localizer = ui.AddComponent<HudLocalizer>();
            Debug.Log("[CarpetSceneBuilder] " + LocFontBuilder.Ensure());
            LocFontBuilder.Wire(localizer);
            var hud = ui.AddComponent<CleaningHUD>();
            // economy side of the HUD (Docs/ECONOMY.md 4-5); ToolAudio is added after BuildUI, so UiAudio finds it at runtime
            var uiAudio = ui.AddComponent<UiAudio>();
            uiAudio.bank = AssetDatabase.LoadAssetAtPath<UiSoundBank>(UiSoundBaker.BankPath);
            var ecoHud = ui.AddComponent<EconomyHud>();
            ecoHud.uiAudio = uiAudio;
            // the pause menu (Docs/PAUSE_MENU.md): Esc / Menu, volume, the in-game credits the CC BY sounds require, Quit
            var menu = ui.AddComponent<PauseMenu>();
            menu.uiAudio = uiAudio;
            menu.economyHud = ecoHud;
            menu.localizer = localizer;
            menu.credits = AssetDatabase.LoadAssetAtPath<TextAsset>(CreditsPath);
            if (menu.credits == null) Debug.LogError("[CarpetSceneBuilder] credits not found at " + CreditsPath);
            // the inspection lamp (Docs/HIGHLIGHTS.md): Z / X, one-time shop items, shader globals _Highlight / _HighlightTune
            var lamp = ui.AddComponent<SurfaceHighlight>();
            lamp.uiAudio = uiAudio;
            lamp.hud = hud;
            hud.highlight = lamp;
            var ecoGo = GameObject.Find("Economy");
            if (ecoGo != null) ecoHud.economy = ecoGo.GetComponent<Economy>();
            lamp.economy = ecoHud.economy;
            var root = GameObject.Find("CarpetRoot");
            if (root != null)
            {
                hud.sim = root.GetComponent<CarpetSimulation>();
                hud.tools = root.GetComponent<ToolController>();
                hud.handover = root.GetComponent<CarpetHandover>();
                ecoHud.sim = hud.sim; ecoHud.tools = hud.tools; ecoHud.handover = hud.handover;
                menu.sim = hud.sim; menu.tools = hud.tools;
                lamp.tools = hud.tools;
            }
        }

        static PanelSettings BuildPanelSettings()
        {
            HudArtBaker.EnsureFolders();
            if (!File.Exists(HudThemePath))
            {
                File.WriteAllText(HudThemePath, "@import url(" + "\"unity-theme://default\"" + ");");
                AssetDatabase.ImportAsset(HudThemePath, ImportAssetOptions.ForceSynchronousImport);
            }
            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(HudThemePath);
            if (theme == null) Debug.LogError("[CarpetSceneBuilder] runtime theme failed to import: " + HudThemePath);

            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(HudPanelPath);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, HudPanelPath);
            }
            panel.themeStyleSheet = theme;
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.referenceResolution = new Vector2Int(1920, 1080);
            panel.match = 0.5f;                 // same compromise the old CanvasScaler used
            panel.sortingOrder = 10f;
            panel.clearColor = false;
            EditorUtility.SetDirty(panel);
            AssetDatabase.SaveAssets();
            return panel;
        }

        static void BuildEventSystem()
        {
            var old = GameObject.Find("EventSystem");
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        /// <summary>
        /// Settings/EconomySettings.asset (Docs/ECONOMY.md). Created with the C# defaults only when missing, so a tuned
        /// economy survives a Build Scene (the ToolSoundBank rule).
        /// </summary>
        static EconomySettings BuildEconomySettings()
        {
            string path = SetDir + "/EconomySettings.asset";
            var s = AssetDatabase.LoadAssetAtPath<EconomySettings>(path);
            if (s == null)
            {
                s = ScriptableObject.CreateInstance<EconomySettings>();
                AssetDatabase.CreateAsset(s, path);
                EditorUtility.SetDirty(s);
            }
            return s;
        }

        /// <summary>FxSparkle.mat for CelebrationFx: additive, HDR tint above the bloom threshold.</summary>
        static Material BuildSparkleMaterial()
        {
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(HudArtBaker.WorldSparkle) == null) HudArtBaker.Bake();
            var m = LoadOrCreateMaterial(MatDir + "/FxSparkle.mat", "CarpetCleaning/FxSparkle");
            m.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(HudArtBaker.WorldSparkle));
            m.SetColor("_Tint", Color.white);
            m.SetFloat("_Intensity", 5f);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>The wallet, the job clock and the upgrade rules (Docs/ECONOMY.md). Kept, not recreated, like Steam.</summary>
        static void BuildEconomy(GameObject root, EconomySettings settings)
        {
            var go = FindOrCreate("Economy", out _);
            var eco = Ensure<Economy>(go);
            eco.settings = settings;
            eco.sim = root.GetComponent<CarpetSimulation>();
            eco.handover = root.GetComponent<CarpetHandover>();
            eco.tools = root.GetComponent<ToolController>();
            var fx = root.GetComponent<CelebrationFx>();
            if (fx != null) fx.economy = eco;
        }

        /// <summary>Steam lifetime + rich presence (Docs/STEAM.md) + achievements (Docs/ACHIEVEMENTS.md). Kept, not recreated,
        /// so a tuned initInEditor / steamInEditor survives a re-run. Runs after BuildEconomy (the "Economy" object exists).</summary>
        static void BuildSteam(GameObject root)
        {
            var go = FindOrCreate("Steam", out _);
            var service = Ensure<SteamService>(go);
            service.sim = root.GetComponent<CarpetSimulation>();
            service.tools = root.GetComponent<ToolController>();
            var presence = Ensure<SteamRichPresence>(go);
            presence.sim = service.sim;
            presence.handover = root.GetComponent<CarpetHandover>();
            var achievements = Ensure<SteamAchievements>(go);
            achievements.sim = service.sim;
            achievements.handover = presence.handover;
            achievements.tools = service.tools;
            var economyGo = GameObject.Find("Economy");
            achievements.economy = economyGo != null ? economyGo.GetComponent<Economy>() : null;
            if (achievements.economy == null) Debug.LogWarning("[CarpetSceneBuilder] no Economy object: SteamAchievements finds it at runtime");
        }
    }
}
