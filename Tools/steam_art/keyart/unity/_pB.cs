var keep = new System.Collections.Generic.HashSet<UnityEngine.Renderer>(CarpetCleaning.CarpetDebugApi.Heads.GetComponentsInChildren<UnityEngine.Renderer>(false));
var sw=System.Diagnostics.Stopwatch.StartNew(); string res="";
foreach (var r in keep) r.enabled = false;
try { CarpetCleaning.StoreShotApi.RenderView("keyart/raw3/hero1v2_nohead.png", 8640, 2790, 0.417f, 0.485f, -0.79f, 34f, -42f, 0f, 15f); } finally { foreach (var r in keep) r.enabled = true; }
return "B ms=" + sw.ElapsedMilliseconds + " keep=" + keep.Count;
