#!/bin/bash
# usage: runh.sh <variant>   (runs base h1..h6, then hv_<variant>.cs, composes, saves to v<variant>/)
export MSYS_NO_PATHCONV=1
cd /c/UnityProjects/CarpetCleaning
W="C:/Users/denis/AppData/Local/Temp/claude/c--UnityProjects-CarpetCleaning/89732399-1dce-4f4b-ab46-20a0ca01c488/scratchpad/fx/reshoot_d"
for s in h1 h2 h3 h4 h5 h6; do unity command eval_file --file "$W/$s.cs" | grep -o '"result":"[^}]*' | cut -c1-200; done
unity command eval_file --file "$W/hv_$1.cs" | grep -o '"result":.*' | cut -c1-900
python Tools/steam_art/compose_screens.py 03_hose_spill
mkdir -p /c/Users/denis/AppData/Local/Temp/claude/c--UnityProjects-CarpetCleaning/89732399-1dce-4f4b-ab46-20a0ca01c488/scratchpad/fx/reshoot_d/v$1
cp SteamStore/_work/screens/03_hose_spill*.png SteamStore/_work/shots/03_hose_spill_* /c/Users/denis/AppData/Local/Temp/claude/c--UnityProjects-CarpetCleaning/89732399-1dce-4f4b-ab46-20a0ca01c488/scratchpad/fx/reshoot_d/v$1/
unity command eval --code "CarpetCleaning.CarpetDebugApi.SaveDebugView(\"fluid\", \"$W/v$1/fluid.png\"); return \"ok\";" > /dev/null
python -c "
from PIL import Image; import numpy as np
im=np.asarray(Image.open(r'$W/v$1/fluid.png')).astype(float)
h=im[...,0]/255*10
print('h>0.9',(h>0.9).mean(),'h>2.5',(h>2.5).mean(),'h>5',(h>5).mean())
v=(np.clip(h/6,0,1)*255).astype(np.uint8)
Image.fromarray(v).resize((384*2,480*2),Image.NEAREST).save(r'$W/v$1/h.png')
"
