using System.Globalization;
using System.Text;
using UnityEngine;

namespace CarpetCleaning
{
    /// <summary>
    /// CLI hooks for the economy (Docs/ECONOMY.md 7). Kept out of CarpetDebugApi so the two can evolve independently.
    /// Every call is safe from an editor eval in Play mode; JSON uses the invariant culture (a ru-RU editor writes decimal commas).
    /// </summary>
    public static partial class EconomyDebugApi
    {
        static Economy s_eco; static CelebrationFx s_fx;
        public static Economy Eco => s_eco != null ? s_eco : (s_eco = UnityEngine.Object.FindAnyObjectByType<Economy>());
        public static CelebrationFx Fx => s_fx != null ? s_fx : (s_fx = UnityEngine.Object.FindAnyObjectByType<CelebrationFx>());

        static readonly CultureInfo C = CultureInfo.InvariantCulture;
        static string F(float v) { return (float.IsNaN(v) || float.IsInfinity(v)) ? "null" : v.ToString("0.####", C); }

        public static string EconomyJson()
        {
            var e = Eco;
            if (e == null || e.Data == null) return "{\"error\":\"no Economy in scene\"}";
            var d = e.Data;
            var sb = new StringBuilder(1024);
            sb.Append("{\"money\":").Append(d.money.ToString(C));
            sb.Append(",\"lifetime\":").Append(d.lifetimeEarned.ToString(C));
            sb.Append(",\"carpets\":").Append(d.carpetsDelivered.ToString(C));
            sb.Append(",\"perfectClean\":").Append(d.perfectClean.ToString(C));
            sb.Append(",\"perfectDry\":").Append(d.perfectDry.ToString(C));
            sb.Append(",\"combos\":").Append(d.combos.ToString(C));
            sb.Append(",\"bestSeconds\":").Append(F(d.bestSeconds));
            sb.Append(",\"clock\":").Append(F(e.CarpetSeconds)).Append(",\"clockRunning\":").Append(e.ClockRunning ? "true" : "false");
            sb.Append(",\"V\":").Append(F(e.JobValue)).Append(",\"par\":").Append(F(e.ParSeconds));
            sb.Append(",\"neutral\":").Append(ToolTuning.Neutral ? "true" : "false");
            sb.Append(",\"save\":\"").Append(SaveStore.FilePath.Replace("\\", "/")).Append("\",\"loadNote\":\"").Append(e.LoadNote).Append('"');
            sb.Append(",\"items\":{\"moisture\":").Append(e.Owns(ShopItem.MoistureHighlight) ? "true" : "false")
              .Append(",\"dirt\":").Append(e.Owns(ShopItem.DirtHighlight) ? "true" : "false")
              .Append(",\"priceMoisture\":").Append(e.ItemPrice(ShopItem.MoistureHighlight).ToString(C))
              .Append(",\"priceDirt\":").Append(e.ItemPrice(ShopItem.DirtHighlight).ToString(C)).Append('}');
            sb.Append(",\"tools\":[");
            for (int i = 0; i < ToolTable.All.Length; i++)
            {
                var t = (ToolId)i;
                var eff = ToolTuning.Effective(t);
                if (i > 0) sb.Append(',');
                sb.Append("{\"id\":").Append(i).Append(",\"name\":\"").Append(ToolTable.All[i].name).Append('"');
                sb.Append(",\"power\":").Append(e.Level(t, UpgradeStat.Power)).Append(",\"area\":").Append(e.Level(t, UpgradeStat.Area));
                sb.Append(",\"P\":").Append(F(ToolTuning.Power(t))).Append(",\"A\":").Append(F(ToolTuning.Area(t)));
                sb.Append(",\"radius\":").Append(F(eff.radiusMm)).Append(",\"length\":").Append(F(eff.lengthMm)).Append(",\"spacing\":").Append(F(eff.spacingMm));
                sb.Append(",\"pricePower\":").Append(e.Price(t, UpgradeStat.Power)).Append(",\"priceArea\":").Append(e.Price(t, UpgradeStat.Area)).Append('}');
            }
            sb.Append("],\"last\":").Append(e.LastPayout.valid ? e.LastPayout.ToJson() : "null");
            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>Pure pay-slip maths for a hypothetical carpet: clean/dry 0..1, sim seconds, job value V.</summary>
        public static string Preview(float clean, float dry, float seconds, float jobValue)
        {
            var e = Eco;
            var s = e != null && e.settings != null ? e.settings : ScriptableObject.CreateInstance<EconomySettings>();
            return PayoutResult.Compute(s, clean, dry, seconds, jobValue).ToJson();
        }

        public static string Grant(int amount) { var e = Eco; if (e != null) e.DebugGrant(amount); return EconomyJson(); }
        public static string ResetSave() { var e = Eco; if (e != null) e.DebugResetSave(); return EconomyJson(); }
        public static string Reload() { var e = Eco; if (e != null) e.DebugReload(); return EconomyJson(); }
        public static string SetLevel(int tool, int stat, int level) { var e = Eco; if (e != null) e.DebugSetLevel((ToolId)tool, (UpgradeStat)stat, level); return EconomyJson(); }

        public static string Buy(int tool, int stat)
        {
            var e = Eco;
            if (e == null) return "{\"error\":\"no Economy\"}";
            int price = e.Price((ToolId)tool, (UpgradeStat)stat);
            bool ok = e.TryBuy((ToolId)tool, (UpgradeStat)stat);
            return "{\"ok\":" + (ok ? "true" : "false") + ",\"price\":" + price.ToString(C) + ",\"money\":" + e.Money.ToString(C) +
                   ",\"level\":" + e.Level((ToolId)tool, (UpgradeStat)stat).ToString(C) + "}";
        }

        /// <summary>Buys a one-time item (0 moisture highlight, 1 dirt highlight) exactly like its shop button.</summary>
        public static string BuyItem(int item)
        {
            var e = Eco;
            if (e == null) return "{\"error\":\"no Economy\"}";
            int price = e.ItemPrice((ShopItem)item);
            bool ok = e.TryBuyItem((ShopItem)item);
            return "{\"ok\":" + (ok ? "true" : "false") + ",\"price\":" + price.ToString(C) + ",\"money\":" + e.Money.ToString(C) +
                   ",\"owned\":" + (e.Owns((ShopItem)item) ? "true" : "false") + "}";
        }

        /// <summary>Grants / takes away a one-time item without money (writes the save: snapshot it first).</summary>
        public static string SetOwned(int item, bool owned) { var e = Eco; if (e != null) e.DebugSetOwned((ShopItem)item, owned); return EconomyJson(); }

        /// <summary>
        /// true: scripted runs keep the saved upgrade levels (the next CarpetDebugApi.Reset stops forcing factory tuning).
        /// false (default): every CarpetDebugApi.Reset switches to factory tuning, so old recipes keep their numbers.
        /// </summary>
        public static string LiveTuning(bool on)
        {
            ToolTuning.ScriptsUseLiveTuning = on;
            ToolTuning.SetNeutral(!on);
            return "{\"neutral\":" + (ToolTuning.Neutral ? "true" : "false") + "}";
        }

        public static string SetNeutral(bool on) { ToolTuning.SetNeutral(on); return "{\"neutral\":" + (ToolTuning.Neutral ? "true" : "false") + "}"; }

        /// <summary>Shows a made-up slip on the card without paying (visual tests).</summary>
        public static string ShowPayout(float clean, float dry, float seconds, float jobValue)
        {
            var e = Eco;
            if (e == null) return "{\"error\":\"no Economy\"}";
            var r = PayoutResult.Compute(e.settings, clean, dry, seconds, jobValue);
            r.carpetNumber = e.Data.carpetsDelivered;
            r.dirtLevel = jobValue;
            e.DebugShowPayout(r);
            return r.ToJson();
        }

        /// <summary>World sparkles: emits the burst of a slip and simulates it to t seconds (deterministic capture).</summary>
        public static string SparkleSeek(float clean, float dry, float t)
        {
            var fx = Fx;
            if (fx == null) return "{\"error\":\"no CelebrationFx\"}";
            var e = Eco;
            var r = PayoutResult.Compute(e != null ? e.settings : ScriptableObject.CreateInstance<EconomySettings>(), clean, dry, 0f, 1f);
            return fx.DebugSeek(r, t);
        }
    }
}
