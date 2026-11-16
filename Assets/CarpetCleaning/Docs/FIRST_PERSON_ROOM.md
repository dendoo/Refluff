# Larger workshop, closed door and window light (2026-11-15)

**Current revision (2026-11-16):** `WORKSHOP_VISUALS.md`, `WORKSHOP_DRESSING.md` and
`WORKSHOP_ATMOSPHERE.md` supersede the lighting, furnishing, shaft settings and initial camera pitch
below. Room dimensions and walking boundaries remain the same. The following records the original
November 15 implementation and its historical QA.

The first-person room is **12 × 12 metres inside the walls**, with a 3.8 m ceiling. The surrounding
walkspace accommodates the enlarged 3.6 × 4.5 m carpet. Carpet and floor simulation dimensions are
defined by their own runtime/compute constants; the room builder does not scale simulation transforms.

## Layout and ownership

`Editor/FirstPersonSceneBuilder.cs` remains the scene source of truth for `Workshop` and the player
wiring. `CarpetSceneBuilder.Build` calls it after the carpet, floor and UI have been constructed.
Every build replaces `Workshop`, reuses named material assets in `Materials/Workshop`, and resets
the existing player. Do not edit the `.unity` scene by hand.

- Interior wall faces are at x/z ±6 m. All four walls and the invisible walking floor have colliders.
- The player clamp is x/z ±5.65 m; CharacterController resolves furniture and the door leaf as well.
- Spawn is `(0, -0.003, -3.8)`, with the unchanged 1.62 m eye height and 68° FOV. Both the camera and
  `FirstPersonPlayer.initialPitch` are set to 43°. Its initial centre ray reaches z ≈ −2.06 on the rug.
- The generated room has no baked occlusion data. The builder disables camera occlusion culling;
  otherwise the inherited camera setting can hide the rebuilt far wall and furniture. Frustum
  culling and normal depth testing remain active.
- The upgrade workbench is at x=−4.6, z=0.5. Its existing E interaction, tabletop and terminal colliders
  are retained. The rear-right storage cabinet is centred at x/z=4.8.
- The north wall has an identifiable **closed, non-interactive door**: solid leaf, frame, recessed
  panels, threshold, handle and hinges. The wall behind it also remains solid. No opening mechanic
  or new HUD string is introduced.
- Factory windows sit on the south and east walls. Their three columns each have two glass panes,
  with real frame gaps and a horizontal mullion. Six ceiling fixtures keep the room's proportions.

## Daylight and bloom

The existing key light remains at rotation `(50, −30, 0)`, intensity 3, with the same flat ambient,
ACES, exposure and bloom settings. That light travels down/north/west, so south/east windows face its
source. The carpet's established shading is not retuned for the room.

The window material `WindowDaylight` has HDR emission 5, which passes the existing bloom threshold
1.2. It is separate from the terminal's `Daylight` material, so making the windows brighter does not
blow out the upgrade screen. Furniture fills move with the larger layout, and a weak outward-facing
fill makes the closed door readable. These local lights do not change the simulation.

URP 17.7 validates `_EMISSION` against `MaterialGlobalIlluminationFlags.RealtimeDirectEmission`.
The material helper sets this flag as well as the HDR colour and keyword: setting only the colour
and keyword is insufficient, because the validator removes the keyword when assets are saved.

## Volumetric window shafts

`Shaders/WindowLightVolume.shader` integrates a bounded three-dimensional medium. Each window column
gets one volume and one saved material, for **six volumes total**. The renderer's cube is only a raster
bound; it is not a translucent beam card or a painted opaque plane.

For each covered pixel the shader:

1. Transforms the camera ray into sheared portal coordinates and finds entry/exit intervals through
   the upper and lower half-pane separately, including when the camera is inside the volume.
   Ray distance stays in world metres under the nonuniform proxy scale.
2. Caps that interval at the opaque scene depth reconstructed with the inverse view-projection matrix.
   The final 16 cm fade softens its intersection with the floor or foreground furniture.
3. Samples the actual **oblique extrusion of the rectangular window aperture**. Projecting each sample
   back along the light direction onto the window plane keeps the shaft attached to the real panes;
   the horizontal mullion masks its density too.
4. Integrates extinction and single scattering with Beer–Lambert transmittance in **8 samples per
   half-pane, at most 16 total**. Analytic interval clipping avoids regular sampling bands across
   the thin horizontal mullion; integrating the full loose AABB produced visible bands.
   Low-amplitude three-dimensional density noise drifts slowly, suggesting dust without large fog puffs.
   Forward scattering gives the rays more presence when looking toward the light.

The pass draws exit faces, writes no depth, and uses premultiplied blending. Opaque depth testing is
done by the ray interval rather than testing the distant exit face. The builder requests a depth
texture on the main URP camera. Beam objects have no colliders and use Ignore Raycast, so they cannot
block cleaning, workbench selection or walking.

Saved material parameters are:

| Parameter | Meaning | Initial value |
| --- | --- | --- |
| `_WindowOrigin` | Portal centre in world metres | Per column |
| `_WindowRight`, `_WindowUp` | Unit axes and aperture half extents | Per column |
| `_BeamDirection` | Unit travel direction and extrusion length | Original sun direction, 4.8 m |
| `_BeamParams` | Extinction/m, edge softness, depth fade, half mullion | 0.046, 0.055 m, 0.16 m, 0.0275 m |
| `_NoiseParams` | Noise frequency, variation, drift speed, scattering gain | 1.15, 0.18, 0.035, 3.5 |
| `_Anisotropy` | Forward scattering preference | 0.28 |

### Scope of occlusion

Occlusion is based on the **camera's opaque depth**. A foreground wall, door or object correctly clips
the visible shaft. This is not a full participating-media solver: it does not march a second shadow
ray from every sample to the sun, and it does not create volumetric shadows cast by furniture onto
other parts of a shaft. Transparent water does not occlude it; surfaces absent from the camera depth
texture cannot cut it off. Bloom comes from the HDR windows, while the shafts add subtle scattering.

## Verification and capture

`reports/first_person/window_volume_static.json` records 22 passing offline checks: direction/portal
geometry, box intervals in world metres, inside-camera intervals, foreground clipping, 16-step
Beer–Lambert energy, shader/material property agreement, and the spawn-ray position.

After the central shader import, C# compile, scene build and Play start, run:

`unity command eval_file --file C:/UnityProjects/CarpetCleaning/Tools/first_person/verify_room.cs`

Run it **three times**. Each call checks the actual scene bounds, walking floor, player clamp, door
ray/body collision, wall collision independent of the numerical clamp, reachable/out-of-range
workbench aim, camera depth request, six distinct volume materials, HDR emission and shader errors.
It captures one 2560×1440 HUD view and restores the original player pose, movement bounds, input flags
and pause state in a `finally` block. The script refuses to disturb an open modal or Steam overlay.

- `Assets/Screenshots/first_person_room_door.png`
- `Assets/Screenshots/first_person_south_window.png`
- `Assets/Screenshots/first_person_east_window.png`
- `Assets/Screenshots/first_person_room_overview.png` — additional overview, Russian HUD, generated dry vacuum;
  captured by `Tools/first_person/capture_room_overview.cs` at the real player eye height/FOV.
- Reports: `reports/first_person/room_qa_0.json` through `room_qa_2.json`.

Final live D3D12 QA on 2026-11-15 passed **18 checks in all three views**, after the final compile,
Build Scene and fresh Play. The shader reported
zero compile errors before and after rendering; door collision stopped the player at z=5.565, and
the wall independently stopped it at z=5.765. The reachable bench succeeded and the distant aim
was rejected. The eighteenth check asserts that legacy camera occlusion is disabled. Material checks
also assert the final scattering gain of 3.5 on all six volumes.

Visual review found and fixed the half-pane sampling bands, URP emission keyword validation, and
the inherited camera occlusion setting. Disabling camera occlusion restored the distant door,
wainscot and furniture in `Assets/Screenshots/room_occlusion_off.png` with all six volumes active.
HDR rendering verified bright windows, soft bloom and smooth shafts in
`Assets/Screenshots/room_hdr_encoded.png`. Final A/B review selected scattering gain 3.5 instead of
1.25/2.2: diagonal shafts remain localized to the window volumes and read against the wainscot;
extinction stays 0.046, so this does not increase their opacity. The final four standard captures above
use that setting. No performance measurement is claimed.

The old `EconomyHud.DebugCapture` camera target was ARGB32; URP consequently clamped HDR light before
ACES and bloom, making windows flat gray in the early captures. It now reuses
`StoreShotApi.NewCaptureTarget/RenderHdr/FreeCaptureTarget`: ARGBHalf scene rendering followed by
sRGB encoding, with the existing UI capture and premultiplied composition unchanged. All four final
captures use this corrected path at 2560×1440. Play was stopped after QA; the live console reported
zero errors/warnings and compilationFailed=false. The editor save SHA256 remained
`97b434b9148bb8b9ac4162e6aa3532964dacfece5ad012824f960f73658faba6` throughout the final capture pass.
