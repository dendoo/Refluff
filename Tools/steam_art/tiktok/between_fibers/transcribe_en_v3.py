"""Optional local ASR audit. Requires the project's existing cp312 ASR packages."""
import json
import argparse
import subprocess
import sys
from pathlib import Path

sys.stdout.reconfigure(encoding='utf-8')
ROOT = Path(__file__).resolve().parents[4]
OUT = ROOT / 'SteamStore/tiktok/between_fibers/revision3'
ap = argparse.ArgumentParser(description=__doc__)
ap.add_argument('--input', default=str(OUT / 'voice/syntx_en_original.mp3'))
ap.add_argument('--output', default=str(OUT / 'voice/source_asr.json'))
args = ap.parse_args()
sys.path.insert(0, str(ROOT / 'SteamStore/_work/tiktok_scraper/asr_packages'))
import numpy as np
from faster_whisper import WhisperModel

ff = Path('C:/UnityProjects/StainedGlassKeeper/Tools/SocialStudio/node_modules/ffmpeg-static/ffmpeg.exe')
x = np.frombuffer(subprocess.check_output([str(ff), '-v', 'error', '-i', args.input,
    '-f', 'f32le', '-ac', '1', '-ar', '16000', '-']), dtype=np.float32)
model_dir = ROOT / 'SteamStore/_work/tiktok_scraper/asr_models'
local_model = next(model_dir.rglob('model.bin'), None)
model = WhisperModel(str(local_model.parent) if local_model else 'base.en', device='cpu', compute_type='int8',
    download_root=str(model_dir))
segments, info = model.transcribe(x, language='en', beam_size=5, word_timestamps=True, vad_filter=False)
rows = []
for s in segments:
    row = {'start': s.start, 'end': s.end, 'text': s.text,
        'words': [{'start': w.start, 'end': w.end, 'word': w.word, 'probability': w.probability} for w in (s.words or [])]}
    rows.append(row)
    print(json.dumps(row, ensure_ascii=False), flush=True)
Path(args.output).parent.mkdir(parents=True, exist_ok=True)
Path(args.output).write_text(json.dumps({'asr': 'faster-whisper base.en int8, en', 'source': str(args.input),
    'language': info.language, 'segments': rows}, ensure_ascii=False, indent=2), encoding='utf-8')
