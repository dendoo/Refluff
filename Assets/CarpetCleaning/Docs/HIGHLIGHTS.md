# Inspection lamp: dirt highlight (X) and moisture highlight (Z) (2026-09-24)

Two one-time shop items. Once bought, a key switches each lamp on and off:

| Item | Key | Price | What glows |
|---|---|---|---|
| Moisture highlight | Z | $250 | wet pile, standing water, damp tile, splash drops: teal (damp) -> blue (soaked) |
| Dirt highlight | X | $400 | every trace of soil that still counts against Clean, crumbs on the floor: deep amber -> bright amber |

User decisions (2026-09-24):
- **Style: inspection lamp.** While a lamp is on, the carpet and the floor dim and desaturate, and the lamp's measure glows on top.
- **Prices:** $250 / $400. Dirt costs more because it serves both Spotless and the combo.
- **Where:** carpet and floor, including water, foam and crumbs.
- **Both at once:** allowed. A cell that is dirty AND wet shows amber and blue stripes, so the two colours never add up to a grey mush.
- **Nothing is simulated.** The lamp only shows what the sims already hold, and the pay is untouched.
- **The carpet lamp agrees with the HUD bars** (user, same day): while CLEAN (or DRY) shows 100 %, nothing on the carpet
  glows, however many traces the cells still hold. Below 100 % it shows what is left to do before the bar reaches 100 %
  (section 4.1). The floor has no bar and keeps its full lamp.

## 1. Economy side (`Scripts/Economy/`)

**Items and prices**
- `ShopItem.cs`: `enum ShopItem { MoistureHighlight = 0, DirtHighlight = 1 }`. `ShopItems` holds `Count`, `Name(item)` and `Key(item)`.
- `EconomySettings`, header "One-time items": `moistureHighlightPrice = 250`, `dirtHighlightPrice = 400`. The asset was synced.

**Save**
- `SaveData.items` is an `int[]` indexed by `ShopItem`; 1 = owned. `Normalize` fits it to `ShopItems.Count` and clamps each entry to 0..1.
- A save from before this change reads as "nothing owned". There is no version bump: this is the same fit-and-clamp migration as `power` / `area`.

**`Economy` API**
- `Owns(item)`.
- `ItemPrice(item)` returns -1 once the item is owned.
- `TryBuyItem(item)` takes the money, saves, then raises `MoneyChanged` followed by `ItemChanged(item, true)`.
- `AnyAffordable()` also counts unowned items, so the Upgrades badge lights up for them.
- `DebugSetOwned(item, bool)`.
- `DebugResetSave` / `DebugReload` raise `ItemChanged` for every item, so the lamps and chips follow a reset.

## 2. Shop (`UI/CarpetHud.uxml`, `CarpetHud.uss`, `Scripts/UI/ShopView.cs`)

**Layout**
- The two cards sit under INSPECTION in the FLOOR TOOLS column, below Detergent and Mop. That column was half empty, so the panel did not grow.
- They are fixed widgets, so they live in the UXML (HUD.md 5): `#item-moisture` and `#item-dirt`.
- Each card has a keycap, a name, an icon + text row (`#item-*-text`), and a price button (`#item-*-buy` > `#item-*-price`).
- `ShopView.BindItem` finds them by name.

**States**
- `Refresh()` sets the price label or `OWNED`.
- On the button: `.is-poor` / `.is-owned`. On the card: `.is-owned` (a wash in the lamp colour).
- Owned text: "Owned: press X to switch it on and off".

**Buying**
- `OnBuyItem` mirrors `OnBuy`:
  - owned already, or not enough money: shake + Deny;
  - otherwise: Buy sound (pitch 1.04 moisture / 1.12 dirt), a sparkle burst, ring and glow (`fx--sky` / `fx--amber`), and a small card punch.
- `ShopJson()` lists the items under `oneTime`.

**Style and icon**
- USS tokens: `--hl-dirt*` and `--hl-wet*` on `.hud`, and `--fx-sky`.
- The icons are `icon--drop` tinted blue, and a new `icon--eye` (`Textures/icon_eye.png`, SDF almond + pupil, baked by `HudArtBaker`).

## 3. The lamp (`Scripts/SurfaceHighlight.cs`, on CarpetUI, order 225)

**Keys** (`Update`)
- Z and X through `Keyboard.current`.
- Ignored while `tools.Suspended` (Steam overlay) or `tools.UiModal` (shop): the same gate as H / N. Still live during the carpet flight.
- `Toggle(item)` when the item is not owned: Deny, plus a 2.2 s toast "Dirt highlight: buy it in Upgrades (U)" (`CleaningHUD.Toast`).
- `Toggle(item)` when owned: switch the lamp. On plays `UiSfx.Open`, off plays `UiSfx.Close`; pitch 1.12 for dirt, 0.94 for moisture.

**State**
- Both lamps are off on every Play and every launch; this state is not saved.
- If an item stops being owned (save reset, debug), its lamp is forced off.
- `Changed` is raised on every switch.

**Globals** (`LateUpdate`, unscaled time)
- `_Highlight` = (dirt amount, moisture amount, dirt sweep, moisture sweep).
- Amounts fade in over `fadeIn` 0.25 s and out over `fadeOut` 0.2 s, smoothstep-eased.
- Switching on from dark runs a scan ring from the carpet centre to 2700 mm over `sweepSeconds` 0.55 s. It covers the carpet and the whole floor domain.
- `_HighlightTune` = (`dirtRef` 0.02, `wetRef` 0.02, `dim` 0.62, `glow` 1). These are inspector fields, pushed every frame, so they can be tuned live.
- `OnDisable` zeroes `_Highlight`. Shader globals outlive Play mode: without this the Scene view keeps the lamp after Stop (verified: (1,1,1,1) before Stop, (0,0,0,0) after).

**HUD**
- A `.kbd.lamp-chip` sits in each meter head: X beside CLEAN, Z beside DRY. Its display is none until the item is owned.
- `.is-on` fills the chip amber / blue while its lamp is on. `CleaningHUD.RefreshLampChips` follows `SurfaceHighlight.Changed`.
- The chips are in `#bars`, never pick, and stay visible while stroking.

## 4. Shaders (`Shaders/HighlightCommon.hlsl`)

Every user branches on `HL_Active()`, a uniform test, so a dark lamp costs nothing.

**Measures.** They are the pay slip's own, so what glows is exactly what keeps a bar under 100 %.
- Carpet dirt `HL_CarpetDirt` = 1 crust + 0.8 mud + 0.6 stain + 0.9 grit + 0.7 slurry + 0.5 foam dirt. These are the `CarpetSimulation.DirtScore` weights.
- Carpet wet `HL_CarpetWet` = w + h / W_CAP (`Wetness`).
- Floor dirt = dust + residue + 0.7 slurry + 0.5 foam dirt.
- Floor wet = h / 6 + 0.06 wetFilm, in the carpet's unit.

**Response**
- `r = log2(1 + x/ref) / log2(101)`, with a cut-off at 0.04 ref.
- With ref 0.02: x 0.005 -> 0.05, 0.02 -> 0.15, 0.1 -> 0.39, 0.5 -> 0.70, 2.0 -> 1.
- A factor-2 change is one visible step anywhere between a trace and a caked cell.
- Intensity = smoothstep(0, 0.03, r) x (0.28 + 0.72 r). The quick onset keeps a trace from vanishing in the dimmed rug.

**Colour**
- Dirt ramp: (1, 0.24, 0.01) -> (1, 0.38, 0.03) -> (1, 0.54, 0.10).
- Wet ramp: teal (0.02, 0.62, 0.72) -> (0.08, 0.45, 1) -> (0.28, 0.52, 1).
- Glow = ramp x intensity x 1.1 x `glow`. It stays saturated because the neutral tonemapper turned a white-ish peak into flat cream.
- Since 2026-09-26 the scene uses the ACES tonemapper (user decision); the lamp was tuned and measured under Neutral.

**Dim**
- The luminance is compressed: `lum' = (1 - d) lum / (1 + 2.5 d lum)`.
- 85 % desaturation.
- A plain x0.38 left the white tile light grey (203 -> 174 sRGB) next to the darkened rug. Compressed, the tile ends near 100.

**Both lamps on**
- Where both measures are present, the amber and blue glows alternate in 28 mm diagonal stripes.
- 12 mm (~9 px at the play camera) blurred to grey-blue.

**Scan ring:** a 70 mm gaussian front, gone by the end of the sweep. Inside it the lamp is on, outside it is not yet.

**Splash drops** (`DebrisDrop.shader`, since 2026-09-25; DEBRIS_SYSTEM.md "Splash drop look"). Until then the drop shader did not include the
lamp, so the "splash drops blue" in the table above was not true.
- A drop calls `HL_Terms(0.7 x C, 2.0, cmm, 1, 0)`. Its water measure is a pool's (2.0: full blue); its dirt is the slurry concentration it
  carries, weighted like the floor's slurry. There is no level, as on the floor.
- The body is dimmed and the white sun glint is removed (x (1 - dim)). The glow is added over the streak's coverage, so under Z a splash reads
  as bright blue comets, and under X as dimmed grey streaks (amber only for muddy spray).
- The `hl_*` reference captures of 2026-09-24 predate this: their drops were 1-2 px grey specks.

### 4.1 The carpet follows the HUD bars (the level)

**Why.** A bar shows 100 % once the carpet-wide mean is within 0.5 % of the start: `ShownPercent` rounds, and 0.995 reads as
100. That is also the Spotless / Bone dry threshold. A plain per-cell lamp still lit the leftover traces then: at 99.6 %
clean, all 184,320 cells of a scaled fully soiled carpet still held dirt, and the whole rug glowed orange
(`hl_level_100_pair`, left).

**The level.** `CarpetSimulation.WaterLevel` finds the value `a` at which the carpet, with every cell cut down to `a`
(the sum of min(value, a)), holds exactly what the bar tolerates at 100 %. That is 0.005 x the starting DirtScore for
CLEAN, and 0.005 for DRY.
- A carpet cell glows with what it holds **above** `a`: `HL_Response(max(x - a, 0))`.
- Cleaning or drying every cell down to `a` brings the bar to exactly 100 %, and each cell's glow fades out exactly when it gets there.
- A soiled carpet gets a tiny level: 0.0079 on the fully soiled carpet, so all 184,320 cells glow and nothing is left as a hole.
- The level climbs as the bar nears 100 %.
- While the bar shows 100 % the level is `NoCut` (1e30): nothing glows.
- The level is capped (keep <= 0.999), so a bar under 100 % always leaves something lit.
- An earlier variant, "the least-soiled cells up to the budget go dark", left dark holes in heavy dirt: its cut on the fully
  soiled carpet was 1.26. The level has no such holes.

**Where the numbers come from.** `K_Reduce` runs every 10 frames and also fills two log histograms: the per-cell DirtScore
and the wetness (w + h / W_CAP), the bars' own measures.
- 60 bins per measure, 3 per octave, covering 2^-16 .. 2^4. Each bin holds an amount (x512 fixed point; reduced on 2026-11-15 to keep the 512x640 raster below uint32 overflow at cap16) and a cell count.
- They travel in the same stats buffer as the 12 sums: 256 uints, one async readback.
- `ApplyStats` computes `Cleanliness`, `Wetness`, and both levels (`LampCutDirt`, `LampCutWet`) from that one snapshot, and pushes `_HighlightCut`.
- So the lamp goes dark in the same frame the HUD label turns 100 %.
- `CleaningHUD` prints the bars through the same `CarpetSimulation.ShownPercent`.
- `WaterLevel` is exact at the bin edges and linear in between.

**Who follows it**
- The carpet surfaces: fibres, carpet water, and carpet-domain foam, through `HL_ApplyCarpet` / `HL_ApplyOverCarpet` / `HL_CarpetCut()`.
- Crust crumbs lying on the carpet, through `CarpetCoversCMM`. A crumb on the tile keeps glowing.
- The floor, floor water, floor foam and crumbs on the tile have no bar, so they pass level 0.

**Per surface**

| Shader | What the lamp does |
|---|---|
| `CarpetFibers` (ForwardLit) | `HL_ApplyCarpet` on the lit colour; the glow keeps a little strand shading, x lerp(0.6, 1, shell t) |
| `CarpetWater` / `FloorWater` | only the water's own light dims; the refracted surface is already lit. The cell's glow is added x (1 - transmittance): murky water glows like the pile it hides, clear water adds nothing (no double glow, no dark murk) |
| `FloorSurface` | the whole floor dims; only the sim domain glows (fl / dp are 0 outside it and under the carpet) |
| `CarpetFoam` (both domains) | dims, glows with the cell under it (explicit-LOD taps, after the discards; carpet domain above the carpet's level) and turns x0.55 translucent so the lit pile shows through |
| `DebrisChunk` | a crust chip glows as a caked cell (1.0) above the carpet's level while it lies on the carpet, in full on the tile; the underside is dimmer |
| `DebrisDrop` | a splash drop (2026-09-25): moisture glows as a pool (2.0), dirt = 0.7 x the slurry C it carries, no level; the body is dimmed and the white glint removed |

Walls, the tool head and the cursor are unchanged.

## 5. Testing

**Hooks**
- `CarpetDebugApi.SetHighlight(which 0 moisture / 1 dirt, on, instant = true)`: skips the ownership check, for captures; pushes the globals at once.
- `CarpetDebugApi.ToggleHighlight(which)`: the key's path.
- `CarpetDebugApi.HighlightJson()`: the lamp state, plus `shown` (the bar percentages), `cut` (the two levels, 1e30 = nothing lit) and `mean` (the histogram means next to the stats they must match).
- `CarpetSimulation.HistMean(0 | 1)`, `LampCutDirt` / `LampCutWet`.
- Level check used on 2026-09-24:
  - read the four carpet RTs back and scale the dirt (or the water) by a factor, to put the carpet just under or over the 100 % mark;
  - `RefreshStatsSync`;
  - compare with an exact CPU bisection over every cell (the sum of min(d, a) = allowed).
- `EconomyDebugApi.BuyItem(i)`, `SetOwned(i, bool)`; the `items` object in `EconomyJson()`.
- Menu CarpetCleaning/Economy > Unlock / Remove highlights (test): works in Play through `Economy`; in Edit mode it edits the save file.

**Real frames without a focused Editor.** `UnityEditor.EditorApplication.isPaused = true; EditorApplication.Step();`
- Each Step runs one full player-loop frame synchronously (Update, LateUpdate, UI Toolkit, rendering), dt 0.02 s.
- That is how the fades, the scan ring, the USS transitions and the shop entrance were captured.
- `Screen.width` reads 640 while paused: pass the size to `CaptureHud(path, 2560, 1440)`.
- Unpause before `editor_stop`, or the next Play starts paused.

**Keys.** Queued Input System events do not reach the game from a CLI eval, even with the Game-view-focus and background settings relaxed. Paused + Step, or `InputSystem.Update()` from an eval, never produces `wasPressedThisFrame`. The literal Z / X press needs a focused Editor. Everything behind the key was verified through `ToggleHighlight`.

## 6. Verified 2026-09-24 (Play mode, captures in `Assets/Screenshots/hl_*`)

**Compile and scene**
- `recompile`: 0 errors.
- Shaders: 0 errors. CarpetFoam still shows its existing `FieldAt` / `FAt` "potentially uninitialized" warnings; that code is untouched.
- Build Scene wires `SurfaceHighlight`, and the scene is saved.

**Economy**
- The user's old save loaded with items false.
- Not owned: X was refused, and the toast read "Dirt highlight: buy it in Upgrades (U)".
- With $300: moisture affordable, dirt `poor`. The badge was lit, with every tool track already MAX, so the items alone light it.
- Purchases through the real shop button (`NavigationSubmitEvent` -> `Button.clicked`):
  - moisture: $300 -> $50;
  - dirt with $50: refused;
  - after +$400, dirt: $450 -> $50;
  - clicking again while owned: no charge.
  - Saved `items: [1, 1]`.
- After the purchases the badge went out, and `BuyItem` on an owned item returned `ok:false, price:-1`.
- A Play restart kept both items owned, the chips shown and both lamps off.
- Taking dirt away while lit switched it off, hid its chip and refused its key. The menu unlocked it again.
- In Edit mode the menu wrote `items [1, 1]` and kept money and levels.
- The user's save was restored byte-for-byte (md5 09b445ca...) after every test.

**Look**
- `hl_off` / `hl_dirt` / `hl_wet` / `hl_both`: the fully soiled carpet with a scraped band, a hose puddle, foam, a floor puddle and crumbs.
  - The scraped band glows dimmer amber.
  - The moisture view shows dry pile dark, wet pile blue with a teal edge, and the floor puddle and splash drops blue.
  - With both lamps on, the dirty puddle is striped and the clean floor puddle is solid blue.
- `hl_light_pair`: a level-0.3 carpet that looks clean to the eye, where the lamp shows a large dust and grime patch.
- `hl_light_vac_pair`: vacuumed spots drop to a deep amber, because the mud they still hold keeps glowing.
- `hl_sweep`: the ring 0.2 s after switching on.
- `hl_shop_*`: the cards. `hl_meters_owned` / `hl_hud_both_on`: the chips.

**Cost:** 40 stepped frames each at the test resolution. Lamps off 6.16-6.30 ms/frame, both lamps on 6.80-6.91 ms: about +0.6 ms.

**The level (4.1), same day.** Recompile 0 errors; `CarpetSim.compute` 0 messages, also after its first dispatch.

The histogram mean equals the stats and an exact CPU sum on every carpet tried (1.5718 / 1.5718 / 1.5718 fully soiled).

| Carpet | Bar | Level (exact) | Lit cells | Left to do (mean) |
|---|---|---|---|---|
| fully soiled | CLEAN 0 % | 0.0078588 (0.0078588) | 184,320 of 184,320 | 1.5639 |
| same, dirt x0.01 | 99 % | 0.0078591 (0.0078588) | all | 0.0078585 |
| same, dirt x0.004 | **100 %** | none | 0 (every cell still holds dirt) | - |
| level 0.3, patchy | CLEAN 0 % | 0.0012292 (0.001229) | 122,747 | 0.163 |
| same, x0.03 | 97 % | 0.0012431 | 120,565 | 0.0041 |
| same, x0.01 | 99 % | 0.0013523 (0.0013469) | 93,743 | 0.00082 |
| same, x0.006 | 99.4 % (reads 99) | 0.0015897 (0.0015796) | 59,921 | 0.00016 |
| same, x0.004 | **100 %** | none | 0 (121,358 cells hold dirt) | - |
| hose stripe | DRY 95 % | 0.025874 (0.025869) | 34,976 | - |
| water x0.2174 | DRY 99 % | 0.028368 (0.028317) | 28,482 | - |
| water x0.4 more | **DRY 100 %** | none | 0 (36,269 cells hold water) | - |

- The histogram level runs 0.4-0.6 % above the exact one: the linear interpolation inside a bin. That is harmless: while
  the bar reads below 100 % something always stays lit.
- Captures:
  - `hl_level_100_pair`: the same 99.6 % carpet, old lamp (whole rug orange) against the level (dark);
  - `hl_patch_grid`: 97 / 99 / 99.4 / 100 %; the lit area shrinks to the last spots, then goes;
  - `hl_wet_lvl_row`: DRY 95 / 99 / 100 %;
  - `hl_chunks_pair` / `hl_chunks_zoom_pair`: at 100 %, 437 crumbs on the carpet stay dim, while the ones on the tile beside it glow;
  - `hl_hud_clean100`: the HUD at CLEAN 100 % with X lit and a dark carpet.
- Cost after the change:
  - lamps off 6.00-6.26 ms per stepped frame, on 6.77-7.23 (no change);
  - `ReadStatsSync` with the histograms 0.29 ms including its blocking readback. The game reads it asynchronously every 10 frames.

## 7. Open

- The literal Z / X key press is still to be tried with a focused Editor (see 5).
- The toggle and buy sounds were not listened to.
- The floor outside the 9 m sim domain dims but can never glow: there is no data there.
- A fully soiled carpet glows everywhere. That is true to the data, but gives the least guidance at the start of a job.
- The floor keeps its full lamp at CLEAN / DRY 100 %: crumbs, dust and splashes on the tile still glow, because the tile has
  no bar. Pass it the carpet level too if the player reads that as "dirt at 100 %".
- Airborne dust puffs (`DebrisDust`) are not part of the lamp; they stay visible until they settle.
