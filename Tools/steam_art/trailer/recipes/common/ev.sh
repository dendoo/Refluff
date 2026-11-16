#!/usr/bin/env bash
# ev.sh FILE.cs [maxchars]  - run one eval file in the live Editor, print only the result string (or the error).
export MSYS_NO_PATHCONV=1
f="$1"; n="${2:-600}"
[ -f "$f" ] || { echo "no file $f"; exit 1; }
w=$(cygpath -w "$(realpath "$f")")
out=$(unity command eval_file --file "$w" 2>&1 | tail -1)
python - "$n" "$out" <<'PY'
import sys, json, re
n = int(sys.argv[1]); line = sys.argv[2]
i = line.find('{')
try:
    d = json.JSONDecoder().raw_decode(line[i:])[0]
    r = d.get('result'); 
    if not d.get('success', True) or d.get('diagnostics'):
        print('ERR', json.dumps(d.get('diagnostics'))[:n], str(r)[:n])
    else:
        print(str(r)[:n])
except Exception as e:
    print('RAW', line[:n])
PY
