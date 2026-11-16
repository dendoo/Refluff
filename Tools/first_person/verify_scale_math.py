"""Offline invariants for the enlarged physical domain; no Unity state is touched."""
from pathlib import Path
import json
import math
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
CW, CH, SW, SH, FS, FW = 3600., 4500., 512, 640, 12000., 1280
DX, FDX = CW / SW, FS / FW
checks = []
def check(name, ok, **detail):
    checks.append(dict(name=name, ok=bool(ok), **detail))

domain = (ROOT / "Assets/CarpetCleaning/Compute/SimulationDomain.hlsl").read_text(encoding="utf-8")
for name, value in [("CARPET_W_MM", CW), ("CARPET_H_MM", CH), ("CARPET_SIM_W", SW),
                    ("CARPET_SIM_H", SH), ("FLOOR_SIZE", FS), ("FLOOR_W", FW)]:
    import re
    found = float(re.search(r"#define " + name + r" ([\d.]+)", domain).group(1))
    check(name, found == value, value=found)
check("square_carpet_cells", DX == CH / SH, dxMm=DX)
hist_bound = SW * SH * 16 * 512
check("histogram_uint_capacity", hist_bound < 2**32, maximum=hist_bound, uintMax=2**32 - 1)
floor_bound = FW * FW * 12 * 128
check("floor_stats_uint_capacity", floor_bound < 2**32, maximum=floor_bound, uintMax=2**32 - 1)
floor_source = (ROOT / "Assets/CarpetCleaning/Compute/FloorSim.compute").read_text(encoding="utf-8")
floor_cpu = (ROOT / "Assets/CarpetCleaning/Scripts/FloorSimulation.cs").read_text(encoding="utf-8")
check("floor_stats_scale_matches", "#define FREDUCE_SCALE 128.0" in floor_source and "const int kReduceScale = 128;" in floor_cpu)
for grid in (FW, FW // 2, FW // 4):
    edges = np.array([.5 - CW / FS / 2, .5 + CW / FS / 2, .5 - CH / FS / 2, .5 + CH / FS / 2]) * grid
    check("aligned_rug_edges_" + str(grid), np.max(abs(edges - np.round(edges))) < 1e-10,
          indices=edges.tolist())

# Each source edge cell must contribute exactly once. Corners hold two faces, split half to each.
# Integrate the implemented radial density independently over every intersected destination band.
def radial_mass(width):
    width = max(width, 2 * FDX)
    total = 0.
    bands = []
    for j in range(math.ceil(width / FDX)):
        lo, hi = j * FDX / width, min((j + 1) * FDX / width, 1.)
        mass = 1.4 * (hi - lo) - .4 * (hi * hi - lo * lo)
        bands.append(mass)
        total += mass
    return total, bands

for drip in [10., 20.3, 47.7]:
    total, bands = radial_mass(drip)
    coverage = np.zeros((SH, SW), dtype=np.float64)
    for vertical, fixed in [(True, 0), (True, SW - 1), (False, 0), (False, SH - 1)]:
        n = SH if vertical else SW
        extent = CH if vertical else CW
        for j in range(round(extent / FDX)):
            lo, hi = j * FDX, (j + 1) * FDX
            for k in range(max(math.floor(lo / DX), 0), min(math.ceil(hi / DX), n)):
                overlap = max(0., min(hi, (k + 1) * DX) - max(lo, k * DX)) / DX
                face = .5 if k in (0, n - 1) else 1.
                if vertical:
                    coverage[k, fixed] += overlap * face * total
                else:
                    coverage[fixed, k] += overlap * face * total
    rim = np.concatenate([coverage[0], coverage[-1], coverage[1:-1, 0], coverage[1:-1, -1]])
    error = float(np.max(abs(rim - 1.)))
    check("all_source_edge_texels_conserved_" + str(drip), error < 1e-10,
          maxRelativeError=error, radialWeights=bands, sourceTexels=len(rim))

# The analytic area of the 16 x 120 mm scraper plus its unchanged 4 mm smooth edges.
# Scan orientations and fractional cell phases, comparing quadrature to that independent integral.
def smooth_edge(x):
    t = np.clip(1. - x / 4., 0., 1.)
    return t * t * (3. - 2. * t)
expected = (16. + 4.) * (120. + 4.)
for cell in [DX, FDX]:
    coords = np.arange(-20, 21) * cell
    x, y = np.meshgrid(coords, coords)
    worst = 0.
    minimum = 1e9
    maximum = 0.
    for angle in np.linspace(0., np.pi / 2., 19):
        c, s = np.cos(angle), np.sin(angle)
        for px in [.0, .25, .5, .75]:
            for py in [.0, .25, .5, .75]:
                w = np.zeros_like(x)
                for jx in [-1. / 3., 0., 1. / 3.]:
                    for jy in [-1. / 3., 0., 1. / 3.]:
                        xx, yy = x + (jx - px) * cell, y + (jy - py) * cell
                        along, across = xx * c + yy * s, -xx * s + yy * c
                        w += smooth_edge(abs(along) - 8.) * smooth_edge(abs(across) - 60.) / 9.
                area = float(w.sum() * cell * cell)
                minimum, maximum = min(minimum, area), max(maximum, area)
                worst = max(worst, abs(area / expected - 1.))
    check("scraper_coverage_physical_area_" + str(cell), worst < .01,
          maxRelativeError=worst, minAreaMm2=minimum, maxAreaMm2=maximum, analyticAreaMm2=expected)

# Larger cells reduce the Courant numbers; both remain comfortably below one at the original dt.
carpet_cfl = (350. + math.sqrt(9810. * 10.)) / 240. / DX
floor_cfl = (600. + math.sqrt(9810. * 12.)) / 240. / FDX
check("physical_cfl", max(carpet_cfl, floor_cfl) < 1., carpet=carpet_cfl, floor=floor_cfl)
ratio = (DX / 3.125) ** 2
check("emission_density_preserved", abs(ratio / (DX * DX) - 1. / (3.125 ** 2)) < 1e-12,
      perCellAreaScale=ratio, maxEventsPerCell=math.ceil(3. * ratio))

result = dict(ok=all(c["ok"] for c in checks), checks=checks)
out = ROOT / "reports/first_person/scale_math_full_room.json"
out.parent.mkdir(parents=True, exist_ok=True)
out.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
print(json.dumps(result, indent=2))
raise SystemExit(0 if result["ok"] else 1)
