"""Prepare an isolated red-rug edit; keep all v1 artifacts untouched."""
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
source = (HERE / 'compose.py').read_text(encoding='utf-8')
source = source.replace("OUT = ROOT / 'SteamStore/tiktok/between_fibers'",
    "BASE = ROOT / 'SteamStore/tiktok/between_fibers'\nOUT = BASE / 'revision2'")
source = source.replace("source = OUT / 'voice/syntx_ru_original.mp3'",
    "source = BASE / 'voice/syntx_ru_original.mp3'")
source = source.replace('assembled_v1.mp4', 'assembled_red_v2.mp4')
source = source.replace('python compose.py', 'python compose_red.py')
source = source.replace('between_fibers_20261023_v1', 'between_fibers_20261023_v2_red')
compile(source, 'compose_red.py', 'exec')
(HERE / 'compose_red.py').write_text(source, encoding='utf-8')
out = ROOT / 'SteamStore/tiktok/between_fibers/revision2'
out.mkdir(parents=True, exist_ok=True)
manifest = {'version': 1, 'output': 'refluff_between_fibers_red_v2.mp4',
    'width': 1080, 'height': 1920, 'fps': 60, 'fit': 'contain', 'mute': False,
    'clips': [{'file': 'assembled_red_v2.mp4', 'start': 0, 'duration': 12}]}
(out / 'render_social_studio_red_v2.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
print('Prepared compose_red.py and revision2/render_social_studio_red_v2.json; syntax OK')
