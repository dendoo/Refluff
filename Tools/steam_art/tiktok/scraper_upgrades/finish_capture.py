"""Record staging checks, restore the Editor rig, stop Play, restore the editor save byte-for-byte."""
import argparse
import hashlib
import json
import shutil
from pathlib import Path

from drive import ROOT, WORK, action, ev, unity


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--human', action='store_true', help='Restore the human-movement retake and its separate save backup')
    args = ap.parse_args()
    save = Path.home() / 'AppData/LocalLow/VahovGames/Refluff/carpet_save_editor.json'
    report_dir = WORK / 'retake' if args.human else WORK
    backup = report_dir / 'carpet_save_editor.before.json'
    report_path = report_dir / ('capture_report_human.json' if args.human else 'capture_report.json')
    if not backup.exists():
        raise SystemExit('Original save backup missing; refusing to overwrite save')
    report = {'save_original_sha256': sha(backup), 'human': args.human, 'errors': []}

    def attempt(label, operation):
        try:
            value = operation()
            report[label] = value
            return value
        except Exception as exc:
            report['errors'].append({'step': label, 'type': type(exc).__name__, 'message': str(exc)})
            return None

    try:
        attempt('final_stats', lambda: json.loads(ev('CarpetCleaning.CarpetDebugApi.Sim.RefreshStatsSync(); return CarpetCleaning.CarpetDebugApi.StatsJson();')))
        attempt('compute_messages', lambda: int(ev('var cs=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.ComputeShader>("Assets/CarpetCleaning/Editor/_CaptureOnly/ScuStage.compute"); return UnityEditor.ShaderUtil.GetComputeShaderMessages(cs).Length.ToString();')))
        report['takes'] = {}
        names = ['scu_arrival'] + (['scu_weak_human', 'scu_shop_human', 'scu_power_human', 'scu_reveal_human']
                                   if args.human else ['scu_weak', 'scu_shop', 'scu_power', 'scu_reveal'])
        for name in names:
            try:
                data = json.loads((ROOT / 'SteamStore/_work/clips' / name / 'seq.json').read_text(encoding='utf-8'))
                report['takes'][name] = {k: data.get(k) for k in ['fps', 'steps', 'frames', 'stepDt', 'errors', 'audioLog']}
            except Exception as exc:
                report['errors'].append({'step': 'take:' + name, 'type': type(exc).__name__, 'message': str(exc)})
    finally:
        if args.human:
            attempt('human_uninstall', lambda: action('human.uninstall'))
            attempt('human_heading_restore', lambda: ev('System.AppDomain.CurrentDomain.SetData("scu.human.initialheading.strong", null); return "Replay heading override removed";'))
        attempt('restore', lambda: action('restore'))
        attempt('editor_unpause', lambda: ev('UnityEditor.EditorApplication.isPaused=false; return "unpaused";'))
        attempt('editor_stop', lambda: unity('editor_stop'))
        attempt('save_copy', lambda: str(shutil.copy2(backup, save)))
        attempt('save_restored_sha256', lambda: sha(save))
        report['save_restored_identical'] = report['save_original_sha256'] == report.get('save_restored_sha256')
        if not report['save_restored_identical']:
            report['errors'].append({'step': 'save_hash', 'type': 'RuntimeError', 'message': 'Save hash differs after restore'})
        report_dir.mkdir(parents=True, exist_ok=True)
        report_path.write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps(report))
    if report['errors']:
        raise RuntimeError('Capture cleanup/checks reported errors: ' + '; '.join(
            item['step'] + ': ' + item['message'] for item in report['errors']))


if __name__ == '__main__':
    main()
