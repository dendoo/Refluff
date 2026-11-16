# Economy: pay per carpet, tool upgrades, the payout card (2026-09-24)

User request (RU):
- Every handed-over carpet pays: a base rate, a speed bonus, a bonus for 100 % clean, a bonus for 100 % dry, and a
  combo when both of the last two land.
- The pay slip must appear line by line, beautifully, with VFX.
- Each tool gets upgrades: POWER (strength) and AREA (area of effect).

User decisions:
- The shop opens only from its HUD button (**Upgrades**, key **U**). There is no automatic shop screen.
- Money and levels persist in a JSON save.
- Levels run 0..6.
- Where today's power tuning sits on that ladder, per tool: scraper 3 ("the 4th by count"), dry vac 1, hose 2, foam 3,
  brush 0, squeegee 3, wet vac 0, dryer 0, detergent 0, mop 0. AREA starts at today's size for every tool.
- Pay follows the dirt that was really on the carpet. R / Reset now roll a new carpet, and a spotless one pays about 0,
  so re-rolling is pointless.
- The dry bonus pays at 100 % even on a carpet that never got wet, because every line is scaled by the job value anyway.
- UI text is English, like the rest of the HUD.

## 1. The pay slip (`Scripts/Economy/Economy.cs`, `PayoutResult.cs`)

**When.** `CarpetHandover.StateChanged(FlyOut)` is the only "carpet handed over" signal; only `Begin()` raises it.
- `Economy.Pay()` calls `sim.RefreshStatsSync()`, because the async stats can be 10 frames old.
- It computes the slip, credits the money and saves at once. It then raises `PayoutReady`.
- The HUD only replays the slip, so quitting mid-animation never loses money.

**Job value.** V = clamp01(`DirtScore(sim.InitStats)` / `dirtRef`).

| Carpet | DirtScore (measured over 40 seeds) |
|---|---|
| Game rolls | median 0.39, p90 0.73 |
| Heaviest rolls | 1.05 (seed 29), 1.37 (seed 54); 4 of 40 are spotless |
| Scripted fully soiled carpet, `Reset(seed, 1)` | 1.53 – 1.57 |

- `dirtRef` = 1.0, so a heavy rolled carpet is a full job and the scripted carpet clamps to V = 1.
- Scaling every line by V is what makes R-rerolling worthless: a spotless carpet has V = 0 and pays nothing.

**Lines.** All values live in `Settings/EconomySettings.asset`; each line is rounded to 5.

| Line | Condition | Amount |
|---|---|---|
| Base pay | always | 120 V |
| Speed bonus | full up to par; linear down to 0 at 2.5 × par | up to 80 V |
| Spotless | Clean ≥ 0.995 | 60 V |
| Bone dry | 1 − Wetness ≥ 0.995 | 40 V |
| Combo | Spotless and Bone dry | 100 V |

- The 0.995 threshold is exactly what the HUD bar rounds to "100 %".
- The best slip is 400 for a full job.

**Clock and par.**
- The clock is the difference in `CarpetSimulation.SimTime` since the carpet last entered Idle: the first carpet, R /
  Reset / debug resets, and the landing after FlyIn.
- It therefore stops under the Steam overlay and while the shop is open (both pause the sim), and it does not punish a
  frame rate below 30. It is guarded against `SimTime` restarting without an Idle entry.
- par = 45 s + (600 s − 45 s) · V.

**Calibration (2026-09-24).** A hint-following bot (`scratchpad/bot.sh`) rasters the whole carpet with each suggested
tool at 300 mm/s.
- Factory tools, `Reset(101, 1)`:
  - Clean passes 0.995 after 10 passes (≈760 s).
  - During drying it slips to 0.9947, because dissolved dirt redeposits as the pile dries (the "wicking rings" term).
  - One more hose → wet vac ×2 → dryer cycle gives Clean 0.9995 and Dry 1.0000: **both 100 % are reachable together.**
  - Total ≈ 24 min for the naive bot.
- Level-0 tools, seed 29 (V = 1): Won at 1012 s, fully dry at ≈1200 s, Clean 0.9924 without a rinse.
- A player who works only the dirty patches beats the bot by a wide margin, hence par 600 s for V = 1.

**Payout math checks** (`EconomyDebugApi.Preview`; all as designed):

| Case | Result |
|---|---|
| V 0 | 0 in every line |
| 0.999 / 1 / 200 s / V 1 | 120 + 80 + 60 + 40 + 100 = 400 |
| Clean 0.9949 | no Spotless, no Combo: 240 |
| 0.995 / 0.995 | full 400 |
| 525 s at par 300 | speed 40 |
| 750 s | speed 0 |
| V 0.5 | 200 |
| V 0.1 | 40 |

## 2. Upgrades (`Scripts/Economy/ToolTuning.cs`, `Economy.cs`)

**Levels, multipliers, prices.**
- Two tracks per tool, POWER and AREA, each 0..6.
- Multipliers against the **factory tuning** (1 = the tool as it was before the economy):
  - power = 1.15^(L − powerAnchor[t]);
  - area = areaMax^((L − areaAnchor[t]) / maxLevel), where area is the swept area. `areaMax` = 3 (user, 2026-09-24:
    "at most 3×, smoothly from the first to the last level"). The levels are evenly spaced in ratio, ×1.2009 each:
    1, 1.20, 1.44, 1.73, 2.08, 2.50, 3.00.
- The shop shows each row as a multiple of the player's own start: "×1.44 → ×1.73". POWER ends at ×2.31, AREA at ×3.00.
- AREA at level 6, verified in Play:
  - blades: scraper 360 mm, squeegee 660 mm, wet vac 480 mm, mop 960 mm;
  - disc radii: dry vac 87 mm, hose 69, foam 121, brush 78, dryer 234, detergent 147.
- Price of L → L+1 = round5(40 · 1.35^L); ×1.2 for AREA, ×0.6 for the floor tools.
  - POWER: 40, 55, 75, 100, 135, 180.
  - All 20 tracks: about 9.4 k.

`ToolTuning` is static, because the sims, the controller and the head view all need it without a scene reference.
- Reset in `SubsystemRegistration` (domain reload is off). Every multiplier defaults to 1, so a scene without an Economy
  plays factory tuning.
- **`Neutral`**: every multiplier reads 1.
  - `CarpetDebugApi.Reset` turns it on (`ToolTuning.EnterScriptedRun`), so every scripted recipe and every number in
    SESSION_LOG keeps meaning factory tools whatever the player's save holds.
  - `EconomyDebugApi.LiveTuning(true)` opts scripts out.

**AREA: one entry point, `ToolTuning.Effective(t)`** (a copy of the `ToolTable` struct).

| Kind | What grows |
|---|---|
| Blades (scraper, squeegee, wet vac, mop) | `lengthMm × area` |
| Discs | `radiusMm` and `spacingMm × √area` |

- Readers:
  - `ToolController.StrokeSegment`, and through it `EnqueuePlow` (the squeegee width).
  - The three `SetCursor` calls: the outline, `_ToolCursor`, and the wet-vac dip.
  - `CarpetSimulation._ToolSpacing`, refilled on every upload, because K_Fiber must see the real stamp spacing.
  - `ToolModelView`: every head scales uniformly to preserve its proportions. Blades use area on all three axes,
    matching effective blade length; discs use √area. The first-person shaft follows the scaled HandleSocket.
- The floor sim, the debris capture radius and the plow reach follow the stamp automatically.
- The factory table stays in use for the HUD strips, `ToolModelPostprocess` and the builder.
- A wider disc also stays over a cell longer (2R / v), so AREA gives discs up to +26 % dose per pass. This is accepted.

**POWER: `_ToolPower[4]`**, declared once in `DebrisCommon.hlsl`.
- It is uploaded as (P − 1), so an unset uniform means factory power.
- It multiplies only effect RATES, never pile styling (f / b / vb), transport or swirl:

| Tool | Carpet (`K_Stamp`, `K_Chemistry`, plow) | Floor (`K_FInject`, `K_FChem`) |
|---|---|---|
| Scraper | crack, chip, wet crust → mud; chips clamped to the crust present | residue (clamped) |
| Dry vac | grit 6.0, mud 0.8, foam 2.0 | dust, residue, foam |
| Hose | inflow 150, agitation 0.5, push 400 (clamps stay) | same |
| Foam | soap 3, water 6, foam 12 | same |
| Brush | agitation 1.5; cap `min(agit, 2·max(P,1))`; crust abrasion | agitation, same cap |
| Squeegee | pile left by the blade `clamp(SQ_W_DAMP·P^-0.3, 0.20, 0.34)`; foam crush × P | — (pure push) |
| Wet vac | water 200, pile 12, flush, foam 60 (`WETVAC_W_MIN` untouched) | water, foam, dust |
| Dryer | `DRYER_BOOST·P` | `F_DRYER_BOOST·P` |
| Detergent | — | DET_RATE, DET_SOAP, DET_K_RES (water untouched) |
| Mop | — | k, SOAK, FOAM (EAT untouched, so power is not just speed) |

No UAV was added; `K_Stamp` stays at 8.

**Measured in Play** (scripted `StepAll`, 2026-09-24):

| Test | Low level | High level |
|---|---|---|
| Hose AREA (radius 40 → 50.4 mm): one stroke, wet cells after soak-in | 23.1 % | 29.9 % (×1.30) |
| Scraper AREA (blade 120 → 190 mm, the former ×1.59 top): crust removed in one pass | 25.2e-3 | 39.2e-3 (×1.55; ×1.59 expected) |
| Squeegee AREA ×3 (660 mm), straight pass over soaked, foamy pile at 600 mm/s: behind the blade | h 3.89 → **0**, F 2.06 → **0** | |
| Squeegee ×3 on an arc (radius 350 mm, turning 140°): middle and outer end | **0** water, **0** foam | |
| Squeegee ×3 near the pivot (inner end at radius 20 mm): what is left at radius 60 – 145 mm | h 0.78 mm | factory 220 mm blade on the same relative turn (R 117 mm): h **1.70 mm** |
| Dry vac POWER (×0.87 → ×2.01): grit lifted in one pass over a scraped band | 34.7 % | 54.7 % |
| Dryer POWER (×1 → ×2.31): 0.5 s over a soaked spot, pile w | 0.184 → 0.088 (−52 %) | 0.187 → 0.032 (−83 %) |
| Squeegee POWER: pile w after one pass | 0.34 (level 0) | 0.30 (level 3 = factory, unchanged); 0.264 (level 6) |

## 3. Save (`Scripts/Economy/SaveStore.cs`)

- **Where.** JsonUtility in `Application.persistentDataPath`: `carpet_save.json` in a player, `carpet_save_editor.json`
  in the Editor.
  - Today that is `%USERPROFILE%/AppData/LocalLow/VahovGames/Refluff/` (`companyName`/`productName`; until 2026-09-25 it
    was `DefaultCompany/CarpetCleaning`, and the editor save was copied across).
- **How it writes.** Atomic: a temp file, then `File.Replace`, or `File.Move` for the first save. It writes on every
  payout, every purchase and every debug change.
- **A broken file** is moved to `.bak` (an old `.bak` is overwritten); the game starts fresh with a warning.
- **Fields:** version, money, lifetimeEarned, carpetsDelivered, perfectClean, perfectDry, combos, bestSeconds, power[10],
  area[10], items[2] (one-time purchases by `ShopItem`, 1 = owned; added 2026-09-24, `HIGHLIGHTS.md`), and since 2026-09-30
  achieved[10] + toolsUsed (`ACHIEVEMENTS.md` 3). `Normalize` pads or clamps them, so a hand-edited or older file is safe: a
  file from before `items` existed reads "nothing owned".
- **Verified:** money + a level survived Play stop/start (loaded 1017 and brush power 3, P 1.52). Garbage JSON → `.bak`,
  money 0, `loadNote` says why.
- **Steam Cloud:** Auto-Cloud on the partner site (root `WinAppDataLocalLow`, path `VahovGames/Refluff`, pattern
  `carpet_save.json`), no code (`STEAM.md` 11).
- **Other writers:** `Economy.SaveNow()` lets another system persist fields it owns in the same file (SteamAchievements).

## 4. The payout card (`Scripts/UI/PayoutView.cs`, `UiFx.cs`, `EconomyHud.cs`)

### 4.1 Timeline
The whole card is a timeline in seconds built from the slip. All durations are in `EconomySettings`, header "Payout
card timing". A combo slip runs about 8.1 s.

| t (s) | What happens |
|---|---|
| 0.30 | the card drops in (overshoot); caption CARPET DELIVERED, "Heavy soil · 2:30", carpet number |
| 0.85, then every 0.55 | lines slide in: Base pay, Speed bonus, Spotless, Bone dry, Combo |
| per line | the amount counts up with ticks rising in pitch, then punches (scale 1.34 → 1); a light band sweeps the line; glow + sparkles (+ ring on the perfect lines); coin clink (+ glitter on perfect lines) |
| missed line | shown dimmed with its reason ("97% clean", "100% clean + 100% dry"), a soft thud, and a shorter beat (0.3 s) |
| combo | the COMBO! stamp slams from 2.6× onto the card's corner; the card shakes 0.4 s; gentle flash; two confetti bursts in five colours; ring + glow + sparkles; rays turn behind the card; stamp sound + glitter |
| after the lines | the divider draws; TOTAL counts up accelerating (ticks speed up), punches, shock ring + ka-ching |
| total pop + 0.04 | 14 coins fly on Bezier arcs into the wallet, spinning edge-on and back; each landing adds its share, bumps the wallet and clinks softly |
| end | 1.4 s hold, then the card lifts away |

- **Skipping:** a click on the card or Space jumps to the total with the coins taking off now (plus the ka-ching). A
  second skip closes the card.
- **Forced finish:** a new carpet (FlyIn), R / Reset (`Economy.JobRestarted`) or opening the shop finishes the card at
  once. The wallet then shows the final amount.
- **Rules:**
  - The card is hidden with `display:none`, never opacity or picking. `CleaningHUD.SetBlockersLive` re-enables picking
    on every `.ui-blocking` element after each stroke.
  - Every Button in the HUD is `focusable = false`, because Enter is the UI Submit key and would re-click a focused Buy.

### 4.2 UI particles (`UiFx`)
- **Pool.** 220 plain VisualElements in the top `#fx` layer, `usageHints = DynamicTransform | DynamicColor`, picking off.
- **Kinds:** sparkle, glow, ring, confetti, coin. The look is set once at birth by USS classes: `.fx--sparkle`, and tints
  `.fx--gold` / `mint` / `amber` / `violet` / `rose`.
- **Per frame:** only translate / rotate / scale / opacity change, which never triggers a layout pass.
- **Motion is analytic in age:** ballistic with linear drag and gravity; Bezier for coins; confetti tumble (scaleX cos)
  and flutter. So `EconomyDebugApi.PayoutSeek(t)` rebuilds the exact frame: it clears the particles and re-fires every
  event before t, backdated and silent.
- **Cost at the combo peak:** 125 particles alive, `UiFx.Update` 0.43 ms CPU in the Editor.
- **Textures** are procedural, baked by `HudArtBaker` into `UI/Textures/fx_*.png` and `icon_*.png`: coin, sparkle, glow,
  ring, confetti, rays, stopwatch, drop, star, bolt, expand. They are ≤ 64 px (the panel's atlas limit), except the
  256 px rays.

### 4.3 World glints (`Scripts/Economy/CelebrationFx.cs`, `Shaders/FxSparkle.shader`)
- **Setup.** A Shuriken system `CarpetStage/CleanSparkles` in LOCAL space, so the glints ride the rug during the
  hand-over flight. `Clear()` at Away, or they would pop up where the carpet used to lie.
- **Shader.** Additive and unlit; brightness from the material `_Intensity` 5 × `[HDR] _Tint`, because vertex colours
  are ≤ 1 and the bloom threshold is ≈1.2.
- **Amount:** 24 for any carpet; +110 gold for Spotless; +60 cool white for Bone dry; ×1.5 for a combo. Sizes 3 – 8 cm,
  life 0.35 – 0.8 s.
- **Configuration.** `CelebrationFx.Configure()` holds the whole setup (no rate, no bursts, not on awake;
  size/rotation/alpha over lifetime); `CarpetSceneBuilder.BuildCarpetRoot` calls it. The texture is
  `FX/fx_sparkle_world.png`, mipmapped, and NOT under `Textures/`, where every texture becomes a rug picture.
- **Seek gotcha:** `ParticleSystem.Simulate` is applied lazily; the time only landed with the next call. `DebugSeek`
  flushes it with a second tiny `Simulate`.

### 4.4 Sounds
See `AUDIO.md` 10: `UiAudio` with 12 voices, clips in `UiSoundBank`, placeholders from `UiSfxSynth` baked to
`Audio/Generated/ui_*.wav`.

## 5. HUD pieces and the shop (`Scripts/UI/ShopView.cs`, `UI/CarpetHud.uxml` / `.uss`)

- **Meters card** (`#bars`, always visible):
  - A ledger row: stopwatch + job clock + "par m:ss" (amber `is-late` once past par; "spotless" for V = 0).
  - The wallet pill, where the payout coins land.
- **Top right:** Upgrades (U) beside Reset. A gold dot shows while any upgrade or unowned one-time item is affordable.
- **Shop (`#shop`, modal):**
  - A scrim (`ui-blocking`; a click closes) and a panel with the wallet and Close (Esc). U toggles.
  - Cards are built from `ToolTable` (the only data-driven list the HUD rules allow in C#): carpet tools two per row,
    floor tools in their own column.
  - Each card has a POWER row (bolt, gold pips) and an AREA row (expand, teal pips): 7 pips (level 0 lit = the tool
    as bought), "+32% → +52%", and a price button or MAX.
  - Buying pops the new pip with sparkles + ring, plays the buy sound and counts the wallet down. Without the money the
    button shakes and plays deny.
  - U / Esc respect `tools.Suspended` (the Steam overlay).
  - **One-time items (2026-09-24, `HIGHLIGHTS.md`).** INSPECTION, under the floor tools: Moisture highlight (Z, $250) and
    Dirt highlight (X, $400). They are two fixed cards in the UXML that `ShopView` binds by name. A card shows its price, or
    OWNED plus its key once bought. Buying runs `Economy.TryBuyItem`, which raises `ItemChanged`. The prices live in
    `EconomySettings` "One-time items".
- **While the shop is open:**
  - `CarpetSimulation.UiPaused` stops the carpet and floor sims, and with them the job clock (`IsPaused = Paused ||
    UiPaused`).
  - `ToolController.UiModal` stops strokes and hotkeys, including H / N in `CarpetHandover`. Clearing it keeps the tools
    blocked until both mouse buttons are up, so the closing click never becomes a stroke.
  - Both are flags of their own: `SteamService` only lifts the pause it set itself.
  - Verified: `SimTime` stayed at 0.0200 across ticks with the shop open; both flags clear on close.

## 6. Files
- New:
  - `Scripts/Economy/`: `ToolTuning`, `EconomySettings`, `SaveStore` (+ `SaveData`), `PayoutResult`, `Economy`,
    `CelebrationFx`, `EconomyDebugApi` (+ `.Hud.cs`).
  - `Scripts/UI/`: `EconomyHud`, `PayoutView`, `ShopView`, `UiFx`, `UiEase`.
  - `Scripts/Audio/`: `UiAudio`, `UiSfxSynth`, `UiSoundBank`.
  - `Editor/UiSoundBaker.cs`, `Shaders/FxSparkle.shader`, `Materials/FxSparkle.mat`.
  - `Settings/EconomySettings.asset` and `Settings/UiSoundBank.asset` (both created only when missing).
- Changed:
  - Compute and C#: `DebrisCommon.hlsl` (`_ToolPower`), `CarpetSim.compute`, `FloorSim.compute`, `CarpetSimulation`
    (`UiPaused` / `IsPaused`, spacing / power upload), `FloorSimulation`, `ToolController` (`Effective`, `UiModal`),
    `ToolModelView` (head scale), `CarpetHandover` (H/N gate), `CarpetDebugApi` (Neutral in `Reset`, `uiPaused` in
    `SteamJson`).
  - UI and editor: `HudArtBaker` (generic `BakeTexture` + economy art, re-imports the USS), `CarpetSceneBuilder`
    (`BuildEconomySettings`, `BuildSparkleMaterial`, `BuildEconomy`, particles under the stage, `EconomyHud` + `UiAudio`
    on CarpetUI), `CarpetHud.uxml` / `.uss`.
- Scene: the `Economy` object is kept across Build Scene (like `Steam`). The rest is rebuilt.
- Later (2026-09-24, the inspection lamp, `HIGHLIGHTS.md`): `Scripts/Economy/ShopItem.cs`, `Scripts/SurfaceHighlight.cs`,
  `Shaders/HighlightCommon.hlsl`, `UI/Textures/icon_eye.png`.

## 7. Testing (`EconomyDebugApi`; JSON uses the invariant culture)

| Call | What it does |
|---|---|
| `EconomyJson()` | wallet, counters, clock / V / par, Neutral, save path + load note, per-tool levels, multipliers, effective sizes, prices, last slip |
| `Preview(clean, dry, seconds, V)` | the pure slip maths |
| `Grant(n)`, `ResetSave()`, `Reload()`, `SetLevel(tool, stat, level)`, `Buy(tool, stat)` | stat 0 = POWER, 1 = AREA |
| `LiveTuning(bool)`, `SetNeutral(bool)` | see section 2 |
| `ShowPayout(clean, dry, seconds, V)` | plays a made-up slip without paying |
| `PayoutSeek(t)`, `PayoutResume()`, `PayoutJson()` | frozen, exact frame of the card |
| `ShopOpen(bool)`, `ShopJson()` | the shop (`oneTime` lists the items) |
| `BuyItem(item)`, `SetOwned(item, bool)` | one-time items: 0 = moisture highlight, 1 = dirt highlight (`HIGHLIGHTS.md` 5) |
| `SparkleSeek(clean, dry, t)` | world glints simulated to t |
| `CaptureHud("Screenshots/x.png")` | see below |

**Test money (Editor tool, `Editor/EconomyDebugWindow.cs`).** Menu **CarpetCleaning > Economy**:
- **Add $10,000 / $100,000 / $1,000,000 (test).**
- **Test Money Window...** Shows the live balance and has +1k / +10k / +100k / +1M buttons, a custom amount (Add or Set),
  "Wallet to $0 (keeps upgrades)", and "Reset all progress...".
- **Reset Progress...** (2026-11-16): also under **Tools > CarpetCleaning > Reset Progress...**. After confirmation,
  resets all saved Editor progression, including upgrades, items, counters and local achievements. Works in Edit and
  Play; keeps player/Steam progress and preferences. See [PROGRESS_RESET.md](PROGRESS_RESET.md) for scope and failure
  handling. `EconomyDebugWindow.ResetProgress()` is the public CLI hook (no dialog).
- **In Play** it goes through `Economy.DebugGrant`, so the HUD wallet rolls up, the shop refreshes and the game saves.
- **Outside Play** it edits `carpet_save_editor.json` directly, and the next Play loads it.
- Money is clamped to [0, int.MaxValue]. `EconomyDebugWindow.Grant / SetMoney / Balance` are public for CLI tests.
- **Unlock highlights / Remove highlights (test)** (2026-09-24): owns or drops both one-time items, keeps the wallet.
  `SetHighlightsOwned(bool)` is public. It works in Play and in Edit mode, like the money items.
- **Fixed after review (2026-09-24).** The HUD wallet roll interpolated in float. From ≥ 2 147 483 584 (reachable only with
  this tool) float rounds up to 2^31, and the wallet showed "$-2,147,483,648" until the next change. It now interpolates
  in double. Verified: Set int.MaxValue → Set 0 shows $2,147,483,647 on the first frame, then $0.
- **Hardening.** After a hot reload during Play clears `Economy.Instance`, the tool finds the component in the scene.
- Verified 2026-09-24:
  - outside Play: the menu +100k, −5k, Set int.MaxValue and +1k all clamp;
  - in Play: +$1M lands via Economy, the window shows "(Play mode, live)", and the HUD wallet target updates on its next
    frame.

**`CaptureHud`.**
- `VisualElementCaptureExtensions.TryCaptureIntoRenderTexture` of the panel, composited over a `Camera.Render`, into
  `Assets/Screenshots/`.
- It works while the Editor is unfocused, when the Game view never repaints: the first HUD screenshots of the project.
- Captures of 2026-09-24: `econ_hud_idle`, `payout_t*` / `payout2_t*` (card frames), `flow_flyout` (a real hand-over),
  `sparkle_world`, `shop_open`.

**Gotchas.**
- Re-importing `CarpetHud.uss` during Play makes UI Toolkit rebuild the tree (editor live reload) and the HUD scripts
  lose their elements: stop / start Play.
- A stylesheet imported before its PNGs existed shows the missing-image glyph until it is re-imported
  (`HudArtBaker.Bake` now does that).
- A seek right after `ShowPayout` in the same call has no layout yet (particles at 0,0): seek in a later call.
- In an unfocused Editor, time-based HUD animations (wallet count-up, shop entrance) and USS transitions (a button's
  colour after `is-poor` / `is-max`) do not advance at all. A capture then shows the state the transition started
  from. To run real frames: `EditorApplication.isPaused = true; EditorApplication.Step()`. Each Step is one full
  player-loop frame of 0.02 s, including UI Toolkit. Pass the size to `CaptureHud(path, 2560, 1440)` while paused,
  because `Screen.width` then reads 640. Unpause before `editor_stop` (`HIGHLIGHTS.md` 5).

## 8. Open
- **Balance** is a first pass: the prices, the par, and V from `dirtRef` 1.0. Every knob is in the asset.
- **Not heard or seen live by the user:** the sounds (coin / bell are intentionally tonal struck metal, the rest
  noise-first, numbers in AUDIO.md 10), the feel of the real-time card, the 0.85 s flight window of the glints.
- **Squeegee on tight turns.** When the turning radius is below half the blade, water stays near the pivot. The plow
  already did this before the change (the factory blade leaves more), but with a 660 mm blade any turn tighter than
  330 mm triggers it.
- **Floor upgrades** do not affect the payout (the floor never does); floor power is barely visible because the tile
  already cleans in one pass.
- **Unity 6000.7 analyzer warnings** UAL0010 / UAL0013 on the new statics, like on the rest of the project: statics are
  reset in `SubsystemRegistration` instead of `AutoStaticsCleanup`.
