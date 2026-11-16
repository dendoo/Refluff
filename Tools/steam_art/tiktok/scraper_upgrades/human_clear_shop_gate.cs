// Patch an already-prepared ephemeral rig without replacing its restoration backups.
// Fresh installs use HumanStageStrong's RealSteps(1) directly.
var ad = System.AppDomain.CurrentDomain;
if (ad.GetData("scu.human.shopgatepatched.v3") == null)
{
    var replay = (System.Func<string>)ad.GetData("scu.human.replaystrong");
    ad.SetData("scu.human.replaystrong", (System.Func<string>)(() =>
    {
        ((System.Func<string>)ad.GetData("scu.human.stagestrong"))();
        var t = CarpetCleaning.CarpetDebugApi.Tools;
        t.ScriptedPointer = true;
        t.ScriptedUv = new UnityEngine.Vector2(-5f, -5f);
        t.ScriptedDown = false;
        t.ScriptedRight = false;
        UnityEditor.EditorApplication.isPaused = true;
        UnityEditor.EditorApplication.Step();
        ad.SetData("scu.human.initialheading.strong",90f);
        var result = replay();
        CarpetCleaning.ToolTuning.Set(CarpetCleaning.ToolId.Scraper, 20f, 5f);
        return result;
    }));
    ad.SetData("scu.human.shopgatepatched.v3", true);
}
return "Shop release gate consumed before the human replay; stored motion unchanged";
