import sys
from PIL import Image
D='C:/Users/denis/AppData/Local/Temp/claude/c--UnityProjects-CarpetCleaning/89732399-1dce-4f4b-ab46-20a0ca01c488/scratchpad/fx/reshoot_d/'
# usage: crop.py out.png x0 y0 x1 y1 scale src1 [src2 ...]  (paths relative to D)
out=sys.argv[1]; x0,y0,x1,y1=map(int,sys.argv[2:6]); sc=float(sys.argv[6]); srcs=sys.argv[7:]
ims=[Image.open(D+s).convert('RGB').crop((x0,y0,x1,y1)) for s in srcs]
w=int((x1-x0)*sc); h=int((y1-y0)*sc)
c=Image.new('RGB',(w*len(ims)+8*(len(ims)-1),h),(255,255,255))
for i,im in enumerate(ims): c.paste(im.resize((w,h),Image.LANCZOS),(i*(w+8),0))
c.save(D+out); print(c.size)
