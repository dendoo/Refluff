var sb = new System.Text.StringBuilder();
var cr = UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.CarpetRenderer>();
sb.Append("CR ").Append(cr.name).Append(" pos=").Append(cr.transform.position.ToString("F3")).Append(" scale=").Append(cr.transform.lossyScale.ToString("F3")).Append(" parent=").Append(cr.transform.parent? cr.transform.parent.name:"-").Append("\n");
foreach (var r in cr.GetComponentsInChildren<UnityEngine.Renderer>()) sb.Append(" R ").Append(r.name).Append(" b=").Append(r.bounds.center.ToString("F3")).Append(" ext=").Append(r.bounds.extents.ToString("F3")).Append("\n");
CarpetCleaning.CarpetDebugApi.PinToolHead(4, 0.25f, 0.75f, true);
sb.Append(CarpetCleaning.CarpetDebugApi.ToolHeadJson());
return sb.ToString();
