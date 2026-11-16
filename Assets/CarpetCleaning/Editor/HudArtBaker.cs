using System.IO;
using UnityEditor;
using UnityEngine;

namespace CarpetCleaning
{
    /// <summary>
    /// Bakes every bitmap the HUD needs, procedurally (no third-party art):
    ///   - the soft drop shadow in two sizes: USS has no box-shadow, so a raised surface gets a 9-sliced blurred rounded
    ///     rect behind it (Docs/HUD.md 4). Black with an alpha falloff, tinted per use via -unity-background-image-tint-color;
    ///   - the economy art (Docs/ECONOMY.md 4): line icons (coin, stopwatch, sparkle, drop, star, bolt, expand) and the
    ///     payout particles (sparkle, glow, ring, confetti, coin, rays). White alpha masks tinted in USS, except the coin,
    ///     which is baked in colour. <= 64 px where it matters, so they fit the panel's dynamic atlas (maxSubTextureSize 64);
    ///   - the world sparkle of CelebrationFx (FX/fx_sparkle_world.png, mipmapped). NOT under Textures/: every texture
    ///     there becomes a rug picture (CarpetSceneBuilder.LoadPictures).
    ///
    /// Idempotent: re-running overwrites the PNGs and re-applies the import settings.
    /// Called from CarpetSceneBuilder.Build(); also on its own menu item.
    /// </summary>
    public static class HudArtBaker
    {
        public const string UiDir = "Assets/CarpetCleaning/UI";
        public const string TexDir = UiDir + "/Textures";
        public const string FxDir = "Assets/CarpetCleaning/FX";
        public const string ShadowLg = TexDir + "/hud_shadow_lg.png";
        public const string ShadowSm = TexDir + "/hud_shadow_sm.png";
        public const string WorldSparkle = FxDir + "/fx_sparkle_world.png";

        delegate Color Shade(float x, float y);   // x, y in [-1, 1], y up

        [MenuItem("CarpetCleaning/Bake HUD Art")]
        public static void Bake()
        {
            EnsureFolders();
            // size, inset (solid rect margin), corner radius, blur sigma, slice
            BakeShadow(ShadowLg, 128, 32f, 18f, 12f);
            BakeShadow(ShadowSm, 64, 14f, 10f, 5.5f);

            // ---- icons (white masks, tinted in USS)
            BakeTexture(TexDir + "/icon_clock.png", 64, 4, Stopwatch, false);
            BakeTexture(TexDir + "/icon_sparkle.png", 64, 4, (x, y) => White(Sparkle(x, y, 0.95f, 0f)), false);
            BakeTexture(TexDir + "/icon_drop.png", 64, 4, Drop, false);
            BakeTexture(TexDir + "/icon_star.png", 64, 4, (x, y) => White(Cover(SdStar5(x, y + 0.06f, 0.92f, 0.46f))), false);
            BakeTexture(TexDir + "/icon_bolt.png", 64, 4, Bolt, false);
            BakeTexture(TexDir + "/icon_expand.png", 64, 4, Expand, false);
            BakeTexture(TexDir + "/icon_eye.png", 64, 4, Eye, false);

            // ---- payout particles
            BakeTexture(TexDir + "/fx_sparkle.png", 64, 2, (x, y) => White(Sparkle(x, y, 1f, 0.35f)), false);
            BakeTexture(TexDir + "/fx_glow.png", 64, 1, (x, y) => White(Glow(x, y)), false);
            BakeTexture(TexDir + "/fx_ring.png", 64, 2, Ring, false);
            BakeTexture(TexDir + "/fx_confetti.png", 32, 4, (x, y) => White(Cover(SdRoundBox(x, y, 0.86f, 0.46f, 0.14f))), false);
            BakeTexture(TexDir + "/fx_coin.png", 64, 4, Coin, false);
            BakeTexture(TexDir + "/fx_rays.png", 256, 2, Rays, false);   // drawn ~1000 px wide: outside the 64 px atlas anyway

            // ---- world sparkle (CelebrationFx)
            BakeTexture(WorldSparkle, 128, 2, (x, y) => White(Sparkle(x, y, 1f, 0.5f)), true);

            AssetDatabase.Refresh();
            // a stylesheet resolves its url() references when it is imported: one imported before these PNGs existed
            // keeps showing the missing-image glyph until it is imported again
            const string uss = UiDir + "/CarpetHud.uss";
            if (File.Exists(Path.GetFullPath(uss))) AssetDatabase.ImportAsset(uss, ImportAssetOptions.ForceUpdate);
            Debug.Log("[HudArtBaker] HUD art baked");
        }

        public static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(UiDir)) AssetDatabase.CreateFolder("Assets/CarpetCleaning", "UI");
            if (!AssetDatabase.IsValidFolder(TexDir)) AssetDatabase.CreateFolder(UiDir, "Textures");
            if (!AssetDatabase.IsValidFolder(FxDir)) AssetDatabase.CreateFolder("Assets/CarpetCleaning", "FX");
        }

        /// <summary>
        /// alpha = 1 inside a rounded rect inset by `inset`, then a gaussian falloff outside it.
        /// The slice centre must stay inside the solid part, hence inset &lt; slice &lt; size - inset.
        /// </summary>
        static void BakeShadow(string path, int size, float inset, float radius, float sigma)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            var px = new Color32[size * size];
            float half = size * 0.5f;
            float ext = half - inset - radius;     // half-extents of the rounded rect's straight part
            if (ext < 0f) ext = 0f;
            float inv2s2 = 1f / (2f * sigma * sigma);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float qx = Mathf.Abs(x + 0.5f - half) - ext;
                    float qy = Mathf.Abs(y + 0.5f - half) - ext;
                    float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
                    float d = Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                    float a = d <= 0f ? 1f : Mathf.Exp(-d * d * inv2s2);
                    px[y * size + x] = new Color32(0, 0, 0, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(Path.GetFullPath(path), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Import(path, false);
        }

        /// <summary>Supersampled (ss x ss per pixel) procedural texture; the shade gets x, y in [-1, 1] with y up.</summary>
        static void BakeTexture(string path, int size, int ss, Shade shade, bool mips)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
            var px = new Color[size * size];
            float inv = 1f / (ss * ss);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Color acc = Color.clear;
                    for (int j = 0; j < ss; j++)
                    {
                        for (int i = 0; i < ss; i++)
                        {
                            float u = ((x + (i + 0.5f) / ss) / size) * 2f - 1f;
                            float v = ((y + (j + 0.5f) / ss) / size) * 2f - 1f;
                            Color c = shade(u, v);
                            // premultiply for the average, so a transparent sample adds no colour
                            acc += new Color(c.r * c.a, c.g * c.a, c.b * c.a, c.a);
                        }
                    }
                    acc *= inv;
                    px[y * size + x] = acc.a > 1e-5f ? new Color(acc.r / acc.a, acc.g / acc.a, acc.b / acc.a, acc.a) : new Color(1f, 1f, 1f, 0f);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(Path.GetFullPath(path), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Import(path, mips);
        }

        static void Import(string path, bool mips)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) { Debug.LogError("[HudArtBaker] no importer for " + path); return; }
            imp.textureType = TextureImporterType.Default;
            imp.alphaSource = TextureImporterAlphaSource.FromInput;
            imp.alphaIsTransparency = true;
            imp.sRGBTexture = true;
            imp.mipmapEnabled = mips;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Bilinear;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }

        // ------------------------------------------------------------------ shapes
        static Color White(float a) { return new Color(1f, 1f, 1f, Mathf.Clamp01(a)); }
        static float Cover(float sdf) { return sdf <= 0f ? 1f : 0f; }   // supersampling does the anti-aliasing

        /// <summary>Four-point star with thin arms tapering to the tips, a hot core and an optional halo.</summary>
        static float Sparkle(float x, float y, float reach, float halo)
        {
            float ax = Mathf.Abs(x), ay = Mathf.Abs(y);
            float r = Mathf.Sqrt(x * x + y * y);
            // an arm is a diamond-ish taper: width shrinks linearly to the tip
            float armH = Mathf.Clamp01(1f - ax / reach) * Mathf.Clamp01(1f - ay / (0.16f * Mathf.Clamp01(1f - ax / reach) + 1e-3f));
            float armV = Mathf.Clamp01(1f - ay / reach) * Mathf.Clamp01(1f - ax / (0.16f * Mathf.Clamp01(1f - ay / reach) + 1e-3f));
            float arms = Mathf.Max(armH, armV);
            arms = Mathf.Pow(arms, 0.7f);
            float core = Mathf.Exp(-r * r * 60f);
            float glow = halo * Mathf.Exp(-r * r * 7f) * Mathf.Clamp01((1f - r) * 4f);
            return Mathf.Max(Mathf.Max(arms, core), glow);
        }

        static float Glow(float x, float y)
        {
            float r2 = x * x + y * y;
            return Mathf.Exp(-r2 * 4.5f) * Mathf.Clamp01((1f - Mathf.Sqrt(r2)) * 5f);
        }

        static Color Ring(float x, float y)
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float band = Mathf.Exp(-Mathf.Pow((r - 0.8f) / 0.075f, 2f));
            float inner = 0.12f * Mathf.Exp(-Mathf.Pow((r - 0.7f) / 0.2f, 2f));
            return White((band + inner) * Mathf.Clamp01((1f - r) * 12f));
        }

        static Color Rays(float x, float y)
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float th = Mathf.Atan2(y, x);
            float rays = Mathf.Pow(0.5f + 0.5f * Mathf.Cos(th * 16f), 9f);
            float fade = Mathf.Clamp01(1f - r) * Mathf.Clamp01(r * 3f);
            float core = 0.35f * Mathf.Exp(-r * r * 6f);
            return White((rays * 0.85f * fade + core) * Mathf.Clamp01((1f - r) * 6f));
        }

        static Color Stopwatch(float x, float y)
        {
            float cy = -0.1f;
            float r = Mathf.Sqrt(x * x + (y - cy) * (y - cy));
            bool ring = r <= 0.74f && r >= 0.56f;
            bool crown = Mathf.Abs(x) <= 0.12f && y >= 0.66f && y <= 0.9f;
            bool cap = Mathf.Abs(x) <= 0.26f && y >= 0.84f && y <= 0.98f;
            // hand towards "two o'clock"
            Vector2 d = new Vector2(0.55f, 0.72f).normalized;
            Vector2 p = new Vector2(x, y - cy);
            float along = Vector2.Dot(p, d), across = Mathf.Abs(p.x * d.y - p.y * d.x);
            bool hand = along >= -0.04f && along <= 0.46f && across <= 0.07f;
            bool hub = r <= 0.11f;
            return White(ring || crown || cap || hand || hub ? 1f : 0f);
        }

        static Color Drop(float x, float y)
        {
            const float cy = -0.28f, R = 0.56f, tip = 0.92f;
            bool inside;
            if (y <= cy) inside = x * x + (y - cy) * (y - cy) <= R * R;
            else
            {
                float t = (y - cy) / (tip - cy);          // 0 at the widest point, 1 at the tip
                float half = R * Mathf.Pow(Mathf.Clamp01(1f - t), 0.85f) * Mathf.Sqrt(Mathf.Clamp01(1f - 0.15f * t));
                inside = Mathf.Abs(x) <= half && y <= tip;
            }
            // a highlight notch keeps the icon readable at 20 px
            float hx = x + 0.2f, hy = y + 0.38f;
            bool shine = hx * hx / 0.02f + hy * hy / 0.05f <= 1f;
            return White(inside && !shine ? 1f : 0f);
        }

        // the dirt highlight (Docs/HIGHLIGHTS.md): an almond outline (two circle arcs meeting in the corners) and a pupil with a shine
        static Color Eye(float x, float y)
        {
            const float c = 0.56f, R = 1.06f, T = 0.15f;     // arcs through (+-0.9, 0), lid height 0.5
            float up = x * x + (y + c) * (y + c), dn = x * x + (y - c) * (y - c);
            bool outer = up <= R * R && dn <= R * R;
            bool inner = up <= (R - T) * (R - T) && dn <= (R - T) * (R - T);
            float r2 = x * x + y * y;
            bool pupil = r2 <= 0.30f * 0.30f;
            float sx = x - 0.1f, sy = y - 0.1f;
            bool shine = sx * sx + sy * sy <= 0.085f * 0.085f;
            return White((outer && !inner) || (pupil && !shine) ? 1f : 0f);
        }

        static Color Bolt(float x, float y)
        {
            // lightning bolt polygon (y up)
            var poly = new[]
            {
                new Vector2(0.18f, 0.95f), new Vector2(-0.52f, -0.06f), new Vector2(-0.06f, -0.06f),
                new Vector2(-0.22f, -0.95f), new Vector2(0.52f, 0.12f), new Vector2(0.06f, 0.12f)
            };
            return White(InPoly(new Vector2(x, y), poly) ? 1f : 0f);
        }

        static Color Expand(float x, float y)
        {
            // four corner arrows pointing out of a centre square: "wider reach"
            float ax = Mathf.Abs(x), ay = Mathf.Abs(y);
            bool square = SdRoundBox(x, y, 0.34f, 0.34f, 0.08f) <= 0f && SdRoundBox(x, y, 0.2f, 0.2f, 0.02f) > 0f;
            // corner chevron: an L at each corner
            bool lx = ax >= 0.5f && ax <= 0.95f && ay >= 0.83f && ay <= 0.95f;
            bool ly = ay >= 0.5f && ay <= 0.95f && ax >= 0.83f && ax <= 0.95f;
            return White(square || lx || ly ? 1f : 0f);
        }

        static Color Coin(float x, float y)
        {
            float r = Mathf.Sqrt(x * x + y * y);
            if (r > 0.94f) return new Color(1f, 1f, 1f, 0f);
            var rimDark = new Color(0.72f, 0.45f, 0.05f);
            var rim = new Color(0.93f, 0.66f, 0.12f);
            var face = new Color(1f, 0.80f, 0.24f);
            var faceLit = new Color(1f, 0.93f, 0.62f);
            // light from the upper left
            float light = Mathf.Clamp01(0.5f + 0.5f * (-x * 0.6f + y * 0.8f));
            Color c;
            if (r > 0.8f) c = Color.Lerp(rimDark, rim, light);
            else
            {
                c = Color.Lerp(face, faceLit, light * 0.8f);
                // embossed inner ring
                float ring = Mathf.Exp(-Mathf.Pow((r - 0.62f) / 0.035f, 2f));
                c = Color.Lerp(c, rim, ring * 0.8f);
                // star emboss in the middle
                if (SdStar5(x, y + 0.03f, 0.38f, 0.45f) <= 0f) c = Color.Lerp(c, rimDark, 0.55f + 0.2f * (1f - light));
            }
            // specular glint
            float gx = x + 0.34f, gy = y - 0.4f;
            c = Color.Lerp(c, Color.white, 0.75f * Mathf.Exp(-(gx * gx + gy * gy) * 30f));
            c.a = 1f;
            return c;
        }

        static float SdRoundBox(float x, float y, float hx, float hy, float rad)
        {
            float qx = Mathf.Abs(x) - hx + rad, qy = Mathf.Abs(y) - hy + rad;
            float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - rad;
        }

        /// <summary>Inigo Quilez's 5-point star, pointing up (y up).</summary>
        static float SdStar5(float px, float py, float r, float rf)
        {
            var k1 = new Vector2(0.809016994375f, -0.587785252292f);
            var k2 = new Vector2(-k1.x, k1.y);
            var p = new Vector2(Mathf.Abs(px), py);
            p -= 2f * Mathf.Max(Vector2.Dot(k1, p), 0f) * k1;
            p -= 2f * Mathf.Max(Vector2.Dot(k2, p), 0f) * k2;
            p.x = Mathf.Abs(p.x);
            p.y -= r;
            var ba = rf * new Vector2(-k1.y, k1.x) - new Vector2(0f, 1f);
            float h = Mathf.Clamp(Vector2.Dot(p, ba) / Vector2.Dot(ba, ba), 0f, r);
            return (p - ba * h).magnitude * Mathf.Sign(p.y * ba.x - p.x * ba.y);
        }

        static bool InPoly(Vector2 p, Vector2[] v)
        {
            bool c = false;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i++)
                if (((v[i].y > p.y) != (v[j].y > p.y)) && (p.x < (v[j].x - v[i].x) * (p.y - v[i].y) / (v[j].y - v[i].y) + v[i].x)) c = !c;
            return c;
        }
    }
}
