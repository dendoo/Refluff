# Tool models

Every cleaning tool shows a small 3D **working head** where the flat outline cursor used to be.
The heads are generated with Tripo AI, normalised into prefabs, and placed by the same maths the
outline cursor already used.

## 1. Why the head only

The camera looks down at 55 deg from close range (`CarpetSceneBuilder.SetupCamera`: pos (0, 1.5, -1.12),
FOV 45). A full tool with a handle covers the patch the player is cleaning, which is the one thing the
game is about. A head sized to the tool's own footprint reads as "this is the squeegee" without hiding
the carpet.

## 2. Generation pipeline

The `Tripo3d_Unity_Bridge` folder in the project root is **not** used: it is a WebSocket receiver for
models pushed from the Tripo web frontend and cannot generate anything. Generation goes through the
already-installed `com.coplaydev.unity-mcp` package, whose `generate_model` tool has a Tripo provider
with the API key in Windows Credential Manager (`MCPForUnity.AssetGen:tripo`).

The MCP server runs on `http://127.0.0.1:8080/mcp` and is driven over plain JSON-RPC:
`initialize` -> `notifications/initialized` -> `tools/call`, carrying the `mcp-session-id` header.

```
generate_model  action=generate  provider=tripo  mode=text  texture=true
                name=Tool_<Name>  output_folder=Assets/CarpetCleaning/Models/Tools
                target_size=<long axis, m>  prompt=<see below>
-> job_id, then poll action=status until data.state == "done"   (the key is `state`, not `status`)
```

### Prompt shape

Tripo ignores negative phrasing: "no handle, no stick" produced a full scraper **with** a handle
(`Screenshots/prev_Tool_Scraper.png`). Describing the object positively as a detached part works:

> a single detached replacement **&lt;head&gt;**, **&lt;shape&gt;**, with a short thick mounting collar on top,
> isolated spare part lying flat alone, simple low-poly geometry, smooth clean surfaces,
> matte muted **&lt;colour&gt;**

The mounting collar gives the mesh a natural termination where a handle would otherwise grow.

| id | Tool | Head | target_size, m |
|---|---|---|---|
| 0 | Scraper | flat trapezoid plate, straight sharp bottom edge | 0.120 |
| 1 | Dry vacuum | round disc suction head, slot underneath | 0.100 |
| 2 | Hose | compact round nozzle, concentric rings | 0.080 |
| 3 | Foam sprayer | wide round fan nozzle, ring of holes | 0.140 |
| 4 | Brush | round disc, short dense bristles | 0.090 |
| 5 | Squeegee | long straight rubber blade in a channel | 0.220 |
| 6 | Wet vacuum | narrow elongated head, clear window strip | 0.160 |
| 7 | Dryer | wide round outlet disc with a grille | 0.220 |
| 8 | Detergent | round applicator disc, central spout | 0.170 |
| 9 | Mop | long rectangular flat pad | 0.320 |

`target_size` is the extent **across** the stroke: `lengthMm` for a blade, `radiusMm * 2` for a disc,
straight out of `ToolTable.All`.

**Brush v2 (2026-09-24), `Tool_Brush_v2.glb`.** The user generated it in the Tripo web app. It came back
part-segmented (section 7.1). The source was this prompt, or its image-first variant (a product render of the same
brush, 3/4 view from about 30 deg above, plain light-grey background, then Image to 3D):

> A single round rotary scrubbing brush for a floor machine, detached and lying on its bristles: a wide low
> disc-shaped brush block of matte charcoal-grey plastic with a dense solid skirt of stiff bright blue nylon bristles
> on the underside, bristles pointing straight down and cut perfectly flat, the skirt as tall as the block. Flat top
> face with a round brushed-steel clutch plate in the centre carrying three raised radial lugs, and a ring of six small
> hex bolt heads. Overall height one third of the diameter. Chunky, clean, simple low-poly geometry, the bristles
> modelled as one dense block with fine vertical fibre grooves.

Why it is worded this way:
- **Bristles as one dense block.** Separate thin tufts come back as loose spikes, as they did on the first take.
- **An asymmetric top.** The clutch-plate lugs are the kind of detail that makes a spinning head readable at ~60 px.
- **Neutral grey body, coloured bristles.** The split (`IsBristle`) relies on it.

### What the tool does and does not control

- Output is always `.glb`: `AssetGenJobManager.WriteFile` prefers the extension of Tripo's own result
  URL over the requested `format`, so **glTFast (`com.unity.cloud.gltfast`) is a hard dependency**.
- `target_size` normalisation does **not** apply. It goes through `ModelImporter.globalScale`, and a
  `.glb` is handled by glTFast's own importer, so the raw meshes arrive at roughly unit size. Scale is
  fixed in the normalisation step below instead.
- `face_limit` / `quad` are not forwarded to Tripo by `TripoAdapter`, so polycount cannot be requested.
  The prompt wording is the only lever; measured result was ~4.7k triangles per head, which is nothing
  next to the 384x480 compute simulation.

## 3. Normalisation

`CarpetCleaning/Normalize Tool Models` (`Editor/ToolModelPostprocess.cs`) turns each raw `.glb` into
`Prefabs/Tools/Tool_<ToolId>.prefab`. Tripo returns arbitrary pivot, orientation and scale, so every
head is re-based inside a wrapper GameObject:

1. the longest horizontal axis is rotated onto **+Z** (across the stroke, matching the cursor),
2. the model is uniformly scaled so that its +Z extent equals the tool footprint,
3. the pivot is centred on XZ with the bottom of the mesh at **y = 0**.

Bounds are measured from mesh vertices, not renderer AABBs, so the rotation step sees the real extents.
The step is idempotent: re-running rebuilds the prefabs from the `.glb` sources.

`ToolModelPostprocess.Source` maps a tool to its source basename, which is how a regenerated take is
adopted (`Scraper` points at `Tool_Scraper_v2`).

Two per-tool extras run inside the same pass:

- `UpsideDown` turns a head over (180 deg about X) **before** step 1, so the invariants above still hold. Tripo
  built the brush with its bristles in the air.
- `SpinSplit` cuts the head into a static body and a spinning part under a `Spin` pivot (section 7).

`ToolModelPostprocess.Normalize(ToolId)` rebuilds one head and leaves the other prefabs alone. The menu item still
rebuilds them all.

## 4. Runtime

- `Settings/ToolModelSet.asset` (`Scripts/ToolModelSet.cs`) holds one entry per tool that has a prefab,
  plus per-tool `posOffsetM` / `eulerOffset` / `extraScale` for art tuning, and `hoverMarginM`.
- `Scripts/ToolModelView.cs` hangs off **`CarpetRoot`**, never `CarpetStage`: the hand-over animates the
  stage (`HANDOVER.md`) and the tool must stay put with the collider and the stamp path, exactly like the
  outline cursor.
- It is driven from `CarpetRenderer.SetCursor`, so it inherits the uv -> world mapping
  (`((u-0.5)*1.2, h, (v-0.5)*1.5)`, yaw `-atan2(dir.y, dir.x)`) and the hover height `ToolController`
  already picks per domain.
- **`hoverMarginM = -0.012`.** `ToolController` builds both hover heights as "surface + 0.012 m"
  (0.03 over the carpet backing, 0.009 over the tile at -3 mm), so -0.012 drops the head onto the
  surface in either domain: 2 mm into the 20 mm pile on the carpet, flush with the tile on the floor.
- Renderers are stripped to match the scene (one shadowless sun, flat ambient): shadows off, probes off,
  colliders disabled so the head never catches the tool raycast.

### Fallback

A tool without a prefab keeps the outline cursor: `SetCursor` enables the quad only when
`ToolModelView.Has(tool)` is false. No tool is ever left without a cursor, which is what makes a
partially generated set usable.

### Lazy build

Heads are instantiated on first use, not in `Awake`. `AddComponent` runs `Awake` in **edit mode** too,
and instantiating there left eight real GameObjects in the scene on every `Build Scene` run - and those
leftovers then shadowed the play-mode instances, so `Has()` returned false while the children existed.
`Awake` now only clears; `EnsureBuilt()` fills the dictionary once `Application.isPlaying`.

## 5. Gotchas

- **Do not write C# while a generation is in flight.** A domain reload kills the in-flight AssetGen job
  with "Interrupted by an editor reload"; two heads were lost that way. The downloaded file may still
  land on disk even when the job reports failure, so check `Models/Tools/` before regenerating.
- Tripo credits are consumed per generation. A 403 with `code 2010` ("not enough credit") is reported by
  `status` as `state: failed` *after* `generate` has already returned a `job_id` and `pending`.
- `PreviewRenderUtility` is the way to render a model preview from `eval`: a plain `Camera.Render()` into
  a RenderTexture returns an empty frame under URP.
- `eval` wraps the code in a method body, so `using` directives are a compile error - fully qualify, and
  write `UnityEngine.Object.DestroyImmediate` (bare `Object` is ambiguous).

## 6. Heading smoothing

The head is placed by `CarpetRenderer.SetCursor` -> `ToolModelView.SetTool` from a single yaw, and that
yaw used to be the raw per-frame pointer segment. It trembled, for two independent reasons.

**Why it trembled.** The pointer arrives quantised to whole screen pixels, and at this camera one pixel is
**1.06 mm of carpet** (measured with `TryGetCarpetUV` on the live camera). A perfectly straight drag is
therefore a staircase: the per-frame segment alternates between (1,0) and (0,1) pixels, and its direction
swings **51.5 deg** at 1.5 px/frame, 17 deg at 3 px/frame, 11 deg at 8 px/frame. On top of that nothing
eased the yaw, so every one of those swings was applied whole in one frame.

**The filter** lives in `ToolController.UpdateHeading`, called exactly once per frame - from `StrokeSegment`
while a button is down, from `Update` while the pointer only hovers (so pressing never snaps the head round
from a stale heading). Two stages:

1. *Measurement.* Travel is accumulated into `_headAccum` until it reaches `headingMinMoveMm` (2.5 mm), so
   sub-pixel wobble never counts as a direction, and a wiggle that cancels out never trips it at all. The
   target then blends towards the measured angle by `1 - exp(-len / headingTurnMm)` - **distance, not time**,
   so a slow drag turns the head exactly as far as a fast one over the same path.
2. *Output.* The shown angle eases towards the target at `headingEaseHz` under a hard `headingMaxDegPerSec`
   cap, so no single frame can snap the head.

A pointer move longer than `headingTeleportMm` (300 mm) in one frame is a window re-entry or a UI release,
not a stroke, and is dropped instead of spinning the head.

### 6.1 The head is an axis, not an arrow

`_headAngle` is a tool **axis**, kept mod 180 deg, plus `_travelSign` (+-1) saying which way along it the
tool is going. `StrokeDir = ViewDir * _travelSign` is what goes into the stamp, and it serves both readers
of `ToolStamp.dir` at once:

| reader | what it needs | what it gets |
|---|---|---|
| `FootprintWeight` (SimCommon.hlsl, `FloorCommon.hlsl`, `Debris.compute`) | an axis - it reads `dir` through `abs(dot(...))` | the axis the head is drawn at, always |
| fibre bend, debris throw, `vStroke` (`CarpetSim.compute` 139/170/180/696, `FloorSim.compute`) | a signed stroke direction | the axis times the travel sign |

The fold is applied **to the measurement**: when a new direction is more than 90 deg off the axis it is
turned by 180 deg and the sign flips instead. A reversal therefore moves the axis by **nothing at all**
(measured 0.0 deg) and only flips the sign; a 135 deg corner turns the axis 45 deg the short way.

Two traps, both hit while building this:

- Folding the *eased* angle never fires. The eased angle follows a reversal round degree by degree and never
  sees a jump bigger than 90, so the condition is never true. It has to be the measurement.
- Easing a **signed** heading round a reversal looks fine on the model but is wrong in the simulation: for
  the ~0.3 s of the sweep the angle passes through the perpendicular, so the 220 mm squeegee lies *along*
  the stroke and the footprint collapses to twice its 15 mm radius. On fast repeated reversals it never
  finishes the sweep and the tool paints a thin stripe that the head on screen never covered. That was the
  first version of this filter, and it is the reason the axis+sign form exists.

**Stamp placement is not filtered.** `StrokeSegment` keeps `stepDir = seg / len` for where the stamps land -
they must follow the path the pointer really took - and uses the filtered heading only for the stamp's own
`dir`. Mixing the two would drag the stamps off the pointer on every turn.

### 6.2 Measured

Filter behaviour, by reflection on the live component:

| | before | after |
|---|---|---|
| straight drag 1.5 px/frame | 51.5 deg of swing | 2.3 deg |
| straight drag 3 px/frame | 17.0 deg | 1.8 deg |
| straight drag 8 px/frame | 11.0 deg | 0.9 deg |
| 90 deg corner at 250 mm/s | instant | 90% in 16 frames (0.27 s) |
| 135 deg corner | 135 deg of spin | axis turns 45 deg, same footprint |
| stroke reversed | 180 deg spin | axis moves 0.0 deg, sign flips, stroke dir exact |
| biggest single-frame step | unbounded | 6.0 deg (the 360 deg/s cap) |

Footprint on the carpet - scraper (blade 120 mm across the stroke), 96 stamping frames at 1350 mm/s over the
same span, measured from the damage channel of the `crust` debug view, which is written straight into the
footprint and does not flow:

| | touched width across the stroke |
|---|---|
| 12 passes, always one way | 125 mm |
| 12 passes, reversing every 8 frames | 125 mm |
| control: stamp axis turned 90 deg off the stroke | **25 mm** |

The control is the old bug reproduced deliberately, and it is what proves the 125 mm means something.

`headingEaseHz = 20` came out of a 3x3 sweep of `headingTurnMm` x `headingEaseHz`: 12/16/22 mm against
14/20/28 Hz moved the residual tremble between 1.3 and 3.4 deg and the 90% corner between 15 and 22 frames,
and 22/20 sits where the tremble is still invisible and the corner is as quick as it gets.

**Cursor height** is eased too (`heightEaseHz`). Carpet hover (0.03 m) and tile hover (0.009 m) are 21 mm
apart, so crossing the rim popped the head up and down and shivered wherever the collider edge is ambiguous.
It snaps, not eases, when the cursor reappears somewhere new.

**Scripted strokes** call `SnapHeadingOnNextMove` (from `CarpetDebugApi.Reset`, `StrokeNow` and the queued
stroke runner) so each one starts pointing along itself instead of easing round from the previous stroke.

## 7. Spinning brush (tool 5)

The brush head is the brush of a rotary floor machine. It uses the v2 model, `Tool_Brush_v2.glb`, which the user
generated in the Tripo web app from the section-2 style prompt: a grey block with a clutch plate (three lugs, two
latches) and eight bolts on top, over a flared blue bristle skirt.

**The top stands still. The bristles turn at 350 rpm, only while the tool works:** they spin up when the button goes
down and coast down after it is released.

User decisions, all on 2026-09-24:
- Only the bristles turn, and only while the tool works.
- The first take was cut down to its bristle ring (`SpinOnly`). It looked poor, so it was replaced by v2, whose top
  stays still.
- The speed was doubled from a low-speed machine's 175 rpm to 350, so the spin reads faster.

What the spinning pad does to the carpet, and its motor sound, are in `ROTARY_BRUSH.md`.

### 7.1 The split (`ToolModelPostprocess.SplitSpinPart`)

**v2: by part (`SplitByParts`).** Tripo returned v2 part-segmented: 71 meshes, each with its own material and texture.

| parts | what | triangles | where |
|---|---|---|---|
| 42 | blue bristle chunks | 2313, spin | height 0-0.36, radius 0.37-0.50 of the unit model |
| 29 | grey body, clutch plate, latches, eight bolts | 2246, still | the body reaches the full height; the rest sits on top |

- **Classifier.** A part spins when its area-weighted albedo passes `IsBristle`: chroma `max - min > 0.12` and
  `max > 0.25`. The blue parts measure chroma 0.23-0.37, the grey ones at most 0.05.
- **No cutting.** The spinning parts move, poses kept, into the container `Tool_Brush_Spin` under `Spin`. Nothing is
  cut or saved: every mesh stays in the .glb.
- **Prefab.** `Tool_Brush` -> `Tool_Brush_v2` (the still parts) + `Spin` -> `Tool_Brush_Spin` (the 42 parts).
- **Pivot.** On the bristles' XZ centre, (0.00, 0.00) mm.
- **Checked in edit mode.** Turning `Spin` 30 deg in a 640 px preview changed 0 pixels of the top and 16k of the
  skirt.
- **Orientation.** `UpsideDown` is keyed by source file, not by tool: v2 stands on its bristles, the first take did not.
- **Stale meshes.** Cut meshes a single-mesh take left in `Prefabs/Tools/Meshes/` are deleted.

**First take: a single welded mesh (`Tool_Brush.glb`, still the code path for any single-mesh head).** Once the
vertices are welded by position, that brush is 145 connected pieces:

| pieces | what | triangles |
|---|---|---|
| 113 beige | bristle tufts | 3245 spinning + 136 left static |
| 32 grey | plate, rivets, hub | 986 |
| 0 | mixed | - |

- **Classifier.** A piece spins when more than half of its triangles sample bristle-coloured in the albedo
  (`IsBristle`; the old beige-only rule split it identically).
- **Albedo readback.** glTFast imports the texture non-readable, so the albedo is read back through a blit into a
  512 x 512 RT.
- **SpinOnly.** A `SpinOnly` head keeps the tufts alone and whole, and lowers them onto y = 0. The list is now empty.

The **plate ceiling** stops tufts sliding across a still plate. The tuft tops are flush with the plate: the plate is
a thin dish at 26-27 mm that dips towards its middle, and the tops sit at 26-27 mm too. Turned under a still plate, they
slid beige specks all round it. The fix works per 0.5 mm of radius round the spin axis:

1. The **ceiling** at that radius is the lowest point of the static surfaces, at any angle, since a tuft passes under
   all of them. It is sampled on a 0.5 mm XZ grid inside every static triangle, plus at the vertices. Only surfaces
   above 80 % of the tuft top count, which leaves out the hub hanging down in the middle.
   Measured: r 30 mm 25.1, r 34 mm 26.6, r 38 mm 26.6, r 42 mm 25.4 mm.
2. A spinning triangle whose centroid reaches `ceiling - 0.5 mm` stays with the body. That keeps 136 triangles, the
   specks in the plate's holes.
3. Every spinning vertex above `ceiling - 0.5 mm` is pulled down onto it (559 vertices).

A flat cut at the plate's underside was not enough, because the dish is lower at the inner and outer edges of the tuft
ring. A dark cover ring under the holes was also tried and then removed, because the ceiling alone closes them.

The pivot sits on the centre of the tufts' XZ bounds, (-0.18, 0.35) mm from the head's centre, so they turn in place.
With a body, the two meshes live in `Prefabs/Tools/Meshes/Tool_Brush_Body.asset` and `Tool_Brush_Spin.asset`, and the
prefab is `Tool_Brush` -> imported node (Body mesh) + `Spin` -> `Tool_Brush_Spin`.

**Trap:** `EditorUtility.CopySerialized` onto an existing Mesh asset updates the asset but not its GPU copy. The Editor
kept drawing the very first version of the mesh, and three rebuilds rendered identical to the pixel until
`UploadMeshData` was forced. `SaveMesh` therefore overwrites through the Mesh API (`Clear` + `Set*`), which also keeps
the GUID.

### 7.2 Runtime (`ToolModelView`)

- **Working.** The tool counts as working when `ToolController.IsStroking` is true or `StampCount` changed since the
  last frame. The stamp test also catches scripted strokes and `LiveStroke`. Working is held for `spinReleaseGraceS`
  (0.12 s, the same grace as `ToolAudio`).
- **Speed.** `spin01` eases towards 1 or 0 at `spinUpHz` 4 / `spinDownHz` 1.2, with a bounded exponential. The angle
  advances by `spinRpm * 6 * spin01 * dt` degrees.
  - A tool change resets the spin.
  - The state advances before `SetTool`'s early returns, so a released pad keeps coasting while the head is hidden.
- **World-locked.** `Spin.localRotation = Euler(0, spinDeg + headingDeg)`. The head yaws with the stroke, and adding
  that yaw back keeps the bristle speed independent of turns.
- **Anti-strobe smear.** The first take had ~40 tufts, about 9 deg apart. One frame at 175 rpm / 60 fps turned them
  17.5 deg, about two tuft spacings. Sampled once per frame, that reads as a still or backwards brush (the wagon-wheel
  effect).
  - Up to `smearMaxCopies` (4) runtime copies of the part are drawn at `-i * step / (n + 1)`, with
    `n = ceil(step / smearMaxStepDeg) - 1` and `smearMaxStepDeg` = 4.5. They sample the arc swept that frame and turn
    the strands into a dense, streaked skirt.
  - The copies appear one by one as the pad spins up.
  - At 350 rpm / 60 fps a frame turns 35 deg. The 4 copies then sit 7 deg apart, wider than the 4.5 deg target; more
    copies would cost more draw calls.
  - v2 costs: the copy is the whole 42-part container, so at most 5 x 42 bristle renderers plus 29 still ones.
  - Each earlier copy is nested `SmearShrink` (0.4 %) smaller radially. v2's skirt is continuous, and a copy turned a
    few degrees lies on it and z-fights. Nested, it only shows through the gaps between strands.
  - Measured in Play at 175 rpm (0.3 m close-up): consecutive frames change the skirt band only, up to 8k pixels per
    50-row band. The top changes 9-21 pixels per band, which is the carpet pile around it.
- **Pile sink.** `pileSinkM` (brush 0.008) presses the head into the pile: the bristle tips sit 10 mm above the
  backing, half way into the 20 mm pile, where `hoverMarginM` alone leaves every head 2 mm in.
  - It applies over the carpet only. The share is `InverseLerp(tile hover 0.009, carpet hover 0.03, heightM)`, so it
    eases out with the cursor height across the rim, and a head over the hard tile sits flush.
  - Under a pressed, wet pile the tips are 1-5 mm in: the pile top drops to 11-15 mm, see `PileTopMm`.
- **Knobs.** `spinRpm` and `pileSinkM` are per entry in `ToolModelSet.asset` (brush 350 / 0.008).
  - `CarpetSceneBuilder.BuildToolModelSet` seeds `DefaultSpinRpm` / `DefaultPileSinkM` for a prefab with a `Spin` node
    whose entry is still 0. It preserves the values on every later run.
  - The easing and smear knobs are set-wide.
  - All of them can be edited live in Play.
  - `SimSettings.BRUSH_RPM` is the pad speed the simulation uses: keep it equal to `spinRpm`.
- **Debug.**
  - `CarpetDebugApi.PinToolHead(tool, u, v, working)` / `UnpinToolHead()` show a head without the pointer. An unfocused
    Editor never has the pointer over the Game view.
  - `ToolHeadJson()` returns `spin01`, `spinDeg`, `stepDeg` and `smearCopies`.
  - `StepAll` calls `ToolModelView.StepFrame(dt)`, so scripted frames spin the head.

### 7.3 Measured (2026-09-24, scripted 1/60 s frames, first take with its plate, 175 rpm)

| | value |
|---|---|
| spin-up | 0.33 @ 0.1 s, 0.91 @ 0.6 s, 0.98 @ 1.0 s; full step 17.5 deg/frame = 175 rpm; 1 -> 2 -> 3 smear copies |
| coast-down after release | 0.85 @ 0.25 s, 0.35 @ 1.0 s, 0.10 @ 2.0 s, 0.03 @ 3.0 s |
| driven by stamps only (`StrokeNow` 36 frames, pin not working) | 0 -> 0.91; +7 frames inside the grace 0.94; +1 s 0.28 |
| tool switch brush -> scraper -> brush | 0.94 -> 0 -> 0 |
| plate far half, pixels changed > 40/255, idle -> full spin / frame -> frame | game camera 0 / 0 of 504; 0.3 m close-up 0.14 % / 0 % of 27.7k |
| bristle skirt, same test | game camera 27.5 % / 23.9 %; close-up 23.5 % / 15.9 % |

Screenshots: `Screenshots/brush_game_{idle,spin_a,spin_b}.png` (game camera) and
`brush_close_{idle,spin_a,spin_b}.png` (camera moved to 0.3 m in Play only).

The spin numbers do not depend on the mesh. The plate rows are history. The ring-only head and the pile sink were built
without Play mode, at the user's request; see `SESSION_LOG.md`.

## 8. AREA upgrade scales the head (revised 2026-11-16, `ECONOMY.md` 2)

- **The scale.** `ToolModelView.SetTool` sets `localScale = extraScale * ToolTuning.HeadScale(tool)`:
  - a blade: area uniformly on X/Y/Z, matching its effective blade length;
  - a disc: √area uniformly, so the brush's `Spin` pivot and smear copies never shear.
- **Proportions.** Every head keeps its authored proportions, including round details and mounting collars. At AREA 6
  the mop, squeegee, wet-vac and scraper are 3× larger on all axes. Previously only Z grew, visibly stretching them.
- **Contact and handles.** The normalized prefab's bottom remains at local Y=0. Scaling therefore preserves its surface
  placement and pile sink. `FirstPersonToolView` follows the scaled `HandleSocket` via `TransformPoint`; the camera-side
  grip and shaft diameter stay fixed. The idle contact-head copy uses the same `HeadScale` rule.
- **Gameplay.** Effective blade length, disc radius, stamp spacing, power and upgrade prices keep their existing rules.
  Blade depth/height grow visually with the width; the blade simulation remains a swept working edge.

## 9. First-person presentation (2026-11-15)

`FirstPersonToolView` (207, on the camera) joins the actual contact head to a camera-side grip with a telescopic
metal shaft. The contact head is still placed by `ToolModelView`, so scraper, rotary pad, squeegee, wet-vac and
mop footprints, pile sink, upgrade scale and brush spin keep their existing simulation alignment. When the
player looks away from a reachable floor, a held copy replaces the contact head. These mechanical connectors
are runtime geometry; the working heads remain the existing Tripo assets.

The hose keeps its existing generated nozzle in the grip. The detergent tool reuses the accepted complete
foam-sprayer prefab at scale **0.85**, with its own `FloorCleaner` entry in `FirstPersonToolSet`; its old disc head
remains available only to the established scripted surface-head view. Both sprayers point towards the same
surface aim as `ToolController`. Complete generated dry-vacuum, foam-cannon and blower bodies live in
`FirstPersonToolSet`. The dry vacuum attaches its generated motor/grip body to the regenerated wet-extraction head
used as a suction foot. Its 160mm source width scales to the effective dry-vac **diameter** (100mm at factory
tuning), and its yaw follows the stroke axis. The body and grip never stretch when an AREA upgrade changes the
footprint. The dry-vacuum tube has an 18mm diameter and terminates at the foot's actual collar. A flexible hose is
visible for hose, detergent and wet extraction. `FirstPersonSpray.shader` draws the water/foam/detergent jet from
nozzle to contact while the simulation lays stamps; it does not change simulation state.

`ToolController.SurfaceAimWorld` is the existing hover height (30 mm above backing, 9 mm above tile), so the new
view subtracts 12 mm for actual spray/suction contact. `ToolModelView.ShownTransform` gives the existing heads'
exact placed transform. Generated first-person bodies are listed in `Settings/FirstPersonToolSet.asset`; all
runtime children have no active colliders and use Ignore Raycast. Scripted pointer captures and pinned-head
tests retain the established surface-head presentation.

### Importing an accepted Tripo result

1. Keep the accepted image and high-quality original outside `Assets` under `Tools/first_person/`.
2. Inspect the high-quality model, simplify it in Tripo, and inspect the simplified version before import.
3. Put the simplified GLB in `Assets/CarpetCleaning/Models/FirstPerson/Tool_<ToolId>.glb`.
4. Call `FirstPersonModelImporter.Import(ToolId, sourcePath, sourceEuler, longestSizeM)`. The explicit source
   rotation must put the nozzle along +Z and the grip downward. The importer centres the vertex bounds and
   scales the longest extent to `longestSizeM`; it saves `Prefabs/FirstPerson/FP_<ToolId>.prefab` and registers it.
5. Tune the entry's camera offset, rotation and nozzle position after viewing the actual prefab in Play. The
   automatically measured frontmost point is only a starting point for nozzle alignment.
6. `FirstPersonModelImporter.Report()` prints missing models and triangle counts. `EnsureSet()` creates the
   set and the referenced spray material, which keeps its shader available in builds.

The importer intentionally does not generate, approve, or silently reduce a source mesh: those are separate
art review steps. The raw high-quality originals are not shipped in the player.

### Accepted first-person exports, 2026-11-15

All three came from the accepted image references through Tripo H3.1 high-quality generation, then a separate
simplification pass. Their raw exports face +X with +Y up; importer source rotation is `(0,-90,0)`.

| Body | Triangles | Normalized dimensions X/Y/Z, m | Nozzle position in prefab, m |
|---|---:|---|---|
| Dry vacuum | 39,999 | .145 / .356 / .460 | 0 / .0366 / .2320 |
| Foam sprayer | 18,527 | .166 / .341 / .450 | 0 / .1074 / .2270 |
| Dryer | 19,310 | .206 / .368 / .440 | 0 / .0723 / .2220 |

`Tools/first_person/import_generated.cs` repeats these imports and measures the nozzle's actual front rim,
rather than placing the outlet at the bounding-box centre. Its initial detergent fallback shared the foam body
at scale 0.85; the second pass below replaces that entry with its own generated gun and preserves it on replay.
All entries use camera offset `(.23,-.26,.48)`.
`FirstPersonToolView` iteratively aligns the actual outlet axis to the physical contact point; for the vacuum,
that point is the suction foot's upper collar. Contact-head socket points are transformed through the head,
so AREA scaling and the idle pose cannot detach the shaft.

`FirstPersonToolView.ContactSocket` reads each new prefab's `HandleSocket` marker. Its legacy fallback uses
measured normalized-prefab coordinates, in metres. The mounting
brackets of the squeegee, mop and extraction head are offset from their footprint centres; attaching at the
centre left those brackets visibly unused. `Tools/first_person/inspect_sockets.cs` records the vertex-bound
inspection used to fit these points:

| Legacy head fallback (superseded by the new markers below) | Local socket X / Y / Z, m |
|---|---|
| Squeegee | .012 / .116 / -.008 |
| Mop | .130 / .058 / -.001 |
| Wet-extraction head, including the dry-vacuum foot | 0 / .074 / -.057 |
| Rotary brush | 0 / .035 / 0 |
| Other existing contact heads | 0 / .020 / 0 |

The complete generated bodies aim from their measured outlet markers. For dry extraction, the target includes
the suction foot's yaw, scale and collar offset; for sprays, it is the physical surface rather than the cursor's
hover plane. Final views `Assets/Screenshots/first_person_tool_{1,5,6,8,9}.png` were reviewed after these fixes:
the tube and shafts connect to their mountings, and the detergent tool reads as a spray gun instead of a disc.

The first dry-vacuum Smart Mesh quad reconstruction was rejected in Unity: torn and overlapping casing faces
were visible under both its materials and a plain clay material. Flipping the 392 triangles whose winding opposed
their vertex normals did not fix the shell. A new **40k triangle simplification with Smart Mesh off**, from the
high-quality source, resolved it and has zero degenerate triangles. The rejected GLB stays outside Assets under
`Tools/first_person/rejected/`. Unity side/front/three-quarter inspection images and geometry reports are in
`reports/first_person/models/`. The accepted foam and dryer retain 12 and 7 zero-area export fragments; they have
finite geometry, valid indices and visually intact shells. Their source meshes were left unchanged.

## 10. Regenerating the remaining seven tools (2026-11-15)

The second art pass covers Scraper, Hose, Brush, Squeegee, WetVacuum, FloorCleaner and Mop. It preserves the
three accepted complete bodies above. References, reviewed orientation/marker settings and replay scripts live
in `Tools/first_person/regeneration_20261115/`. The larger 3.6 x 4.5m carpet does **not** change tool factory
dimensions: scraper 120mm, brush 90mm diameter, squeegee 220mm, extraction head 160mm and mop 320mm.

All seven passed Tripo HD review, Triangle remeshing with **Smart Mesh off / 40,000 target**, and solid Unity
front/back/side review. The seven exports have finite geometry, valid indices and **zero degenerate faces**.
They retain their generated 4K albedo textures and source materials. Generation and remeshing used 420 credits;
the prior three-body pass used 290. Exact Tripo IDs, image references, HD counts, hashes and geometry reports
are in `regeneration_20261115/generation_manifest.json` (paths relative to `Tools/first_person/`).

| Tool | HD triangles | Simplified source triangles | Normalized role |
|---|---:|---:|---|
| Scraper | 1,915,466 | 39,999 | 120mm contact blade |
| Hose | 1,982,210 | 40,000 | Complete 300mm gun |
| Brush | 1,918,645 | 40,000 | 90mm rotary head, derived split 40,820 triangles |
| Squeegee | 1,953,457 | 39,998 | 220mm contact blade |
| WetVacuum | 1,980,476 | 39,990 | 160mm extraction head; also the dry-vac suction foot |
| FloorCleaner | 1,917,846 | 40,000 | Independent 400mm detergent gun |
| Mop | 1,945,598 | 40,000 | 320mm cloth pad |

The five heads use source rotation `(0,90,0)`; both guns use `(0,-90,0)`. The three previously approved body
GLBs and prefabs retain their exact SHA-256 hashes (`reports/first_person/regeneration_20261115/approved_three_preservation.json`).

### Contact heads

`FirstPersonModelImporter.ImportContact(id, sourcePath, sourceEuler, socketLocalM = null,
brushBristleTopM = -1, contactBandFraction = .15)` imports the five contact heads to the canonical
`Prefabs/Tools/Tool_<ToolId>.prefab` paths. Explicit source rotation makes +Y up, +X the stroke direction and +Z
the working width. The lower 15% height band supplies the **working** width and XZ centre; a protruding mounting
bracket therefore cannot shrink or offset the footprint. The bottom is y=0, and factory width is applied once
at import. Runtime AREA scaling remains in `ToolModelView`.

Each imported root has a `HandleSocket` marker. The importer initially measures the top 8% of vertices when
no explicit point is supplied; this is a fitting suggestion, not a substitute for visual review. The runtime
shaft, idle copy and dry-vacuum foot all use that marker through the head's transform, including AREA scale.
Legacy hardcoded socket positions apply only to prefabs without a marker.

The new measured openings use these marker positions and physical shaft sizes. Short cuffs have radius
`shaft radius + 2mm`, length 18mm for the brush and 26mm for the other heads. This keeps the new small brush
and scraper collars visible. The near-grip sleeve and grip retain their hand-sized dimensions.

| Head | HandleSocket X / Y / Z, m | Shaft diameter |
|---|---|---:|
| Scraper | -.025101 / .074177 / .015297 | 18mm |
| Brush | 0 / .034200 / 0 (central silver hub) | 12mm |
| Squeegee | -.013764 / .114549 / .016579 | 22mm |
| WetVacuum | -.058686 / .097243 / .016493 | 28mm |
| Mop | -.044036 / .118698 / .018735 | 16mm |

The dry-vac foot uses the WetVacuum marker through its own effective footprint scale, with an 18mm tube.
Connector dimensions do not change the simulation footprint or upgrade calculations.

The brush handle attaches to the **centre of the stationary housing, directly over the Spin axis**, as requested
by the user. The silver hub rim spans y=33.81–34.58mm; the marker is `(0,.0342,0)`, and its 6.02mm bore fits the
existing 6mm-radius shaft. The old off-centre black mounting tube is no longer the attachment point.
`Tools/first_person/fit_brush_center.cs` updates only this prefab marker; the replay recipes keep the same centre
on future normalization. The housing/skirt meshes, 90mm factory diameter, 350rpm and 8mm pile sink are unchanged.

Accepted recipes are recorded in `Tools/first_person/contact_imports.json`. Both variants of the existing
Normalize Tool Models command consult those recipes before old source mappings, so normalizing again cannot
silently restore an older head. Build Scene keeps the canonical prefab references and brush tuning.

### Single-mesh rotary brush

New brush art uses a neutral grey/metal housing above a distinct saturated blue skirt, with a horizontal
joint and no blue trim on the upper housing. `FirstPersonBrushSplitter` does not rely on disconnected islands:
it samples the imported albedo to propose the top of the lower blue skirt, then clips triangles at the reviewed
horizontal seam. `brushBristleTopM` supplies an explicit seam in **normalized metres** when needed. The complete
lower skirt goes under `Spin/BrushSkirt`; the upper housing stays static. UVs, normals, tangents, vertex colours
and material submeshes are interpolated/preserved. A face exactly on the joint belongs to the housing only.

The split writes derived meshes under `Prefabs/FirstPerson/Meshes` and leaves the source GLB and textures intact.
Existing mesh GUIDs survive repeat imports; mesh data is replaced through Mesh API, never `CopySerialized`.
The spin pivot uses the skirt's XZ bounds. The import registers **350rpm** and **8mm pile sink**, matching the
existing simulation. Preview the normalized brush both at zero and 30 degrees before accepting the seam;
the housing must remain still and no bristles may sweep through it. The generated groove details, existing
bounded spin easing and smear copies provide motion without changing the physics.

The accepted split seam is **15.20mm**: 14,323 housing triangles remain static and 26,497 skirt triangles rotate
(40,820 total after clipping crossing faces). Solid previews at zero and 30 degrees show a stationary housing
and intact skirt with no sweeping through the housing. The source remains the untouched 40,000-triangle GLB.

### Complete hose and detergent bodies

The new Hose and FloorCleaner use `Import()` with their inspected source rotation, followed by
`FitNozzle(id, frontSliceM, scale, cameraPosition)`. The latter measures the actual front rim instead of the
bounding-box centre; use scale 1 for a separately sized new body. `SetHoseSocket(id, localPositionM)` adds an
inspected inlet marker so the flexible hose starts on the body instead of a generic grip position. The new
FloorCleaner receives its own `FP_FloorCleaner.prefab`; it replaces the initial foam-body reuse. The old
three-body replay script preserves an independent FloorCleaner entry instead of reintroducing that reuse.

Hose nozzle/inlet are `(.0001,.0680,.1520)` / `(-.007962,-.116374,-.120958)` metres; FloorCleaner nozzle/inlet
are `(0,.0767,.2020)` / `(0,-.126448,-.170685)`. Each inlet is fitted to the brass connector below the grip,
and each independent gun uses scale 1. The old floor-tool disc is no longer used by the live first-person view.

### Review and replay files

- `import_plan.json`: seven model paths and art settings. `reviewed: false` means the source orientation/export
  has not yet passed the agent's art check; this is internal bookkeeping, not an extra user approval step.
- `prepare_import.py [--tool ToolId]`: writes `import_reviewed.cs` from reviewed entries only. It validates enum
  mapping, finite numbers and source-directory containment. It does not connect to Unity.
- `check_exports.py`: inspects imported sources (`--downloads` checks the pre-import copies outside Assets) and writes SHA-256, triangle/vertex counts, finite/index checks,
  degenerate-face counts, normal-consistency data and texture sizes under `reports/first_person/regeneration_20261115/`.
- `preview_model.cs`: reads `preview_path.txt`, imports only that asset and produces front/side/three-quarter
  Unity previews; a normalized brush additionally gets a 30-degree spin view. Preview objects are cleaned up.
- `inspect_imported.cs`: reads saved prefabs and reports working widths, ground alignment, socket proximity,
  finite meshes/materials, brush split/tuning and independent held-body registrations. It does not alter the
  scene or Play state. Numeric checks complement the rendered review, not replace it.
- `offline_axes.py`: textured vertex projections in the Unity coordinate convention, for axis planning while
  another agent owns the Editor; final visual acceptance uses the solid Unity renders.
- `capture_gameplay.cs` + `capture_tool.txt`: fresh-Play captures of all ten tools at the larger carpet. Uses live
  effective upgrades, suspends achievements/economy subscriptions, restores the exact save bytes and in-memory
  save, and writes images/pose/connector measurements outside Assets under `reports/first_person/regeneration_20261115/gameplay/`.

Run imports and model previews with Play stopped and one owner of Unity's import/compile loop. After the last
asset write, restart Play before gameplay/HUD captures; an asset refresh can leave old runtime UI references.

### Final FPS fitting verification (2026-11-15)

All ten working tools passed fresh-Play visual review at the enlarged carpet. Maximum shaft/socket error was 0.0000003 m; both new hose inlets matched exactly; maximum jet-axis deviation was 0.0442 degrees. Nearest held vertex was 0.2302 m from the camera (near clip 0.025 m). The brush working report retained 350 rpm. All three previously approved body GLBs and prefabs retained their SHA-256, and the fresh session save was unchanged. Reports: `reports/first_person/regeneration_20261115/gameplay_fitting.json`, `unity_geometry.txt` and `approved_three_preservation.json`. The existing HUD fade during cleaning is intentional and was retained.
