# Debris system (lane B of the floor job, 2026-09-19)

Implements `FLOOR_SYSTEM.md` section 3: GPU particles for crust chunks (kind 0), dust puffs (kind 1) and splash droplets (kind 2).
Owned files: `Compute/Debris.compute`, `Scripts/DebrisSystem.cs`, `Scripts/DebrisMeshBuilder.cs`, `Shaders/DebrisChunk.shader`,
`Shaders/DebrisDust.shader`, `Shaders/DebrisDrop.shader`. Shared contracts it depends on: `Compute/DebrisCommon.hlsl`, `Scripts/DebrisStructs.cs`,
`Scripts/FloorSettings.cs` (`_D_*` uniforms), `CarpetSimulation` (EmitBuffer / StampBuffer / StampCount / Fiber / Fluid / SimTime / CarpetReset),
`FloorSimulation` (Fluid, InjectBuffer, Ready — optional).

## Data flow per frame (LateUpdate order 105)
1. `CarpetSimulation` (100) has run K_Emit -> `EmitBuffer` (Append, 8192 x 48 B) holds this frame's `DebrisEmit` list.
2. `DebrisSystem.LateUpdate`: `dt = carpet.SimTime - lastSimTime` (exactly the carpet's dtFrame; 0 when the carpet is paused -> only redraw).
   `ComputeBuffer.CopyCount(EmitBuffer, _emitCount)`; upload `_DtFrame`, `_Time`, `_SimConsts` (W_CAP, PILE_H, F_MAX, H_MAX from carpet.settings), `FloorSettings.Apply`.
   K_DebSpawn (128 groups x 64): `i < min(_EmitCount[0], 8192)` -> `slot = InterlockedAdd(_Head[0]) % 16384`, random quaternion + angVel for chunks, `life = D_DUST_LIFE*(0.7+0.6 seed)` and `pad = lifeMax` for dust.
   K_DebSim (256 groups x 64): one particle per thread (below). Deposits go to `floor.InjectBuffer` via InterlockedAdd (x INJECT_SCALE).
3. Three `Graphics.RenderMeshIndirect` draws (chunk, drop, dust), 16384 instances each. Other kinds / dead instances: chunk and dust collapse to
   scale 0 in the vertex shader, the drop streak puts all four corners outside the clip volume. Each kind is queued at most once per frame
   with a given material (`DebrisSystem.Render`; see the capture trap below).
4. `FloorSimulation` (110) consumes the inject buffer in K_FInject.

`_Head[0]` is monotonic = total spawned since the last K_DebInit; `ReadSpawnedTotalSync()` reads it back (blocking). Reset: `carpet.CarpetReset` -> `ResetDebris()` (K_DebInit).

## K_DebSim state machine (chunks)
- `state 1 (FLY)` is both ballistic flight and sliding. **Sliding is encoded as `vel.y == 0.0` exactly** (set on settle); sliding chunks skip gravity and are glued
  to the ground; if the ground drops away by > 1 mm (sliding off the carpet edge) they get `vel.y = -1e-3` and fall.
- Landing: `bottom = pos.y - 0.5 size <= ground` -> snap, `vy = -vel.y * rest` (`D_REST_CARPET` / `D_REST_FLOOR`), `vel.xz *= 0.7`, `angVel *= 0.6`.
  `vy > 60` -> bounce, else settle (`vel.y = 0`) and `|vel.xz| < 60` -> `vel.xz = 0`.
- Grounded chunks (sliding or resting 2/3/5) every frame: friction `exp(-D_FRICTION dt)` (sliding only), tool loop over `_Stamps` with `FootprintWeight` (p in CMM):
  vac 1/6 `m > 0.15` -> state 4 (target in angVel, life 0.4); squeegee 5 `vel.xz = 0.85 vStroke`; brush 4 `vel.xz += 0.4 m vStroke, vel.y = 120 m` (hop);
  hose 2 `vel.xz += radial 500 m dt`; dryer 7 (`size < 6`) `vel.xz += radial 900 m dt`. Water (`h > 0.3`): `vel.xz -> flow` with rate 3/s, `size -= D_DISSOLVE dt saturate(h/0.5)`,
  `size < 1.5` -> die (+ slurry `0.02 amount` to ch1 over the floor).
  Resolve: `vel.y > 0` -> airborne; `|vel.xz| > goThr` -> sliding (goThr 20 when already sliding or in water, 30 from rest, so hose/dryer pushes accumulate); else rest:
  state 5 in water, 3 on carpet, 2 on floor, `rot = QRestPose(seed)` (flat side down, random yaw, +-10 deg tilt). A resting chunk keeps its sub-threshold
  `vel.xz` (leaking with `exp(-2 dt)`, zeroed only on the landing frame) so hose / dryer / flow pushes accumulate across frames until they pass the threshold.
  Deviations from FLOOR_SYSTEM.md: tools also act on *sliding* chunks (not only resting), otherwise a squeegee/hose could push a chunk only one frame per stop;
  rest thresholds are 60 mm/s on landing (doc), 20 while sliding / in water, 30 from rest.
- Sucked (4): `pos -> target` with rate `D_SUCK_SPEED`, `size *= exp(-4 dt)`, dies at `life <= 0`, `dist < 8` or `size < 0.3`.
- Dust: `gravity x0.04`, drag `exp(-2.5 dt)`, buoyancy +20 mm/s; clamped to `ground + 0.25 size` (hugs the pile / floor and keeps drifting), dies at
  `life <= 0` (`D_DUST_LIFE x (0.7..1.3)`); over the floor it then deposits `amount` into ch3. `p.dirt` holds the ground (mm) under the puff for the
  shader's lift + soft fade: seeded at spawn with the pile top (`e.pos.y - 10`), then low-passed every step,
  `p.dirt += (ground - p.dirt)(1 - exp(-dt / 0.12 s))` (see "Dust puff look" below).
- Droplet: ballistic; on contact over the floor deposits `amount / FLOOR_AREA` mm water (ch0) and `dirt * amount * 0.001` slurry (ch1); on the carpet nothing.
- Safety: NaN or out-of-world (floor domain + 200 mm, y in [-100, 2000]) particles are recycled; pos.y / vel / size / angVel are clamped on write-back.
- Without a floor (`floor == null` or not Ready): `_FloorValid = 0`, `_FFluidTex` = 1x1 black RT, `_FInject` = 4-uint dummy; deposits are skipped, ground off the carpet = `FLOOR_Y`.

## Bindings / budgets
- UAVs: K_DebInit 2 (`_Particles`, `_Head`), K_DebSpawn 2, K_DebSim 3 (`_Particles`, `_FInject`, `_SuckEvents`),
  K_DebClearEvents 1 (`_SuckEvents`). `_SuckEvents` = vacuum crumb-capture events for the tube rattle (2026-09-21, `AUDIO.md` 8):
  written at the SUCKED transition with the exact vanish time, cleared per step by K_DebClearEvents, read back async by `DebrisSystem`. `_EmitIn` is the Append buffer bound as an SRV, `_EmitCount` a 1-uint Structured buffer (CopyCount target).
- Shaders include `../Compute/DebrisCommon.hlsl` (relative include verified) so `DebrisParticle` is byte-identical; `_Particles`, `_CarpetToWorld`, `_KindFilter` are material
  properties declared outside `UnityPerMaterial`. World pos = `mul(_CarpetToWorld, float4(pos.x/1000 - 0.6, pos.y/1000, pos.z/1000 - 0.75, 1))`.
- Meshes: chunk = icosphere 1 subdiv, radial jitter `0.7 + 0.6 hash`, y x0.45, unwelded (240 verts) for flat facets; dust = XY quad +-0.5, one
  winding (both passes are Cull Off). Since 2026-09-25 the droplet is a screen-space motion streak on the same quad ("Splash drop look", "Drop look v2" and "v3" below; the
  icosphere droplet is gone): one `DebrisMeshBuilder.BuildQuad` mesh serves both draws (2026-09-26), each with its own args buffer. The chunk has
  unit diameter and `size` (mm) x 0.001 scales it; the quad is expanded in the vertex shaders.
- Materials expected by the scene builder: `Materials/DebrisChunk.mat` (`CarpetCleaning/DebrisChunk`), `DebrisDust.mat`, `DebrisDrop.mat`. Render queues: chunk Geometry, drop Transparent+5, dust Transparent+10.

## Verification recipes (Git Bash, `cd /c/UnityProjects/CarpetCleaning && export MSYS_NO_PATHCONV=1`)
- Compute: `unity command eval --code "...ImportAsset(\"Assets/CarpetCleaning/Compute/Debris.compute\"...GetComputeShaderMessages..."` (see CLAUDE.md) -> 0 msgs, kernels (64,1,1).
- Shaders: same recipe with `Shader.Find("CarpetCleaning/DebrisChunk|DebrisDust|DebrisDrop")` -> 0 msgs each (2026-09-19).
- C#: dry-run file = union of usings + bodies of DebrisSystem/DebrisMeshBuilder/DebrisStructs/FloorSettings/CarpetSimulation + a FloorSimulation stub;
  `unity command run_script --file "C:/...scratchpad/laneB_dryrun.cs" --entry DryRunEntry.Main --dry_run true` (Windows path! MSYS conversion is off) -> "Compiled successfully", only CS0436.
- Play-mode checks (after integration): scraper strokes near the left edge -> `DebrisSystem.ReadSpawnedTotalSync()` grows, chunks visible left of the carpet;
  hose hold -> droplets + `FloorStatsJson().water > 0`; dry vac over floor chunks -> they shrink into the nozzle.

## Open points for the integrator
- `CarpetSceneBuilder` must create the three materials and wire `DebrisSystem { compute, settings, carpet, floor, carpetRoot, chunkMaterial, dustMaterial, dropMaterial }` on `FloorRoot`.
- Dust has no depth-texture soft fade (`_CameraDepthTexture` is off); since 2026-09-25 it fades analytically over the lowest `_SoftMm` above the
  ground height K_DebSim stores in `p.dirt` ("Dust puff look" below).
- Chunk-vs-chunk collisions are not simulated (piles overlap); emission rates (`D_CHIPS_PER_UNIT`, `D_DUST_PER_UNIT`, `D_DROPS_PER_MM`) are the tuning knobs.

## Dust puff look (2026-09-25)
Problem: scraper dust puffs (kind 1; only the scraper emits them, `dustAcc` in K_Stamp) grew over ~1 s into large OPAQUE beige
discs that read as mud or extra crust lying on the rug, with a hard, plate-cut lower edge.

Root cause (`Shaders/DebrisDust.shader`):
- The size grew as `1 + 3 age01` (area x16), but the alpha followed an unrelated `0.8 x 4 life01 (1 - life01)` envelope. It peaked at mid-life
  (centre ~0.76), so a puff's visible mass (alpha x area) rose ~30x as it spread. Kinematics had been changed to "hug the ground, live the whole
  D_DUST_LIFE" without re-tuning this envelope (SESSION_LOG 2026-09-19).
- A fully crusted scraper lane emits ~130 puffs in a band only ~50 mm wide, so 5-15 grown puffs stacked per pixel. Over-compositing drove it to alpha 1.
- The flat opaque core (`1 - smoothstep(0.3, 1, r)`), the noise in puff UV (it only magnified), and the colour equal to the crust plates
  (0.50, 0.44, 0.36) made the result a solid disc. The billboard centre sat 8.75 mm above the pile, so the pile cut its lower third with a hard line.

Fix:
- Optical depth instead of alpha: `tauC = _Density / grow^2 x exp(-_Settle age) x smoothstep(0, 0.25, life01) x saturate(age / 50 ms)`.
  This is 1/area (mass kept while it spreads), and faster as the dust settles; the last quarter of the life fades out, and a 50 ms ramp avoids a pop.
  Alpha = `1 - exp(-tau)`, so overlapping puffs add optical depth: a dense lane reads as a thicker veil, not a solid slab.
- The profile is a soft Gaussian `exp(-3 r^2)(1 - r^2)`. The breakup is log-normal (mean ~1, so the mass is kept). Its coarse lobes (`_NoiseTex.r`,
  puff UV) ride the spreading puff; its fine eddies (`_NoiseTex.a`, newborn-size units) keep their world scale. The contrast sigma rises 0.55 -> 1.15
  with age, so old puffs tear into wisps.
- Ground: K_DebSim keeps the ground (mm) under the puff in `p.dirt` for dust (`Debris.compute`; the field is documented in `DebrisCommon.hlsl` /
  `DebrisStructs.cs`). The vertex shader lifts the puff until its centre stands `0.5 R` (along the view's up) above the ground. The fragment fades
  over the lowest `_SoftMm` = 12 mm above the ground (a height that is linear over the quad), so on locally flat pile or floor there is no hard
  edge where the ground meets the cloud. The sim, xz motion and deposits are unchanged.
- Known limitation: the fade follows the ground under the puff's centre only. Where a relief step taller than `_SoftMm` crosses the footprint,
  above all the carpet rim (pile top 19-23 mm above the tile), the step still cuts a straight alpha edge (the old shader had the same edge).
  Sampling the ground at 2-3 points across the footprint and fading on the lowest would remove it; not done.
- Rim crossing (2026-09-26 review fix): `GroundAt` switches between pile and floor on a hard test, so the raw ground under a puff dropped
  ~15-23 mm in one frame when it drifted off the rim, and the fade band and the lift popped. K_DebSim now low-passes it (~0.12 s) and K_DebSpawn
  seeds it with the pile top. Edge scraper stroke `Reset(1); StrokeNow(0, 0.08,0.5, -0.06,0.5, 20)`, 60 frames read back: the largest
  per-frame change of `p.dirt` over 7 rim crossings fell from 14.75 mm (7 steps over 5 mm) to 1.89 mm (none over 5 mm).
- Colour: a pale powder (0.68, 0.64, 0.58), lighter than the crust it comes from. Lit `L x 0.6 + SH x 0.8`, and dense cores are up to 20 % darker.
- Material (builder = source of truth, `.mat` synced): `_Density 0.7`, `_Settle 0.6`, `_SoftMm 12`, `_DustColor` above; `_Opacity` is gone.
  The emission knobs (`D_DUST_PER_UNIT 0.06`, `D_DUST_AMOUNT 0.02`, `D_DUST_LIFE 1.6`) are untouched.
- Single puff (centre, analytic, lifeMax 1.6 s): alpha 0.37 / 0.13 / 0.045 / ~0 at 0.1 / 0.5 / 1.0 / 1.5 s. The old shader gave 0.17 / 0.61 / 0.67 / 0.17.
  Visible mass (alpha x area) now goes 0.52 -> 0.49 -> 0.37; before it went 0.24 -> 2.3 -> 5.5.

Measured (same particles rendered with the old shader, the new one and without dust, 1920x1080; re-measured 2026-09-26 with the HDR
`StoreShotApi` (ARGBHalf -> sRGB blit, like the Game view), one draw per capture, the low-passed ground, on rug_salor_turkmen). a = 1 - (slope of
the rug's luminance texture through the dust), per 16 px tile over the cloud, so 0 = clear and 1 = rug hidden:

| capture (t after the stroke ends) | before p50 / p90 / tiles > 0.9 | after p50 / p90 / tiles > 0.9 |
|---|---|---|
| Reset(2,1) fast lane, game camera, t+0 | 0.59 / 0.90 / 12 % | 0.41 / 0.71 / 1 % |
| same, t+0.5 s | 0.91 / 1.00 / 52 % | 0.33 / 0.57 / 0 % |
| same, t+1.0 s | 0.89 / 1.00 / 47 % | 0.22 / 0.41 / 0 % |
| same, t+1.5 s | 0.67 / 0.91 / 13 % | 0.12 / 0.21 / 0 % |
| same, close view, t+0.5 s | 0.98 / 1.00 / 69 % | 0.40 / 0.70 / 1 % |
| Reset(154,0.6) lane, game camera, t+0.5 s | 0.88 / 1.00 / 46 % | 0.32 / 0.55 / 0 % |
| same, close view, t+0.5 s | 0.98 / 1.00 / 65 % | 0.41 / 0.70 / 1 % |
| same, close view, t+1.0 s | 0.90 / 1.00 / 50 % | 0.25 / 0.51 / 0 % |

The cloud's luminance lift falls with age (d2 game camera 33 -> 23 -> 15 -> 7 of 255; before 38 -> 50 -> 42 -> 28). Puff counts are identical
(d2: 127 / 127 / 106 / 61; d154: 58 / 58 / 47 / 25). The 2026-09-25 LDR run gave the same figures within 0.02 (the dust is far below 1.0, so
the LDR clamp barely touched it). Floor dust is bit-identical: `FloorStatsJson().dust` is 2.6491E-07 for Reset(1) + StrokeNow(0,0.08,0.5,-0.06,0.5,20)
+ RunFrames(150), and 9.93411E-07 for Reset(2,1) + StrokeNow(0,0.30,0.5,-0.08,0.5,12) + RunFrames(150), with the raw and with the low-passed
ground. Chunks and droplets are untouched by the dust changes. Captures: scratchpad `fx/dust/ab/after_review/`, sheet `fx/dust/after_review/compare.png`.

Capture trap (it affects every transparent debris capture; fixed in `DebrisSystem` 2026-09-26). A `RenderMeshIndirect` call stays queued until
the next player-loop frame starts, and every camera rendered before then draws it: the Game view, and also a manual `Camera.Render` from an eval
after that frame (`StoreShotApi`), even from a later eval, since evals between frames share one `Time.frameCount`. History:
- first, `SubmitDrawsForCapture()` queued all three draws again for every capture, so in a paused or unfocused editor each capture added one more
  dust and drop copy and alpha compounded (dust lift 23 -> 28 -> 32 in three shots);
- on the evening of 2026-09-25 it queued at most once per frame, but still on top of the live LateUpdate draw: every capture after a real frame
  drew each drop and puff twice. The drop A/B of 2026-09-25 (and its approved look) came from such double draws.
- Since 2026-09-26 `DebrisSystem` remembers, per kind, the material queued this frame, and `SubmitDrawsForCapture()` queues only a kind that is
  missing (for example one whose material was null at LateUpdate). Every capture draws exactly one copy. Verified (`fx/drops/rv/single/`):
  a capture right after a real frame, the same capture from the next eval, and the flush recipe below are pixel-identical; clearing the record
  by reflection (the old behaviour) changes up to 34 levels and adds 36 % drop light.

Material swaps between captures still need a flush: a draw queued with material X cannot be un-queued, so swapping to Y and capturing draws
both. Run one real frame with the sim held and the swapped kind's material null first: `sim.UiPaused = true; debris.dustMaterial = null;
EditorApplication.isPaused = true; EditorApplication.Step();`, restore, then capture. This empties the queue without advancing the sim. A draw
submitted with a temporary material that is then destroyed renders as a 1 m magenta quad at the carpet centre, so flush before destroying it.
Opaque chunks look the same either way. Store shots and A/B images taken before 2026-09-26 over-state dust and drop density.

Verification recipe (Git Bash, `export MSYS_NO_PATHCONV=1`). Scenarios: `Reset(154,0.6f); StrokeNow(0,0.62f,0.52f,0.62f,0.10f,30,1f); PinToolHead(0,0.62f,0.10f,true)`
and `Reset(2,1f); StrokeNow(0,0.15f,0.5f,0.85f,0.5f,25,1f); PinToolHead(0,0.85f,0.5f,true)`. Capture at t+0, then `RunFrames(30)` between captures.
Use `StoreShotApi.RenderScene(path,1920,1080)` plus the close views `RenderView(path,1920,1080, 0.144f,0.45f,-0.95f, 35f,0f,0f, 30f)` (d154) and
`(0f,0.45f,-0.64f, 35f,0f,0f, 30f)` (d2). Each capture runs as flush -> render with dust, and flush -> render with `dustMaterial = null` (ref).
Pass criteria: at most 1 % of tiles above a = 0.9 at t+0.5 s (game camera 0 %) and none from t+1.0 s; p90 falls monotonically with age; t+0 still shows a cloud of 15k+ px at 1080p.
Afterwards set `isPaused = false`.

## Splash drop look (2026-09-25)
Problem: the hose's splash droplets (kind 2) were invisible from the game camera. The store shot `03_hose_spill` showed 36 blue-grey specks
about 1.6 px across at 1080p, with no glint and no streak.

Root cause:
- Drawing, not spawning. `DebrisDrop.shader` scaled a 20-triangle icosphere to the drop's real diameter. `p.size` = lerp(1.5, 3.0, h) mm is
  0.9-2.8 px at the 1080p game camera (focal 1304 px, 1.4-2.3 m away). The only elongation was x1.6 along the velocity. The colour was a flat
  grey (sRGB ~(170,176,185)): slightly darker than the tile, like the pale fibre tips over the pile. The pow-60 glint covered ~4 % of a 2 px disc,
  so no pixel centre sampled it.
- Low, flat, short flights. K_Emit launched at 33-35 degrees: the apex was 14-83 mm, and a drop stayed up only 0.11-0.26 s against the busy pile.
- Not the count: ~225 spawns/s during a hold, ~40 in the air. The emission does not saturate on a flooded spot: spawns per 0.5 s of a hold are
  132 / 105 / 112 / 102, so the jet-driven emission the diagnosis kept in reserve was not needed.

Fix (physics volume untouched: `e.size`, `e.amount` = `D_DROP_WATER` 3.5 mm^3, the 0.5 size collision and the deposits are as they were):
- `Shaders/DebrisDrop.shader`, rewritten. One quad per drop (`DebrisMeshBuilder.BuildQuad`, the same mesh as the dust), laid out in screen pixels:
  - Shape: from the head (where the drop is now) back to where it was `_Shutter` = 1/60 s ago (one 60 Hz frame), with a soft knee toward
    `_MaxStreakPx` 30 (`L = Lraw / sqrt(1 + (Lraw/Lmax)^2)`). A hard cap made every close-up streak the same length.
  - Width: the half-width is the true radius x `_SizeBoost` 1.3, never under `_MinRadiusPx` 1.5. Both px values scale with target height / 1080,
    so a 4K capture shows the same drop. The streak thins to `_TailWidth` 0.55 at the tail (a comet: the direction reads), and alpha falls to
    `_TailAlpha` 0.64 there.
  - Coverage is analytic: distance to the swept segment with a 1 px ramp. A sub-pixel far drop fades by sqrt(true / drawn radius) instead of popping.
  - Shading is a sphere smeared along its motion (normal from the offset to the swept centre, round caps):
    - a clear lens centre (`_BodyAlpha` 0.44);
    - a refraction band toward the edge, `smoothstep(0.15, 0.85, r)`, where the rays bend out to the dimmer room (ambient x 0.85, `_RimAlpha` 1.0).
      This is what makes a drop read over the lit tile (a dark outline) and over the pile (a light one);
    - a Fresnel rim that mirrors the room;
    - the sun's glint: a point at the head (`_GlintHDR` 4.25 x the light colour) plus a weaker copy smeared back along the streak
      (`_GlintLine` 0.2). The lobe is never under 0.75 px at 1080p (scaled with the target height) and is integrated over the pixel (erf box,
      energy-conserving) instead of sampled at the pixel centre. The per-drop strength is skewed, `lerp(0.15, 1, x^2)` from the seed (drops
      wobble): about half the heads clip white and bloom, the rest glint softly.
  - `_BodyAlpha`, `_RimAlpha`, `_TailAlpha` are `Range(0, 1)` and saturated in the shader: alpha above 1 would give the premultiplied blend a
    negative destination weight.
  - A long streak fades by `(2r / (L + 2r))^_StreakFade` (0.2; 1 would be the physical share of the shutter). The head disc never fades.
  - `Blend One OneMinusSrcAlpha` (premultiplied, so the glint adds light), `Cull Off`, ZWrite off, queue Transparent+5 as before.
  - The screen axes are mapped back to world directions with `sign(UNITY_MATRIX_P[1][1])`, so the normal is right in the Game view and in
    render-texture captures alike (y-flipped projection).
- Lamp (`HighlightCommon.hlsl`, Docs/HIGHLIGHTS.md): `HL_Terms(0.7 x slurry C, 2.0, cmm, 1, 0)`. The moisture lamp lights drops blue, as
  HIGHLIGHTS.md always said. The dirt lamp lights them amber only by the slurry they carry. Either lamp dims the body and removes the white glint.
- `CarpetSim.compute` K_Emit: a splash crown `vxy = radial x S x (0.35 + 0.6 h)`, `vy = S x (0.55 + 0.6 h)` (was 0.5 + 0.8 h / 0.35 + 0.5 h).
  That is 50-57 degrees up instead of 33-35. The landing ring is the same (80-500 mm from the launch point on the level), but the apex is
  35-150 mm and the flight 0.17-0.35 s, so drops rise out of the pile into view and ~45 % more are in the air at the same spawn rate.
  Since 2026-09-26 a drop leaves from the nozzle's skirt, `hoseCentre + radial x max(rl, stamp radius)`, not from the cell under the head, so
  none flies up through the head model (they read as pins stuck in the nozzle in close and free views).
- Builder = source of truth (`BuildDebrisMaterials`, constants `Drop*`); `Materials/DebrisDrop.mat` synced by Build Scene. (The old `_Alpha`
  entry left in the .mat is unused.)
- Test API: `DebrisSystem.CountAliveSync(out inRect, kinds, out dropsAir)`. `CarpetDebugApi.DebrisAliveJson()` now adds `"kinds":[chunk,dust,drop]`
  and `"dropsAir"` (kind 2 above 25 mm).

Review fixes (2026-09-26):
- The 2026-09-25 A/B drew every drop twice in its after, tuning and lamp captures (capture trap above). Its BEFORE panels were the new streak
  over the old 33-degree flights plus the old sphere, and its after numbers (47 px, 93 % white heads, contrast 204, tile -35/+32) came from a
  double draw the game never renders: in game the drops were clearly fainter (one draw of 0.25 / 0.4 / 2.5 composites like ~0.44 / 0.64 / x1.7
  with two). Fixed in `DebrisSystem` (one copy per kind per frame), re-captured with one draw, re-tuned to `_BodyAlpha` 0.44, `_TailAlpha` 0.64,
  `_GlintHDR` 4.25 with the skewed sparkle (without the skew 4.25 would turn nearly every head into the same white pin).
- The glint lobe is box-filtered and its floor scales with the resolution (see above). Measured with 4 sub-pixel camera shifts of the centre
  scene: the per-drop head peak varies by a median 3.4 % across the shifts both with point sampling and with the box filter, so no shimmer
  was measurable here (most heads clip after tonemapping); the box filter keeps the lobe's energy constant wherever it lands.
- Drops launch from the skirt (above).

Measured (deterministic scripted runs, identical spawns in every column; all on rug_salor_turkmen, re-measured 2026-09-26). Drop pixels =
|capture - the same frame with the drop pass off| > 12. Every capture draws ONE copy (recipe below) into an HDR target (`ARGBHalf` -> sRGB
blit, like the Game view and the current `StoreShotApi`). "Round 1" is the 2026-09-25 shader and values as the game really drew them.

| | before (sphere, 33 deg) | round 1 (one draw) | after |
|---|---|---|---|
| centre hold `Reset(17,0.38)`: spawns stroke / hold | 142 / 58 | 142 / 58 | 142 / 58 |
| drops in the air / mean height / max height | 36 / 57 mm / 99 mm | 52 / 87 mm / 160 mm | 52 / 87 mm / 160 mm |
| mean distance from the hose centre (all drops) | 153 mm | 172 mm | 188 mm (skirt launch) |
| game camera 1080p: visible drops | 34 | 47 | 49 |
| median drop footprint / length / width (px) | 3 / 3.8 / 1.0 | 44 / 16.9 / 4.8 | 44 / 17.1 / 4.8 |
| median peak luma / heads with a near-white core (>= 240) | 141 / 0 % | 243 / 70 % | 242 / 53 % |
| median contrast (max abs diff) | 113 | 193 | 184 |
| 4K: median length / heads >= 240 | 5.5 / 0 % | 32.5 / 78 % | 31.9 / 60 % |
| rim hold `u 0.06`: drops in the air / over the tile | 48 / 18 | 68 / 21 | 68 / 24 |
| over the tile, per drop: darkest / brightest vs tile (luma), size | -44 / -40, 4 px (a dark speck) | -28 / +32, 17 px | -27 / +24, 24 px |

(The 2026-09-25 table's double-draw after column read 47 px / 93 % / 204 / -35 +32.)

Floor water per hose second (drop-borne only = drops at `D_DROPS_PER_MM` 0.004 minus the same run at 0; `Hold(2, u, 0.5, 1 s)` + 30 frames to
settle; FloorStats.water x 1.296e7 mm^3):

| hose at | spawns | before | round 1 (crown) | after (skirt) | after vs before |
|---|---|---|---|---|---|
| u 0.2 (240 mm from the rim) | 222 | 110.4 mm^3 | 114.3 | 127.7 | +16 % (~5 drops of 222) |
| u 0.1 | 220 | 252.8 | 260.8 | 277.5 | +9.8 % (~7 drops) |
| u 0.05 | 243 | 341.8 | 343.9 | 349.0 | +2.1 % (~2 drops) |

The skirt moves the landing ring out by up to 40 mm, so a few more drops reach the tile. That breaks the old +-5 % criterion at u 0.2 / 0.1
and is accepted (a handful of 3.5 mm^3 drops against 187,000 mm^3 of runoff at u 0.1). At u 0.2 the q = 0 baseline itself varies between
repeats (14842 / 15126), so that row uses the paired 15126; the water the drops deposit during the hold itself is deterministic, 82.3 -> 92.6 mm^3.

Also checked:
- Carpet water does not depend on the drops: with drops 0.283208 / pile water 0.00351014, with `D_DROPS_PER_MM` 0: 0.283211 / 0.00350978
  (`Reset(17,0.38)` + 1 s hose + 30 frames, skirt launch).
- Scraper run `Reset(2,1)`: 223 chunks + 127 dust, 0 drops. Its capture is pixel-identical with the drop pass on and off.
- The drop pass costs nothing measurable: 24 HDR 1080p renders took 159 ms with drops and 191 ms without (noise).
- Lamps (`SetHighlight(0|1, true)`, one draw): the moisture lamp shows blue comets; the dirt lamp shows dimmed grey streaks with no glint
  (these drops carry C ~ 0.001, so they have no amber).
- The streak direction is right: in a low side view, rising drops near the nozzle point up and outward, and falling ones point down.
- Captures: scratchpad `fx/drops/rv/{before,cur,after}/`, sheet `fx/drops/after_review/compare.png`, lamps `fx/drops/after_review/lamps.png`.

Capture trap 2 (history): until the evening of 2026-09-25 `StoreShotApi.Render` rendered into an 8-bit `ARGB32` texture. With a target texture,
URP takes its LDR format for the camera colour buffer, so the scene clamped at 1.0 BEFORE tonemapping: every capture maxed out at sRGB 208, the
sunlit tile read (205,205,205) instead of the Game view's warm (230,220,214), and glints and bloom never showed. `StoreShotApi.Render` now renders
`ARGBHalf` and blits to sRGB (STORE_ART.md "HDR"); the eval-side `fx/drops/hdrshot.cs` does the same.

Verification recipe (Git Bash, `export MSYS_NO_PATHCONV=1`; scratchpad `fx/drops/rv/cap_rv.sh <dir> <centre|edge> [old]`):
- Centre: `Reset(17,0.38f); StrokeNow(2,0.3f,0.5f,0.7f,0.5f,40,1f); PinToolHead(2,0.5f,0.5f,true); Hold(2,0.5f,0.5f,0.25f)`, then
  `RenderScene(path,1920,1080)`, `RenderView(path,1920,1080, 0f,0.45f,-0.64f, 35f,0f,0f, 30f)` and the low view `(0f,0.16f,-0.50f, 14f,0f,0f, 32f)`.
- Rim: `Reset(17,0.38f); StrokeNow(2,0.06f,0.2f,0.06f,0.8f,40,1f); PinToolHead(2,0.06f,0.5f,true); Hold(2,0.06f,0.5f,0.25f)`, same captures with
  x = -0.53.
- After the scripted frames, one real frame with the sim held (`sim.UiPaused = true; isPaused = true; Step()`), then the captures; with the
  per-kind record in `DebrisSystem` that is one draw. To swap in another drop look (the old sphere for a before), swap it BEFORE that frame and
  flush with `dropMaterial = null` before destroying it. End with `UiPaused = false; isPaused = false`.
- Each capture is repeated with `dropMaterial.SetShaderPassEnabled("UniversalForward", false)` as the no-drop ref. Pool counts come from `DebrisAliveJson()`.
- Floor A/B: set `Sim.floorSettings.D_DROPS_PER_MM` to 0 and then 0.004 (restore it in a finally; never SaveAssets in Play). For each value:
  `Reset(17,0.38f)`, read `Floor.FloorStatsSync().water`, `Hold(2,u,0.5f,1f)`, `RunFrames(30)`, read it again.
- Pass criteria:
  - the same spawns as before;
  - at the 1080p game camera, drops at least 3 px wide and 5-20 px long, about half of the heads near-white;
  - over the tile, a dark outline plus a glint;
  - drop-borne floor water within ~+-15 % (the skirt launch, above).
  - (Superseded by "Drop look v2" below for the length and the white-head share.)

### Drop look v2 (2026-09-26)
Problem: the art director's verdict on a still frame: every drop read as a SEWING PIN. A grey shaft of even width (25-40 px at 2560), a
round head clipping to white, and ~50 near-equal streaks pointing outward on an even ring, like a pincushion.

Root cause (all in `DebrisDrop.shader` + its material values; the physics is fine):
- The full-strength sun glint (`_GlintHDR` 4.25) sat on the head disc, with a 0.75 px lobe floor: half the drops got a white, blooming pinhead.
- The refraction band was opaque (`_RimAlpha` 1.0) and wide (`smoothstep(0.15, 0.85, r)`), in a flat ambient grey (x0.85). In a 3-5 px
  streak it covered nearly every pixel, so the shaft read as solid grey.
- `_TailWidth` 0.55 / `_TailAlpha` 0.64 barely tapered the tail, and a full 1/60 s exposure on a still frame reads as a rigid rod.
- Every drop had the same shape and brightness recipe; only the glint strength varied.

Fix (the shader, `CarpetSceneBuilder` constants `Drop*`, `Materials/DebrisDrop.mat` synced by Build Scene; the sim, spawns, sizes and
velocities are unchanged, one draw per frame, every alpha <= 1, px values still scale with the target height / 1080):
- Short exposure with a minimum: the streak spans `_Shutter` 0.011 s (x0.55-1.45 per drop), but never less than `_MinStreakPx` 9 (at 1080p)
  if the drop really moves that far in 1/60 s. A fast drop still reads as motion; a slow one never claims more than one frame of travel.
  The soft knee is now at `_MaxStreakPx` 20 (was 30).
- A teardrop: the half-width eases to `_TailWidth` 0.25 (`lerp(tw, 1, t(2 - t))`), and the alpha falls to `_TailAlpha` 0 over the tail
  (`smoothstep(0, 0.75, t)`), so the motion reads from the fade, not from a rod.
- A clear, water-tinted bead: centre `_BodyAlpha` 0.2 (was 0.44). The refraction band starts further out (`smoothstep(0.25, 0.9, r)`),
  `_RimAlpha` 0.9, and it is darker (`_RimShade` 0.45 x ambient, was 0.85). That is enough for the dark outline over the lit tile, and only a
  faint light one over the pile. The Fresnel rim mirrors `_SkyColor`, the sky the puddles reflect (weight 0.6), instead of the flat ambient
  SH. A faint `_Caustic` 0.12 (the sun focused through the drop) sits on the side opposite the glint.
- The glint is a sparkle, not a pinhead: `_GlintHDR` 3.2, lobe `_GlintSize` 0.28 x the half-width (floor 0.55 px at 1080p, was 0.35 / 0.75).
  It sits where the wobbling drop flashed during the exposure, at a per-drop point 40-100 % of the way from the tail to the head (not always
  the head). `_GlintLine` 0.35 smears it weakly along the streak around that point only. The strength is skewed per drop,
  `lerp(0.2, 1, h^3)`: about a third of the drops show a white core, one pixel wide at 1080p.
- Per-drop variation (`_Vary` 1): the exposure length (x0.55-1.45), the width (x0.8-1.3 over the floored radius) and the brightness
  (x0.6-1, alpha and caustic). The values come from a hash of `p.seed`, which also sets the size and speed, so they are decorrelated from them.
- Lamps unchanged in behaviour: the moisture lamp shows blue teardrops; the dirt lamp dims the drops and switches off both the glint and
  the caustic (`fx/drops_look/lamp/lamps.png`).

Measured (identical spawns in both columns: centre 142 / 58, rim 148 / 71, 52 / 68 drops in the air; rug_salor_turkmen; one draw, HDR
capture; recipe = "Verification recipe" above via scratchpad `fx/drops_look/cap.sh`, metrics `fx/drops/metrics.py`, `tilecon.py`, and
`fx/drops_look/varmetrics.py`):

| | v1 (before) | v2 (after) |
|---|---|---|
| centre, 1080p game camera: visible drops | 50 | 50 |
| median footprint / length / p90 length / width (px) | 44 / 16.9 / 21.7 / 4.8 | 28.5 / 10.4 / 13.5 / 5.1 |
| median peak luma / median contrast (max abs diff) | 243 / 181 | 229 / 169 |
| drops with a near-white core (>= 240) / white px in all drops / per sparkling drop | 52 % / 51 / 2 | 34 % / 21 / 1 |
| centre 4K: median length / width / near-white / white px | 31.1 / 7.5 / 58 % / 203 | 18.9 / 8.4 / 49 % / 92 |
| centre close view: median length / near-white | 35.7 / 71 % | 18.8 / 61 % |
| rim 1080p, per drop over the tile: darkest / brightest vs tile (luma), mean abs, px | -26.6 / +22.2, 13.3, 24 px | -22.6 / +27.6, 14.5, 15 px |
| rim 4K, over the tile: darkest / brightest, mean abs | -47.4 / +32.8, 15.8 | -30.4 / +32.0, 16.1 |
| rim 1080p, over the pile: brightest, mean abs | +155, 41.3 | +147, 36.3 |

The drops keep their visibility: the same count at 1080p, contrast within 7 %, and the per-pixel contrast over the tile is unchanged
(mean abs 13.3 -> 14.5), with a smaller footprint. The sheet is `fx/drops_look/compare.png`: before | after, the full frame, a 2x nozzle crop
and a 2x tile crop, at 1080p and at 4K.

Capture trap: in one Play session `Reset(17, 0.38)` + the centre recipe spawned 166 / 67 drops on a different carpet picture, and 142 / 58 on
rug_salor_turkmen after a Play restart. The spawns depend on the carpet on the sim. Capture before and after in the SAME Play session (swap the
old shader text in, import it, capture, restore; set the old values through the material on the eval side).

Open:
- The hose over bare tile, or with the carpet away, still makes no splash: K_Emit runs only on carpet cells, and FloorSim.compute has no emit path.
- The glint twinkle is static per drop (from the seed); a time-varying twinkle was left out so captures stay deterministic.
- `03_hose_spill` (STORE_ART.md) predates the streaks and needs a re-capture.

### Drop look v3 (2026-09-26)
Problem (review of v2): the drops had become less visible over the light tile, and v2's "contrast over the tile unchanged" was wrong.
v2's per-pixel mean hid it. With the same spawns (rim hold, 1080p), the total luma contrast over tile pixels fell from 12886 to 8946 (-31 %);
at 4K it fell from 57506 to 36899 (-36 %). Behind a 0.7 px eye-like blur, 10 tile drops cleared 20 luma before and 3 after. At 1:1 the
tile drops were pale 4 x 3 px specks that blended into the tile's own grey speckle. Close to the camera they looked like milky white eggs.

Root cause:
- Over a bright tile, v2 drew a thin dark ring (`_RimShade` 0.45, band `smoothstep(0.25, 0.9)`) around a near-clear centre (`_BodyAlpha` 0.2)
  with a white core. At viewing distance the ring and the core average back to the tile colour.
- `_TailAlpha` 0 removed the faint streak that carried the motion over the tile, and a 0.6-1 brightness jitter scaled the alpha down further.
- The lens colour was a constant light blue-grey, so a drop read milky over dark pile and puddles. The broad glint smear (`0.3 x r` wide)
  and the soft caustic (`/0.12`) made near drops white eggs.

Fix (`DebrisDrop.shader`, the builder constants `Drop*`, `Materials/DebrisDrop.mat` via Build Scene; the physics, spawns and draw count are
unchanged, one draw per frame, every alpha <= 1, px values scale with the target height / 1080):
- The lens shows the real scene behind it. It samples the URP opaque texture (`SampleSceneColor`; PC_RPAsset has Opaque Texture on)
  mirrored through the swept centre, like a ball lens: offset `-(1 + _LensInvert 1.5) x o`, mapped from q to screen px by the inverse of
  q's screen derivatives, so it is flip-safe. It passes on `_LensTransmit` 0.35 of that scene, tinted by the water. Over the lit tile that is
  a darker bead; over the dark pile it is dark, not milky; a near drop shows a real inverted refraction.
- The lens density depends on how big the drop is drawn. A few-pixel drop is seen box-filtered, so its ring and lens average to a darker
  bead: `_BodyAlpha` 0.5. A near drop resolves (half-width 3 -> 9 px at 1080p): `_BodyAlphaNear` 0.15, a clear lens, and the band
  starts at 0.5 x r instead of `_BandInner`. The game camera never resolves a drop (r ~ 1.8-2.3 px), so this only changes close views.
- A wider, darker refraction band: `smoothstep(_BandInner 0.12, _BandOuter 0.82, r)` at `_RimShade` 0.28 x ambient, `_RimAlpha` 0.9.
- A faint tail: `_TailAlpha` 0.15, `_TailWidth` 0.3. `_MinStreakPx` 11 (was 9), `_MinRadiusPx` 1.7 (was 1.5).
- The per-drop brightness jitter is narrower: `_BrightMin` 0.8 (was 0.6).
- The glint: `_GlintMinPx` 0.75 (v2 0.55, back to v1). At 0.55 px the brightest pixel swung about 2.6x between a glint centred in a pixel
  and one on a pixel corner; at 0.75 px it swings about 1.9x. `_GlintSize` 0.15 (was 0.28) only shrinks near drops' sparkles. The
  smear along the streak is as thin as the sparkle (`1.2 x lobe`, was `0.3 x r`). The caustic is a point (`/0.04`, was `/0.12`).
  `_GlintHDR` 3.2 and `_GlintLine` 0.35 are unchanged.
- The teardrop grows in with the streak, `taper = saturate(L / 2r)`, so a drop flying along the view ray is a round bead. In v2, `t` was
  forced to 1 at L <= 0.5 px, so the back half of the head popped from opaque to alpha 0 as L crossed 0.5 px.
- The lamps are unchanged in behaviour: the moisture lamp shows blue teardrops; the dirt lamp dims the drops (dark, no amber at C ~ 0.001)
  and switches the glint and caustic off (`fx/drops_look3/lamp/lamps.png`).

Measured (identical spawns in every column: centre 142 / 58, rim 148 / 71; rug_salor_turkmen; one draw, HDR capture. v1 and v2 were
re-captured in the tuning Play session: v1 by swapping its shader text in, v2 with the unchanged shader. v3 was captured after Build Scene
and a Play restart with the built material and matches the tuning session's capture within noise. Scripts in scratchpad `fx/drops_look3/`:
`cap.sh`, `tileblur.py` (tile = ref luma > 170 and chroma < 40; blur = Gaussian sigma x the target height / 1080), `jm.py` (the reviewer's
per-drop measure), `shim.py`.)

| | v1 (pins) | v2 | v3 |
|---|---|---|---|
| rim 1080p: total luma contrast over tile pixels | 12886 | 8946 | 14380 |
| rim 4K: total luma contrast over tile pixels | 57506 | 36899 | 59756 |
| rim 1080p, blobs over the tile: 0.7 px blur > 20 luma / 1 px blur > 15 | 10 / 6 | 3 / 1 | 22 / 20 |
| rim 1080p, tile drops (of ~30) whose 0.7 px-blurred peak > 20 / 1 px-blurred > 15; median | 10 / 6; 17.1 | 4 / 2; 13.6 | 17 / 18; 21.0 |
| rim 4K, the same (blur scaled x2) | 10 / 5; 18.7 | 4 / 3; 14.6 | 21 / 22; 23.3 |
| rim 1080p, per tile drop: area (px, > 12) / mean abs / share with peak > 25 luma | 6.5 / 21.8 / 74 % | 6 / 18.8 / 55 % | 14 / 23.5 / 93 % |
| rim 1080p, per pile drop: area / px above +30 luma / brightest | 37 / 18 / +141 | 25 / 10 / +142 | 30.5 / 14 / +148 |
| centre 1080p: visible drops / median length / p90 / width (px) | 50 / 16.9 / 21.7 / 4.7 | 50 / 10.3 / 13.5 / 5.1 | 51 / 11.8 / 14.3 / 5.5 |
| centre 1080p: median peak luma / contrast | 243 / 183 | 229 / 168 | 232 / 174 |
| drops with a near-white core, 1080p / 4K (1080p : 4K) | 53 % / 64 % (0.83) | 35 % / 50 % (0.70) | 43 % / 52 % (0.83) |
| centre 4K: median length / width | 31.1 / 7.5 | 18.9 / 8.4 | 21.6 / 9.0 |

- Visibility over the tile is back above v1 on every measure, with streaks about 30 % shorter than v1's. Over the pile the drops are bright
  short dashes with a sparkle, larger than v2's but smaller than v1's pins.
- Shimmer: 4 camera shifts of 1/4 px (centre scene). The median of each drop's brightest-pixel max / min is 1.10, p90 1.36. Drops whose
  white core comes and goes: 9 %. The same scene at a 0.55 px floor gives 1.11 / 1.42 / 11 %.
- Sheets: `fx/drops_look/compare.png` (v1 before | v3 after: the full frame, a 2x nozzle crop and a 2x tile crop, at 1080p and 4K) and
  `fx/drops_look3/compare_tile_3way.png` (v1 | v2 | v3 over the tile).

Open:
- The lens samples the opaque texture, which does not contain transparents. A drop over a puddle or foam refracts the pile under it, not
  the water film. That is invisible at the game camera's 2 px drops.
- Every camera here takes the Opaque Texture setting from PC_RPAsset. A camera that switched it off would feed the lens URP's fallback
  texture instead of the scene.
