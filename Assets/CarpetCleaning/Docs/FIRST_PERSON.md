# First-person workshop (2026-11-15)

The game now starts in a walkable workshop with visible tools and no arms. The carpet/floor/debris compute simulation, dirt profiles, 48 fibre shells, audio, economy, achievements and carpet catalogue keep their existing data and rules.

## Scene and controls

`CarpetSceneBuilder.Build` calls `FirstPersonSceneBuilder.Build`. It creates the room, an invisible walking-floor collider, furniture, the upgrade workbench and `FirstPersonPlayer`, then parents Main Camera to the player. Do not edit SampleScene by hand.

- WASD / arrows: walk; Shift: faster walking.
- Mouse: look; left/right mouse: existing full/half-pressure cleaning.
- 1–9 / 0 or wheel: the ten tools, including both floor tools.
- E: open upgrades while aiming at the nearby workbench or its terminal.
- Esc: pause / close a modal. The cursor is released for the shop, pause menu, Steam overlay and loss of focus.
- H / N / R: existing hand-over, next-carpet and full new-carpet reset controls.
- Z / X: existing owned moisture/dirt inspection lamps.

The camera has 68-degree vertical FOV, 1.62m eye height and a 25mm near clip. Spawn is (0,-0.003,-3.8), looking down 28 degrees (2026-11-16 visual update; see `WORKSHOP_VISUALS.md`). Movement bounds are x/z -5.65..5.65m; CharacterController also resolves furniture collision. Mouse input is degrees per pixel, independent of frame time. Modal clicks are ignored until mouse buttons have been released.

## Aiming and simulation

`ToolController.player` enables the first-person input path. `TryGetFirstPersonUV` projects a centre-of-viewport ray onto the 3.6x4.5m carpet / full 12x12m room floor domain. Contact tools and the foam/detergent sprays have 2m camera-ray reach; the hose has no distance cap. Workshop colliders still block the aiming ray. Tile intersections use the actual floor height (-3mm), so shallow hose shots land under the reticle. It exposes `HasSurfaceAim`, `SurfaceAimUV`, `SurfaceAimWorld` and `SurfaceAimDirection` to the tool view and HUD. SurfaceAimWorld retains the old hover-height convention (surface +12mm).

The validated point passes through the same `StrokeSegment`, heading smoothing, stamp spacing and geometric squeegee plow. Initial tool heading follows camera ground-forward; stroke travel thereafter determines its smoothed axis. All effective dimensions still use `ToolTuning`.

`ScriptedPointer` takes precedence and disables the held view. Existing deterministic simulation/capture recipes remain available. The old camera composition itself is no longer the gameplay camera; historical fixed-camera store recipes must explicitly set their recorded pose when reused.

## Visible tools

AREA upgrades enlarge contact heads uniformly on all axes (2026-11-16): blade heads by the effective width ratio,
round heads by the effective radius ratio. Round details keep their shape; the shaft endpoint follows the scaled
HandleSocket. The hand grip and complete handheld machine bodies retain their usual size. See `TOOL_MODELS.md` section 8.

`FirstPersonToolView` on Main Camera joins existing generated contact heads to mechanical shafts, collars and grips. The hose and floor detergent now each have their own complete generated gun and flexible hose. The new rotary brush separates its stationary housing from the spinning blue bristle skirt (350 rpm, 8mm pile sink). Dry vacuum has a generated motor/grip body, suction tube and the new wet-extraction head scaled to its effective diameter. Measured socket points join the shafts to each head's mounting bracket, including AREA scaling. Stream visuals for water/foam/detergent end at the same valid surface point used for stamps. The legacy cursor outline is hidden in live FPS mode; the reticle marks aim.

The initial three missing tools were made with the requested reference-image → high-detail model → visual review → retopology → Unity import workflow:

| Tool | Tripo HD triangles | Imported GLB triangles |
| --- | ---: | ---: |
| Dry vacuum | 1,967,448 | 39,999 |
| Foam sprayer | 1,902,180 | 18,527 |
| Dryer | 1,984,199 | 19,310 |

References and exact built-in image-generation prompts: `Tools/first_person/references/`. Tripo: H3.1 Best Quality,
then Smart Mesh P1.0 with target 10,000 quads for foam and dryer. The accepted dry vacuum instead uses **40,000
target triangles with Smart Mesh OFF**, simplified from the original HD model restored through History.
The first dry-vacuum quad reconstruction (19,779 exported triangles) was rejected in Unity because its casing
had torn and overlapping faces, visible with both textured and plain clay materials. Correcting 392 opposed
face windings did not restore the shell. The rejected GLB is preserved outside Assets under
`Tools/first_person/rejected/`; the accepted triangle export has an intact casing and no degenerate faces.

All three GLBs contain 4K base colour and 2K metallic/roughness maps. Foam and dryer have 2K normal maps;
the accepted dry vacuum has a 1K normal map. Task IDs, simplification settings, full SHA-256 hashes, texture sizes
and geometry reports: `Tools/first_person/generation_manifest.json`. Total use: **290 existing Tripo credits**
(285 initial + 5 for the dry-vacuum retry; no subscription or credit purchase).

| Accepted export | Vertices | Zero-area faces | Invalid indices / non-finite vertices |
| --- | ---: | ---: | --- |
| Dry vacuum | 48,090 | 0 | 0 / 0 |
| Foam sprayer | 18,552 | 12 | 0 / 0 |
| Dryer | 21,048 | 7 | 0 / 0 |

Each export uses one mesh and one material. The small zero-area fragments in foam/dryer were retained because
their visible shells passed front, side and three-quarter review in Unity. Numeric reports and inspection
images, including the rejected vacuum, live in `reports/first_person/models/`. The reports also count face
normals opposing interpolated vertex normals (286/57/45 respectively); that measurement alone is not a
visual-acceptance verdict.

Sources live in `Models/FirstPerson/`; complete-body prefabs in `Prefabs/FirstPerson/` and contact heads in `Prefabs/Tools/`; held registration/nozzles are in `Settings/FirstPersonToolSet.asset`, with contact entries in `ToolModelSet.asset`. `FirstPersonModelImporter` creates the prefabs. `Tools/first_person/import_generated.cs` records the accepted orientation and nozzle fitting.

## Seven regenerated tools and larger room

The remaining seven tools were regenerated from individually reviewed original images using Tripo H3.1 Best Quality, reviewed in 3D, simplified with Triangle / Smart Mesh OFF to a 40,000-triangle target, and exported as GLB with 4K base textures. The initial three accepted bodies above are retained.

| Replacement | HD triangles | Simplified triangles |
| --- | ---: | ---: |
| Scraper | 1,915,466 | 39,999 |
| Hose | 1,982,210 | 40,000 |
| Brush | 1,918,645 | 40,000 |
| Squeegee | 1,953,457 | 39,998 |
| Wet vacuum head | 1,980,476 | 39,990 |
| Floor detergent gun | 1,917,846 | 40,000 |
| Mop | 1,945,598 | 40,000 |

Every new export has finite geometry, valid indices and zero degenerate triangles. References, prompts, Tripo IDs, import recipes and previews are under `Tools/first_person/regeneration_20261115/`; geometry/Unity review reports are under `reports/first_person/regeneration_20261115/`. Generation and remeshing consumed 420 existing Tripo credits. See `TOOL_MODELS.md` section10 for normalization, measured sockets and brush splitting.

The room is 12x12x3.8m, with a closed north door, luminous factory windows and six volumetric daylight shafts. The carpet is three times larger along each axis (3.6x4.5m); tools and pile keep their physical dimensions. Details and regression checks: `FIRST_PERSON_ROOM.md` and `PHYSICAL_SCALE.md`.

## Upgrade table and HUD

`UpgradeWorkbench` checks a clear 2.1m central ray. `EconomyHud.OpenFromWorkbench` opens the same existing shop and save-backed upgrade system. The old global U shortcut and shop button are disabled in first person. `FirstPersonHud` provides the reticle, current tool, ten numeric slots and localized controls; see `FIRST_PERSON_UI.md`.

## Verification

- `Tools/first_person/verify_controls.cs`: eleven checks for reach, sky, table occlusion/collision, boundaries, diagonal speed, scripted stamps and modal/overlay blocking.
- `Tools/first_person/verify_live.cs`: 25 checks using actual ToolController.Update and injected MouseState, including all ten tools, GPU stamp dimensions/pressure, centre-ray agreement, menu/focus/overlay gates and HUD picking. Steam achievement listening is disabled during this check.
- `Tools/first_person/verify_cycle.cs`: workbench interaction, pause and paid hand-over/new-carpet cycle; restores the economy and exact save bytes.
- `Tools/first_person/verify_ui.cs`: batched checks of HUD, workbench prompt and shop across 31 languages, with state restored between calls. Reports in `reports/first_person/`.
- `Tools/first_person/inspect_glb.py`: finite vertex/index validation, triangle counts, normal consistency and embedded texture dimensions. The accepted dry vacuum has no zero-area fragments; foam/dryer have 12/7. All three have valid indices and finite vertices. Stored JSON reports and the generation manifest include SHA-256 hashes of the accepted GLBs.
- Capture with `EconomyDebugApi.CaptureHud(path,2560,1440)`: the UI Toolkit panel retains the Game view layout, so a smaller capture crops the HUD.

The pre-conversion scene is retained outside Assets at `Tools/first_person/baseline/SampleScene_before_fps.unity`.
