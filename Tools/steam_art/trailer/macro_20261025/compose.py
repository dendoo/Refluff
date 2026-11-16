"""Compose the 2026-10-25 horizontal macro trailer with the established trailer engine.

  python Tools/steam_art/trailer/macro_20261025/compose.py --preflight
  python Tools/steam_art/trailer/macro_20261025/compose.py --card-preview
  python Tools/steam_art/trailer/macro_20261025/compose.py
  python Tools/steam_art/trailer/macro_20261025/compose.py --preview --out REVIEW.mp4
  python Tools/steam_art/trailer/macro_20261025/compose.py --qa

All normal make_trailer.py flags are forwarded. This script does not drive Unity or
publish anything. The source take frame timing and the take's real virtual-clock
audio log are mandatory. No voice, music, synthetic substitute, or date layer.
"""
from __future__ import annotations

import argparse
from dataclasses import replace
import hashlib
import json
import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
ENGINE = HERE.parent
EDL = HERE / "edl.json"
OUT = ROOT / "SteamStore/trailer/macro_20261025/refluff_macro_trailer_v1.mp4"
sys.path.insert(0, str(ENGINE))
sys.path.insert(0, str(ENGINE.parent))

import make_trailer as MT
import foley
import numpy as np
from PIL import Image

_original_build_mix = MT.build_mix


def macro_mix(edl, sr):
    """Motor-only J cut; original contact and rattle samples stay at their picture times."""
    stems, master, sources, notes = _original_build_mix(edl, sr)
    cfg = edl.raw.get("motor_prelap")
    if not cfg:
        return stems, master, sources, notes
    shot = edl.by_id(cfg["from_shot"])
    lead = float(cfg["lead"])
    cross_start = float(cfg["crossfade_start"])
    cross_dur = float(cfg["crossfade_duration"])
    source_in = float(cfg["source_in"])
    bed_len = lead + cross_start + cross_dur
    local_end = cross_start + cross_dur
    origin = shot.in_ + shot.src.t_origin
    # Render the source once, then isolate real motor voices using foley's layer stems.
    length = max(source_in + bed_len, local_end)
    _, layers = foley.render_take(str(shot.src.dir), origin, origin + length, sr, return_stems=True)
    motor_keys = [k for k in layers if k == "motor" or k.startswith("machine_")]
    if not motor_keys:
        raise RuntimeError("mf02 has no genuine motor layer for the authorized prelap")
    motor = sum((layers[k] for k in motor_keys), np.zeros_like(next(iter(layers.values()))))
    motor *= 10 ** (float(shot.audio.get("gain_db", 0)) / 20)
    nbed = round(bed_len * sr)
    ni = round(source_in * sr)
    bed = MT._fit_len(motor[ni:ni + nbed], nbed).copy()
    bed *= MT.ramp(nbed, round(float(cfg["fade_in"]) * sr), round(cross_dur * sr))[:, None]
    delta = np.zeros_like(master)
    cut_at = round(shot.k0 / edl.fps * sr)
    MT._add(delta, cut_at - round(lead * sr), bed)
    # Remove the original early motor, restoring it as the prelap fades. All other layers remain untouched.
    nold = round(local_end * sr)
    original_motor = MT._fit_len(motor, nold).copy()
    original_motor *= MT.ramp(nold, round(edl.audio["cut_fade_ms"] * sr / 1000), 0)[:, None]
    t = np.arange(nold, dtype=np.float64) / sr
    u = np.clip((t - cross_start) / cross_dur, 0, 1)
    restore = .5 - .5 * np.cos(np.pi * u)
    original_motor *= (1 - restore)[:, None]
    MT._add(delta, cut_at, -original_motor)
    stems["foley"] += delta
    master += delta
    plan = foley.build_plan(str(shot.src.dir), sr)
    sources.append({"shot": "01->02", "take": MT.rel(shot.src.dir), "layer": "motor prelap",
                    "at": shot.k0 / edl.fps - lead, "duration": bed_len,
                    "t0": origin + source_in, "t1": origin + source_in + bed_len,
                    "files": sorted({v.clip for v in plan.voices if v.layer in motor_keys})})
    notes.append("Genuine mf02 motor anticipates the cut by 0.5 s; original contact/rattle timing retained.")
    extend_dryer(edl, sr, stems, master, sources, notes)
    return stems, master, sources, notes


def extend_dryer(edl, sr, stems, master, sources, notes):
    cfg = edl.raw.get("dryer_carry")
    if not cfg:
        return
    shot = edl.by_id(cfg["from_shot"])
    cut = shot.k1 / edl.fps
    total = edl.n / edl.fps
    ramp_before = float(cfg["ramp_before_cut"])
    at = cut - ramp_before
    origin = shot.in_ + shot.src.t_origin
    t0 = origin + shot.dur - ramp_before
    t1 = origin + total - shot.k0 / edl.fps
    plan = foley.build_plan(str(shot.src.dir), sr)
    voices = [v for v in plan.voices if v.layer == "machine_Dryer" and v.loop]
    if not voices:
        raise RuntimeError("No genuine running dryer loop is available for the authorized offscreen carry")
    na, nb = round(t0 * sr), round(t1 * sr)
    extended = np.zeros((nb - na, 2), np.float32)
    for voice in voices:
        continued = replace(voice, t_end=max(t1, voice.t_end), fade_out_at=float("inf"), end_fade=0)
        rendered = foley._render_voice(continued, na, nb, sr)
        if rendered:
            start, values = rendered
            extended[start - na:start - na + len(values)] += values
    extended *= 10 ** (float(shot.audio.get("gain_db", 0)) / 20)
    # The actual loop keeps its sample phase and logged speed/gain. Only the editorial level changes.
    t = at + np.arange(len(extended), dtype=np.float64) / sr
    te = total - float(edl.audio["tail_silence"])
    tf = float(edl.audio["tail_fade"])
    u = np.clip((t - at) / ramp_before, 0, 1)
    after_u = np.clip((t - cut) / max(1e-6, te - tf - cut), 0, 1)
    after_db = float(cfg["gain_at_cut_db"]) + after_u * (float(cfg["gain_before_final_fade_db"]) - float(cfg["gain_at_cut_db"]))
    env = 10 ** (after_db / 20)
    env[t < cut] *= (.5 - .5 * np.cos(np.pi * u[t < cut]))
    tail_u = np.clip((te - t) / max(1e-6, tf), 0, 1)
    env *= .5 - .5 * np.cos(np.pi * tail_u)
    env[t >= te] = 0
    desired = extended * env[:, None]
    # The take already contains its genuine coast-down; ease this quiet offscreen bed in beneath its tail.
    # Do not remove, retime or replace any sound synchronized with the recorded tool action.
    delta = np.zeros_like(master)
    MT._add(delta, round(at * sr), desired)
    stems["foley"] += delta
    master += delta
    sources.append({"shot": "07->09", "take": MT.rel(shot.src.dir), "layer": "phase-continuous offscreen dryer carry",
                    "at": at, "duration": total - at, "t0": t0, "t1": t1,
                    "gain_at_cut_db": cfg["gain_at_cut_db"], "gain_before_final_fade_db": cfg["gain_before_final_fade_db"],
                    "files": sorted({v.clip for v in voices})})
    notes.append("Genuine mf07 dryer loop continues offscreen with unchanged phase/speed, quietly declining through the final card.")


def _write(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2, default=str) + "\n", encoding="utf-8")


def validate_edit(edl_path):
    data = json.loads(edl_path.read_text(encoding="utf-8"))
    assert data["fps"] == 60 and data["size"] == [1920, 1080], "1080p60 edit required"
    assert not data.get("music") and not data.get("voice"), "No voice/music authorized"
    assert not data.get("date"), "No release date authorized"
    cursor = 0.0
    for shot in data["shots"]:
        assert shot["start"] == cursor, "A gap/overlap would change the approved sequence"
        cursor += shot["dur"]
        assert shot.get("speed", 1) == 1 and not shot.get("hud"), "Keep real-time gameplay and clean frame"
        assert not shot.get("titles") and not shot.get("title"), "Only final logo/CTA authorized"
        assert not shot.get("audio", {}).get("extra"), "Only source gameplay foley authorized"
        for fx in shot.get("fx", []):
            assert fx["type"] == "endcard", "No wipe or change to gameplay picture authorized"
            assert shot is data["shots"][-1], "Only the final shot has an overlay"
            assert fx["cta"] == "Wishlist on Steam" and not fx.get("date") and not fx.get("studio")
    assert cursor > 0, "The edit must have a positive duration"
    return data


def timeline_seconds(data):
    return max(shot["start"] + shot["dur"] for shot in data["shots"])


def preflight(edl_path, output):
    data = validate_edit(edl_path)
    report = {"edl": MT.rel(edl_path), "duration": timeline_seconds(data), "fps": 60, "size": [1920, 1080],
              "music": None, "voice": None, "date": None, "takes": [], "errors": []}
    for shot in data["shots"]:
        name = shot["take"]
        take_dir = ROOT / "SteamStore/_work/clips" / name
        item = {"id": shot["id"], "take": MT.rel(take_dir), "required_seconds": shot["in"] + shot["dur"]}
        report["takes"].append(item)
        for required in ("seq.json", "audio.jsonl", "f0001.png"):
            if not (take_dir / required).is_file():
                report["errors"].append(f"{name}: missing {required}")
        if any(x.startswith(name + ":") for x in report["errors"]):
            continue
        source = MT.Source(take_dir)
        seq = source.seq
        frames = seq.get("frames", len(source.frames))
        steps = seq.get("frameSteps", [])
        item.update({"available_seconds": source.duration, "frames": len(source.frames),
                     "size": source.size(), "audio_origin_s": source.t_origin})
        if seq.get("errors"):
            report["errors"].append(f"{name}: capture errors {seq['errors']}")
        if not steps or frames != len(source.frames) or len(steps) != len(source.frames):
            report["errors"].append(f"{name}: incomplete sequence")
        if steps and any(b - a != 1 for a, b in zip(steps, steps[1:])):
            report["errors"].append(f"{name}: each simulation Step must be captured")
        if abs(float(seq.get("stepDt", 0)) - 1 / 60) > 1e-6:
            report["errors"].append(f"{name}: stepDt is not 1/60")
        if source.duration + 1e-5 < item["required_seconds"]:
            report["errors"].append(f"{name}: too short ({source.duration:.4f} s)")
        sw, sh = source.size()
        if sw < 1920 or sh < 1080 or abs(sw / sh - 16 / 9) > .001:
            report["errors"].append(f"{name}: horizontal capture at least 1920x1080 required")
        with (take_dir / "audio.jsonl").open(encoding="utf-8") as log:
            header = json.loads(log.readline())
        if header.get("clock") != "virtual":
            report["errors"].append(f"{name}: audio clock must be virtual")
        try:
            plan = foley.build_plan(str(take_dir), 48000)
            item.update({"voices": len(plan.voices), "warnings": plan.warnings,
                         "credits": plan.opts["credits"], "clips": sorted(set(v.clip for v in plan.voices))})
            bad = [w for w in plan.warnings if any(text in w for text in
                   ("NON-COMMERCIAL", "cannot resolve", "no clip named", "no complete machine recording resolves", "muted was on"))]
            report["errors"].extend(f"{name}: {w}" for w in bad)
        except Exception as exc:
            report["errors"].append(f"{name}: cannot build authentic foley: {exc}")
    logo = MT.TT.LOGO
    if not logo.is_file():
        report["errors"].append("Approved logo is missing")
    else:
        report["logo"] = {"path": MT.rel(logo), "sha256": hashlib.sha256(logo.read_bytes()).hexdigest()}
    report["ok"] = not report["errors"]
    _write(output.parent / "preflight.json", report)
    print(json.dumps(report, ensure_ascii=False, indent=2, default=str))
    return report["ok"]


def card_preview(edl_path, output):
    data = validate_edit(edl_path)
    cfg = next(fx for fx in data["shots"][-1]["fx"] if fx["type"] == "endcard")
    # A slate verifies the genuine logo and the final card geometry without inventing a gameplay frame.
    im = np.full((1080, 1920, 3), (62, 16, 24), np.uint8)
    MT.TT.draw_endcard(im, 2.0, cfg)
    boxes = MT.TT.endcard_boxes(cfg, 1920, 1080)
    assert all(MT.TT.inside(boxes[k], MT.TT.safe_rect(1920, 1080)) for k in ("logo", "cta")), boxes
    target = output.parent / "edit/endcard_layout_preview.png"
    target.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(im).save(target)
    _write(target.with_suffix(".json"), {"logo": MT.rel(MT.TT.LOGO), "cta": cfg["cta"],
                                       "date": None, "studio": None, "boxes": boxes, "safe": True,
                                       "note": "Layout slate only; final background is mf09_end gameplay."})
    print(target)


def qa(edl_path, output):
    duration = timeline_seconds(validate_edit(edl_path))
    cmd = [sys.executable, str(ENGINE / "check_trailer.py"), str(output), "--edl", str(edl_path),
           "--out-dir", str(output.parent / "qa"), "--expect-seconds", str(duration)]
    result = subprocess.run(cmd, check=False).returncode
    if result == 0:
        audit_delivery(output)
    return result


def audit_delivery(output):
    samples = MT.decode_audio(output, 48000, cache=False)
    db = lambda x: round(float(20 * np.log10(max(float(np.max(np.abs(x))), 1e-12))), 3)
    report = {"file": MT.rel(output), "bytes": output.stat().st_size,
              "sha256": hashlib.sha256(output.read_bytes()).hexdigest(),
              "decoded_audio": {"samples_per_channel": len(samples), "finite": bool(np.isfinite(samples).all()),
                                "clipped_samples": int(np.sum(np.abs(samples) >= 1)),
                                "sample_peak_dbfs": db(samples), "tail_last_0_08s_peak_dbfs": db(samples[-3840:])}}
    assert report["decoded_audio"]["finite"] and report["decoded_audio"]["clipped_samples"] == 0, report
    _write(output.parent / "delivery.json", report)
    preflight_path = output.parent / "preflight.json"
    if preflight_path.is_file():
        manifest = json.loads(preflight_path.read_text(encoding="utf-8"))
        credits = sorted({credit for take in manifest["takes"] for credit in take.get("credits", [])})
        (output.parent / "AUDIO_CREDITS.txt").write_text(
            "Refluff macro trailer\nGame sound recordings; edited/levelled for the trailer.\n\n" +
            "\n\n".join(credits) + "\n", encoding="utf-8")
    print(json.dumps({"delivery": report}, ensure_ascii=False, indent=2))


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--edl", type=Path, default=EDL)
    ap.add_argument("--out", type=Path, default=OUT)
    ap.add_argument("--preflight", action="store_true")
    ap.add_argument("--card-preview", action="store_true")
    ap.add_argument("--qa", action="store_true")
    args, forwarded = ap.parse_known_args()
    edl, output = args.edl.resolve(), args.out.resolve()
    if args.card_preview:
        card_preview(edl, output)
        return 0
    if args.qa:
        return qa(edl, output)
    if not preflight(edl, output):
        return 1
    if args.preflight:
        return 0
    if "--procs" in forwarded:
        ap.error("Use the established threaded renderer for this edit")
    MT.build_mix = macro_mix
    result = MT.main(["--edl", str(edl), "--out", str(output), *forwarded])
    # The core deliberately permits silent picture reviews. A final macro master may not silently lose its foley.
    paths = MT.out_paths(output, "")
    if paths["report"].is_file() and "--no-audio" not in forwarded and "--frames-at" not in forwarded:
        report = json.loads(paths["report"].read_text(encoding="utf-8"))
        problems = [w for w in report.get("warnings", []) if any(text in w.lower() for text in
                    ("take audio silent", "foley.py missing", "foley.py failed", "foley.render_take failed", ": silent"))]
        if problems:
            raise RuntimeError("Foley failed; master is not ready: " + str(problems))
    return result or 0


if __name__ == "__main__":
    sys.exit(main())
