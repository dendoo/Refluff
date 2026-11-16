namespace CarpetCleaning
{
    public struct ToolDefinition
    {
        public ToolId id;
        public string name;
        public int hotkey;       // number-row slot 1..10 (10 = the 0 key); 0 = none (mouse only). ToolController maps it, the HUD shows HotkeyLabel
        public float radiusMm;
        public float lengthMm;
        public bool isBlade;
        public float spacingMm;
        public bool floorTool;   // true = right-hand HUD strip, acts only on the floor simulation

        public ToolDefinition(ToolId id, string name, int hotkey, float radiusMm, float lengthMm, bool isBlade, float spacingMm, bool floorTool)
        {
            this.id = id; this.name = name; this.hotkey = hotkey; this.radiusMm = radiusMm;
            this.lengthMm = lengthMm; this.isBlade = isBlade; this.spacingMm = spacingMm; this.floorTool = floorTool;
        }

        public bool HasHotkey { get { return hotkey >= 1 && hotkey <= 10; } }

        /// <summary>What the key chip reads: "1".."9", "0" for slot 10, "" without a hotkey.</summary>
        public string HotkeyLabel
        {
            get { return !HasHotkey ? "" : hotkey == 10 ? "0" : hotkey.ToString(System.Globalization.CultureInfo.InvariantCulture); }
        }
    }

    /// <summary>
    /// Section 7.1: order = enum = toolbar order = number-row order. Tools 8/9 are floor-only and mouse-driven (Docs/FLOOR_TOOLS.md,
    /// the user's call: no hotkeys); a tool without a hotkey shows no key chip anywhere (HUD.md 9).
    /// </summary>
    public static class ToolTable
    {
        public static readonly ToolDefinition[] All =
        {
            new ToolDefinition(ToolId.Scraper,      "Scraper",      1,   8f, 120f, true,   8f, false),
            new ToolDefinition(ToolId.DryVacuum,    "Dry vacuum",   2,  50f,   0f, false, 12f, false),
            new ToolDefinition(ToolId.Hose,         "Hose",         3,  40f,   0f, false, 10f, false),
            new ToolDefinition(ToolId.FoamSprayer,  "Foam sprayer", 4,  70f,   0f, false, 17f, false),
            new ToolDefinition(ToolId.Brush,        "Brush",        5,  45f,   0f, false, 11f, false),
            new ToolDefinition(ToolId.Squeegee,     "Squeegee",     6,  15f, 220f, true,  15f, false),
            new ToolDefinition(ToolId.WetVacuum,    "Wet vacuum",   7,  25f, 160f, true,  25f, false),
            new ToolDefinition(ToolId.Dryer,        "Dryer",        8, 135f,   0f, false, 33f, false),   // 135 = 110 * sqrt(1.5): 1.5x the area
            new ToolDefinition(ToolId.FloorCleaner, "Detergent",    0,  85f,   0f, false, 20f, true),
            new ToolDefinition(ToolId.Mop,          "Mop",          0,  30f, 320f, true,  18f, true),
        };

        public static ToolDefinition Get(ToolId id)
        {
            int i = (int)id;
            if (i < 0 || i >= All.Length) i = 0;
            return All[i];
        }

        public static bool IsFloorTool(ToolId id) { return Get(id).floorTool; }

        /// <summary>How many tools sit on the carpet strip (floor == false) or the floor strip (floor == true).</summary>
        public static int StripCount(bool floor)
        {
            int n = 0;
            for (int i = 0; i < All.Length; i++) if (All[i].floorTool == floor) n++;
            return n;
        }

        /// <summary>Scroll-wheel step: walks All[] in `dir`, wrapping, staying on the strip the current tool belongs to.</summary>
        public static ToolId NextOnStrip(ToolId cur, int dir)
        {
            bool floor = IsFloorTool(cur);
            int n = All.Length;
            int i = (int)cur;
            if (i < 0 || i >= n) i = 0;
            if (dir == 0) dir = 1;
            for (int step = 0; step < n; step++)
            {
                i = ((i + dir) % n + n) % n;
                if (All[i].floorTool == floor) return All[i].id;
            }
            return cur;
        }

        /// <summary>First tool of a strip; the fallback when the other strip is blocked.</summary>
        public static ToolId FirstOnStrip(bool floor)
        {
            for (int i = 0; i < All.Length; i++) if (All[i].floorTool == floor) return All[i].id;
            return ToolId.Scraper;
        }
    }
}
