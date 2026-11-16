"""Turn a real recording into a bank clip other than a machine take (Docs/AUDIO.md 11): a seamless surface or
machine LOOP, or a ONE-SHOT (power on / off). Machine takes with a switch-on and a coast-down go through
cut_machine_recording.py instead.

    python Tools/prepare_recording.py loop <source> --out <dir> --name <name> --slot contact|motor
        --span A B [--span A B ...] [--grains] [--len 8] [--xfade 0.06] [--hp 60] [--lp 0] [--mono] [--duck T0 T1 dB] [--limit DB [--release 0.08]]
        [--match <reference.wav>] [--credit "<title> by <author>, <url>, <licence>"] [--seed 1]
    python Tools/prepare_recording.py oneshot <source> --out <dir> --name <name> --slot on|off
        --span A B [--fadein 0.002] [--fadeout 0.05] [--hp 40] [--gain-db G | --match <reference.wav>] [--credit ...]
    python Tools/prepare_recording.py scan <source> [--from A --to B]
        level every 0.1 s and which frames count as "mid-stroke" for --grains

LOOP, plain: the spans (steady stretches) are joined with equal-power crossfades and the tail is folded over the
head, so the wrap continues as naturally as any other point of the take. The fold eats one crossfade: a single span
[A, B] gives a loop of B - A - xfade seconds, so for a periodic machine (a pad turning) make B - A = N periods + xfade.
LOOP, --grains: for hand strokes (scrub, scrape, squeegee, mop). The loud middle of each stroke is kept, the
direction changes and pauses between strokes are dropped: grains of 0.35-0.9 s are cut from the frames within
--keep dB (default 8) of the span's loud frames, shuffled (never the same stroke twice in a row) and crossfaded
into --len seconds of continuous friction. The game fades the layer with the player's own stroke speed, so a
recorded stroke rhythm would fight the player's; the grains keep the real texture without the recorded rhythm.
Level: --match gives the loop the K-weighted loudness of the reference clip (the placeholder it replaces), so the
bank's volumes keep their meaning; peaks cap the gain at -1 dBFS. --limit DB lets the loudness win by up to DB: a
look-ahead limiter (5 ms, --release 80 ms, circular for a loop) holds the peaks at -1 dBFS and may take at most DB off
them; crackle (scraping, scrubbing, slurping) wants --release 0.02 so each tamed tick does not duck the next ones. Meant for SPARSE peaks (water splashes in a squeegee push, one slap in a mop take); dense material keeps its
crest factor and simply ends up quieter (raise the bank volume). The report says how much it took off and how often. Output: 48 kHz 16-bit dithered WAV, the source's
channels (or --mono), plus <name>.json, which Editor/ToolSoundBaker.ImportClip puts into the bank slot.
Any format soundfile reads (a Freesound OGG preview). Needs numpy, scipy, soundfile.
"""
import argparse, json, os
import numpy as np
from scipy.signal import butter, sosfilt
from cut_machine_recording import load, resample, loudness, write_wav16, db, duck

RATE = 48000


def filtered(y, sr, hp, lp):
    if hp > 0: y = sosfilt(butter(2, hp, 'highpass', fs=sr, output='sos'), y, axis=0)
    if lp > 0: y = sosfilt(butter(2, lp, 'lowpass', fs=sr, output='sos'), y, axis=0)
    return y


def envelope_db(mono, sr, hop=0.02):
    h = int(hop * sr)
    n = len(mono) // h
    return np.array([db(np.sqrt(np.mean(mono[i * h:(i + 1) * h] ** 2)) + 1e-12) for i in range(n)]), hop


def join(pieces, n):
    """Overlap-add with an n-sample equal-power crossfade at every join (the pieces are unrelated noise)."""
    u = (np.arange(n) + 0.5) / n
    fin, fout = np.sin(0.5 * np.pi * u)[:, None], np.cos(0.5 * np.pi * u)[:, None]
    out = pieces[0].copy()
    for p in pieces[1:]:
        out[-n:] = out[-n:] * fout + p[:n] * fin
        out = np.concatenate([out, p[n:]])
    return out


def fold(s, n):
    """Seamless loop from s: the n samples after the end are crossfaded over the head, so loop[-1] -> loop[0]
    continues exactly as s[L-1] -> s[L] did."""
    L = len(s) - n
    u = (np.arange(n) + 0.5) / n
    loop = s[:L].copy()
    loop[:n] = s[:n] * np.sin(0.5 * np.pi * u)[:, None] + s[L:L + n] * np.cos(0.5 * np.pi * u)[:, None]
    return loop


def grains(y, sr, spans, length, n, keep_db, rng, gmin=0.35, gmax=0.9):
    """Continuous friction from the loud middles of recorded strokes (see the module docstring)."""
    mono = y.mean(axis=1)
    env, hop = envelope_db(mono, sr)
    ok = np.zeros(len(env), bool)
    for a, b in spans:
        i0, i1 = int(a / hop), min(len(env), int(b / hop))
        seg = env[i0:i1]
        if len(seg) == 0: continue
        ok[i0:i1] = seg >= np.percentile(seg, 90) - keep_db
    # runs of kept frames = usable regions, each long enough for the shortest grain plus both crossfades
    regions, i = [], 0
    while i < len(ok):
        if ok[i]:
            j = i
            while j < len(ok) and ok[j]: j += 1
            if (j - i) * hop >= gmin + 2 * n / sr: regions.append((i * hop, j * hop))
            i = j
        else:
            i += 1
    if not regions: raise SystemExit('no stretch loud and long enough for a grain: widen the spans or raise --keep')
    total = sum(b - a for a, b in regions)
    pieces, got, last = [], 0.0, -1
    need = length + n / sr
    while got < need:
        # pick a region by length (long drags give more grains), never the one just used when there is a choice
        w = np.array([b - a for a, b in regions])
        if len(regions) > 1 and last >= 0: w[last] = 0.0
        k = int(rng.choice(len(regions), p=w / w.sum()))
        a, b = regions[k]
        g = min(rng.uniform(gmin, gmax), b - a)
        t0 = rng.uniform(a, b - g)
        pieces.append(y[int(t0 * sr):int(t0 * sr) + int(g * sr)])
        got += g - n / sr
        last = k
    return join(pieces, n), regions, total


def limiter(y, sr, ceiling_db=-1.0, look=0.005, release=0.08, circular=False):
    """Look-ahead peak limiter: the gain needed to keep every sample under the ceiling, as a running minimum over the
    next `look` seconds (so it is already down when the peak arrives), released exponentially. circular: the clip is
    a loop, so its head sees the tail's peaks and the wrap stays seamless. Returns (y, max reduction dB, % of the
    samples turned down by more than 1 dB)."""
    c = 10 ** (ceiling_db / 20)
    a = np.abs(y).max(axis=1)
    L, n = max(1, int(look * sr)), len(a)
    ext = np.concatenate([a[-L:], a, a[:L]]) if circular else np.concatenate([np.zeros(L), a, np.zeros(L)])
    need = np.minimum(1.0, c / np.maximum(ext, 1e-12))
    from scipy.ndimage import minimum_filter1d
    need = minimum_filter1d(need, size=2 * L + 1)[L:L + n]         # down L samples before the peak, up L after
    k = 1 - np.exp(-1.0 / (release * sr))
    g = np.empty(n)
    for rep in range(2 if circular else 1):                          # a loop runs twice so the release wraps too
        prev = g[-1] if rep else 1.0
        for i in range(n):
            prev = min(need[i], prev + (1 - prev) * k)
            g[i] = prev
    return y * g[:, None], float(-db(g.min())), float(np.mean(g < 10 ** (-1 / 20)) * 100)


def level(clips, sr, match, gain_db, limit_db=0.0, circular=False, release=0.08):
    """One gain for the clips: the K-weighted loudness of clips[0] = the reference's, or a fixed gain. Peaks cap the gain
    at -1 dBFS + limit_db; with limit_db > 0 a limiter holds them at -1 dBFS (a second pass puts the loudness back on
    the reference when the limiter took some of it)."""
    ref_lufs = None
    if match:
        rs, rx = load(match)
        ref_lufs = loudness(rx, rs)
        gain_db = ref_lufs - loudness(clips[0], sr)
    peak_db = db(max(np.abs(c).max() for c in clips))
    cap = -1.0 - peak_db + max(0.0, limit_db)
    limited = gain_db > cap
    gain_db = min(gain_db, cap)
    if limit_db > 0 and gain_db + peak_db > -1.0:
        out, info = clips, (0.0, 0.0)
        for _ in range(2):
            g = 10 ** (gain_db / 20)
            out, red, pct = zip(*(limiter(c * g, sr, release=release, circular=circular) for c in clips))
            info = (max(red), max(pct))
            if ref_lufs is None or limited: break
            gain_db = min(cap, gain_db + ref_lufs - loudness(out[0], sr))
        print(f'  limiter: up to {info[0]:.1f} dB off, {info[1]:.2f} % of the samples more than 1 dB down')
        return list(out), gain_db, limited, ref_lufs
    g = 10 ** (gain_db / 20)
    return [c * g for c in clips], gain_db, limited, ref_lufs


def seam_report(loop, sr):
    """The wrap step against neighbouring samples, and the 1 ms high-band energy there against the loudest frame
    around it (a click shows up as the wrap standing out)."""
    rms = np.sqrt(np.mean(loop ** 2)) + 1e-12
    step = np.abs(loop[-1] - loop[0]).max() / rms
    typical = np.median(np.abs(np.diff(loop, axis=0)).max(axis=1)) / rms
    hp = sosfilt(butter(2, 6000, 'highpass', fs=sr, output='sos'), np.concatenate([loop[-int(0.2 * sr):], loop[:int(0.2 * sr)]]).mean(axis=1))
    m = int(0.001 * sr)
    fr = np.array([db(np.sqrt(np.mean(hp[i:i + m] ** 2)) + 1e-12) for i in range(0, len(hp) - m, m)])
    med = np.median(fr)
    mid = len(fr) // 2
    at_wrap = fr[mid - 1:mid + 1].max() - med
    loudest = np.delete(fr, [mid - 1, mid]).max() - med
    return float(step), float(typical), float(at_wrap), float(loudest)


def write(a, clip, extra):
    os.makedirs(a.out, exist_ok=True)
    fn = a.name + '.wav'
    write_wav16(os.path.join(a.out, fn), clip, RATE, np.random.default_rng(20260925))
    info = {'slot': a.slot, 'file': fn, 'source': os.path.basename(a.source), 'credit': a.credit,
            'rate': RATE, 'channels': int(clip.shape[1]), 'seconds': round(len(clip) / RATE, 4),
            'rmsDb': round(db(np.sqrt(np.mean(clip ** 2))), 2), 'peakDb': round(db(np.abs(clip).max()), 2),
            'lufs': round(loudness(clip, RATE), 2), 'spans': a.span, 'highPassHz': a.hp, 'duck': a.duck or []}
    info.update(extra)
    with open(os.path.join(a.out, a.name + '.json'), 'w', encoding='utf-8') as fh:
        json.dump(info, fh, indent=1)
    print(f"{fn}: {info['seconds']:.2f} s, {info['channels']} ch, rms {info['rmsDb']:.1f} dBFS, peak {info['peakDb']:.1f}, {info['lufs']:.1f} LUFS")
    return info


def source(a):
    sr0, x0 = load(a.source)
    y = duck(resample(x0, sr0, RATE), RATE, a.duck)
    if getattr(a, 'mono', False): y = y.mean(axis=1, keepdims=True)
    return filtered(y, RATE, a.hp, getattr(a, 'lp', 0.0))


def cmd_loop(a):
    y = source(a)
    n = int(round(a.xfade * RATE))
    rng = np.random.default_rng(a.seed)
    if a.grains:
        s, regions, total = grains(y, RATE, a.span, a.len, n, a.keep, rng)
        extra = {'method': 'grains', 'keepDb': a.keep, 'regions': [[round(p, 2), round(q, 2)] for p, q in regions], 'usableS': round(total, 2)}
    else:
        pieces = [y[int(p * RATE):int(q * RATE)] for p, q in a.span]
        s = join(pieces, n) if len(pieces) > 1 else pieces[0]
        extra = {'method': 'spans'}
    loop = fold(s, n)
    (loop,), gain, limited, ref = level([loop], RATE, a.match, 0.0, a.limit, circular=True, release=a.release)
    extra['limitDb'] = a.limit
    extra['limitReleaseS'] = a.release
    step, typical, at_wrap, loudest = seam_report(loop, RATE)
    extra.update({'gainDb': round(gain, 2), 'peakLimited': bool(limited), 'referenceLufs': None if ref is None else round(ref, 2),
                  'xfadeS': a.xfade, 'wrapStepOverRms': round(step, 3), 'typicalStepOverRms': round(typical, 3),
                  'wrapHighBandDb': round(at_wrap, 2), 'loudestNearbyDb': round(loudest, 2)})
    write(a, loop, extra)
    print(f"  gain {gain:+.2f} dB{' (peak-limited)' if limited else ''}" + ('' if ref is None else f', reference {ref:.1f} LUFS')
          + f"; wrap step {step:.3f} x rms (neighbours {typical:.3f}); high band at the wrap {at_wrap:+.1f} dB vs loudest nearby {loudest:+.1f}")
    if a.grains: print(f"  {len(extra['regions'])} stroke regions, {extra['usableS']:.1f} s usable")


def cmd_oneshot(a):
    y = source(a)
    p, q = a.span[0]
    clip = y[int(p * RATE):int(q * RATE)].copy()
    fi, fo = int(a.fadein * RATE), min(int(a.fadeout * RATE), len(clip) // 2)
    if fi > 0: clip[:fi] *= (0.5 - 0.5 * np.cos(np.pi * (np.arange(fi) + 0.5) / fi))[:, None]
    if fo > 0: clip[-fo:] *= (0.5 + 0.5 * np.cos(np.pi * (np.arange(fo) + 0.5) / fo))[:, None]
    (clip,), gain, limited, ref = level([clip], RATE, a.match, a.gain_db, a.limit, release=a.release)
    write(a, clip, {'method': 'oneshot', 'gainDb': round(gain, 2), 'peakLimited': bool(limited),
                    'referenceLufs': None if ref is None else round(ref, 2), 'fadeInS': a.fadein, 'fadeOutS': a.fadeout})
    print(f"  gain {gain:+.2f} dB{' (peak-limited)' if limited else ''}")


def cmd_scan(a):
    sr0, x0 = load(a.source)
    mono = x0.mean(axis=1)
    env, hop = envelope_db(mono, sr0, 0.1)
    t0 = a.frm or 0.0
    t1 = a.to or len(env) * hop
    sel = env[int(t0 / hop):int(t1 / hop)]
    thr = np.percentile(sel, 90) - a.keep
    for i in range(int(t0 / hop), min(len(env), int(t1 / hop))):
        print(f"{i * hop:6.1f}s {env[i]:6.1f} dBFS {'#' * max(0, int((env[i] + 60) / 2))}{'  keep' if env[i] >= thr else ''}")


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest='cmd', required=True)
    for name in ('loop', 'oneshot'):
        p = sub.add_parser(name)
        p.add_argument('source'); p.add_argument('--out', required=True); p.add_argument('--name', required=True)
        p.add_argument('--slot', required=True, choices=('contact', 'motor') if name == 'loop' else ('on', 'off'))
        p.add_argument('--span', type=float, nargs=2, action='append', required=True)
        p.add_argument('--hp', type=float, default=0.0); p.add_argument('--lp', type=float, default=0.0)
        p.add_argument('--mono', action='store_true')
        p.add_argument('--duck', type=float, nargs=3, action='append', metavar=('T0', 'T1', 'DB'))
        p.add_argument('--match'); p.add_argument('--credit', default='')
        p.add_argument('--limit', type=float, default=0.0, metavar='DB',
                       help='let a limiter take up to DB off sparse peaks instead of capping the gain there')
        p.add_argument('--release', type=float, default=0.08,
                       help='limiter release (s); 0.02 for crackle, so a tamed tick does not duck the ticks after it')
    pl = sub.choices['loop']
    pl.add_argument('--grains', action='store_true'); pl.add_argument('--len', type=float, default=8.0)
    pl.add_argument('--xfade', type=float, default=0.06); pl.add_argument('--keep', type=float, default=8.0)
    pl.add_argument('--seed', type=int, default=1)
    po = sub.choices['oneshot']
    po.add_argument('--fadein', type=float, default=0.002); po.add_argument('--fadeout', type=float, default=0.05)
    po.add_argument('--gain-db', dest='gain_db', type=float, default=0.0)
    ps = sub.add_parser('scan')
    ps.add_argument('source'); ps.add_argument('--from', dest='frm', type=float); ps.add_argument('--to', type=float)
    ps.add_argument('--keep', type=float, default=8.0)
    a = ap.parse_args()
    {'loop': cmd_loop, 'oneshot': cmd_oneshot, 'scan': cmd_scan}[a.cmd](a)


if __name__ == '__main__':
    main()
