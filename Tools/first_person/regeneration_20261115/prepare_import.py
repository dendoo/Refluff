"""Compile reviewed JSON art settings into a replayable Unity eval_file script; does not run Unity."""
from __future__ import annotations
import argparse
import json
import math
from pathlib import Path

DIRECTORY = Path(__file__).resolve().parent
TOOLS = {"Scraper": 0, "Hose": 2, "Brush": 4, "Squeegee": 5, "WetVacuum": 6, "FloorCleaner": 8, "Mop": 9}


def number(value: float) -> str:
    value = float(value)
    if not math.isfinite(value):
        raise ValueError("Art settings must be finite")
    return f"{value:.9g}f"


def vector(values: list[float] | None) -> str:
    if values is None:
        return "null"
    if len(values) != 3:
        raise ValueError("Expected three vector components")
    return "new UnityEngine.Vector3(" + ",".join(map(number, values)) + ")"


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--plan", type=Path, default=DIRECTORY / "import_plan.json")
    parser.add_argument("--tool", choices=sorted(TOOLS), help="Prepare only one reviewed tool")
    parser.add_argument("--out", type=Path, default=DIRECTORY / "import_reviewed.cs")
    args = parser.parse_args()
    plan = json.loads(args.plan.read_text(encoding="utf-8-sig"))
    code = ["// Generated from reviewed art settings by prepare_import.py. No global refresh or scene rebuild.",
            "var report = new System.Collections.Generic.List<string>();"]
    count = 0
    for model in plan["models"]:
        name = model["tool"]
        if name not in TOOLS or model["id"] != TOOLS[name]:
            raise ValueError("Unexpected tool or enum mapping: " + name)
        if args.tool and args.tool != name:
            continue
        if not model.get("reviewed"):
            print("SKIP unreviewed", name)
            continue
        source = model["source"].replace("\\", "/")
        if not source.startswith("Assets/CarpetCleaning/Models/FirstPerson/") or ".." in Path(source).parts:
            raise ValueError("Source must stay in the first-person model folder")
        tool = "CarpetCleaning.ToolId." + name
        rotation = vector(model["source_euler"])
        quoted_source = json.dumps(source)
        if model["kind"] == "contact":
            seam = model.get("bristle_seam_m")
            code.append("report.Add(CarpetCleaning.FirstPersonModelImporter.ImportContact("
                        + ",".join((tool, quoted_source, rotation, vector(model.get("socket_local_m")),
                                    number(-1 if seam is None else seam), number(model.get("contact_band_fraction", .15)))) + "));")
        elif model["kind"] == "held":
            code.append("report.Add(CarpetCleaning.FirstPersonModelImporter.Import("
                        + ",".join((tool, quoted_source, rotation, number(model["longest_size_m"]))) + "));")
            code.append("report.Add(CarpetCleaning.FirstPersonModelImporter.FitNozzle("
                        + ",".join((tool, number(model.get("nozzle_slice_m", .004)), "1f", vector(model["camera_position_m"]))) + "));")
            if model.get("hose_socket_m") is not None:
                code.append("report.Add(CarpetCleaning.FirstPersonModelImporter.SetHoseSocket("
                            + tool + "," + vector(model["hose_socket_m"]) + "));")
        else:
            raise ValueError("Unknown import kind: " + model["kind"])
        count += 1
    code.append('return string.Join("\\n",report);')
    args.out.write_text("\n".join(code) + "\n", encoding="utf-8")
    print(f"Prepared {count} reviewed imports: {args.out}")


if __name__ == "__main__":
    main()
