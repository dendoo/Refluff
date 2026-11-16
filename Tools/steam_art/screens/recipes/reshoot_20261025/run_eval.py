"""Run a saved Unity eval recipe; never recompile or refresh."""
import json, subprocess, sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[5]
BASH=r'C:/Program Files/Git/bin/bash.exe'
def run(path):
    p=Path(path).resolve().as_posix()
    command='export MSYS_NO_PATHCONV=1; unity command eval_file --file "'+p+'" --format json'
    r=subprocess.run([BASH,'-lc',command],cwd=ROOT,capture_output=True,text=True,encoding='utf-8',errors='replace')
    try: j=json.loads(r.stdout)
    except Exception: raise RuntimeError(r.stdout+r.stderr)
    if not j.get('success') or not j.get('data',{}).get('success',True): raise RuntimeError(json.dumps(j,ensure_ascii=False))
    v=j.get('data',{}).get('result')
    if isinstance(v,dict) and v.get('success') is False: raise RuntimeError(json.dumps(v,ensure_ascii=False))
    if isinstance(v,dict) and 'result' in v: v=v['result']
    print(Path(p).name+': '+str(v),flush=True)
    return v
if __name__=='__main__':
    for p in sys.argv[1:]: run(p)
