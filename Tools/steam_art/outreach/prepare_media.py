"""Build a self-contained creator media folder from the project's licensed takes.

Run from the project root with the existing Python 3.12 media environment.
Original masters, EDLs and captured frames are never overwritten.
"""
import argparse
import json
import shutil
import subprocess
import sys
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "Tools/steam_art"))
sys.path.insert(0, str(ROOT / "Tools/steam_art/trailer"))
from clip_probe import ffmpeg_exe, probe_media
import foley

OUT = ROOT / "SteamStore/outreach/2026-10-18"
WORK = ROOT / "SteamStore/_work/outreach_20261018"
PUBLIC = OUT / "PUBLIC_REFLUFF_CREATOR_KIT"
FF = ffmpeg_exe()


def run(args):
    p = subprocess.run([FF, "-hide_banner", "-nostats", "-y"] + list(map(str, args)),
                       capture_output=True, text=True, errors="replace")
    if p.returncode:
        raise RuntimeError(p.stderr[-4000:])
    return p.stderr


def video_args():
    return ["-c:v", "libx264", "-preset", "fast", "-crf", "18", "-pix_fmt", "yuv420p",
            "-color_primaries", "bt709", "-color_trc", "bt709", "-colorspace", "bt709",
            "-color_range", "tv", "-r", "60", "-g", "120", "-movflags", "+faststart"]


def export_take(name, start, duration, dest, portrait=False):
    """1x speed and one frame per logged 60 Hz Step; no invented in-game audio."""
    take = ROOT / "SteamStore/_work/clips" / name
    seq = json.loads((take / "seq.json").read_text(encoding="utf-8"))
    first = round(start * 60) + 1
    count = round(duration * 60)
    assert first + count - 1 <= seq["frames"], name
    assert seq["fps"] == seq["stepFps"] == 60 and not seq["errors"], name
    # The first image is Step 1, whose logged interval begins at take time zero.
    audio = foley.render_take(str(take), start, start + duration, warm=True)
    assert np.isfinite(audio).all()
    wav = WORK / (dest.stem + ".wav")
    foley.write_wav(str(wav), audio)
    size = "1080:1920" if portrait else "1920:1080"
    filt = f"scale={size}:force_original_aspect_ratio=increase,crop={size},setsar=1"
    run(["-framerate", "60", "-start_number", first, "-i", take / "f%04d.png", "-i", wav,
         "-map", "0:v:0", "-map", "1:a:0", "-frames:v", count, "-t", duration,
         "-vf", filt, *video_args(), "-af",
         "afade=t=in:d=0.015,afade=t=out:st=%.6f:d=0.025,loudnorm=I=-16:TP=-2:LRA=11" % (duration-.025),
         "-c:a", "aac", "-ar", "48000", "-ac", "2", "-b:a", "192k", dest])
    print("clip", dest.name, flush=True)
    return {"file": str(dest.relative_to(PUBLIC)).replace("\\", "/"), "take": name,
            "start": start, "duration": duration, "speed": 1, "portrait": portrait,
            "camera": "cinematic macro" if portrait else "fixed game camera, cropped to 16:9",
            "audio": "game foley reconstructed from recorded Step audio state; no voice/music"}


def update_trailer():
    """Replace only the dated last shot, retain the existing 60 s audio mix."""
    import make_trailer as mt
    edl = mt.Edl(ROOT / "Tools/steam_art/trailer/edl.json", 60)
    shot = edl.shots[-1]
    assert shot.id == "21" and shot.k0 == 3280
    nframes = 3600 - shot.k0
    tail = WORK / "trailer_november_tail.mp4"
    cmd = [FF, "-v", "error", "-y", "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", "1920x1080",
           "-r", "60", "-i", "-", "-frames:v", str(nframes), "-an", *video_args(), str(tail)]
    proc = subprocess.Popen(cmd, stdin=subprocess.PIPE, stderr=subprocess.PIPE)
    try:
        # Chunked mapping bounds the number of 1080p raw frames held in RAM.
        with ThreadPoolExecutor(max_workers=4) as pool:
            for base in range(shot.k0, 3600, 8):
                frames = pool.map(lambda k: mt.render_frame(edl, k, 1920, 1080, date="November 2026"),
                                  range(base, min(base+8, 3600)))
                for frame in frames:
                    proc.stdin.write(frame.tobytes())
        proc.stdin.close()
        err = proc.stderr.read().decode("utf-8", "replace")
        if proc.wait():
            raise RuntimeError(err)
    finally:
        if proc.poll() is None:
            proc.kill()
    master = ROOT / "SteamStore/trailer/refluff_trailer_main.mp4"
    dest = PUBLIC / "01_START_HERE/refluff_trailer_60s_november_2026.mp4"
    run(["-i", master, "-i", tail, "-filter_complex",
         "[0:v]trim=end_frame=3280,setpts=PTS-STARTPTS[a];[1:v]setpts=PTS-STARTPTS[b];[a][b]concat=n=2:v=1:a=0[v]",
         "-map", "[v]", "-map", "0:a:0", *video_args(), "-c:a", "copy", "-t", "60", dest])
    run(["-ss", "58", "-i", dest, "-frames:v", "1", WORK / "trailer_november_check.png"])
    print("trailer end card updated (outreach copy only)", flush=True)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--skip-trailer", action="store_true")
    a = ap.parse_args()
    if not FF:
        raise SystemExit("Use Python 3.12 with the project's imageio-ffmpeg package.")
    for folder in ["01_START_HERE", "02_GAMEPLAY_BROLL", "03_SCREENSHOTS", "04_LOGOS"]:
        (PUBLIC / folder).mkdir(parents=True, exist_ok=True)
    WORK.mkdir(parents=True, exist_ok=True)
    provenance = []
    landscape = [("tr07b_vacuum", 0.1, 3.5, "01_dry_vacuum_3p5s"),
                 ("tr09_foam", 0.0, 3.5, "02_foam_3p5s"),
                 ("tr10_scrub", 0.0, 3.5, "03_rotary_brush_3p5s"),
                 ("tr11_squeegee", 0.0, 4.0, "04_squeegee_4s"),
                 ("tr12_extract", 0.0, 4.5, "05_wet_vacuum_reveal_4p5s")]
    for take, t0, dur, name in landscape:
        dst = PUBLIC / "02_GAMEPLAY_BROLL" / (name + "_1920x1080.mp4")
        if not dst.exists():
            provenance.append(export_take(take, t0, dur, dst))
    portrait = [("tt2_vacuum", .3, 3.0), ("tt3_hose", .2, 3.4), ("tt4_brush", .55, 3.0),
                ("tt5_squeegee", .3, 3.6), ("tt6_dryer", .2, 4.0), ("tt7_reveal", .1, 3.0)]
    parts = []
    for i, (take, t0, dur) in enumerate(portrait):
        temp = WORK / f"portrait_part_{i}.mp4"
        if not temp.exists():
            rec = export_take(take, t0, dur, PUBLIC / "02_GAMEPLAY_BROLL" / f"_part_{i}.mp4", True)
            source = PUBLIC / rec["file"]
            shutil.move(source, temp)
        parts.append(temp)
        provenance.append({"montagePart": i, "take": take, "start": t0, "duration": dur,
                           "speed": 1, "camera": "cinematic macro, 48 shells / staged light"})
    concat = WORK / "portrait_concat.txt"
    concat.write_text("\n".join("file '"+str(p).replace("\\", "/")+"'" for p in parts), encoding="utf-8")
    # Join decoded audio then normalize once, avoiding AAC encoder padding at cuts.
    inputs = [item for p in parts for item in ("-i", p)]
    av = "".join(f"[{i}:v:0][{i}:a:0]" for i in range(len(parts)))
    run([*inputs, "-filter_complex", av+f"concat=n={len(parts)}:v=1:a=1[v][a];[a]loudnorm=I=-16:TP=-2:LRA=11[na]",
         "-map", "[v]", "-map", "[na]", *video_args(), "-c:a", "aac", "-b:a", "192k", "-ar", "48000",
         "-t", "20", PUBLIC / "01_START_HERE/refluff_clean_edit_20s_vertical.mp4"])
    shutil.copy2(ROOT / "SteamStore/tiktok/refluff_tiktok_fibers.mp4",
                 PUBLIC / "01_START_HERE/refluff_captioned_25p7s_vertical.mp4")
    if not a.skip_trailer:
        update_trailer()
    for name in ["01_hero_reveal.jpg", "02_scraper_crust.jpg", "04_rya_half_clean.jpg", "05_hose_spill.jpg"]:
        shutil.copy2(ROOT / "SteamStore/screenshots" / name, PUBLIC / "03_SCREENSHOTS" / name)
    for name in ["refluff_logo.png", "refluff_wordmark.png", "refluff_mark_1024.png"]:
        shutil.copy2(ROOT / "SteamStore/logo" / name, PUBLIC / "04_LOGOS" / name)
    credits = (ROOT / "Assets/CarpetCleaning/Docs/AUDIO_CREDITS.md").read_text(encoding="utf-8")
    block = credits.split("```", 2)[1].strip()
    (PUBLIC / "AUDIO_CREDITS.txt").write_text(block+"\n", encoding="utf-8")
    (WORK / "media_provenance.json").write_text(json.dumps(provenance, ensure_ascii=False, indent=2), encoding="utf-8")
    # Review samples include every distributed video, including all cuts in the montage.
    thumbs = []
    for path in sorted(PUBLIC.rglob("*.mp4")):
        info = probe_media(path)
        for t in ([1, 4, 8, 11, 15, 18] if "clean_edit" in path.name else [min(2, info["duration"]/2)]):
            img = WORK / (path.stem+"_"+str(t)+".jpg")
            run(["-ss", t, "-i", path, "-frames:v", "1", "-vf", "scale=480:270:force_original_aspect_ratio=decrease", img])
            thumbs.append((path.name+" @"+str(t), img))
    rows = (len(thumbs)+2)//3
    sheet = Image.new("RGB", (1440, rows*310), "#202024")
    draw = ImageDraw.Draw(sheet)
    for i, (name, img) in enumerate(thumbs):
        x, y = (i%3)*480, (i//3)*310
        sheet.paste(Image.open(img), (x, y+32))
        draw.text((x+5, y+5), name, fill="white")
    sheet.save(WORK / "public_video_review.jpg", quality=90)
    print("Media ready:", PUBLIC, flush=True)


if __name__ == "__main__":
    main()
