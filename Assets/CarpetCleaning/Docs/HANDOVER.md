# Hand over to client, and the next carpet (2026-09-20)

User request: a button that appears when the carpet is completely clean and dry; pressing it flies the carpet
off screen as if handed to the client. The floor mess stays. A second button then brings in a new carpet with a
different picture, smoothly into place. The point is the choice: mop the floor while nothing is in the way, or
take the next carpet and work over a dirty floor.

## 1. State machine (`Scripts/CarpetHandover.cs`, order 210, on CarpetRoot)

`Idle -> FlyOut -> Away -> FlyIn -> Idle`

- `CanHandOver` = `Idle && (debugForceWin || sim.IsWon)`; `CanTakeNew` = `Away`.
- `Begin()`: input off, carpet collider off, foam surface hidden, one-shot debris cull of the carpet rect.
- On reaching `Away`: the three carpet renderers are disabled, the stage is parked back at identity, input is
  turned **back on** (the floor is still there to work on, the bare rect included) and the current tool is
  switched to the first floor tool as a hint, not as a restriction - every tool stays selectable (section 6).
- `TakeNewCarpet()`: next picture + base colour, `sim.ResetCarpet(seed + 1, notify: false)` (fresh dirt at a random
  level, `DIRT_LEVEL.md`), debris cull again,
  renderers on, stage at the entry pose, `FlyIn`.
- On reaching `Idle`: collider on, foam surface restored, `sim.CarpetAway = false` (the floor wall closes again and
  wipes the rect, hidden under the carpet that has just landed), tool set to `sim.SuggestedTool`.
- `SnapToIdle()` is called from `Awake` and from `CarpetReset`, so a recompile, a Play restart or the Reset
  button always puts everything back at the home pose.

Order 210 is after `CarpetRenderer` (200) so `Stage` and the child renderers exist in `Awake`; `Stage` is a lazy
property, so a reorder cannot break it. Keyboard shortcuts H and N mirror the two buttons.

## 2. Why a visual-only stage transform

`DebrisSystem.Render()` draws **every** particle, floor chunks included, through
`_CarpetToWorld = carpetRoot.localToWorldMatrix`, and `ToolController.TryGetCarpetUV` intersects the pointer ray
with the plane of the carpet root. Animating `CarpetRoot` would drag the whole floor mess along with the carpet
and make the floor tools unusable while it flies.

So `CarpetRenderer` now builds a `CarpetStage` child and parents `CarpetShells`, `WaterSurface` and
`FoamSurface` to it; `ToolCursor`, the `BoxCollider`, the simulation and the debris frame stay on `CarpetRoot` at
the origin. `Setup` adopts a child built before `CarpetStage` existed, so an old scene keeps working, and
`CarpetSceneBuilder` creates the stage so a rebuild is idempotent (verified: two builds in a row leave
`CarpetRoot` with exactly `CarpetStage` + `ToolCursor`).

The carpet shaders sample the simulation by mesh UV and take world position through `TransformObjectToWorld`, so
a translation is exact. Two constraints follow from the shading:

- **Never scale the stage.** The shells displace by millimetres (`pos.y = t * pileMm * 0.001`); a scale would
  grow the pile.
- **Keep rotations small.** Fibre, water and foam normals are built in object space and lit directly, so they do
  not rotate with the mesh. 18 degrees of tilt is fine, a flip would not be.

The foam surface is hidden for the whole flight: in the carpet domain it reads the floor foam field in a 24 mm
rim band and snaps its rim vertices to the floor grid pitch, which is meaningless once the mesh is a metre away.
The water surface is carpet-local and stays.

## 3. Flight path

Camera at (0, 1.5, -1.12), pitch 55 deg, vertical FOV 45, so `forward = (0, -0.819, 0.574)`,
`up = (0, 0.574, 0.819)`, and a point leaves the top of the frame when `dot(rel, up) / dot(rel, forward) > 0.414`.
A top exit is aspect independent (the FOV is vertical), which a sideways exit is not.

| | position | rotation | duration | easing |
| --- | --- | --- | --- | --- |
| FlyOut | (0.25, 0.65, 1.75) | Euler(-18, 4, 6) | 0.85 s | `InQuad` on position (zero velocity while still visible), rotation eased over the first 70 % |
| FlyIn | from (0.10, 0.45, 1.60) | Euler(14, -3, -4) | 0.95 s | `OutCubic(t/0.82)` on position, `OutCubic(t)` on rotation so the tilt flattens after the slide stops |

The entry tilt has the opposite sign, so the arrival reads as a rug being rolled out toward the player rather
than as a rewind of the exit. Verified: at the end of the fly-out all four carpet corners are at viewport
y >= 1.136, i.e. fully off the top of the frame (a corner at rest sits at 0.033).

Both poses and both durations are serialized fields, so they can be retuned from the CLI without a recompile.

On top of the table, `ApplyPose` lifts the stage just enough that no corner goes below its resting height (the tilt
pivots on the rug centre and would otherwise dip a corner under the tiles, section 10).

## 4. Carpet-only reset

`ResetCarpet(int newSeed)` keeps its old meaning; the new `ResetCarpet(int newSeed, bool notify)` skips the
`CarpetReset` event, which is what `FloorSimulation` and `DebrisSystem` use to wipe the floor and kill every
particle. The hand-over passes `false`, so the floor state and the floor debris survive the swap. The Reset
button, the R key and `CarpetDebugApi.Reset` still reset the whole scene. Since 2026-09-24 the Reset button and R
go through `ResetToNewCarpet()`: the next picture, colour and seed, i.e. a brand-new carpet with new dirt, plus that
full reset. `CarpetDebugApi.Reset(seed)` keeps the picture (`DIRT_LEVEL.md` 1).

The seed goes up by one per carpet: `K_Init` maps it through `seed * 0.618 + 7.31` into every noise offset, and
the carpet's dirt profile (level 0 spotless .. 1 fully soiled, and the dust / grime / crust / spill mix) is hashed
from it. Consecutive integers therefore give unrelated carpets, and the sequence is reproducible for tests
(`DIRT_LEVEL.md`, 2026-09-24). The first carpet of a play session rolls its seed (`CarpetSimulation.randomFirstSeed`).

`ResetCarpet` also clears pending stamps, resets the emit counter and recomputes `_initStats`, so a dirty new carpet
has `Cleanliness` 0 and `IsWon` false before it is even visible: the win cannot re-trigger. A spotless roll is won
at once, but `CanHandOver` needs `Idle`, so its hand-over button only appears once it has landed.

## 5. Pictures: museum rugs and original cartoon designs (2026-10-19)

`CarpetSceneBuilder.LoadPictures()` collects every `Texture2D` under `Assets/CarpetCleaning/Textures`, sorted by
path so the order is the same on every machine, and serializes them on `CarpetHandover.pictures`. Drop a PNG or
JPG in that folder and re-run `CarpetCleaning/Build Scene` to add a carpet design. Designs under
`Textures/Resources/CarpetPictures/` also join at runtime: `CarpetHandover.Awake` merges
`Resources.LoadAll<Texture2D>("CarpetPictures")` before material setup, preserving the serialized order and
appending resource textures in ordinal name order. Asset-reference deduplication prevents a second copy after
a later Build Scene includes those files. This keeps the existing scene usable without running the builder.
By default the first carpet of a session is `CarpetSceneBuilder.PicPath` (`rug_transylvanian_medallion.jpg`),
written into `Materials/CarpetFibers.mat`; a nonempty override pool below takes priority.

**Full pool restored (2026-10-25).** All 11 designs are active: five museum rugs, five generated cartoon
designs and the user's supplied `test.png`, imported as `Textures/Resources/CarpetPictures/rug_user_test.png`.
The supplied rug was moved from `CarpetPicturesOverride` with its metadata intact; that override folder is
now empty. The previous temporary single-picture selection (2026-10-21) is no longer in use. A nonempty
`Resources/CarpetPicturesOverride` replaces the active pool, sorted by texture name. `CarpetHandover.ActivePictures`
drives first-picture validation, the shuffled bag, resets and handovers; `pictures` retains the full catalogue.
The builder also uses the first override for its initial material. With the override empty, the regular
catalogue and runtime Resources merge apply. The ten previous designs remain unchanged on disk.

The supplied file was WebP despite its `.png` extension. It was decoded to a true RGB PNG at **850 x 973**;
decoded pixels match exactly. No artwork changes or resampling were applied. Import uses NPOT None, mipmaps,
sRGB and clamp wrapping. The existing cover fit crops about 8.42% of the width evenly across the two sides.
This is a user-supplied image; it is not classified as museum CC0 or as one of the generated originals below.

The folder holds five photographs of real historic rugs from museum open-access collections. All five are
**public domain / CC0**: they can ship without attribution, and the credit below is courtesy only. The SpongeBob
print and `cat_meme.jpg` (CC BY-SA 2.0, which needed attribution) were removed on 2026-09-23.

| file | rug | source (object page) | licence |
| --- | --- | --- | --- |
| `rug_anatolian_star.jpg` | Carpet, Turkey, probably late 18th - early 19th c., Met 1974.149.29 | https://www.metmuseum.org/art/collection/search/452581 | Met Open Access, CC0 |
| `rug_transylvanian_medallion.jpg` | 'Transylvanian' Carpet, Turkey, probably 18th c., Met 22.100.91 | https://www.metmuseum.org/art/collection/search/447549 | Met Open Access, CC0 |
| `rug_european_green.jpg` | Carpet, European, 1814-30, Met 10.152.14 | https://www.metmuseum.org/art/collection/search/218949 | Met Open Access, CC0 |
| `rug_salor_turkmen.jpg` | Salor Main Carpet, southern Turkmenistan, late 18th - early 19th c., Met 1974.149.46 | https://www.metmuseum.org/art/collection/search/452598 | Met Open Access, CC0 |
| `rug_finnish_rya.jpg` | Carpet ("Ryijy" / "Rya"), Central Finland, 1701-25, Art Institute of Chicago 1924.458 | https://www.artic.edu/artworks/12821 | AIC public domain, CC0 |

Picked for distinct palettes (navy/red/white, orange-red medallion, green, deep red guls, gold/blue shag) and
for a rug aspect within 5 % of the carpet's 4:5. Processing (Python/PIL; the scripts are not kept in the project):

1. Separate the rug from the museum backdrop (colour distance, or a "darker or saturated" mask where cream fringe
   looks like the backdrop).
2. Fit a robust line to each edge and warp that quad to a rectangle (old rugs lie skewed).
3. Inset each edge by 0.2 - 2.8 % so no fringe, kilim end or backdrop is left.
4. Resample to exactly **1024 x 1280** (4:5); the non-uniform stretch is at most 4.3 %. Save as JPEG q92.

An upright rug reads upright in game: the rya's figures stand.

Since 2026-10-19 the pool also includes five bright, original cartoon designs generated for Refluff at the
user's explicit request. They depict a fox, axolotl, dragon, capybara and cat; no existing franchise character
was used as a reference. They are generated artwork, **not museum open-access / CC0 assets**. Files live in
`Assets/CarpetCleaning/Textures/Resources/CarpetPictures/`:

- `rug_cartoon_fox.png`
- `rug_cartoon_axolotl.png`
- `rug_cartoon_dragon.png`
- `rug_cartoon_capybara.png`
- `rug_cartoon_cat.png`

All five are opaque RGB PNGs at the generator's native **1122 x 1402** resolution (approximately 4:5), copied
unchanged with NPOT scaling disabled, mipmaps enabled, sRGB and clamp wrapping. `PictureFit` crops less than one
pixel of aspect excess; no resampling or artwork edits were applied. Together with the five museum rugs they
make ten designs; the supplied rug brings the default catalogue to 11. Their Resources location includes
them in players even before the next scene rebuild. The default runtime merge preserves the authored first
carpet and the shuffled-bag selection rules. Exact prompts,
generation mode and asset hashes: [`Tools/rug_art/cartoon_rugs.json`](../../../Tools/rug_art/cartoon_rugs.json).
Implementation and file checks were performed offline; Unity validation was deliberately left to the user.

**Fit.** A rug photo covers the whole 1200 x 1500 mm carpet, so the rug's own border is the carpet's edge.
`CarpetHandover.PictureFit(tex)` scales the picture to cover without stretching and crops the excess evenly. A 4:5
picture gives offset (0, 0) and scale (1, 1); another aspect loses its overflow at both ends. The builder calls the
same function for the first carpet, so the material and the hand-over can never disagree. `CarpetFibers.shader`
samples the picture at the strand root with a clamp, so strands bent past the rim keep the edge colour. The old 3 %
feather was for a print floating on a plain base and is gone. The base colour bag (six mid-dark colours; lighter
ones blow out under the 3x sun) still runs, but an opaque rug hides it. It only shows through a picture with
transparent texels.

**Backing (revised 2026-10-21).** Shell 0 repeats the same picture and cover fit as the fibres, matching the
local design colour throughout the carpet. Lighting and gap occlusion keep it visibly recessed. The previous
whole-image white/black selection has been removed. This applies to the first carpet, every swap and the
temporary single-image pool automatically; see [`BACKING_TONE.md`](BACKING_TONE.md).
The backing also follows local cleanliness: dust and crumb masks use the same live local grit channel as
the tips, alongside the existing mud/stain/crust shading, so cleaning reveals both picture surfaces together.

Selection is a shuffled bag held in **instance** fields (domain reload is disabled, so a static bag would leak
across play sessions): every picture is used once before any repeat, then the bag is reshuffled and its first
entry is swapped away if it matches the last picture shown.

`CarpetHandover.Awake` clones the fibre material into a runtime instance
(`hideFlags = DontUnloadUnusedAsset`) and only ever writes to that, so `Materials/CarpetFibers.mat` on disk keeps
its authored values. Awake clears the bags and last-picture bookkeeping, then identifies the current texture
again even if disabled scene/domain reload has kept the runtime material alive.

**Import gotcha:** `Texture2D.width/height` report the *imported* size. With Unity's default NPOT scaling a
1024 x 1280 rug imports as 1024 x 1024 and the fit crops the rug's ends. `LoadPicture` forces `npotScale = None`
before any size is read, both for `LoadPictures` and for the builder's first carpet.

## 6. The bare rect: every tool, everywhere (2026-09-20)

The carpet simulation keeps running while the carpet is invisible: `FloorSimulation.LateUpdate` returns early on
`carpet.Paused` and `DebrisSystem` derives its dt from `carpet.SimTime`, so pausing the carpet would freeze the
floor and the player could not mop.

**One flag** now carries the whole state: `CarpetSimulation.CarpetAway` (`[NonSerialized]`, written only by
`CarpetHandover.SetState`), true from the end of the fly-out until the next carpet has **landed**. Holding it
through `FlyIn` is deliberate: the rect is wiped the instant the wall closes, and that instant must be the one
where the arriving carpet covers it, not an open-floor pop.

| consumer | how it reads the flag | effect |
| --- | --- | --- |
| `CarpetSim.compute` | `UploadStamps` sends `_StampCount = 0` while away (the C# `StampCount` still reports the real count) | no tool can wet an invisible carpet, whoever queued the stamp - mouse, hotkey or `CarpetDebugApi` |
| `FloorSim.compute` | `_CarpetAway` -> `CarpetCoversCMM()` in `DebrisCommon.hlsl` | `underCarpet`, `FloorWallCell` and the +40 mm bed term all switch off: the rect injects, flows, foams and dries like any tile |
| `Debris.compute` | same helper in `GroundAt` / the pile-edge bounce | chunks fall to tile height inside the rect instead of onto a ghost pile |
| `FloorSurface`, `FloorWater`, `CarpetFoam(_FLOOR_DOMAIN)` | global `float _CarpetAway` (set in `FloorSimulation.SetGlobals`) | the rect stops being cut out of the floor; `FieldAt` returns the pure floor field, so foam is seated on the tile, not on a phantom pile top |

The wall term moved **out of the bed texture**: `K_FInit` writes plain tile relief and `K_FFlux` adds the 40 mm at
read time (`BedAt`). `_FBed` is written once, so a baked wall could not have been switched off mid-play.

`ToolController` lost `CarpetToolsEnabled` and `BlockCarpetStamps` with this: **every tool is always selectable
and always does something**, and no stamp is ever dropped (the floor sim consumes the same buffer). The collider
is still disabled while away, and `TryGetCarpetUV` only reports `PointerOnCarpet` with the collider enabled, so
the cursor sits at tile height over the bare rectangle. The HUD greys nothing out any more.

Verified in Play (2026-09-20, scripted frames):

- Away, hose held 1 s at uv (0.5, 0.5) - dead centre of the rect: floor `water` 0 -> 0.0423, `pooledCells` 0.0098,
  carpet stats **completely unchanged** (`water` 0). One mop pass over it: water -71 %, `wetFilm` x2.
- Detergent inside the rect: `detergent` 0 -> 1.45e-3. Foam sprayer + brush inside the rect: `foam` 0.0174.
- `TakeNewCarpet` -> landing: the rect reads 0 in every channel again, and a hose hold on the new carpet puts
  `water` 0.18 on the **carpet** with the floor still at 0, i.e. the wall is back.
- Floor state **outside** the rect survives a full cycle untouched (detergent 1.5009e-3 -> 1.4981e-3, only its
  normal drying).

## 7. Deliberate limits
- The rect is wiped when the carpet lands, so anything left inside it is lost. Sharing the puddle with the floor
  underneath would need a carpet that soaks up what it is laid on, which the carpet sim has no term for.
- The flight ignores `Paused` and `TimeScale` on purpose: it is presentation, and a `TimeScale` of 4 must not
  turn a hand-over into a 0.2 s blur. Freezing it on `Paused` would strand the carpet in mid-air with no HUD way
  to resume.
- With a single picture in the folder the rug repeats every hand-over; the dirt seed and the stain hues still
  change (the base colour does too, but an opaque rug covers it).
- The carpet is always 1200 x 1500 mm, so a rug far from 4:5 loses its ends to the cover fit. A long runner
  (the Met's Chinese carpet 42748 is 0.64) was rejected for that reason, not stretched.

## 8. Testing from the CLI

`CarpetDebugApi`: `HandoverNow(force)`, `TakeNewCarpetNow()`, `SetForceWin(bool)`, `HandoverStep(dt)`,
`HandoverSeek(t01)`, `HandoverSnap()`, `HandoverJson()`, `StageCornersJson()`, `DebrisAliveJson()`.

`HandoverSeek` writes the pose without accumulating time, which is what makes a capture deterministic. Note the
opposite trap to the usual one: while the Editor is **focused** it advances real frames between CLI calls, and a
0.85 s flight finishes in a single large `Time.deltaTime` tick. For capture runs set `flyOutSeconds` /
`flyInSeconds` to a large value first (they are serialized fields), seek, capture, then restore them.

Overlay UI does not appear in `capture_game_view --source screen` while the Editor is unfocused, because the
Game View never repaints. For a HUD screenshot switch the canvas to `ScreenSpaceCamera` with `Camera.main`
temporarily; `--source camera` then renders the UI with the scene.

## 9. Pay slip at the hand-over (2026-09-24, `ECONOMY.md`)

- **The hook.** `StateChanged(FlyOut)` is where `Economy` pays: only `Begin()` raises it. `Economy` re-reads the stats
  synchronously there, credits and saves, and `EconomyHud` plays the card while the rug flies away.
- **The clock.** Every Idle entry restarts the job clock: `Awake`, a full reset, the landing after FlyIn.
- **Keys.** H and N are also gated by `ToolController.UiModal` (upgrade shop open).
- **Glints.** `CelebrationFx` emits them into `CarpetStage/CleanSparkles` (local space, so they ride the flight) and
  clears them at Away.
- **Card and a new carpet.** FlyIn finishes the card, so it never overlaps a landing carpet.

## 10. A carpet corner dipped under the tiles in flight (found 2026-09-26; fixed in code 2026-09-26, pending Play verification)

Found while staging the store clip 4 ("A new rug every job"); an art-director review caught it on round 2.

- **What you saw.** On every fly-out the near-left corner of the outgoing rug sank through the tiles before the rug
  lifted away.
- **Measured** with real Steps (`Tools/steam_art/clips/recipes/clip4_rugs/_diag/c4_clamp_probe.cs`): the lowest
  corner reached about **-78 mm** (-77.9) on the way out (t 0.05-0.65) and -5.6 mm on landing. An offline numpy
  replay of `ApplyPose` gives the same numbers (-77.8 mm at t 0.44, -5.7 mm).
- **Cause** (`ApplyPose`). The tilt pivots on the rug centre and runs ahead of the lift:
  - FlyOut eases the position with `InQuad(t)` but the tilt with `Smooth(t / 0.70)`, so `outEuler (-18, 4, 6)` drops the
    near-left corner before the lift catches up;
  - FlyIn stops the slide at t 0.82 while the tilt keeps flattening until t 1, so a corner touches down early.
- **Fix** (`CarpetHandover.ApplyPose`, the same rule as the clip's hook). After the pose is computed and before
  `st.localPosition` is written: `pos.y = Mathf.Max(pos.y, CornerLift(rot))`.
  - `public static float CornerLift(Quaternion rot)` turns the four corners (+-0.6, 0, +-0.75) m by `rot` and returns how
    far the stage must rise so none sits below y 0, the resting backing top.
  - Every corner stays at or above its resting height on both legs: the near corner stays on the floor while the far
    edge lifts, then the whole rug leaves; on landing the rug touches down on a corner and settles.
  - `Seek` (the capture hook) goes through `ApplyPose`, so seeks get the lift too.
  - There is no other posed state: Idle and Away return before the pose is written, and `Advance` / `SnapToIdle` write
    the identity pose.
  - It is a no-op when the rug lies flat: the identity gives a lift of 0, and `pos.y >= 0` on both legs.
- **Clip workaround, now redundant.** `clip4_rugs/c4_clamp_on.cs` installed a runtime
  `RenderPipelineManager.beginCameraRendering` hook doing the same (probe with the hook: worst 0.0 mm out and in).
  With the fix it lifts nothing and can be dropped; `run.sh` may keep calling it.
- **Verify in Play** (after the recompile). Run the side-effect-free probe
  `Tools/steam_art/trailer/fix_probes/v1_corner_lift.cs` (reflection puts the stage in FlyOut / FlyIn and walks `Seek`, with no
  `Begin`, payout or carpet swap). Expect `out worst 0.0` and `in worst 0.0` (before: -77.9 / -5.6), with rest 0.0 mm.
  Then re-measure a real hand-over with `c4_clamp_probe.cs`. That probe calls `Begin(true)`, which pays: snapshot
  `carpet_save_editor.json` first.
