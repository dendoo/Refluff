var fx = UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.CelebrationFx>();
if (fx != null && fx.sparkles != null) fx.sparkles.Clear(true);
return fx == null ? "nofx" : ("cleared alive=" + fx.sparkles.particleCount);
