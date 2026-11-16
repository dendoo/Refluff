// debug A/B only (NOT part of the take): renders the current sim state under each rug picture by swapping the fibre
// material's texture, then restores the handover's own picture
var ho = UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.CarpetHandover>();
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var mat = (UnityEngine.Material)typeof(CarpetCleaning.CarpetHandover).GetField("_fiber", bf).GetValue(ho);
var fit = typeof(CarpetCleaning.CarpetHandover).GetMethod("FitPicture", bf);
string r = "";
foreach (var p in ho.pictures) {
  if (p == null) continue;
  mat.SetTexture("_PictureTex", p); fit.Invoke(ho, new object[]{ p });
  r += CarpetCleaning.StoreShotApi.RenderScene("test/ab_" + p.name + ".png", 3840, 2160).Length + " ";
}
var own = ho.pictures[ho.PictureIndex]; mat.SetTexture("_PictureTex", own); fit.Invoke(ho, new object[]{ own });
return r + " restored " + own.name;
