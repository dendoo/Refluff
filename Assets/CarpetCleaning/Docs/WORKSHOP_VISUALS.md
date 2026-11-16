# Warm working workshop — 2026-11-16

The environment now follows a warm, lived-in cleaning workshop direction. Carpet pictures, carpet
materials/shaders, pile, compute simulation and save rules were not edited. The room remains
12 × 12 × 3.8 m; the central rug and its circulation space remain clear.

## Rebuilding and ownership

`FirstPersonSceneBuilder.Build` calls three focused builders, also reached by the main Build Scene:

- `WorkshopFinishBuilder`: architectural trims/timber, lighting, wall finishes, floor appearance,
  reflection probe, pictographic upgrade screen, normalized Tripo spare extractor.
- `WorkshopDetailBuilder`: eight furnished zones, 70 combined renderers, 199,688 triangles, seven
  broad furniture colliders. See `WORKSHOP_DRESSING.md`.
- `WorkshopAtmosphereBuilder`: six window volumes, animated exterior shadow casters and sparse dust.
  See `WORKSHOP_ATMOSPHERE.md` for integration, physical boundaries and limitations.

Menu **CarpetCleaning / Workshop / Rebuild environment only** rebuilds and saves the environment
without recreating the carpet, UI or simulations. Stop Play before invoking it. This was the menu
used for this update; the `.unity` file was saved by Unity, never edited as text.

## Art direction and rendering

Cream limewash, sage lower panels, oiled oak, warm enamel and worn metal replace the flat room.
Real window apertures and mullions cast the directional light, with the glass excluded from casting.
The sun is `(38, -30, 0)`, intensity 2.8, warm colour `(1, .84, .62)`, soft shadows. The main-light
atlas is 4096 with four cascades over 24 m. Trilight ambient and local station/ceiling bounce fills
keep the shelf fronts readable. A 128 px realtime reflection probe captures the room once at start.
ACES remains active; bloom is .18, saturation +7 and vignette .16. No gameplay depth of field.

The floor now receives real sun/additional-light shadows and SSAO. Its warmer porcelain palette
keeps the existing dirt/water/detergent behaviour. Dry sparkle fades when it becomes subpixel,
preventing glitter-like noise while walking. Carpet rendering code remains unchanged.

The camera starts at the existing position and eye height, with pitch **28°** instead of 43° so the
room is visible above the work area. FOV 68, controls and tool reach are unchanged. The upgrade
screen uses language-independent pictograms; no new user-facing strings were introduced.

## Generated extractor

The stationary spare machine near the northeast cabinet is **1.05 m high**. The user-authorized
Tripo workflow was completed in the internal browser: two image concepts → selected image →
H3.1 Best Quality textured model → front/side/back review → Smart Mesh triangle retopology →
optimized-model review and GLB export. 1,924,471 source triangles became **16,977** (one material).
Cost: 90 existing credits plus two free image slots; no subscription/purchase.

Source concept, original optimized GLB, numerical mesh QA and provenance live in
`Tools/workshop_20261116/`. Imported asset: `Models/Workshop/extractor.glb`.
`Tools/workshop/prepare_extractor.py` gives the GLB root a stable readable name;
the mesh/texture binary chunk is unchanged. The generated prop is original AI content,
not represented as CC0. No StainedGlassKeeper code/assets were copied.

Reference: Kyle Banks, “How to Create Beautiful Games in Unity”,
<https://www.youtube.com/watch?v=1agSNKuAfTM>. Transcript was read through the browser; the main
applied ideas are warm diagonal light, indirect fill, contact shadows, reflection and subtle motion.

## Verified result

- Real C# compile completed without errors. Floor and volume shaders reported no errors after render.
- Dressing audit: 70 renderers, 137,201 vertices, 199,688 triangles, seven colliders, zero errors.
- Environment QA: all 18 checks pass, including door/wall collision, reachable/out-of-range bench,
  depth texture, six unique volume materials and HDR windows. The wall test uses the open gap
  beside the door; its old x=2 path is now correctly blocked by the new sink.
- Camera-inside-volume views checked in both directions. After warming the newly created test
  renderer, an opaque near-camera blocker produces **identical** rays-on/off images (0 pixel error).
- Frozen camera/simulation at atmosphere times 0 and 8 seconds: 42,904 pixels change by >3/255,
  confirming actual density/foliage motion. No camera movement or video edit fakes the effect.
- All 37 protected carpet texture/material/shader/compute files have unchanged SHA256 hashes.
  The editor save hash also remains unchanged (`CE70CA27…B5A6F0`).

Captures and JSON reports: `reports/workshop_20261116/`. Main views are `overview.png`,
`overview_hud.png`, `south_light.png`, `workbench.png`, `wash_station.png`, `spawn.png` and
`inside_volume_0/1.png`. Reproduce using `Tools/workshop/capture_review.cs`,
`verify_environment.cs`, `verify_atmosphere.cs` and `Tools/first_person/verify_workshop_dressing.cs`.
The latter three are evaluation bodies for Unity CLI, not Unity scripts under Assets.

No player build or target-hardware frame-rate benchmark was performed in this visual session.
The editor-only static-cleanup analyzer warnings elsewhere in the project remain outside this change.
glTFast also emits a nonfatal Unity 6000.7 `SetMainObject` hierarchy warning for the extractor;
the imported model, mesh, material and saved scene instance were verified. This does not justify
patching the project's third-party importer package as part of the environment artwork.
