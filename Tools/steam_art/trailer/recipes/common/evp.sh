#!/usr/bin/env bash
# evp.sh FILE.cs [...]  - run eval files with the scripted pointer parked off the play area first (paused Steps read the OS
# mouse otherwise). Prints "== name: result" per file; stops on an error.
here="$(cd "$(dirname "$0")" && pwd)"
tmp="$(cygpath -u "$TEMP")/evp_$$.cs"
for f in "$@"; do
  cat "$here/park.inc" "$f" > "$tmp"
  r=$(bash "$here/ev.sh" "$tmp" 700)
  echo "== $(basename "$f" .cs): $r"
  case "$r" in ERR*|RAW*) rm -f "$tmp"; exit 1;; esac
done
rm -f "$tmp"
