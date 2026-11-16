using System.Globalization;
using System.Text;
using UnityEngine;

namespace CarpetCleaning
{
    /// <summary>One carpet's pay slip (Docs/ECONOMY.md 1). Computed once at the hand-over; the payout card only replays it.</summary>
    public struct PayoutResult
    {
        public bool valid;
        public int carpetNumber;       // 1-based count of carpets delivered, this one included
        public float jobValue;         // V = clamp01(DirtScore(InitStats) / dirtRef)
        public float dirtLevel;        // CarpetSimulation.DirtLevel (for the card's caption)
        public float seconds;          // sim seconds from landing to hand-over
        public float parSeconds, zeroSeconds;
        public float clean, dry;       // 0..1 at the hand-over
        public bool perfectClean, perfectDry, combo;
        public int basePay, speedBonus, cleanBonus, dryBonus, comboBonus, total;
        public int moneyBefore, moneyAfter;
        public string pictureName;

        /// <summary>Pure maths, shared by Economy and EconomyDebugApi.Preview.</summary>
        public static PayoutResult Compute(EconomySettings s, float clean, float dry, float seconds, float jobValue)
        {
            var r = new PayoutResult { valid = true };
            float v = Mathf.Clamp01(jobValue);
            r.jobValue = v;
            r.clean = Mathf.Clamp01(clean);
            r.dry = Mathf.Clamp01(dry);
            r.seconds = Mathf.Max(seconds, 0f);
            r.parSeconds = Par(s, v);
            r.zeroSeconds = r.parSeconds * Mathf.Max(s.speedZeroFactor, 1.01f);
            float pm = s.perfectMin;
            r.perfectClean = r.clean >= pm;
            r.perfectDry = r.dry >= pm;
            r.combo = r.perfectClean && r.perfectDry;
            float speed01 = Mathf.Clamp01((r.zeroSeconds - r.seconds) / Mathf.Max(r.zeroSeconds - r.parSeconds, 1e-3f));
            r.basePay = Round(s, s.basePay * v);
            r.speedBonus = Round(s, s.speedBonusMax * v * speed01);
            r.cleanBonus = r.perfectClean ? Round(s, s.perfectCleanBonus * v) : 0;
            r.dryBonus = r.perfectDry ? Round(s, s.perfectDryBonus * v) : 0;
            r.comboBonus = r.combo ? Round(s, s.comboBonus * v) : 0;
            r.total = r.basePay + r.speedBonus + r.cleanBonus + r.dryBonus + r.comboBonus;
            return r;
        }

        public static float Par(EconomySettings s, float jobValue)
        {
            return Mathf.Lerp(s.parMinSeconds, s.parFullSeconds, Mathf.Clamp01(jobValue));
        }

        static int Round(EconomySettings s, float x)
        {
            int step = Mathf.Max(s.roundTo, 1);
            return Mathf.Max(0, Mathf.RoundToInt(x / step) * step);
        }

        public static string Money(int amount) { return "$" + amount.ToString("N0", CultureInfo.InvariantCulture); }

        public static string Clock(float seconds)
        {
            int t = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return (t / 60).ToString(CultureInfo.InvariantCulture) + ":" + (t % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        // Soil classes of the pay slip by V (SoilName). Shared with the achievements (Achievements.cs, Docs/ACHIEVEMENTS.md),
        // so "dirty", "Heavy soil" and "Filthy" mean the same thing on the card and on Steam.
        /// <summary>V at or below this: "Already spotless".</summary>
        public const float SpotlessMaxValue = 0.001f;
        /// <summary>V below this: "Light soil".</summary>
        public const float LightMaxValue = 0.25f;
        /// <summary>V below this: "Medium soil".</summary>
        public const float MediumMaxValue = 0.6f;
        /// <summary>V below this: "Heavy soil"; from it on: "Filthy".</summary>
        public const float HeavyMaxValue = 0.9f;

        /// <summary>Caption of the job from the carpet's dirt level (Docs/DIRT_LEVEL.md). English: a Loc key, shown through
        /// Loc.T (PayoutView).</summary>
        public static string SoilName(float jobValue)
        {
            if (jobValue <= SpotlessMaxValue) return "Already spotless";
            if (jobValue < LightMaxValue) return "Light soil";
            if (jobValue < MediumMaxValue) return "Medium soil";
            if (jobValue < HeavyMaxValue) return "Heavy soil";
            return "Filthy";
        }

        public string ToJson()
        {
            var c = CultureInfo.InvariantCulture;
            var sb = new StringBuilder(256);
            sb.Append("{\"valid\":").Append(valid ? "true" : "false");
            sb.Append(",\"carpet\":").Append(carpetNumber.ToString(c));
            sb.Append(",\"V\":").Append(jobValue.ToString("0.###", c));
            sb.Append(",\"dirtLevel\":").Append(dirtLevel.ToString("0.###", c));
            sb.Append(",\"seconds\":").Append(seconds.ToString("0.#", c));
            sb.Append(",\"par\":").Append(parSeconds.ToString("0.#", c));
            sb.Append(",\"zero\":").Append(zeroSeconds.ToString("0.#", c));
            sb.Append(",\"clean\":").Append(clean.ToString("0.####", c));
            sb.Append(",\"dry\":").Append(dry.ToString("0.####", c));
            sb.Append(",\"perfectClean\":").Append(perfectClean ? "true" : "false");
            sb.Append(",\"perfectDry\":").Append(perfectDry ? "true" : "false");
            sb.Append(",\"combo\":").Append(combo ? "true" : "false");
            sb.Append(",\"base\":").Append(basePay.ToString(c));
            sb.Append(",\"speed\":").Append(speedBonus.ToString(c));
            sb.Append(",\"cleanBonus\":").Append(cleanBonus.ToString(c));
            sb.Append(",\"dryBonus\":").Append(dryBonus.ToString(c));
            sb.Append(",\"comboBonus\":").Append(comboBonus.ToString(c));
            sb.Append(",\"total\":").Append(total.ToString(c));
            sb.Append(",\"moneyBefore\":").Append(moneyBefore.ToString(c));
            sb.Append(",\"moneyAfter\":").Append(moneyAfter.ToString(c));
            sb.Append('}');
            return sb.ToString();
        }
    }
}
