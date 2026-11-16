#!/bin/bash
# head.sh U V RAISE SPINFRAMES -> C# snippet: pin brush working at (U,V), spin up, raise by RAISE m, glossy body
cat <<CS
CarpetCleaning.CarpetDebugApi.PinToolHead(4, ${1}f, ${2}f, true);
CarpetCleaning.CarpetDebugApi.RunFrames($4);
UnityEngine.Transform bt = null; foreach (UnityEngine.Transform c in CarpetCleaning.CarpetDebugApi.Heads.transform) if (c.gameObject.activeSelf) bt = c;
bt.localPosition = new UnityEngine.Vector3(bt.localPosition.x, 0.010f + ${3}f, bt.localPosition.z);
var mpb = new UnityEngine.MaterialPropertyBlock(); mpb.SetFloat("roughnessFactor", 0.28f);
foreach (UnityEngine.Transform c in bt) if (c.name != "Spin") foreach (var rr in c.GetComponentsInChildren<UnityEngine.Renderer>(true)) rr.SetPropertyBlock(mpb);
CS
