using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace CarpetCleaning
{
    /// <summary>
    /// Steam store art captures (Docs/STORE_ART.md), driven from `unity command eval`. A gameplay shot is two layers:
    /// the game camera rendered supersampled (`_scene.png`) and the UI Toolkit HUD captured at the Game view's layout
    /// size (`_hud_ui.png`, premultiplied) - the panel does not re-layout for another target size, so it cannot simply be
    /// rendered at 4K. Tools/steam_art/compose_screens.py stacks them into the 1920x1080 upload files. Key art uses
    /// RenderView: the same live scene from a free camera, optionally with a temporary depth-of-field volume.
    /// Nothing here touches a saved asset; the game camera is restored before every call returns.
    /// </summary>
    public static class StoreShotApi
    {
        static readonly CultureInfo C = CultureInfo.InvariantCulture;

        /// <summary>SteamStore/_work next to Assets. Relative paths given to the capture calls resolve against it.</summary>
        public static string WorkDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "SteamStore", "_work"));

        static string Full(string path) => Path.IsPathRooted(path) ? path : Path.Combine(WorkDir, path);

        /// <summary>Hides the debug-only parts of the HUD (the fps counter) for marketing captures; false shows them again.</summary>
        public static string MarketingHud(bool on)
        {
            int n = 0;
            foreach (var doc in Object.FindObjectsByType<UIDocument>())
            {
                var root = doc.rootVisualElement;
                var fps = root != null ? root.Q("fps-label") : null;
                if (fps == null) continue;
                fps.style.display = on ? DisplayStyle.None : DisplayStyle.Flex;
                n++;
            }
            return "{\"marketing\":" + (on ? "true" : "false") + ",\"labels\":" + n.ToString(C) + "}";
        }

        /// <summary>True while MarketingHud(true) hides the fps label, so a caller can put back the state it found.</summary>
        public static bool MarketingHudOn()
        {
            foreach (var doc in Object.FindObjectsByType<UIDocument>())
            {
                var root = doc.rootVisualElement;
                var fps = root != null ? root.Q("fps-label") : null;
                if (fps != null) return fps.style.display.value == DisplayStyle.None;
            }
            return false;
        }

        /// <summary>
        /// One gameplay shot: `<name>_scene.png` = the game camera at sceneW x sceneH, and, when hud is true,
        /// `<name>_hud.png` + `<name>_hud_ui.png` = the HUD panel at uiW x uiH (the Game view size; Screen.width reads 640
        /// while the Editor is paused, so it is passed in). Written to WorkDir/shots/.
        /// </summary>
        public static string Shot(string name, bool hud = true, int sceneW = 3840, int sceneH = 2160, int uiW = 2560, int uiH = 1440)
        {
            string dir = Path.Combine(WorkDir, "shots");
            string scene = RenderScene(Path.Combine(dir, name + "_scene.png"), sceneW, sceneH);
            if (!hud) return "{\"scene\":" + scene + "}";
            string ui = EconomyDebugApi.CaptureHud(Path.Combine(dir, name + "_hud.png"), uiW, uiH);
            return "{\"scene\":" + scene + ",\"hud\":" + ui + "}";
        }

        /// <summary>Renders Camera.main as it stands (post-processing on, no UI) into a w x h PNG.</summary>
        public static string RenderScene(string path, int w = 3840, int h = 2160)
        {
            var cam = Camera.main;
            if (cam == null) return "{\"error\":\"no main camera\"}";
            return Render(cam, Full(path), w, h);
        }

        /// <summary>
        /// Renders the scene from a free camera: world position, Euler angles (degrees), vertical FOV. focusM > 0 adds a
        /// temporary Bokeh depth of field focused focusM metres from the camera (aperture f-stop, focal length mm).
        /// The game camera's pose and FOV are put back before returning.
        /// </summary>
        public static string RenderView(string path, int w, int h, float px, float py, float pz, float rx, float ry, float rz,
                                        float fov, float focusM = 0f, float aperture = 4f, float focalMm = 60f)
        {
            var cam = Camera.main;
            if (cam == null) return "{\"error\":\"no main camera\"}";
            var tf = cam.transform;
            Vector3 p0 = tf.position; Quaternion r0 = tf.rotation; float f0 = cam.fieldOfView;
            GameObject dofGo = null; Volume vol = null; VolumeProfile prof = null;
            try
            {
                tf.SetPositionAndRotation(new Vector3(px, py, pz), Quaternion.Euler(rx, ry, rz));
                cam.fieldOfView = fov;
                if (focusM > 0f)
                {
                    prof = ScriptableObject.CreateInstance<VolumeProfile>();
                    prof.hideFlags = HideFlags.DontSave;
                    var dof = prof.Add<DepthOfField>(true);
                    dof.mode.Override(DepthOfFieldMode.Bokeh);
                    dof.focusDistance.Override(focusM);
                    dof.aperture.Override(aperture);
                    dof.focalLength.Override(focalMm);
                    dofGo = new GameObject("StoreShot DoF") { hideFlags = HideFlags.DontSave, layer = cam.gameObject.layer };
                    vol = dofGo.AddComponent<Volume>();
                    vol.isGlobal = true;
                    vol.priority = 1000f;
                    vol.sharedProfile = prof;
                }
                return Render(cam, Full(path), w, h);
            }
            finally
            {
                tf.SetPositionAndRotation(p0, r0);
                cam.fieldOfView = f0;
                // Destroy is deferred to the end of the frame: disable first so a second render in the same eval is clean
                if (vol != null) vol.enabled = false;
                if (dofGo != null) Object.Destroy(dofGo);
                if (prof != null) Object.Destroy(prof);
            }
        }

        /// <summary>The game camera's pose, so a free view can start from it: {"pos":[x,y,z],"euler":[x,y,z],"fov":f}.</summary>
        public static string CameraJson()
        {
            var cam = Camera.main;
            if (cam == null) return "{\"error\":\"no main camera\"}";
            Vector3 p = cam.transform.position, e = cam.transform.eulerAngles;
            return "{\"pos\":[" + p.x.ToString("0.####", C) + "," + p.y.ToString("0.####", C) + "," + p.z.ToString("0.####", C)
                 + "],\"euler\":[" + e.x.ToString("0.##", C) + "," + e.y.ToString("0.##", C) + "," + e.z.ToString("0.##", C)
                 + "],\"fov\":" + cam.fieldOfView.ToString("0.##", C) + "}";
        }

        static string Render(Camera cam, string full, int w, int h)
        {
            var hdr = NewCaptureTarget(w, h, true);
            var ldr = NewCaptureTarget(w, h, false);
            var prevActive = RenderTexture.active;
            Texture2D tex = null;
            try
            {
                RenderHdr(cam, hdr, ldr);
                RenderTexture.active = ldr;
                tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(full));
                File.WriteAllBytes(full, tex.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = prevActive;
                FreeCaptureTarget(hdr); FreeCaptureTarget(ldr);
                if (tex != null) DestroyNow(tex);
            }
            return "{\"path\":\"" + full.Replace("\\", "/") + "\",\"w\":" + w.ToString(C) + ",\"h\":" + h.ToString(C) + "}";
        }

        // ------------------------------------------------------------------ capture core (shared with Editor/StoreGifApi)
        /// <summary>
        /// A w x h capture target: hdr = ARGBHalf linear with a depth buffer (what the camera renders into), else ARGB32 sRGB
        /// (what the blit encodes into and ReadPixels reads). HideAndDontSave, which includes DontUnloadUnusedAsset, so an
        /// asset refresh in the middle of a clip take cannot destroy it.
        /// </summary>
        public static RenderTexture NewCaptureTarget(int w, int h, bool hdr)
        {
            var rt = hdr ? new RenderTexture(w, h, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear)
                         : new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.hideFlags = HideFlags.HideAndDontSave;
            rt.Create();
            return rt;
        }

        public static void FreeCaptureTarget(RenderTexture rt)
        {
            if (rt == null) return;
            rt.Release();
            DestroyNow(rt);
        }

        static void DestroyNow(Object o)
        {
            if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
        }

        /// <summary>
        /// Renders cam into `hdr` and blits it into the sRGB 8-bit `ldr` (read `ldr` afterwards). The camera's target and
        /// RenderTexture.active are restored. Pass `debris` to skip the lookup when this runs every frame.
        /// </summary>
        public static void RenderHdr(Camera cam, RenderTexture hdr, RenderTexture ldr, DebrisSystem debris = null)
        {
            // particles are RenderMeshIndirect calls queued in LateUpdate and live until the next frame; this queues only what is
            // missing (a kind drawn with a null material then), never a second copy (Docs/DEBRIS_SYSTEM.md, capture trap)
            if (debris == null) debris = Object.FindAnyObjectByType<DebrisSystem>();
            if (debris != null) debris.SubmitDrawsForCapture();

            // URP takes a target texture's format for its intermediate colour buffer: an 8-bit target renders the whole scene
            // in LDR (clamped at 1 before tonemapping, no glints, no bloom, the sunlit tile grey). Render HDR like the Game
            // view, then let a blit into an sRGB 8-bit texture do the encoding.
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;
            try
            {
                cam.targetTexture = hdr;
                cam.Render();
                cam.targetTexture = prevTarget;
                Graphics.Blit(hdr, ldr);
            }
            finally
            {
                cam.targetTexture = prevTarget;
                RenderTexture.active = prevActive;
            }
        }
    }
}
