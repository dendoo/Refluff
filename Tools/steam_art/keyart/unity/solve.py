import sys, math, numpy as np
sys.path.insert(0, r"C:/UnityProjects/CarpetCleaning/SteamStore/_work/keyart")
from search import place, proj, corners_ok
from hits import hits
# solve.py aspect fov  bx by bz sx sy  pitch yaw  dmin dmax
a = [float(x) for x in sys.argv[1:]]
aspect, fov, bx, by, bz, sx, sy, pitch, yaw, dmin, dmax = a
for py in np.arange(0.1, 1.6, 0.005):
    cam = place((bx, by, bz), (sx, sy), py, pitch, yaw, fov, aspect)
    if cam is None: continue
    d = proj(cam, aspect, (bx, by, bz))[2]
    if dmin <= d <= dmax:
        print([round(v, 3) for v in cam], "d=%.3f" % d, "onrug" if corners_ok(cam, aspect) else "OFF", hits(cam, aspect))
        break
