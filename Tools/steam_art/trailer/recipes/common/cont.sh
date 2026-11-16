#!/usr/bin/env bash
# cont.sh - continue a running take: Run until done, then SeqEnd until seq.json.
export MSYS_NO_PATHCONV=1
for i in $(seq 1 200); do
  r=$(unity command eval --code "return CarpetCleaning.StoreGifApi.Run(3500);" 2>&1 | tail -1)
  s=$(echo "$r" | grep -o '\\"totalFrames\\":[0-9]*' | head -1); e=$(echo "$r" | grep -o '\\"errors\\":[0-9]*' | head -1)
  echo -n "[$s $e] "
  echo "$r" | grep -q '\\"done\\":true' && break
  echo "$r" | grep -q 'no take' && { echo "no take"; exit 1; }
done
echo
for i in $(seq 1 20); do
  r=$(unity command eval --code "return CarpetCleaning.StoreGifApi.SeqEnd(4000);" 2>&1 | tail -1)
  echo "$r" | grep -q '\\"seqJson\\":true' && { echo "end: $(echo "$r" | grep -o '{\\"frames.*dir' | head -c 300)"; exit 0; }
done
echo "SeqEnd did not finish: ${r:0:300}"; exit 1
