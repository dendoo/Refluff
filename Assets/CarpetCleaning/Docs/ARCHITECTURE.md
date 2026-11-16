# Architecture overview (first-person update 2026-11-15)

The playable camera is now a walking first-person workshop. `FirstPersonSceneBuilder`, invoked by the existing scene builder, creates `FirstPersonPlayer` (CharacterController), parents Main Camera to it (eye height 1.62 m, FOV 68), and creates a physical upgrade workbench. `ToolController` sends the same simulation stamps from a validated centre-screen ray. `FirstPersonToolView` joins generated tool bodies/contact heads to visible handles without arms. `FirstPersonHud` replaces the tool strips; upgrades open with E at the table. The simulation, economy and hand-over rules are retained. See `FIRST_PERSON.md` and `FIRST_PERSON_UI.md`.

## Runtime objects (built by CarpetSceneBuilder)
- `CarpetRoot` (identity transform, BoxCollider 3.6 x 0.02 x 4.5): `CarpetSimulation`, `CarpetRenderer`, `ToolController`, `CarpetHandover` (210).
  Children: `CarpetStage` (visual-only, animated by the hand-over) holding `CarpetShells` (48 stacked shell grids, `CarpetFibers.mat`),
  `WaterSurface` (256x320 grid, `CarpetWater.mat`) and `FoamSurface` (512x640 grid displaced by the foam mound, `CarpetFoam.mat`);
  plus `ToolCursor` and `ToolModels` directly on the root. The collider, the cursor, the tool head, the stamp path and the debris frame never move (`HANDOVER.md`).
  `ToolCursor` is now the fallback only: a tool with a generated head shows the 3D model instead (`TOOL_MODELS.md`).
  Carpet pictures are the builder's serialized texture list plus `Resources/CarpetPictures`, merged in
  `CarpetHandover.Awake` by asset reference while preserving the scene's first picture. The default catalogue contains five
  museum open-access rugs, five original generated cartoon designs and user-supplied `rug_user_test.png`
  (11 designs, restored 2026-10-25); resource designs join existing scenes without a builder run (`HANDOVER.md` 5).
  Since 2026-10-21, a nonempty
  `Resources/CarpetPicturesOverride` supplies `ActivePictures` instead of that catalogue, including the first
  carpet and the builder's initial material. The override folder is now empty; `rug_user_test.png` lives in
  `Resources/CarpetPictures`, so all 11 designs participate in normal selection.
- `FloorRoot` (identity): `FloorSimulation`, `FloorRenderer`, `DebrisSystem`. Children `FloorSurface` (big opaque plane, `FloorSurface.mat`),
  `FloorWater` (320x320 grid over the full 12 m room floor domain, `FloorWater.mat`), `FloorFoam` (640x640 grid, `FloorFoam.mat` = `CarpetFoam` shader
  with `_FLOOR_DOMAIN`). Debris is drawn with `Graphics.RenderMeshIndirect` (no GameObjects).
- `ToolAudio` sits on `CarpetRoot` too (execution order 120): two crossfading loop layers per tool (machine + surface) driven by
  `ToolController.StrokeSpeedMmPerSec` / `StampCount`, fed from `Settings/ToolSoundBank.asset`. Its five AudioSources hang off a
  runtime-only `ToolAudioSources` child (`HideFlags.DontSave`). Every tool plays real recordings (`Audio/Recorded/`, cut by
  `Tools/audio_recordings.py`, credits in `AUDIO_CREDITS.md`); six machines are whole takes played by `RecordedMachine`; the synthesised
  placeholders in `Audio/Generated/*.wav` are the fallback of every slot (`AUDIO.md` 11).
- `Steam` (2026-09-23): `SteamService` (order -1000: relaunch through Steam, SteamAPI init/callbacks/shutdown, overlay pause via
  `CarpetSimulation.Paused` + `ToolController.Suspended`) and `SteamRichPresence` (220). Steam is optional at runtime: a failed init
  only logs. Player builds and SteamPipe upload: `STEAM.md`. Since 2026-09-30 also `SteamAchievements` (222): 10 achievements + 2 stats
  from `Economy.Paid` / `Upgraded` / `ItemChanged` and `ToolController.Stamped`, the save as local truth, Steam only while ready (never
  from Editor Play unless `steamInEditor`) — `ACHIEVEMENTS.md`. The save file is synced by Steam Auto-Cloud (`STEAM.md` 11).
- `Economy` (2026-09-24, order 215, kept across Build Scene like `Steam`): the wallet, the job clock, the pay slip at every hand-over
  (`CarpetHandover.StateChanged(FlyOut)`) and the upgrade rules; JSON save in `persistentDataPath`. Upgrades reach the tools through the
  static `ToolTuning` (AREA: `ToolTuning.Effective` in `ToolController`; POWER: `_ToolPower` uniform in CarpetSim / FloorSim). `CelebrationFx`
  (216) on `CarpetRoot` with the `CarpetStage/CleanSparkles` particle system: glints on the rug during the hand-over flight (`ECONOMY.md`).
- `FirstPersonPlayer` (-100) starts at (0,-.003,-3.8); child `Main Camera` at eye height 1.62m, pitched 28 degrees, FOV 68, with `AudioListener` and `FirstPersonToolView` (207). `Workshop` contains wall/furniture colliders and `UpgradeWorkbench` (40); WASD + mouse, E opens upgrades. The workshop is 12x12x3.8 m with a closed north door, warm sun (38,-30), local bounce fills, a reflection probe, actual south/east window apertures and six dynamic shadowed volumetric shafts. `WorkshopFinishBuilder`, `WorkshopDetailBuilder` and `WorkshopAtmosphereBuilder` create the furnished environment; see `WORKSHOP_VISUALS.md` (2026-11-16). `Global Volume` keeps ACES with restrained bloom and vignette.
- `CarpetUI`: UI Toolkit `UIDocument`, `HudLocalizer` (-50, all 31 languages), `CleaningHUD`, `EconomyHud` (230, payout/shop), `FirstPersonHud` (235, reticle/tool belt/table prompt), `UiAudio` (125), `SurfaceHighlight` (225, Z/X), `PauseMenu` (228, Esc). `EventSystem` remains for modal UI. See `FIRST_PERSON_UI.md`, `HUD.md`, `LOCALIZATION.md`, `PAUSE_MENU.md`.

## Per-frame order (all in LateUpdate)
1. `CarpetSimulation` (100): K_ClearEdge -> K_Stamp -> K_Emit -> per squeegee sweep (K_PlowScatter, K_PlowApply) -> K_PlowDrain -> nSub x (K_Flux, K_Integrate) -> K_Chemistry -> K_Fiber (flow lay from the water velocity, then loops `_Stamps` for the fiber set, 5 UAVs) -> stats every 10 frames. The squeegee moves water and foam by the path its blade sweeps, not by `_ToolFlow` (`SQUEEGEE.md`).
2. `DebrisSystem` (105): reset spawn count -> K_DebSpawn (consumes the carpet's emit append buffer) -> K_DebSim -> RenderMeshIndirect x3 (chunks, droplets, dust).
3. `FloorSimulation` (110): K_FInject (carpet edge outflow + debris deposits + tool stamps) -> nSub x (K_FFlux, K_FIntegrate) -> K_FChem -> globals.
4. Renderers (200) push cursor globals. Camera renders.

The shop pauses the sims with `CarpetSimulation.UiPaused` (`IsPaused = Paused || UiPaused`, checked by the carpet and floor LateUpdate;
the debris system follows `SimTime`) and stops the tools with `ToolController.UiModal`; both are separate from the Steam overlay's flags.

`ToolAudio.StepFrame(dt)` runs from its own Update (unscaled dt) and from `CarpetDebugApi.StepAll`, so scripted strokes drive the mix
like real ones.

## Carpet simulation state (512x640, per cell, mm / s)
- `_Fluid` (h free water mm, u, v mm/s, s dissolved dirt "slurry" cell total), `_Soap` (sigma surfactant, F foam, Fd foam dirt, Fage),
  `_Dirt` (crust, mud, stain, grit in 0..1), `_Crust` (D scraper damage, plateShade, stainHue, e edge distance), `_Fiber` (bend.xy, f flattening, w pile moisture 0..1 of W_CAP),
  `_FiberSet` (bSet.xy, fSet: the lay pressed into the pile by tools, flowing water and the dry vac's suction; damp pile keeps it,
  dry pile springs back up over seconds; K_Fiber only, `FIBER_SET.md`), `_FiberVel` (vb.xy, chipAcc, dustAcc), `_Agit` (agit, inflow, dryer, collapse), `_ToolFlow` (vTool.xy, wt, wf), `_Bed`, `_Flux`, `_EdgeOut` / `_EdgeOut2` (this frame's runoff over the carpet edge).
- New carpet dirt (`K_Init`, `DIRT_LEVEL.md`): a per-carpet `DirtProfile` hashed from the seed (level 0 spotless .. 1 fully
  soiled, spread, and a dust / grime / crust / spill mix) spends a broad mottled soil field on loose dust (grit channel, a grey
  film in `CarpetFibers`), ground-in mud, crust patches and spill stains. Since 2026-10-19, every natural gameplay reset
  requires dust on at least 50 % of cells (`grit > 0.04`) and crust on at least 20 % (`crust > 0.1`); the layers can overlap.
  Natural rolls clamp dust/crust mix to at least 0.5/0.2, then use actual stats to raise the original profile's level/crust
  toward 1 in up to eight initialization retries if needed. Explicit forced levels/profiles retain their staging behavior.
  The first carpet of a play session rolls its seed;
  Reset / R = `CarpetHandover.ResetToNewCarpet` (next picture + seed, full reset). `CarpetDebugApi.Reset(seed)` still builds
  the fully soiled carpet for scripted tests. Offline look preview: `Tools/dirt_preview.py`.
- Open edges: the ghost neighbour has h = 0 and a bed `EDGE_DROP` (5 mm) lower, but only water above the bound-edge lip `EDGE_LIP` (0.3 mm) runs off and foam rides off only with spilling water or a tool push, so the rim cells keep their film and foam (`CARPET_EDGE.md`).
- Tools are 48-byte `ToolStamp`s (uv centre, dir, radius, blade length, pressure, speed, dtS, tool id, vStroke) queued by `ToolController.StrokeSegment` (every `spacingMm` along the stroke, hold stamp when stationary). The stamps land along the raw pointer path; their `dir` is the smoothed tool axis from `UpdateHeading` times the travel sign (`TOOL_MODELS.md` 6), because the raw per-frame direction is pixel-quantisation noise and because `FootprintWeight` reads `dir` as an axis while the fibre bend and `vStroke` read it signed.
- Stats: K_Reduce sums 14 quantities into a uint buffer (x256 fixed point; slots 12/13 count visible dust/crust coverage);
  `Cleanliness` is 1 - weighted dirt / initial.
  The same pass fills the inspection lamp's log histograms of the per-cell DirtScore and wetness (amount + count, 60 bins each;
  the buffer is `CarpetSimulation.StatsLen` = 256 uints), which `ApplyStats` turns into `_HighlightCut` (`HIGHLIGHTS.md` 4.1).
- Tools: 10 entries in `ToolTable`, selected by 1..9/0 or wheel in first person. 0..7 act on the carpet; 8 Detergent (key 9) and 9 Mop (key 0) are floor-only
  (`ToolDefinition.floorTool`) and have no branch in `CarpetSim.compute` (`FLOOR_TOOLS.md`). The legacy pointer-mode strips remain in UXML but are hidden in first person.
  All ten are selectable in every game state: while the carpet is away the carpet tools act on the bare tile it leaves
  behind, because the floor sim consumes the same stamp buffer and the carpet rect stops being a wall (`HANDOVER.md` 6).

## Rendering
- Fibers: shell texturing (48 layers, hashed 5 mm strand grid, cantilever bend from `_Fiber`, crust plate on the lower shells with Voronoi cracks, MSAA alpha-to-coverage).
  `CarpetRenderer.DefaultShellCount = 48` is shared with the builder (2026-10-21). Awake upgrades the previous
  serialized default of 24 to 48 before building the mesh, so existing scenes do not require a rebuild.
  Layer count includes shell 0 (backing). The enlarged 3.6x4.5 m rug retains physical pile height and strand spacing; the simulation is 512x640 at 7.03125 mm per cell (`PHYSICAL_SCALE.md`).
  Shell 0 is the backing between the tufts: it repeats the same local `_PictureTex` albedo as the fibres,
  with the same cover fit and alpha fallback (`BACKING_TONE.md`, revised user rule 2026-10-21).
  At shell 0, strand-root UV equals carpet UV; bending moves the fibres while their backing print stays in place.
  There is no whole-image light/dark classification, GPU readback or separate backing colour. Existing gap
  occlusion, mud, stains, crust and wet shading still apply, so the matching print looks recessed between tufts.
  Backing dust/crumb masks use tip-level coverage from the same live local `_Dirt.w`; cleaning clears that
  film on backing and fibres together. Actual fibre-shell height profiles remain unchanged (`BACKING_TONE.md`).
- Water: transparent grid displaced by pile top + h, refraction via the opaque texture, murk = slurry C screen-combined with the carpet dirt under the water (per-channel absorption + scalar silt extinction + lit silt in-scatter, `WET_LOOK.md`); discarded under an opaque foam mound (F > 1.2).
- Foam: separate displaced height-field surfaces (`FoamSurface` / `FloorFoam`, `CarpetFoam.shader`, Transparent+1, ZWrite On, premultiplied): mound up to ~45 mm from F, ray-traced bubble caps, pink-violet hue-preserving shading, height-field cast shadow, cross-domain field = union of the carpet mound and the tile heap so foam runs over the carpet edge without a valley or a seam (`FOAM_LOOK.md`, `CARPET_EDGE.md`).
- Wet pile: `wetV = sat(1 - exp(-3w) + sat(h/0.3))` darkens and saturates the fibre albedo (`_WetDarken`), drives wet specular/rim (`WET_LOOK.md`). Colour properties are sRGB->linear converted (Linear colour space).
- Inspection lamp (`HIGHLIGHTS.md`): `Shaders/HighlightCommon.hlsl`, included by `CarpetFibers`, `CarpetWater`, `CarpetFoam`,
  `FloorSurface`, `FloorWater`, `DebrisChunk` and `DebrisDrop`. It reads the globals `_Highlight` (dirt / moisture amount + scan sweep) and
  `_HighlightTune`, set by `SurfaceHighlight` and zeroed in its `OnDisable`. Each shader branches on a uniform test, so a dark lamp
  costs nothing. While on, the surfaces dim, and the pay slip's own measures glow: DirtScore weights amber, w + h/W_CAP blue.
- Globals set by `CarpetSimulation.SetGlobals`: `_FluidTex _SoapTex _DirtTex _CrustTex _FiberTex _BedTex _NoiseTex _SimTexel _CarpetMm _SimConsts _RimDir _RimColor`; by `FloorSimulation.SetGlobals`: `_FFluidTex _FSoapTex _FDepositTex _FBedTex _FloorRect _FloorTexel _FloorConsts _CarpetAway _F_TILE _F_GROUT_W _F_GROUT_DEPTH`; by `FloorRenderer.Awake`: the same tile defaults plus `_FloorFoamPitchMm` (the foam rim seam, `CARPET_EDGE.md`).

The floor deposit texture is `(dust, residue, wetFilm, detergent film)`; the fourth channel is the detergent (`FLOOR_TOOLS.md`).

The HUD hides everything except the Clean/Dry bars while a tool is in use, and those bars never block a click, so
the strips and buttons cannot cover debris the player is trying to reach (`HUD.md`).

See `SQUEEGEE.md` for the squeegee plow (speed-independent, mass-conserving roll, damp pile behind), `ROTARY_BRUSH.md` for the brush as a rotary floor machine (pad swirl, arcs on wet pile, motor hum), `FLOOR_TOOLS.md` for the detergent / mop model, `HUD.md` for the HUD containers and the working fade, `HANDOVER.md` for the carpet swap, `DIRT_LEVEL.md` for the random dirt level and soil mask of each new carpet, `WET_LOOK.md` for the water-murk / wet-pile shading rules, `FOAM_LOOK.md` for the foam surfaces, `FLOOR_SYSTEM.md` for the floor + debris design contract, `FLOOR_SIM_RENDER_NOTES.md` (floor sim/render implementation notes) and `DEBRIS_SYSTEM.md` (particle system notes), and `SESSION_LOG.md` for history / verified numbers.

## Testing without a mouse
`CarpetDebugApi` (Play mode, via `unity command eval`): `Reset`, `StrokeNow(tool,u0,v0,u1,v1,frames)`, `Hold(tool,u,v,s)`, `RunFrames(n)` all step the whole chain (`StepAll`: carpet -> debris -> floor); `StatsJson()`, `FloorStatsJson()`, `DebrisCount()`, `SaveDebugView`, `SaveFloorView`. UVs may lie outside [0,1] to hit the full room floor (u in [-7/6,13/6], v in [-5/6,11/6]). The Editor does not advance frames while unfocused, so `LiveStroke/LiveHold` (real-frame runner) only progress while CLI commands tick it.

