"""Verify media, unique key assignment and the public/private boundary locally."""
import hashlib
import importlib.util
import json
import re
import subprocess
import sys
import tempfile
import zipfile
from email import policy
from email.parser import BytesParser
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(ROOT / "Tools/steam_art"))
import build_bundle as b
from clip_probe import ffmpeg_exe, probe_media


def key_assignment_checks():
    # Entire fixture stays in a temporary private directory; no fake keys touch drafts.
    with tempfile.TemporaryDirectory(prefix="refluff_assignment_qa_") as task_tmp:
        old_private = b.PRIVATE
        try:
            b.PRIVATE = Path(task_tmp) / "private"
            b.PRIVATE.mkdir()
            sample = [f"QA{i:03d}-DRAFT-XXXXX" for i in range(20)]
            source = Path(task_tmp) / "fixture.txt"
            source.write_text("\n".join(sample), encoding="utf-8")
            first = b.assign_keys(source)
            assert len(first) == len(set(first.values())) == 20
            assert list(first) == [c["id"] for c in b.CREATORS]
            source.write_text("\n".join(reversed(sample)), encoding="utf-8")
            assert b.assign_keys(source) == first, "Rerun changed recipient assignments"
            saved = (b.PRIVATE / "key_assignments.json").read_bytes()
            source.write_text(sample[0]+"\n"+sample[0], encoding="utf-8")
            try:
                b.assign_keys(source)
                raise AssertionError("Duplicate input was accepted")
            except ValueError:
                pass
            assert (b.PRIVATE / "key_assignments.json").read_bytes() == saved
            for c in b.CREATORS:
                _, body = b.message(c, {"media_url": "https://example.org/media", "sender_name": "QA"}, first)
                assert body.count(first[c["id"]]) == 1
                assert not any(k in body for id, k in first.items() if id != c["id"])
                assert "[[" not in body
            config = {"media_url": "https://example.org/media", "sender_name": "QA", "sender_email": ""}
            b.drafts(config, first)
            tracker = b.PRIVATE / "TRACKER.json"
            rows = json.loads(tracker.read_text(encoding="utf-8"))
            rows[0].update(sent_at="2026-10-18", response="fixture response", next_action="fixture next step", custom_note="keep")
            b.write(tracker, json.dumps(rows))
            b.drafts(config, first)
            updated = json.loads(tracker.read_text(encoding="utf-8"))[0]
            assert updated["state"] == "SENT" and updated["sent_at"] == "2026-10-18"
            assert updated["response"] == "fixture response" and updated["next_action"] == "fixture next step"
            assert updated["custom_note"] == "keep"
        finally:
            b.PRIVATE = old_private
    return {"20_unique": True, "stable_on_reordered_rerun": True,
            "duplicates_rejected_without_mutation": True, "exactly_one_own_key_per_letter": True,
            "tracker_history_preserved": True, "temporary_fixture_only": True}


def main():
    ff = ffmpeg_exe()
    report = {"date": "2026-10-18", "key_assignment": key_assignment_checks(), "videos": [], "failures": []}
    expected = {
        "refluff_clean_edit_20s_vertical.mp4": (1080, 1920, 1200),
        "refluff_captioned_25p7s_vertical.mp4": (1080, 1920, 1542),
        "refluff_trailer_60s_november_2026.mp4": (1920, 1080, 3600),
        "01_dry_vacuum_3p5s_1920x1080.mp4": (1920, 1080, 210),
        "02_foam_3p5s_1920x1080.mp4": (1920, 1080, 210),
        "03_rotary_brush_3p5s_1920x1080.mp4": (1920, 1080, 210),
        "04_squeegee_4s_1920x1080.mp4": (1920, 1080, 240),
        "05_wet_vacuum_reveal_4p5s_1920x1080.mp4": (1920, 1080, 270),
    }
    for p in sorted(b.PUBLIC.rglob("*.mp4")):
        info = probe_media(p)
        assert (info["width"], info["height"], info["frames"]) == expected[p.name], p.name
        assert info["codec"] == "h264" and info["pix_fmt"] == "yuv420p"
        assert info["fps"] == 60 and info["audio"], p.name
        cmd = [ff, "-hide_banner", "-nostats", "-i", str(p), "-vf", "blackdetect=d=0.04:pix_th=0.05",
               "-af", "ebur128=peak=true", "-f", "null", "-"]
        run = subprocess.run(cmd, capture_output=True, text=True, errors="replace")
        assert run.returncode == 0, p.name
        tail = run.stderr.rsplit("Summary:", 1)[-1]
        loudness = re.findall(r"I:\s+(-?[\d.]+) LUFS", tail)
        peak = re.findall(r"Peak:\s+(-?[\d.]+) dBFS", tail)
        black = re.findall(r"black_start:([^\s]+).*?black_duration:([^\s]+)", run.stderr)
        decerr = re.findall(r"(?:Error|Invalid data|corrupt)", run.stderr)
        if black or decerr or not peak or float(peak[-1]) > 0:
            report["failures"].append({"file": p.name, "black": black, "decode_errors": decerr, "peak": peak})
        report["videos"].append(dict(file=p.relative_to(b.PUBLIC).as_posix(), **info,
                                      integrated_lufs=float(loudness[-1]) if loudness else None,
                                      true_peak_dbfs=float(peak[-1]) if peak else None,
                                      black_intervals=black, full_decode=True))
        print("verified", p.name, flush=True)
    drafts = list((b.PRIVATE / "drafts").glob("*.txt"))
    assert len(drafts) == 20
    assigned_file = b.PRIVATE / "key_assignments.json"
    assigned = json.loads(assigned_file.read_text()) if assigned_file.exists() else {}
    for i, c in enumerate(b.CREATORS, 1):
        p = b.PRIVATE / "drafts" / f"{i:02}_{c['id']}.txt"
        text = p.read_text(encoding="utf-8")
        own_key = assigned.get(c["id"], "[[STEAM_KEY:"+c["id"]+"]]")
        assert text.count(own_key) == 1
        assert "October 2026" not in text
    emails = list((b.PRIVATE / "drafts").glob("*.eml"))
    assert len(emails) == sum(bool(c["email"]) for c in b.CREATORS)
    for p in emails:
        msg = BytesParser(policy=policy.default).parsebytes(p.read_bytes())
        assert msg["X-Unsent"] == "1" and msg["To"] and not msg["Bcc"] and not msg["Cc"]
        assert len(msg.get_all("To")) == 1 and len(msg.get_payload()) > 0
    manifest = json.loads((b.PUBLIC / "MANIFEST.json").read_text())
    for item in manifest["files"]:
        p = b.PUBLIC / item["file"]
        assert hashlib.sha256(p.read_bytes()).hexdigest() == item["sha256"]
    archive = b.OUT / "REFLUFF_PUBLIC_CREATOR_KIT.zip"
    with zipfile.ZipFile(archive) as z:
        assert z.testzip() is None
        assert all(name.startswith("PUBLIC_REFLUFF_CREATOR_KIT/") for name in z.namelist())
        assert not any("PRIVATE" in name or ".eml" in name or "key_assignments" in name for name in z.namelist())
        for name in z.namelist():
            if Path(name).suffix in {".md", ".txt", ".json"}:
                text = z.read(name).decode("utf-8")
                assert "[[STEAM_KEY:" not in text
                assert not any(key in text for key in assigned.values())
    report.update(drafts=20, email_drafts=len(emails), unique_key_slots=True,
                  public_archive_crc=True, public_manifest_sha256=True,
                  real_keys_provided=bool(assigned), media_link_configured=bool(json.loads((b.PRIVATE / "campaign_config.json").read_text())["media_url"]),
                  readiness="WAITING_FOR_USER_KEYS_AND_PUBLIC_LINK" if not assigned else "SEE_TRACKER",
                  visual_review="contact sheet and November end-card inspected by agent",
                  sent=any(bool(r.get("sent_at")) for r in json.loads((b.PRIVATE / "TRACKER.json").read_text(encoding="utf-8"))))
    b.write(b.PRIVATE / "QA_REPORT.json", json.dumps(report, ensure_ascii=False, indent=2)+"\n")
    print(json.dumps({"videos": len(report["videos"]), "failures": report["failures"], "drafts": 20,
                      "key_checks": report["key_assignment"], "readiness": report["readiness"]}))
    if report["failures"]:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
