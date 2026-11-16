"""Lays the user's voiceover (one continuous TTS read) onto the TikTok cut: every phrase is cut out at its pauses and
placed at its shot (Docs/TIKTOK.md 5). Writes a 48 kHz stereo float WAV that the EDL's "voice" slot plays from 0.

    python Tools/steam_art/tiktok/align_voice.py [SRC] [OUT]

SRC = SteamStore/tiktok/voice.mp3, OUT = SteamStore/tiktok/voice/vo_aligned.wav. PIECES = (src_from, src_to, dst) in
seconds: the speech edges found by a 10 ms RMS gate at -40 dBFS (prints them again with --scan); each piece gets PRE s
before and POST s after its edges, with short fades, so dst is where the first word starts."""
import sys
import wave
from pathlib import Path

import numpy as np

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(HERE.parent / "trailer"))
import make_trailer as M  # noqa: E402

SR = 48000
PRE, POST = 0.06, 0.14
# shot starts (edl_tiktok.json): 01 0.0, 02 3.3, 03 7.1, 04 10.5, 05 13.5, 06 17.1, 07 21.1 (logo 24.1, CTA 24.55)
PIECES = [
    (0.00, 3.65, 0.10),    # Look, I made a game where every fiber... is simulated.   (runs 0.45 s into the vacuum)
    (4.18, 5.04, 3.90),    # Vacuum it,
    (5.62, 7.70, 5.06),    # and the pile lays down.   (the read's 0.58 s pause cut to 0.30 s)
    (8.11, 10.59, 7.35),   # Water lifts the dirt... and turns brown.
    (11.11, 12.46, 10.75),  # Foam. Scrub.
    (12.91, 14.83, 13.75),  # Then squeeze the mud right off the carpet.
    (15.35, 17.72, 17.40),  # Dry it, and every fiber springs back up.
    (18.21, 18.78, 24.10),  # Refluff.   (on the logo)
    (19.46, 20.82, 24.95),  # Wishlist it on Steam.   (after the CTA pops)
]


def scan(x):
    w = SR // 100
    m = len(x) // w
    r = 20 * np.log10(np.sqrt((x[:m * w].reshape(m, w) ** 2).mean(1)) + 1e-9)
    on, i = r > -40, 0
    while i < m:
        if on[i]:
            j = i
            while j < m and (on[j] or on[j:j + 25].any()):
                j += 1
            print("%.2f - %.2f" % (i / 100, j / 100))
            i = j
        else:
            i += 1


def main(argv):
    args = [a for a in argv if not a.startswith("--")]
    src = Path(args[0]) if args else ROOT / "SteamStore" / "tiktok" / "voice.mp3"
    out = Path(args[1]) if len(args) > 1 else ROOT / "SteamStore" / "tiktok" / "voice" / "vo_aligned.wav"
    x = M.decode_audio(str(src), SR, cache=False)
    if "--scan" in argv:
        scan(x.mean(axis=1))
        return 0
    end = max(d + (b - a) + POST for a, b, d in PIECES)
    y = np.zeros((int(end * SR) + SR, 2), np.float32)
    last = 0.0
    for a, b, d in PIECES:
        i0, i1 = int(max(0.0, a - PRE) * SR), int(min(len(x) / SR, b + POST) * SR)
        seg = x[i0:i1] * M.ramp(i1 - i0, int(0.01 * SR), int(0.04 * SR))[:, None]
        o = int((d - (a - max(0.0, a - PRE))) * SR)
        if d < last + 0.1:
            raise SystemExit("piece at %.2f s starts too close to the previous phrase (ends %.2f s)" % (d, last))
        y[o:o + seg.shape[0]] += seg
        last = d + (b - a)
    out.parent.mkdir(parents=True, exist_ok=True)
    pcm = (np.clip(y, -1, 1) * 32767).astype("<i2")
    with wave.open(str(out), "wb") as f:
        f.setnchannels(2)
        f.setsampwidth(2)
        f.setframerate(SR)
        f.writeframes(pcm.tobytes())
    print("%s  %.2f s, last word ends %.2f s" % (out.as_posix(), len(y) / SR, end - POST))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
