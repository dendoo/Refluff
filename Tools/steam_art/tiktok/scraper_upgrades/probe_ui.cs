var doc=CarpetCleaning.EconomyDebugApi.Hud.GetComponent<UnityEngine.UIElements.UIDocument>();
var root=doc.rootVisualElement;
var b=new System.Text.StringBuilder(CarpetCleaning.EconomyDebugApi.ShopJson());
foreach(string n in new[]{"hud","shop","shop-panel","shop-carpet-tools"}){
 var e=UnityEngine.UIElements.UQueryExtensions.Q(root,n); b.Append(" | ").Append(n).Append(" ").Append(e.layout).Append(" ").Append(e.resolvedStyle.display).Append(" op ").Append(e.resolvedStyle.opacity).Append(" visible ").Append(e.visible).Append(" ").Append(e.worldBound);
}
foreach(var e in UnityEngine.UIElements.UQueryExtensions.Q(root,"hud").Children())b.Append(" ; ").Append(e.name).Append("=").Append(e.resolvedStyle.display);
return b.ToString();
