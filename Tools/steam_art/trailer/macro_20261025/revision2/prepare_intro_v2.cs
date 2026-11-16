// Run stage.cs from the parent folder first; preserve a fresh pre-session save backup.
// This is a temporary capture transition explicitly requested by the owner.
CarpetCleaning.CarpetDebugApi.ResetWithProfile(394,.70f,0,.40f,.85f,.12f);
var ad=System.AppDomain.CurrentDomain;var sim=CarpetCleaning.CarpetDebugApi.Sim;var rt=sim.Dirt;
var tex=new UnityEngine.Texture2D(rt.width,rt.height,UnityEngine.TextureFormat.RGBAFloat,false,true);
tex.name="MacroIntroDirtRamp";tex.hideFlags=UnityEngine.HideFlags.HideAndDontSave;tex.filterMode=UnityEngine.FilterMode.Point;
var old=UnityEngine.RenderTexture.active;UnityEngine.RenderTexture.active=rt;
tex.ReadPixels(new UnityEngine.Rect(0,0,rt.width,rt.height),0,0);tex.Apply(false,false);UnityEngine.RenderTexture.active=old;
var target=tex.GetPixels();ad.SetData("mfDirtTarget",target);ad.SetData("mfDirtPixels",new UnityEngine.Color[target.Length]);ad.SetData("mfDirtTex",tex);
tex.SetPixels((UnityEngine.Color[])ad.GetData("mfDirtPixels"));tex.Apply(false,false);UnityEngine.Graphics.Blit(tex,rt);
var cr=UnityEngine.Object.FindFirstObjectByType<CarpetCleaning.CarpetRenderer>();var sz=CarpetCleaning.CarpetSimulation.CarpetSizeM;
var mesh=CarpetCleaning.ShellMeshBuilder.BuildShells(sz.x,sz.y,96,cr.shellSubdivX,cr.shellSubdivZ,cr.pileHeightM);
mesh.name="MacroIntroReturn96";mesh.hideFlags=UnityEngine.HideFlags.HideAndDontSave;ad.SetData("mfCaptureMesh96",mesh);
CarpetCleaning.CarpetDebugApi.UnpinToolHead();var tools=CarpetCleaning.CarpetDebugApi.Tools;
tools.ScriptedPointer=true;tools.ScriptedUv=new UnityEngine.Vector2(-5,-5);tools.ScriptedDown=false;tools.ScriptedRight=false;
UnityEditor.EditorApplication.Step();return "target dirt snapshot, clean ramp start, pointer parked";

