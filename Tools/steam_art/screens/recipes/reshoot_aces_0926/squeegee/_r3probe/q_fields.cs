var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
string r = "";
foreach (var o in new object[]{ CarpetCleaning.CarpetDebugApi.Sim, CarpetCleaning.CarpetDebugApi.Floor }) {
  r += o.GetType().Name + ": ";
  foreach (var f in o.GetType().GetFields(bf)) {
    var t = f.FieldType;
    if (t == typeof(UnityEngine.RenderTexture) || t == typeof(UnityEngine.RenderTexture[]) || t == typeof(UnityEngine.ComputeBuffer) || t == typeof(UnityEngine.GraphicsBuffer) || t == typeof(int) || t == typeof(float) || t == typeof(bool) || t == typeof(double))
      r += f.Name + ":" + t.Name + " ";
  }
  r += " || ";
}
return r;
