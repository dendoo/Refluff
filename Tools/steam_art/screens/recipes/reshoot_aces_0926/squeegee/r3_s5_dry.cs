// dryer pass over the whole rug (run 7x): collapses the suds (their dirt drops back into the water) and dries the pile right
// out, so the dissolved grime settles back into the fibres as mud (K_Chemistry redeposition)
UnityEditor.EditorApplication.isPaused = true;
CarpetCleaning.CarpetDebugApi.Zigzag(7, 0.03f,0.03f, 0.97f,0.97f, 10, 50, 1f);
return "ok";
