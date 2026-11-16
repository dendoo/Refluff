using System.IO;
using UnityEditor;
using UnityEngine;

namespace CarpetCleaning
{
    /// <summary>
    /// Bakes the UiSfxSynth recipes to Audio/Generated/ui_*.wav and assigns them to the EMPTY slots of
    /// Settings/UiSoundBank.asset (Docs/AUDIO.md 10). A slot that already holds a clip (a real recording) is never
    /// overwritten. With the WAVs baked, nothing is synthesised at runtime and the first payout is never silent.
    /// </summary>
    public static class UiSoundBaker
    {
        const string OutDir = "Assets/CarpetCleaning/Audio/Generated";
        public const string BankPath = "Assets/CarpetCleaning/Settings/UiSoundBank.asset";

        [MenuItem("CarpetCleaning/Audio/Bake UI Sounds")]
        public static void BakeMenu() { Debug.Log(Bake(false)); }

        /// <summary>Renders every recipe to WAV; `force` re-assigns generated clips even over the bank's current ones.</summary>
        public static string Bake(bool force)
        {
            if (!AssetDatabase.IsValidFolder("Assets/CarpetCleaning/Audio")) AssetDatabase.CreateFolder("Assets/CarpetCleaning", "Audio");
            if (!AssetDatabase.IsValidFolder(OutDir)) AssetDatabase.CreateFolder("Assets/CarpetCleaning/Audio", "Generated");
            var bank = EnsureBank();
            var sb = new System.Text.StringBuilder("[UiSoundBaker] ");
            for (int i = 0; i < UiSfxSynth.Count; i++)
            {
                var id = (UiSfx)i;
                string path = OutDir + "/" + UiSfxSynth.Name(id) + ".wav";
                var data = UiSfxSynth.Render(id);
                File.WriteAllBytes(Path.GetFullPath(path), EncodeWav(data, UiSfxSynth.SampleRate));
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var imp = AssetImporter.GetAtPath(path) as AudioImporter;
                if (imp != null)
                {
                    imp.forceToMono = true;
                    imp.loadInBackground = false;
                    var st = imp.defaultSampleSettings;
                    st.loadType = AudioClipLoadType.DecompressOnLoad;
                    st.compressionFormat = AudioCompressionFormat.PCM;
                    st.preloadAudioData = true;
                    imp.defaultSampleSettings = st;
                    imp.SaveAndReimport();
                }
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                var cur = bank.clips[i];
                bool generated = cur == null || AssetDatabase.GetAssetPath(cur).StartsWith(OutDir + "/ui_");
                if (clip != null && (generated || force)) bank.clips[i] = clip;
                float peak = 0f, rms = 0f;
                foreach (var x in data) { peak = Mathf.Max(peak, Mathf.Abs(x)); rms += x * x; }
                rms = Mathf.Sqrt(rms / Mathf.Max(data.Length, 1));
                sb.Append(UiSfxSynth.Name(id)).Append(' ').Append((data.Length / (float)UiSfxSynth.SampleRate).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture))
                  .Append("s pk ").Append(peak.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture))
                  .Append(" rms ").Append(rms.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)).Append("; ");
            }
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();
            return sb.ToString();
        }

        /// <summary>The bank asset; created with default gains when missing (never overwritten otherwise).</summary>
        public static UiSoundBank EnsureBank()
        {
            var bank = AssetDatabase.LoadAssetAtPath<UiSoundBank>(BankPath);
            if (bank == null)
            {
                bank = ScriptableObject.CreateInstance<UiSoundBank>();
                AssetDatabase.CreateAsset(bank, BankPath);
            }
            bank.Normalize();
            return bank;
        }

        static byte[] EncodeWav(float[] data, int rate)
        {
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                int bytes = data.Length * 2;
                w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + bytes);
                w.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                w.Write(System.Text.Encoding.ASCII.GetBytes("fmt ")); w.Write(16); w.Write((short)1); w.Write((short)1);
                w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
                w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(bytes);
                foreach (var x in data) w.Write((short)Mathf.Clamp(Mathf.RoundToInt(x * 32767f), -32768, 32767));
                w.Flush();
                return ms.ToArray();
            }
        }
    }
}
