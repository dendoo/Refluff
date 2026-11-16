using UnityEngine;

namespace CarpetCleaning
{
    /// <summary>
    /// Every number of the economy (Docs/ECONOMY.md): what a carpet pays, what an upgrade costs and does, and the timing of
    /// the payout card. Settings/EconomySettings.asset is written by CarpetSceneBuilder only when it does not exist yet, so
    /// tuned values survive a Build Scene; keep these C# defaults and the asset in sync when a default changes.
    /// </summary>
    [CreateAssetMenu(fileName = "EconomySettings", menuName = "CarpetCleaning/EconomySettings")]
    public sealed class EconomySettings : ScriptableObject
    {
        [Header("Payout (each line x job value V, rounded to 'roundTo')")]
        [Tooltip("Paid for every carpet handed over.")]
        public int basePay = 120;
        [Tooltip("Full speed bonus: handed over within par seconds; falls linearly to 0 at speedZeroFactor x par.")]
        public int speedBonusMax = 80;
        [Tooltip("Clean >= perfectMin (the HUD shows 100 %).")]
        public int perfectCleanBonus = 60;
        [Tooltip("Dry (1 - Wetness) >= perfectMin (the HUD shows 100 %).")]
        public int perfectDryBonus = 40;
        [Tooltip("Both perfect bonuses on the same carpet.")]
        public int comboBonus = 100;
        [Tooltip("Mathf.RoundToInt(clean * 100) == 100 exactly when clean >= 0.995: the bonus pays when the bar says 100 %.")]
        public float perfectMin = 0.995f;
        [Tooltip("DirtScore that counts as a full job. V = clamp01(DirtScore(InitStats) / dirtRef): a spotless carpet pays 0. Game rolls measured 2026-09-24 over 40 seeds: median 0.39, p90 0.73, max 1.05-1.37; the scripted fully soiled carpet (Reset(seed, 1)) scores 1.53-1.57.")]
        public float dirtRef = 1.0f;
        public int roundTo = 5;

        [Header("Speed par (seconds of sim time from landing to hand-over)")]
        [Tooltip("par = parMinSeconds + (parFullSeconds - parMinSeconds) * V.")]
        public float parMinSeconds = 45f;
        [Tooltip("Par of a full job (V = 1). Measured 2026-09-24: the hint-following bot (whole-carpet raster at 300 mm/s, level-0 tools) needs ~1010 s to Won on the dirtiest rolled carpet; a player who works only the dirty patches is well under 600 s.")]
        public float parFullSeconds = 600f;
        [Tooltip("The speed bonus reaches 0 at this multiple of par.")]
        public float speedZeroFactor = 2.5f;

        [Header("Upgrades (Docs/ECONOMY.md 2)")]
        public int maxLevel = 6;
        [Tooltip("POWER multiplier per level: power = powerStep ^ (level - powerAnchor[tool]) relative to the factory tuning.")]
        public float powerStep = 1.15f;
        [Tooltip("AREA multiplier at the last level (swept area: blade length, disc area). The levels in between are spaced evenly in ratio: area = areaMax ^ ((level - areaAnchor[tool]) / maxLevel), i.e. x1.20 per level for 3 over 6 levels.")]
        public float areaMax = 3f;
        [Tooltip("Level at which POWER equals today's factory tuning, per ToolId (the player starts at level 0).")]
        public int[] powerAnchor = { 3, 1, 2, 3, 0, 3, 0, 0, 0, 0 };
        [Tooltip("Level at which AREA equals today's factory size, per ToolId.")]
        public int[] areaAnchor = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        [Tooltip("Price of level L -> L+1 = round(priceBase * priceGrowth ^ L) x stat / floor factors.")]
        public int priceBase = 40;
        public float priceGrowth = 1.35f;
        public float areaPriceFactor = 1.2f;
        public float floorToolPriceFactor = 0.6f;

        [Header("One-time items (Docs/HIGHLIGHTS.md)")]
        [Tooltip("Z: moisture highlight. User 2026-09-24: $250, about two to four ordinary carpets.")]
        public int moistureHighlightPrice = 250;
        [Tooltip("X: dirt highlight. Dearer than moisture: it helps with Spotless AND the combo.")]
        public int dirtHighlightPrice = 400;

        [Header("Payout card timing (seconds, unscaled)")]
        public float cardDelay = 0.30f;
        public float cardIn = 0.35f;
        public float firstLine = 0.55f;
        public float lineGap = 0.55f;
        public float missedLineGap = 0.30f;
        public float countUp = 0.35f;
        public float comboBeat = 0.80f;
        public float totalCount = 0.60f;
        public float coinFlight = 0.70f;
        public int coins = 14;
        public float hold = 1.40f;
        public float cardOut = 0.30f;
    }
}
