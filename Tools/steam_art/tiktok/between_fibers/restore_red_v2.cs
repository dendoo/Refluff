var ad=System.AppDomain.CurrentDomain;
var a=ad.GetData("bfRedOriginal") as object[];
if(a!=null){var mat=a[0] as UnityEngine.Material;if(mat!=null){mat.SetTexture("_PictureTex",a[1] as UnityEngine.Texture);mat.SetVector("_PicOffset",(UnityEngine.Vector4)a[2]);mat.SetVector("_PicScale",(UnityEngine.Vector4)a[3]);}}
ad.SetData("bfRedOriginal",null);
return "original picture restored";
