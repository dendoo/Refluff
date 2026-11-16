Pre-shoot fix probes (TRAILER.md 8.3 items 3-5, 2026-09-26). Eval bodies for the live Editor in Play mode, after the
recompile that brings in the CarpetHandover / ToolDefinition / ToolController / CleaningHUD / ShopView changes:
    export MSYS_NO_PATHCONV=1
    unity command eval_file --file "C:\UnityProjects\CarpetCleaning\Tools\steam_art\trailer\fix_probes\<name>.cs"

v1_corner_lift.cs  hand-over corner lift (HANDOVER.md 10). Side-effect free: sets State by reflection, walks Seek, restores
                   the stage; no Begin (no payout), no carpet swap, no Steps. Must print "out worst 0.0" and "in worst 0.0"
                   (before the fix: -77.9 / -5.6 mm); "rest 0.0" and "back to Idle 0.0".
v2_shop_pills.cs   shop price centring (.ubuy, HUD.md 9). Opens the shop and runs 40 real Steps (pauses the Editor).
                   Must print "worst dy" and "dx" within +-0.5 px (before: about -6 px). The shop stays open, so take a
                   CaptureHud(path, 2560, 1440) now; then run v2b_shop_close.cs.
v3_key_chips.cs    key chips + number-row map (HUD.md 9 / FLOOR_TOOLS.md 1). Must print carpet strips [1]..[8] plain=0 and the
                   floor tools with NO chip: floor-tools plain=2, shop-floor-tools plain=2 (plain = strip rows / shop heads
                   without a chip), no "·" anywhere; Detergent / Mop have no key (mouse only, the user's call).
The Editor stays paused after v2: set UnityEditor.EditorApplication.isPaused = false before editor_stop.
