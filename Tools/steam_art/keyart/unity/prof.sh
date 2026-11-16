#!/bin/bash
D="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -W 2>/dev/null || pwd)"
$D/ev.sh wv.cs > /dev/null
cd /c/UnityProjects/CarpetCleaning/SteamStore/_work/keyart/test3; python -c "
from PIL import Image
import numpy as np
d=np.asarray(Image.open('dv_dirt.png').convert('RGBA')).astype(float)
f=np.asarray(Image.open('dv_fluid.png').convert('RGBA')).astype(float)
def p(a,n): print(n.ljust(6),' '.join(f'{a[:,int(i*383/20)].mean():3.0f}' for i in range(21)))
print('u     ',' '.join(f'{i*5:3d}' for i in range(21)))
p(d[:,:,1],'mud'); p(d[:,:,2],'stain'); p(f[:,:,0],'h'); p(f[:,:,3],'C')
"
