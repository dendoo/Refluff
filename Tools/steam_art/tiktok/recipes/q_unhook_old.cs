// one-off: drop the first version's Application.onBeforeRender camera hook
var ad = System.AppDomain.CurrentDomain;
var old = ad.GetData("ttCamHook") as UnityEngine.Events.UnityAction;
if (old != null) { UnityEngine.Application.onBeforeRender -= old; ad.SetData("ttCamHook", null); return "removed"; }
return "none";
