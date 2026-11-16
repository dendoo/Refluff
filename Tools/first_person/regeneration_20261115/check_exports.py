"""Read-only geometry/hash audit of downloaded replacement GLBs; no imports or Unity connection."""
from __future__ import annotations
import hashlib
import importlib.util
import json
import argparse
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
spec = importlib.util.spec_from_file_location("tool_glb_inspector", HERE.parent / "inspect_glb.py")
inspector = importlib.util.module_from_spec(spec)
spec.loader.exec_module(inspector)
plan = json.loads((HERE / "import_plan.json").read_text(encoding="utf-8-sig"))
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--downloads", action="store_true", help="Inspect outside Assets before the exclusive Editor import window")
args = parser.parse_args()
report_dir = ROOT / "reports/first_person/regeneration_20261115/models"
report_dir.mkdir(parents=True, exist_ok=True)
summary = []
for model in plan["models"]:
    path = HERE / "downloads" / Path(model["source"]).name if args.downloads else ROOT / model["source"]
    if not path.exists():
        print("MISSING", model["tool"])
        summary.append({"tool": model["tool"], "downloaded": False})
        continue
    report = inspector.inspect(path)
    report["sha256"] = hashlib.sha256(path.read_bytes()).hexdigest()
    report["reviewed_in_plan"] = bool(model.get("reviewed"))
    report_path = report_dir / (model["tool"] + "_geometry.json")
    report_path.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    summary.append({"tool": model["tool"], "downloaded": True, "triangles": report["triangles"],
                    "vertices": report["vertices"], "finite_indices_pass": report["valid_geometry"],
                    "degenerate_faces": report["degenerate_faces"], "sha256": report["sha256"]})
    print(model["tool"], report["triangles"], "triangles; finite/index", "PASS" if report["valid_geometry"] else "FAIL",
          "; zero-area", report["degenerate_faces"], "; visually reviewed", bool(model.get("reviewed")))
(report_dir.parent / "exports_summary.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")
