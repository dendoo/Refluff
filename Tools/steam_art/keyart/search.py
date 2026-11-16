import sys, math, numpy as np
from lensdof import unity_rot
def corners_ok(cam, aspect, margin=0.01, plane=0.0, pts=((-1,1),(1,1),(-1,-1),(1,-1),(0,1),(-1,0),(1,0))):
    px,py,pz,rx,ry,rz,fov = cam
    R = unity_rot(rx,ry,rz); t = math.tan(math.radians(fov)/2)
    for sx,sy in pts:
        d = R @ np.array([sx*t*aspect, sy*t, 1.0])
        if d[1] >= -1e-6: return False
        k = (plane-py)/d[1]; x = px+k*d[0]; z = pz+k*d[2]
        if not (-0.6+margin <= x <= 0.6-margin and -0.75+margin <= z <= 0.75-margin): return False
    return True
def proj(cam, aspect, p):
    px,py,pz,rx,ry,rz,fov = cam
    R = unity_rot(rx,ry,rz); t = math.tan(math.radians(fov)/2)
    v = R.T @ (np.array(p)-np.array([px,py,pz]))
    return ((v[0]/v[2]/(t*aspect)+1)/2, (1-v[1]/v[2]/t)/2, v[2])
def place(p, s, py, pitch, yaw, fov, aspect):
    R = unity_rot(pitch,yaw,0); t = math.tan(math.radians(fov)/2)
    d = R @ np.array([(s[0]*2-1)*t*aspect, (1-s[1]*2)*t, 1.0])
    if d[1] >= -1e-6: return None
    k = (p[1]-py)/d[1]            # p = cam + k d  -> cam = p - k d, with cam.y = py
    k = (py - p[1]) / -d[1]
    c = np.array(p) - k*d
    return [float(c[0]), py, float(c[2]), pitch, yaw, 0, fov]
if __name__ == "__main__":
    aspect=float(sys.argv[1]); fov=float(sys.argv[2]); tol=float(sys.argv[3])
    tgt=[(tuple(map(float,a.split(',')[:3])), tuple(map(float,a.split(',')[3:5]))) for a in sys.argv[4:]]
    best=[]
    for py in np.arange(0.15,1.6,0.02):
      for pitch in np.arange(float(__import__("os").environ.get("PMIN",15)),86,1.0):
        for yaw in (np.arange(-70,71,2.0) if "YAWS" not in __import__("os").environ else np.arange(*map(float, __import__("os").environ["YAWS"].split(","))))  :
          cam = place(tgt[0][0], tgt[0][1], py, pitch, yaw, fov, aspect)
          if cam is None: continue
          err=0; bad=False
          for p,(sx,sy) in tgt[1:]:
              x,y,d = proj(cam,aspect,p)
              if d<=0.05: bad=True;break
              err += (x-sx)**2+(y-sy)**2
          if bad or err>tol: continue
          if not corners_ok(cam,aspect): continue
          d = proj(cam,aspect,tgt[0][0])[2]
          best.append((round(d,3),round(err,4),[round(float(v),3) for v in cam]))
    best.sort()
    for b in best[:10]: print(b)
    print(len(best))
