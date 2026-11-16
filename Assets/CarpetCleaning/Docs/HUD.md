# HUD — UI Toolkit, layout and the "get out of the way" rule

The HUD is UI Toolkit (UIElements), not uGUI. Three files, one job each:

| File | Holds |
|---|---|
| `UI/CarpetHud.uxml` | structure — every element, its name and its classes |
| `UI/CarpetHud.uss` | **all** style — colours, sizes, radii, spacing, transitions |
| `Scripts/CleaningHUD.cs` | behaviour — binds simulation state to named elements, fills the tool lists |

Nothing in the UXML or the C# picks a colour or a pixel value, so a restyle is an edit of the
stylesheet only. `Editor/CarpetSceneBuilder.BuildUI()` is the source of truth for the scene side: it
bakes the shadow textures, creates `UI/CarpetHudPanel.asset` (PanelSettings, 1920x1080 reference,
`MatchWidthOrHeight` 0.5, sortingOrder 10) with the stock runtime theme imported from the one-line
`UI/UnityDefaultRuntimeTheme.tss`, and puts a `UIDocument` + `CleaningHUD` on the `CarpetUI` object.

## 1. Conceptual placement (unchanged from the uGUI version)

The carpet fills the middle of the screen, so nothing that is not a tool may sit over it.

```
 CLEAN/DRY meters          hint pill            Reset (R) + fps
 ┌──────────┐                                        ┌──────────┐
 │ CARPET   │                                        │ FLOOR    │   both strips are
 │ 1 Scraper│           <-- carpet -->               │9 Deterg. │   centred on their
 │ 2 Dry vac│                                        │0 Mop     │   screen edge
 └──────────┘                                        └──────────┘
                                           Hand over (H) / Take a new carpet (N)
```

A 24 px gutter on all four sides, shared by the corners and by both edge strips.

**Reset is Editor-only (2026-10-19).** `CleaningHUD.Build()` binds the Reset button under
`UNITY_EDITOR`; player builds remove its `#reset-wrap` before collecting click blockers, including
the shadow and row spacing. This applies to development players too. The R shortcut is unchanged.
This change was checked statically only; Unity verification was left to the user as requested.

## 2. Style

Light "spa glass" over a bright scene: near-white translucent surfaces (`--surface`, 92% white), one
hairline rim, a baked soft drop shadow, deep-slate ink, and exactly three accents — teal
(`--accent`) for the active tool, emerald (`--go`) for hand over, blue (`--new`) for a new carpet.
The meters use mint (`--clean`) and amber (`--dry`).

All tokens are USS variables declared once on `.hud`; changing the whole look means changing that
block. Vertical rhythm: every margin, padding, height and radius is a multiple of 2 and the layout
steps on a 4 px grid (4 / 8 / 12 / 16 / 24). Tool rows are 44 px with a 2 px gap; meter rows are
14 px apart; type is 11 / 12 / 13 / 14 / 18 px.

USS has no `box-shadow`, so every raised surface has a `.shadow` sibling behind it: a 9-sliced
blurred rounded rect baked by `Editor/HudArtBaker.cs` into `UI/Textures/hud_shadow_{lg,sm}.png`.
The bitmaps are black with an alpha falloff and are recoloured per use with
`-unity-background-image-tint-color`, so one texture serves every shadow. A shadow always needs a
wrapper element (`.strip`, `.raised`, `.stats`), because a child draws **over** its parent's
background — the shadow must be a sibling of the surface, not a child of it.

Re-bake with the menu item `CarpetCleaning/Bake HUD Art`; `Build Scene` does it too.

## 3. Two layers, and hiding the HUD while a tool is in use

The problem this solves: the strips, the Reset button and the action button sit over real floor, and
`ToolController` refuses to stamp while the pointer is over UI — so debris that happened to land
under a HUD element was unreachable, and a stroke that wandered onto one stalled.

| Layer | Holds | While stroking |
|---|---|---|
| `#chrome` | both strips, hint, Reset, fps, the action buttons | `.layer--hidden` (opacity 0) and stops picking |
| `#bars`   | the Clean/Dry meters only | `.layer--dim` (opacity 0.45), **never** picks |

`#chrome` is first in the UXML so the meters draw on top of it. Both are absolute full-screen rects,
so moving a widget between them is a one-line parent change.

- `ToolController.IsStroking` is true while a mouse button is held **and** the stroke has actually
  started (`_hasLast || (held && hovering)`). It is deliberately latched by `_hasLast`, so the HUD
  does not flicker back mid-stroke when the pointer leaves the floor domain for a frame.
- The fade itself is the USS `transition` on `.layer`; `CleaningHUD.UpdateWorkingFade()` only flips
  the class.
- **Picking drops on the FIRST stroking frame, not when the fade finishes.** Waiting for the fade
  would let the chrome swallow ~140 ms of stamps at the screen edges, which is exactly the bug being
  fixed. On the way back it waits `RestoreDelay` (0.07 s, half the transition), so a click landing
  right after the button is released does not hit a widget that is still half transparent.
- The meters stay up because they are the feedback the stroke is *for*, and they are never a pick
  target, so a crumb lying under them can always be reached.

## 4. Who is allowed to swallow a click

A UI Toolkit panel is **invisible to `EventSystem.IsPointerOverGameObject()`**, so the HUD hands
`ToolController` its own hit test: `CleaningHUD.PointerOverHud()` runs `panel.Pick()` at the pointer
and walks up the parents looking for the class **`ui-blocking`**. Only elements carrying that class
block a stamp: the two tool panels, the tool rows, Reset and the two action buttons. Everything else
is `picking-mode="Ignore"`.

`ToolController.PointerOverUI` is the `Func<Vector2, bool>` the HUD assigns in `Build()` and clears
in `OnDisable`. It is an instance field, not a static one, so a play session with domain reload
disabled cannot keep a delegate pointing at a destroyed HUD.

Two traps worth remembering:

- `UIDocument` instantiates the UXML into a **`TemplateContainer`** that neither stretches nor
  ignores picking by default. `CleaningHUD.Build()` forces both on every root child: without the
  stretch, `bottom: 24px` measures from a zero-height parent; without `PickingMode.Ignore`, a
  full-screen container answers `Pick()` and **every stamp on the whole screen** is refused.
- A parent set to `Ignore` does not make its children unpickable (that is why the arrangement above
  works), and setting `pickingMode` is what actually stops a click — opacity 0 alone does not.

## 5. If you add a widget

Parent it into `#chrome` unless it must survive a stroke. Anything parented into `#bars` must be
non-blocking by construction — that layer is the promise that a click there reaches the floor. Give
it `ui-blocking` only if it should eat clicks, and add it to the UXML, not to C#; data-driven lists
(the tool rows) are the only thing built in code, from `ToolTable`.

## 6. Screenshots of the HUD (2026-09-24)

`EconomyDebugApi.CaptureHud("Screenshots/x.png")` captures the panel with
`VisualElementCaptureExtensions.TryCaptureIntoRenderTexture` and composites it over a `Camera.Render` of the same size.
- It works while the Editor is unfocused, when the Game view never repaints and `capture_game_view --source screen` has
  no HUD.
- `x_ui.png` next to it is the HUD alone.

## 7. Economy pieces (Docs/ECONOMY.md 4-5)

- **Scripts.** A second script, `EconomyHud`, sits on `CarpetUI` beside `CleaningHUD` and shares its UIDocument.
  `CleaningHUD` is untouched.
- **New layers** inside `#hud` (so they inherit the tokens and get the TemplateContainer stretch), in draw order after
  `#bars`:
  - `#payout`: the pay-slip card;
  - `#shop`: the modal upgrade shop, with a `ui-blocking` scrim;
  - `#fx`: UI particles, top-most, never picking.
- **New in existing places:**
  - a ledger row (job clock + wallet) at the bottom of the meters card in `#bars`;
  - the Upgrades (U) button beside Reset in `#chrome` (`.top-right__row`).
- **Animation.** The payout card and the shop animate from C# on a seekable timeline, writing only translate / rotate /
  scale / opacity / text. Every colour and size stays in the USS, including the particle looks (`.fx--*` classes).
  - This is the one deliberate exception to "transitions live in USS": a USS transition cannot be seeked, and in a CLI
    test the frames do not tick.
- **Hidden states are `display:none`**, never opacity or `pickingMode`. `SetBlockersLive` turns picking back on for every
  cached `.ui-blocking` element after each stroke, so a transparent card would still swallow clicks.
- **Every HUD Button is `focusable = false`.** Enter is the UI Submit key and would re-click the focused button (a
  double purchase).
- **Tokens** added on `.hud`: `--money`, `--money-lit`, `--money-deep`, `--money-wash`, `--clean-wash`, `--clean-deep`,
  `--dry-wash`, `--dry-deep`, `--combo`, `--combo-wash`, `--late`, `--rim-strong`, `--card-sunken`, `--scrim`, and the
  saturated particle tints `--fx-gold`, `--fx-mint`, `--fx-amber`, `--fx-violet`, `--fx-rose`.
  - Gold = money; teal = clean and amber = dry keep their meaning; violet is the combo's own.
- **Textures** (`HudArtBaker`): `icon_*` masks tinted in USS, `fx_coin` baked in colour, particles ≤ 64 px for the
  dynamic atlas. A stylesheet imported before its PNGs existed shows the missing-image glyph, so `Bake` re-imports
  `CarpetHud.uss`.
- **Trap:** re-importing `CarpetHud.uss` during Play rebuilds the UI tree (editor live reload). Both HUD scripts then hold
  dead elements, so restart Play.

## 8. Inspection lamp pieces (Docs/HIGHLIGHTS.md, 2026-09-24)

- **Lamp chips.** Each meter head has a `.meter__title` row: caption plus a small keycap, `#hl-dirt-chip` "X" beside
  CLEAN and `#hl-wet-chip` "Z" beside DRY (`.kbd.lamp-chip`).
  - Each chip is `display:none` until its item is bought. `.is-on` fills it `--hl-dirt` amber or `--hl-wet` blue while its lamp is on.
  - The chips sit in `#bars`, so they are non-blocking by construction and stay visible while stroking.
  - `CleaningHUD.RefreshLampChips` binds them from `SurfaceHighlight.Changed`; `CleaningHUD.highlight` is wired by the builder.
- **Toast.** `CleaningHUD.Toast(text, seconds)` puts a message in the hint pill that wins over the sim's hint for that long
  (unscaled time). The lamp uses it when Z or X is pressed before the item is bought: "Dirt highlight: buy it in Upgrades (U)".
- **Shop.** An INSPECTION section with two fixed cards: `#item-moisture` and `#item-dirt`, each with `-text`, `-buy` and `-price`.
  - Styles: `.icard`, `.irow`, `.irow__text`, and `.ibuy` with `.is-poor` / `.is-owned`. An owned card gets `.is-owned` (a lamp-colour wash).
  - The buttons are in the UXML, so they carry `ui-blocking` there and get `focusable = false` from `EconomyHud.Build`.
- **Tokens** added on `.hud`: `--hl-dirt`, `--hl-dirt-lit`, `--hl-dirt-wash`, `--hl-wet`, `--hl-wet-lit`, `--hl-wet-wash`,
  `--fx-sky` (particle tint `.fx--sky`). Icon `.icon--eye` (`Textures/icon_eye.png`, `HudArtBaker.Eye`).

## 9. Key chips and price pills (2026-09-26, pre-shoot fixes, `TRAILER.md` 8.3; pending Play verification)

- **No placeholder glyph.** The shop cards of Detergent and Mop showed a "·" chip (store screenshot 08, clip 5).
  - Root cause: both floor tools have `hotkey = 0` (mouse only: the user's call, `FLOOR_TOOLS.md` 1). The HUD strip hid
    their chip (`.tool--plain`), but `ShopView.BuildCard` printed a "·" placeholder in the same chip.
  - Fix: a tool without a hotkey gets no chip anywhere: `.tool--plain` on the strip, `.ucard__head--plain` in the shop
    (name at the card edge). The floor tools stay mouse-only (a first draft gave them keys 9 / 0; reverted, it overrode
    the user's 2026-09-20 "no hotkeys, mouse only").
  - `ToolDefinition.hotkey` is a number-row slot, 1..10 (10 = the 0 key), with 0 = none. `HasHotkey` / `HotkeyLabel`
    feed both `CleaningHUD.BuildToolRows` and `ShopView.BuildCard`.
  - `ToolController` maps the slots through one `NumberRow` table (`Key.Digit1..Digit9, Digit0`), replacing eight
    hard-coded `digitNKey` lines, so the keys and the chips cannot drift apart. Slots 9 / 10 are unused.
- **Price pills.** `.ubuy` is a Button whose price is a child Label (`.ubuy__label`). A Button lays its child out as a
  column from the top, so the price sat at the top of its 30 px pill. `.ubuy` now has `justify-content: center` and
  `align-items: center`. This covers the tool rows and the two INSPECTION cards (`.ubuy.ibuy`, including OWNED).
- **Verify.** Use the probes in `Tools/steam_art/trailer/fix_probes/` (`README.txt`):
  - `v2_shop_pills.cs`: every label centre within 0.5 px of its pill's centre;
  - `v3_key_chips.cs`: carpet tools `[1]..[8]`, floor tools plain (no chip) on the strip and in the shop;
  - The USS change rebuilds the UI tree on import (section 7 trap), so restart Play after it.

## 10. Pause menu (2026-09-27, `PAUSE_MENU.md`)

- A third button in the top-right row, **Menu** with an `Esc` chip, after Upgrades and Reset.
- A modal `#menu` layer between `#shop` and `#fx`: Resume, Volume slider, Credits page, Quit. `PauseMenu` (228) owns it;
  it pauses like the shop (`UiPaused` + `UiModal`), and the two never stack.
- It reuses `.shop__scrim` for the scrim. Its own classes are `.menu*` and `.credits*`, at the end of `CarpetHud.uss`,
  including a restyled slider and scroller (the stock runtime theme draws them dark).
- USS has no `:first-child`; the first credits heading gets `.credits__head--first` from code.

## 11. Localization (2026-09-27, `LOCALIZATION.md`)

- Every text is an English key of `Resources/Loc`. An **unnamed** label in the UXML is static text: `HudLocalizer` (order
  -50, on `CarpetUI`) registers it once and re-texts it on a language switch. A **named** label belongs to C#, which
  translates it itself. A new static label needs its text added to `Tools/loc/strings.json` and the 30 language files.
- Nothing may assume English widths: use `min-width`, let rows wrap, or equalize from C# (`.urow__caption`,
  `.menu__row-caption`). `CarpetDebugApi.LocOverflow()` lists every label that does not fit, per language.
- `HudLocalizer` puts `lang-<code>`, `script-<base|arabic|thai|cjk>` and `lang-rtl` on `.hud` for script-specific USS
  (letter-spacing, the Arabic pay-slip row height, the mirrored rows in RTL), and the script's Noto font inline on the root.
- Single-letter `.kbd` chips show the player's keyboard layout (QWERTZ: Z -> Y).
- The pause menu has a Language drop-down (`LanguagePicker`, style section "language drop-down").
