"""Room-tone bed for the trailer (review of cut 1: a third of the timeline sat at digital zero, which sounds like broken
audio to a viewer who hovers the festival tile with sound on; TRAILER.md 7 asks for "the quiet of the room").

    python Tools/steam_art/trailer/make_roomtone.py [OUT.wav] [--seconds 62] [--seed 7]

Source: the quietest stretches of richwise's "Pressure Washer" (Freesound 580534, CC0; already in Docs/AUDIO_CREDITS.md
and Tools/audio_recordings.py): 107.6-112.6 s and 134.1-135.65 s minus two ticks, the room between the bursts (median
about -59 dBFS); grains with a click are re-drawn. Grains of 1.0-2.0 s are drawn from the stretches and overlap-added with
0.35 s equal-power crossfades (no audible loop), high-passed at 60 Hz (rumble) and levelled to -38 dBFS RMS; the EDL sets the final gain.
Deterministic for a given seed. Like every recording here it is cut from the Freesound PREVIEW: rebuild from the original
before release (AUDIO.md 11.2).
"""
import argparse
import subprocess
import sys
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[3]
SRC = ROOT / "Assets/CarpetCleaning/Audio/Sources/580534__richwise__pressure_washer.ogg"
# the quiet room between the bursts, minus a double tick at 109.3-109.6 and 135.8-135.9 s that repeated like a clock
# (review of cut 2: 19 recurrences in 60 s)
STRETCHES = [(107.6, 109.3), (109.6, 112.6), (134.1, 135.65)]
SR = 48000


def decode(path):
    import imageio_ffmpeg
    raw = subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), "-v", "error", "-i", str(path), "-f", "f32le", "-ac", "2",
                          "-ar", str(SR), "-"], capture_output=True, check=True).stdout
    return np.frombuffer(raw, np.float32).reshape(-1, 2).astype(np.float64)


def highpass(x, fc=60.0):  # noqa: D401
    # 2nd-order Butterworth biquad, per channel (RBJ cookbook)
    w0 = 2 * np.pi * fc / SR; q = 0.7071
    al = np.sin(w0) / (2 * q); c = np.cos(w0)
    b = np.array([(1 + c) / 2, -(1 + c), (1 + c) / 2]); a = np.array([1 + al, -2 * c, 1 - al])
    b /= a[0]; a /= a[0]
    try:
        from scipy.signal import lfilter
        return lfilter(b, a, x, axis=0)
    except ImportError:
        y = np.zeros_like(x); z1 = np.zeros(x.shape[1]); z2 = np.zeros(x.shape[1])
        for n in range(x.shape[0]):
            xn = x[n]; yn = b[0] * xn + z1
            z1 = b[1] * xn - a[1] * yn + z2; z2 = b[2] * xn - a[2] * yn; y[n] = yn
        return y


def transient_db(seg):
    """Peak of the 5 ms RMS of a 1 kHz high-passed grain over its median, dB: a tick stands out, the room does not."""
    y = highpass(seg, 1000.0).mean(axis=1)
    w = int(0.005 * SR)
    e = np.sqrt(np.convolve(y * y, np.ones(w) / w, mode="valid") + 1e-20)
    return 20 * np.log10(e.max() / np.median(e))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("out", nargs="?", default=str(ROOT / "Tools/steam_art/trailer/audio/roomtone_580534.wav"))
    ap.add_argument("--seconds", type=float, default=62.0)
    ap.add_argument("--seed", type=int, default=7)
    ap.add_argument("--rms-db", type=float, default=-38.0)
    a = ap.parse_args()
    x = decode(SRC)
    pools = [x[int(s * SR):int(e * SR)] for s, e in STRETCHES]
    rng = np.random.default_rng(a.seed)
    n = int(a.seconds * SR); xf = int(0.35 * SR)
    out = np.zeros((n + 3 * SR, 2)); pos = 0; tries = 0
    while pos < n:
        p = pools[int(rng.integers(len(pools)))]
        g = int(rng.uniform(1.0, min(2.0, p.shape[0] / SR - 0.05)) * SR)   # a grain never longer than its stretch
        i0 = int(rng.integers(0, p.shape[0] - g))
        seg = p[i0:i0 + g].copy()
        if transient_db(seg) > 12.0 and tries < 50:        # a grain with a click (5 ms high-passed peak > 12 dB over its median)
            tries += 1
            continue
        tries = 0
        if rng.random() < 0.5:
            seg = seg[:, ::-1]                                 # swap L/R now and then: more variety
        t = np.linspace(0, np.pi / 2, xf)
        seg[:xf] *= np.sin(t)[:, None]; seg[-xf:] *= np.cos(t)[:, None]      # equal-power fades
        out[pos:pos + g] += seg
        pos += g - xf
    y = highpass(out[:n])
    rms = np.sqrt((y ** 2).mean())
    y *= 10 ** (a.rms_db / 20) / max(rms, 1e-12)
    fi = int(0.5 * SR)
    y[:fi] *= np.linspace(0, 1, fi)[:, None]; y[-fi:] *= np.linspace(1, 0, fi)[:, None]
    Path(a.out).parent.mkdir(parents=True, exist_ok=True)
    import wave
    pcm = (np.clip(y, -1, 1) * 32767).astype("<i2")
    with wave.open(a.out, "wb") as w:
        w.setnchannels(2); w.setsampwidth(2); w.setframerate(SR); w.writeframes(pcm.tobytes())
    print(a.out, "%.1f s" % (n / SR), "rms %.1f dBFS" % (20 * np.log10(np.sqrt((y ** 2).mean()))),
          "peak %.1f dBFS" % (20 * np.log10(np.abs(y).max())))


if __name__ == "__main__":
    sys.exit(main())
