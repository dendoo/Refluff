var sb = new System.Text.StringBuilder();
var C = System.Globalization.CultureInfo.InvariantCulture;
var d14 = CarpetCleaning.CarpetSimulation.RollDirt(14, 0.99f);
sb.Append("s14@0.99 spread=" + d14.spread.ToString("F2",C) + " dust=" + d14.dust.ToString("F2",C) + " mud=" + d14.mud.ToString("F2",C) + " crust=" + d14.crust.ToString("F2",C) + " spill=" + d14.spill.ToString("F2",C) + "\n");
for (int s = 1; s < 400; s++) {
  var d = CarpetCleaning.CarpetSimulation.RollDirt(s, 0.8f);
  if (d.crust == 0f && d.dust > 0.85f && d.mud > 0.45f && d.spread < 0.8f && d.spill > 0.4f)
    sb.Append(s + " spread=" + d.spread.ToString("F2",C) + " dust=" + d.dust.ToString("F2",C) + " mud=" + d.mud.ToString("F2",C) + " crust=" + d.crust.ToString("F2",C) + " spill=" + d.spill.ToString("F2",C) + "\n");
}
return sb.ToString();
