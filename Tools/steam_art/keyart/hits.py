import sys, math, numpy as np
from lensdof import unity_rot
def hits(cam, aspect):
    px,py,pz,rx,ry,rz,fov = cam
    R = unity_rot(rx,ry,rz); t = math.tan(math.radians(fov)/2); out={}
    for name,(sx,sy) in {"TL":(-1,1),"TC":(0,1),"TR":(1,1),"ML":(-1,0),"MR":(1,0),"BL":(-1,-1),"BR":(1,-1)}.items():
        d = R @ np.array([sx*t*aspect, sy*t, 1.0])
        if d[1] >= -1e-6: out[name]="sky"; continue
        k=(0-py)/d[1]; x=px+k*d[0]; z=pz+k*d[2]
        u=(x+0.6)/1.2; v=(z+0.75)/1.5
        out[name]=f"u{u:.2f} v{v:.2f}" + ("" if 0<=u<=1 and 0<=v<=1 else " FLOOR")
    return out
if __name__=="__main__":
    cam=[float(x) for x in sys.argv[1:8]]; print(hits(cam,float(sys.argv[8])))
