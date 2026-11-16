"""Extend the real shop scrim past the Editor panel's 1440px viewport.

The runtime document is laid out at1080x1920. Unity's element-capture API still
clips the flat scrim at the physical Game view edge. Preserve all actual shop
pixels and extend only its uniform premultiplied RGBA background. Raw panels
are kept outside the take, so this correction is reversible and repeatable.
"""
import argparse
import json
import shutil
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[4]


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--take', default='scu_shop', help='Take name under SteamStore/_work/clips')
    ap.add_argument('--raw-dir', type=Path, help='Raw panel backups (relative paths use the project root)')
    args = ap.parse_args()
    take = ROOT / 'SteamStore/_work/clips' / args.take
    human = args.take == 'scu_shop_human'
    raw = args.raw_dir or ROOT / 'SteamStore/_work/tiktok_scraper' / ('shop_ui_raw_human' if human else 'shop_ui_raw')
    if not raw.is_absolute():
        raw = ROOT / raw
    raw.mkdir(parents=True, exist_ok=True)
    count = 0
    for src in sorted(take.glob('f*_ui.png')):
        backup = raw / src.name
        if not backup.exists():
            shutil.copy2(src, backup)
        pixels = np.array(Image.open(backup).convert('RGBA'))
        # Top-left is always outside the centered card. A zero alpha means the
        # shop has not opened yet; leave those frames completely transparent.
        base = pixels[10, 10].copy()
        if base[3] > 5:
            missing = pixels[:, :, 3] < base[3]
            pixels[missing] = base
            Image.fromarray(pixels).save(src)
            count += 1
    report = {'panels_extended': count, 'raw_dir': str(raw), 'take': str(take)}
    report_name = 'shop_ui_correction_human.json' if human else 'shop_ui_correction.json'
    (raw.parent / report_name).write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps(report))


if __name__ == '__main__':
    main()
