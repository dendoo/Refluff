namespace CarpetCleaning
{
    /// <summary>
    /// One-time purchases of the shop (Docs/HIGHLIGHTS.md): bought once, owned for good, stored in SaveData.items by this
    /// index. Unlike the tool upgrades they change no simulation number, only what the player can see.
    /// </summary>
    public enum ShopItem
    {
        /// <summary>Z: wet pile, puddles and damp tile glow blue (SurfaceHighlight).</summary>
        MoistureHighlight = 0,
        /// <summary>X: every trace of soil that still counts against Clean glows amber (SurfaceHighlight).</summary>
        DirtHighlight = 1,
    }

    public static class ShopItems
    {
        public const int Count = 2;

        /// <summary>English name, a key of Resources/Loc: show it through Loc.T (Docs/LOCALIZATION.md).</summary>
        public static string Name(ShopItem item)
        {
            return item == ShopItem.DirtHighlight ? "Dirt highlight" : "Moisture highlight";
        }

        /// <summary>The toggle key as the HUD writes it: the letter the player's keyboard layout prints there (KeyNames).</summary>
        public static string Key(ShopItem item)
        {
            return KeyNames.Label(item == ShopItem.DirtHighlight ? UnityEngine.InputSystem.Key.X : UnityEngine.InputSystem.Key.Z);
        }
    }
}
