// read-only probe: per-cell water murk (CarpetWater model) and film coverage over the carpet or a uv rect
var sim = CarpetCleaning.CarpetDebugApi.Sim;
var st = sim.RefreshStatsSync();
var rd = typeof(CarpetCleaning.CarpetDebugApi).GetMethod("ReadFloatRT", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
var flu = (UnityEngine.Color[])rd.Invoke(null, new object[]{ sim.Fluid });
var fib = (UnityEngine.Color[])rd.Invoke(null, new object[]{ sim.Fiber });
var dir = (UnityEngine.Color[])rd.Invoke(null, new object[]{ sim.Dirt });
var soa = (UnityEngine.Color[])rd.Invoke(null, new object[]{ sim.Soap });
int W = sim.Fluid.width, H = sim.Fluid.height;
System.Func<float,string> F = x => x.ToString("G3", System.Globalization.CultureInfo.InvariantCulture);
var fi = typeof(CarpetCleaning.CarpetSimulation).GetField("_initStats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var i0 = (CarpetCleaning.SimStats)fi.GetValue(sim);
string outS = "";
float[][] rects = { new float[]{0f,0f,1f,1f}, new float[]{0.5f,0f,1f,0.5f}, new float[]{0f,0.5f,1f,1f} };
foreach (var r in rects) {
  int x0 = (int)(r[0]*W), x1 = (int)(r[2]*W), y0 = (int)(r[1]*H), y1 = (int)(r[3]*H);
  double sh=0, sw=0, ss=0, sF=0, sm=0; int n=0, nH3=0, nH9=0, nBrown=0, nM4=0, nF=0;
  for (int y=y0;y<y1;y++) for (int x=x0;x<x1;x++) {
    int i=y*W+x; float h=flu[i].r, s=flu[i].a, w=fib[i].a, Fo=soa[i].g;
    var d=dir[i]; float pd = d.g + 0.5f*d.r + 0.5f*d.b + 0.5f*d.a;
    float wt = h + w*6f; float murk = Mathf.Clamp01((s/(wt+1e-3f) + pd/(wt+1e-3f))/0.4f);
    sh+=h; sw+=w; ss+=s; sF+=Fo; sm+=murk; n++;
    if (h>0.3f) nH3++; if (h>0.9f) nH9++; if (murk>0.4f) nM4++; if (h>0.9f && murk>0.4f) nBrown++; if (Fo>0.05f) nF++;
  }
  double k=1.0/Mathf.Max(n,1);
  outS += "[" + r[0]+","+r[1]+"-"+r[2]+","+r[3]+"] h "+F((float)(sh*k))+" w "+F((float)(sw*k))+" s "+F((float)(ss*k))+" F "+F((float)(sF*k))+" murk "+F((float)(sm*k))
       +" h>.3 "+F((float)(nH3*k))+" h>.9 "+F((float)(nH9*k))+" murk>.4 "+F((float)(nM4*k))+" BROWN "+F((float)(nBrown*k))+" foamC "+F((float)(nF*k))+" | ";
}
return outS + "crustR " + F(st.crust/Mathf.Max(i0.crust,1e-6f)) + " mudR " + F(st.mud/Mathf.Max(i0.mud,1e-6f)) + " stainR " + F(st.stain/Mathf.Max(i0.stain,1e-6f))
  + " grit " + F(st.grit) + " slurry " + F(st.slurry) + " fd " + F(st.foamDirt) + " pooled " + F(st.pooledCells) + " soap " + F(sim.SoapLeft) + " foam " + F(sim.FoamMean) + " wet " + F(sim.Wetness) + " | " + sim.Hint
  + " | floor " + CarpetCleaning.CarpetDebugApi.FloorStatsJson();
