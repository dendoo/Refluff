# First-person HUD and upgrade workbench (2026-11-15)

First-person gameplay buys the existing upgrades at a physical table. The wallet, prices, powers,
areas, purchased lamps, saves and carpet payout rules are unchanged.

## Workbench

`Scripts/UI/UpgradeWorkbench.cs` belongs to the table root created by the scene builder. Wire its
`interactionCollider` to the tabletop, `viewCamera` to the player's camera, and `hud` / `tools` to
the existing components. A centre-screen physics ray must hit the table within 2.1 metres, with no
solid collider in front of it. The player must own the mouse and no shop, pause menu or Steam overlay
may be open. Looking at the reachable table shows **E + Upgrades**; E opens the existing `ShopView`.
Esc and the shop's Close button return to the room. The first-person controller owns cursor locking
and prevents the Close click from becoming a cleaning stroke.

`EconomyHud.OpenFromWorkbench(table)` validates this interaction. `SetShopOpen` remains available to
the existing debug API and overhead recipes. The old U shortcut and top-right shop button are only
available when no first-person player is active. Opening the shop while the pause menu or Steam
overlay is open is refused. The inspection lamp's unowned-item hint now points to the upgrade table.

For a geometry-only test in an unfocused Editor, `table.IsAimedAt(camera)` performs the same raycast
without mouse-ownership checks. `table.DebugJson()` reports the ray and interaction state.

## HUD

`Scripts/UI/FirstPersonHud.cs` is added to `CarpetUI` beside `CleaningHUD`. It toggles `.first-person`
on the UXML's HUD root while `tools.FirstPersonActive` is true. `UI/CarpetHud.uxml` supplies the new
`fp-overlay`, and all colours, dimensions and layout rules remain in `UI/CarpetHud.uss`.

- The original side tool strips become a bottom-centre held-tool name and ten small number slots.
- A small central reticle marks the contact aim. It turns teal while cleaning and grows over a
  reachable upgrade table. The table prompt is below it.
- A small bottom-left legend explains WASD movement, mouse look, left-button cleaning and 1–0 tools.
  Mouse icons use USS geometry; physical letter chips follow `KeyNames` and the keyboard layout.
- The meters, money, clock, contextual cleaning hint, hand-over and next-carpet actions remain.
  The editor-only Reset badge and FPS readout are hidden in first person.
- The reticle, tool belt and control legend do not pick mouse input and disappear during modals.
  `CleaningHUD.PointerOverHud` cannot swallow a centre-ray cleaning stroke in first-person play.

The four control labels and the new lamp hint are keys in `Tools/loc/strings.json`, translated into
all 30 non-English tables. Existing `Upgrades` and tool names are reused. Tool names and the centre
prompt can wrap; the control legend grows to its translated text. Arabic rows mirror their contents.

## Verification

- `python Tools/loc/loc_tool.py sync`: English key list contains 105 keys.
- `python Tools/loc/loc_tool.py check`: 30 languages, 0 errors and 0 warnings, including glyph coverage.
- `CarpetHud.uxml` parses as valid XML.
- Live Play: `Tools/first_person/verify_ui.cs`, seven sequential batches, checked the HUD, visible
  workbench prompt and shop in all 31 languages: **0 overflowing texts**. The test presents the
  prompt directly while the Editor lacks mouse focus, and restores the language and modal state.
  Per-language reports: `reports/first_person/ui_<code>.json`; aggregate: `ui_summary.json`.
- Shop and pause menu reject stacking in both directions; closing clears both modal flags.
- A ray aimed at the physical terminal from player x=-1.5, z=0.88, yaw=270, pitch=20 hits the table
  (`aimed=true`). `CanInteract=false` in the unfocused Editor, as required by cursor ownership.
- Reviewed 2560x1440 captures: `Assets/Screenshots/first_person_ui_review_en.png` and
  `first_person_shop_review_ru.png`. The first review found that narrowing the old 300 px stats
  card to 264 px crowded the Russian par text against the wallet; retain the original card width.
- See `SESSION_LOG.md` for the overall gameplay compile and control tests.
