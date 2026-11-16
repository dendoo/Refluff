"""Drive only this film's ephemeral Unity rig; never refresh/recompile the project."""
import argparse
import json
import subprocess
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
WORK = ROOT / 'SteamStore/_work/tiktok_scraper'


def unity(command, **kwargs):
    args = ['unity', 'command', command, '--format', 'json']
    for key, value in kwargs.items():
        args += ['--' + key, str(value)]
    result = subprocess.run(args, cwd=ROOT, capture_output=True, text=True, encoding='utf-8', errors='replace')
    raw = result.stdout
    try:
        response = json.loads(raw[raw.index('{'):])
    except Exception:
        raise RuntimeError(raw + result.stderr)
    data = response.get('data', response)
    if result.returncode or not response.get('success', True) or not data.get('success', True):
        raise RuntimeError(json.dumps(response, ensure_ascii=False))
    result = data.get('result', response.get('result'))
    if isinstance(result, dict) and any(d.get('severity') == 'error' for d in result.get('diagnostics', [])):
        raise RuntimeError(json.dumps(result))
    if isinstance(result, dict) and result.get('success') is False:
        raise RuntimeError(json.dumps(result))
    if isinstance(result, dict) and 'result' in result and result['result'] is not None:
        result = result['result']
    return result


def ev(code):
    return unity('eval', code=code)


def action(name):
    return ev('return ((System.Func<string>)System.AppDomain.CurrentDomain.GetData("scu.' + name + '"))();')


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('action')
    ap.add_argument('--take', action='store_true')
    ap.add_argument('--dry-run', action='store_true')
    args = ap.parse_args()
    WORK.mkdir(parents=True, exist_ok=True)
    if args.action == 'install':
        print(json.dumps(unity('run_script', file=str(Path(__file__).with_name('CaptureRig.cs')),
                               dry_run=str(args.dry_run).lower()), ensure_ascii=False))
        return
    if args.action == 'eval':
        raise SystemExit('Use action names for reproducibility')
    start = time.monotonic()
    if args.action == 'continue':
        result = 'Continuing existing take'
    elif args.action.endswith('.cs'):
        result = unity('eval_file', file=str(Path(__file__).with_name(args.action)))
    else:
        result = action(args.action)
    print(result, flush=True)
    if not args.take:
        return
    log = []
    for i in range(1000):
        result = json.loads(ev('return CarpetCleaning.StoreGifApi.Run(2400);'))
        log.append(result)
        if result.get('errors') or 'error' in result:
            raise RuntimeError(result)
        if i % 8 == 0 or result.get('done'):
            print(json.dumps({'take': args.action, 'frames': result.get('totalFrames'),
                              'done': result.get('done'), 'elapsed': round(time.monotonic() - start, 1)}), flush=True)
        if result.get('done'):
            break
    else:
        raise RuntimeError('take exceeded maximum loops')
    for i in range(20):
        result = json.loads(ev('return CarpetCleaning.StoreGifApi.SeqEnd(2000);'))
        if result.get('seqJson') and not result.get('pendingPng'):
            break
    else:
        raise RuntimeError('pending PNGs did not finish')
    (WORK / (args.action + '_capture_log.json')).write_text(json.dumps(log, indent=2), encoding='utf-8')
    print(json.dumps(result), flush=True)


if __name__ == '__main__':
    main()
