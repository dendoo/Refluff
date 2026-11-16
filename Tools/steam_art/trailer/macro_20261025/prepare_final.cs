// Off-camera continuation on the same carpet after mf07_dryer, before mf08_reveal.
// Execute the blocks separately through execute_code (codedom), no AssetDatabase refresh.
// This uses real tool stamps/StepAll; it does not reset the carpet or alter simulation settings.
// Initial preparation used 2.5 POWER / 2 AREA; the finishing passes below use exact level-6 multipliers.
CarpetCleaning.ToolTuning.SetNeutral(false);
for(int i=0;i<8;i++)CarpetCleaning.ToolTuning.Set((CarpetCleaning.ToolId)i,2.5f,2f);
CarpetCleaning.CarpetDebugApi.Zigzag(1,-.02f,.025f,1.02f,.975f,12,25);
CarpetCleaning.CarpetDebugApi.Zigzag(2,-.02f,.025f,1.02f,.975f,12,25,.5f);
CarpetCleaning.CarpetDebugApi.Zigzag(3,-.02f,.025f,1.02f,.975f,12,10,.5f);
CarpetCleaning.CarpetDebugApi.Zigzag(4,-.02f,.025f,1.02f,.975f,16,45);
CarpetCleaning.CarpetDebugApi.Zigzag(5,-.08f,.025f,1.08f,.975f,10,45);
CarpetCleaning.CarpetDebugApi.Zigzag(6,-.06f,.025f,1.06f,.975f,12,75);
CarpetCleaning.CarpetDebugApi.Zigzag(6,-.06f,.025f,1.06f,.975f,12,90);
CarpetCleaning.CarpetDebugApi.Zigzag(7,-.06f,.025f,1.06f,.975f,9,120);
CarpetCleaning.CarpetDebugApi.RunFrames(360);
CarpetCleaning.CarpetDebugApi.Zigzag(3,-.02f,.025f,1.02f,.975f,12,30);
CarpetCleaning.CarpetDebugApi.Zigzag(4,-.02f,.025f,1.02f,.975f,16,90);
CarpetCleaning.CarpetDebugApi.RunFrames(1200);
int[] a={3,1,2,3,0,3,0,0};
for(int i=0;i<8;i++)CarpetCleaning.ToolTuning.Set((CarpetCleaning.ToolId)i,UnityEngine.Mathf.Pow(1.15f,6-a[i]),3f);
CarpetCleaning.CarpetDebugApi.Zigzag(4,-.04f,.02f,1.04f,.98f,18,90);
CarpetCleaning.CarpetDebugApi.RunFrames(1200);
CarpetCleaning.CarpetDebugApi.Zigzag(6,-.06f,.02f,1.06f,.98f,12,120);
CarpetCleaning.CarpetDebugApi.Zigzag(6,-.06f,.02f,1.06f,.98f,12,120);
CarpetCleaning.CarpetDebugApi.Zigzag(7,-.06f,.02f,1.06f,.98f,10,150);
CarpetCleaning.CarpetDebugApi.RunFrames(480);
return CarpetCleaning.CarpetDebugApi.StatsJson();
