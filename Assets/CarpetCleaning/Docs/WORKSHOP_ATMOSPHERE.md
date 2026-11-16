# Workshop atmosphere — 2026-11-16

`FirstPersonSceneBuilder` calls `WorkshopAtmosphereBuilder.Configure(room)` after creating the six
window-column volumes. The builder is the source of truth; no manual scene edits are required.
Carpet textures, materials, geometry and simulation are outside this feature.

## Rendering and motion

`Shaders/WindowLightVolume.shader` raymarches the physical, oblique extrusion of each window aperture.
It analytically intersects the upper and lower pane separately, sorts them front-to-back, then uses
12 samples per pane (24 maximum). Empty intervals do no sampling. Transmittance and single scattering
use Beer–Lambert integration; extinction is modulated by two counter-drifting samples of a repeating
3D density field. A fixed per-pixel offset breaks up regular shadow-sampling bands without introducing
time-varying screen noise.

The medium is clipped against the camera's opaque depth **and** the physical floor/ceiling planes
at **y = −0.003 m / 3.8 m**. These explicit planes are essential: the 7 m proxy bounds extend below
the room, and some capture/depth configurations otherwise show the below-floor medium. Density also
fades out at those planes. If the room height or floor elevation changes, update both the interval
caps and density bounds in this shader. Foreground objects use the opaque depth texture; transparent
water does not stop the light.

Every sample checks the main directional light's real shadow map. Actual wall openings, frame rails
and mullions are therefore required. Glass and volume proxies do not cast shadows. The shader uses
world-space cascaded shadows even with screen-space surface shadows enabled; it performs one filtered
shadow comparison per sample, without the surface shader's larger PCF kernel.

Four sparse exterior foliage meshes (two layers per window bank) cast shadows only. They have no
colliders, remain outside the walls, and sway slowly in opposite directions. Their shadows move on
the floor and inside the shafts. The two combined mesh assets total 1,856 rendered shadow triangles
across all four instances; leaves have split front/back vertices for valid shadow normal bias.

`Scripts/WorkshopAtmosphere.cs` drives foliage and density with one clock and places 32 faint dust
motes per shaft, 192 total. Motes stay in the pane extrusion and fade before reaching the floor or
travel endpoints. They are small decorative unlit particles, not simulated dust or individual
shadow receivers. This component does not stamp tools or alter any cleaning state.

## Assets and controls

`Editor/WorkshopAtmosphereBuilder.cs` creates reusable assets in `Materials/Workshop`:

- `AtmosphereDensity.asset`: deterministic repeating 48³ R8 density texture.
- `AtmosphereMote.asset` and `AtmosphereDust.mat`: soft 32² particle texture and transparent material.
- `AtmosphereFoliage0/1.asset` and `AtmosphereFoliage.mat`: two sparse combined shadow meshes.

Final starting settings: extinction **0.042/m**, edge softness **0.065 m**, depth fade **0.18 m**;
noise scale/variation/drift/scattering **0.16 / 0.65 / 0.035 / 2.55**; anisotropy **0.32**;
scattering tint **(1, 0.86, 0.62)**. Aperture dimensions, mullion width and sun direction are supplied
by `FirstPersonSceneBuilder`, with a 7 m extrusion. Tune saved material values through the helper so
rebuilding the scene reproduces the result.

For deterministic captures, call `WorkshopAtmosphere.SetPreviewTime(seconds)`; this freezes density,
foliage and motes at the supplied time. `ResumeTime()` resumes the live clock. Private caches are
recreated in `Awake`, including when domain/scene reload are disabled. No runtime textures are created.

## Reference and limits

The implementation was informed by the user's `StainedGlassKeeper` project: its embedded CQF
`VolumetricFog.hlsl` / `VolumetricShadows.hlsl` use real shadow maps and counter-drifting density
layers. No package code, textures or models were copied. The supplied Kyle Banks video also uses
outside moving shadow-only geometry to break up window light.

This is bounded single scattering, not a whole-room multiple-scattering solver. There is no
additional-light volumetric pass or arbitrary moving sun direction support: changing the sun requires
rebuilding the portal extrusion and proxy bounds. The floor/ceiling bounds assume this workshop's
current coordinates. Cost scales with visible volume pixels, overlap and sample count; no standalone
GPU timing is claimed here.

## Verification recipe

Use the real player camera and HDR capture path. After importing the shader, render it once and inspect
`ShaderUtil.GetShaderMessages` again for the actual D3D12 variant. Keep simulation and pose fixed for
time comparisons; restore pause/input/pose state afterward.

1. Capture the overview and south window. Tile detail must remain visible beneath the beam; rays must
   terminate at the floor and remain attached to their panes, without streaks across the ceiling.
2. Capture `SetPreviewTime(0f)` and `SetPreviewTime(8f)`. The density and foliage shadows should move;
   room geometry, camera and carpet state must remain fixed.
3. For an exact inside-volume pose, call
   `player.DebugPose(-2.086f, -4.325f, 150f, -38f)` to look toward the south window. The eye is in its
   middle column's upper half-pane, approximately 2.25 m along the sun ray, above the mullion's shadow.
   Reverse to `player.DebugPose(-2.086f, -4.325f, -30f, 38f)` to look down the shaft. No face popping
   or volume disappearance should occur.
4. Place an opaque foreground prop between camera and shaft and check the depth cut at its silhouette.
   A small wet-floor patch should still show water beneath the light because water is transparent.

The first full-room render exposed below-floor medium; explicit physical plane clipping removed
the gray floor veil and long fan artifacts. Live overview and south-window captures after that fix
retained the tile pattern and correctly bounded rays. Root-session QA records contain final captures
and compilation/interaction verification.
