#!/usr/bin/env bash
# run.sh NAME [NAME ...] - run recipes/NAME.cs through evp.sh one by one, printing the head and tail of each result.
export MSYS_NO_PATHCONV=1
for f in "$@"; do
  r=$(bash Tools/steam_art/trailer/recipes/common/evp.sh Tools/steam_art/tiktok/recipes/$f.cs)
  echo "${r:0:28} ... ${r: -300}"
done
