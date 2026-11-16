// wet-vac pass over the dried rug (run 5x): the air pulled through the pile flushes the leftover detergent out
// (WETVAC_FLUSH), while the settled grime stays in the fibres as mud; without it the final hose would whip the soap into suds
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Zigzag(6, -0.01f,0.02f, 1.01f,0.98f, 10, 40, 1f);
return "ok";
