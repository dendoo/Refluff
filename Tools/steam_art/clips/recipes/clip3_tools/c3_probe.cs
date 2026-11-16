// clip3 diagnostic: lane state (u 0.62-0.86 red field part, u 0.88-0.98 border part) + a crust reference patch above the lane
return "red " + CarpetCleaning.CarpetDebugApi.SurfaceProbeJson(0.62f,0.22f,0.86f,0.30f) + " border " + CarpetCleaning.CarpetDebugApi.SurfaceProbeJson(0.88f,0.22f,0.98f,0.30f) + " ref " + CarpetCleaning.CarpetDebugApi.SurfaceProbeJson(0.62f,0.36f,0.86f,0.42f);
