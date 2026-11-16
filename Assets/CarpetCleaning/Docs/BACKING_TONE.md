# Carpet backing follows the picture (2026-10-21)

Current user rule: the backing repeats the carpet picture locally, matching the colours and local cleanliness
of nearby fibres. Dirty areas have dirty backing; cleaning those areas reveals the clean backing print too.
This replaces the earlier same-day rule choosing a single white or black backing for the entire carpet.

## Rendering

`Shaders/CarpetFibers.shader` uses the same picture albedo for every shell, including shell 0 between tufts:

`lerp(_CarpetColor.rgb, picture.rgb, picture.a) * (0.92 + 0.16 * strandVariation)`

The texture uses the existing `_PicOffset` / `_PicScale` cover fit and clamp sampling. At shell 0, `t = 0`,
so both the bend displacement (`t*t`) and wet-clump shift (`t`) vanish: strand-root UV equals carpet UV.
The backing print stays attached to the carpet while bent fibres keep the colours at their roots.
Transparent parts use the same `_CarpetColor` fallback as the fibres. Ordinary mip filtering applies at
all distances; there is no separate backing-colour override or brightness fade.

The existing shell-0 occlusion (AO 0.45) remains, so the backing still looks recessed in the gaps. Matching
source colour does not force identical lit brightness. Mud, stains, crust and dirty-water/wet shading are
applied afterwards as before.

## Local cleanliness

All shells already read the same live `_DirtTex` at carpet UV: crust, mud, stain and loose grit/dust. The
backing receives the existing stain, mud, crust and dirty-water shading. Previously, the dust-film and crumb
height masks were zero at shell 0, exposing clean artwork underneath dusty fibres.

For these two loose-soil masks only, `looseSoilHeight = t <= 0 ? 1 : t`: backing receives the full tip-level
dust/crumb mask, with the same `_Dirt.w`, colours, thresholds and per-strand variation. Every actual fibre
shell (`t > 0`) retains its previous shading. At zero grit/dust both new backing contributions are exactly
zero. The existing vacuum/water simulation updates the shared dirt channels; no independent backing dirt
state or delayed copy is involved. A dust-only dirty patch now soils its backing too, and a vacuum stripe
clears both together. Geometry, crust/mud height behavior, wetness and lighting remain unchanged.

## Picture changes and cleanup

The first picture, reset and handover paths already set `_PictureTex` and the shared cover fit, so both
backing and fibres follow every image change automatically. The temporary single-image pool is preserved.
No scene rebuild is required.

Removed the superseded `CarpetPictureBacking` light/dark classifier, per-picture cache and related runtime/
builder calls, plus the shader's `_BackingColor` property and uniform. Older material serialization may
retain an unused `_BackingColor` entry until Unity rewrites it; it has no effect on this shader. No GPU
readback, extra texture sample, simulation channel or save field is introduced.

## Verification

Read-only source review confirms shell-0 UV mapping, the shared local picture/dirt samples, the backing's
nonzero dust mask when dirt is present and zero dust/crumb contributions when grit is zero. All `t > 0`
fibre masks and the other soil, wetness and lighting paths remain unchanged.
No Unity calls, compilation, Play-mode checks, screenshots or builds;
the user is checking the result personally. Earlier light/dark classifier verification remains historical
in SESSION_LOG.md.
