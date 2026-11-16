var p=UnityEngine.Object.FindAnyObjectByType<CarpetCleaning.FirstPersonPlayer>(); var c=p.viewCamera;
var pos=p.transform.position; float yaw=p.Yaw,pitch=p.Pitch; bool occ=c.useOcclusionCulling;
var previous=c.targetTexture; var active=UnityEngine.RenderTexture.active;
var rt=new UnityEngine.RenderTexture(2560,1440,24,UnityEngine.RenderTextureFormat.DefaultHDR);
var encoded=new UnityEngine.RenderTexture(2560,1440,0,UnityEngine.RenderTextureFormat.ARGB32,UnityEngine.RenderTextureReadWrite.sRGB); bool prevSrgb=UnityEngine.GL.sRGBWrite; var tex=new UnityEngine.Texture2D(2560,1440,UnityEngine.TextureFormat.RGBA32,false);
try {
 c.useOcclusionCulling=false;p.DebugPose(2.8f,-3.3f,245.8f,-5.6f);rt.Create();c.targetTexture=rt;c.Render();
 encoded.Create();UnityEngine.GL.sRGBWrite=true;UnityEngine.Graphics.Blit(rt,encoded);UnityEngine.GL.sRGBWrite=prevSrgb;UnityEngine.RenderTexture.active=encoded;tex.ReadPixels(new UnityEngine.Rect(0,0,2560,1440),0,0);tex.Apply();
 System.IO.File.WriteAllBytes("C:/UnityProjects/CarpetCleaning/Assets/Screenshots/room_hdr_encoded.png",tex.EncodeToPNG());
 return new{format=rt.format.ToString(),srgb=rt.sRGB};
} finally {c.targetTexture=previous;UnityEngine.RenderTexture.active=active;rt.Release();encoded.Release();UnityEngine.Object.DestroyImmediate(encoded);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.GL.sRGBWrite=prevSrgb;UnityEngine.Object.DestroyImmediate(tex);c.useOcclusionCulling=occ;p.DebugPose(pos.x,pos.z,yaw,pitch);}

