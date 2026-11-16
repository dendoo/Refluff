import math
P=math.radians(55); F=540/math.tan(math.radians(22.5))
CAM=(0,1.5,-1.12)
def s2uv(px,py,H=0.015):
    x=(px-960)/F; y=(540-py)/F
    d=(x, y*math.cos(P)-math.sin(P), y*math.sin(P)+math.cos(P))
    t=(CAM[1]-H)/-d[1]
    wx=CAM[0]+t*d[0]; wz=CAM[2]+t*d[2]
    return wx/1.2+0.5, wz/1.5+0.5
def uv2s(u,v,H=0.015):
    wx=(u-0.5)*1.2; wz=(v-0.5)*1.5
    rel=(wx-CAM[0],H-CAM[1],wz-CAM[2])
    # camera basis
    r=(1,0,0); up=(0,math.cos(P),math.sin(P)); fw=(0,-math.sin(P),math.cos(P))
    dot=lambda a,b:sum(i*j for i,j in zip(a,b))
    zc=dot(rel,fw); xc=dot(rel,r)/zc; yc=dot(rel,up)/zc
    return 960+xc*F, 540-yc*F, zc
if __name__=='__main__':
    import sys
    for p in [(618,157),(1300,157),(418,1040),(1502,1040),(800,540),(590,790),(700,220),(1115,750)]:
        print(p, [round(a,3) for a in s2uv(*p)])
