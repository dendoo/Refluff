# Steam store art (2026-09-25)

Request (RU): make every Steam capsule screenshot and a logo, put them in a separate folder, follow Steam capsule best
practice. Official title fixed by the user: **"Refluff: Carpet Cleaning ASMR"** (logo may show "Refluff" alone).

Output: `SteamStore/` at the project root, outside Assets so Unity does not import ~1 GB of PNGs.
- `SteamStore/README.md`: which file goes into which Steamworks field.
- `SteamStore/_work/steam_spec.md`: the researched spec, rules and best practice, with sources.

Everything is rebuilt by the scripts in `Tools/steam_art/`.

Updates since:
- **2026-09-26, ACES.** The scene now uses the ACES tonemap. The 9 screenshots were re-shot under it, two were re-staged
  and the order changed (section 3, "ACES re-shoot"). The capsules, library art and key art still use the Neutral plates
  (sections 4-5).
- **2026-09-26, description clips.** Five looping MP4 clips and four header images for "About This Game":
  `STORE_CLIPS.md`.

## 1. Spec in one table

| Asset | Size | Notes |
|---|---|---|
| Header / Library header | 920x430 | logo + art only |
| Small capsule | 462x174 | logo nearly fills it, must read at 120x45 |
| Main capsule | 1232x706 | |
| Vertical capsule | 748x896 | |
| Library capsule | 600x900 | |
| Library hero | 3840x1240 | NO text / logo, subject in the central 860x380 |
| Library logo | fits 1280x720, touches 1280 wide or 720 tall | transparent PNG, trimmed |
| Page background | 1438x810 | calm; Steam fades it |
| App icon | 184x184 JPG | |
| Shortcut icon | 256 / 512 PNG | |
| Event cover / header | 800x450 / 1920x622 | |
| Screenshots | 16:9, >= 1920x1080, >= 5 | real gameplay, no overlays; the first 4 feed the hover tooltip |

Since 2022-09-01 capsules may carry only artwork, the name/logo and the official subtitle. The pre-2024 half sizes are
rejected.

## 2. Logo (`Tools/steam_art/make_logo.py`)

- **How it was chosen.** A 3-concept panel: plush pile / clean & foamy / rug heritage, then 3 judges and 2 art-director
  fix rounds. The winner is "Rug-Cut Refluff":
  - cream wool-pile letters, about 174k procedural strands, on a red madder rug plate with a stitched edge and navy
    thickness;
  - a teal runner carrying "CARPET CLEANING ASMR", with fringe tassels;
  - pink foam bubbles and a sparkle on the last "f".
- **Font.** Fredoka variable (OFL) from `Tools/steam_art/fonts/`. Every font there is OFL or Apache, and each licence
  file sits next to its font. Never use `C:/Windows/Fonts`.
- **Render.** Deterministic, about 5 min; `--quick` makes a 40 % preview.
- **Outputs** (`SteamStore/logo/`):
  - `refluff_logo.png` (3015x887) and `refluff_wordmark.png`;
  - `refluff_wordmark_small.png`, a softer, bolder render for use under about 600 px;
  - `refluff_logo_1280x720.png`;
  - `refluff_mark_1024.png`, a fluffy "R" tile, plus sparkle-free `_64/_48/_32`;
  - `refluff_app_icon_184.jpg` and `refluff_shortcut_icon.ico` (16-256).
- **Exe icon** (2026-09-27): `python Tools/steam_art/export_app_icon.py` cuts the same art into
  `Assets/CarpetCleaning/AppIcon/refluff_icon_<n>.png`, one per Unity Standalone slot (1024 .. 16). `SteamBuild` assigns them
  before every build (`STEAM.md` 5.1). Re-run the script after `make_logo.py` changes the mark.
- **Concept sources** are in `Tools/steam_art/logo_concepts/`.

## 3. Gameplay screenshots

### Capture API

`Scripts/StoreShotApi.cs` is a static debug API, driven with `unity command eval` in Play mode.

- **`Shot(name)`** writes two layers to `SteamStore/_work/shots/`:
  - `<name>_scene.png`: the game camera rendered at 3840x2160;
  - `<name>_hud.png` + `<name>_hud_ui.png` (premultiplied): the HUD panel captured at **2560x1440**.

  The UI Toolkit panel lays out for the Game view. Captured into a 4K target, it draws at 2560 scale in the top-left
  corner (seen in the first probe), so the HUD has to be its own layer.
- **`MarketingHud(true)`** hides the fps label.
- **`RenderScene`** renders the game camera only. **`RenderView(path, w, h, pos, euler, fov, focusM, aperture, focalMm)`**
  renders from a free camera, with an optional temporary Bokeh DoF volume. The game camera's pose and FOV are restored
  before it returns.
- **`CameraJson()`** returns the game camera's pose.
- **Particles.** `DebrisSystem.SubmitDrawsForCapture()` makes sure the particle `RenderMeshIndirect` draws are queued before
  a manual `Camera.Render`. A draw queued in LateUpdate stays queued until the next player-loop frame starts, and every
  camera rendered before then draws it, a capture from a later eval included.
  - Since 2026-09-26 it queues **only a kind that is not queued yet with its current material this frame** (for
    example one whose material was null at LateUpdate). Before, every capture added one more copy: first per capture,
    then (evening of 2026-09-25) once per frame on top of the live LateUpdate draw, so every capture after a real frame
    drew each drop and puff twice (`DEBRIS_SYSTEM.md`, capture trap).
  - Verified: a capture right after a real frame, the same capture from the next eval, and the flush recipe are
    pixel-identical; forcing a second copy changes up to 34 levels (+36 % drop light). Consecutive captures are identical.
- **HDR.** Since the evening of 2026-09-25, `Render` draws into an `ARGBHalf` target and then blits to sRGB 8-bit. URP
  takes a target texture's format for its intermediate buffer, so the old 8-bit target rendered the whole scene in LDR:
  - values clamped at 1 before tonemapping;
  - the sunlit tile came out grey 205 instead of the Game view's (229,221,215);
  - no glints and no bloom.

  *Superseded the same day by the ACES re-shoot (below). Kept as history; slot numbers here are the Neutral-set order.*

  **The 9 store screenshots were re-shot on 2026-09-26** with the fixed looks and HDR captures: the warm tile of the
  Game view, translucent dust puffs, splash-drop streaks and no grey "glass" lanes. 05 (rya), 08 (shop) and 09 (hose)
  were retaken once more after the art-director review (see "Final set").
  - `SteamStore/_work/screens/reshoot_notes/` holds one note per shot plus the eval scripts of the first re-shoot. Each
    note records the recipe, the HUD values and an A/B against the old shot.
  - The scripts of the 2026-09-26 retakes of 05, 08 and 09 are in `Tools/steam_art/screens/recipes/reshoot_0926/`
    ("Staging recipes" below). The 09 note (`03_hose_spill.md`) describes the final take. The 05 and 08 notes
    (`12_rya_half_clean.md`, `11_upgrades_shop.md`) still describe the first re-shoot.

  The key-art plates behind the capsules (section 4) were rendered before these fixes and have not been re-rendered.
  The capsules regrade them anyway (section 5).

### Composing

`Tools/steam_art/compose_screens.py [names]` builds:
- a Lanczos 4K -> 2560 downscale in linear light, with the HUD composited in sRGB exactly as `EconomyHud.DebugCapture`
  does;
- the `_2560.png` master plus a 1920x1080 file in `SteamStore/_work/screens/`.

### Staging recipes

The eval `.cs` files are in `Tools/steam_art/screens/recipes/` (round 2 in `r2/`). `manifest.json` and
`final_order.json` sit next to them. `final_order.json` decides the screenshot order; its copy in `SteamStore/_work/screens/`
must stay identical.

**Historical ACES re-shoot (2026-09-26): `recipes/reshoot_aces_0926/`.** Every script keeps `EditorApplication.isPaused = true`,
so no live frame runs between evals.

| Folder | Slot | What |
|---|---|---|
| `hero/` | 01 | round 3 (final): `run.sh` + `README.txt`; `_round1/`, `_round2/` kept |
| `rya/` | 04 | round 2 (final): `run.sh` + `README.txt`; `_round1/`, `_tried/` kept |
| `flight_pay_shop/` | 03, 07, 08 | copies of `reshoot_0926/reshoot_ho` + `reshoot_d/s11_a..d`, now paused between evals; `e4b_shot.cs` = the pay-slip retake (`README.txt`) |
| `scraper_lamp/final_paused.cs` | 02, 06 | the closing script of the first re-shoot's scraper / lamp chain (`reshoot_notes/scripts_scraper_lamp_spot/`), now leaving the Editor paused |
| `08_spotless_handover/` | 09 | `s_spot2b.cs` = the final take, scripted pointer parked |
| `squeegee/` | (10, rejected) | round 4 + `_round1/.._round3/`. Its README still names `screenshots/10_squeegee_floor.jpg`: that line predates the rejection |
| `probe/` | - | tonemap check (`p0_*`), rug picks, dirt probes for 01 (seeds 242 / 435) and 04 (seeds 367 / 958) |

05 (hose) was re-captured with `reshoot_0926/hose_final/run.sh`, unchanged.

*The rest of this section describes the Neutral set. It is superseded and kept as history; its slot numbers are the old
order.*

The 2026-09-26 re-shoots are in `recipes/reshoot_0926/`. The shots keep their capture names there: 05 = `12_rya_half_clean`,
08 = `11_upgrades_shop`, 09 = `03_hose_spill`.
- `reshoot_a/`, `reshoot_c/`, `reshoot_ho/`: the first re-shoot, the same scripts as
  `SteamStore/_work/screens/reshoot_notes/scripts_{hero_rya_hose, scraper_lamp_spot, flight_payslip_shop}/` plus a few
  helpers (`pre2`, `shot_hero`, `dirtview`, `w_hose2a/b`; `final`, `q1`, `state0`).
- `reshoot_d/`: the retakes after the art-director review.
  - `rya/`: the 05 retake (`n_foam1/2` lay the fresh foam band, `n_dry`, `n_shot` pins the brush and shoots).
  - `s11_a..d.cs`: the 08 retake (rug_european, `Reset(14, 0.7)`, Dry vacuum selected, shop open, shot).
  - `h1..h6.cs` + `hv_A..L.cs`: the hose base staging and 12 shot variants; K was picked.
- `hose_final/`: the final 09. It is variant K with the shot moment moved (`hv_K15.cs`); `run.sh` runs the whole take,
  composes it and measures the HUD bars (`measure_bars.py`). The sequence is in
  `SteamStore/_work/screens/reshoot_notes/03_hose_spill.md`.
- `reshoot_d/runh.sh` and `reshoot_d/rya/runrya.sh` still point `W` at the tuning session's scratchpad. Point it at their
  own folder before re-running them. `hose_final/run.sh` already uses its own folder.

Tips from staging (still valid for every set):
- Use a mid dirt level (0.4-0.9) for a readable rug. Level 1 is brown rubble.
- Keep the Editor paused between evals, and park `ToolController.ScriptedPointer` off the play area during any real
  `Step()` before a `Shot`: paused Steps read the OS mouse (ACES re-shoot, 03 and 09).
- Clear the dust stage with 3-4 dry-vac passes, otherwise the hint stays "Vacuum up the dust".
- `PinToolHead` the tool at the end of the last stroke.
- Before `Shot`, run 10-30 real frames with `EditorApplication.isPaused = true; Step()` so the HUD bars update.

### Final set

**Current live store page (2026-10-25, published revision 8).** The gallery contains the preserved capybara first
frame and nine replacement gameplay screenshots from `SteamStore/screenshots/20261025_reshoot/`.
The owner uploaded the nine files in order; their categories, age flags and nine English accessibility texts were saved,
then the page was published. All ten are Gameplay, suitable for all ages, English. The first file and its Steam SHA1
remain unchanged. Macro views of the five museum rugs alternate with wide views of the four cartoon rugs after the
first capybara wide view. All new frames use the actual 48-layer gameplay pile and simulated dirt/water.

| Slot | File under `SteamStore/screenshots/` | View / rug | Uploaded-file SHA1 |
|---|---|---|---|
| 1 | `20261025_current_paused.png` | wide / capybara, preserved | `444bbebfbfc5b40917c1e35ecb860fdd63a67a89` |
| 2 | `20261025_reshoot/r02_anatolian_extract.jpg` | macro / Anatolian | `09592f125c76c17515490fca81c5b1cffe6d7ac3` |
| 3 | `20261025_reshoot/r03_dragon_brush.jpg` | wide / cartoon dragon | `d26203800f0b21b5ec3b20f0cd22c57ca1b68d8a` |
| 4 | `20261025_reshoot/r04_green_scraper.jpg` | macro / European green | `0a7c249f0a10d44992c24073b7d262be32f775a2` |
| 5 | `20261025_reshoot/r05_fox_foam.jpg` | wide / cartoon fox | `f78f035c736d7de1ebb8598cdc4d1b4b819a6430` |
| 6 | `20261025_reshoot/r06_salor_hose.jpg` | macro / Salor Turkmen | `5434e3291d81ecf25f4bfe291eb65c4a56590f1c` |
| 7 | `20261025_reshoot/r07_cat_squeegee.jpg` | wide / cartoon cat | `947172dc6c8af7be3bf178a2d556f73554c12dfd` |
| 8 | `20261025_reshoot/r08_rya_moisture.jpg` | macro / Finnish rya | `f262010a1ac7da09b921e219f625d12695ef10cc` |
| 9 | `20261025_reshoot/r09_axolotl_finish.jpg` | wide / cartoon axolotl | `d9d05f003f25ce662a0612cf8364476a50fad8f7` |
| 10 | `20261025_reshoot/r10_transylvanian_vacuum.jpg` | macro / Transylvanian | `d6d1ecd6b1be7135069c26428b5e9bc59859b6d1` |

The public carousel's ten thumbnail SHA1s match this local-file order exactly. Slot 2 was opened publicly: the macro
image and its English accessibility text appear. Approved macro trailer V4 remains unchanged before the screenshots.
Publication evidence is in `SteamStore/screenshots/20261025_reshoot/review/`: `publication.json`, `steam_diff.txt`,
`steam_publish_success.jpg` and `steam_live_macro.jpg`. The reviewed publish diff contains only the nine replacement
screenshots, their categories/accessibility texts and asset timestamps; unrelated store settings are unchanged.

**Historical revision 7 (2026-10-25, superseded by revision 8 above).** The gallery contained 10 screenshots.
First is `SteamStore/screenshots/20261025_current_paused.png`: the owner's exact paused Play frame, 2560x1440,
with the capybara rug and the current Clean 63 / Dry 16 / Hose HUD. It is Gameplay, suitable for all ages, English.
The previous nine screenshots keep their existing order and alt text; the shop screenshot retains In-Game Menu.
The new first screenshot's Steam URL contains SHA1 `444bbebfbfc5b40917c1e35ecb860fdd63a67a89`.
Live order: 1 capybara paused frame, 2 Finnish rya half-clean/foam, 3 spotless Transylvanian,
4 pay slip, 5 filthy Anatolian/wet-vac band, 6 green rug/scraper, 7 Turkmen/hose,
8 moisture lamp, 9 hand-over sparkles, 10 upgrades shop.
It was published together with approved macro trailer V4, `SteamStore/trailer/macro_20261025/revision4/refluff_macro_trailer_v4.mp4`
(57.75 seconds, 1080p60), replacing the previous trailer. Steam generated its current thumbnail;
the local V4 cover PNG was not uploaded. The public carousel was checked after publication.
Evidence: `SteamStore/trailer/macro_20261025/revision4/steam_publish_success.jpg`,
`steam_live_trailer.jpg` and `steam_live_first_screenshot.jpg` in the same folder.

**Historical nine-shot order** (ACES re-shoot, uploaded to Steamworks 2026-09-26), in `final_order.json` and `SteamStore/screenshots/`:
01 hero reveal, 02 scraper crust, 03 pay slip, 04 rya half-clean, 05 hose, 06 moisture lamp, 07 hand-over flight,
08 upgrades shop, 09 spotless + Hand over. The table, the reasons and the re-staged shots are in "ACES re-shoot" below.

- The set covers all 5 rugs. 7 shots carry the HUD, and each hint matches the tool in hand.
- 01 and 04 have no HUD: they are crops of the 4K scene layer (`SHOT_VARIANT` in `build_store_art.py`).

#### Neutral set (superseded, kept as history)

The slot numbers below are the old order. That set is backed up in `SteamStore/_work/v2_neutral/`.

Order then: 01 hero reveal, 02 hand-over flight, 03 scraper crust, 04 pay slip, 05 rya half-clean, 06 moisture lamp,
07 spotless + Hand over, 08 upgrades shop, 09 hose.

- The set covers all 5 rugs.
- 7 shots carry the HUD, and each hint matches the tool in hand.
- 01 and 05 have no HUD: they are crops of the 4K scene layer (01 1:1 at 1.5x, 05 at 1.25x). Their hints ("Wet it
  down") did not match the tool, and the game hides the HUD chrome mid-stroke anyway.
  - 05 was cropped at 1.35x, with its top edge kept below grey floor streaks. The 2026-09-26 retake mopped those
    streaks away, so the window now shows the rug's top edge and the foam band's rounded top with a tile margin.
    The brush sits beside the foam on the dirty half, clear of the bottom edge.
- 2026-09-26 re-shoot: the same moments as before, with the fixed looks. The HUD values match the old shots except
  for small shifts in 09 and the rya. The rya (05) was also re-staged from the art director's notes, which moved its
  HUD to Clean 59 % and 3:50:
  - the spill stains were extracted from its clean half;
  - the floor streaks and grout marks were mopped;
  - the brush now stands beside the foam band on the dirty half, fully visible, instead of half buried in it.
- After the art-director review of that set, three shots were retaken on 2026-09-26. Their scripts are in
  `Tools/steam_art/screens/recipes/reshoot_0926/` (`reshoot_d/`, `hose_final/`, see "Staging recipes"), and the notes are
  in `SteamStore/_work/screens/reshoot_notes/`.
  - 05 with a fresh foam band. The brush pin is unchanged.
  - 08 with the Dry vacuum selected, so the hint "Vacuum up the dust and crumbs" matches the tool.
  - 09 with a new hose stroke. The nozzle sits in the dark wet field on the red Turkmen rug (rug_salor_turkmen), with
    the splash drops around it and murky brown water pooled at the lower left.
    - The final take is shot 15 real Steps after `RefreshStatsSync`. The first one was shot after 3, so the HUD bars
      were mid-transition (CLEAN 59 % over a 54 % fill, DRY 81 % over 97.5 %). Now the fills measure 59.0 % and 81.5 %.
    - The HUD reads 0:58 / par 3:00, $1,960, Hose selected, hint "Wet it down".
    - 48 drops are in the air, at the same positions as before (deterministic staging, sim held). They are drawn with
      the new drop look (section 6, item 2): short beads with a sparkle, not the white pins of the first take.
    - About 12 brown crumbs on the tile under the hint pill come from the staging and were kept.

### ACES re-shoot (2026-09-26, evening)

Request (RU): the user switched Tone Mapping to ACES and asked to re-shoot and re-upload the screenshots, then the clips.
On the rya (05 then) and on the first shot, the dirty part had to be crust chunks, "maximally dirty", not just dust. The
order and content follow the steam-izdat store-page guide (https://steam-izdat.github.io/04-marketing/store-page/).

- **ACES in the scene.** The change was only in memory. It is now saved in `Assets/Settings/SampleSceneProfile.asset`
  (Tonemapping mode ACES, preset not overridden), and `CarpetSceneBuilder.SetupVolume` writes ACES (was Neutral).
- **Cost, measured.** Dark dyes and wet pile go near-black. In the hose shot the rug's 5th-percentile luma drops 30 -> 6
  and the soaked area (69,43,42) -> (34,16,15); the tiles read ~222 instead of 226. Asked about it, the user kept ACES as
  is (no shadow lift).

**Order.** steam-izdat: slot 1 striking, 2 gameplay, 3 progression, 4 striking, then alternate. No two neighbours show the
same rug. The first 4 feed Steam's hover tooltip; 50-70 % of visitors look at the first 2-3, only 3 in 100 past 4.

| Slot | Capture name | File | Rug | HUD | Recipe / notes |
|---|---|---|---|---|---|
| 01 | `01_hero_reveal` | `01_hero_reveal.jpg` | Anatolian star | no | **re-staged** (below); `reshoot_aces_0926/hero/` |
| 02 | `04_scraper_crust` | `02_scraper_crust.jpg` | European green | yes | pure ACES re-capture of the first re-shoot's recipe (`reshoot_notes/04_scraper_crust.md`) |
| 03 | `10_payslip` | `03_payslip.jpg` | (Transylvanian, handed over) | yes | re-capture; retaken with `flight_pay_shop/e4b_shot.cs` (scripted pointer parked, 40 Steps) because the plain `Shot` eval caught a floor-detergent head from the OS mouse and a floor-strip pill mid-fade |
| 04 | `12_rya_half_clean` | `04_rya_half_clean.jpg` | Finnish rya | no | **re-staged** (below); `reshoot_aces_0926/rya/` |
| 05 | `03_hose_spill` | `05_hose_spill.jpg` | Salor Turkmen | yes | `reshoot_0926/hose_final/run.sh` unchanged; bars 59.0 % / 81.5 % |
| 06 | `17_lamp_moisture` | `06_lamp_moisture.jpg` | European green | yes | re-capture; `scraper_lamp/final_paused.cs` keeps the Editor paused |
| 07 | `14_handover_flight` | `07_handover_flight.jpg` | Anatolian star | yes | re-capture (sparkles are random) |
| 08 | `11_upgrades_shop` | `08_upgrades_shop.jpg` | European green (dimmed) | yes | re-capture |
| 09 | `08_spotless_handover` | `09_spotless_handover.jpg` | Transylvanian | yes | re-captured twice (cursor-ring fix, below) |

**01 hero reveal, re-staged** (3 art-director rounds):
- `Reset(394, 0.9f)`, crust over grime (dust 0.65, mud 0.91, crust 0.61, spill 0.89). Seed 242 (rounds 1 and 2) was rejected
  after round 2: its crust turned the whole surround into one even mosaic of same-size plates, a "pebble / cork mat".
- A ragged scraped cut through the medallion, washed, extracted and dried; the wet-vac wand pinned at (6, 0.50, 0.28).
- Crop (670, 250) + 2560x1440 of the 4K scene, no HUD (unchanged window).
- `reshoot_aces_0926/hero/run.sh` + `README.txt` (round 3); `_round1/` and `_round2/` are kept.

**04 rya half-clean, re-staged** (2 rounds):
- `Reset(367, 0.9f)`. The left half scraped dry, extracted and dried; one pink foam band on the seam; the right strip is
  99 % crust plates; the brush pinned at (4, 0.765, 0.15).
- Crop (528, 232) + 3072x1728 -> 2560, no HUD.
- `reshoot_aces_0926/rya/run.sh` + `README.txt`.

**09 cursor-ring fix.** The first ACES take showed a faint dryer cursor ring on the tile (bottom left): paused
`EditorApplication.Step()` frames read the OS mouse. `08_spotless_handover/s_spot2b.cs` parks
`ToolController.ScriptedPointer` at uv (-20, -20) (off the play area: no head, no ring) during the Steps and the `Shot`.

**A 10th shot was tried and rejected.** steam-izdat asks for every mechanic on a screenshot, and the squeegee plow was not
shown. Four art-director rounds staged it pushing slurry off the rug onto the tiles. It was not uploaded: under ACES the
wet pile goes near-black, the lanes left film edges (in one round a hard oblique film strip over the medallion), and the
matte floor water reads as a shadow. The last frame is `SteamStore/_work/screens/not_uploaded/10_squeegee_floor_round4.jpg`,
the recipe `reshoot_aces_0926/squeegee/` (round 4 README + `_round3/`, `_round2/`). It was removed from `final_order.json`
again.

**Build.** `build_store_art.py`: `SHOT_MOVE = {}` (`final_order.json` alone decides the order); `SHOT_VARIANT` is keyed by
capture name, so the pay-slip deband follows `10_payslip` to slot 03. Both `final_order.json` copies were updated.
`verify_assets.py` prints ALL OK. The Neutral set (shots, screens, screenshots) is backed up in `SteamStore/_work/v2_neutral/`.

**Steam upload (2026-09-26).**
- The 9 new files went up in slot order through Graphical Assets -> Screenshots, each flagged "suitable for all ages"
  (Valve asks for at least 4: `SteamStore/_work/steam_spec.md` 3).
- The 9 old (Neutral) screenshots were deleted.
- Order check by SHA1: the `ss_<hash>` in a Steam screenshot URL is the SHA1 of the uploaded file. Compare
  `sha1sum SteamStore/screenshots/*.jpg` with the carousel's URLs, in order.
- Alt text was written for all 9 (the "Edit Accessibility Text" dialog).
- Publish and "Mark as ready for review" were not pressed (the user's earlier decision stands).

Screenshot upload quirks:
- The drop zone `#game_image_drop` accepts only drag-and-drop. (The session simulated a `DataTransfer` drop per file,
  then pressed Upload.)
- Each staged card has YES / NO "suitable for all ages" radios. "Upload" submits and reloads the page.
- Staged cards appear in async order (small files first), so stage one file at a time and wait for its card.
- `DeleteScreenshot(...)` deletes at once via AJAX, with no confirm.

The description-media upload has its own quirks: `STORE_CLIPS.md` 8.

### HUD dressing

In 07 (hand-over flight) and 09 (spotless; slots 02 and 07 in the Neutral set) the hand-over timer shows a medium job
(3:07-3:12 against par 3:32). The spotless render starts at level 0, which would otherwise show 0:00, so this was set
through reflection on `CarpetSimulation._initStats` and `Economy._clockAnchor`. The rug pixels are real.

## 4. Key art plates (capsules only)

The plates were rendered under the Neutral tonemap and were NOT re-rendered after the 2026-09-26 switch to ACES. Every
capsule, the library art and the page background still come from them (`build_store_art.py` regrades them anyway, see 5).

Capsules may be marketing renders, so the plates use tricks the screenshots must not. Each plate is:
- `RenderView` from a free camera;
- a temporary **48-shell** pile mesh (gameplay used 24 at capture time; since 2026-10-21 gameplay also uses 48), swapped in with `ShellMeshBuilder.BuildShells`, `DontSave`;
- a temporary raking sun (about 4800 K);
- the brush cap made glossier through a MaterialPropertyBlock;
- separate passes: full / no-head / head-only / empty.

`Tools/steam_art/keyart/finish2.py` + `lensdof.py` then add:
- layered depth of field;
- a contact and cast shadow under the head;
- a grime regrade in carpet UV.

The Unity-side scripts are `Tools/steam_art/keyart/unity/plate.sh` and friends: bash wrappers that write eval files.
Nothing is saved to assets: `find Assets -newer` showed no changed file after the session.

- **Carpet state:** Anatolian rug, `Reset(102, 0.9)`. The right half was really cleaned with dry vac, hose, brush, foam,
  brush, wet vac x2 and dryer x2. A foam band sits on the seam, and the brush is pinned on it.
- **Plates:** `SteamStore/_work/keyart/plates.json`, round 2:
  - wide2 split seam, wide4 foam bank, wide5 macro brush;
  - tallB seam, tallD macro;
  - hero2 split, hero1 foam bank;
  - background soft pile.

## 5. Capsules (`Tools/steam_art/build_store_art.py`)

Three composer sets were made (`_work/capsules/split|macro|director`, scripts in `Tools/steam_art/capsule_variants/`),
then 3 judges, one assembler and 2 art-director rounds. Final picks:

- **macro:** header = library header, main, vertical, library capsule, events, page background.
- **director:** small capsule. `wordmark_small` covers about 91 % of the width over a simplified wide5 crop.
- **split:** library hero. hero2 is re-finished with the brush scaled x0.74 so it sits inside the 860x380 safe area.

One family treatment runs through every asset:
- the grime is regraded to grey-olive (hue ~40, sat ~0.28), so it reads as soil and not as a brown rug;
- the clean side is warmed;
- one soft dark glow sits behind the logo;
- a light vignette fades out at the edges;
- a seeded dither prevents banding;
- holes in the brush cap are refilled from the head pass.

The library logo is a pure crop of the official PNG to 1280x373.

**Verification.** `python Tools/steam_art/verify_assets.py` checks exact sizes, opaque vs transparent, the JPG app icon,
screenshot count and aspect, and file size. It prints ALL OK (25 files). Since 2026-09-26 it also checks the description
clips and headers in `SteamStore/description/` (length, width, no audio, 5 MB each, 15 MB total: `STORE_CLIPS.md` 6).

**Determinism.** Two consecutive full builds are byte-identical over all output files (md5).

## 6. Findings about the game (from staging; items marked fixed have since been addressed)

Slot numbers in items 1-4 are the Neutral-set order (hose = 05, scraper = 02, rya = 04 now). Items 5-8 come from the ACES
re-shoot and the description clips (not fixed in the game yet).

1. The dry vac (1), foam sprayer (3) and dryer (7) have no head models. `ToolModelSet.asset` holds only ids
   0, 2, 4, 5, 6, 8 and 9. A "crumbs into the nozzle" shot is impossible, and the dryer shows only a ring.
2. Splash droplets are 1.5-3 mm (`K_Emit`), which is 1-2 px at 1080p, so the hose never shows water in the air.
   **Fixed 2026-09-25** (screen-space streaks, a steeper splash crown), re-tuned 2026-09-26 for one draw per frame and
   launched from the nozzle skirt: `DEBRIS_SYSTEM.md` "Splash drop look". The hose shot (09) was re-captured on
   2026-09-26.
   - **Drop look v2 (2026-09-26).** The art director read every drop in that 09 as a sewing pin: a grey shaft of even
     width, a white pinhead and ~50 equal streaks on an even ring. v2 changes only `DebrisDrop.shader` and its material
     values (the builder's `Drop*` constants); the spawns, sizes, velocities and draw count are unchanged:
     - a short exposure (`_Shutter` 0.011 s, at least `_MinStreakPx`) instead of a full 1/60 s rod;
     - a teardrop tail that narrows and fades;
     - a clear, water-tinted bead with a dark refraction band;
     - a small sparkle instead of the white pinhead, on about a third of the drops;
     - per-drop variation of length, width and brightness.

     At the 1080p game camera the same 50 drops stay visible, with a median length of 10.4 px instead of 16.9 px.
   - **Drop look v3 (the same day).** v2 made the drops fainter over the light tile (-31 % total tile contrast). v3 keeps
     v2's short teardrop streak. Its lens refracts the real scene behind it, the band is wider and darker, and a faint tail is back.
     Tile visibility is now above v1. Details and numbers: `DEBRIS_SYSTEM.md` "Drop look v2" / "Drop look v3".
   - The final 09 was captured (03:26) after the v3 shader and material were in place (03:13-03:14), so it shows v3
     drops.
3. Damp, pressed pile in the far half renders as translucent grey "glass" lanes at a grazing angle, after wet-vac
   stripes. It disappears once the dryer has dried the pile. Suspects: the laid-pile sheen in `CarpetFibers`, or
   `CarpetWater`'s thin film. **Fixed 2026-09-25** in `CarpetWater`, review fixes 2026-09-26 (`WET_LOOK.md` 5).
   The shots that showed it (01 hero reveal, 05 rya) were re-captured on 2026-09-26.
4. Dust puffs grow into opaque beige blobs over about 1 s (`D_DUST_LIFE` 1.6). Shots are taken 2 frames after a stroke.
   **Fixed 2026-09-25** (optical depth instead of alpha, soft ground fade; the rim pop fixed 2026-09-26):
   `DEBRIS_SYSTEM.md` "Dust puff look". The scraper shot (03) was re-captured on 2026-09-26.
5. **Hand-over corner clips through the tiles.** On fly-out a carpet corner dips ~78 mm under the tiles (5.6 mm on
   landing): the tilt runs ahead of the lift in `CarpetHandover.ApplyPose`. Clip 4 hides it with a runtime hook; the fix
   for the game is written out in `STORE_CLIPS.md` 9.
6. **Paused `EditorApplication.Step()` frames read the OS mouse**, so a stray tool head or cursor ring appears in captures
   unless `ToolController.ScriptedPointer` parks it (the 09 and 03 retakes). Editor-only.
7. **Floor water renders matte** (no gloss): a puddle reads as a cast shadow at store size. One reason the squeegee shot
   was rejected.
8. **Crust plates** have one size, one light greige tone and a visible shell grid on top, so at a distance under ACES they
   read as a "pebble / cork mat" (why seed 242 lost the hero shot). Reviewers suggest size / height / tone variation later.

## 7. Before release

- Updated 2026-10-25: the nine replacement screenshots were published as store revision 8, preserving the first capybara
  frame and macro trailer V4. The public ten-image order and a macro image/accessibility text were verified (section 3,
  "Final set"). The first screenshot and V4 were initially published together in revision 7; the dated notes below preserve history.
- Done 2026-09-26: the rya shot (05 then) was re-captured. Its spill stains are extracted, the grey floor streaks are
  mopped, and the brush stands clear of the foam (section 3, "Final set").
- Done 2026-09-26: the ACES re-shoot is uploaded to Steamworks in the new order, with age flags and alt text (section 3,
  "ACES re-shoot"); the description clips and headers are uploaded too (`STORE_CLIPS.md`). Publish / "Mark as ready for review"
  not pressed (the user's decision).
- Open: the capsules, library art and page background still come from the Neutral key-art plates (section 4). Re-render
  them under ACES if their look should match the screenshots.
- Open: the hand-over corner fix in `CarpetHandover.ApplyPose` (section 6, item 5); floor water gloss (item 7).
- The brush model (on every capsule) comes from Tripo. Check that the Tripo plan allows commercial use.
- The store page needs the CC BY audio credits (`AUDIO_CREDITS.md`). The CC BY-NC dry-vac machine must be replaced for
  a paid release (`STEAM.md` 10).


### 2026-10-25 published replacement set — alternating macro / wide, revision 8

`SteamStore/screenshots/20261025_reshoot/` holds nine newly shot 2560x1440 q95 4:4:4 JPEGs, with the published full gallery shown in `review/contact_sheet.jpg`. Existing slot 1 was preserved (`20261025_current_paused.png`, SHA256 `32f1c0879314d045e56a5d981c5af620a6b8dd2a6c984d5e4fb0ff146940a85a`). Slots 2-10 alternate macro/wide: Anatolian extraction, dragon brush, European scraper, fox foam, Salor hose, cat squeegee, Finnish rya drying, axolotl finish, Transylvanian dry vacuum. This set replaces the former shop/pay-slip/flight selection too; all new categories are Gameplay.

Owner specifically requested macro detail alternating with general views, overriding the earlier fixed-camera-only screenshot convention for this set. Macro views use temporary RenderView poses; actual gameplay mesh remains 48 shells, all lighting/post remains unchanged. Curved tool gestures and real simulated muddy runoff/debris are retained; dirty floor persists between final jobs as after an ordinary handover. No screenshot dirt painting or additional fibers. First file verified byte-identical; every new export verified 16:9, distinct, valid and finite stats. Recipe/order/export docs: `Tools/steam_art/screens/recipes/reshoot_20261025/README.md`. Hashes/source layers in output `manifest.json`; simulation QA and save-restoration evidence in `SteamStore/_work/screens/reshoot_20261025/`.

**Status: uploaded, saved and published 2026-10-25 as store revision 8.** The owner manually uploaded the nine root `r*.jpg` files in order. All nine are Gameplay / English / suitable for all ages, with English accessibility texts saved. The public carousel matches all ten local SHA1s in the intended order; opening slot 2 confirmed its macro image and alt text. Existing first asset and macro trailer V4 remain unchanged. See `UPLOAD_ORDER.txt` and `review/publication.json`; section 3 "Final set" is the authoritative live order.
