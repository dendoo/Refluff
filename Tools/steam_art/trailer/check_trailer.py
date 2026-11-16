"""Pre-upload checks for the trailer (Docs/TRAILER.md 9 and 10).

    python Tools/steam_art/trailer/check_trailer.py [VIDEO] [--edl EDL] [--out-dir DIR] [--expect-seconds 62] [--no-clips]

VIDEO = SteamStore/trailer/refluff_trailer_main.mp4, EDL = Tools/steam_art/trailer/edl.json, DIR = SteamStore/_work/trailer/checks/.
Machine checks (each prints PASS / FAIL and lands in DIR/check_report.json):
  format     the EDL's size (1920x1080 by default), H.264 High, yuv420p, BT.709 (tv), 60 fps, CFR (ffmpeg vfrdet), frame count = duration x fps,
             video bitrate >= 5000 kbit/s (Steam) and ~20 Mbit/s, AAC-LC 48 kHz stereo ~192 kbit/s
  duration   the expected length +- 0.25 s (1:02 = 62 s)
  black      ffmpeg blackdetect finds no black frame anywhere (the micro trailer may cut from any second)
  loudness   ebur128: integrated -14 LUFS +- 1, true peak <= -1 dBTP, LRA reported
  titles     every caption, the #03 logo + bubbles and the #21 end card lie inside the 5 % title-safe area, and every
             caption stays up >= 1 s per 3 words (geometry from the EDL through titles.py, the renderer's own code)
  licences   no Samsung dry-vac recording (CC BY-NC) in the render report's audio sources or the EDL; a music track
             needs a licence file in SteamStore/trailer/licences/
Review files for the human checks in 10 (written to DIR unless --no-clips): t600.mp4 (600 px, muted), first5.mp4 (with
sound), festival_12_35.mp4 (muted), micro.mp4 (6 x 1 s at 5, 15, ..., 55 s, muted: Steam's micro trailer, simulated) and
micro_frames.jpg (the middle frame of each micro piece). The end card's date is printed for the Steamworks comparison.
"""
import argparse
import json
import math
import re
import subprocess
import sys
from pathlib import Path

from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))
sys.path.insert(0, str(HERE))
from clip_probe import ffmpeg_exe, probe_media  # noqa: E402

ROOT = HERE.parents[2]
DEFAULT_VIDEO = ROOT / "SteamStore" / "trailer" / "refluff_trailer_main.mp4"
DEFAULT_EDL = HERE / "edl.json"
CHECKS = ROOT / "SteamStore" / "_work" / "trailer" / "checks"
LICENCES = ROOT / "SteamStore" / "trailer" / "licences"
MICRO_AT = (5, 15, 25, 35, 45, 55)

RESULTS = []


def check(name, ok, value, want):
    RESULTS.append({"check": name, "ok": bool(ok), "value": value, "want": want})
    print("%s  %-22s %s   (want %s)" % ("PASS" if ok else "FAIL", name, value, want))


def ff(args, timeout=1800):
    r = subprocess.run([ffmpeg_exe(), "-hide_banner", "-nostats"] + args, capture_output=True, text=True,
                       errors="replace", timeout=timeout)
    return r.returncode, r.stderr


# ------------------------------------------------------------------------------------------------ format
def probe(video):
    info = probe_media(video)
    _, txt = ff(["-i", str(video)])
    v = re.search(r"Stream #\d+:\d+.*?: Video: (.*)", txt)
    a = re.search(r"Stream #\d+:\d+.*?: Audio: (.*)", txt)
    vline, aline = (v.group(1) if v else ""), (a.group(1) if a else "")
    m = re.search(r"Duration:.*?bitrate:\s*(\d+)\s*kb/s", txt)
    info["total_kbps"] = int(m.group(1)) if m else None
    m = re.search(r"(\d+)\s*kb/s", vline)
    info["video_kbps"] = int(m.group(1)) if m else None
    info["profile"] = (re.search(r"h264 \(([^)]*)\)", vline) or [None, None])[1]
    info["audio_line"] = aline
    m = re.search(r"(\d+)\s*Hz", aline)
    info["audio_hz"] = int(m.group(1)) if m else None
    m = re.search(r"(\d+)\s*kb/s", aline)
    info["audio_kbps"] = int(m.group(1)) if m else None
    info["audio_codec"] = aline.split(",")[0].strip() if aline else None
    info["audio_layout"] = aline.split(",")[2].strip() if aline.count(",") >= 2 else None
    return info


def format_checks(info, size=(1920, 1080)):
    check("size", (info["width"], info["height"]) == tuple(size), "%sx%s" % (info["width"], info["height"]),
          "%dx%d" % tuple(size))
    check("codec", info["codec"] == "h264" and info["profile"] == "High", "%s %s" % (info["codec"], info["profile"]),
          "h264 High")
    check("pixel format", info["pix_fmt"] == "yuv420p", info["pix_fmt"], "yuv420p")
    col = info["color"] or ""
    check("colour", "bt709" in col and "tv" in col, col, "tv, bt709")
    check("fps", info["fps"] is not None and abs(info["fps"] - 60.0) < 0.01, info["fps"], "60")
    want_frames = int(round((info["duration"] or 0) * 60))
    check("frame count", info["frames"] is not None and abs(info["frames"] - want_frames) <= 1, info["frames"],
          "%d (duration x 60)" % want_frames)
    vk = info["video_kbps"]
    check("video bitrate", vk is not None and 12000 <= vk <= 26000, "%s kbit/s" % vk, ">= 5000 (Steam), ~20000")
    ac = (info["audio_codec"] or "")
    check("audio codec", ac.startswith("aac") and "LC" in ac, ac or "none", "aac (LC)")
    check("audio format", info["audio_hz"] == 48000 and info["audio_layout"] == "stereo",
          "%s Hz %s" % (info["audio_hz"], info["audio_layout"]), "48000 Hz stereo")
    ak = info["audio_kbps"]
    # ffmpeg's native AAC is ABR: -b:a 192k lands near 192 on dense foley but undershoots on sparse / quiet audio
    check("audio bitrate", ak is not None and 96 <= ak <= 208, "%s kbit/s" % ak, "192 target (ABR: 96-208 accepted)")


def picture_checks(video, info, expect):
    dur = info["duration"] or 0.0
    check("duration", abs(dur - expect) <= 0.25, "%.3f s" % dur, "%.2f s +- 0.25" % expect)
    # one decode pass: constant frame rate + black frames (a 1-frame black flash counts too)
    _, txt = ff(["-i", str(video), "-an", "-vf", "vfrdet,blackdetect=d=0.016:pix_th=0.10:pic_th=0.98", "-f", "null", "-"])
    m = re.findall(r"VFR:([\d.]+)\s*\((\d+)/(\d+)\)", txt)
    vfr = float(m[-1][0]) if m else None
    check("constant frame rate", vfr is not None and vfr == 0.0, "VFR %s" % ("%s (%s/%s)" % m[-1] if m else "n/a"),
          "VFR 0 (CFR)")
    blacks = re.findall(r"black_start:([\d.]+)\s+black_end:([\d.]+)", txt)
    check("no black frames", not blacks, ("%d: " % len(blacks) + ", ".join("%s-%s" % b for b in blacks[:6])) if blacks
          else "none", "none")


def loudness_checks(video, info):
    if not info["audio"]:
        check("loudness", False, "no audio stream", "-14 LUFS, TP <= -1 dBTP")
        return {}
    _, txt = ff(["-i", str(video), "-vn", "-af", "ebur128=peak=true", "-f", "null", "-"])
    tail = txt[txt.rfind("Summary:"):] if "Summary:" in txt else txt
    num = lambda pat: (lambda m: float(m.group(1)) if m else None)(re.search(pat, tail, re.S))  # noqa: E731
    I, lra, tp = num(r"I:\s*(-?[\d.]+|-inf)\s*LUFS"), num(r"LRA:\s*(-?[\d.]+)\s*LU"), num(r"True peak:.*?Peak:\s*(-?[\d.]+|-inf)")
    check("integrated loudness", I is not None and abs(I + 14.0) <= 1.0, "%s LUFS" % I, "-14 +- 1")
    check("true peak", tp is not None and tp <= -1.0, "%s dBTP" % tp, "<= -1.0")
    print("      loudness range     %s LU" % lra)
    fin = lambda v: v if v is None or math.isfinite(v) else str(v)  # noqa: E731  (strict JSON: no -Infinity)
    return {"I": fin(I), "LRA": fin(lra), "TP": fin(tp)}


# ------------------------------------------------------------------------------------------------ EDL: titles + licences
def title_checks(edl_path, safe_frac=None):
    import make_trailer as MT
    import titles as TT
    edl = MT.Edl(edl_path)
    W, H = edl.size
    safe = TT.safe_rect(W, H) if safe_frac is None else (safe_frac[0] * W, safe_frac[1] * H, safe_frac[2] * W,
                                                         safe_frac[3] * H)
    area = "5 % safe area" if safe_frac is None else "safe zone"
    bad, slow = [], []
    for f0, f1, spec, s in edl.titles:
        ctx = {}
        if "logo_whip" in s.fx:
            ctx["logo_box"] = TT.logo_rest_box(s.fx["logo_whip"], W, H, TT.WHIP_WIDTH, TT.WHIP_CY)
        box = TT.title_box(spec, W, H, ctx)
        if not TT.inside(box, safe):
            bad.append("#%s %r %s" % (s.id, spec["text"], [round(v) for v in box]))
        words = len(spec["text"].split()) + (1 if spec.get("num") is not None else 0)
        secs = (f1 - f0) / edl.fps
        if secs + 1e-6 < words / 3.0:
            slow.append("#%s %r %.2f s for %d words" % (s.id, spec["text"], secs, words))
    for s in edl.shots:
        for name, boxes in ((("logo_whip", TT.whip_boxes), ("endcard", TT.endcard_boxes))):
            if name in s.fx:
                cfg = {k: v for k, v in s.fx[name].items() if k != "type"}
                shown = dict(TT.END_DEFAULTS, **cfg) if name == "endcard" else cfg
                for part, box in boxes(cfg, W, H).items():
                    if part in ("cta", "date", "studio") and name == "endcard" and not shown.get(part):
                        continue                                   # an empty end-card line is not drawn
                    if not TT.inside(box, safe):
                        bad.append("#%s %s %s %s" % (s.id, name, part, [round(v) for v in box]))
    check("titles in " + area, not bad, "; ".join(bad) or "%d captions + logo/end card OK" % len(edl.titles),
          "inside %s" % [round(v) for v in safe])
    check("reading time", not slow, "; ".join(slow) or "all >= 1 s / 3 words", ">= 1 s per 3 words")
    date = None
    for s in edl.shots:
        if "endcard" in s.fx:
            cfg = s.fx["endcard"]
            date = cfg["date"] if "date" in cfg else (edl.date or TT.END_DEFAULTS["date"])
    return edl, date


def licence_checks(edl, video):
    hits = []
    txt = Path(edl.path).read_text(encoding="utf-8")
    if re.search(r"samsung", txt, re.I):
        hits.append("EDL mentions samsung")
    stem = Path(video).stem
    base = stem[:-5] if stem.endswith("_main") else stem
    reps = [Path(video).with_name(base + "_render.json"), ROOT / "SteamStore" / "_work" / "trailer" / (base + "_render.json"),
            ROOT / "SteamStore" / "_work" / Path(video).resolve().parent.name / (base + "_render.json")]
    rep = next((r for r in reps if r.exists()), None)
    if rep is not None:
        srcs = json.loads(rep.read_text(encoding="utf-8")).get("audio_sources", [])
        if re.search(r"samsung|100210|debsound", json.dumps(srcs), re.I):
            hits.append("render report audio sources contain the Samsung take")
        unknown = [str(x.get("shot")) for x in srcs if isinstance(x, dict) and "take" in x and x.get("files") is None]
        if unknown:
            hits.append("clips of shot(s) %s unknown: cannot rule the Samsung take out" % ", ".join(unknown))
    elif probe_media(video).get("audio"):
        hits.append("no render report next to the video or in SteamStore/_work/trailer/: audio sources unverified")
    check("no CC BY-NC audio", not hits, "; ".join(hits) or ("clean (%s)" % (rep.name if rep else "no render report")),
          "no Samsung dry vac (TRAILER.md 7)")
    if edl.music and edl.music.get("wav"):
        lic = [p for p in LICENCES.glob("*") if p.is_file()] if LICENCES.is_dir() else []
        check("music licence", bool(lic), ", ".join(p.name for p in lic) or "missing", "a file in SteamStore/trailer/licences/")
    else:
        print("      music              none (no licence needed)")


# ------------------------------------------------------------------------------------------------ review clips
def review_clips(video, info, out):
    out.mkdir(parents=True, exist_ok=True)
    dur = info["duration"] or 0.0
    enc = ["-c:v", "libx264", "-preset", "veryfast", "-crf", "20", "-pix_fmt", "yuv420p", "-movflags", "+faststart"]
    made = {}

    def run(name, args):
        rc, err = ff(["-y"] + args + [str(out / name)])
        if rc == 0:
            made[name] = str(out / name).replace("\\", "/")
        else:
            print("      could not write %s: %s" % (name, err.strip().splitlines()[-1] if err.strip() else rc))

    run("t600.mp4", ["-i", str(video), "-vf", "scale=600:-2", "-an"] + enc)
    run("first5.mp4", ["-i", str(video), "-t", "5"] + enc + ["-c:a", "aac", "-b:a", "192k"])
    if dur > 12.0:
        run("festival_12_35.mp4", ["-ss", "12", "-to", "%.3f" % min(35.0, dur), "-i", str(video), "-an"] + enc)
    at = [t for t in MICRO_AT if t + 1.0 <= dur + 1e-6]
    if at:
        parts = ";".join("[0:v]trim=%d:%d,setpts=PTS-STARTPTS[p%d]" % (t, t + 1, i) for i, t in enumerate(at))
        cat = "".join("[p%d]" % i for i in range(len(at))) + "concat=n=%d:v=1:a=0[v]" % len(at)
        run("micro.mp4", ["-i", str(video), "-filter_complex", parts + ";" + cat, "-map", "[v]", "-an"] + enc)
        thumbs = []
        for t in at:
            rc, _ = ff(["-y", "-ss", "%.3f" % (t + 0.5), "-i", str(video), "-frames:v", "1", str(out / "_micro.png")])
            if rc == 0:
                thumbs.append((t, Image.open(out / "_micro.png").convert("RGB").resize((480, 270), Image.Resampling.LANCZOS)))
        if thumbs:
            sheet = Image.new("RGB", (3 * 480 + 4 * 8, ((len(thumbs) + 2) // 3) * (270 + 30) + 8), (22, 24, 29))
            d = ImageDraw.Draw(sheet)
            for i, (t, im) in enumerate(thumbs):
                x, y = 8 + (i % 3) * 488, 8 + (i // 3) * 300
                sheet.paste(im, (x, y))
                d.text((x + 2, y + 276), "micro piece %d: %d-%d s" % (i + 1, t, t + 1), fill=(230, 230, 230))
            sheet.save(out / "micro_frames.jpg", quality=90)
            made["micro_frames.jpg"] = str(out / "micro_frames.jpg").replace("\\", "/")
        (out / "_micro.png").unlink(missing_ok=True)
    return made


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("video", nargs="?", default=str(DEFAULT_VIDEO))
    ap.add_argument("--edl", default=str(DEFAULT_EDL))
    ap.add_argument("--out-dir", default=str(CHECKS))
    ap.add_argument("--expect-seconds", type=float, default=62.0)
    ap.add_argument("--no-clips", action="store_true")
    ap.add_argument("--safe", default=None,
                    help="x0,y0,x1,y1 frame fractions every caption must stay inside instead of the 5 %% title-safe area")
    a = ap.parse_args(argv)
    safe_frac = tuple(float(v) for v in a.safe.split(",")) if a.safe else None
    if not ffmpeg_exe():
        raise SystemExit("ffmpeg not available: pip install imageio-ffmpeg")
    video = Path(a.video)
    if not video.is_file():
        raise SystemExit("no video: %s" % video)
    print("check_trailer: %s" % video)
    info = probe(video)
    has_edl = bool(a.edl) and Path(a.edl).is_file()
    size = tuple(json.loads(Path(a.edl).read_text(encoding="utf-8")).get("size", [1920, 1080])) if has_edl else (1920, 1080)
    format_checks(info, size)
    picture_checks(video, info, a.expect_seconds)
    loud = loudness_checks(video, info)
    date = None
    if has_edl:
        edl, date = title_checks(a.edl, safe_frac)
        licence_checks(edl, video)
        if date:
            print("      end card date      %r  <- must match the release date in Steamworks" % date)
        else:
            print("      end card date      none shown")
    else:
        print("      no EDL: title and licence checks skipped")
    out = Path(a.out_dir)
    out.mkdir(parents=True, exist_ok=True)
    clips = {} if a.no_clips else review_clips(video, info, out)
    failed = [r["check"] for r in RESULTS if not r["ok"]]
    rep = {"video": str(video).replace("\\", "/"), "edl": a.edl, "ok": not failed, "failed": failed, "checks": RESULTS,
           "loudness": loud, "endcard_date": date, "review_files": clips,
           "probe": {k: info.get(k) for k in ("duration", "width", "height", "codec", "profile", "pix_fmt", "color", "fps",
                                              "frames", "total_kbps", "video_kbps", "audio_line")}}
    (out / "check_report.json").write_text(json.dumps(rep, indent=1), encoding="utf-8")
    print("%s: %d checks, %d failed%s -> %s" % ("OK" if not failed else "NOT READY", len(RESULTS), len(failed),
                                               (" (" + ", ".join(failed) + ")") if failed else "",
                                               (out / "check_report.json").as_posix()))
    return 0 if not failed else 1


if __name__ == "__main__":
    sys.exit(main())
