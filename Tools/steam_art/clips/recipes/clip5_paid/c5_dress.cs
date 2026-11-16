// clip5_paid: HUD dressing as reshoot_ho e3_pay (screenshot 04): the job's starting soil (CarpetSimulation._initStats: mud 0.22,
// grit 0.10, stain 0.06 -> V 0.302 = "Medium soil", par 3:32) and the job clock (Economy._clockAnchor) so that it reads 3:08 when
// the take hands over after its 0.40 s pre-roll. Run in the SAME eval as SeqBegin (no frames in between).
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var eco = CarpetCleaning.EconomyDebugApi.Eco; var sim = CarpetCleaning.CarpetDebugApi.Sim;
var fi = typeof(CarpetCleaning.CarpetSimulation).GetField("_initStats", bf);
object st = fi.GetValue(sim);
st.GetType().GetField("mud").SetValue(st, 0.22f);
st.GetType().GetField("grit").SetValue(st, 0.10f);
st.GetType().GetField("stain").SetValue(st, 0.06f);
st.GetType().GetField("valid").SetValue(st, true);
fi.SetValue(sim, st);
typeof(CarpetCleaning.Economy).GetField("_clockAnchor", bf).SetValue(eco, sim.SimTime - 187.6f);
