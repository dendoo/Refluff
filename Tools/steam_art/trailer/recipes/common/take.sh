#!/usr/bin/env bash
# take.sh TAKE_EVAL.cs  - start a StoreGifApi take with the pointer parked (the eval queues it and returns SeqBegin + its
# first Run), then cont.sh: Run until done, SeqEnd until seq.json is written.
export MSYS_NO_PATHCONV=1
here="$(cd "$(dirname "$0")" && pwd)"
tmp="$(cygpath -u "$TEMP")/take_$$.cs"
cat "$here/park.inc" "$1" > "$tmp"
r=$(bash "$here/ev.sh" "$tmp" 2000); rm -f "$tmp"
echo "start: ${r:0:300}"
case "$r" in ERR*|RAW*|'{"error"'*) exit 1;; esac
bash "$here/cont.sh"
