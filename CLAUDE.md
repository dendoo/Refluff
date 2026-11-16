# CarpetCleaning — agent guide

ASMR-style carpet cleaning prototype in Unity 6000.7.0b1 / URP 17.7 / D3D12, new Input System only.
Goal and quality bar: visibly fluffy shell-textured carpet, realistic water + foam + dirt physics
(RT-based compute simulation), NOT an alpha-erase. Favour visual fidelity over feature count.
Git repository: `https://github.com/dendoo/Refluff.git` (main; binary assets use Git LFS). Full docs live in `Assets/CarpetCleaning/Docs/` (read `ARCHITECTURE.md` first,
then the feature docs; append to `SESSION_LOG.md` at the end of every working session).

## Layout
- `Assets/CarpetCleaning/Compute/CarpetSim.compute` + `SimCommon.hlsl` — carpet simulation (384x480 cells, mm/s units).
  The squeegee is a geometric plow (K_PlowScatter/Apply/Drain, fixed-point `_PlowAcc` + off-carpet spill ring), not a `_ToolFlow` velocity — `Docs/SQUEEGEE.md`.
  Pile lay = `_FiberSet` (K_Fiber, `Docs/FIBER_SET.md`): pressing tools, flowing water (the hose's spread) and the dry vac's suction lay the pile;
  damp pile keeps the lay until dried or re-laid, dry pile always springs back upright over seconds (`SET_DRY_RELAX`; user rule).
  A new carpet's dirt = seed-hashed `DirtProfile` (`CarpetSimulation.RollDirt`: level 0 spotless .. 1 fully soiled, spread, dust / grime / crust / spill mix) spent by K_Init on a broad
  mottled soil field; dust lives in the grit channel and `CarpetFibers` draws it as a grey film. Reset / R = `CarpetHandover.ResetToNewCarpet` (next picture + seed, full reset);
  the first carpet of a session rolls its seed. Tune the look offline with `python Tools/dirt_preview.py out.png <rug.jpg> [seeds]` (numpy port, keep in step) — `Docs/DIRT_LEVEL.md`.
- `Assets/CarpetCleaning/Compute/FloorSim.compute` + `FloorCommon.hlsl` — floor simulation (768x768, 3.6 m square around the carpet).
- `Assets/CarpetCleaning/Compute/Debris.compute` — GPU particles: crust chunks, dust puffs, splash droplets.
- `Assets/CarpetCleaning/Scripts/` — `CarpetSimulation` (owns carpet RTs, LateUpdate order 100), `DebrisSystem` (105),
  `CarpetHandover` (210; hand-over / new-carpet state machine, animates the `CarpetStage` child only — `Docs/HANDOVER.md`),
  `FloorSimulation` (110), `CarpetRenderer`/`FloorRenderer` (200), `ToolController` (input -> ToolStamp),
  `SimSettings` / `FloorSettings` (ScriptableObjects; every public float field is uploaded as `_FIELDNAME`), `CarpetDebugApi` (scripted strokes, stats JSON, debug PNGs).
- `Assets/CarpetCleaning/Scripts/Audio/` — `ToolAudio` (120; machine + surface loop layers per tool, `StepFrame(dt)` also called by `StepAll`),
  `ToolSoundBank` (ScriptableObject: clip slots per tool, empty slot = procedural recipe), `ProceduralSfx` (synthesiser; only `ToClip` touches the engine,
  the rest runs on a worker thread because a 2 s loop costs tens of ms). Clips baked to `Audio/Generated/*.wav` by `Editor/ToolSoundBaker` — `Docs/AUDIO.md`.
  Never ship audio ripped from videos. Since 2026-09-25 EVERY tool plays real recordings (Freesound CC0 / CC BY + one Mixkit; AUDIO.md 11,
  credits `Docs/AUDIO_CREDITS.md` — CC BY needs them in the game's credits and on the store page); placeholders are the per-slot fallback.
  Sources `Audio/Sources/`, clips + cut JSONs `Audio/Recorded/`; `Tools/audio_recordings.py list|credits|build|fetch` is the manifest (every source,
  licence and exact cut; `build` is bit-reproducible; the sources are Freesound previews — rebuild from the originals before release).
  Cut loops / one-shots with `Tools/prepare_recording.py` (`--grains` re-joins stroke middles; `--limit DB` only for sparse peaks), import with
  `ToolSoundBaker.ImportClip(ToolId, json)` (credit -> `clipCredits`); `ImportShippedRecordings()` = what the bank ships with (the builder runs it on
  a new bank); `Bake`/`Clear` never touch recorded slots. Only CC0 / CC BY / Mixkit: read the licence on the sound's own page.
  Six machines are whole takes (dry + wet vac, dryer, hose, foam, detergent): `RecordedMachine` plays start -> `PlayScheduled`-stitched loop -> stop,
  entering each transition at the current speed (motor line, or `--speed envelope` = level for sprays). The dry vac's is InspectorJ's Dyson (CC BY 4.0,
  since 2026-09-26); the old Samsung take (Freesound 100210, **CC BY-NC 4.0**) stays on disk as a reference only and must never ship (`SteamBuild` warns).
  Cut with `Tools/cut_machine_recording.py`, import with `ToolSoundBaker.ImportMachineRecording(ToolId, json)`, test the transitions with
  `CarpetDebugApi.RecordedMachineScenario(ToolId, ...)` (virtual clock: the real `dspTime` stands still in an unfocused editor) — AUDIO.md 9.
  One tool only: `ToolSoundBaker.BakeTool(ToolId)` and `ToolSoundBank.ResetVoicingKeepClips(ToolId)` (keeps clips, credits, recordings).
  Synthesis rules (learned from a "sounds alien" verdict): noise-first, no sine grains, no periodic LFOs, speed/spin drive a
  non-resonant low-pass not pitch; SVF band gain = Q at centre. Vet any clip with `python Tools/analyze_sfx.py <wav folder>` (`--hits` for impacts).
  Vacuum tube rattle = GPU crumb-capture events (`Debris.compute` `_SuckEvents`, exact vanish frame) -> async readback in
  `DebrisSystem.DebrisSucked` -> `ToolAudio` PlayScheduled; regression test `CarpetDebugApi.SuckTimingBegin/Run/Report` (AUDIO.md 8).
- `Assets/CarpetCleaning/UI/` — UI Toolkit HUD: `CarpetHud.uxml` (structure), `CarpetHud.uss` (ALL style; tokens are USS vars on `.hud`),
  `CarpetHudPanel.asset` (PanelSettings), `UnityDefaultRuntimeTheme.tss`, `Textures/hud_shadow_*.png` (baked by `Editor/HudArtBaker.cs`). `Docs/HUD.md`.
  The HUD is UI Toolkit, not uGUI — a UI Toolkit panel is invisible to `EventSystem.IsPointerOverGameObject()`, so `CleaningHUD` feeds
  `ToolController.PointerOverUI` its own `panel.Pick()` test over elements classed `ui-blocking`.
  Pause menu (`Docs/PAUSE_MENU.md`): `Scripts/UI/PauseMenu.cs` (228, before EconomyHud 230): Esc / Menu -> Resume, master volume (PlayerPrefs), Language, Credits
  (`UI/Credits.txt`, the in-game CC BY credits the licences require; keep it in step with `AUDIO_CREDITS.md`), Quit. Modal like the shop (`UiPaused` + `UiModal`).
- Localization (`Docs/LOCALIZATION.md`, 2026-09-27): all 31 Steam interface languages; Menu > Language drop-down (`Scripts/UI/LanguagePicker.cs`),
  saved in PlayerPrefs `refluff.language`, else Steam's game language (`SteamService.GameLanguage`), else the OS, else English; `Refluff.exe -language ja` for one run.
  Keys = the English text as shown: `Scripts/Localization/Loc.cs` (`Loc.T/F`, `Loc.Key` marks a literal passed through a variable), tables
  `Localization/Resources/Loc/<code>.json` (+ `en.json` = key list, written by `python Tools/loc/loc_tool.py sync` from `Tools/loc/strings.json`,
  the translator source with context). `Scripts/UI/HudLocalizer.cs` (-50) re-texts every UNNAMED UXML label whose text is a key (named labels are C#'s),
  puts the script's font on the HUD root, RTL for Arabic, classes `lang-*` / `script-*` / `lang-rtl` on `.hud`, and keyboard-layout letters on `.kbd` chips.
  Fonts: built-in Inter for Latin / Greek / Cyrillic / Vietnamese; subset Noto Sans Arabic / Thai / JP / KR / SC / TC in `UI/Fonts`
  (`python Tools/loc/build_fonts.py`, sources in `Tools/loc/fonts_src`, then menu CarpetCleaning/Localization/Build Font Assets = `Editor/LocFontBuilder`).
  Debug: `CarpetDebugApi.LocJson/SetLanguage(code)/LanguagePick/LanguageForget/LocOverflow`. Rich presence: `Tools/Steam/rich_presence_<steam api language>.vdf` x31.
- `Assets/CarpetCleaning/Scripts/Steam/` — Steamworks.NET 2025.164.1 (UPM git package). `SteamService` (-1000; relaunch through Steam, init, overlay -> `sim.Paused` + `ToolController.Suspended`),
  `SteamRichPresence` (220), `SteamConfig` (App ID 5304930, depot 5304931 — also in `steam_appid.txt` and `Tools/Steam/*.vdf`). Player = `Refluff.exe` (`SteamBuild.ExeName` + `upload.ps1` + Steamworks launch option), company `VahovGames`, product `Refluff` (save folder `LocalLow/VahovGames/Refluff`). Builds: `Editor/SteamBuild.cs`
  (menu CarpetCleaning/Steam) -> `Builds/Steam/content/windows` (upload) or `Builds/Steam/local` (+steam_appid.txt). Upload: `Tools/Steam/upload.bat -Account <login> [-Preview]` (never sets a branch live). `Docs/STEAM.md`.
  Every build writes `CREDITS.txt` + `THIRD_PARTY_NOTICES.txt` (from `Tools/Steam/make_notices.py`) next to the exe and warns about a managed dll without a notice.
  Every build also sets the exe / taskbar icon and the full-screen defaults (`SteamBuild.EnsurePlayerSettings`, `STEAM.md` 5.1). The icon files are
  `AppIcon/refluff_icon_<n>.png`, one per slot from 1024 down to 16, cut from the Steam icon art by `Tools/steam_art/export_app_icon.py`; keep them free of NPOT scaling.
  Check a built exe with `python Tools/Steam/check_exe_icon.py`.
  Achievements (`Docs/ACHIEVEMENTS.md`, 2026-09-30): 10 + 2 stats, `Scripts/Steam/Achievements.cs` (pure rules; `AchievementId` order = Steamworks creation
  order = token index, append only) + `SteamAchievements` (222; save `achieved[]`/`toolsUsed` = local truth, two-way sync; `steamInEditor` OFF by default
  because Editor Play is the developer's real Steam account). Texts/icons/upload files in `SteamStore/achievements/` (`README.md` = Steamworks steps):
  `python Tools/Steam/achievements.py check|build [--tokens dl.vdf]|show`, `python -B Tools/steam_art/make_achievement_icons.py`. Debug: `CarpetDebugApi.Achievements*`.
  Steam Cloud = Auto-Cloud on `carpet_save.json` only (`STEAM.md` 11), no code; `SteamJson().cloud`. Steamworks (2026-09-30): stats, 10 achievements
  (tokens `NEW_ACHIEVEMENT_3_<i>`), 31-language localization and the cloud config are saved; Publish (the user presses it) and Supported Features still open (README status block).
  v0.9.0 = BuildID 25558788, live on `default` since 2026-09-27, uploaded via SteamPipe **Web Uploads** (ZIP of the content root, no steamcmd login; `STEAM.md` 7.1).
  v0.9.1 (31 languages + the exe icon) = BuildID 25562494, uploaded with steamcmd on 2026-09-27, live on `default` (set by the user; seen 2026-09-28): the user sets builds live and publishes them.
  To upload, the agent opens `Tools/Steam/upload_window.ps1` in a console window, where the user types the login, password and Steam Guard code (`STEAM.md` 7.2).
  steamcmd caches the login in `Tools/Steam/steamcmd/config` (a credential: never ship or copy that folder).
  Both Steamworks checklists are COMPLETE (price $3.99); "Mark as ready for review" and the build review are the user's buttons (`Docs/RELEASE_CHECKLIST.md`).
  Smoke-test a player copy with `steam_appid.txt` AND its folder as the working directory (else it relaunches through Steam); grab its frame with `PrintWindow`, never a screen copy.
  Start the smoke test WITHOUT screen arguments, or afterwards delete the `Screenmanager *` values under `HKCU\Software\VahovGames\Refluff`.
  The player restores its last screen mode, so one windowed run makes every later start on this machine windowed, Steam installs included (`STEAM.md` 5.1).
- Store art (`Docs/STORE_ART.md`): official title **"Refluff: Carpet Cleaning ASMR"**. Everything for the Steam page lives in `SteamStore/` (outside Assets; `README.md` = upload map),
  built by `Tools/steam_art/` (`make_logo.py`, `build_store_art.py`, `verify_assets.py` must print ALL OK). Captures: `StoreShotApi.Shot(name)` = game camera at 4K
  + HUD layer at the Game view's 2560x1440 (UI Toolkit does not re-layout for another RT size) -> `compose_screens.py`; `RenderView(...)` = free camera + temporary DoF for key art.
  Screenshots = fixed game camera only; key art may use free cameras / more shells / post. Tonemap = ACES since 2026-09-26 (user decision; capsules / key art are still Neutral plates).
  Description clips (`Docs/STORE_CLIPS.md`, `SteamStore/description/`): `Editor/StoreGifApi` = real-Step takes, the tool driven by `ToolController.ScriptedPointer` (park it off the play area
  in ANY paused-Step capture: those frames read the OS mouse) -> `Tools/steam_art/make_clips.py` (MP4, < 11.5 s) + `make_headers.py` (image headers).
  Trailer (`Docs/TRAILER.md`, RU; section 12 = how it was made): **shot and edited 2026-09-27, uploaded the same day** ("Refluff - Gameplay Trailer", before the screenshots, own cover; `TRAILER.md` 13). Master `SteamStore/trailer/refluff_trailer_main.mp4`
  (1:00, 1080p60, -14 LUFS) + cover. #07 / #08 were retaken 2026-09-28 with the pile lay (cut 3.1, `recipes/retake_0928`); Steam still has the 09-27 master
  (kept in `SteamStore/_work/trailer/master_0927/`) until the user presses Replace. 60 fps takes: `StoreGifApi.SeqBegin(..., fps: 60, audioLog: true)` (Step advances `Time.fixedDeltaTime`; the take sets
  it to 1/60 and restores it). Sound = the game's own foley: the take logs its audio state per Step (`Scripts/Audio/AudioTap.cs`, virtual audio clock,
  `AUDIO.md` 12) and `Tools/steam_art/trailer/foley.py` renders it offline (an unfocused Editor's `AudioRenderer` records silence). Edit = code:
  `Tools/steam_art/trailer/edl.json` -> `make_trailer.py` (`--audio-only` remuxes a new mix) -> `check_trailer.py`; shoot recipes `Tools/steam_art/trailer/recipes/`.
- TikTok 9:16 (`Docs/TIKTOK.md`, 2026-10-04): `SteamStore/tiktok/refluff_tiktok_fibers.mp4` (25.7 s) from macro takes (48 shells, raking sun, free camera hooked on
 `RenderPipelineManager.beginContextRendering`: `onBeforeRender` does not fire in `Step`), `Tools/steam_art/tiktok/` (EDL, recipes); `make_trailer.py` / `check_trailer.py`
 take the canvas from the EDL `size`, `--safe` = TikTok UI zone. VO text `voiceover_en.txt`; the EDL `voice` slot ducks the foley when the user's WAV arrives.
- Store page localization (`Docs/STORE_LOCALIZATION.md`, 2026-09-28): the 29 recommended store languages (game's 31 minus English, Arabic), saved in
  Steamworks. Text source `SteamStore/localization/source_en.json` + `<code>.json` (plain-text fields, reviews in `_review/`); `python Tools/steam_art/store_loc.py
  check|build|show|glossary` assembles the BBCode and writes Steam's "Upload Localization" JSONs (its import ignores `[rec]` sysreqs: set those in the form).
  Header images per language: `make_headers.py` (HarfBuzz + FreeType, one size per language) -> `SteamStore/description/<code>/hdr_*_<steam lang>.png`;
  Steamworks groups `<name>_<language>.png` with `<name>`, so every language's BBCode references the English names. Supported Languages = exactly 31.
- `Assets/CarpetCleaning/Scripts/Economy/` + `Scripts/UI/` — money per carpet, tool upgrades, pay-slip card, shop (`Docs/ECONOMY.md`). `Economy` (215, `Economy` object kept
  across Build Scene) pays on `CarpetHandover.StateChanged(FlyOut)`, V = starting DirtScore / `dirtRef`, JSON save in `persistentDataPath` (`carpet_save_editor.json` in the Editor).
  Upgrades reach the tools only through static `ToolTuning`: AREA = `ToolTuning.Effective(t)` (stamp, plow, cursor, head scale), POWER = `_ToolPower` uniform
  (`DebrisCommon.hlsl`, uploaded as P-1) multiplying effect rates in K_Stamp / K_FInject. `EconomyHud` (+ `PayoutView`, `ShopView`, `UiFx`) on CarpetUI, `UiAudio` + `UiSoundBank`.
  The shop pauses via `CarpetSimulation.UiPaused` / `ToolController.UiModal` (separate from the Steam overlay's `Paused` / `Suspended`).
- Inspection lamp (`Docs/HIGHLIGHTS.md`): one-time shop items `ShopItem` (Moisture highlight Z $250, Dirt highlight X $400; `SaveData.items`,
  `Economy.Owns/TryBuyItem`, event `ItemChanged`). `SurfaceHighlight` (225, on CarpetUI) = keys + globals `_Highlight` / `_HighlightTune`
  (zeroed in OnDisable); `Shaders/HighlightCommon.hlsl` (fibres, water, foam, floor, chunks, splash drops) dims the surfaces and glows the pay slip's own
  measures (DirtScore weights amber, w + h/W_CAP blue). Captures: `CarpetDebugApi.SetHighlight(which, on)`; `EconomyDebugApi.BuyItem/SetOwned`.
  On the carpet the lamp follows the HUD bars: K_Reduce also fills log histograms of those measures (stats buffer 256 uints), and
  `CarpetSimulation.WaterLevel` -> `_HighlightCut` makes a cell glow only with what it holds above the level. Nothing is lit while
  `ShownPercent` reads 100 (HIGHLIGHTS.md 4.1). Keep the HIST_* constants in CarpetSim.compute and CarpetSimulation in step.
- Tool heads: Tripo `.glb` -> `Editor/ToolModelPostprocess` (menu `Normalize Tool Models`, or `Normalize(ToolId)` for one) -> `Prefabs/Tools/*.prefab`
  -> `Settings/ToolModelSet.asset` -> `ToolModelView` (205, placed by `CarpetRenderer.SetCursor`). The brush head is `Tool_Brush_v2.glb` (Tripo, part-segmented):
  its blue bristle parts spin at 350 rpm under a still grey top while it works (`SplitByParts`, colour rule `IsBristle`), and it sinks into the pile
  (`pileSinkM`) — `Docs/TOOL_MODELS.md` 7. Test a head without the pointer:
  `CarpetDebugApi.PinToolHead(tool,u,v,working)` + `ToolHeadJson()`.
  Overwrite a Mesh asset through the Mesh API, never `EditorUtility.CopySerialized` (its GPU copy stays stale until reload).
- The brush is a rotary floor machine in the sims and audio too (`Docs/ROTARY_BRUSH.md`): `BrushPadVelocity` in `DebrisCommon.hlsl`, `SimSettings`
  `BRUSH_RPM/SWIRL/FLING` (FloorSimulation copies them), `SfxRecipe.RotaryMotor`. Keep `BRUSH_RPM` = `ToolModelSet` brush `spinRpm`.
- `Assets/CarpetCleaning/Shaders/` — `RoomReflection.hlsl` (the ceiling with long light fixtures that standing water mirrors: without it a puddle on the
  white tile read as a shadow; `FLOOR_SYSTEM.md` 5.1), `CarpetFibers` (shells; dried-mud slabs `DIRT_LEVEL.md` 3.1), `CarpetWater`, `CarpetFoam` (foam mound surfaces for carpet + floor, keyword `_FLOOR_DOMAIN`; `Docs/FOAM_LOOK.md`), `FloorSurface`, `FloorWater`, `DebrisChunk`, `DebrisDust`, `DebrisDrop`, `ToolCursor`.
- `Assets/CarpetCleaning/Textures/` — carpet pictures = 5 public-domain/CC0 museum rug photos, 1024x1280 (4:5), each covers the whole carpet
  (`CarpetHandover.PictureFit`); every texture in the folder joins the hand-over bag on Build Scene. Only add public-domain/CC0 images; credits in `Docs/HANDOVER.md` 5.
- `Assets/CarpetCleaning/Editor/CarpetSceneBuilder.cs` — idempotent scene builder, menu `CarpetCleaning/Build Scene`. It is the source of truth for materials, lights, camera, volume and object wiring. Re-run it after adding scene objects.
- `Assets/CarpetCleaning/Settings/SimSettings.asset`, `FloorSettings.asset`, `ToolSoundBank.asset` — keep the `.asset` values and the C# defaults in sync
  (the builder writes sound-bank defaults only when it has to create the asset, so tuned values and assigned clips survive a re-run).

## Hard rules (learned the expensive way)
- Tool upgrades change the tools. `CarpetDebugApi.Reset` switches `ToolTuning` to Neutral (factory tools) so scripted recipes and SESSION_LOG numbers stay comparable;
  call `EconomyDebugApi.LiveTuning(true)` only when a test is about the upgrades. Never read `ToolTable` geometry directly for a tool that acts: use `ToolTuning.Effective`.
- Unity caps a compute kernel at 8 UAVs here; a 9th silently breaks binding. Count every `RW*` resource bound to a kernel. Read-only inputs must be declared `Texture2D`/`StructuredBuffer`, not `RW*`.
- All slowly-changing simulation state RTs are `R32G32B32A32_SFloat`. Half floats lose sub-ulp per-frame increments (evaporation, wicking) — water vanished because of this once.
- Runtime RTs/buffers referenced only from private fields need `hideFlags = HideFlags.DontUnloadUnusedAsset`, or editor asset refreshes destroy them.
- Legacy `UnityEngine.Input` does not work (activeInputHandler = new Input System). Use `Keyboard.current` / `Mouse.current`.
- URP 17.7: fog keywords come from `#include_with_pragmas Fog.hlsl`; Forward+ keyword is `_CLUSTER_LIGHT_LOOP`. Custom shaders declare sim globals OUTSIDE `CBUFFER_START(UnityPerMaterial)`.
- Do not hand-edit `SampleScene.unity`; change `CarpetSceneBuilder` and re-run it.
- Enter Play Mode options: domain reload and scene reload are DISABLED, so statics persist between play sessions; static caches must be reset in `Awake`.
- The UI is localized into 31 languages: never show a new English literal directly. Unnamed UXML labels must carry a key; C# goes through `Loc.T/F`.
  Every new or changed string goes into `Tools/loc/strings.json` AND all 30 `Resources/Loc/<code>.json`, then `python Tools/loc/loc_tool.py sync && python Tools/loc/loc_tool.py check`
  (0 errors). A character outside the fonts' everyday sets needs `build_fonts.py` + Build Font Assets (a regenerated TTF with the old dynamic FontAsset draws wrong glyphs).
  Never size a text box for English: `min-width`, wrap, or equalize from C#; check with `CarpetDebugApi.LocOverflow()` per language.
- Never ship `steam_appid.txt` (release builds and the depot script strip it). Keep `ProjectSettings/SteamworksNETSettings.json` at `canManageDefineSymbols: false`:
  otherwise Steamworks.NET and the Windows build profile fight over `STEAMWORKS_NET` and trigger a recompile inside Play mode on every session.

## Driving the live Editor (Unity CLI Pipeline package)
Run from Git Bash with `export MSYS_NO_PATHCONV=1` (otherwise "/Main Camera" becomes "C:/Program Files/Git/Main Camera").
- `unity status` — editor reachable? `unity command editor_status`.
- Shader / compute compile check WITHOUT a domain reload:
  `unity command eval --code "UnityEditor.AssetDatabase.ImportAsset(\"Assets/CarpetCleaning/Shaders/X.shader\", UnityEditor.ImportAssetOptions.ForceSynchronousImport); var sh=UnityEngine.Shader.Find(\"CarpetCleaning/X\"); var m=UnityEditor.ShaderUtil.GetShaderMessages(sh); return m.Length+\" msgs \"+string.Join(\" | \", System.Array.ConvertAll(m, x=>x.severity+\": \"+x.message+\" @\"+x.line));"`
  `unity command eval --code "UnityEditor.AssetDatabase.ImportAsset(\"Assets/CarpetCleaning/Compute/X.compute\", UnityEditor.ImportAssetOptions.ForceSynchronousImport); var cs=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.ComputeShader>(\"Assets/CarpetCleaning/Compute/X.compute\"); var m=UnityEditor.ShaderUtil.GetComputeShaderMessages(cs); return m.Length+\" msgs \"+string.Join(\" | \", System.Array.ConvertAll(m, x=>x.severity+\": \"+x.message+\" @\"+x.line));"`
- C# syntax/type check WITHOUT a domain reload: concatenate the changed .cs files into one file under the scratchpad and run
  `unity command run_script --file <path.cs> --dry_run true` (Roslyn in-memory against project assemblies). Note: Editor-only files (`CarpetSceneBuilder`) need `UnityEditor` which IS available in the editor domain.
- Real compile: `unity command recompile` then poll `unity command recompile_status`; errors via `unity command console --level error`.
- Play: `unity command editor_play`, stop `unity command editor_stop`. Scripted test: `unity command eval --code "CarpetCleaning.CarpetDebugApi.Reset(1); CarpetCleaning.CarpetDebugApi.StrokeNow(0, 0.2f,0.3f, 0.8f,0.3f, 60, 1f); return CarpetCleaning.CarpetDebugApi.StatsJson();"` (tool ids: 0 scraper, 1 dry vac, 2 hose, 3 foam, 4 brush, 5 squeegee, 6 wet vac, 7 dryer, 8 floor detergent, 9 mop; 8/9 are floor-only).
  `Reset(seed)` builds the FULLY soiled carpet (dirt level 1) that every recorded number assumes; `Reset(seed, -1)` = the game's roll for that seed, `Reset(seed, 0.3f)` = its mix at that level;
  `ResetWithProfile(seed, level, crust, mud=-1, dust=-1, spill=-1)` = that level with chosen mix fields (negative = keep the seed's roll); `NewCarpetNow()` = what R does; `DirtJson()` = the profile of the carpet on the sim.
- Audio state in Play: `unity command eval --code "return CarpetCleaning.CarpetDebugApi.AudioJson();"`; clip levels/loop seams: `CarpetCleaning.ToolSoundBaker.Report()`.
  A ru-RU editor turns `ToString("F3")` into decimal commas and breaks JSON — always pass `CultureInfo.InvariantCulture`.
- Steam state in Play: `unity command eval --code "return CarpetCleaning.CarpetDebugApi.SteamJson();"`; fake the overlay with `SteamSimulateOverlay(true/false)`.
- Economy in Play: `CarpetCleaning.EconomyDebugApi.EconomyJson()`, `Preview(clean, dry, sec, V)`, `Grant(n)`, `SetLevel(tool, stat 0 power / 1 area, level)`, `ResetSave()`,
  `ShowPayout(...)` + `PayoutSeek(t)` (exact frozen frame of the card), `ShopOpen(bool)`.
  Test money without code: menu CarpetCleaning/Economy (Add $10k/$100k/$1M, Test Money Window) - works in Play (via Economy) and in Edit mode (edits the save file).
- HUD screenshot (works unfocused, unlike `--source screen`): `CarpetCleaning.EconomyDebugApi.CaptureHud("Screenshots/x.png")` = UI Toolkit panel capture over a camera render.
- Player build: `unity command menu --path "CarpetCleaning/Steam/Build Windows (release, for upload)" --timeout 1800` (blocks until done; result line `[SteamBuild] OK ...` in the console).
  `EditorApplication.delayCall` does NOT fire while the Editor is unfocused, so do not schedule builds through it.
- Screenshot: `unity command capture_game_view --save_path Screenshots/x.png` -> `Assets/Screenshots/x.png` (camera render, no overlay UI; `--source screen` includes UI in Play Mode).
  It renders 1280x720 while `Camera.main.pixelWidth` is the Game view's 2560: scale `WorldToScreenPoint` before cropping.
- Long eval code: write it to a .cs file and use `unity command eval_file --file <path>` (Windows-style path). An eval times out after
  5 s of main-thread work (~1000 scripted frames); split long scenarios into several calls, sim state persists between them.
- Play mode does NOT advance frames while the Editor is unfocused (autotick/runInBackground do not help). Test with scripted frames (`CarpetDebugApi.StepAll` chain used by StrokeNow/Hold/RunFrames); for a mid-flight screenshot run a short stroke and capture immediately.
  Real frames anyway: `UnityEditor.EditorApplication.isPaused = true; UnityEditor.EditorApplication.Step();` runs one full player-loop frame
  (Update, LateUpdate, UI Toolkit, render; dt 0.02 s) synchronously per call, even unfocused: fades, USS transitions, the shop entrance.
  While paused `Screen.width` reads 640 (pass sizes: `CaptureHud(path, 2560, 1440)`); set `isPaused = false` before `editor_stop`.
  Keyboard presses cannot be faked from an eval (queued Input System events never give `wasPressedThisFrame` to the game): test the logic behind the key.
- A script recompile during Play mode wipes the private RenderTextures; always `editor_stop` -> `editor_play` after a recompile.
  Editing a `.cs` file while Play runs is enough: the next `ImportAsset`/refresh compiles it. Edit C# with Play stopped, or restart Play after. Changing C# defaults of a ScriptableObject does not change an existing `.asset`; sync it (reflection copy from a fresh instance + SaveAssets) or edit the asset.
- Compute compile errors can be LAZY: `GetComputeShaderMessages` right after `ImportAsset` may say 0 while a kernel fails for D3D12 (seen: `continue` inside an `[unroll]` loop -> "Kernel at index (N) is invalid" only at first Dispatch). After importing a compute, dispatch it once (`CarpetDebugApi.Reset(1); RunFrames(3)`) and re-check the messages and `console --level error`.
- Only ONE process should trigger `recompile`/`refresh` at a time; parallel agents must limit themselves to importing their own shader/compute files and to `run_script --dry_run`.

## Conventions
- Units: mm and seconds inside every simulation; metres only in Unity transforms. Carpet mm space (CMM): origin at carpet UV (0,0) corner, x along u (1200 mm), y along v (1500 mm). Carpet backing top = y 0, floor top = y -3 mm.
- Bounded decays only: `x -= x*(1-exp(-k*dt))` (`DecayAmount`). Clamp everything on write-back. Never let a state go NaN — guard divisions with `+EPS`.
- New feature = new doc in `Docs/`, plus a dated entry in `Docs/SESSION_LOG.md` with what was verified (numbers, screenshots).
