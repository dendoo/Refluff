using UnityEngine;

namespace CarpetCleaning
{
    /// <summary>HUD side of the economy's CLI hooks (Docs/ECONOMY.md 7): the payout card, the shop and HUD screenshots.</summary>
    public static partial class EconomyDebugApi
    {
        static EconomyHud s_hud;
        public static EconomyHud Hud => s_hud != null ? s_hud : (s_hud = UnityEngine.Object.FindAnyObjectByType<EconomyHud>());

        /// <summary>Puts the payout card at t seconds of its timeline and freezes it there (no unscaled-time advance) until PayoutResume.</summary>
        public static string PayoutSeek(float t) { var h = Hud; if (h == null) return "{\"error\":\"no EconomyHud\"}"; h.DebugSeekPayout(t); return PayoutJson(); }
        public static void PayoutResume() { var h = Hud; if (h != null) h.DebugResumePayout(); }
        public static string PayoutJson() { var h = Hud; return h != null ? h.DebugPayoutJson() : "{\"error\":\"no EconomyHud\"}"; }

        public static string ShopOpen(bool open) { var h = Hud; if (h == null) return "{\"error\":\"no EconomyHud\"}"; h.SetShopOpen(open); return ShopJson(); }
        public static string ShopJson() { var h = Hud; return h != null ? h.DebugShopJson() : "{\"error\":\"no EconomyHud\"}"; }

        /// <summary>Saves a PNG of the HUD (UI Toolkit capture) composited over a camera render; relative paths are under Assets/.</summary>
        public static string CaptureHud(string pngPath, int width = 0, int height = 0)
        {
            var h = Hud;
            if (h == null) return "{\"error\":\"no EconomyHud\"}";
            return h.DebugCapture(pngPath, width, height);
        }
    }
}
