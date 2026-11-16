# Workshop dressing (2026-11-16)

`Editor/WorkshopDetailBuilder.cs` is the source of truth for the working-room props. The main
`FirstPersonSceneBuilder` calls `WorkshopDetailBuilder.Build(workshop)` after the architectural finish
and lighting pass. Rebuilding the environment replaces `Workshop/WorkshopDressing` and writes the same
mesh/material asset paths. It does not access carpet pictures, carpet materials, simulation settings,
economy state, or saves.

## Layout

Eight groups organize the room and the combined meshes:

| Group | Contents / position |
| --- | --- |
| NorthStorage | Open steel rack at (-3.38, 0, 5.42), oak boards, diagonal braces, slatted crates, cans, spray bottles, cloth stacks and taped delivery boxes |
| WashStation | Recessed porcelain utility sink at (2.45, 0, 5.45), oak surround, tile backsplash, swan-neck tap, exposed drain trap, bucket and towel rail |
| BenchDetails | Perforated board behind the existing west upgrade bench, hanging hand brushes/squeegee/coiled hose, high shelf, task lamp, pencils, cloth and stool |
| ServiceCart | Three-tier wheeled sage cart at (4.75, 0, -1.8), supplies and brush; rotated to break up the room's orthogonal layout |
| WallServices | Copper plumbing/valve, east-wall hose reel, west-wall electrical conduit and isolator, wall clock and fire extinguisher |
| WindowGarden | Sill plants, floor plant in southwest corner, south-wall ribbed radiator, books and mug |
| CabinetDetails | Existing northeast cabinet handles and supplies, corner delivery boxes |
| WorkNotes | Corkboard and pinned papers on west wall; abstract marks only |

The centre remains clear for the 3.6 × 4.5 m rug, including a 0.3 m margin. Coarse furniture colliders
block walking and cleaning rays; individual pencils, caps and bristles do not get physics components.
The upgrade terminal and interaction collider remain owned by the main builder. Northeast position
around (4.7, 0, 3.4) remains available for the separately imported extractor model.

## Geometry and materials

All dressing is original procedural geometry, with no external model or texture sources. Boxes have
actual rounded edges; containers use rotational profiles with distinct shoulders, rims, handles and
labels. Plumbing, hoses and lamp arms use swept tubes. Basin and bucket interiors are geometric
recesses. Plant leaves are curved meshes with separate back vertices so opposite face normals do not
cancel. The leaf width's fractional power clamps its sine input at zero to keep tip coordinates finite.

The builder combines static meshes by zone and material. The saved results live in
`Models/WorkshopGenerated/<zone>_<material>.asset`. It updates existing Mesh assets through the Mesh
API, validates all input vertices for finiteness before combining, and destroys only temporary source
meshes after the combined assets are assigned. There are no generated meshes left for runtime to own.

Materials and generated oak/fine-grain textures live in `Materials/Workshop/Details`. They use the
standard URP Lit shader, cast/receive shadows, and share materials between zones. The palette uses
oiled oak, sage enamel, cream porcelain, aged brass, brushed steel, charcoal rubber and terracotta.
`ApplyOak(Material)` also gives the base room's existing Oak material the same grain. The small task
lamp has a warm emissive interior and a local shadowless spotlight. Other lighting is owned by
`WorkshopFinishBuilder`.

No English labels, instructions or UI were added. Container labels, notes and clock use geometry and
abstract marks. The room therefore introduces no new localization keys.

## Verification

Run `Tools/first_person/verify_workshop_dressing.cs` through `unity command eval_file` after an
environment rebuild. It reports renderer, vertex, triangle and collider counts; checks persistent
meshes, finite geometry, supported materials and shadow flags; and verifies no dressing collider
enters the protected 4.2 × 5.1 m work area. It is read-only and works in Edit or Play mode.

Visual inspection should include the north wall, sink interior, workbench, south sill and cart at the
actual 1.62 m eye height. The root workshop documentation/session log records the resulting captures
and final live verification numbers.

The first successful live audit on 2026-11-16 reported **70 renderers, 137,201 vertices,
199,688 triangles, seven colliders and zero errors**. The first overview review confirmed the furniture
composition and clear work area; light/volume tuning continued independently afterward.
