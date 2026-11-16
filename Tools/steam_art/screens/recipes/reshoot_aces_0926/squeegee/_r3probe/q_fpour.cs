// read-only: floor water in a CMM box right of the right rim (x 1200..1800, y 150..1350) and carpet C in the lane band
var fl = CarpetCleaning.CarpetDebugApi.Floor; var sim = CarpetCleaning.CarpetDebugApi.Sim;
var rd = typeof(CarpetCleaning.CarpetDebugApi).GetMethod("ReadFloatRT", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
var f = (UnityEngine.Color[])rd.Invoke(null, new object[]{ fl.Fluid });
int N = fl.Fluid.width; float cell = 3600f / N;
System.Func<float,string> F = x => x.ToString("G3", System.Globalization.CultureInfo.InvariantCulture);
double sh=0, ss=0; int n=0; float hmax=0; int n3=0, n12=0, n2=0, n4=0;
for (float y=150; y<1350; y+=cell) for (float x=1203; x<1800; x+=cell) {
  int ix=(int)((x+1200f)/cell), iy=(int)((y+1050f)/cell); var c=f[iy*N+ix];
  if (c.r>0.05f){ sh+=c.r; ss+=c.a; n++; } hmax=Mathf.Max(hmax,c.r); if(c.r>0.3f)n3++; if(c.r>1.2f)n12++; if(c.r>2f)n2++; if(c.r>4f)n4++;
}
// carpet: mean C over the lower-left unwiped zone (u .05-.35, v .08-.35)
var flu = (UnityEngine.Color[])rd.Invoke(null, new object[]{ sim.Fluid }); var fib = (UnityEngine.Color[])rd.Invoke(null, new object[]{ sim.Fiber });
int W=sim.Fluid.width, H=sim.Fluid.height; double cs=0, ch=0; int cn=0;
for (int y=(int)(0.08f*H); y<(int)(0.35f*H); y++) for (int x=(int)(0.05f*W); x<(int)(0.35f*W); x++) { int i=y*W+x; cs+=flu[i].a/(flu[i].r+6f*fib[i].a+1e-3f); ch+=flu[i].r; cn++; }
return "floor wet cells " + n + " h mean " + F((float)(sh/Mathf.Max(n,1))) + " hmax " + F(hmax) + " C(s/h) " + F((float)(ss/System.Math.Max(sh,1e-6))) + " cells h>.3 " + n3 + " >1.2 " + n12 + " >2 " + n2 + " >4 " + n4
  + " | carpet LL zone C " + F((float)(cs/cn)) + " h " + F((float)(ch/cn));
