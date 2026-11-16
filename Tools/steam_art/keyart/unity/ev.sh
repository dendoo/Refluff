#!/bin/bash
# usage: ev.sh file.cs
export MSYS_NO_PATHCONV=1
D="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -W 2>/dev/null || pwd)"
unity command eval_file --file "$D/$1" 2>&1 | tail -1 | cut -c1-2500
