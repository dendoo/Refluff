"""Align the supplied read to the scraper-upgrades TikTok, without retiming speech.

    python Tools/steam_art/tiktok/scraper_upgrades/align_voice.py [--scan]

The immutable MP3 copy is the input; vo_aligned.wav, timing.json and subtitles.srt
are regenerated in SteamStore/tiktok/scraper_upgrades/. Source bounds were checked
with a 10 ms RMS gate and local faster-whisper base.en (the latter is only an
audit aid, and is not required to rebuild). The three scrape seconds after
"Max size" are intentional. No source file is deleted or overwritten.

Caption layout belongs to the hand-authored edl.json, not this audio-only script.
The weak-shot speech is at y 0.30 / 0.352 to clear the captured scraper action;
LEVEL 1 stays at y 0.17. Rebuilding the audio preserves those EDL positions.
"""
import argparse
import hashlib
import json
import sys
import wave
from pathlib import Path

import numpy as np

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
sys.path.insert(0, str(HERE.parent.parent / "trailer"))
import make_trailer as M  # noqa: E402

OUT = ROOT / "SteamStore/tiktok/scraper_upgrades"
SOURCE = OUT / "voice_source.mp3"
SR = 48000
DURATION = 26.8
PRE, POST = 0.06, 0.14
# (source first/last audible edge, destination first audible edge, transcript)
PIECES = [
    (0.07, 2.84, 0.35, "Oh, come on. Is there even a carpet under all that?"),
    (3.49, 5.50, 5.50, "At this rate, I'll be scraping all week."),
    (6.68, 7.35, 10.70, "Wait..."),
    (7.81, 9.24, 11.80, "I can upgrade my scraper."),
    (10.20, 11.23, 14.00, "Max power."),
    (11.82, 12.89, 15.42, "Max size."),
    (13.69, 16.31, 19.80, "Okay... that might be overkill."),
    (17.00, 17.73, 23.00, "Refluff."),
    (17.99, 19.28, 24.00, "Wishlist it on Steam."),
]
SHOTS = [
    ("scu_arrival", 0.0, 5.0),
    ("scu_weak", 5.0, 5.5),
    ("scu_shop", 10.5, 6.0),
    ("scu_power", 16.5, 3.2),
    ("scu_reveal", 19.7, 7.1),
]


def scan(x):
    w = SR // 100
    y = x[:len(x) // w * w].mean(axis=1).reshape(-1, w)
    on = 20 * np.log10(np.sqrt(np.mean(y * y, axis=1)) + 1e-9) > -38
    a, off = None, 0
    for i, v in enumerate(on):
        if v and a is None:
            a = i
        if a is not None:
            off = 0 if v else off + 1
            if off >= 18:
                print("%.2f - %.2f" % (a / 100, (i - off + 1) / 100))
                a, off = None, 0
    if a is not None:
        print("%.2f - %.2f" % (a / 100, len(on) / 100))


def srt_time(t):
    ms = round(t * 1000)
    return "%02d:%02d:%02d,%03d" % (ms // 3600000, ms // 60000 % 60, ms // 1000 % 60, ms % 1000)


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--scan", action="store_true")
    args = ap.parse_args()
    x = M.decode_audio(str(SOURCE), SR, cache=False)
    if args.scan:
        scan(x)
        return
    OUT.mkdir(parents=True, exist_ok=True)
    y = np.zeros((round(DURATION * SR), 2), np.float32)
    timing = []
    last_written = 0
    for a, b, d, text in PIECES:
        i0, i1 = round(max(0, a - PRE) * SR), min(len(x), round((b + POST) * SR))
        o = round((d - (a - i0 / SR)) * SR)
        if o < last_written or o < 0 or o + i1 - i0 > len(y):
            raise ValueError("piece overlap or out of range: " + text)
        seg = x[i0:i1] * M.ramp(i1 - i0, round(0.008 * SR), round(0.025 * SR))[:, None]
        y[o:o + len(seg)] += seg
        last_written = o + len(seg)
        timing.append({"text": text, "source": [a, b], "start": d,
                       "end": round(d + b - a, 3),
                       "padded_destination": [round(o / SR, 3), round(last_written / SR, 3)]})
    pcm = (np.clip(y, -1, 1) * 32767).astype("<i2")
    wav = OUT / "vo_aligned.wav"
    with wave.open(str(wav), "wb") as f:
        f.setnchannels(2)
        f.setsampwidth(2)
        f.setframerate(SR)
        f.writeframes(pcm.tobytes())
    report = {"source": SOURCE.relative_to(ROOT).as_posix(),
              "source_sha256": hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
              "source_seconds": len(x) / SR, "duration": DURATION,
              "sample_rate": SR, "speech_retime": 1.0,
              "scrape_without_speech": [16.49, 19.80],
              "shots": [{"take": t, "start": a, "dur": b} for t, a, b in SHOTS],
              "phrases": timing}
    (OUT / "timing.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    srt = []
    for i, p in enumerate(timing, 1):
        srt.extend([str(i), srt_time(p["start"]) + " --> " + srt_time(p["end"]), p["text"], ""])
    (OUT / "subtitles.srt").write_text("\n".join(srt), encoding="utf-8")
    (OUT / "voiceover_en.txt").write_text("\n\n".join(p[3] for p in PIECES) + "\n", encoding="utf-8")
    print(json.dumps({"wav": wav.relative_to(ROOT).as_posix(), "seconds": len(y) / SR,
                      "last_word": timing[-1]["end"], "peak_dbfs": float(20 * np.log10(np.max(np.abs(y)) + 1e-9))}))


if __name__ == "__main__":
    main()
