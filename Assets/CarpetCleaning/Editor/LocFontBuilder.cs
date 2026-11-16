using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;

namespace CarpetCleaning
{
    /// <summary>
    /// UI Toolkit font assets for the non-Latin languages (Docs/LOCALIZATION.md 4). Input: UI/Fonts/NotoSans&lt;Script&gt;-Regular
    /// and -Bold.ttf, written by `python Tools/loc/build_fonts.py` (subset Noto Sans, SIL OFL 1.1). Output: one dynamic
    /// SDF FontAsset per TTF next to it ("&lt;name&gt; SDF.asset"), the Bold asset in the Regular one's weight table (700), so
    /// -unity-font-style: bold draws the real bold face, and the other scripts' Regular assets as its fallbacks.
    /// Dynamic: glyphs are rasterised when first shown (the Advanced Text Generator shapes from the font file anyway), and
    /// TextSettings.clearDynamicDataOnBuild keeps editor-time atlas pages out of the player.
    /// Menu CarpetCleaning/Localization/Build Font Assets recreates all of them and re-wires the scene's HudLocalizer; the
    /// scene builder only creates missing ones (Ensure).
    /// </summary>
    public static class LocFontBuilder
    {
        public const string FontDir = "Assets/CarpetCleaning/UI/Fonts";
        /// <summary>Order = LocScript order from Arabic on (HudLocalizer.FontFor).</summary>
        public static readonly string[] Families = { "NotoSansArabic", "NotoSansThai", "NotoSansJP", "NotoSansKR", "NotoSansSC", "NotoSansTC" };

        const int SamplingPointSize = 72;   // SDF source size: sharp from 11 px captions to the 40 px total
        const int Padding = 8;
        const int AtlasSize = 1024;

        [MenuItem("CarpetCleaning/Localization/Build Font Assets")]
        public static void BuildMenu()
        {
            string r = Build(true);
            var loc = Object.FindAnyObjectByType<HudLocalizer>();
            if (loc != null && !Application.isPlaying)
            {
                Wire(loc);
                EditorSceneManager.MarkSceneDirty(loc.gameObject.scene);
                EditorSceneManager.SaveScene(loc.gameObject.scene);
                r += ", HudLocalizer re-wired";
            }
            Debug.Log("[LocFontBuilder] " + r);
        }

        /// <summary>Creates the missing font assets (the scene builder's call).</summary>
        public static string Ensure() { return Build(false); }

        /// <summary>Creates (rebuild: recreates) the font assets; returns a one-line report.</summary>
        public static string Build(bool rebuild)
        {
            var regular = new List<FontAsset>();
            int made = 0;
            foreach (var fam in Families)
            {
                var reg = Make(fam + "-Regular", rebuild, ref made);
                var bold = Make(fam + "-Bold", rebuild, ref made);
                if (reg != null && bold != null)
                {
                    var table = reg.fontWeightTable;
                    if (table != null && table.Length > 7 && table[7].regularTypeface != bold)
                    {
                        table[7].regularTypeface = bold;
                        EditorUtility.SetDirty(reg);
                    }
                }
                regular.Add(reg);
            }
            // every script falls back to the others: the language list shows all of them whatever the HUD language is
            for (int i = 0; i < regular.Count; i++)
            {
                var fa = regular[i];
                if (fa == null) continue;
                var fb = new List<FontAsset>();
                for (int j = 0; j < regular.Count; j++) if (j != i && regular[j] != null) fb.Add(regular[j]);
                if (!SameList(fa.fallbackFontAssetTable, fb))
                {
                    fa.fallbackFontAssetTable = fb;
                    EditorUtility.SetDirty(fa);
                }
            }
            AssetDatabase.SaveAssets();
            int present = 0;
            foreach (var fa in regular) if (fa != null) present++;
            return "font assets: " + present + "/" + Families.Length + " scripts, " + made + " created";
        }

        /// <summary>Puts the six Regular assets on a HudLocalizer (scene builder and the menu).</summary>
        public static void Wire(HudLocalizer loc)
        {
            if (loc == null) return;
            loc.arabic = Load(Families[0]);
            loc.thai = Load(Families[1]);
            loc.japanese = Load(Families[2]);
            loc.korean = Load(Families[3]);
            loc.simplifiedChinese = Load(Families[4]);
            loc.traditionalChinese = Load(Families[5]);
            EditorUtility.SetDirty(loc);
        }

        /// <summary>The Regular asset of a family, or null.</summary>
        public static FontAsset Load(string family)
        {
            return AssetDatabase.LoadAssetAtPath<FontAsset>(AssetPath(family + "-Regular"));
        }

        static string AssetPath(string name) { return FontDir + "/" + name + " SDF.asset"; }

        static FontAsset Make(string name, bool rebuild, ref int made)
        {
            string path = AssetPath(name);
            var existing = AssetDatabase.LoadAssetAtPath<FontAsset>(path);
            if (existing != null && !rebuild) return existing;
            string ttf = FontDir + "/" + name + ".ttf";
            if (!File.Exists(ttf))
            {
                Debug.LogWarning("[LocFontBuilder] " + ttf + " missing: run `python Tools/loc/build_fonts.py`");
                return existing;
            }
            AssetDatabase.ImportAsset(ttf, ImportAssetOptions.ForceSynchronousImport);
            var font = AssetDatabase.LoadAssetAtPath<Font>(ttf);
            if (font == null) { Debug.LogError("[LocFontBuilder] " + ttf + " did not import as a Font"); return existing; }
            if (existing != null) AssetDatabase.DeleteAsset(path);   // a fresh asset (new GUID): Wire re-points the scene

            var fa = FontAsset.CreateFontAsset(font, SamplingPointSize, Padding, GlyphRenderMode.SDFAA, AtlasSize, AtlasSize,
                AtlasPopulationMode.Dynamic, true);
            if (fa == null) { Debug.LogError("[LocFontBuilder] CreateFontAsset failed for " + ttf); return null; }
            fa.name = name + " SDF";
            AssetDatabase.CreateAsset(fa, path);
            var tex = fa.atlasTextures != null && fa.atlasTextures.Length > 0 ? fa.atlasTextures[0] : null;
            if (tex != null)
            {
                tex.name = name + " Atlas";
                AssetDatabase.AddObjectToAsset(tex, fa);
            }
            if (fa.material != null)
            {
                fa.material.name = name + " Material";
                AssetDatabase.AddObjectToAsset(fa.material, fa);
            }
            EditorUtility.SetDirty(fa);
            made++;
            return fa;
        }

        static bool SameList(List<FontAsset> a, List<FontAsset> b)
        {
            if (a == null) return b == null || b.Count == 0;
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++) if (a[i] != b[i]) return false;
            return true;
        }
    }
}
