"""Build the English 14 s portrait short from the untouched red-rug Unity take.

Python 3.14 + numpy/Pillow; scoped scipy in .deps is used by the game's foley port.
  python compose_en_v3.py --overlays-only
  python compose_en_v3.py --voice-only
  python compose_en_v3.py --take SteamStore/_work/clips/between_fibers_20261023_v2_red
  python compose_en_v3.py --qa SteamStore/tiktok/between_fibers/revision3/refluff_between_fibers_en_v3.mp4

VO source speed and pitch are unchanged; only the between-phrase silence is edited.
Requires revision3/voice/timing_en_config.json after real VO timing verification.
The original 12 s red take is followed by a 2 s hold of its actual final frame.
The real approved logo and an English CTA occupy the final three seconds.
Final publishing is deliberately outside this script.
"""
import argparse
import hashlib
import json
import math
import os
from pathlib import Path
import re
import subprocess
import sys
import wave

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
BASE = ROOT / 'SteamStore/tiktok/between_fibers'
OUT = BASE / 'revision3'
LOGO = ROOT / 'SteamStore/logo/refluff_logo.png'
FF = Path('C:/UnityProjects/StainedGlassKeeper/Tools/SocialStudio/node_modules/ffmpeg-static/ffmpeg.exe')
FP = FF.parent.parent / 'ffprobe-static/bin/win32/x64/ffprobe.exe'
sys.path.insert(0, str(HERE / '.deps'))
import numpy as np
from PIL import Image, ImageDraw, ImageFont

SR, FPS, DURATION = 48000, 60, 14.0
# source start/end, destination trim start, target active-speech RMS dBFS
PHRASES = []
DROP_RANGES = {}
CAPTIONS = [
    (0.30, 2.20, 'Hmm… where am I?'),
    (2.60, 3.30, 'Wait…'),
    (3.30, 5.15, "I'm between the\ncarpet fibers…"),
    (5.15, 6.30, 'in my own game!'),
]


def load_timing():
    global PHRASES, CAPTIONS, DROP_RANGES
    p = OUT / 'voice/timing_en_config.json'
    config = json.loads(p.read_text(encoding='utf-8'))
    if not config.get('verified_source_timing'):
        raise ValueError('English VO needs checked source timing before rendering.')
    PHRASES = [(v['source'][0], v['source'][1], v['timeline_start'], v.get('active_rms_dbfs', -20))
        for v in config['phrases']]
    DROP_RANGES = {i: v.get('drop_ranges', []) for i, v in enumerate(config['phrases'])}
    if 'captions' in config:
        CAPTIONS = [(v['start'], v['end'], v['text']) for v in config['captions']]
    if len(PHRASES) != 2:
        raise ValueError('Expected two natural-speed English phrases.')
    for i, (a, b, dst, _) in enumerate(PHRASES):
        previous = a
        for ca, cb in DROP_RANGES.get(i, []):
            if ca < previous or cb <= ca or cb > b:
                raise ValueError('Internal silence cut is invalid: ' + str((ca, cb)))
            previous = cb
        duration = b - a - sum(cb - ca for ca, cb in DROP_RANGES.get(i, []))
        if a < 0 or b <= a or dst < 0 or dst + duration > 6.4:
            raise ValueError('VO phrase must finish before the vacuum pass: ' + str((a, b, dst)))


def run(cmd, **kw):
    return subprocess.run([str(x) for x in cmd], check=True, **kw)


def decode(path):
    return np.frombuffer(subprocess.check_output([str(FF), '-v', 'error', '-nostdin', '-i', str(path),
        '-ar', str(SR), '-ac', '2', '-f', 'f32le', '-']), dtype='<f4').reshape(-1, 2).copy()


def wav(path, x):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), 'wb') as f:
        f.setnchannels(2)
        f.setsampwidth(2)
        f.setframerate(SR)
        f.writeframes(np.round(np.clip(x, -1, 1) * 32767).astype('<i2').tobytes())


def amp_stats(x):
    return {'duration': round(len(x) / SR, 6), 'peak_dbfs': round(20 * math.log10(float(np.max(np.abs(x))) + 1e-12), 3),
        'rms_dbfs': round(20 * math.log10(float(np.sqrt(np.mean(x.astype(np.float64) ** 2))) + 1e-12), 3),
        'finite': bool(np.isfinite(x).all()), 'clipped_samples': int(np.count_nonzero(np.abs(x) >= .99997))}


def voice():
    source = OUT / 'voice/syntx_en_original.mp3'
    x = decode(source)
    y = np.zeros((round(SR * DURATION), 2), np.float32)
    report = {'source': str(source.relative_to(ROOT)), 'sha256': hashlib.sha256(source.read_bytes()).hexdigest(),
        'source_decoded_seconds': len(x) / SR, 'sample_rate': SR, 'speed': 1.0, 'pitch_shift': 0, 'phrases': []}
    for phrase_index, (a, b, dst, target) in enumerate(PHRASES):
        if round(b * SR) > len(x):
            raise ValueError('English VO trim exceeds the decoded source duration.')
        segments = []
        last = a
        for cut_a, cut_b in DROP_RANGES.get(phrase_index, []):
            segments.append(x[round(last * SR):round(cut_a * SR)].copy())
            last = cut_b
        segments.append(x[round(last * SR):round(b * SR)].copy())
        # Eight-ms ramps act only on the quiet join; no time-stretch or pitch shift.
        for part in segments:
            n = min(round(.008 * SR), len(part) // 2)
            part[:n] *= np.linspace(0, 1, n)[:, None]
            part[-n:] *= np.linspace(1, 0, n)[:, None]
        seg = np.concatenate(segments, axis=0)
        block = SR // 100
        mono = seg.mean(axis=1)
        rms = np.sqrt(np.mean(mono[:len(mono) // block * block].reshape(-1, block) ** 2, axis=1))
        active = rms > max(float(np.max(rms)) * .025, .001)
        power = float(np.sqrt(np.mean(rms[active].astype(np.float64) ** 2)))
        gain = 10 ** (target / 20) / (power + 1e-12)
        seg *= gain
        fade = round(.008 * SR)
        seg[:fade] *= np.linspace(0, 1, fade)[:, None]
        seg[-fade:] *= np.linspace(1, 0, fade)[:, None]
        start = round(dst * SR)
        y[start:start + len(seg)] += seg
        report['phrases'].append({'source_trim': [a, b], 'timeline_trim': [dst, round(dst + len(seg) / SR, 3)],
            'internal_silence_cuts': DROP_RANGES.get(phrase_index, []),
            'gain_db': round(20 * math.log10(gain), 3), 'active_rms_dbfs': target})
    wav(OUT / 'voice/voice_aligned.wav', y)
    report['aligned'] = amp_stats(y)
    (OUT / 'voice/timing.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps(report, ensure_ascii=False, indent=2))
    return y


def text_image(text, target, size=56):
    canvas = Image.new('RGBA', (1080, 1920), (0, 0, 0, 0))
    d = ImageDraw.Draw(canvas)
    font = ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf', size)
    lines = text.split('\n')
    while max(d.textbbox((0, 0), line, font=font)[2] for line in lines) > 840:
        size -= 1
        font = ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf', size)
    y = 1370 - (len(lines) - 1) * round(size * .61)
    for line in lines:
        d.text((540, y), line, font=font, anchor='mm', fill=(250, 249, 244, 255),
            stroke_width=4, stroke_fill=(18, 25, 28, 230))
        y += round(size * 1.22)
    canvas.save(target)


def packshot_image(target):
    # Local dark gradient behind the real transparent logo/CTA; rug below stays visible.
    canvas = Image.new('RGBA', (1080, 1920), (0, 0, 0, 0))
    alpha = np.zeros((1920, 1080), dtype=np.uint8)
    alpha[:600] = 154
    alpha[600:760] = np.linspace(154, 0, 160, dtype=np.uint8)[:, None]
    backing = Image.new('RGBA', canvas.size, (10, 27, 34, 0))
    backing.putalpha(Image.fromarray(alpha))
    canvas.alpha_composite(backing)
    logo = Image.open(LOGO).convert('RGBA')
    logo = logo.crop(logo.getchannel('A').getbbox())
    logo = logo.resize((730, round(logo.height * 730 / logo.width)), Image.Resampling.LANCZOS)
    canvas.alpha_composite(logo, (175, 210))
    d = ImageDraw.Draw(canvas)
    d.text((540, 496), 'Wishlist on Steam', anchor='mm',
        font=ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', 56), fill=(255, 249, 232, 255),
        stroke_width=1, stroke_fill=(7, 20, 27, 220))
    d.text((540, 573), 'Coming this November', anchor='mm',
        font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 44), fill=(255, 249, 232, 255),
        stroke_width=1, stroke_fill=(7, 20, 27, 220))
    canvas.save(target)
    (OUT / 'edit/packshot_source.json').write_text(json.dumps({'logo': str(LOGO.relative_to(ROOT)),
        'logo_sha256': hashlib.sha256(LOGO.read_bytes()).hexdigest(), 'start': 11.0, 'end': DURATION,
        'cta': 'Wishlist on Steam', 'release_text': 'Coming this November',
        'release_month_authority': 'Explicit user instruction in this chat; no exact date implied.',
        'source_video_seconds': 12, 'final_frame_hold_seconds': 2}, indent=2), encoding='utf-8')


def overlays():
    d = OUT / 'edit'
    d.mkdir(parents=True, exist_ok=True)
    items = []
    for i, (start, end, text) in enumerate(CAPTIONS):
        path = d / ('subtitle_%02d.png' % i)
        text_image(text, path)
        items.append((start, end, path))
    path = d / 'packshot.png'
    packshot_image(path)
    items.append((11.0, DURATION, path))
    def stamp(sec):
        ms = round(sec * 1000)
        return '%02d:%02d:%02d,%03d' % (ms // 3600000, ms // 60000 % 60, ms // 1000 % 60, ms % 1000)
    (OUT / 'subtitles_en.srt').write_text('\n\n'.join('%d\n%s --> %s\n%s' %
        (i + 1, stamp(a), stamp(b), t) for i, (a, b, t) in enumerate(CAPTIONS)) + '\n', encoding='utf-8')
    return items


def game_audio(take):
    sys.path.insert(0, str(ROOT / 'Tools/steam_art/trailer'))
    import foley
    plan = foley.build_plan(str(take), SR, seed=20261023)
    if any('NON-COMMERCIAL' in w for w in plan.warnings):
        raise RuntimeError('A non-commercial reference recording cannot be used in the short: ' + str(plan.warnings))
    x = foley.render_take(str(take), 0, DURATION, SR, seed=20261023)
    wav(OUT / 'edit/game_foley.wav', x)
    report = {'take': str(take), 'stats': amp_stats(x), 'warnings': plan.warnings,
        'voices': len(plan.voices), 'credits': plan.opts['credits'],
        'clips': sorted(set(v.clip for v in plan.voices if hasattr(v, 'clip')))}
    (OUT / 'edit/foley_report.json').write_text(json.dumps(report, ensure_ascii=False, indent=2, default=str), encoding='utf-8')
    print('Game foley:', json.dumps(report, ensure_ascii=False, default=str))
    return x


def mix_audio(take, y, foley_gain):
    x = game_audio(take)
    t = np.arange(len(x)) / SR
    duck = np.ones(len(x), np.float32)
    for phrase_index, (a, b, dst, _) in enumerate(PHRASES):
        end = dst + b - a - sum(cb - ca for ca, cb in DROP_RANGES.get(phrase_index, []))
        ramp = np.clip((t - (dst - .08)) / .08, 0, 1) * np.clip(((end + .10) - t) / .10, 0, 1)
        duck = np.minimum(duck, 1 - .75 * ramp)
    x *= (10 ** (foley_gain / 20) * duck)[:, None]
    x[-round(.10 * SR):] *= np.linspace(1, 0, round(.10 * SR))[:, None]
    mix = x + y
    # Keep pre-normalization PCM un-clipped; this is constant gain, not compression.
    peak = float(np.max(np.abs(mix)))
    if peak > .95:
        mix *= .95 / peak
    raw = OUT / 'edit/mix_raw.wav'
    wav(raw, mix)
    first = run([FF, '-hide_banner', '-nostdin', '-i', raw, '-af', 'loudnorm=I=-15:LRA=9:TP=-1.5:print_format=json',
        '-f', 'null', '-'], capture_output=True, text=True, errors='replace')
    match = re.search(r'\{\s*"input_i".*?\}', first.stderr, flags=re.S)
    if not match:
        raise RuntimeError('loudnorm did not return measurements')
    measured = json.loads(match.group())
    filt = ('loudnorm=I=-15:LRA=9:TP=-1.5:measured_I={input_i}:measured_LRA={input_lra}:'
        'measured_TP={input_tp}:measured_thresh={input_thresh}:offset={target_offset}:linear=true:print_format=json').format(**measured)
    final = OUT / 'mix_master.wav'
    second = run([FF, '-v', 'info', '-y', '-nostdin', '-i', raw, '-af', filt, '-ar', SR, '-c:a', 'pcm_s24le', final],
        capture_output=True, text=True, errors='replace')
    report = {'target_lufs': -15, 'target_true_peak_db': -1.5, 'game_gain_db': foley_gain,
        'raw': amp_stats(mix), 'measurement': measured}
    match2 = re.search(r'\{\s*"input_i".*?\}', second.stderr, flags=re.S)
    if match2:
        report['normalization'] = json.loads(match2.group())
    (OUT / 'edit/mix_report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    return final


def compose(args):
    take = Path(args.take).resolve()
    if not (take / 'audio.jsonl').is_file():
        raise FileNotFoundError('Authentic Unity audio.jsonl is required: ' + str(take))
    y = voice()
    items = overlays()
    sound = mix_audio(take, y, args.foley_gain)
    output = Path(args.output).resolve() if args.output else OUT / 'assembled_en_v3.mp4'
    if args.video:
        cmd = [FF, '-y', '-v', 'warning', '-nostdin', '-i', Path(args.video).resolve()]
    else:
        pattern = Path(args.frames).resolve() if args.frames else take / 'f%04d.png'
        cmd = [FF, '-y', '-v', 'warning', '-nostdin', '-thread_queue_size', '128',
            '-framerate', FPS, '-start_number', args.start_number, '-i', pattern]
    cmd += ['-i', sound]
    for _a, _b, path in items:
        cmd += ['-loop', '1', '-i', path]
    filters = ['[0:v]scale=1080:1920:flags=lanczos,setsar=1,tpad=stop_mode=clone:stop_duration=2,format=rgba[v0]']
    for i, (start, end, item_path) in enumerate(items):
        src = '%d:v' % (i + 2)
        if item_path.name == 'packshot.png':
            filters.append('[%s]format=rgba,fade=t=in:st=11:d=0.3:alpha=1[card]' % src)
            src = 'card'
        filters.append("[v%d][%s]overlay=enable='gte(t,%.3f)*lt(t,%.3f)':format=auto[v%d]" %
            (i, src, start, end, i + 1))
    filters.append('[v%d]scale=iw:ih:out_color_matrix=bt709:out_range=tv,format=yuv420p[v]' % len(items))
    cmd += ['-filter_complex', ';'.join(filters), '-map', '[v]', '-map', '1:a:0', '-t', DURATION,
        '-r', FPS, '-c:v', 'libx264', '-preset', 'slow', '-crf', '17', '-profile:v', 'high', '-level', '4.2',
        '-pix_fmt', 'yuv420p', '-color_range', 'tv', '-color_primaries', 'bt709', '-color_trc', 'bt709', '-colorspace', 'bt709',
        '-c:a', 'aac', '-b:a', '192k', '-ar', SR, '-ac', '2', '-movflags', '+faststart', output]
    run(cmd)
    qa(output)


def qa(path):
    path = Path(path).resolve()
    d = OUT / 'qa'
    d.mkdir(parents=True, exist_ok=True)
    data = json.loads(subprocess.check_output([str(FP), '-v', 'error', '-count_frames', '-show_streams',
        '-show_format', '-of', 'json', str(path)]))
    run([FF, '-v', 'error', '-nostdin', '-i', path, '-f', 'null', '-'])
    v = next(s for s in data['streams'] if s['codec_type'] == 'video')
    a = next(s for s in data['streams'] if s['codec_type'] == 'audio')
    assert (v['width'], v['height'], v['codec_name'], v['pix_fmt']) == (1080, 1920, 'h264', 'yuv420p'), v
    assert abs(float(data['format']['duration']) - DURATION) < .06, data['format']['duration']
    assert v['r_frame_rate'] == '60/1', v['r_frame_rate']
    assert a['codec_name'] == 'aac' and int(a['sample_rate']) == SR, a
    assert int(v['nb_read_frames']) == round(DURATION * FPS), v['nb_read_frames']
    audio = decode(path)
    measures = amp_stats(audio)
    assert measures['finite'] and measures['clipped_samples'] == 0, measures
    loud = run([FF, '-v', 'info', '-nostdin', '-i', path, '-af', 'loudnorm=I=-15:LRA=9:TP=-1.5:print_format=json',
        '-f', 'null', '-'], capture_output=True, text=True, errors='replace')
    match = re.search(r'\{\s*"input_i".*?\}', loud.stderr, re.S)
    summary = {'file': str(path), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest(),
        'video': v, 'audio': a, 'format': data['format'], 'decoded_audio': measures,
        'loudness': json.loads(match.group()) if match else None}
    (d / (path.stem + '_report.json')).write_text(json.dumps(summary, indent=2), encoding='utf-8')
    times = [.5, 1.5, 2.7, 3.7, 4.7, 5.8, 6.5, 7.0, 7.5, 8.2, 8.8, 9.5, 10.5, 11.2, 12.1, 12.8, 13.4, 13.85]
    sheet = Image.new('RGB', (6 * 270, 3 * (480 + 28)), '#1b2023')
    for i, t in enumerate(times):
        frame = d / ('frame_%02d.png' % i)
        run([FF, '-y', '-v', 'error', '-nostdin', '-ss', t, '-i', path, '-frames:v', '1', frame])
        im = Image.open(frame).convert('RGB')
        sheet.paste(im.resize((270, 480), Image.Resampling.LANCZOS), ((i % 6) * 270, (i // 6) * 508))
        ImageDraw.Draw(sheet).text(((i % 6) * 270 + 8, (i // 6) * 508 + 483), '%.2fs' % t,
            fill='white', font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 19))
    sheet.save(d / (path.stem + '_contact_sheet.jpg'), quality=95)
    print(json.dumps({'file': str(path), 'frames': v['nb_read_frames'], 'duration': data['format']['duration'],
        'audio': measures, 'loudness': summary['loudness']}, ensure_ascii=False, indent=2))


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--take')
    ap.add_argument('--frames', help='absolute/relative image sequence pattern; default TAKE/f%%04d.png')
    ap.add_argument('--start-number', type=int, default=1)
    ap.add_argument('--video', help='already encoded continuous camera take instead of PNG frames')
    ap.add_argument('--output')
    ap.add_argument('--foley-gain', type=float, default=-4.0)
    ap.add_argument('--voice-only', action='store_true')
    ap.add_argument('--overlays-only', action='store_true')
    ap.add_argument('--qa')
    args = ap.parse_args()
    if args.qa:
        qa(args.qa)
    elif args.overlays_only:
        overlays()
    elif args.voice_only:
        load_timing()
        voice()
        overlays()
    elif args.take:
        load_timing()
        compose(args)
    else:
        ap.error('--take, --qa, or --voice-only is required')


if __name__ == '__main__':
    main()
