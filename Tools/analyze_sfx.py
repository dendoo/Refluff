"""Objective 'does it sound synthetic' check for the tool clips (Docs/AUDIO.md 3).

Usage:  python Tools/analyze_sfx.py Assets/CarpetCleaning/Audio/Generated
        python Tools/analyze_sfx.py <folder of rattle clips> --hits     (one-shot impacts: onset, strikes, centroid)
Needs numpy + scipy. Works on any folder of mono/stereo 16-bit WAVs, so run it on your own recordings too:
a real loop should land near the white-noise reference (st90 ~12, am ~5) unless it is a machine whine.
Calibration: white noise = tonal 2.7 / am 4.9 / st90 12.4; a 600 Hz tone in noise = tonal 52 / st90 46.
Ignore am_dB on one-shots: a decaying hit is amplitude modulation by definition.

tonal_db : strongest narrow spectral peak above its local (1/3-octave median) floor. Real friction, air
           and water are broadband: a peak above ~12 dB reads as a whistle / bleep / sci-fi tone.
peaks12  : how many such peaks there are (a harmonic stack = a synth chord).
flat     : spectral flatness 100 Hz-10 kHz on log-spaced bands (1 = white-ish, 0 = pure tone).
am_db    : strongest amplitude modulation 0.5-20 Hz above the modulation-spectrum median.
           A clean LFO wobble (vibrato/tremolo) shows up here and is the classic 'UFO' tell. Slow RANDOM
           drift also scores at the lowest bin (0.8 Hz); tell them apart with the envelope autocorrelation:
           a periodic wobble has a clear peak at its period (old dryer: 0.64 at 0.41 s), random drift not.
st90     : 90th percentile of per-frame (23 ms) tonality: catches sine 'grains' that hide in the average.
"""
import sys, glob, os
import numpy as np
from scipy.io import wavfile
from scipy.signal import welch
from scipy.ndimage import median_filter

def analyse(path):
    sr, x = wavfile.read(path)
    x = x.astype(np.float64) / 32768.0
    if x.ndim > 1: x = x.mean(axis=1)
    f, p = welch(x, sr, nperseg=min(8192, len(x)))
    band = (f >= 100) & (f <= 10000)
    fb, pb = f[band], p[band] + 1e-20
    # local floor: median over a window that is ~1/3 octave wide at 1 kHz (bins are 5.4 Hz)
    floor = median_filter(pb, size=61, mode='nearest')
    ratio = 10 * np.log10(pb / floor)
    tonal = ratio.max(); tonal_f = fb[ratio.argmax()]
    # count distinct peaks above 12 dB
    above = ratio > 12
    peaks = int(np.sum(above[1:] & ~above[:-1]))
    # flatness on 1/6-octave bands so the high end does not dominate
    edges = 100 * 2 ** (np.arange(0, 6.65, 1 / 6))
    bands = [pb[(fb >= a) & (fb < b)].mean() for a, b in zip(edges[:-1], edges[1:]) if np.any((fb >= a) & (fb < b))]
    bands = np.array(bands)
    flat = np.exp(np.mean(np.log(bands))) / np.mean(bands)
    # envelope modulation spectrum
    hop = int(sr * 0.005)
    env = np.sqrt(np.convolve(x * x, np.ones(hop) / hop, mode='same'))[::hop]
    env = env - env.mean()
    if len(env) > 64:
        fm, pm = welch(env, 1.0 / 0.005, nperseg=min(len(env), 256))
        sel = (fm >= 0.5) & (fm <= 20)
        pm = pm[sel] + 1e-20
        am = 10 * np.log10(pm.max() / np.median(pm)); am_f = fm[sel][pm.argmax()]
    else:
        am, am_f = 0.0, 0.0
    # short-time tonality: 23 ms frames, strongest peak over the frame's own smoothed spectrum.
    # Sine 'grains' hide in the long-term average but every frame that contains one is a bleep.
    from scipy.signal import stft
    _, _, Z = stft(x, sr, nperseg=1024, noverlap=512)
    P = np.abs(Z) ** 2 + 1e-20
    fr = np.fft.rfftfreq(1024, 1 / sr)
    sel = (fr >= 150) & (fr <= 9000)
    P = P[sel]
    fl = median_filter(P, size=(15, 1), mode='nearest')
    st = 10 * np.log10((P / fl).max(axis=0))
    energy = P.sum(axis=0)
    st = st[energy > np.percentile(energy, 20)]
    st90 = np.percentile(st, 90) if len(st) else 0.0
    return tonal, tonal_f, peaks, flat, am, am_f, st90

def hits(fp):
    """One-shot impact clips (tube rattle): onset, strike times and levels, spectral centroid of the first 20 ms."""
    from scipy.signal import find_peaks
    sr, x = wavfile.read(fp)
    x = x.astype(np.float64) / 32768.0
    if x.ndim > 1: x = x.mean(axis=1)
    env = np.sqrt(np.convolve(x * x, np.ones(44) / 44, 'same'))
    onset = np.argmax(np.abs(x) > 0.1 * np.abs(x).max()) / sr * 1000
    pk, _ = find_peaks(env, height=env.max() * 0.1, distance=int(0.004 * sr))
    levels = [int(round(20 * np.log10(env[p] / env.max()))) for p in pk]
    seg = x[:int(0.02 * sr)]
    S = np.abs(np.fft.rfft(seg)); fr = np.fft.rfftfreq(len(seg), 1 / sr)
    cent = float(np.sum(S * fr) / np.sum(S))
    flag = '  <-- first strike not at the start: shifts the sync by this much' if onset > 3 else ''
    print(f"{os.path.basename(fp)[:-4]:32s} onset={onset:5.1f}ms hits={len(pk):2d} at_ms={[int(round(p / sr * 1000)) for p in pk]} dB={levels} centroid={cent:5.0f}Hz{flag}")

if len(sys.argv) > 2 and sys.argv[2] == '--hits':
    # Rattle clips are scheduled so their FIRST strike lands when the crumb enters the nozzle (Docs/AUDIO.md 8):
    # onset must be ~0-3 ms. The shipped rattle sits at a 5-9 kHz centroid (raised on request); a broadband noise click instead of ringing modes reads as a hiss.
    for fp in sorted(glob.glob(os.path.join(sys.argv[1], '*.wav'))): hits(fp)
    sys.exit()

files = sorted(glob.glob(os.path.join(sys.argv[1], '*.wav')))
print(f"{'clip':32s} {'tonal_dB':>8s} {'@Hz':>6s} {'peaks12':>7s} {'flat':>5s} {'am_dB':>6s} {'@Hz':>5s} {'st90':>5s}")
for fp in files:
    t, tf, n, fl, am, amf, st90 = analyse(fp)
    flag = '  <-- tonal' if t > 15 else ''
    flag += '  <-- wobble' if am > 12 else ''
    flag += '  <-- bleepy' if st90 > 16 else ''
    print(f"{os.path.basename(fp)[:-4]:32s} {t:8.1f} {tf:6.0f} {n:7d} {fl:5.2f} {am:6.1f} {amf:5.1f} {st90:5.1f}{flag}")
