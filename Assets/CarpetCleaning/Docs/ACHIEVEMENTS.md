# Steam achievements and stats (2026-09-30)

User request (2026-09-30): add Steam achievements and Steam Cloud saves; invent about 10 achievements; generate simple icons.
Cloud is `STEAM.md` 11 (Auto-Cloud, no code). This doc is the achievements side: 10 achievements, 2 stats, their rules,
the code, the icons, the 31-language texts, and the Steamworks steps that only the developer's account can do.

State on 2026-09-30: code, icons, texts and upload files are done and verified in the Editor. In Steamworks the 2 stats,
the 10 achievements (icons, progress stats), the 31-language localization and the Steam Cloud config are in place;
**Publish and the store page's Supported Features are still open** (section 5). Until the schema is published, Steam answers every call with "unknown name": the game logs it once
per name and keeps everything in the save. Players get achievements only after that Publish, and only with a build
that has this code (v0.9.1 on Steam does not).

## 1. The achievements

Order = `AchievementId` = index in `SaveData.achieved` = Steamworks creation order = localization token
`NEW_ACHIEVEMENT_3_<index>` (block 3: the two stats took IDs 1 and 2; checked against Steamworks' download).
**Append only, never reorder.** English source with translator notes:
`SteamStore/achievements/source_en.json`.

| # | API name | Name | Description | Rule (pay slip terms, `PayoutResult`) | Icon emblem |
|---|---|---|---|---|---|
| 0 | `ACH_FIRST_CARPET` | First Fluff | Hand over your first carpet. | `carpetsDelivered >= 1` | tilted rug with a rolled end |
| 1 | `ACH_SPOTLESS` | Spotless | Hand over a dirty carpet 100% clean. | `perfectClean` and V > 0.001 (not "Already spotless") | big + small sparkle |
| 2 | `ACH_BONE_DRY` | Bone Dry | Soak a carpet, then hand it over 100% dry. | `perfectDry` and job peak Wetness >= 0.2 | dryer fan with warm-air arcs |
| 3 | `ACH_COMBO` | Showroom Finish | Hand over a dirty carpet 100% clean and 100% dry. | `combo` and V > 0.001 | gold medal with star and ribbons |
| 4 | `ACH_FILTHY` | Filthy to Fluffy | Hand over a Filthy carpet 100% clean. | `perfectClean` and V >= 0.9 ("Filthy") | pink foam bubbles |
| 5 | `ACH_EXPRESS` | Express Service | Earn the full speed bonus on a heavily soiled carpet. | V >= 0.6 ("Heavy soil" / "Filthy") and `seconds <= par` | stopwatch |
| 6 | `ACH_REGULARS` | Regular Customers | Hand over 25 carpets. | `carpetsDelivered >= 25`; progress stat `STAT_CARPETS` 0..25 | stack of three folded rugs |
| 7 | `ACH_FULL_KIT` | Every Tool in the Van | Use all 10 tools. | all 10 bits of `toolsUsed`; progress stat `STAT_TOOLS_USED` 0..10 | toolbox with two tool heads |
| 8 | `ACH_MAXED_OUT` | Maxed Out | Upgrade one tool's POWER and AREA to MAX. | some tool with `power[i]` and `area[i]` both at `MaxLevel` | amber lightning bolt |
| 9 | `ACH_INSPECTOR` | Inspector | Buy the Moisture highlight and the Dirt highlight. | every `ShopItem` owned | eye |

None is hidden. Stats (both INT, Set By Client, increment only, default 0):

| API name | Display name | Min / Max | Value |
|---|---|---|---|
| `STAT_CARPETS` | Carpets handed over | 0 / 1000000 | `SaveData.carpetsDelivered` |
| `STAT_TOOLS_USED` | Different tools used | 0 / 10 | bits set in `SaveData.toolsUsed` |

Why these thresholds (numbers from `ECONOMY.md` and a sweep of seeds 1..999 with `Tools/dirt_preview.py`):

- **"Dirty" = V > 0.001**, the pay slip's "Already spotless" boundary. About 7 % of rolled carpets are already spotless;
  without the guard, Spotless and Showroom Finish would come free on them. A very light carpet still counts: measured
  below, a V 0.006 dust carpet took minutes of vacuuming and paid $0 yet earned both. That is deliberate (first-tier
  achievements); raise `Achievements.DirtyMinValue` if it ever feels cheap.
- **Bone Dry needs a soak** because a carpet that never got wet reads 100 % dry, and the dry bonus pays for it anyway
  (user rule, `ECONOMY.md`). Peak carpet-mean `Wetness` measured on `Reset(101)`, factory tools: two hose passes through
  the middle 0.052, the hose over half the carpet 0.325, over the whole carpet 0.538. 0.2 = "about a third of the carpet
  or more was hosed". Knob: `SteamAchievements.boneDryPeakWetness` (default `Achievements.DefaultBoneDryPeakWetness`).
- **Filthy**: 9.0 % of rolled carpets (V >= 0.9, 1 in 11). Seeds for tests: 19, 27, 29, 51, 54, 64, 70, 86, 116, 128
  (`Reset(seed, -1)`).
- **Express**: par is 378 s at V 0.6 and 600 s at V 1; the factory tools needed about 1000-1200 s for a V 1 carpet, so
  this is the skill + upgrades achievement. 21.4 % of carpets have V >= 0.6.
- **Maxed Out**: both tracks of one tool; the cheapest pair is a floor tool ($350 + $415 = $765), a carpet tool
  $585 + $695 = $1280 (about 10 average carpets). One track alone would be 3 carpets.
- **Regular Customers**: 25 carpets, several hours of play at 5-20 min per carpet.

## 2. Code

- `Scripts/Steam/Achievements.cs`: `AchievementId`, the `Defs` table (API name, progress stat and max), the stat names, the
  thresholds, and the rules as pure functions: `PayoutUnlocks(PayoutResult, peakWetness, boneDryPeak, list)` and
  `StateUnlocks(SaveData, toolCount, maxLevel, list)`. No Steam, no scene: tests and the debug preview call them directly.
- `Scripts/Steam/SteamAchievements.cs` (order 222, on the `Steam` object, wired by `CarpetSceneBuilder.BuildSteam`):
  - listens to `Economy.Paid` (raised only by a real payout, not by `DebugShowPayout`), `Economy.Upgraded`,
    `Economy.ItemChanged`, `ToolController.Stamped`, `CarpetSimulation.StatsUpdated` / `CarpetReset`,
    `CarpetHandover.StateChanged`;
  - **peak wetness**: max of `sim.Wetness` while the hand-over is `Idle`; back to 0 on `CarpetReset` and on `FlyIn`.
    The async stats readback has no reset generation, so a readback still in flight at a reset would land afterwards
    with the old carpet's Wetness: `ClearPeak` calls `AsyncGPUReadback.WaitAllRequests()` first, then zeroes;
  - **tools**: the first stamp of each tool sets its bit in `toolsUsed`. Floor tools (detergent, mop) count only off the
    carpet or while the carpet is away (they do nothing on it). An id outside `ToolTable` is ignored;
  - `Unlock(id, reason)` sets `SaveData.achieved[i]`, logs `[Achievements] unlocked ACH_X (reason)` once, and pushes to
    Steam while `SteamActive`. The save is written once per frame (`Economy.SaveNow`, coalesced in `LateUpdate`);
  - `Start` retro-grants what an existing save already earns (carpets, levels, items, tools) and syncs with Steam first.
- Hooks added for it: `Economy.Paid` + `Economy.SaveNow()`, `ToolController.Stamped(tool, uv)`,
  `PayoutResult.SpotlessMaxValue / LightMaxValue / MediumMaxValue / HeavyMaxValue` (the soil classes, now shared by
  `SoilName` and the rules).

### 2.1 Steam behaviour

- **SteamActive** = `SteamService.IsReady` and (a player, or `steamInEditor`). **`steamInEditor` is off by default**:
  Editor Play inits Steam with the real App ID, so every scripted test would unlock achievements on the developer's own
  account. With it off, the Editor never calls Steamworks for achievements; the save side works the same.
- SDK 1.64 has no `RequestCurrentStats`: the client loads the user's stats before the game starts, so `Start` reads them
  directly. `SyncWithSteam` (Start, `UserStatsReceived_t`, debug API) works both ways and never revokes:
  - earned in the save, not on Steam -> `SetAchievement` (earned offline, or before the schema was published);
  - unlocked on Steam, not in the save -> earned in the save (another PC without cloud, an older save);
  - a stat below the save's value -> `SetStat` (stats only ever grow; both are increment-only in Steamworks).
- **StoreStats**: right after an unlock (that shows the toast), otherwise at most every `statsStoreInterval` (60 s real)
  for stat-only changes, and once in `OnApplicationQuit` (before `SteamService.OnDestroy` shuts Steam down). Valve asks
  for minutes, not seconds, between stores.
- Progress pop-ups (`IndicateAchievementProgress`): Regular Customers at 5, 10, 15, 20 carpets, Every Tool in the Van at
  5 tools. Steam draws the progress bars on the achievements page from the progress stats by itself.
- A name missing from the **published** schema makes `Get*` / `Set*` return false: logged once as `[Achievements]
  ACH_X is not in the published Steamworks schema`, listed in `schemaMissing`, skipped until the next sync finds it.
- Callbacks (`UserStatsReceived_t`, `UserStatsStored_t`, `UserAchievementStored_t`) are filtered to this app;
  `k_EResultInvalidParam` on a store (Steam rejected a stat) re-reads Steam's values without pushing again.

## 3. Save fields (`SaveStore.cs`, cloud-synced with the rest of the file)

- `achieved[10]`: 1 = earned locally (index = `AchievementId`). Needed because a one-off event (a spotless hand-over) that
  happens while Steam is down would otherwise be lost; the next sync sends it.
- `toolsUsed`: bit per `ToolId` ever used. Steam keeps no partial progress of its own, so the mask lives in the save.
- An older file reads zeros; `Normalize` pads `achieved` and masks `toolsUsed`. The first launch with this code
  retro-grants whatever the old counters already prove.

## 4. Icons and texts

- **Icons**: `python -B Tools/steam_art/make_achievement_icons.py` (about 17 s, byte-identical reruns) ->
  `SteamStore/achievements/icons/<API>.png` + `<API>_locked.png`, 256x256 opaque RGB (Steam takes up to 256 px, shows
  64 px; the unachieved icon is not derived by Steam). Style "rug patch badges": the app icon's madder rug tile with the
  indigo binding and cream stitch line, one flat pillow-shaded emblem with an indigo keyline, light from the top left;
  palette only from the existing art. Locked = the same render in greys, one stop darker, no padlock.
  Review sheet: `SteamStore/_preview/achievements_sheet.png` (256 / 64 / 32 px on Steam's #1b2838).
  `python Tools/steam_art/verify_assets.py` checks all 20 files (ALL OK on 2026-09-30).
  An art-director pass (2026-09-30) fixed: rugs that read as a microchip / books, three landscape silhouettes that
  merged in grey at 32 px, a locked gold medal that read as an earned silver one, uneven emblem weights.
- **Texts in 31 languages**: `SteamStore/achievements/loc/<code>.json` (game language codes, `Tools/loc/loc_tool.py`).
  Made the store-localization way: translator -> independent editor per language family -> cross-language meaning audit
  (5 issues, all fixed: uk/id/ms; bg/cs got the same fix as uk) -> `check`. Notes in `SteamStore/achievements/_review/`.
  Every text reuses the game's own translation of its terms (Spotless, Bone dry, Filthy, Heavy soil, Speed bonus, POWER,
  AREA, MAX, Moisture/Dirt highlight). Only Russian was read by a Russian speaker; no native pass for the others.
- `python Tools/Steam/achievements.py check` (source, 31 languages, digits, lengths, the order against `Achievements.cs`,
  icon sizes) -> `build [--tokens downloaded.vdf]` writes `SteamStore/achievements/upload/achievements_loc_all.vdf`
  (Steamworks "All languages" format) and `upload/single/achievements_loc_<steam language>.vdf` x31;
  `show [code]` prints a table for the Steamworks form.

## 5. Steamworks setup (manual, the developer's account)

Step by step, in Russian, with every value: `SteamStore/achievements/README.md`. Short version:

1. App Admin > Stats & Achievements > **Stats**: create `STAT_CARPETS` and `STAT_TOOLS_USED` (table in section 1).
2. **Achievements**: create the 10 **in the order of section 1**: API name, English name and description, Set By Client,
   not hidden, both icons. Progress stat: `ACH_REGULARS` -> `STAT_CARPETS` 0..25, `ACH_FULL_KIT` -> `STAT_TOOLS_USED` 0..10.
3. **Achievement Localization**: download the English data once, run
   `python Tools/Steam/achievements.py build --tokens <that file>` (maps Steam's tokens to our API names by the English
   name, so a different creation order cannot mix texts up), upload `upload/achievements_loc_all.vdf`.
4. App Admin > **Publish** (together with the Steam Cloud config of `STEAM.md` 11). Until then nothing reaches Steam.
5. Upload a build with this code (the live v0.9.1 has none), then check it: section 6.

Done on 2026-09-30 by the agent through Claude in Chrome, each checked after a page reload: steps 1 and 2 (stats ID 1 / 2,
achievements `a3_0` .. `a3_9`, colour icon = Achieved, grey = Unachieved), the English token download
(`SteamStore/achievements/steamworks_tokens_english.vdf`) and the rebuilt `upload/` (only the token prefix changed, 1 -> 3).
Then, with the user's explicit go-ahead in chat (the auto-mode classifier had refused it without one): the 31 languages
ticked and saved (`SetAppLanguages`, "Language list saved.", reload shows 31/31), `upload/achievements_loc_all.vdf` uploaded
("310 achievements processed", 0 names / descriptions without a token), and the "All Languages" download matched the file
(620 tokens, canonical SHA-256 equal). Still the user's: Publish, then tick Steam Achievements / Steam Cloud / Stats under the
store page's Supported Features (Steam marks them "Must first implement" until the schema is published), then a new build.

## 6. Testing (`CarpetDebugApi`, JSON in the invariant culture)

- `AchievementsJson()`: steamAllowed / steamActive / steamReady / steamInEditor, peakWetness, toolsUsed (+ count),
  carpets, stores, lastStore, pending, per achievement `{api, earned, steam, inSchema, progress}`, Steam's stat values,
  `schemaMissing`.
- `AchievementsPreview(V, clean, dry, seconds, peakWetness)`: the pay-slip rules alone, no side effects.
- `AchievementsForgetLocal()`: clears `achieved` + `toolsUsed` in the save (snapshot `carpet_save_editor.json` first).
- `AchievementsSetSteamInEditor(bool)`, `AchievementsSteamSync()`, `AchievementsSteamClear(api)` (ClearAchievement +
  StoreStats on the real account: only to undo a test unlock). Without code: `steam://open/console` ->
  `achievement_clear 5304930 ACH_X` or `reset_all_stats 5304930`; Steam logs to `%Steam%\logs\stats_log.txt`.
- `SteamJson()` now has the `cloud` block (`STEAM.md` 11.3).

Verified 2026-09-30 in the Editor (Play, Editor save snapshot restored byte for byte afterwards):

- **Retro-grant**: the Editor save (11 carpets, every track at 6, both lamps) earned FIRST_CARPET, MAXED_OUT, INSPECTOR at
  Start; nothing went to Steam (`steamInEditor` off).
- **Rules** (`AchievementsPreview`): V 0 perfect -> nothing; V 0.1 perfect, never wet -> SPOTLESS + COMBO; clean 0.994
  -> nothing; peak 0.3 vs 0.19 -> BONE_DRY or not; V 0.9 vs 0.89 -> FILTHY or not; V 0.6 at 378 s (par) vs 379 s ->
  EXPRESS or not; V 0.59 at 100 s -> nothing; V 1 perfect, soaked, fast -> the five pay-slip achievements at once.
- **Tools**: detergent + mop on the carpet -> not counted; tools 0-7 -> mask 255; detergent / mop on the floor ring
  (u -0.25) -> 511 / 1023 and FULL_KIT.
- **Peak after a reset**: hose, then `ResetWithProfile` in the same call: the stale readback arrived with Wetness 0.32 and
  the peak stayed 0 (and 0 after more frames).
- **End to end**: `ResetWithProfile(7, 0.08, crust 0, mud 0, dust 1, spill 0)` (V 0.006), dry-vac raster at factory
  tuning -> 0.770 (grit left along the edges), cross pass with live tuning -> 0.9982; `HandoverNow` -> carpet #12,
  perfect clean + dry + combo, $0 -> SPOTLESS + COMBO unlocked via `Economy.Paid`, BONE_DRY not (peak 0); the file had
  `achieved [1,1,0,1,0,0,0,1,1,1]`, `toolsUsed 1023`.
- **Steam, schema not published**, `steamInEditor` on: `SyncWithSteam` -> all 10 achievements + 2 stats in
  `schemaMissing`, 0 StoreStats, nothing written to the account. Cloud: account on, app on, no quota yet, 0 files.
- `DISABLESTEAMWORKS` path: compiled in a dry run with the guard forced on. Console: 0 errors.

Not verified (needs the published schema, a build with this code, and the Steam client): the toast, the progress
pop-ups, progress bars, the Steam-side unlock and the cloud upload. Checklist for that run: `AchievementsJson()` shows
`inSchema: true` everywhere, handing over a carpet shows the toast, `stats_log.txt` shows the store, and
`STEAM.md` 11.3 for the cloud.

## 7. Files

| File | What |
|---|---|
| `Scripts/Steam/Achievements.cs` | ids, API names, stats, thresholds, pure rules |
| `Scripts/Steam/SteamAchievements.cs` | events -> save -> Steam, sync, stores, debug JSON, cloud JSON |
| `Scripts/Economy/SaveStore.cs`, `Economy.cs`, `PayoutResult.cs`, `Scripts/ToolController.cs` | hooks (section 2) |
| `Scripts/CarpetDebugApi.cs` | `Achievements*` methods, `SteamJson` cloud block |
| `Editor/CarpetSceneBuilder.cs` | `BuildSteam` adds and wires `SteamAchievements` |
| `SteamStore/achievements/source_en.json`, `loc/*.json`, `_review/` | texts (source, 30 translations, review notes) |
| `SteamStore/achievements/icons/`, `upload/`, `README.md` | what goes to Steamworks and how |
| `Tools/Steam/achievements.py`, `Tools/steam_art/make_achievement_icons.py` | check/build texts, render icons |

## 8. Adding an achievement later

Append it to `AchievementId`, `Achievements.Defs` (and a rule), `source_en.json` (+ `emblem`, notes, terms), all 30
`loc/<code>.json`; `Achievements.Count` / the tool's `COUNT` go up by one; draw its emblem in
`make_achievement_icons.py`; `achievements.py check && build`; create it LAST in Steamworks (its token index follows),
upload icons + the localization file, Publish. Never delete or reorder one: players' saves and Steam hold them by index /
API name.
