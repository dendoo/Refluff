// #01 pre-take: squeegee POWER 6 (leaves w 0.264 behind the blade instead of ~0.31: a lighter damp band; not in a HUD
// shot), then the gates: lane water / murk (SurfaceProbeJson over the lane's box), foam, spill ring.
CarpetCleaning.EconomyDebugApi.SetLevel(5, 0, 6);
CarpetCleaning.CarpetDebugApi.SetTool(5);
return "lane " + CarpetCleaning.CarpetDebugApi.SurfaceProbeJson(0.10f, 0.0f, 1.0f, 0.45f) + " | spill " + CarpetCleaning.CarpetDebugApi.PlowSpillJson();
