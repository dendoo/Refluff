#!/usr/bin/env bash
# stills.sh 'V("name", px,py,pz, tx,ty,tz, fov, focusM);' ...  - look_on.inc + q_stills.body with the given views, one eval.
here="$(cd "$(dirname "$0")" && pwd)"
tmp="$(cygpath -u "$TEMP")/tt_stills_$$.cs"
views="$*"
{ cat "$here/look_on.inc"; python - "$(cygpath -w "$here/q_stills.body")" "$views" <<'PY'
import sys
print(open(sys.argv[1], encoding='utf-8').read().replace('STILLS', sys.argv[2]))
PY
} > "$tmp"
bash "$here/../../trailer/recipes/common/ev.sh" "$tmp" 1500
rm -f "$tmp"
