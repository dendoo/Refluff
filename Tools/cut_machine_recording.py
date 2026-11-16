"""Cut one real machine recording (switch-on, running, switch-off) into the three clips ToolAudio's recorded
machine plays (Docs/AUDIO.md 9), and measure how fast the motor turns through both transitions.

    python Tools/cut_machine_recording.py <source.wav> --scan
        level and the tracked motor line every 0.1 s: pick the cut points from this
    python Tools/cut_machine_recording.py <source.wav> --out <dir> --name <name>
        --start T --loop L0 L1 --stop S0 [S1] [--rate 48000] [--xfade 0.08] [--match <reference.wav>]
        [--speed line|envelope] [--env-smooth 0.05] [--line HZ] [--from-rest] [--hp 40] [--duck T0 T1 dB ...] [--credit "<title> by <author>, <url>, <licence>"]

Writes <name>_start.wav, <name>_loop.wav, <name>_stop.wav (16-bit PCM, dithered, the source's channels) and
<name>.json, which Editor/ToolSoundBaker.ImportMachineRecording puts into the bank.

- start = source[T, L0]: the spin-up. Faded in over 5 ms, NOT faded out: its last sample runs straight on into
  the loop's first, and ToolAudio stitches the two with PlayScheduled, sample-exact.
- loop  = source[L0, L1']: L1' is L1 moved (+-20 ms) to where the tail best matches the audio just before L0,
  and the tail is crossfaded into that audio. So the wrap continues into loop[0] = source[L0] exactly as the
  start clip does, and the head stays untouched. The crossfade gain is corrected for the correlation of the two
  stretches (equal power for unrelated noise, equal gain for a coherent whine), so the level does not dip.
- stop  = source[S0, S1]: the coast-down, faded out over its last 0.3 s.
- One gain for all three (--match: the K-weighted loudness of the loop = that of the reference clip, e.g. the
  placeholder it replaces, so the mix balance and the bank's motorVolume stay right), limited to -1 dBFS peak.

--speed envelope (sprayers, valves, any take without a steady tonal line): "speed" is the level instead, the
smoothed RMS against the loop's (monotonic, 1 = the loop), so a release during the opening of a spray valve
enters the closing where it is as loud as it was. The source may be any format soundfile reads (a Freesound
OGG preview), and --hp high-passes everything (2nd-order Butterworth) to take out handling rumble. --duck turns
[T0, T1] down by dB (3 ms ramps): a switch click some uploader boosted to 0 dBFS would otherwise cap the gain of
the whole take.

Motor speed = frequency of the strongest steady tonal line of the running motor (a vacuum's impeller
blade-pass whine), relative to the loop's. It is found in the loop, tracked backwards through the spin-up and
forwards through the coast-down (each step searches just below the previous frequency), smoothed, made
monotonic, and extrapolated exponentially where the line sinks into the noise late in the coast-down.
Needs numpy + scipy.
"""
import argparse, json, os, sys
from math import gcd
import numpy as np
from scipy.io import wavfile
from scipy.signal import resample_poly, welch, sosfilt
from scipy.ndimage import median_filter


def load(path):
    if not path.lower().endswith('.wav'):
        import soundfile                                     # OGG / FLAC / MP3 (Freesound previews): pip install soundfile
        x, sr = soundfile.read(path, always_2d=True, dtype='float64')
        return sr, x
    sr, x = wavfile.read(path)
    if x.dtype == np.int16: x = x / 32768.0
    elif x.dtype == np.int32: x = x / 2147483648.0          # 24-bit WAVs come back left-aligned in int32
    elif x.dtype == np.uint8: x = (x.astype(np.float64) - 128.0) / 128.0
    else: x = x.astype(np.float64)
    if x.ndim == 1: x = x[:, None]
    return sr, x.astype(np.float64)


def resample(x, sr, rate):
    if sr == rate: return x
    g = gcd(sr, rate)
    return resample_poly(x, rate // g, sr // g, axis=0)


def db(v): return 20.0 * np.log10(max(float(v), 1e-12))


def duck(y, sr, spans):
    """[T0, T1, dB] spans turned down by dB with 3 ms raised-cosine ramps (a boosted click, a knock)."""
    y = y.copy()
    r = int(0.003 * sr)
    ramp = 0.5 - 0.5 * np.cos(np.pi * (np.arange(r) + 0.5) / r)          # 0 -> 1
    for t0, t1, d in spans or []:
        k = 10 ** (-abs(d) / 20)
        a, b = int(t0 * sr), min(len(y), int(t1 * sr))
        g = np.ones(len(y))
        g[a:b] = k
        a0, b1 = max(0, a - r), min(len(y), b + r)
        g[a0:a] = 1 - (1 - k) * ramp[r - (a - a0):]
        g[b:b1] = k + (1 - k) * ramp[:b1 - b]
        y *= g[:, None]
    return y


# ---------------------------------------------------------------------------------------------- the motor line
def find_line(mono, sr, lo=300.0, hi=12000.0):
    """Strongest narrow peak over its local (~200 Hz) median floor in the running motor: (Hz, prominence dB)."""
    f, p = welch(mono, sr, nperseg=min(len(mono), 32768))
    band = (f >= lo) & (f <= hi)
    fb, pb = f[band], p[band] + 1e-30
    width = int(round(200.0 / (f[1] - f[0]))) | 1
    r = 10 * np.log10(pb / median_filter(pb, size=width, mode='nearest'))
    i = int(r.argmax())
    d = 0.0
    if 0 < i < len(pb) - 1:
        a, b, c = np.log(pb[i - 1]), np.log(pb[i]), np.log(pb[i + 1])
        d = 0.5 * (a - c) / (a - 2 * b + c)
    return float(fb[i] + d * (f[1] - f[0])), float(r[i])


def prominence_map(mono, sr, times, grid, npers=(1024, 2048, 4096), nfft=16384):
    """Frames x log-frequency grid: how far each cell stands over its local (+-8 %) median, in dB, best of three
    window lengths -- a 21 ms window keeps a line sweeping 10 %/10 ms sharp at the start of a spin-up, an 85 ms one
    resolves a slow line deep in a coast-down."""
    fr = np.fft.rfftfreq(nfft, 1.0 / sr)
    P = np.full((len(times), len(grid)), -99.0)
    for nper in npers:
        win = np.hanning(nper)
        for i, t in enumerate(times):
            a = int(round(t * sr)) - nper // 2
            seg = mono[max(0, a):max(0, a + nper)]
            if a < 0: seg = np.concatenate([np.zeros(-a), seg])
            if len(seg) < nper: seg = np.concatenate([seg, np.zeros(nper - len(seg))])
            Sg = np.interp(grid, fr, np.abs(np.fft.rfft(seg * win, nfft)) ** 2) + 1e-30
            P[i] = np.maximum(P[i], 10 * np.log10(Sg / median_filter(Sg, size=41, mode='nearest')))
    return P


def track(mono, sr, t_from, t_to, f_ref, hop=0.01, grid_step=0.004, max_step=0.15):
    """The motor line from t_from to t_to, walked AWAY from the running motor: backwards through a spin-up
    (t_from = where it runs, t_to = the switch-on) or forwards through a coast-down. Either way the line only falls
    along the walk, by at most max_step per frame, and it starts at the running motor's f_ref. Solved as one best
    path over the prominence map (dynamic programming), so a frame where the line is buried is bridged by its
    neighbours instead of derailing a frame-by-frame search. Returns (times ascending, Hz, prominence dB)."""
    step = hop if t_to >= t_from else -hop
    times = np.arange(t_from, t_to + step * 0.5, step)
    grid = f_ref * np.exp(np.arange(np.log(0.04), np.log(1.03), grid_step))
    score = np.clip(prominence_map(mono, sr, times, grid), 0.0, 30.0)
    n, m = score.shape
    down = int(np.ceil(-np.log(1.0 - max_step) / grid_step))      # bins the line may fall per frame
    start = np.abs(np.log(grid / f_ref)) <= 0.01                    # the walk starts on the running line (+-1 %)
    D = np.where(start, score[0], -np.inf)
    back = np.zeros((n, m), np.int16)
    for i in range(1, n):
        best = np.full(m, -np.inf); arg = np.zeros(m, np.int16)
        for d in range(-1, down + 1):                              # previous bin = this bin + d
            sh = np.full(m, -np.inf)
            if d >= 0: sh[:m - d] = D[d:]
            else: sh[1:] = D[:-1]
            better = sh > best
            best[better] = sh[better]; arg[better] = d
        D = best + score[i]
        back[i] = arg
    b = np.empty(n, int); b[-1] = int(np.argmax(D))
    for i in range(n - 1, 0, -1): b[i - 1] = b[i] + back[i, b[i]]
    f, prom = grid[b], score[np.arange(n), b]
    order = np.argsort(times)
    return times[order], f[order], prom[order]


def envelope_speed(mono, sr, t_from, t_to, ref_rms, increasing, smooth=0.05, hop=0.01, win=0.03):
    """Level instead of pitch: RMS in 30 ms windows every 10 ms over [t_from, t_to] (ascending times), relative to
    the running loop's RMS, median-smoothed over `smooth` seconds (0.25 s keeps a switch click out of the curve),
    clipped to 0..1 and made monotonic. Returns (times, speed)."""
    t = np.arange(min(t_from, t_to), max(t_from, t_to) + hop * 0.5, hop)
    w = int(win * sr)
    r = np.empty(len(t))
    for i, tt in enumerate(t):
        a = max(0, int(round(tt * sr)) - w // 2)
        seg = mono[a:a + w]
        r[i] = np.sqrt(np.mean(seg ** 2)) if len(seg) else 0.0
    s = median_filter(r / (ref_rms + 1e-12), size=max(1, int(round(smooth / hop))) | 1, mode='nearest')
    s = isotonic(np.clip(s, 0.0, 1.0), increasing)
    if increasing: s[-1] = 1.0                                  # the start clip runs straight into the loop
    else: s[0] = max(s[0], s[1] if len(s) > 1 else s[0])
    return t, np.clip(s, 0.0, 1.0)


def isotonic(y, increasing=True):
    """Pool-adjacent-violators: the closest monotonic sequence (least squares)."""
    v = list(y if increasing else -np.asarray(y))
    vals, cnts = [], []
    for x in v:
        vals.append(x); cnts.append(1)
        while len(vals) > 1 and vals[-2] > vals[-1]:
            n = cnts[-2] + cnts[-1]
            m = (vals[-2] * cnts[-2] + vals[-1] * cnts[-1]) / n
            vals[-2:] = [m]; cnts[-2:] = [n]
    out = np.repeat(vals, cnts)
    return out if increasing else -out


def speed_curve(t, f, prom, f_line, increasing, from_rest=False):
    """Tracked line -> smoothed, monotonic speed (1 = the loop). Frames where the line is buried in the noise
    (prominence < 8 dB) are dropped; past the last confident frame of a coast-down the speed decays exponentially
    at the rate of its last confident second. from_rest: the spin-up starts at standstill, so before its first
    confident frame (a line sweeping too fast to be seen) the speed ramps up from 0 instead of holding that frame."""
    ok = prom >= 8.0
    good = np.flatnonzero(ok)
    if len(good) == 0: raise SystemExit('the motor line was never found in this stretch')
    if not increasing:
        # late in a coast-down the line sinks into the noise for good: cut where fewer than half of the frames
        # in the next 0.3 s still see it, so a stray noise peak further on cannot hold the curve up
        frac = np.convolve(ok.astype(float), np.ones(31) / 31.0, mode='full')[30:]
        lost = np.flatnonzero((frac < 0.5) & (np.arange(len(t)) > good[0]))
        if len(lost): good = good[good < lost[0]]
    # bridge the buried frames from their confident neighbours (and hold the ends), then drop single-frame jumps
    s = median_filter(np.interp(t, t[good], f[good] / f_line), size=7, mode='nearest')
    if not increasing:
        last = good[-1]
        tail = good[(t[good] >= t[last] - 1.0)]
        if len(tail) >= 5:
            k = -np.polyfit(t[tail], np.log(np.maximum(s[tail], 1e-3)), 1)[0]
        else:
            k = 1.0
        k = max(k, 0.2)
        s[last:] = s[last] * np.exp(-k * (t[last:] - t[last]))
    if increasing and from_rest and t[good[0]] > t[0]:
        s[:good[0]] = s[good[0]] * (t[:good[0]] - t[0]) / (t[good[0]] - t[0])
    s = isotonic(np.clip(s, 0.0, 1.0), increasing)
    return np.clip(s, 0.0, 1.0)


def simplify(t, v, tol=0.004):
    """Ramer-Douglas-Peucker on the curve, so the bank carries tens of keys, not hundreds."""
    keep = np.zeros(len(t), bool); keep[0] = keep[-1] = True
    stack = [(0, len(t) - 1)]
    while stack:
        a, b = stack.pop()
        if b <= a + 1: continue
        tt, vv = t[a + 1:b], v[a + 1:b]
        lin = v[a] + (v[b] - v[a]) * (tt - t[a]) / max(t[b] - t[a], 1e-9)
        i = int(np.argmax(np.abs(vv - lin)))
        if abs(vv[i] - lin[i]) > tol:
            m = a + 1 + i
            keep[m] = True
            stack += [(a, m), (m, b)]
    return t[keep], v[keep]


# ---------------------------------------------------------------------------------------------- loudness
def k_weight(x, sr):
    """ITU-R BS.1770 K-weighting (pre-filter + RLB high-pass), coefficients for 48 kHz."""
    if sr != 48000: raise ValueError('k_weight wants 48 kHz')
    pre = np.array([[1.53512485958697, -2.69169618940638, 1.19839281085285, 1.0, -1.69065929318241, 0.73248077421585]])
    rlb = np.array([[1.0, -2.0, 1.0, 1.0, -1.99004745483398, 0.99007225036621]])
    return sosfilt(rlb, sosfilt(pre, x, axis=0), axis=0)


def loudness(x, sr):
    """Ungated BS.1770 loudness (LKFS) of a steady clip; a mono clip counts as played on both speakers."""
    y = k_weight(resample(x, sr, 48000), 48000)
    ms = (y ** 2).mean(axis=0)
    if len(ms) == 1: ms = np.array([ms[0], ms[0]])
    return -0.691 + 10 * np.log10(ms.sum() + 1e-30)


# ---------------------------------------------------------------------------------------------- output
def write_wav16(path, x, sr, rng):
    """16-bit PCM with TPDF dither."""
    d = (rng.random(x.shape) - rng.random(x.shape)) / 32768.0
    q = np.clip(np.round((x + d) * 32767.0), -32768, 32767).astype(np.int16)
    wavfile.write(path, sr, q if q.shape[1] > 1 else q[:, 0])


def splice_loop(y, sr, i0, i1, n, search):
    """Tail of the loop -> the audio just before its head. Returns (loop, end index used, lag samples, correlation)."""
    mono = y.mean(axis=1)
    target = mono[i0 - n:i0]
    win = mono[i1 - search - n:i1 + search]
    num = np.correlate(win, target, 'valid')                     # one value per lag -search..+search
    c = np.concatenate([[0.0], np.cumsum(win * win)])
    den = np.sqrt((c[n:] - c[:-n]) * np.dot(target, target)) + 1e-30
    rho = num / den
    j = int(np.argmax(rho))
    lag = j - search
    e1 = i1 + lag
    rho_best = float(rho[j])
    loop = y[i0:e1].copy()
    u = (np.arange(n) + 0.5) / n
    a, b = np.cos(0.5 * np.pi * u), np.sin(0.5 * np.pi * u)
    g = 1.0 / np.sqrt(a * a + b * b + 2.0 * max(rho_best, 0.0) * a * b)
    loop[-n:] = (a * g)[:, None] * y[e1 - n:e1] + (b * g)[:, None] * y[i0 - n:i0]
    return loop, e1, lag, rho_best


def scan(path):
    sr, x = load(path)
    mono = x.mean(axis=1)
    n = len(mono)
    mid = mono[int(0.35 * n):int(0.65 * n)]
    f_line, prom = find_line(mid, sr)
    print(f'{os.path.basename(path)}: {sr} Hz, {x.shape[1]} ch, {n / sr:.2f} s; motor line {f_line:.1f} Hz ({prom:.0f} dB over its floor)')
    # a first look only: the strongest line below the running one in each frame, no tracking
    h = int(0.1 * sr)
    times = (np.arange(n // h) + 0.5) * 0.1
    grid = f_line * np.exp(np.arange(np.log(0.04), np.log(1.03), 0.004))
    P = prominence_map(mono, sr, times, grid, npers=(4096,))
    for i, t in enumerate(times):
        k = int(np.argmax(P[i]))
        rms = db(np.sqrt(np.mean(mono[i * h:(i + 1) * h] ** 2)))
        print(f'{t - 0.05:6.1f}s {rms:6.1f} dBFS  line {grid[k]:7.1f} Hz ({P[i, k]:4.1f} dB)  speed {grid[k] / f_line:5.3f}')


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('source')
    ap.add_argument('--scan', action='store_true')
    ap.add_argument('--out'); ap.add_argument('--name')
    ap.add_argument('--start', type=float, default=0.0)
    ap.add_argument('--loop', type=float, nargs=2)
    ap.add_argument('--stop', type=float, nargs='+')
    ap.add_argument('--rate', type=int, default=48000)
    ap.add_argument('--xfade', type=float, default=0.08)
    ap.add_argument('--match')
    ap.add_argument('--credit', default='')
    ap.add_argument('--speed', choices=('line', 'envelope'), default='line')
    ap.add_argument('--env-smooth', dest='env_smooth', type=float, default=0.05,
                    help='--speed envelope: median window (s); 0.25 keeps switch clicks out of the curve')
    ap.add_argument('--from-rest', dest='from_rest', action='store_true',
                    help='the take starts at standstill: the spin-up curve ramps from 0 at --start up to its first confident frame')
    ap.add_argument('--line', type=float, default=0.0, help='track this line (Hz, +-3 %%) instead of the strongest one: '
                    'a high harmonic smears in a fast spin-up, the fundamental does not')
    ap.add_argument('--hp', type=float, default=0.0, help='high-pass (Hz) against handling rumble; 0 = off')
    ap.add_argument('--duck', type=float, nargs=3, action='append', metavar=('T0', 'T1', 'DB'), help='turn [T0, T1] down by DB')
    a = ap.parse_args()
    if a.scan: scan(a.source); return
    if not (a.out and a.name and a.loop and a.stop): ap.error('--out, --name, --loop and --stop are required')

    sr0, x0 = load(a.source)
    sr = a.rate
    y = duck(resample(x0, sr0, sr), sr, a.duck)
    if a.hp > 0:
        from scipy.signal import butter
        y = sosfilt(butter(2, a.hp, 'highpass', fs=sr, output='sos'), y, axis=0)
    mono = y.mean(axis=1)
    T, (L0, L1) = a.start, a.loop
    S0 = a.stop[0]; S1 = a.stop[1] if len(a.stop) > 1 else len(y) / sr
    iT, i0, i1, iS0, iS1 = (int(round(v * sr)) for v in (T, L0, L1, S0, S1))
    n = int(round(a.xfade * sr)); search = int(round(0.02 * sr))
    if not (0 <= iT < i0 - n and i0 + 4 * n < i1 and i1 + search <= len(y) and iS0 < iS1 <= len(y)):
        raise SystemExit('cut points out of order or out of range')

    # the motor line and the speed through both transitions, measured on the untouched source (resampled)
    if a.speed == 'line':
        f_line, prom = find_line(mono[i0:i1], sr, *((a.line * 0.97, a.line * 1.03) if a.line > 0 else ()))
        ts, fs, ps = track(mono, sr, L0, T, f_line)              # backwards through the spin-up
        te, fe, pe = track(mono, sr, S0, S1, f_line)             # forwards through the coast-down
        vs = speed_curve(ts, fs, ps, f_line, True, a.from_rest)
        ve = speed_curve(te, fe, pe, f_line, False)
    else:
        f_line, prom = 0.0, 0.0
        ref = np.sqrt(np.mean(mono[i0:i1] ** 2))
        ts, vs = envelope_speed(mono, sr, T, L0, ref, True, a.env_smooth)
        te, ve = envelope_speed(mono, sr, S0, S1, ref, False, a.env_smooth)
    ks_t, ks_v = simplify(ts - T, vs)
    ke_t, ke_v = simplify(te - S0, ve)

    # clips
    start = y[iT:i0].copy()
    fin = int(0.005 * sr)
    start[:fin] *= (0.5 - 0.5 * np.cos(np.pi * (np.arange(fin) + 0.5) / fin))[:, None]
    loop, e1, lag, rho = splice_loop(y, sr, i0, i1, n, search)
    stop = y[iS0:iS1].copy()
    fout = min(int(0.3 * sr), len(stop) // 4)
    stop[-fout:] *= (0.5 + 0.5 * np.cos(np.pi * (np.arange(fout) + 0.5) / fout))[:, None]

    # one gain for all three, so the relative levels of the take survive
    gain_db, ref_lufs = 0.0, None
    loop_lufs = loudness(loop, sr)
    if a.match:
        rs, rx = load(a.match)
        ref_lufs = loudness(rx, rs)
        gain_db = ref_lufs - loop_lufs
    peak = max(np.abs(c).max() for c in (start, loop, stop))
    limited = False
    if db(peak) + gain_db > -1.0:
        gain_db = -1.0 - db(peak); limited = True
    g = 10 ** (gain_db / 20)
    start, loop, stop = start * g, loop * g, stop * g

    os.makedirs(a.out, exist_ok=True)
    rng = np.random.default_rng(20260924)
    files = {}
    for key, clip in (('start', start), ('loop', loop), ('stop', stop)):
        fn = f'{a.name}_{key}.wav'
        write_wav16(os.path.join(a.out, fn), clip, sr, rng)
        files[key] = {'file': fn, 'seconds': round(len(clip) / sr, 4),
                      'rmsDb': round(db(np.sqrt(np.mean(clip ** 2))), 2), 'peakDb': round(db(np.abs(clip).max()), 2)}

    # the wrap: the last sample must lead into the first as naturally as neighbouring source samples do
    step = np.abs(loop[-1] - loop[0]).max() / (np.sqrt(np.mean(loop ** 2)) + 1e-12)
    typical = np.median(np.abs(np.diff(loop, axis=0)).max(axis=1)) / (np.sqrt(np.mean(loop ** 2)) + 1e-12)
    w = int(0.05 * sr)
    around = [db(np.sqrt(np.mean(loop[k:k + w] ** 2))) for k in (len(loop) - n - w, len(loop) - n // 2 - w // 2, len(loop) - w, 0)]

    info = {
        'source': os.path.basename(a.source), 'credit': a.credit, 'rate': sr, 'channels': int(y.shape[1]),
        'startFile': files['start']['file'], 'loopFile': files['loop']['file'], 'stopFile': files['stop']['file'],
        'cutStart': [T, L0], 'cutLoop': [L0, round(e1 / sr, 5)], 'cutStop': [S0, round(S1, 5)],
        'lineHz': round(f_line, 2), 'lineProminenceDb': round(prom, 1), 'speedFrom': a.speed, 'highPassHz': a.hp, 'duck': a.duck or [],
        'spliceXfadeS': a.xfade, 'spliceLagMs': round(lag / sr * 1000, 2), 'spliceCorrelation': round(rho, 3),
        'wrapStepOverRms': round(float(step), 3), 'typicalStepOverRms': round(float(typical), 3),
        'wrapLevelsDb': [round(v, 2) for v in around],
        'gainDb': round(gain_db, 2), 'peakLimited': limited, 'loopLufs': round(loop_lufs + gain_db, 2),
        'referenceLufs': None if ref_lufs is None else round(ref_lufs, 2),
        'clips': files,
        'startSpeedT': [round(float(v), 4) for v in ks_t], 'startSpeedV': [round(float(v), 4) for v in ks_v],
        'stopSpeedT': [round(float(v), 4) for v in ke_t], 'stopSpeedV': [round(float(v), 4) for v in ke_v],
    }
    with open(os.path.join(a.out, a.name + '.json'), 'w', encoding='utf-8') as fh:
        json.dump(info, fh, indent=1)

    print((f'motor line {f_line:.1f} Hz ({prom:.0f} dB)' if a.speed == 'line' else 'speed from the level envelope') + f'; gain {gain_db:+.2f} dB{" (peak-limited)" if limited else ""}; loop {loop_lufs + gain_db:.1f} LUFS'
          + ('' if ref_lufs is None else f' vs reference {ref_lufs:.1f}'))
    print(f'loop {L0:.3f}-{e1 / sr:.5f} s: splice lag {lag / sr * 1000:+.2f} ms, correlation {rho:.3f}; wrap step {step:.3f} x rms (neighbouring samples: {typical:.3f})')
    print(f'levels around the wrap (dB): before fade {around[0]:.2f}, mid fade {around[1]:.2f}, end {around[2]:.2f}, head {around[3]:.2f}')
    for k, v in files.items(): print(f'  {v["file"]}: {v["seconds"]:.3f} s, rms {v["rmsDb"]:.1f} dBFS, peak {v["peakDb"]:.1f}')
    print('spin-up speed:  ' + '  '.join(f'{t:.2f}s={v:.3f}' for t, v in zip(ks_t, ks_v)))
    print('coast-down:     ' + '  '.join(f'{t:.2f}s={v:.3f}' for t, v in zip(ke_t, ke_v)))


if __name__ == '__main__':
    main()
