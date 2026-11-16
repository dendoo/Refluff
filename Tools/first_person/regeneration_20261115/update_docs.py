"""Apply final scale/model documentation only while the Unity Editor is stopped."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
DOCS = ROOT / 'Assets/CarpetCleaning/Docs'

def replace(path, pairs):
    text = path.read_text(encoding='utf-8-sig')
    for old, new in pairs:
        if old not in text:
            raise RuntimeError(f'Missing expected text in {path}: {old[:90]}')
        text = text.replace(old, new)
    path.write_text(text, encoding='utf-8', newline='\n')

replace(DOCS / 'ARCHITECTURE.md', [
    ('BoxCollider 1.2 x 0.02 x 1.5', 'BoxCollider 3.6 x 0.02 x 4.5'),
    ('WaterSurface` (192x240 grid', 'WaterSurface` (256x320 grid'),
    ('FoamSurface` (384x480 grid', 'FoamSurface` (512x640 grid'),
    ('256x256 grid over the 3.6 m floor domain', '256x256 grid over the 9 m floor domain'),
    ('FloorFoam` (384x384 grid', 'FloorFoam` (480x480 grid'),
    ('starts at (0,-.003,-1.8)', 'starts at (0,-.003,-3.8)'),
    ('pitched 48 degrees', 'pitched 43 degrees'),
    ('two furniture fills and one held-tool fill improve vertical surfaces.', 'three furniture/door fills and one held-tool fill improve vertical surfaces. The workshop is 12x12x3.8 m with a closed north door, HDR south/east windows and six depth-clipped volumetric shafts (`FIRST_PERSON_ROOM.md`).'),
    ('## Carpet simulation state (384x480, per cell, mm / s)', '## Carpet simulation state (512x640, per cell, mm / s)'),
    ('hashed 2.5 mm strand grid', 'hashed 5 mm strand grid'),
    ('Layer count includes shell 0 (backing); pile height, strand layout and simulation resolution are unchanged.', 'Layer count includes shell 0 (backing). The enlarged 3.6x4.5 m rug retains physical pile height and strand spacing; the simulation is 512x640 at 7.03125 mm per cell (`PHYSICAL_SCALE.md`).'),
    ('(u in [-1,2], v in [-0.7,1.7])', '(u in [-0.75,1.75], v in [-0.5,1.5])'),
])

replace(DOCS / 'FIRST_PERSON.md', [
    ('Spawn is (0,-0.003,-1.8), looking down 48 degrees. Movement bounds are x/z -2.95..2.95m;', 'Spawn is (0,-0.003,-3.8), looking down 43 degrees. Movement bounds are x/z -5.65..5.65m;'),
    ('onto the original carpet/floor domain', 'onto the 3.6x4.5m carpet / 9x9m floor domain'),
    ('The hose head becomes a held nozzle with a flexible hose; floor detergent reuses the accepted foam-gun body at 0.85 scale.', 'The hose and floor detergent now each have their own complete generated gun and flexible hose.'),
    ('The rotary brush retains the existing spinning bristle implementation.', 'The new rotary brush separates its stationary housing from the spinning blue bristle skirt (350 rpm, 8mm pile sink).'),
    ('the existing wet-extraction head scaled', 'the new wet-extraction head scaled'),
    ('Three missing tools were made with the requested', 'The initial three missing tools were made with the requested'),
    ('Sources live in `Models/FirstPerson/`; normalized prefabs in `Prefabs/FirstPerson/`; registration and nozzle markers in `Settings/FirstPersonToolSet.asset`.', 'Sources live in `Models/FirstPerson/`; complete-body prefabs in `Prefabs/FirstPerson/` and contact heads in `Prefabs/Tools/`; held registration/nozzles are in `Settings/FirstPersonToolSet.asset`, with contact entries in `ToolModelSet.asset`.'),
])

text = (DOCS / 'FIRST_PERSON.md').read_text(encoding='utf-8')
addition = '''## Seven regenerated tools and larger room

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

'''
text = text.replace('## Upgrade table and HUD', addition + '## Upgrade table and HUD')
(DOCS / 'FIRST_PERSON.md').write_text(text, encoding='utf-8', newline='\n')

scale = (ROOT / 'Tools/first_person/scale_notes.md').read_text(encoding='utf-8-sig')
scale = scale.replace(' Runtime outcomes should be appended after execution.', '')
(DOCS / 'PHYSICAL_SCALE.md').write_text(scale, encoding='utf-8', newline='\n')

replace(DOCS / 'HIGHLIGHTS.md', [
    ('amount (x1024 fixed point)', 'amount (x512 fixed point; reduced on 2026-11-15 to keep the 512x640 raster below uint32 overflow at cap16)'),
    ('outside the 3.6 m sim domain', 'outside the 9 m sim domain'),
])

replace(DOCS / 'FLOOR_SYSTEM.md', [
    ('## 0. Frames, units, domains', '''> **2026-11-15 physical-scale update:** the original design and numeric examples below describe the earlier raster. Current geometry is a 3600x4500mm rug (512x640 cells, DX7.03125mm) and 9000x9000mm floor (960x960, DX9.375mm), centred at CMM(1800,2250), floor min(-2700,-2250). `Compute/SimulationDomain.hlsl` mirrors the C# constants. EdgePull now integrates a normalized density across at least two floor cells, retaining 60/40 distribution and half-weight corners; the old fixed two-band formula below is historical. Particle emission and per-cell caps scale by cell area (5.0625x); jitter follows the full cell. See `PHYSICAL_SCALE.md` for current grids, physical dimensions and GPU conservation evidence.

## 0. Frames, units, domains'''),
])

replace(ROOT / 'AGENTS.md', [
    ('384x480 cells', '512x640 cells, 3.6x4.5 m rug'),
    ('768x768, 3.6 m square around the carpet', '960x960, 9 m square around the carpet'),
    ('x along u (1200 mm), y along v (1500 mm)', 'x along u (3600 mm), y along v (4500 mm)'),
    ('The brush head is `Tool_Brush_v2.glb` (Tripo, part-segmented):\n  its blue bristle parts spin at 350 rpm under a still grey top while it works (`SplitByParts`, colour rule `IsBristle`), and it sinks into the pile\n  (`pileSinkM`) — `Docs/TOOL_MODELS.md` 7.',
     'The current contact heads are regenerated Tripo GLBs in `Models/FirstPerson/`; `FirstPersonContactImporter` fits the physical working width and `HandleSocket`.\n  `Tools/first_person/contact_imports.json` records their normalization; `Normalize Tool Models` preserves these recipes.\n  `FirstPersonBrushSplitter` separates the new brush housing from its blue bristle skirt, which spins at 350 rpm with 8 mm pile sink — `Docs/TOOL_MODELS.md` 10.'),
])
agent_path = ROOT / 'AGENTS.md'
agent_text = agent_path.read_text(encoding='utf-8')
anchor = '## Hard rules (learned the expensive way)'
agent_text = agent_text.replace(anchor, '''- First-person workshop (`Docs/FIRST_PERSON.md`, `FIRST_PERSON_ROOM.md`, `PHYSICAL_SCALE.md`, 2026-11-15): `FirstPersonSceneBuilder` runs from Build Scene, room 12x12x3.8m, closed north door, luminous south/east windows and six depth-clipped volumetric shafts. Camera disables legacy occlusion culling; HDR capture uses `StoreShotApi` before HUD composition. Player spawn (0,-.003,-3.8), eye 1.62m, FOV68, bounds ±5.65m; WASD/arrows, Shift, mouse, 1–0/wheel; E at the separate upgrade bench. Complete held models use `FirstPersonToolSet`; contact heads use `ToolModelSet`. No arms. The 3.6x4.5m rug retains 20mm pile, 48 shells and physical tool dimensions. C# domain constants and `Compute/SimulationDomain.hlsl` must stay in step; HIST_SCALE is 512 in both compute and C# to avoid uint overflow at the larger resolution.

''' + anchor)
agent_path.write_text(agent_text, encoding='utf-8', newline='\n')
print('Updated architecture, first-person, physical-scale, highlights, floor and agent documentation.')
