using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CarpetCleaning
{
    /// <summary>
    /// Windows x64 player builds for Steam (Docs/STEAM.md 5). Menu CarpetCleaning/Steam/*. The public methods return a
    /// one-line summary ("OK ..." / "FAIL: ...") so `unity command eval` can drive them.
    ///   BuildRelease   -> Builds/Steam/content/windows, the depot content root Tools/Steam/upload.ps1 uploads.
    ///                     No steam_appid.txt; Unity's *_DoNotShip folders are moved to Builds/Steam/symbols/{version}.
    ///   BuildLocalTest -> Builds/Steam/local, the same player plus steam_appid.txt so the exe runs without being
    ///                     launched by the Steam client. Never upload this one.
    /// Both apply EnsurePlayerSettings first (exe icon, full-screen defaults; Docs/STEAM.md 5.1).
    /// </summary>
    public static class SteamBuild
    {
        const string ExeName = "Refluff.exe"; // also Tools/Steam/upload.ps1 and the Steamworks launch option (Docs/STEAM.md 5)
        public const string SteamRoot = "Builds/Steam";
        public const string ContentDir = SteamRoot + "/content/windows";
        public const string LocalDir = SteamRoot + "/local";
        public const string SymbolsDir = SteamRoot + "/symbols";
        public const string AppIconDir = "Assets/CarpetCleaning/AppIcon"; // refluff_icon_<n>.png, python Tools/steam_art/export_app_icon.py

        [MenuItem("CarpetCleaning/Steam/Build Windows (release, for upload)")]
        static void BuildReleaseMenu() { Log(BuildRelease()); }

        [MenuItem("CarpetCleaning/Steam/Build Windows (local test, with steam_appid.txt)")]
        static void BuildLocalTestMenu() { Log(BuildLocalTest()); }

        [MenuItem("CarpetCleaning/Steam/Show Builds Folder")]
        static void ShowFolder()
        {
            string dir = Abs(SteamRoot);
            Directory.CreateDirectory(dir);
            EditorUtility.RevealInFinder(dir);
        }

        public static string BuildRelease() { return Build(ContentDir, false); }
        public static string BuildLocalTest() { return Build(LocalDir, true); }

        static string Build(string relDir, bool local)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "FAIL: leave Play mode first";
            string steamRoot = Abs(SteamRoot);
            string outDir = Abs(relDir);
            // the output folder is wiped: only ever inside Builds/Steam
            if (!outDir.StartsWith(steamRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                return "FAIL: refusing to clean " + outDir;

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) return "FAIL: no enabled scene in Build Settings";

            string player = EnsurePlayerSettings();
            if (player.StartsWith("FAIL", StringComparison.Ordinal)) return player;

            string licence = NonCommercialAudio();
            if (licence.Length > 0) Debug.LogWarning("[SteamBuild] " + licence);

            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
            Directory.CreateDirectory(outDir);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(outDir, ExeName),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            });
            var sum = report.summary;
            if (sum.result != BuildResult.Succeeded)
                return "FAIL: build " + sum.result + ", " + sum.totalErrors.ToString(CultureInfo.InvariantCulture) + " errors (see Console)";

            string version = PlayerSettings.bundleVersion;
            string symbols = Path.Combine(Abs(SymbolsDir), version);
            foreach (var d in Directory.GetDirectories(outDir))
            {
                string name = Path.GetFileName(d);
                if (!name.EndsWith("_DoNotShip", StringComparison.OrdinalIgnoreCase)
                    && !name.EndsWith("_ButDontShipItWithYourGame", StringComparison.OrdinalIgnoreCase)) continue;
                if (local) { Directory.Delete(d, true); continue; }
                // kept next to the version for crash dumps; the depot script excludes them as well
                Directory.CreateDirectory(symbols);
                string dst = Path.Combine(symbols, name);
                if (Directory.Exists(dst)) Directory.Delete(dst, true);
                Directory.Move(d, dst);
            }

            string appIdFile = Path.Combine(outDir, "steam_appid.txt");
            if (local) File.WriteAllText(appIdFile, SteamConfig.AppId.ToString(CultureInfo.InvariantCulture), new UTF8Encoding(false));
            else if (File.Exists(appIdFile)) File.Delete(appIdFile);

            string dataDir = Path.Combine(outDir, Path.GetFileNameWithoutExtension(ExeName) + "_Data");
            string steamDll = Path.Combine(dataDir, "Plugins", "x86_64", "steam_api64.dll");
            if (!File.Exists(steamDll)) return "FAIL: steam_api64.dll is missing from " + steamDll;

            string legal = CopyLegal(outDir);
            if (legal.Length > 0) return legal;
            string unknown = UnknownThirdParty(Path.Combine(dataDir, "Managed"));
            if (unknown.Length > 0) Debug.LogWarning("[SteamBuild] " + unknown);

            var files = new DirectoryInfo(outDir).GetFiles("*", SearchOption.AllDirectories);
            long bytes = files.Sum(f => f.Length);
            var inv = CultureInfo.InvariantCulture;
            return "OK " + relDir + ": " + files.Length.ToString(inv) + " files, "
                 + (bytes / (1024.0 * 1024.0)).ToString("F1", inv) + " MB, "
                 + sum.totalTime.TotalSeconds.ToString("F0", inv) + " s, version " + version
                 + (local ? ", steam_appid.txt=" + SteamConfig.AppId.ToString(inv) : ", symbols in " + SymbolsDir + "/" + version)
                 + ", CREDITS.txt + THIRD_PARTY_NOTICES.txt, " + player
                 + (licence.Length > 0 ? "; " + licence : "")
                 + (unknown.Length > 0 ? "; " + unknown : "");
        }

        /// <summary>
        /// The licence texts that must travel with the game, next to the exe (Docs/STEAM.md 5): CREDITS.txt = the in-game
        /// credits (UI/Credits.txt, the CC BY sound credits among them) and THIRD_PARTY_NOTICES.txt = the licences of the
        /// libraries in Managed/ (written by Tools/Steam/make_notices.py). Returns "" or a FAIL line.
        /// </summary>
        static string CopyLegal(string outDir)
        {
            string credits = Abs(CarpetSceneBuilder.CreditsPath);
            string notices = Abs(NoticesPath);
            if (!File.Exists(credits)) return "FAIL: missing " + CarpetSceneBuilder.CreditsPath;
            if (!File.Exists(notices)) return "FAIL: missing " + NoticesPath + " (python Tools/Steam/make_notices.py)";
            // CRLF: the files are read in Notepad on the players' machines
            File.WriteAllText(Path.Combine(outDir, "CREDITS.txt"), File.ReadAllText(credits).Replace("\r\n", "\n").Replace("\n", "\r\n"), new UTF8Encoding(false));
            File.Copy(notices, Path.Combine(outDir, "THIRD_PARTY_NOTICES.txt"), true);
            return "";
        }

        const string NoticesPath = "Tools/Steam/THIRD_PARTY_NOTICES.txt";

        /// <summary>
        /// Player settings every Steam build must carry (Docs/STEAM.md 5.1), set before each build so an edit in Project
        /// Settings cannot ship by accident: the exe / window / taskbar icon (one texture of AppIconDir per Standalone slot,
        /// the same art as the Steam shortcut icon; without them the exe gets Unity's default icon) and the display
        /// defaults, a borderless full-screen window at the desktop resolution. The player still starts in the mode it was
        /// last closed in (Unity keeps it in the registry, section 5.1). Returns a summary for the result line, or a FAIL line.
        /// </summary>
        static string EnsurePlayerSettings()
        {
            var standalone = NamedBuildTarget.Standalone;
            int[] sizes = PlayerSettings.GetIconSizes(standalone, IconKind.Any);
            var icons = new Texture2D[sizes.Length];
            for (int i = 0; i < sizes.Length; i++)
            {
                string path = AppIconDir + "/refluff_icon_" + sizes[i].ToString(CultureInfo.InvariantCulture) + ".png";
                if (!File.Exists(Abs(path))) return "FAIL: missing " + path + " (python Tools/steam_art/export_app_icon.py)";
                icons[i] = IconTexture(path, sizes[i]);
                if (icons[i] == null) return "FAIL: cannot load " + path;
                if (icons[i].width != sizes[i] || icons[i].height != sizes[i])
                    return "FAIL: " + path + " imports at " + icons[i].width.ToString(CultureInfo.InvariantCulture) + " px, the slot is "
                         + sizes[i].ToString(CultureInfo.InvariantCulture);
            }
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icons[Array.IndexOf(sizes, sizes.Max())] }, IconKind.Any); // Default Icon
            PlayerSettings.SetIcons(standalone, icons, IconKind.Any);
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultIsNativeResolution = true;
            AssetDatabase.SaveAssets();
            return "icon " + string.Join("/", sizes.Select(s => s.ToString(CultureInfo.InvariantCulture))) + " px, full-screen window";
        }

        /// <summary>
        /// Loads an icon texture, imported uncompressed, without mips and at its own size first (the exe icon is cut from
        /// it; the default non-power-of-two scaling had stretched the 48 px one to 64, and the exe got a blurred 48 frame).
        /// </summary>
        static Texture2D IconTexture(string path, int size)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) return null;
            }
            if (imp.textureType != TextureImporterType.Default || imp.textureCompression != TextureImporterCompression.Uncompressed
                || imp.npotScale != TextureImporterNPOTScale.None || imp.mipmapEnabled || !imp.alphaIsTransparency
                || !imp.isReadable || imp.wrapMode != TextureWrapMode.Clamp || imp.maxTextureSize < size)
            {
                imp.textureType = TextureImporterType.Default;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.npotScale = TextureImporterNPOTScale.None;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.isReadable = true;
                imp.wrapMode = TextureWrapMode.Clamp;
                if (imp.maxTextureSize < size) imp.maxTextureSize = size;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Third-party assemblies THIRD_PARTY_NOTICES.txt covers (prefix match on the file name).</summary>
        static readonly string[] KnownThirdParty = { "com.rlabrecque.steamworks.net", "glTFast", "Newtonsoft.Json", "MCPForUnity.Runtime" };

        /// <summary>Engine and runtime assemblies that need no notice of ours.</summary>
        static readonly string[] EngineAssemblies = { "Assembly-CSharp", "UnityEngine", "Unity.", "System", "Mono.", "mscorlib", "netstandard" };

        /// <summary>
        /// A managed dll that is neither ours, the engine's nor in KnownThirdParty: a new package brought a library whose
        /// licence notice is not in THIRD_PARTY_NOTICES.txt yet. Not a failure; said in the result line.
        /// </summary>
        static string UnknownThirdParty(string managedDir)
        {
            if (!Directory.Exists(managedDir)) return "";
            var names = Directory.GetFiles(managedDir, "*.dll").Select(Path.GetFileName)
                .Where(n => !EngineAssemblies.Any(p => n.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                         && !KnownThirdParty.Any(p => n.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(n => n, StringComparer.Ordinal).ToArray();
            return names.Length == 0 ? "" : "WARNING third-party assemblies without a notice (add them to Tools/Steam/make_notices.py): " + string.Join(", ", names);
        }

        /// <summary>
        /// Recordings in the sound bank whose credit names a non-commercial licence (CC BY-NC..., Docs/AUDIO.md 9.6):
        /// machine takes in use and every recorded clip slot (clipCredits, AUDIO.md 11). Not a failure, a free build
        /// may carry them; a paid release must not, so it is said in the result line.
        /// </summary>
        static string NonCommercialAudio()
        {
            var bank = AssetDatabase.LoadAssetAtPath<ToolSoundBank>("Assets/CarpetCleaning/Settings/ToolSoundBank.asset");
            if (bank == null || bank.entries == null) return "";
            var hits = bank.entries
                .Where(e => e != null && e.UsesRecording && IsNonCommercial(e.recording.credit))
                .Select(e => e.tool + ": " + e.recording.credit)
                .Concat(bank.entries
                    .Where(e => e != null && !string.IsNullOrEmpty(e.clipCredits))
                    .SelectMany(e => e.clipCredits.Split('\n').Where(IsNonCommercial).Select(line => e.tool + " " + line.Trim())))
                .ToArray();
            return hits.Length == 0 ? "" : "WARNING non-commercial audio in this build, not for a paid release: " + string.Join(" | ", hits);
        }

        static bool IsNonCommercial(string credit)
        {
            return !string.IsNullOrEmpty(credit)
                && (credit.IndexOf("-NC", StringComparison.OrdinalIgnoreCase) >= 0
                 || credit.IndexOf("NonCommercial", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        static string Abs(string rel)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", rel));
        }

        static void Log(string summary)
        {
            if (summary.StartsWith("OK", StringComparison.Ordinal)) Debug.Log("[SteamBuild] " + summary);
            else Debug.LogError("[SteamBuild] " + summary);
        }
    }
}
