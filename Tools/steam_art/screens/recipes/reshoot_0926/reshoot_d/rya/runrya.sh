#!/bin/bash
export MSYS_NO_PATHCONV=1
cd /c/UnityProjects/CarpetCleaning
W="C:/Users/denis/AppData/Local/Temp/claude/c--UnityProjects-CarpetCleaning/89732399-1dce-4f4b-ab46-20a0ca01c488/scratchpad/fx/reshoot_d/rya"
for s in "$@"; do echo "== $s"; unity command eval_file --file "$W/$s.cs" | grep -o '"result":.*' | cut -c1-400; done
