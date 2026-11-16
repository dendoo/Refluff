# Session log (newest first). Append a dated entry per working session: what changed, what was verified (numbers/screenshots), what is open.

## 2026-10-17 — TikTok cut 2: user's mouse performances and centered captions

- Re-shot weak and maximum scraper scenes with the user's actual mouse path. Selected successful recordings:
  weak680samples /7.003366s, strong755samples /8.031677s; both ended by duration, fixture:false. Strong keeps
  the entire8s window and permits release/repress between passes. Original JSON SHA256 hashes are in human_cut/timing.json.
  Paths/buttons retain their timing and speed1; replay only interpolates time to60Hz, without smoothing or extra cleaning strokes.
- Replaced JsonUtility serialization of the ephemeral recorder's sample list with Newtonsoft plus disk/roundtrip checks,
  first-frame and0.25s checkpoints, per-kind AppDomain backup and archived previous takes. Added ARMED/REC/SAVED/error badge,
  focus+button-release gate and first-held-button trigger. The first broken capture had no serialized path; it was not used.
  The earlier4.9816s strong take ended on release; after the user's redo request it was replaced by the full8.0317s take.
- Captured new weak/shop/power/reveal436/360/501/426frames, reused arrival300;2023sourceframes, no take errors.
  New EDL trims just one idle weak frame:2022frames,33.7s,1080x1920/60fps. Original26.8s master preserved.
  Source VO speed/pitch unchanged; shifted shop+1.75s and reaction/endcard+6.9s, last word32.19s;
  aligned voice is entirely silent18.38–26.64 for strong-tool ASMR.
- All14titles now have x0.5 instead of0.45; logo/CTA alsox0.5. Weak speech y0.65/0.712 clears the user's upper-left
  work area. Independent geometry/read-time/safe-area checks and visual review of14new composed frames passed.
  Contact sheet: SteamStore/_work/tiktok_scraper/retake/qa/human_composed_contact.jpg.
- Replay consumes one parked unpressed Step after shop close to release ToolController's modal gate. The old sample format
  lacked pre-press hover heading: Snap initially seeded from a short rightward jitter, leaving70cells of crust at60Hz.
  A capture-only initial90deg axis (first main pass) preserves normal heading filtering thereafter and removes the residue.
  Final GPU stats exactly match the live performance: crust0,grit0,mud0.176529,stain0.039978; power/area still20/5.
  Wider-blade and120Hz diagnostic probes were discarded; no production simulation/tool tuning changes were made.
- Camera,48-shell mesh, light, runtime rug texture/material and portrait shop restored; recorder/hook removed, temporary
  Editor/_CaptureOnly removed, Play stopped. Compute messages0, console errors0; updated CaptureRig dry-run compiles cleanly.
  Editor save restored byte-identically: SHA256757633893c0de549cc8c3446fd1bc548c2d1cfd7451db08b4d555850418ec124.
  Capture report: SteamStore/_work/tiktok_scraper/retake/capture_report_human.json.
- Cut2 EDL/build/verification/commands: Tools/steam_art/tiktok/scraper_upgrades/retake_human/.
  New master/audio/transcripts: SteamStore/tiktok/scraper_upgrades/human_cut/. See Docs/TIKTOK_SCRAPER_UPGRADES.md.
- Export completed:33.7s,2022frames,1080x1920/60fps,H.264/yuv420p/BT.709,AAC,64.46MB; render warnings0.
  Render mix measured -14.1LUFS/-2.4dBTP. User requested immediate delivery without further checks;
  the final MP4 checker was stopped and no final-checker PASS result is claimed.

## 2026-10-17 — TikTok: scraper upgrades, user's voiceover (Docs/TIKTOK_SCRAPER_UPGRADES.md)

- Shot and edited the agreed story: empty tile -> rug completely covered in dried crust -> four feeble scraper moves ->
  real POWER/AREA purchases to MAX -> three continuous fast passes -> reaction, logo and Wishlist on Steam.
  User chose SpongeBob and a still-dirty, recognizable pile after crust/crumb removal. The supplied 19.28 s English
  MP3 is aligned without changing speed/pitch; the final video is **26.8 s**, with 3 s of scraper ASMR without speech.
- Master + cover + source audio/transcript: `SteamStore/tiktok/scraper_upgrades/`; reproducible recipes and EDL:
  `Tools/steam_art/tiktok/scraper_upgrades/`. Existing TikTok/Steam trailer files preserved.
- Ephemeral CaptureRig via run_script (outside Assets), StoreGifApi real Steps + audio logs, 2160x3840 @60fps,
  48 shells, warm raking sun. Takes `scu_arrival/weak/shop/power/reveal`: 300/340/360/202/426 frames, no capture errors.
  Final EDL uses1608 frames. Shot continuity verified on14 composed frames; weak captions lifted to avoid the tool.
- Capture-only StageCrust stages full crust and moderate pile mud. Production scraper does all crust removal and
  emits actual GPU chunks/dust; a separate1-UAV cleanup removes generated grit/mud and settled carpet crumbs, keeping
  airborne/floor debris. Weak power/area0.18/1; strong20/5. SnapHeadingOnNextMove before each fast pass prevents missed
  turn arcs. Final stats: crust0, grit0, mud0.176529, stain0.039978. Compute messages0 after actual dispatch; console errors0.
- Real shop widgets reflowed in memory for portrait; ShopBuy preserves pips, sparkle and sounds. Scrim viewport clipping
  corrected in the captured UI layer with raw panels retained. Original SpongeBob restored from Recycle Bin only to
  the film's source folder and loaded into a temporary runtime texture; never added to the game's texture bag.
- Final check_trailer: **18/18 PASS**, 1080x1920, H.264 High yuv420p BT.709,60fps CFR,26.800s,1608frames,
  video13.42Mbit/s,AAC LC48k stereo200kbit/s,45.7MB, no black frames, **-14.1LUFS/-2.3dBTP**; all14captions +logo/CTA safe.
  Audio audit confirms all spoken phrases and zero VO16.70–19.70; limiter max2.86dB, no render warnings.
- Restored camera/light/material/mesh/UI, removed Editor/_CaptureOnly, stopped Play and restored the editor save byte
  for byte (SHA256757633893c0de549cc8c3446fd1bc548c2d1cfd7451db08b4d555850418ec124).
  Production C#/compute, scene assets and normal upgrade balance were not changed. Reports/stills/checks:
  `SteamStore/_work/tiktok_scraper/`. No publishing/upload performed.

## 2026-10-04 - TikTok 9:16 "Every fiber is simulated" (Docs/TIKTOK.md)

- **Request (RU).** A ~24 s vertical TikTok about the visible, simulated pile with dirt and water; burned captions,
  game foley, an English voiceover text for TTS; VO mix once the user sends a WAV.
- **Shot** 7 real-Step takes (`StoreGifApi`, 2160x3840 @ 60 fps, audio log) on the Anatolian star,
  `ResetWithProfile(394, 0.75, crust 0, mud 0.45, dust 0.85)` (the seed's mud 0.91 + crust hid the fibres in stills).
  Key-art look in the take only: 48 shells, raking sun 40/30 deg 4.8 4800 K soft shadows, ambient 0.62/0.65/0.72; DoF volume
  present but focus 0 in every key (off). Free camera = Catmull-Rom keys driven from
  `RenderPipelineManager.beginContextRendering`: `Application.onBeforeRender` does NOT fire inside `EditorApplication.Step`
  (first takes came out with a static camera). Recipes + run order: `Tools/steam_art/tiktok/recipes/README.md`.
- **Edit.** `edl_tiktok.json` (size [1080, 1920]); `make_trailer.py` takes ASPECT from the EDL size and has a top-level
  `voice` slot with RMS sidechain ducking; `titles.py` scales by the short side, end card `cta_scale`. Captions scale 1.15 at
  (0.45, 0.66); `caption_boxes.py` = ALL OK inside TikTok's safe zone x 0.06-0.84, y 0.10-0.78.
  Hose / brush foley -4 dB (limiter GR 6.7 -> 4.4 dB, warning gone).
- **Verified.** `check_trailer.py ... --expect-seconds 25.7 --safe 0.06,0.10,0.84,0.78`: 18/18 PASS (1080x1920, 60 fps CFR,
  1542 frames, 20.0 Mbit/s, no black frames, -14.2 LUFS, -2.7 dBTP, no CC BY-NC audio). `check_trailer.py` learnt the EDL size,
  `--safe`, empty end-card lines and `_work/<folder>/` render reports; on the 16:9 trailer it still passes everything but its old
  default `--expect-seconds 62` vs the 1:00 master. 600 px muted review: `SteamStore/_work/tiktok/checks/first2_600.jpg`
  (fibres read in the first 2 s) and `shots_600.jpg`.
- **Delivered.** `SteamStore/tiktok/refluff_tiktok_fibers.mp4` + cover, `voiceover_en.txt` (7 lines, 44 words, windows per cut).
  `carpet_save_editor.json` restored byte for byte from the backup after Play was stopped (SHA-256 equal).
- **Cut 1.1 (same evening, user review).** (1) The rug flipped 180 deg twice at the end of #07: the near-equal hold keys
  made Catmull-Rom overshoot over the look-at point, the camera looked straight down and `LookRotation` with world up
  flipped yaw. `cam.inc`: equal keys = hold, up slerps to +z when |fwd.y| > 0.9. (2) #07 retaken with crust 0.3 (scattered
  dried-mud slabs on the dirty half; 0.7 paved the whole rug), scraper over the near half first (`retake_t7.sh`); p6i
  split in two (5 s eval timeout with the crumbs). (3) End card `cx 0.45 -> 0.5` (the CTA is always frame-centred, the logo
  looked off-centre). Re-render: 18/18 PASS, caption_boxes ALL OK; flip gone (frame strip `checks/t7_retake.jpg`); save
  restored (SHA-256 equal).
- **VO mix.** The user's `SteamStore/tiktok/voice.mp3` (20.9 s TTS, opens "Look, I made a game where...") cut at its pauses and
  laid per shot by `Tools/steam_art/tiktok/align_voice.py` -> `voice/vo_aligned.wav`; own EDL `edl_tiktok_vo.json` (shots 01/02
  from take 0, later cuts +0.6 s, end card 6.0 s, 27.1 s). Duck -8 dB left the voice 12-17 dB over the machines: -4 dB.
  `refluff_tiktok_fibers_vo.mp4`: 18/18 PASS, -14.0 LUFS, -2.3 dBTP. #06's re-fluff reads as a swirl + brighter band; the
  moisture lamp is the fallback if viewers miss it.

## 2026-09-30 - Steamworks: stats, 10 achievements, localization, Steam Cloud entered through Claude in Chrome (Publish left to the user)

- **Follow-up (same day, RU).** "Can you tick the languages and upload the file? Don't press Publish yet, I allow it."
  Ticked the 30 languages; the Save button click did not persist (reload showed English only), calling the page's own
  `SetAppLanguages(5304930)` did ("Language list saved.", reload 31/31). Uploaded `upload/achievements_loc_all.vdf`
  (44441 bytes): `{"success":true}`, 310 achievements processed, 310 names / 310 descriptions localized, 0 without a token.
  Verified: "All Languages" download = 31 languages x 20 tokens, canonical (language, token, text) SHA-256 equal to the local
  file (Steam writes `korean` where the file says `koreana`). Russian names read back correctly. Publish NOT pressed.

- **Request (RU).** "Set up the Steam achievements and cloud saves on the game's Steam page; use the browser; the data is in
  `SteamStore/achievements/README.md`."
- **Stats** (`/apps/stats/5304930`): `STAT_CARPETS` INT, Client, increment only, 0..1000000, default 0, not aggregated
  (README: optional), "Carpets handed over" = ID 1; `STAT_TOOLS_USED` INT, Client, increment only, 0..10, default 0,
  "Different tools used" = ID 2. Checked after a reload.
- **Achievements** (`/apps/achievements/5304930`): the 10 in source order, Set By Client, not hidden, English name + desc,
  progress `ACH_REGULARS` -> `STAT_CARPETS` 0-25, `ACH_FULL_KIT` -> `STAT_TOOLS_USED` 0-10. Steam gave them stat block 3
  (rows `a3_0` .. `a3_9`). Reload: "Number of achievements: 10", every icon 256x256, a screenshot shows colour = Achieved and
  grey = Unachieved in every row.
- **Tokens.** Read the English "Download Localization Data" (fetch in the page, no file download): `NEW_ACHIEVEMENT_3_0` ..
  `3_9`, English texts equal to the source. Saved as `SteamStore/achievements/steamworks_tokens_english.vdf`;
  `achievements.py build --tokens` -> 0 errors, 0 warnings; `DEFAULT_TOKEN` in the tool switched to block 3, so a plain `build`
  gives the same bytes; the new `upload/achievements_loc_all.vdf` equals the old one with `_1_` -> `_3_` (620 tokens = 31 x 20).
- **Steam Cloud** (`/apps/cloud/5304930`): quota 1048576 / 10 files; Auto-Cloud row `WinAppDataLocalLow` / `VahovGames/Refluff` /
  `carpet_save.json` / [All OSes] / not recursive (preview `%USERPROFILE%/AppData/LocalLow/VahovGames/Refluff`, Cross-Platform
  "No"). Steam ticked "Enable cloud support for developers only" by itself on the first quota save: unticked and saved. Reload
  confirms all values.
- **Not done (the user's).** Achievement Localization: ticking the 30 languages was refused by the auto-mode safety classifier
  ("Account & Standing-Rule Changes"), so that page, the upload of `upload/achievements_loc_all.vdf` and Publish were left
  alone. Store page Supported Features (Steam Achievements / Steam Cloud / Stats) are unticked and marked "Must first implement":
  tick after Publish. Then a new build (v0.9.1 has no achievement code).
- **Stale comment.** `Scripts/Steam/Achievements.cs` line 7 still says `NEW_ACHIEVEMENT_1_<index>`; left alone to avoid a
  script recompile in a possibly running Play session. Change it to `_3_` next time C# is touched.
- **Automation notes.** The Steamworks achievement page: `PerformNewAchievement(appid)` creates a row server-side, the edit row
  has `ach<stat>_<bit>_*` ids, name / desc are `input[name=english]` inside the `_displayname` / `_description` forms, icons can
  only be uploaded after the first Save (then Edit again; each icon form posts through `AIM.submit` into an iframe and fills
  `#achievement_upload_response`). All 18 icons went into one `<input type=file multiple>` in a single upload call, then per form
  via `DataTransfer`. A tab whose window is not in front runs timers about once a second or slower: one achievement took about
  2 minutes that way, so long loops run fire-and-forget in the page and are polled.

## 2026-09-30 - Steam achievements (10) + stats (2) + Steam Cloud; icons and 31-language texts; Steamworks NOT configured yet

- **Request (RU).** "Add Steam achievements and Steam cloud saves; invent about 10 achievements; generate simple icons."
  Docs: `ACHIEVEMENTS.md` (new), `STEAM.md` 11 (cloud), `ECONOMY.md` 3 (save fields), setup guide for the user
  `SteamStore/achievements/README.md` (RU).
- **How.** Research workflow (Steamworks docs, code hooks, art style: 3 agents), then one implementation workflow with three
  lanes: C# (implement -> 2 independent reviews (Steam / game integration) -> fix: 4 findings, all applied), icons
  (generator -> art-director critique, 12 issues -> fix), texts (tool + 5 translator/editor language groups -> 2 meaning-audit
  agents, 5 issues -> fix; bg/cs got the same fix by hand). Recompile, Build Scene and the Play tests by the main agent.
- **Achievements** (order = Steamworks creation order = token index): First Fluff, Spotless, Bone Dry, Showroom Finish,
  Filthy to Fluffy, Express Service, Regular Customers (25 carpets, `STAT_CARPETS`), Every Tool in the Van (10 tools,
  `STAT_TOOLS_USED`), Maxed Out, Inspector. Rules reuse the pay slip's classes (V > 0.001 dirty, >= 0.6 heavy, >= 0.9 Filthy,
  full speed = seconds <= par); Bone Dry needs a peak carpet Wetness >= 0.2 during the job (a never-wet carpet reads 100 % dry).
- **Code.** `Scripts/Steam/Achievements.cs` (pure rules), `SteamAchievements.cs` (order 222 on `Steam`; save = local truth
  `achieved[10]` + `toolsUsed`; two-way sync; StoreStats at unlocks, <= 1/min for stats, at quit; `steamInEditor` OFF by default
  so Editor tests never touch the developer's Steam account). Hooks: `Economy.Paid` (only a real payout) + `SaveNow`,
  `ToolController.Stamped`, `PayoutResult` soil constants. `CarpetDebugApi.Achievements*`, `SteamJson.cloud`.
- **Cloud.** Steam Auto-Cloud on `WinAppDataLocalLow/VahovGames/Refluff/carpet_save.json` (exact name: the folder also holds
  logs, the Editor save, `.tmp`/`.bak`); no root overrides (Windows-only; Proton syncs the same root). PlayerPrefs stay local.
- **Verified (Editor, Play, save snapshot restored byte for byte afterwards, 0 console errors).**
  - Recompile clean; Build Scene wired `SteamAchievements` (sim/handover/tools/economy).
  - Retro-grant at Start from the Editor save (11 carpets, all tracks 6, both lamps): FIRST_CARPET, MAXED_OUT, INSPECTOR.
  - `AchievementsPreview` rule matrix: every boundary on the right side (V 0 / 0.1, clean 0.994, peak 0.19 / 0.3, V 0.89 / 0.9,
    378 / 379 s at V 0.6, V 0.59).
  - Tools: detergent + mop on the carpet not counted; 0-7 -> mask 255; floor tools on the floor ring -> 1023 -> FULL_KIT.
  - Peak Wetness on `Reset(101)`, factory hose: 2 middle passes 0.052, half the carpet 0.325, whole carpet 0.538 (threshold 0.2).
    Stale-readback guard: reset right after hosing -> the late readback showed 0.32, the peak stayed 0.
  - End to end: `ResetWithProfile(7, 0.08, 0, 0, 1, 0)` (V 0.006) dry-vacuumed to 0.9982 (factory raster 0.770, then a live-
    tuning cross pass), `HandoverNow` -> SPOTLESS + COMBO through `Economy.Paid`, not BONE_DRY (peak 0); saved.
  - Steam in the Editor with `steamInEditor` on: all 10 + 2 names reported as not in the published schema, 0 stores, nothing
    written. Cloud: account on, app on, no quota yet.
  - Icons: 20 x 256x256 RGB, `verify_assets.py` ALL OK, a rerun is byte-identical (17 s). Texts: `achievements.py check`
    31/31 languages, 0 errors; `build --tokens` remaps a shuffled Steamworks download by English name (tested).
- **Open (the user's side).** Steamworks: 2 stats, 10 achievements in order with icons, localization upload, Steam Cloud
  quota + Auto-Cloud row, Publish (`SteamStore/achievements/README.md`). Then a build with this code (v0.9.2) and the real-Steam
  checks: toast, progress pop-ups, `stats_log.txt`, cloud upload (`ACHIEVEMENTS.md` 6, `STEAM.md` 11.3). No native-speaker pass
  on the texts except Russian.

## 2026-09-28 - Steam store page localized into the 29 recommended languages (text + header images), saved in Steamworks

- **Request (RU).** "Add the recommended localizations to the Steam page; the translation must be adequate."
  Steamworks App Admin > RECOMMENDED ITEMS lists "<language> store localization" for 29 languages (the game's 31 minus
  English and Arabic, which the Steam store lacks). Doc: `STORE_LOCALIZATION.md`.
- **Text.** `SteamStore/localization/source_en.json` (32 fields with where / note / max) + `Tools/steam_art/store_loc.py`
  (glossaries from the game's `Resources/Loc` tables, `check`, `build`, `show`); the tool assembles the BBCode, so a
  translation holds plain text only. 7 translator agents by family, 7 independent reviewer agents (fixes logged in
  `_review/<code>.md`), then a meaning audit across all 29 (3 agents, `_brief/AUDIT.md`). The audit layer caught real
  errors: the speed-bonus bullet as "the bonus only grows" (ja ko zh-Hans zh-Hant th), "the brush rubs the grime in"
  (fr cs), a per-fibre over-claim (ja ko), uk "a strict order". Russian reviewed line by line by the lead agent (3 fixes).
  `store_loc.py check`: 29 languages, 0 errors.
- **Header images.** `make_headers.py` renders localized strips via HarfBuzz + FreeType (`uharfbuzz`, `freetype-py`
  installed; Pillow here has no raqm, Thai tone marks would collide). Fonts: Fredoka where it covers, Nunito (Latin Ext,
  Vietnamese, Cyrillic), Inter (Greek), Noto JP/SC/TC/KR/Thai from `Tools/loc/fonts_src`. One size + baseline per language
  (`lang_fit`), glyph-by-glyph raster (a full line hit FreeType "raster overflow"). 116 PNGs, English 4 byte-identical
  (sha1 checked).
- **Steamworks.** 116 PNGs uploaded through the description uploader (helper file input -> React onDrop, SHA-256 checked
  in page); Steam groups `<name>_<language>.png` with `<name>` ("LOCALIZED IMAGE GROUP ... Group of 30"). 30 JSON files
  through Localization > Upload Localization; the import ignores `[rec]` sysreqs, so 145 rec values went into the form's
  hidden inputs + Save. Verified: all uploaded fields of 30 languages match by SHA-1, 150 rec fields filled; store
  pages de / ja / th show translated text, alt texts and localized headers.
- **Fixed with the owner's approval (AskUserQuestion):** Supported Languages had Interface ticked for 67 languages
  (36 the game lacks) -> exactly 31; the min "Additional notes" duplicate of Steam's own 64-bit line -> empty everywhere.
  English feature line "English interface" -> "interface in 31 languages".
- **State.** Saved, not published (the app was never public; the page and build are in Valve's review since 09-27).
- **Open.** No human native speaker has read the translations; the AI disclosure text (Content Survey), capsules,
  screenshots and trailer stay English.

## 2026-09-28 - trailer: #07 VACUUM and #08 SOAK retaken with the pile lay, master rebuilt (cut 3.1; NOT uploaded)

- **Request (RU).** Retake only the trailer's #07 and #08 (the storyboard frames `07_vacuum.jpg` / `08_soak.jpg` named
  them): "they look prettier in the game now". Everything else stays as it is. The one game change since the shoot is the
  pile lay of the dry vac and the hose water (2026-09-27, `FIBER_SET.md` 4-7).
- **Shot.** Same Steps from the same state (`Tools/steam_art/trailer/recipes/retake_0928/`, README block "Retake 2026-09-28"):
  - kit + chain reset (star 394 @0.9: dust .648 mud .915 crust .612 spill .889);
  - #06 replayed uncaptured (211 Steps, as the original);
  - clip 1's scraper passes, `p06b_settle`, `p07a_dv` (crust 0.387, grit 0.130);
  - the #07 take `tr07b_vacuum` (226 frames);
  - `p07b_dv` + `c1_dv` (grit 0.098);
  - the #08 take `tr08b_soak` (186 frames).

  Both takes: 60 fps, audio log, 0 errors; the audio event mix is the same as the 09-27 takes (#07: 133 scheduled
  rattles, 220 plays).
- **Verified.**
  - The crust slabs match the 09-27 takes to the pixel (MAD 0.00); only the pile differs.
  - #07 (band MAD 7-11): the pile leans into the ring and a combed trail with a soft sheen stays behind it.
  - #08 (window MAD 1-2): the vac's lanes at the start and a radial starburst in each jet crater.
- **#08 hose variants** tried from the pre-#08 snapshot: full-pressure spots, full / half sweeps, 5 taps,
  spot-drag-spot. All are the same flat murky sheet: `CarpetWater` murk counts the pile's mud (~0.65) and saturates at
  once. Full pressure floods the slabs. The approved three half-pressure spots stay.
- **Edit.** `edl.json`: only #07 / #08 `take` + notes changed (in-points, cameras, titles, audio kept).
  `make_trailer.py` (605 s):
  - 60.0 s, 3600 frames, 17.6 Mbit/s, -14.1 LUFS, -2.7 dBTP; `check_trailer.py --expect-seconds 60`: 18/18 PASS.
  - The renderer's frames outside #07/#08 are pixel-identical to the 09-27 cut-3 stills (9 times, #03-#21); the MP4
    differs there only by the x264 re-encode.
  - The cover is byte-identical (SHA1 `cfdbc78d…`). New master SHA-256 `6124e0434159be87…`.
  - Foley outside #07/#08 is bit-identical. One crumb click in the new #07 has a 1.4 dB inter-sample overshoot, so the
    limiter took 6 rounds: ceiling -0.5 dB, up to 6.2 dB of gain reduction, which trips the "> 6 dB" warning. The rest of
    the trailer's loudness is unchanged (±0.001 dB).
  - #07 at -0.5 dB did not help (the waveform, not the level), so it was left at 0 dB.
- **Housekeeping.**
  - The 09-27 master, stems, render report and EDL are in `SteamStore/_work/trailer/master_0927/`; the old takes are kept.
  - The economy save was restored byte for byte (md5 `f804d132…`); Play stopped, `fixedDeltaTime` 0.02, console 0 errors.
- **Open (the user's).** Steam still shows the 09-27 master: to publish the retake, press Replace in the trailer card
  (`TRAILER.md` 13) and upload the video only. The storyboard (`SteamStore/trailer/storyboard/`) is the pre-shoot plan
  and was left as it is.

## 2026-09-27 (later) - v0.9.1: exe icon + full-screen start fixed, rebuilt, uploaded with steamcmd (BuildID 25562494, not live)

- **Request (RU).**
  1. Upload `Builds/Steam/upload/Refluff_0.9.1.zip` to Steam. Upload only: the user sets it live and publishes it.
  2. Then two problems with the build: it does not open full screen, and the taskbar shows Unity's default icon. Fix
     both, rebuild, upload through the console; the user logs in.
- **First pass.** The ZIP was intact and byte-identical to `Builds/Steam/content/windows` (210/210, CRC32 + size).
  - The session (VS Code) had no browser tools, so Web Uploads could not be driven.
  - steamcmd was downloaded and self-updated into `Tools/Steam/steamcmd/`; the upload was handed to the user.
  - The user then web-uploaded that ZIP and ran it from the Steam install. Its `Player.log` shows the 0.9.1 line
    "game language", and it is the baseline manifest 983883189799426378 below.
- **Full screen: not a build fault.**
  - Player Settings were already right: `fullscreenMode` 1 (borderless window) and native resolution. The build profile
    has no Player overrides.
  - `HKCU\Software\VahovGames\Refluff` held `Screenmanager Fullscreen mode = 3` (window), 1600x900, `Use Native` 0. These
    were left by the windowed smoke tests of 0.9.0 / 0.9.1; Unity restores the last mode on every start, the Steam install
    included.
  - The 15 `Screenmanager *` values were deleted (`reg export` backup in the session scratchpad).
  - `SteamBuild.EnsurePlayerSettings` now also sets FullScreenWindow + native resolution before every build, as a guard.
  - The smoke-test rule is in `STEAM.md` 5.1 and CLAUDE.md.
- **Icon: none was set** (`m_BuildTargetIcons: []`).
  - New `Tools/steam_art/export_app_icon.py` cuts `Assets/CarpetCleaning/AppIcon/refluff_icon_<n>.png` for Unity's 8
    Standalone slots from the Steam icon art. Its 512 / 256 are identical to `SteamStore/icons/shortcut_icon_*.png`.
  - `SteamBuild.EnsurePlayerSettings` assigns them before every build: the Default Icon plus the 8 slots, importer
    uncompressed / no mips / no NPOT scaling. The build fails if a file is missing or imports at another size.
  - First rebuild: the exe's 48 frame was blurred, because the default NPOT scaling had imported the 48 px file at 64 px.
    Fixed as above.
- **Verified.**
  - Recompile: 0 errors. Release build `[SteamBuild] OK ... 210 files, 556.6 MB, version 0.9.1, ... icon
    1024/512/256/128/64/48/32/16 px, full-screen window`. Console warnings are only the known ones (Pipeline note,
    CarpetFoam shader, UAL analyzer).
  - `python Tools/Steam/check_exe_icon.py` (new): the exe's group 103 has 9 frames. 16/32/48/64/128/256 are identical to
    the art (max premultiplied diff 0); Unity scaled 24/96/192. The v0.9.0 exe from Steam fails it on every frame.
  - Smoke test: a copy + `steam_appid.txt`, started with no screen arguments.
    - The window covered the monitor exactly: 0,0 3440x1440, popup style, no caption.
    - The class icon 32 px equals the art; the 16 px one is Windows-resampled.
    - PrintWindow frame 3440x1440: HUD in the corners, carpet centred, 88 fps.
    - Clean `WM_CLOSE` exit 0, which wrote `Fullscreen mode = 1`, 3440x1440, `Use Native = 1`.
    - `Player.log`: 0 exceptions; the user's player save was untouched (none existed).
- **Upload.**
  - The agent opened a console window for the user's login. The first try was a mistyped password (steamcmd exit 5
    "Invalid Password"). The second logged in.
  - `Successfully finished AppID 5304930 build (BuildID 25562494)`, manifest 8316280809583961206.
  - Against the baseline 983883189799426378 only `Refluff.exe` + `Refluff_Data/boot.config` changed: 2 chunks, 7 s.
  - The build is not set live (`SetLive ""`). Setting it live on `default` and publishing are the user's steps.
  - steamcmd now caches the login (`steamcmd/config/config.vdf` ConnectCache).
  - The window script became `Tools/Steam/upload_window.ps1` (retry on exit 5, status file; `STEAM.md` 7.2).
- **ZIP** rebuilt from the new content for the web route: 210 files, 174.6 MB, SHA-256 `a5aea566...6227`.
- **Open.**
  - The user sets BuildID 25562494 live and publishes it.
  - After updating, check the Steam install: full screen, and the fluffy-R icon on the taskbar. Windows may show a cached
    old icon for a pinned shortcut until it is re-pinned.

## 2026-09-27 (evening) - build v0.9.1 with the 31 languages (built + smoke-tested; NOT uploaded to Steam)

- **Request (RU).** Build the game including the localization and upload it to Steam; the user publishes it themselves.
- **Build.** `bundleVersion` 0.9.0 -> **0.9.1**. Pre-checks: `loc_tool.py check` 30 languages, 0 errors / 0 warnings; console
  0 errors; only docs had changed since the localization session's local test build. Release build
  `[SteamBuild] OK Builds/Steam/content/windows: 210 files, 556.6 MB, 28 s, version 0.9.1`, no licence / NC warnings
  (console: only the known CarpetFoam "potentially uninitialized" shader warnings + the Pipeline config note).
- **Smoke test of the release content** (copy + `steam_appid.txt`, cwd = the copy, windowed 1600x900, PrintWindow frames):
  `-language ja`, `ar`, `de`: `[Steam] ready: app 5304930`, 0 exception / error lines in Player.log each run, ~800 MB working
  set, 153-170 fps. HUD, tool strip, hint, buttons translated; Arabic right-to-left with its font; German fits.
- **Upload ZIP.** `Builds/Steam/upload/Refluff_0.9.1.zip`: 210 files at the root (no pdb / steam_appid / DoNotShip),
  174.5 MB, SHA-256 `31f01f4a197cc07003480650c7e914817fc71bcbbc1a36f37d6b975d640e3e00`.
- **Not uploaded.** Passing the ZIP into the Web Uploads page as 9 MB parts (`STEAM.md` 7.1) was stopped at part 18 of 20
  by the Claude Code auto-mode classifier ("Production Deploy"). Nothing reached Steam: the parts were only in the tab's
  memory, Upload was never pressed, the helper input was removed afterwards. The upload is the user's (or needs a
  permission rule for the agent): Web Uploads -> file `Refluff_0.9.1.zip` -> Standard -> Upload -> "Commit uploaded depots
  as build" -> Builds -> set live on `default` -> Publish.

## 2026-09-27 - Localization: 31 interface languages + Language drop-down in the menu (`LOCALIZATION.md`, new)

- **Request (RU).** A language drop-down in the menu, and the game localized into the 31 languages ticked (Interface)
  in Steamworks.
- **System.** Keys are the English text as shown. `Loc` (T / F / Key, language table, Steam -> OS -> English detection,
  PlayerPrefs `refluff.language`, `-language` switch), `LocJson`, `KeyNames`; `HudLocalizer` (-50) re-texts every unnamed
  UXML label that is a key, sets the script font / RTL / `lang-*` `script-*` classes; the views (`CleaningHUD`,
  `EconomyHud`, `ShopView`, `PayoutView`, `PauseMenu`, `SurfaceHighlight` toast) translate their own texts and re-text on
  `Loc.Changed`. `SteamService.GameLanguage` added. 100 keys (`Tools/loc/strings.json`, with context for translators),
  including `{0}%` so percentages follow each language ("85 %", "%85").
- **Menu.** `LanguagePicker`: select + popup list (31 native names, each in its own font and direction, tick, scroll to
  current), catcher closes the list, Esc closes it first; VOLUME / LANGUAGE captions equalized.
- **Translations.** Eight background agents, one per language family (fr it es es-419 / pt-BR pt-PT ro / de nl sv da no /
  ru uk bg / pl cs hu fi / el tr ar / ja ko zh-Hans zh-Hant / th vi id ms), each with the game description, the
  screenshots, `strings.json` and rules (register, uppercase captions, consistency pairs, lengths, kept names), writing
  its json + rich-presence vdfs and validating with `loc_tool.py check`. Russian reviewed line by line afterwards.
  Terms the translators flagged for a native look: `LOCALIZATION.md` 10.
- **Fonts.** Built-in Inter already covers 24 languages; the Advanced Text Generator (default in this Unity build) shapes
  Arabic and Thai. Windows fallback fonts would draw Japanese / Traditional Chinese with YaHei's Simplified forms and
  lose bold, so Noto Sans Arabic / Thai / JP / KR / SC / TC are bundled: `Tools/loc/build_fonts.py` (Regular + Bold
  instances, subset to everyday sets + used characters, ~10 MB total) -> `LocFontBuilder` (dynamic SDF assets, Bold in
  the weight table, cross fallbacks). OFL notices appended by `make_notices.py`; Credits.txt got a Fonts section.
- **Layout.** `.strip` min-width, POWER / AREA and menu captions equalized from C#, shop legend wraps, no letter-spacing
  on Arabic / Thai captions, `.script-arabic .pline` 62 px, `.lang-rtl` mirrors the chip / icon rows; money, percent,
  times and ×-gains pinned LTR. Hotkey chips follow the keyboard layout (QWERTZ Z -> Y), Latin fallback for non-Latin
  layouts.
- **Verified.**
  - `loc_tool.py check`: 30 languages x 100 keys, 0 errors, 0 warnings (incl. glyph coverage, sources, 31 vdfs).
  - Play, scripted Step frames, 31 languages x 6 screens (HUD, menu, menu + list, shop, pay slip, credits): 0
    overflowing labels (detector proven on a planted 91 px / vertical overflow). Spill sweep: only the Arabic pay-slip
    lines (4.5 px) -> fixed.
  - Contact sheets of all 31 (menu + list, shop, pay slip) + credits ar / ja / ru reviewed by eye.
  - Logic: 31 Steam names, OS fallback, 31 tables with 0 missing keys, save on pick / forget, unknown key -> English.
    The Editor starts in English (Steam game language `english`, OS Russian). Keyboard on the Russian layout: Z chip
    stays "Z".
  - Player (`Builds/Steam/local`, 211 files): `-language ja / ar / th` runs captured with PrintWindow, fonts + bold +
    Arabic RTL + Thai marks correct, no exceptions in Player.log.
- **Open (the user's).** Publish the Interface language checkboxes; upload + publish the 29 new rich-presence vdfs;
  build and upload a new version (v0.9.0 has no translations; bump `bundleVersion`); native-speaker pass over the
  flagged terms; the store page text is still English only (`RELEASE_CHECKLIST.md` 2.2).

## 2026-09-27 - Steam: trailer uploaded, pause menu + credits, build v0.9.0 live on default, both checklists COMPLETE (`RELEASE_CHECKLIST.md`)

- **Request (RU).** Upload the trailer to the Steam page, build the game, and close every mandatory item so that both the
  store page and the build are ready to publish.
- **User decisions.**
  - Price **$3.99**.
  - Publish settings and set the build live, but do NOT press either review button.
  - Tripo tool heads came from a **paid plan / API credits** (commercial use allowed).
- **Trailer.** Store Admin > Trailers: "Refluff - Gameplay Trailer".
  - Settings: Gameplay, visible, before screenshots, all ages, English, own cover.
  - The file went into the page as 15 x 9 MB parts (the browser file tool caps a call at 10 MB) and was rebuilt there with
    `new File(parts)`. Its SHA-256 equals the master's. It went to S3 through the page's `AddMovieForUpload`, and the server
    state reads `published`.
  - The cover's CDN path is the cover file's SHA1. `TRAILER.md` 13.
- **Price.** Tier $3.99 plus Steam's recommended regional prices (41 changes). The save with "publish automatically after
  review" was refused to the agent by the auto-mode classifier ("Production Deploy"), and the user saved it. The package
  shows the prices as Current.
- **Game: pause menu** (`PAUSE_MENU.md`, new).
  - Esc or the Menu button opens Resume, master volume (PlayerPrefs), Credits (`UI/Credits.txt`: CC BY sounds, museum
    rugs, Tripo, libraries) and Quit to desktop.
  - It is modal like the shop (`UiPaused` + `UiModal`). It runs at 228, before EconomyHud (230), so one Esc never both
    closes the shop and opens the menu.
  - Wired by `CarpetSceneBuilder.BuildUI`; debug calls `CarpetDebugApi.MenuJson/MenuOpen/MenuVolume`.
- **Legal files in the build.** `SteamBuild` writes `CREDITS.txt` and `THIRD_PARTY_NOTICES.txt` next to the exe.
  - The notices come from `python Tools/Steam/make_notices.py`: Steamworks.NET MIT, glTFast Apache 2.0, Newtonsoft MIT,
    MCP for Unity MIT.
  - `SteamBuild` warns about any managed dll without a notice.
  - `bundleVersion` 0.1.0 -> **0.9.0**.
- **Verified.**
  - Recompile clean (x3), Build Scene wires PauseMenu (credits, sim, tools, economyHud, uiAudio).
  - Play, scripted Steps: sim +0.20 s / 10 frames before, **0.00 s / 20 frames** with the menu open, +0.20 s / 10 after
    closing; menu refused while the shop is open; 28 credit lines, panel 640 px; volume 0.35 live + saved (reset to 1);
    Quit path counted.
  - Captures `Assets/Screenshots/menu_main.png`, `menu_credits.png`. Economy save byte-identical to the snapshot.
  - Release build: 210 files, 546.4 MB, 28 s, no licence warnings.
  - Smoke run of a copy + steam_appid.txt, windowed, 45 s: `[Steam] ready: app 5304930`, no exceptions, 788 MB working
    set. A PrintWindow frame shows carpet + HUD + Menu at 222 fps. URP logs that the DoF / Panini shaders were stripped;
    the game uses neither.
- **Build upload.** SteamPipe > Web Uploads, ZIP 176.6 MB, Standard (`STEAM.md` 7.1).
  - The chunks show no progress for minutes, then finish together (about 4.5 min).
  - Manifest 7836903329287943867, **BuildID 25558788**: 573.0 MB on disk, 165.6 MB download, live on `default`. The final
    native confirm() was clicked in Chrome, not by the agent.
- **Steamworks settings.**
  - Rich Presence en + ru uploaded ("Saved").
  - Publish #1: icons + RP localization.
  - "Platform Support Matches" then turned red: Application > General had no OS ticked. Set Windows + 64-bit only;
    Publish #2 (`oslist windows`, `osarch 64`).
  - Landing: Store Presence **COMPLETE**, Game Build **COMPLETE** 13/13, OS in sync.
- **Mistake, cleaned up.** A first smoke run without a working directory missed `steam_appid.txt`, so the game
  relaunched through the Steam client (a Steam install prompt may have appeared). A `CopyFromScreen` capture then grabbed
  the user's screen instead of the game window; the image was opened once by mistake and deleted at once. Use `PrintWindow`
  (`PW_RENDERFULLCONTENT`) for a game-window grab.
- **Open (the user's buttons).** "Mark as ready for review" on Store Presence first. After it Steam offers "Submit build for review".
- **Open (recommended, not blocking).**
  - Audio rebuilt from the Freesound originals (`AUDIO.md` 11.2).
  - Page localization (2.2).
  - Capsules under ACES.
  - Steam Cloud (Auto-Cloud `WinAppDataLocalLow` / `VahovGames/Refluff` / `carpet_save.json`).
  - Tripo textures shrink (548 MB build).
  - A real Esc press was not tested (keys cannot be faked from an eval).

## 2026-09-27 - the dry vac, the hose's water and the squeegee now lay the pile (`FIBER_SET.md` 4-7; verified in Play, not judged live)

- **Request (RU).** Keys 2 (dry vac), 3 (hose water) and 6 (squeegee) did not move the fibres. The vac should suck them in;
  water should press them the way it spreads until they are dried or pressed another way; the squeegee should press them the
  way it travels.
- **Measured before.** Hose held 1 s: water ran out at 140-280 mm/s but the pile leaned 0.006-0.019 (the only coupling was
  `Fext = 0.0025 (u, v)`), 0 two seconds later. Dry vac: 0 (it only damped b toward upright). Squeegee: on soaked pile it
  already set 0.51 along the stroke, but on dry pile the spring snapped the press back in ~0.3 s, overshooting backward
  (bu -0.064 behind the blade), and damp pile without free water got only a weak set.
- **Changed.**
  - `K_Fiber` flow lay: the water velocity after the substeps lays the set its way (`FLOW_*`, 7 settings); thresholds from a
    measured |u, v| histogram (a settling puddle creeps at < 10 mm/s and leaves the lay alone); a flow never lifts pile already
    lying its way and never un-flattens.
  - Dry vac suction: `VacPullWeight` (SimCommon.hlsl), a ring from 0.35 to 1.8 nozzle radii. `K_Stamp` leans `b` into the nozzle
    at once, also outside the footprint (so the pile ahead of a moving head leans back toward it); `K_Fiber` pulls the set the
    same way per real second, so a moving head leaves the pile combed its way (`VAC_PULL_*`, 3 settings). Under the head the set
    is still lifted (`SET_FLUFF_VAC`).
  - Squeegee: capture gain `max(wetSet, SQ_SET_DRY)` (0.8), so it presses dry and damp pile too.
  - `SET_DRY_RELAX` 4 -> 0.6 /s: a lay on dry pile halves in ~1.2 s and is gone after ~8 s (at 4 it vanished under the blade).
    Dry pile at rest is still always upright.
  - 11 new `SimSettings` fields, C# and `.asset` in sync. No new resources: `K_Stamp` 8/8 and `K_Fiber` 5 UAVs.
- **Verified** (numbers in `FIBER_SET.md` 11): hose starburst 0.53-0.62 around the jet, held at +5 s (before 0.02 -> 0); dry
  squeegee 0.55 forward / f 0.83 right behind the blade -> 0.27 (1 s) -> 0.04 (4 s) -> 0 (8 s); wet squeegee 0.51 held as
  before; vac held: -0.49 into the nozzle around the skirt, centre upright; vac trail 0.40 -> 0.20 (1 s) -> 0.003 (8 s); the vac
  and the hose re-lay a wet squeegee stripe their way; the 2026-09-21 wet-vac + dryer workflow still ends upright (+20 s idle
  |b| 9e-19); a full cleaning chain on the soiled carpet: no NaN; whole chain 2.0-2.5 ms/frame. Compute 0 messages (and after a
  dispatch), two recompiles clean, no console errors. A/B captures `Assets/Screenshots/fiberlay_*.png`.
- **Open.** The user has not seen it live. Under standing water > 0.3 mm the refraction softens the combed pattern until the
  water soaks in. A held foam sprayer's puddle lays the pile a little too (0.27), like any flow.

## 2026-09-26/27 - the Steam trailer shot and edited (`TRAILER.md` 12; master checked 18/18, two review rounds; NOT uploaded)

- **Request (RU).** Shoot and edit the trailer per `TRAILER.md`; use the Transylvanian rug (the user's screenshot) for the
  first shot. Mid-task: follow <https://steam-izdat.github.io/04-marketing/store-page/>. Decisions asked: the Transylvanian
  for the hook only (#01 + #02; the hero stays the Anatolian star); no new Tripo heads (rings stay).
- **Result.** `SteamStore/trailer/refluff_trailer_main.mp4`: 60.0 s, 1920x1080, 60 fps CFR, H.264 High, 17.6 Mbit/s,
  AAC 48 kHz; -14.1 LUFS, -2.5 dBTP. `check_trailer.py`: 18/18 PASS. Cover `refluff_trailer_cover.png` (#14 mid-wipe,
  text-free), sheet `refluff_trailer_sheet.jpg`, review clips in `SteamStore/_work/trailer/checks/`. 21 shots on 20 takes.
- **Pre-shoot fixes (`TRAILER.md` 8.3).**
  - Floor water gloss: `Shaders/RoomReflection.hlsl` (new): water mirrors a ceiling with long light fixtures (0.5 x 1.8 m,
    pitch 4 x 3 m, radiance 8 x light, parallax-correct; one mirrored where the squeegee pours); the mirror normal is
    the sim slope + a slow swell, the 4 mm ripples only blur it (as roughness). Used by `FloorWater`, `CarpetWater` (times
    the tuft occlusion) and `FloorSurface` (damp film). Glaze wet darkening 0.86 (grout 0.55) instead of a flat 0.55.
    Rejected on the way: a 1.2 m panel grid (camouflage blotches, a "sheet of paper" on a flooded rug). `FLOOR_SYSTEM.md` 5.1.
  - Dry vac = InspectorJ's Dyson (CC BY 4.0) instead of the Samsung (CC BY-NC): `ImportMachineRecording` + the
    `ImportShippedRecordings` line; `AUDIO.md` 11.4, `AUDIO_CREDITS.md`, `audio_recordings.py` updated.
  - Hand-over corner: `CarpetHandover.CornerLift` (probe `fix_probes/v1_corner_lift.cs`: 0.0 mm out and in; was -77.9 / -5.6).
  - Shop `.ubuy` centred (probe v2: dy 0.00 px, dx -0.38 px); floor tools show no key chip instead of "·" (mouse-only
    kept: a lane agent had added keys 9/0 against the user's 2026-09-20 request; reverted). `HUD.md` 9.
  - Crust: dried-mud slabs (warped ~40 mm Voronoi, wandering crack width, thin partial crazing, per-slab tone / height /
    tilt, bevel at the cracks, no strand domes) instead of 16 mm pebbles; `dirt_preview.py` follows; the scraper damage
    reads `e * 0.84` so its crust removal stays within +2..6 % (A/B: seed 1 +5.5 %, 394 @0.9 +1.9 %). `DIRT_LEVEL.md` 3.1.
- **Capture + sound tools** (built by a prep workflow: 4 briefs + 4 builders, each adversarially reviewed; C# staged
  outside `Assets` and applied in one recompile).
  - `StoreGifApi.SeqBegin(..., fps, audioLog)`: `EditorApplication.Step()` advances `Time.fixedDeltaTime` (measured; it
    ignores `captureDeltaTime`), so `fps: 60` sets it to 1/60 for the take and `Release` restores it; dt = udt = 0.0167.
  - `AudioRenderer` records silence in an unfocused Editor (dspTime frozen, measured). So `audioLog` writes
    `audio.jsonl` (per-Step `AudioJson` + every playing source + `AudioTap` events) with a virtual audio clock
    (`Scripts/Audio/AudioTap.cs`, `AUDIO.md` 12); `Tools/steam_art/trailer/foley.py` renders the game's sound from it
    (RecordedMachine port, loops, one-shots, licence guard). Finding: ToolAudio's OneShot source is at volume 0 (the
    power / switch clicks are silent in-game); left for the user.
  - Edit = code: `edl.json` -> `make_trailer.py` (virtual camera, captions in the HUD pill style, logo whip, wipe, end
    card, mix, loudnorm, x264/AAC; new `--audio-only` remux; limiter release configurable, 120 dB/s here) ->
    `check_trailer.py`. Room tone `make_roomtone.py` (CC0 580534's quiet stretches, clicks rejected). Docs:
    `Tools/steam_art/trailer/README_editor.md`, `README_foley.md`, recipes `Tools/steam_art/trailer/recipes/README.md`.
- **Shoot** (all 60 fps + audio log; per-shot numbers in the recipes). Hook: the Transylvanian with round-4 chemistry
  (w 0.178, slurry 0.633, soap 0.0018), lane A squeegee at 500 mm/s -> glossy brown pour on the tile; #02 brush AREA 6 in
  pink foam. Arrival: forced $0 hand-over -> Away -> the Anatolian 394 @0.9 flies in. Chain #06-#13 on clip 1's band with a
  snapshot before every take. #10's foam never greys (brushing soapy foam grew F 1.4 -> 5.1); the #11 wave needed the foam
  dry-vacced off (+ hose top-up, 7 s wait). #13: the dryer does act (w 0.25 -> 0.003, bend 0.43 -> 0.01, luma x2) but it
  does not read at 600 px under ACES; lawn stripes also weak -> retaken under the moisture lamp from the pre-#13 snapshot
  (TRAILER 8.2 fallback). Payoff: take B (clean plate + seeded sparkle burst), take C (real hand-over $120, pay slip at
  1.6x via PayoutSeek, shop buy -$75). Lamp #18: seed 39 @0.3 on the star, the floor needed wet-vac lanes, detergent + mop
  for dried residue and the dryer on the tile before nothing glowed there. Montage: rya / Salor / European / mop.
- **Reviews.** Cut 1: five critic agents (steam-izdat rules, 600 px visuals, festival + microtrailer, audio, captions):
  a third of the timeline at digital zero, #13/#07/#12/#17 unreadable at 600 px, logo at 5.3 s, a weak cover, pops at cut
  ends, a HUD flicker. Cut 2 fixed them (half-bar #02 -> logo 4.0 s, chain 12.0-33.3 s, 60.0 s; room tone; retakes /
  cameras / in-points / gains). Cut 2 review (3 critics): "ready" (holistic), one-frame #04 flicker (visual), a clock-like
  click in the room tone + a hole at #08's head (audio). Cut 3 = the master above.
- **Verified.** check_trailer 18/18 (format, CFR, no black, -14.1 LUFS / -2.5 dBTP, safe area, reading time, no Samsung);
  every cut-1 fix confirmed by the cut-2 critics (list in the workflow journal); the economy save restored byte for byte
  (md5 `923ed7495c25dbd3883c38c39a51351c`), Play stopped, `fixedDeltaTime` 0.02.
- **Open.** Upload to Steamworks (`tab_movies`, first slot, custom cover) is the user's call. YouTube: the
  `AUDIO_CREDITS.md` block in the description. ASMR cut / 30 s / 9:16 not built. Freesound previews -> originals before
  release. The floor detergent never appears in the trailer (9 of 10 tools shown).

## 2026-09-26 - Store refresh: ACES screenshots + description clips (uploaded; checked on the Steamworks pages and the store beta preview)

- **2026-09-27 follow-up.** The rug count is gone from the page: the final game will have more than five carpets. The clip 4
  caption now ends "...spills over a real museum carpet." (was "over one of five real museum carpets"). Saved in Steamworks
  ("Changes saved"); the server text matches `SteamStore/description/about_this_game.bbcode` (1834 chars). The same line was
  changed in the `make_headers.py` mock. No other store field (short description, alt texts, headers, clips) named the number.

- **Request (RU).**
  - Finish `RELEASE_CHECKLIST.md` 2.1 (GIFs in About This Game), put them on the page, and replace the screenshots with
    the re-shot set. Do all of it in the browser.
  - Then the user switched Tone Mapping to ACES: re-shoot and upload the screenshots, then the clips. The dirty part of
    the rya shot and of the first shot must be chunks of dirt, as dirty as possible, not just dust. Follow
    <https://steam-izdat.github.io/04-marketing/store-page/> and the rest of that site.
  - Asked about ACES crushing the darks: "оставляем как есть" (keep ACES, no shadow lift).
  - Later: clip 3 (the tool relay) must be the very first clip.
- **ACES.**
  - The user's switch lived only in memory. It is now saved in `Assets/Settings/SampleSceneProfile.asset` (Tonemapping
    mode 2 = ACES, `acesPreset` not overridden), and `CarpetSceneBuilder.SetupVolume` writes `TonemappingMode.ACES`
    (was Neutral).
  - Cost, measured by the reviews: dark dyes and wet pile go near-black.
    - Hose shot, rug p5 luma 30 -> 6.
    - A soaked area (69,43,42) -> (34,16,15).
    - Tiles ~222 vs 226.
  - The user accepted it. Docs that quote Neutral numbers got a one-line note (`FOAM_LOOK.md` 4, `FLOOR_SYSTEM.md` 5,
    `HIGHLIGHTS.md` 4 "Colour"); `ARCHITECTURE.md` now says ACES.
  - Capsules, library art and key art were NOT re-rendered: they are still the Neutral plates.
- **Screenshots** (9, re-shot under ACES, uploaded). New order per steam-izdat: 1 striking, 2 gameplay, 3 progression,
  4 striking, then alternate, and never the same rug side by side.

  | Slot | File (capture name) | Rug | How |
  |---|---|---|---|
  | 01 | `01_hero_reveal` | Anatolian star | RESTAGED: `Reset(394, 0.9f)`, crust over grime (dust .65, mud .91, crust .61, spill .89); a ragged scraped cut through the medallion, dried; wet-vac wand pinned (6, 0.50, 0.28); crop (670,250) + 2560x1440 of the 4K scene, no HUD. 3 art-director rounds (seed 242 read as a pebble mat, rejected). Recipe `Tools/steam_art/screens/recipes/reshoot_aces_0926/hero/` (`run.sh` + `README.txt`, `_round1/`, `_round2/` kept) |
  | 02 | `02_scraper_crust` (`04_scraper_crust`) | European green | plain ACES re-capture from the runbook recipe, HUD |
  | 03 | `03_payslip` (`10_payslip`) | Transylvanian (handed over) | re-capture; retaken with `e4b_shot.cs` because the plain `Shot` eval caught a floor-detergent head from the OS mouse |
  | 04 | `04_rya_half_clean` (`12_rya_half_clean`) | Finnish rya | RESTAGED: `Reset(367, 0.9f)`; left half scraped dry, extracted and dried; one pink foam band on the seam; right strip 99 % crust plates; brush pinned (4, 0.765, 0.15); crop (528,232) + 3072x1728 -> 2560, no HUD. 2 rounds. Recipe `reshoot_aces_0926/rya/` |
  | 05 | `05_hose_spill` (`03_hose_spill`) | Salor Turkmen | `hose_final/run.sh` unchanged; bars 59.0 % / 81.5 % |
  | 06 | `06_lamp_moisture` (`17_lamp_moisture`) | European green | re-capture; `scraper_lamp/final_paused.cs` keeps the Editor paused |
  | 07 | `07_handover_flight` (`14_handover_flight`) | Anatolian star | re-capture (the sparkles are random) |
  | 08 | `08_upgrades_shop` (`11_upgrades_shop`) | European green | re-capture |
  | 09 | `09_spotless_handover` (`08_spotless_handover`) | Transylvanian | captured twice: the first ACES take had a faint dryer cursor ring on the tile (bottom left), because paused Steps read the OS mouse. `08_spotless_handover/s_spot2b.cs` parks `ToolController.ScriptedPointer` at uv (-20,-20) during the Steps and the Shot |

  - `build_store_art.py`: `SHOT_MOVE = {}` (`final_order.json` decides the order); `SHOT_VARIANT` is keyed by name, so
    the pay-slip deband follows `10_payslip` to slot 03. Both `final_order.json` copies updated. `verify_assets` ALL OK.
  - The Neutral set is backed up in `SteamStore/_work/v2_neutral/` (shots, screens, screenshots).
  - Upload: the 9 new files went up in order through the Graphical Assets drop zone, all flagged "suitable for all
    ages". The 9 old (Neutral) screenshots were deleted. Alt text written for all 9.
  - The order was verified by SHA1: Steam's `ss_<hash>` is the SHA1 of the uploaded file.
  - A **10th screenshot** (the squeegee plowing slurry off the rug onto the tiles; the guide wants every mechanic) was
    tried in 4 art-director rounds and NOT uploaded. Under ACES the wet pile goes near-black, the lanes left film edges
    or a hard oblique film strip over the medallion, and the floor water is matte and reads as a shadow. File:
    `SteamStore/_work/screens/not_uploaded/10_squeegee_floor_round4.jpg`; recipe `reshoot_aces_0926/squeegee/`
    (README round 4, `_round3/`, `_round2/`). It is out of `final_order.json` again.
- **Description clips** (About This Game, uploaded). Since 2025 Steam takes MP4/WEBM in the description: a 780 px column,
  1170-1560 px wide recommended, animated assets under 12 s, everything transcoded. Live pages serve WebM VP9
  (`<video autoplay muted loop playsinline>`, AVIF poster) and AVIF stills. So the "GIFs" are MP4 clips (H.264, no
  audio). Details: `STORE_CLIPS.md`.

  | Page # | File | Content | Size | Dur | Frame | Recipe |
  |---|---|---|---|---|---|---|
  | 1 | `refluff_clip3_tools.mp4` | one continuous take on one crusty patch of the Transylvanian rug: the scraper breaks the crust (chunks onto the tiles) -> foam -> rotary brush (AREA 6, so the bristles show) -> the squeegee pushes the foam wall off the rim | 3.47 MB, crf 22 | 8.76 s | 1560x650 | `clip3_tools/` |
  | 2 | `refluff_clip1_reveal.mp4` | the wet vac pulls flooded slurry out of a scraped / brushed band on the crusty Anatolian rug (`Reset(394, 0.9f)`); two horizontal passes, the turn happens off frame; the clean band holds 2.1 s | 3.46 MB, crf 16 | 9.96 s | 1560x650 | `clip1_reveal/` |
  | 3 | `refluff_clip2_water.mp4` | hose: three half-pressure bursts on a dry, dust-only rya (`ResetWithProfile(68, 0.36f, 0, 0, 1, 0)`) -> the mud patch wicks -> a pink foam mound heaps and settles | 2.04 MB, crf 20 | 7.20 s | 1560x650 | `clip2_water/` (r3) |
  | 4 | `refluff_clip4_rugs.mp4` | the spotless Transylvanian rug is handed over with sparkles and flies out, the dirty Anatolian (seed 15 @ 0.95) flies in; two jobs back to back, hard cuts, no dissolve | 2.87 MB, crf 27 | 6.56 s | 1560x878 | `clip4_rugs/` (uses `c4_clamp_on.cs`) |
  | 5 | `refluff_clip5_paid.mp4` | a real hand-over: the pay slip prints line by line (COMBO, $120, coins), the card leaves, the shop opens, two `ShopBuy` purchases (brush POWER, wet vac AREA); HUD take, camera push-in crop | 2.24 MB, crf 24 | 9.68 s | 1560x878 | `clip5_paid/` |

  - Recipes live in `Tools/steam_art/clips/recipes/`. Art-director rounds: clip 1 3, clip 2 3, clip 3 1, clip 4 3,
    clip 5 2.
  - Total description media 14.66 MB including the 4 header PNGs. `verify_assets` limit 15 MB: Valve's old per-image
    5 MB / 15 MB total rule is kept as our page-weight target.
  - **Header images** (steam-izdat: image headers instead of text headings), from `Tools/steam_art/make_headers.py`
    into `SteamStore/description/`:
    - `hdr_reveal` "Watch the pattern come back", `hdr_water` "Real water, real mud", `hdr_rugs` "A new rug every job",
      `hdr_paid` "Get paid, upgrade your kit";
    - 1560x120 RGBA, cream Fredoka pile letters, teal stitched underline, pink bubbles (the winner of 3 directions);
    - `hdr_tools` was dropped when clip 3 moved to the top;
    - Fredoka has no Cyrillic / CJK: localised headers need another OFL font (`FONT_BY_LANG`).
  - BBCode: `SteamStore/description/about_this_game.bbcode` (1845 chars, 9 `[img]` tags). The old text is kept as
    `about_before_2026-09-26.bbcode` (2147 chars, 7 text blocks). Mock: `SteamStore/_preview/description_mock.png`
    (780 + 390 px columns, fold at 850 px, real clip posters).
  - Alt text set for all 9 description assets: a one-sentence description for each clip, the header's own text for each
    header.
- **Code and tools.**
  - `Editor/StoreGifApi.cs` (new, Editor assembly): real-Step clip capture.
    - `SeqBegin(name, w, h, crop, hud, every, asyncPng)`, then a queue (`Tool`, `Hover`, `Stroke`, `Move`, `Hold`, `Up`,
      `Away`, `Call`, `Each`, `Every`), `Run(budgetMs)` until done, `SeqEnd` / `SeqAbort`.
    - Helpers `UvPixelJson`, `CropAround`, `ShopBuy` (reflection into `ShopView.OnBuy`), `Probe`, `SetFlip`.
    - Frames go to `SteamStore/_work/clips/<take>/fNNNN.png` (+ `_ui.png`) with `seq.json`. Audio is muted during a take
      and restored; statics reset via `InitializeOnLoad` / `InitializeOnEnterPlayMode`.
  - `ToolController`: `[NonSerialized]` `ScriptedPointer` / `ScriptedUv` (carpet UV) / `ScriptedDown` / `ScriptedRight`,
    cleared in `Awake`. They replace only the mouse ray, buttons and wheel; a UV outside the floor domain = the pointer
    is off the play area (no head, no cursor ring). The real-mouse path is unchanged (reviewed line by line).
  - `CarpetSimulation.ResetCarpet(int, bool, DirtProfile)` overload (the float overload forwards to it) and
    `CarpetDebugApi.ResetWithProfile(seed, level, crust, mud = -1, dust = -1, spill = -1)`; a negative value keeps the
    seed's roll (`DIRT_LEVEL.md` 8).
  - `StoreShotApi`: the capture core is public (`NewCaptureTarget` / `RenderHdr` / `FreeCaptureTarget` /
    `MarketingHudOn`), RTs are `HideAndDontSave`; behaviour unchanged.
  - `CarpetSceneBuilder`: ACES.
  - `Tools/steam_art/make_clips.py`: take folders -> MP4 (H.264 yuv420p, BT.709 tv range, no audio, faststart);
    linear-light Lanczos to `--width 1560`, `--crop`, `--height`, `--fade-loop`, `--hold-last`, `--crf`, poster + contact
    sheet; refuses >= 11.5 s. Also `clip_probe.py`, `gif_encoder.py` (GIF fallback, `--gif`), `make_headers.py`.
  - `verify_assets.py` checks the description: < 11.5 s, width >= 780, no audio, <= 5 MB each, <= 15 MB total, headers
    >= 780 wide (skips `_poster` / `_sheet`).
  - New dependency: `pip --user imageio-ffmpeg` (bundled ffmpeg 7.1 with libx264 / libvpx-vp9).
- **Steamworks UI quirks** (for the next upload).
  - Description "Upload Custom Images" creates a hidden `<input type=file>` in a form appended to `body`. MP4 staging
    needs a VISIBLE tab: Chrome loads no video metadata in a hidden tab, and the card never appears.
  - Asset reference = `{STEAM_APP_IMAGE}/extras/<file base name>` (no extension).
  - The About This Game textarea is React-controlled; only the sticky footer "Save" saves ("Changes saved" after the
    reload).
  - Graphical Assets screenshots: the drop zone accepts only drops; stage one file at a time (cards appear in async
    order, small files first); each card has the all-ages YES/NO radios. `DeleteScreenshot` deletes at once, no confirm.
  - The Steamworks session expired once mid-work; the user logged in again.
- **Economy save.** Snapshot before any Play (`scratchpad/save_snapshot/carpet_save_editor.json`, md5
  `222db82f7c49664005a620a8066e3e38`), restored byte for byte after `editor_stop`, md5 verified.
- **Verified.**
  - `verify_assets` ALL OK after both the screenshot rebuild and the clip packaging.
  - Screenshot order on Steam checked by SHA1; all 9 flagged all-ages, with alt text.
  - Live preview (store beta mode): `#aboutThisGame` max-height 850 px; clip 1 at 68-393 px, `hdr_reveal` at 462,
    clip 2 at 528-853 px, so both clips sit above Read More. The videos autoplay muted and loop; alt text present.
  - Capture probe: Step 18.6-20.7 ms mean, dt = udt = 0.02 per Step; ~48 ms per 4K frame including the async PNG;
    `EncodeArrayToPNG` output is upright.
- **Not verified.**
  - The public page: nothing was published, so the new media were seen only in the Steamworks editor and the store beta
    preview.
  - How the Neutral capsules / library art sit next to the ACES screenshots was not judged.
  - The real-mouse path of `ToolController` after the scripted-pointer change: code review only.
  - Takes with sparkles (screenshot 07, clip 4) are not bit-reproducible: the sparkle positions differ run to run.
- **Game issues found while staging (not fixed in the game).**
  - Hand-over flight: a carpet corner dips ~78 mm under the tiles on the fly-out (5.6 mm on landing). Clip 4 hides it
    with a runtime `beginCameraRendering` hook (`Tools/steam_art/clips/recipes/clip4_rugs/c4_clamp_on.cs`). The fix
    belongs in `CarpetHandover` (`HANDOVER.md` 10).
  - Paused `EditorApplication.Step()` frames read the OS mouse: a stray tool head or cursor ring shows up in captures
    unless `ToolController.ScriptedPointer` parks it.
  - Floor water renders matte (no gloss): at store size a puddle reads as a cast shadow.
  - Crust plates: one size, one light greige tone and a visible shell grid on top read as a pebble / cork mat at a
    distance under ACES. The reviewers suggest size / height / tone variation later.
  - The foam sprayer, dry vac and dryer have no head models (ring cursor only); known.
- **Open.**
  - Publish and "Mark as ready for review" are NOT pressed (the user's decision stands).
  - Capsules / library / key art are still from the Neutral plates.
  - Page localisation (`RELEASE_CHECKLIST.md` 2.2), the trailer (`TRAILER.md`), the CC BY-NC dry-vac swap, pricing,
    the build upload.
  - The corner clipping in the hand-over and the floor water gloss (above).

## 2026-09-26 (evening) - trailer script + storyboard (`TRAILER.md`, RU); nothing shot yet

- **Request (RU).** Write a trailer script and storyboard that follow <https://steam-izdat.github.io/04-marketing/store-page/>.
- **Research.** Every rule below is mapped to a decision in `TRAILER.md` 1.
  - The guide's trailer rules:
    - the first 3-5 s decide;
    - no studio logo first;
    - write the story on paper first;
    - show something new every 3-4 s;
    - don't fear spoilers;
    - ~20 % of visitors watch trailers, and they watch 30-70 % of the runtime.
  - Its festivals page:
    - the fest hub plays 0:12-0:35 muted and restarts with sound on hover;
    - Valve collects fest trailers ~1 month ahead.
  - The Steamworks Trailers doc:
    - max 1920x1080, 30/60 fps, H.264 5000+ kbps + AAC;
    - the first two trailers sit before the screenshots;
    - a custom thumbnail must be a frame of the video;
    - the microtrailer = six 1 s clips of the first trailer and cannot be customised.
- **Script.** 1:02, 21 shots on a 90 BPM grid (bar 2.67 s).
  - The hero rug is the Anatolian star (screenshot 01, key art, clip 1).
  - Hook (the user's call after the first draft: tool 6 looks more spectacular than the wet vac): the squeegee plows the
    brown wave off the Anatolian star onto the white tile. Then the brush in pink foam, then the title with
    "Headphones on". #11 SQUEEGEE in the chain is wider and on the chain lane, so it does not repeat the hook.
  - The festival window 0:12-0:35 is exactly the 8-tool chain, with verb captions "1 SCRAPE" ... "8 REFLUFF" (the chips =
    HUD hotkeys).
  - Payoff: before/after wipe, hand-over, pay slip, shop, lamp. Then a 5-rug montage, "Filthy in. Fluffy out.", and the
    end card over live footage.
  - Also covered: the caption style (Fredoka in HUD pills), a muted-first / ASMR sound plan (foley from `Audio/Recorded`),
    a per-shot capture table (StoreGifApi full 16:9 + offline virtual camera, reusing the clip and screenshot recipes),
    upload specs, a pre-upload checklist with an ffmpeg microtrailer simulation, and derived cuts (ASMR cut, 30 s, 9:16).
  - No CC BY-NC Samsung dry vac in the trailer.
- **Storyboard.** `Tools/steam_art/trailer/make_storyboard.py` writes `SteamStore/trailer/storyboard/01..21_*.jpg`
  (1280x720, the planned captions drawn in) and `storyboard_sheet.jpg` (2328x4487, timecodes, picture / sound / source
  status, festival-window frame).
  - It reads only existing captures: clips c1-c5, store screenshots, `_work/shots` 4K scene layers and the logo.
  - Placeholders are flagged. Unity was not driven.
- **Verified.**
  - The script runs: 21 shots, 1:02.0.
  - Every frame was checked by eye. The hook frame is the 4K scene layer of screenshot 10 (the squeegee at the rim,
    crop 1.5x), and the pay-slip caption moved bottom-right so it clears the card.
- **Also updated.** `RELEASE_CHECKLIST.md` (both trailer lines), `SteamStore/README.md` 6, `CLAUDE.md` (store-art bullet).
- **Open.** Before the shoot (`TRAILER.md` 8.3):
  - floor water gloss: the matte puddle reads as a cast shadow at store size (store refresh entry above), and the
    water pouring onto the tile is now the point of the hook;
  - a 1/60 s StoreGifApi step: Steam takes 30/60 fps, StoreGifApi is 50 Hz / 25 fps, and it is untested whether
    `Time.captureDeltaTime` drives `EditorApplication.Step`;
  - the Dyson dry-vac swap;
  - the hand-over corner-sink fix in `CarpetHandover.ApplyPose` (code in `clip4_rugs/README.txt`);
  - shop `.ubuy` centring and the floor key chips showing '·';
  - no dry vac / foam / dryer heads.
  - Main creative risk: does the pile lift under the dryer (#13 REFLUFF) read in 2.7 s? Fallback: the moisture lamp fades
    under the dryer.

## 2026-09-26 - dust puffs, hose splash drops, damp-pile "glass" lanes fixed; captures now HDR (verified in Play, reviewed)

- **Request (RU).** Fix three findings from the store-art staging (`STORE_ART.md` 6):
  - dust puffs grow into opaque beige blobs;
  - splash drops are invisible;
  - damp, pressed pile renders as grey "glass" panes.
- **Process.**
  - Three read-only diagnoses, then the fixes one at a time in Unity, each with A/B captures and numbers.
  - Six adversarial reviewers (a rules lens and a visual lens per fix), with a skeptic per non-low finding. 4 findings were
    confirmed, and one agent applied them together with the lows.
  - Baseline copies of every touched file are kept in the session scratchpad.
- **Dust** (`DebrisDust.shader`; `Debris.compute` stores a low-passed ground height in `p.dirt` for dust): density falls as
  1/area plus a 0.6/s settling loss, wisps, pale powder colour, soft base fade.
  - Rug-hiding opacity at +1 s: median/p90 0.90/1.00 -> 0.22/0.42. Fully opaque patches 48 % -> 0 %.
  - Puff counts and floor-dust deposits are bit-identical.
  - Details: `DEBRIS_SYSTEM.md` "Dust puff look".
- **Drops** (`DebrisDrop.shader` rewritten as a screen-space streak with a dark rim and a pixel-integrated glint; `K_Emit`
  launch 33-35 -> 50-57 deg, from the hose skirt). Measured at 1080p with one draw:
  - visible drops 34 -> 49;
  - median footprint 3 -> 44 px, streak 17x4.8 px;
  - near-white heads 0 -> 53 %;
  - floor water carried by drops per hose second within +0.5..+3.5 %.
  - The moisture lamp now shows the drops. `DebrisAliveJson` reports counts per kind.
  - Details: `DEBRIS_SYSTEM.md` "Splash drop look".
- **Glass** (`CarpetWater.shader`, `_SimConsts2` global from `CarpetSimulation.SetGlobals`, 6 new material floats in
  `CarpetSceneBuilder`).
  - **Cause.** Every film, even 0.1-0.4 mm, was drawn as a flat mirror-like free surface on top of pile the fibres already
    darken as wet. Its normal came from the 8 mm pressed-pile step, which made the white and blue edge lines.
  - **Fix:**
    - films under 0.3 mm fade out of the water layer (`_FilmLo`/`_FilmHi`);
    - normals come from the sim head, with the slope limited while shallower than the tufts;
    - band-limited tuft drape;
    - shallow specular occlusion.
  - **Results:**
    - squeegee pane (50,35,34), sat 0.31 -> (42,20,18), sat 0.57;
    - far-edge line peak/median 2.33 -> 1.30 (bare pile 1.46);
    - puddles and hose sheets still read as water;
    - foam unchanged, sim probes identical.
  - Details: `WET_LOOK.md` 5.
- **Capture tooling.**
  - `StoreShotApi` renders into an `ARGBHalf` target, then blits to sRGB. The 8-bit target had made URP render the scene LDR:
    grey tile 205 vs the Game view's (229,221,215), no glints, no bloom.
  - `DebrisSystem.SubmitDrawsForCapture` queues each particle kind at most once per frame. Before, repeated captures drew
    puffs and drops twice.
  - Verified: consecutive captures are pixel-identical.
- **Verified.** 0 shader/compute messages, recompile clean, 0 console errors. A/B images:
  - `scratchpad/fx/{dust,drops,glass}/compare*.png`;
  - `glass/after_review/compare_review.png`.
- **Drop look v3** (after the store re-shoot, the art director read the v1 streaks as "sewing pins"):
  - `DebrisDrop.shader` draws each drop as a ball lens: it samples the opaque texture inverted and transmits 0.35 with a
    water tint, has a thin refraction ring and an off-centre glint smear;
  - the tail fades out and gets shorter; length, width and brightness vary per drop; near drops ease to a clear lens.
  - An independent judge passed it: the pins are gone and the drops are still visible.
  - Numbers:
    - tile visibility above v1 (1080p luma contrast over tile 12886 -> 14380);
    - visible drops 50 -> 51;
    - median streak 16.9 -> 11.8 px.
  - Caveat: the lens needs the opaque texture. The game camera and PC_RPAsset have it; a camera without it gets darker
    drops.
  - Details: `DEBRIS_SYSTEM.md` "Drop look v3".
- **Store screenshots re-shot** (all 9, HDR, with the fixed dust / drops / glass; the old set is in
  `SteamStore/_work/v1/`):
  - two judges chose the new frame for every pair;
  - 05 (rya: no pressed-pile lattice or foam haze), 08 (the dry vac is highlighted, so it matches the hint) and 09 (a
    mid-stroke frame without the plastic-lid sheet; HUD bars settled, CLEAN 59 % fills 58.9 %) were retaken after the
    art-director review;
  - `build_store_art.py` rebuilt `SteamStore`. Capsules, icons and the logo are byte-identical. `verify_assets` prints
    ALL OK, and the final art-director verdict is **ship**.
  - Recipes: `Tools/steam_art/screens/recipes/reshoot_0926/`, notes in `SteamStore/_work/screens/reshoot_notes/`.
- **Economy save.** Restored after the re-shoot (md5 `222db82f...`, $88,905; the hand-overs had made it $1,960 / a
  mid-game kit).
- **Open.**
  - Render matters the art director noted (not capture-fixable):
    - the shop price pills' labels sit at the top (`.ubuy` lacks `justify-content: center`);
    - the floor-tool key chips show '·';
    - a soft magenta fringe along every foam band edge;
    - muddy water reads as a brown haze with no surface cue;
    - no visible hose jet;
    - a faint glassy loop at a static nozzle.
  - Hose over bare tile still makes no splash (no floor emit path).
  - Water still seeps back into pressed lanes (about 1 mm, `SQUEEGEE.md` 9); it now reads as a pooled film, not glass.
  - The dust fade still shows the carpet rim step.

## 2026-09-25 (later) - Store Presence checklist filled end to end: CHECKLIST COMPLETE

- **Request (RU).** Close item 1 (Store Presence), texts to be agreed with the user first. Then: read
  <https://steam-izdat.github.io/04-marketing/store-page/> and say what the drafts needed changing.
- **User decisions.** Support contact = `denisvaho@gmail.com`; release date October 2026; minimum system
  requirements stay as estimated; developer/publisher spelled **"Vahov Games"** with a space (Unity's
  company name stays `VahovGames` - the save path `LocalLow/VahovGames/Refluff` depends on it);
  short description variant A; About This Game rewritten from scratch by me.
- **What the guide changed.** Its rules that actually bit: the first GIF must sit above the "Read More"
  fold (90-95 % never expand), a feature block = heading + GIF + 1-2 lines, 4-5 blocks max, assets
  <= 15 MB total / 5 MB each; drop the generic "Indie" TAG (kept as a genre); no external URLs in
  About This Game (confirms putting the CC BY credits in Legal Lines); page localisation raises
  algorithmic priority, language order Chinese > Russian > German > ... The About text was restructured
  into short skimmable blocks; GIFs are still missing and are now tracked in `RELEASE_CHECKLIST.md` 2.1.
- **Filled in Steamworks** (all verified by re-reading the saved field values, not just by the "Changes
  saved" banner):
  - Developer + Publisher = Vahov Games.
  - Short Description 260 chars; About This Game 2174 chars of BBCode (visual editor unchecked first).
  - System requirements: they were **empty** even though the checklist showed them green - Steam only
    counts the platform selection there. Min: Windows 10 64-bit 1909+, i5-8400 / Ryzen 5 2600, 8 GB,
    DX12 GPU with compute + 4 GB VRAM (GTX 1060 6 GB / RX 580 8 GB), DX 12, 2 GB disk. Rec: Windows 11,
    i7-10750H / Ryzen 7 5800X, 16 GB, RTX 3060 / RX 6600 XT, 1440p 60+ fps. Disk figure from the real
    build: `Builds/Steam/content/windows` = 549 MB. The minimum spec is ESTIMATED - the game has only
    ever been measured on the RTX 3070 Ti laptop.
  - Release date: Oct 30 2026 10:00 (Steam refuses anything before Oct 12 and refuses weekends),
    public display "October 2026".
  - Languages English/Interface; Players Single-player; Genres Casual (primary) + Indie + Simulation.
  - Controller wizard: "Mouse and keyboard only" -> status No Controller Support.
  - Legal Lines: (c) 2026 Vahov Games + the full CC BY credit block from `AUDIO_CREDITS.md`.
  - Support email `denisvaho@gmail.com`.
  - Tag Wizard, 16 tags published in weight order: Cleaning, Relaxing, Cozy, Simulation, Job Simulator,
    Hobby Sim, Singleplayer, Casual, Atmospheric, Realistic, Physics, Colorful, Family Friendly,
    Top-Down, 3D, Procedural Generation. There is no ASMR tag in Steam's taxonomy (searched), nor
    "Satisfying". Steam's "similarly tagged games" then showed Rug Cleaning Simulator, Cozy Cleaner,
    Cozy Organizer - the right neighbourhood.
  - Content Survey: every adult-content box left empty; generated ratings published (Brazil "L",
    Germany "All Ages"). Generative AI = **Yes** (the Tripo tool-head models), with a store-page
    disclosure naming exactly what is AI and what is not; AI during gameplay = No; external AI
    services = No.
- **Verified.** The landing page now reads **CHECKLIST COMPLETE** for Store Presence and offers
  "Mark as ready for review".
- **Technique note.** Steam's store form keeps almost every value in hidden inputs driven by
  `OnLocTextChanged(prefix)` and image-checkboxes (`checkbox_<id>__unchecked` / `__checked` / `__input`),
  so filling it by clicking 40 widgets is hopeless. Setting `<prefix>_textarea` + calling the page's own
  `OnLocTextChanged`, and `.click()`ing the unchecked image, works and is verifiable by reading the
  hidden values back. The tag priority list is a jQuery `ul.ui-sortable` - reordering its children with
  `appendChild` is equivalent to drag & drop and survived Publish.
- **Open.** "Mark as ready for review" NOT pressed (user's call): the 5 description GIFs, the trailer and
  the CC BY-NC dry-vac swap should land first. Also still open: the app has unpublished icon changes
  (Publish tab), pricing, the build, and page localisation. All tracked in `Docs/RELEASE_CHECKLIST.md`.

## 2026-09-25 - Steamworks release checklist snapshot + App/Shortcut icons uploaded (verified on the live page)

- **Request (RU).** Посмотреть в Steamworks, что ещё осталось заполнить для релиза, выписать в список,
  сохранить его и закрыть пункт 2 (Community).
- **Snapshot.** Read both checklists on <https://partner.steamgames.com/apps/landing/5304930> through the browser.
  State before this session: App Release State `Unavailable`, release date `Unset`, package 1833640 `Hidden` /
  "No pricing approved", both checklists incomplete.
  - Store Presence done: System Requirements, 5+ Screenshots, Capsule Images, Library Assets.
  - Store Presence missing: Basic Info, Descriptions, Content Survey, Planned Release Date,
    Controller Support Description, Support Info, Developer and Publisher Names, Store Page Descriptive Tags.
  - Community missing: App Icon, Shortcut Icon.
  - Game Build done: Platform Support Matches, App Configuration, Depot Configured, Launch Options,
    Install Directory, Depot Languages, Store And Devcomp Packages Match, All Depots Attached.
  - Game Build missing: Pricing For At Least One Package, Published Pricing, Trailer Uploaded,
    At Least One Build Configured.
- **New doc** `Docs/RELEASE_CHECKLIST.md`: the full list as tick-boxes with the Steamworks URL per item,
  the post-checklist review timeline (store review 2-3 business days, build review 3-5, 2 weeks of
  "Coming Soon", 21 days since the app-credit purchase) and the project-side blockers
  (CC BY-NC dry-vac sample, CC BY credits on the store page, rebuild audio from the full-size sources,
  Tripo commercial licence, no trailer yet).
- **Done this session.** App Admin -> Installation -> Client Images
  (<https://partner.steamgames.com/apps/clientimages/5304930>), both fields were "(No image set)":
  - Shortcut Icon = `SteamStore/icons/refluff.ico` (118 KB, 16..256 px). "Convert shortcut icon to app icon at
    upload time" was UNCHECKED first, so the purpose-built JPG below is used instead of an autogenerated one.
  - App Icon = `SteamStore/icons/app_icon_184x184.jpg` (23 KB); Steam generated the 3 preview sizes.
  - Verified: the landing page now shows COMMUNITY 2/2 green (App Icon, Shortcut Icon).
- **Open.** The app has unpublished changes ("use the publish tools in the 'Publish' tab") - the icons are
  accepted by the checklist but will only reach the Steam client after a Publish, which was NOT done
  (waiting for the user). Everything else in `RELEASE_CHECKLIST.md`.

## 2026-09-25 - Steam store art: logo, capsules, library, icons, events, 9 screenshots (verified: ALL OK, reproducible)

- **Request (RU).** Every screenshot for the Steam capsule plus a logo, in a separate folder, following Steam capsule
  best practice.
  - Title fixed by the user: **"Refluff: Carpet Cleaning ASMR"** (memory `game-title`).
  - Result: `SteamStore/`, whose `README.md` maps each file to its Steamworks field. Pipeline: `STORE_ART.md`.
- **Code.**
  - New `Scripts/StoreShotApi.cs`: `Shot`, `RenderScene`, `RenderView` (free camera + temporary DoF), `MarketingHud`,
    `CameraJson`.
  - `DebrisSystem.SubmitDrawsForCapture()`: particles re-queued for a manual `Camera.Render`.
  - Dry run clean; recompile clean. In Play:
    - a 4K scene + 2560 HUD pair;
    - a DoF view;
    - 203 particles visible in a free-camera render after a scraper stroke;
    - the game camera restored to (0, 1.5, -1.12) / 55 deg / fov 45 after `RenderView`.
- **Tools.** `Tools/steam_art/` holds:
  - `make_logo.py`, `compose_screens.py`, `build_store_art.py`, `verify_assets.py`;
  - `capsule_variants/`, `keyart/` (finishers + `unity/` eval scripts), `screens/recipes/`, `logo_concepts/`;
  - `fonts/`: 16 OFL / Apache fonts with their licence files.
- **Process (4 workflows, about 40 agents).**
  1. Spec research, written to `_work/steam_spec.md`: the 2024 doubled sizes, the 2022 capsule-content rules,
     best practice.
  2. Logo: 3 concepts, 3 judges, then refine + 2 art-director rounds.
  3. Screenshots: 16 candidates staged with real tool strokes on all 5 rugs, 3 judges, a reshoot, 9 final. Key art:
     2 rounds of plates.
  4. Capsules: 3 composer sets, 3 judges, one assembler, 2 art-director rounds. The last verdict: "from capsules
     through events a professional would change nothing".
- **Verified.**
  - `verify_assets.py` prints ALL OK: 16 fixed assets + 9 screenshots at 2560x1440, JPG q95 4:4:4, 0.45-2.13 MB.
  - A second full `build_store_art.py` run is byte-identical (md5 over every output file).
  - Small capsule readable at 120x45, hero brush inside 1490-2350 x 430-810, library logo trimmed to 1280x373.
  - `_preview/store_page_mock.png` and `library_mock.png` show the assets in context.
- **Honesty.**
  - Screenshots use the fixed game camera only. 01 and 05 are no-HUD crops of the 4K frame.
  - 02 and 07 have the HUD timer dressed through reflection so it reads as a medium job; the rug is real.
  - Key art uses free cameras, 48 shells, a raking sun and post. That is allowed for capsules only.
  - No asset file changed during capture.
- **Economy save.** Backed up first. The screenshot agent changed money and levels; the original was restored after
  Play stopped (md5 `222db82f...`, $88,905). The `VahovGames/Refluff` copy made by the parallel session is the same file.
- **Open.**
  - `STORE_ART.md` 6 has the game findings from staging: no head models for tools 1/3/7, invisible splash droplets,
    grey "glass" lanes on damp pressed pile at grazing angles, dust puffs growing into opaque blobs.
  - An optional rya re-capture.
  - The Tripo commercial licence for the tool models.
  - The store page still needs the audio credits.

## 2026-09-25 - Steam names: Refluff.exe, company VahovGames, product Refluff

- **Request (RU):** what is needed to upload the first build to Steam; then: the executable must be `Refluff.exe`, company name `VahovGames`.
- **Exe.** `SteamBuild.ExeName` = `Refluff.exe` (so the data folder is `Refluff_Data`, the steam_api64.dll check follows it);
  `Tools/Steam/upload.ps1` checks for `Refluff.exe` and defaults `-Desc` to `Refluff <version> <date>`; depot script comment.
  `productName` stays `CarpetCleaning` (window title, second half of the save folder), see `STEAM.md` 10.
- **Company.** `PlayerSettings.companyName` = `VahovGames` (set through the API, `ProjectSettings.asset` saved). persistentDataPath
  moved to `LocalLow/VahovGames/CarpetCleaning` in the Editor at once; `carpet_save_editor.json` copied there (byte-identical,
  $88,905, all levels 6, both lamp items); the `LocalLow/DefaultCompany/CarpetCleaning` copy is kept as a backup.
- **Verified:** recompile clean; release build `OK Builds/Steam/content/windows: 208 files, 547.7 MB, 33 s, version 0.1.0`
  with `Refluff.exe`, `Refluff_Data/Plugins/x86_64/steam_api64.dll`, `Refluff_Data/app.info` = VahovGames / CarpetCleaning,
  no `steam_appid.txt`. The build still carries the CC BY-NC dry-vac machine (the SteamBuild warning). The player was not
  launched (a release exe relaunches through Steam, and the app has no build uploaded yet).
- **User to do on the partner site:** launch option executable `Refluff.exe` (was documented as `CarpetCleaning.exe`), then Publish;
  upload with `Tools\Steam\upload.bat -Account <login> -Preview`, then without `-Preview`; set the build live on a branch.
- **Then `productName` = `Refluff`** (user: yes, rename). persistentDataPath = `LocalLow/VahovGames/Refluff`; the editor save copied
  there (byte-identical to the `DefaultCompany/CarpetCleaning` backup, which stays), the intermediate `VahovGames/CarpetCleaning`
  copy removed. `SaveStore.Load` from the Editor: `loaded`, money 88905, carpets 6. Rebuilt: `OK ... 208 files, 547.7 MB`,
  `Refluff_Data/app.info` = VahovGames / Refluff. Nothing in code reads `productName`.

## 2026-09-25 - dark carpet backing (shell 0) instead of the print

- **Request (RU):** the carpet backing is white, make it dark, almost black.
- **Cause.** Shell 0 (t = 0) of `CarpetFibers` took the print colour x AO 0.45, so under cream / light yarn the ground between the
  tufts read as a white base (`backing_before_0/1.png`, close-ups 12 cm over the pile and at the left rim).
- **Change.** New `_BackingColor` (sRGB 0.13, 0.12, 0.11; shader default, `CarpetFibers.mat`, `CarpetSceneBuilder`), used for shell 0
  with the per-strand +-15 % jitter; dirt layers still apply on top (mud, stains, crust plate, crack colour unchanged).
  Anti-speckle fade: `backFar = smoothstep(0.25, 0.5, max(fwidth(uv * _StrandGrid.xy)))` blends the backing to print x 0.5. A first
  try without it (and with 0.3..0.8) turned the 1280x720 view into black speckle over the whole rug.
- **Verified** (Play, `Reset(3, 0)` spotless carpet, main camera rendered with MSAA 4): shader 0 messages; close-ups show near-black
  ground between the tufts (`backing_final_0/1.png`); game view at 2560x1440 / 1920x1080 shows separated tufts with dark gaps, 1280x720
  shows no speckle (`backing_final{2560,1920,1280}_full.png`); mean sRGB of the central carpet region at 2560 = 0.600/0.345/0.260 vs
  0.656/0.373/0.279 before (-9 %). Soiled carpet `Reset(1)` close-up: crust plate and cracks as before (`backing_dirty_0.png`).
  No new console errors after a Play restart (an Editor .cs edit compiled mid-Play once and wiped the RTs, the usual pitfall).
- **Open:** the user has not seen it live yet; if the carpet reads too dark from the game camera, raise `_BackingColor` or start the
  fade earlier (0.2..0.45).

## 2026-09-25 - every tool plays real recordings (Freesound CC0 / CC BY + Mixkit) instead of the synthesised placeholders (verified in Play; not listened to)

- **Request (RU):** find realistic sounds for every tool and put them in place of the generated ones.
- **Licence rule applied:** only CC0, CC BY 3.0 / 4.0 and the Mixkit SFX licence, each read on the sound's own page (the
  game goes to Steam). No NC, nothing from videos.
  - One Freesound "CC0" upload (371715) turned out to be a re-upload of a CC BY 3.0 sound (correlation 0.91) and was
    dropped.
  - Credits: `AUDIO_CREDITS.md` (new).
- **Search.** Scripted Freesound search + licence check + HQ preview + a level timeline, tonal lines and a spectrogram
  per candidate: about 300 candidates.
  - Four parallel agents took one group of tools each. The first run died on an API 403; the motors and wet-surface
    groups had written their findings, and the dry-surface and spray groups were re-run.
  - The machines and the final picks were checked by the main agent on the plots.
- **What plays now** (table in `AUDIO.md` 11.1):
  - Machine takes (start / loop / stop through `RecordedMachine`):
    - wet vacuum: joedeshon's Bissell carpet extractor (440 Hz line);
    - dryer: busabx's metal fan with a rotary knob;
    - hose: richwise's pressure washer, one trigger press;
    - foam: kev_durr's aerosol can;
    - detergent: Mixkit 3215, a pump-sprayer mist.
  - Brush: prometheus888's floor buffer as a loop of exactly 30 pad turns on the procedural spin; kyles' breaker clacks
    as its power on / off.
  - Surfaces: scraper akent (Berber scratching), dry-vacuum nozzle crcavol (a drag on carpet), brush RossJuterbock (a
    hairbrush on carpet), squeegee adda_records (a blade pushing water; 27 stroke middles re-joined), wet-vacuum slurp
    yehdawgo, mop byronabadia (a wet mop).
  - Unchanged: the dry vacuum's Samsung machine (CC BY-NC) and the crumb rattle (as asked).
  - Ready but not assigned: InspectorJ's Dyson (CC BY 4.0) as the dry vacuum's licence-safe machine.
- **Tools.**
  - `Tools/prepare_recording.py` (new): loops by spans or by re-joined stroke grains, one-shots, loudness matching to
    the placeholder, a look-ahead limiter capped at N dB for sparse peaks, `--release`, `--duck`, `--hp`.
  - `Tools/cut_machine_recording.py` gained OGG input, `--speed envelope` (+ `--env-smooth`), `--from-rest`, `--line`,
    `--hp`, `--duck`.
  - `Tools/audio_recordings.py` (new): the manifest of every source, licence and cut; `build` / `credits` / `fetch`.
- **Code.**
  - `ToolSoundEntry.clipCredits`.
  - `ToolSoundBaker.ImportClip / ImportClipsIn / ImportShippedRecordings (+ menu) / ReimportRecordings / Credits`.
  - `Bake` / `Clear` leave recorded slots alone.
  - `CarpetSceneBuilder` imports the shipped recordings into a NEW bank.
  - `ToolAudio` loops enter at a random point on every (re)start.
  - `RecordedMachineScenario(ToolId, ...)`.
  - `SteamBuild`'s NC check also reads `clipCredits`.
  - `ToolSoundBank` defaults: volumes calibrated to the recordings. `.asset` synced by Reset Voicing (Keep Clips); the
    asset had no hand-tuned numbers.
- **Verified:**
  - Recompile 0 errors (the only console errors are CLI 5 s timeouts of long imports; my static RNG's analyzer warning
    removed).
  - `audio_recordings.py build` of all 15 clip sets = bit-identical to the imported clips (md5).
  - `Report()`: recorded loop seams 0.02-0.51 x RMS.
  - `analyze_sfx.py`: surface loops tonal 2.8-5.8 dB / st90 11.0-14.2 (the noise reference is 12.4); tonal flags only
    on real motor lines, the washer's pump hum and the detergent nozzle's 346 Hz.
  - A scripted stroke with all 10 tools: each plays its recording.
  - `RecordedMachineScenario`: taps of 1 s (wet vacuum, dryer) and 0.1 s (sprayers) enter release and re-press at
    matching speed or level; for example the wet vacuum releases at 0.942 into the stop clip @0.15 s and re-presses at
    0.504 into the start @0.33 s.
  - `SuckTiming`: 42 / 42, error 0.00 ms, 0 late, 0 dropped.
  - Loudness vs the placeholders in `AUDIO.md` 11.1: all within 1 dB after the volume change except the brush scrub
    (4.6 dB under at volume 1), the foam (0.7) and the mop (0.2).
- **Not verified:** listening (the editor was unfocused).
- **Open** (`AUDIO.md` 11.7):
  - listen and tune the volumes: the brush scrub, the detergent's 1.66 s loop, the foam's 4 s loop, the hose recorded
    on stone, the slurp made by mouth;
  - a credits screen for the CC BY credits;
  - before release, rebuild from the Freesound originals (the clips come from ~192 kbps previews) and swap the dry
    vacuum to the Dyson.

## 2026-09-24 (night, after the lamp) - the carpet lamp follows the HUD bars: nothing glows at CLEAN / DRY 100 % (verified in Play)

- **Request (RU):** the look is fine, but at CLEAN 100 % the lamp still showed dirt. The bar and the lamp must agree: at 100 %
  nothing may show on the carpet.
- **Cause.**
  - The bar reads 100 % once the carpet-wide mean is within 0.5 % of the start: rounding, the Spotless threshold.
  - The lamp lit every trace above a tiny per-cell cut-off.
  - Measured: at 99.6 % every one of the 184,320 cells still held dirt, and the whole rug glowed orange.
- **Fix** (`HIGHLIGHTS.md` 4.1):
  - `K_Reduce` also fills two log histograms (amount + cell count, 60 bins, 2^-16 .. 2^4) of the bars' own per-cell
    measures, in the same stats buffer (16 -> 256 uints, one readback).
  - `CarpetSimulation.WaterLevel` finds the level `a` at which the carpet cut down to `a` holds exactly what the bar
    tolerates at 100 %.
  - A carpet cell glows with what it holds above `a`. That is `NoCut` (1e30) while `ShownPercent` reads 100.
  - Levels and bars come from one snapshot in `ApplyStats`, pushed as `_HighlightCut`.
  - `CleaningHUD` prints the bars through the same `ShownPercent`.
  - Shaders: `HL_ApplyCarpet` / `HL_ApplyOverCarpet` / `HL_CarpetCut()` in fibres, carpet water, carpet-domain foam and
    crumbs on the carpet (`CarpetCoversCMM`). The floor keeps its full lamp (no bar).
  - Rejected on the way: "the least-soiled cells up to the budget go dark". On the fully soiled carpet that cut sat at 1.26,
    so heavy mud would have shown dark holes. The level puts it at 0.0079 there, and everything glows.
- **Verified:**
  - Recompile 0 errors; compute 0 messages after its first dispatch; shaders 0 errors.
  - The histogram mean equals the stats and an exact CPU sum (1.5718 fully soiled).
  - The level is within 0.6 % of an exact bisection over every cell (read back from the RTs).
  - Scaled carpets:
    - patchy, level 0.3, CLEAN 97 / 99 / 99.4 / 100 % -> 120,565 / 93,743 / 59,921 / 0 lit cells;
    - fully soiled at 99.6 % -> 0 lit, although every cell holds dirt;
    - DRY 95 / 99 / 100 % -> 34,976 / 28,482 / 0.
  - At 100 %, 437 crumbs on the carpet stay dim and the ones on the tile glow.
  - Captures: `hl_level_100_pair` (old vs new), `hl_patch_grid`, `hl_wet_lvl_row`, `hl_chunks_*`, `hl_hud_clean100`.
  - Frame cost unchanged (off 6.0-6.3, on 6.8-7.2 ms per stepped frame). `ReadStatsSync` 0.29 ms including its blocking
    readback; the game runs it async every 10 frames.
  - The user's save (they bought both lamps in between) was snapshotted and is untouched (md5 29ad7d91...).
- **Open:**
  - the floor still glows at 100 % (dust, crumbs, splashes on the tile; the tile has no bar);
  - the level runs up to 0.6 % high from the in-bin interpolation (harmless: below 100 % something always stays lit).

## 2026-09-24 (night, after the AREA change) - inspection lamp: dirt highlight (X) and moisture highlight (Z), one-time shop items (verified in Play)

- **Request (RU):** two upgrades for the shop, each bought once: dirt highlight and moisture highlight. Z switches
  moisture on / off, X switches dirt.
- **User choices** (asked before building):
  - look: "inspection lamp" (the scene dims, soil glows amber, water blue);
  - prices: moisture $250, dirt $400;
  - where: carpet and floor.
- **Built** (full design: `HIGHLIGHTS.md`):
  - Economy:
    - `ShopItem`, `SaveData.items` (fit-and-clamp migration);
    - `EconomySettings` prices (asset synced);
    - `Economy.Owns / ItemPrice / TryBuyItem / DebugSetOwned`, event `ItemChanged`;
    - the badge counts unowned items.
  - Shop: an INSPECTION section with two UXML cards bound by `ShopView` (price / OWNED, poor, shake + deny, buy burst
    `fx--sky` / `fx--amber`), and an `icon_eye` icon.
  - Lamp:
    - `SurfaceHighlight` on CarpetUI: Z / X, gated by the shop and the Steam overlay; refusal with a toast;
    - fades and a scan ring;
    - globals `_Highlight` / `_HighlightTune`, zeroed on disable.
  - HUD: Z / X chips in the meter heads, shown once owned and filled while on; `CleaningHUD.Toast`.
  - Shaders: `HighlightCommon.hlsl` in six shaders. It uses the pay slip's own measures, a log response (100x range),
    compressive dimming, 28 mm amber / blue stripes where both lamps show, and water glow x (1 - transmittance).
  - Debug:
    - `CarpetDebugApi.SetHighlight / ToggleHighlight / HighlightJson`;
    - `EconomyDebugApi.BuyItem / SetOwned`;
    - menu Economy > Unlock / Remove highlights (test).
- **Verified** (recompile 0 errors; shaders 0 errors; Build Scene wired and saved; the user's save snapshotted first):
  - The old save loads with nothing owned. X without the item: refused, with the toast.
  - Shop:
    - with $300: moisture $250 buyable, dirt $400 `poor`, badge lit (every tool track is MAX, so the items light it);
    - real button clicks: moisture $300 -> $50; dirt with $50 refused; dirt $450 -> $50; clicking an owned item costs
      nothing;
    - saved `items [1, 1]`, badge off.
  - A Play restart keeps both items, the chips show, the lamps start off.
  - Taking an item away while its lamp is on switches it off and hides its chip. The menu works in Play and in Edit mode.
  - Stopping Play with both lamps on leaves `_Highlight` = 0 in Edit mode.
  - Look: captures `hl_off / hl_dirt / hl_wet / hl_both`, `hl_light_pair` (a level-0.3 carpet that looks clean: the lamp
    shows a large dust / grime patch), `hl_light_vac_pair`, `hl_sweep`, `hl_shop_*`, `hl_meters_owned`, `hl_hud_both_on`.
  - Cost: lamps off 6.16-6.30 ms per stepped frame, both on 6.80-6.91 ms (about +0.6 ms at the test size).
  - The user's `carpet_save_editor.json` was restored byte-for-byte (md5 09b445ca...).
- **Tuning found on the way:**
  - The first response saturated at 0.32 (fully soiled carpet = flat cream) and the yellow-white peak washed out: the
    range became 100x the ref and the ramps saturated amber.
  - A plain x0.38 dim left the tile light grey (203 -> 174 sRGB): the dimming now compresses luminance.
  - The water's own glow at 0.5 made a dirty puddle read darker: it is now x (1 - transmittance).
  - 12 mm stripes blurred to grey: now 28 mm.
- **Tooling learned:**
  - `EditorApplication.isPaused = true; EditorApplication.Step()` runs real frames synchronously while unfocused: dt 0.02
    each, UI Toolkit transitions included. `Screen.width` reads 640 while paused. Unpause before stop.
  - Queued Input System key events never reach the game from an eval, even with focus rules and background behaviour
    relaxed, paused or in manual mode.
  - Editing an `.hlsl` on disk and then importing only its shaders logs "Build asset version error". A Refresh clears it.
- **Open:**
  - the literal Z / X press (the logic behind it is verified) and the sounds are for the user to try in a focused Editor;
  - the floor outside the sim domain can dim but never glow;
  - CarpetFoam still shows its existing "potentially uninitialized" warnings (`FieldAt` / `FAt`, untouched code).

## 2026-09-24 (night, latest) - AREA upgrade: up to x3, evenly from the first to the last level (verified in Play)

- **Request (RU):** the area should grow at most 3x, smoothly from the first to the last upgrade level.
- **Formula.** `EconomySettings.areaStep` (1.08 per level, x1.59 top) became `areaMax` = 3: area = areaMax^(L / maxLevel),
  x1.2009 per level, i.e. 1, 1.20, 1.44, 1.73, 2.08, 2.50, 3.00. `Economy.AreaStep` derives the step. The asset was
  updated.
- **Shop display.** Rows now read as multiples of the start ("x1.44 -> x1.73"; AREA max x3.00, POWER max x2.31) instead
  of "+44%".
- **Verified:**
  - Ladder and sizes at level 6: squeegee 660 mm, mop 960, wet vac 480, scraper 360; dryer radius 234, foam 121.
  - Squeegee x3, straight pass over soaked foamy pile: h 3.89 -> 0, F 2.06 -> 0.
  - Squeegee x3 on a 350 mm arc: middle and outer end 0.
  - Near the pivot h 0.78 mm stays, versus 1.70 for the factory blade on the same relative turn. This is old plow
    behaviour that a long blade triggers more often (SQUEEGEE.md 11).
  - Head captures `Screenshots/area6_tool{0,5,6,9}.png`.
- **Incident.** The user had played in between (carpet 1 paid $390 with a combo, then +$100k from the tool). They had
  bought every upgrade: $370 + $11,400 spent = $11,770 = the exact cost of all 20 tracks at level 6. My test reset
  (`SetLevel` to 0) wiped those purchases. From that ledger they were restored to level 6 on every track; money, counters
  and bestSeconds were untouched. Lesson: snapshot `carpet_save_editor.json` before tests that call `SetLevel` / `Grant`
  and restore the file afterwards.

## 2026-09-24 (night, later) - editor tool: test money (verified in Edit and Play)

- **Request (RU):** an Editor tool that adds lots of money for testing.
- **Tool.** `Editor/EconomyDebugWindow.cs`, menu CarpetCleaning > Economy:
  - Add $10k / $100k / $1M;
  - Test Money Window: live balance, +1k / +10k / +100k / +1M, custom amount Add / Set, Wallet to $0.
  - In Play it goes through `Economy.DebugGrant`; outside Play it edits `carpet_save_editor.json`. Money is clamped to
    [0, int.MaxValue].
- **Verified:**
  - Outside Play: the menu adds $100k; −5k works; Set int.MaxValue then +1k stays clamped; the file is correct.
  - In Play: +$1M via Economy; the window reads "(Play mode, live)"; the HUD wallet target updates on its next frame.
- **Review workflow** (2 lenses, a skeptic per finding, 5 agents): 1 confirmed and fixed. The HUD wallet roll in
  `EconomyHud` overflowed its float near int.MaxValue and showed "$-2,147,483,648"; it now uses double, verified by
  replay. 2 findings were refuted: a stale `Instance` after a hot reload (hardened anyway with `FindAnyObjectByType`), and
  the wallet not rolling while `PayoutSeek` freezes the HUD (by design).
- The editor save was put back to its previous $20.

## 2026-09-24 (night) - economy: pay per carpet, tool upgrades, pay-slip card with VFX + sounds (verified in Play, scripted + HUD captures)

- **Request (RU):**
  - A carpet pays: base + speed bonus + 100 % clean bonus + 100 % dry bonus + combo when both.
  - The payout must appear line by line, beautifully, with VFX.
  - Upgrades per tool: POWER and AREA.
- **User decisions:**
  - Shop only by button (U).
  - JSON save.
  - Levels 0..6.
  - Today's power sits at level 3 (scraper, foam, squeegee), 1 (dry vac), 2 (hose), 0 (brush, wet vac, dryer, floor tools).
  - AREA starts at today's size.
  - Pay follows the real starting dirt, so R re-rolls are worthless.
  - Dry bonus always at 100 %.
- **Design and all numbers:** `ECONOMY.md`. Sounds: `AUDIO.md` 10. HUD rules: `HUD.md` 6-7.
- **Code** (new, in separate files: a parallel session was editing CarpetSimulation / Handover / ToolAudio the same day):
  - `Scripts/Economy/*`, `Scripts/UI/*`, `UiAudio` / `UiSfxSynth` / `UiSoundBank`, `Editor/UiSoundBaker`, `FxSparkle.shader`.
  - Surgical edits:
    - `_ToolPower` in `DebrisCommon.hlsl`;
    - per-tool rate multipliers in `K_Stamp`, `K_Chemistry`, `K_PlowScatter`, `K_FInject`, `K_FChem`, plus scraper and floor residue clamps and the brush agitation cap;
    - `CarpetSimulation` (`UiPaused` / `IsPaused`, effective `_ToolSpacing` + `_ToolPower` per upload), `FloorSimulation`;
    - `ToolController` (`ToolTuning.Effective`, `UiModal`), `ToolModelView` (head scale), `CarpetHandover` (H/N gate);
    - `CarpetDebugApi.Reset` (Neutral tuning for scripted runs, `uiPaused` in SteamJson);
    - `HudArtBaker` (+ economy art), `CarpetSceneBuilder` (Economy object, settings, sparkle material / particles, HUD scripts), UXML / USS.
- **Measured before coding** (hint-following bot, `scratchpad/bot.sh`: whole-carpet raster with the suggested tool, 300 mm/s):
  - 100 % is reachable at factory tuning on `Reset(101, 1)`.
    - Clean passes 0.995 after ≈760 s, drops to 0.9947 while drying (redeposition), and one more rinse cycle gives Clean 0.9995 + Dry 1.0000.
    - ≈24 min in total for the naive bot.
  - Seed 29 (heaviest rolled carpet), level-0 tools: Won at 1012 s.
  - Starting DirtScore over 40 seeds: game rolls median 0.39, p90 0.73, max 1.05-1.37; scripted level 1 = 1.53-1.57.
  - Hence `dirtRef` 1.0 and par 45 + 555 V s.
- **Verified in Play:**
  - Compute imports 0 msgs, dispatched.
  - `FxSparkle` 0 msgs.
  - Recompiles clean; no new console errors.
  - Build Scene OK.
  - Slip maths: 11 cases as designed (ECONOMY.md 1).
  - Real flow: forced hand-over of seed 29 paid 240 (120 + 80 + 40), saved to disk; the card started; 84 glints on the flying rug; 0 after Away.
  - Shop open: `SimTime` frozen at 0.0200 across ticks; both flags clear on close.
  - Save: money 1017 + brush level 3 survive a Play restart; garbage JSON goes to `.bak` with a fresh start.
  - Upgrades in the sim:

    | Upgrade | Result |
    |---|---|
    | Hose AREA 0 → 6 | wet band ×1.30 |
    | Scraper AREA | crust per pass ×1.55 (expected 1.59) |
    | Dry vac POWER | grit per pass 34.7 → 54.7 % |
    | Dryer POWER, 0.5 s | −52 → −83 % pile moisture |
    | Squeegee POWER | pile left 0.34 / 0.30 (factory) / 0.264 |

  - UI particles at the combo peak: 125 alive, 0.43 ms CPU.
  - Sounds: analyzer numbers in AUDIO.md 10. The first sparkle recipe (modal pings) measured 22.5 dB tonal with 9 peaks and was replaced by noise glitter (6.9 dB).
- **New test tool.** `EconomyDebugApi.CaptureHud`: a UI Toolkit panel capture composited over a camera render. It works unfocused; these are the first HUD screenshots of the project.
  - Captures in `Assets/Screenshots/`: `econ_hud_idle`, `payout_t*` / `payout2_t*`, `flow_flyout`, `sparkle_world`, `shop_open`.
  - Visual fixes made from them:
    - the icons showed the missing-image glyph (the USS was imported before the PNGs): the baker now re-imports the USS;
    - the COMBO stamp covered the combo amount: it moved to the card corner, outside the clipping body;
    - the flash was too strong: 0.75 → 0.4;
    - the shop cards wrapped one per row (836 px = exact fit): now 850;
    - the sparkles were too faint: made bigger.
- **Gotchas:**
  - Re-importing the USS in Play rebuilds the UI tree and breaks the HUD scripts: restart Play.
  - `ParticleSystem.Simulate` applies lazily: flush with a second call.
  - Unity 6000.7 analyzer UAL0010 / 0013 warnings on statics, like the rest of the project.
  - Python on Windows reads the scratchpad only by its `C:/` path.
- **Open (the user's check):**
  - How it all sounds and feels live (the card in real time, coin / bell tonality, the glints in the 0.85 s flight window).
  - The balance numbers (asset).
  - Stretched handles on blade heads at AREA 6.
  - The test save was reset: the wallet starts at $0; use `EconomyDebugApi.Grant(n)` to try the shop.

## 2026-09-24 (night) - dry vacuum = a real Samsung recording (start / loop / coast-down), rattle untouched (logic verified in Play; not listened to)

- Request (RU): the user put `Audio/100210__debsound__vacuum-cleaner-samsung-easy-1300.wav` into the project for the vacuum
  ("tool 2" = hotkey 2 = dry vacuum) and asked to keep the high crumb rattle as it is. Design and numbers: `AUDIO.md` 9.
- **Licence:** Freesound 100210 by Debsound is **CC BY-NC 4.0** (checked on freesound.org): credit required, non-commercial only.
  - Fine for the prototype. A paid Steam release must replace it or get permission.
  - `SteamBuild` now warns (console + result line) while an NC-credited recording is in the bank; STEAM.md 10 has the note.
- The take has the whole cycle: spin-up (28 % speed at 0 s, 99 % by 1.5 s), running, switch-off at 20.40 s, 9 s coast-down.
  The impeller whine (5040 Hz = 9 x 560 Hz) is the speed gauge.
- `Tools/cut_machine_recording.py` (new, reusable):
  - `--scan`, then the cut: start 0-6.2 s, loop 6.2-17.608 s, stop 20.36 s-end, at 48 kHz stereo;
  - the loop tail is crossfaded into the audio before its head, so start -> loop is contiguous and the wrap is seamless;
  - speed curves are tracked on the whine by DP over a multi-window spectrogram;
  - one gain (-1.82 dB) matches the placeholder motor's loudness (-11.6 LUFS).
- `RecordedMachine` (new) + `MachineRecording` in `ToolSoundEntry` (`useRecording`, `recording`), wired into `ToolAudio`:
  - press = the recorded spin-up, with the loop `PlayScheduled` sample-exact at its end;
  - release / re-press enter the other clip where the motor's speed matches, crossfading 50 ms on `dspTime`;
  - a tool change lets the vacuum coast down;
  - no synthetic power-on/off in that mode.
- `ToolSoundBaker.ImportMachineRecording(ToolId, json)` imports the clips (PCM, stereo, preloaded) and fills the bank entry
  (curves, credit). `Report()` measures the recording; its seam check now compares frame to frame for stereo.
- `CarpetDebugApi.RecordedMachineScenario` runs the transitions on a virtual clock (`RecordedMachine.ClockOverride`, a test hook).
- Fix: `LoopLayer.SetClip(null)` set `time` on an empty source ("Attempting to set `time`... not a clip" warning).
- Verified:
  - `recompile` 0 errors; switching tools in Play: 0 warnings;
  - the loop wrap and the start -> loop stitch show no click (6 kHz+ energy in 1 ms frames at the joint +1.8 / +1.6 dB against
    +3.3 / +3.1 dB peaks nearby);
  - the scenario at 1 s / 1 s / 7.5 s hits every expected offset: release at speed 0.970 -> stop @0.09 s, re-press at 0.558 ->
    start @0.15 s, stitch at +6.092 s, Running, full coast 9.26 s;
  - in the real engine (while the editor had focus) the stop voice's `timeSamples` was 122257 against the predicted 122256;
  - crumb probe 42/42 captured + delivered.
- Not verified: how it sounds. The editor was unfocused, so no frames and no audio (listening needs a focused editor).
- Seen, not mine: 1-2 of the 42 probe crumbs now vanish up to 50 ms before prediction (0.00 ms on 09-21), on the particle side.
  The parallel dirt-level session was told too.
- Coordination: a parallel session's half-finished `CarpetSimulation` edit broke compilation mid-session. I waited for its
  go-ahead before recompiling and entering Play.

## 2026-09-24 (night) - dirt v2: in-between states (dusty / grimy / print gone without crust), Reset = a new carpet (NOT verified in Play)

- Feedback (RU) on v1 (entry below):
  - R / Reset must create a new carpet with a new mask;
  - a carpet was either clean or had dried-crust chunks. The user wants to see the in-between states: just dusty with
    the real print showing through, so dirty the print is gone but no crust, and so on.
- Cause:
  - v1 thresholded one mask and switched stains, mud and crust on within a few cm of each other;
  - crust came with any heavy soil;
  - there was no dust at all.
- Design (`DIRT_LEVEL.md`, rewritten):
  - a per-carpet `DirtProfile` rolled from the seed: level, spread 0.45..1.3, dust, mud, crust (4 in 10 carpets),
    spill;
  - K_Init spends a broad mottled soil field S on four materials:
    - dust in the grit channel, at light soil;
    - grime `S^2 (0.8 + 0.2 S)`;
    - crust only in the carpet's crust patches;
    - spill discs as independent events;
  - dead zones cut invisible dust and grime; the trace rule threshold is now `2e-4`;
  - `CarpetFibers`: a new per-strand grey dust film (`_DustVeil` 0.45, `_DustColor`), and crumb specks now only from
    grit 0.2;
  - the hint asks for the vacuum on starting dust.
- Tuned offline:
  - new `Tools/dirt_preview.py` is a numpy port of `RollDirt`, the HLSL noise, K_Init and the fibre albedo, run on the
    real rug photos;
  - iterations: v1 flat beige fog -> dust fading into grime on heavy soil -> +/-30 % mottling at 50 mm (a stretched,
    too-contrasty version looked like camouflage) -> visibility dead zones;
  - seeds 1..20 give the whole range: spotless 5 / 17 / 18, dust clouds 12 / 11 / 9, print gone without crust 13 / 20,
    crust over grime 19 / 6 / 1 (table in `DIRT_LEVEL.md` 8);
  - the fully soiled reference renders the old crusted carpet.
- Reset:
  - `CarpetHandover.ResetToNewCarpet()` = next picture, colour and `seed + seedStep`, full reset;
  - R (via `ToolController`, which hands it to the handover component on the same object) and the HUD Reset button use it;
  - `CarpetDebugApi.NewCarpetNow()` does the same; `Reset(seed)` still restores the fully soiled carpet on the same
    picture.
- Other: `SimSettings` "Dirt of a new carpet" = `DIRT_MASK_MM` 500, `DIRT_MASK_WARP` 1.5, `DIRT_GRAIN` 0.3 /
  `DIRT_GRAIN_MM` 50, `DIRT_CRUST_MM` 220, `DIRT_DUST` 0.35 (`DIRT_MASK_SOFT` removed; asset synced). The builder
  writes `_DustColor` / `_DustVeil`. `DirtJson()` added.
- Checked (no Play):
  - `CarpetSim.compute`: 0 messages and all 15 kernels `IsSupported`;
  - `CarpetFibers.shader`: 0 messages, the new properties are found;
  - C# dry run of the 6 changed scripts is clean;
  - one `recompile` (coordinated with the parallel audio session): completed, no errors;
  - C# `RollDirt` equals the preview (seed 1: 0.49 / 0.96 / 0.72 / 0.87 / 0.21 / 0.35), and a forced level 1 gives
    FullySoiled.
- Gotcha: `CarpetSimulation` already has `RenderTexture Dirt`. The first name for the profile property (`Dirt`) broke
  compilation for the other session in between; it is now `CarpetDirt`.
- Open (the user's check): the look in the real render (dust grain under the 3x sun, grime vs shade on dark rugs, the
  crust edge); ~40 % of rolls are heavy (uniform level).

## 2026-09-24 (late) - every new carpet: random dirt level + noise soil mask, spotless .. fully soiled (NOT verified in Play, per request)

- Request (RU): give each new carpet a random dirt level and a random noise dirt mask, from absolutely clean to
  absolutely dirty. The user asked to skip the Play-mode check and will test it themselves. Design: `DIRT_LEVEL.md`.
- `CarpetSim.compute` `K_Init`:
  - new `SoilMask(pMm)`: domain-warped fbm stretched to ~uniform, with a ramp that slides with the level;
  - crust, mud and stains are multiplied by gates of the mask; nothing below D 0.1;
  - the crust gate is jittered per Voronoi plate;
  - each splatter disc is gated by the mask at its centre, and the disc loop is now `[loop]`;
  - start flattening is `min(0.55, 0.6 (1 - e^-10 crust))`.
  - Level 1 reproduces the old carpet.
- `SimCommon.hlsl`: `_DirtMask` (level, offset.xy) and `_DIRT_MASK_MM/SOFT/WARP`.
- `SimSettings` gained the header "Dirt mask (new carpet)" with 450 / 0.3 / 1.5 (C# and asset in sync).
- `CarpetSimulation`:
  - `DirtLevel`, `DirtLevelForSeed` (lowbias32), hashed mask offset;
  - `ResetCarpet(seed, notify, dirtLevel)` (negative = the seed's roll);
  - `DispatchInit`;
  - trace rule: an init with `DirtScore < 1e-3` is re-run at level 0, so no invisible trace holds Clean at 0 %;
  - `randomFirstSeed` (default on) rolls `Random.Range(1, 1000)` in `Start`.
- The hand-over keeps `seed + 1`: the hash makes every new carpet a fresh roll.
- `CarpetDebugApi`:
  - `Reset(seed, dirtLevel = 1)`, so old recipes still get the fully soiled carpet;
  - `StatsJson` / `HandoverJson` report `seed` and `dirtLevel`.
- Checked (no Play):
  - `CarpetSim.compute` import: 0 messages, and `IsSupported` true for all 15 kernels on D3D12;
  - C# dry run: only the known CS0618/CS0436 warnings;
  - `recompile` completed with no errors, and no new console errors;
  - `SimSettings.asset` loads 450 / 0.3 / 1.5;
  - `DirtLevelForSeed` over seeds 1..999: 93/110/100/100/92/114/93/98/102/97 per decile.
  - Seeds 1..12 roll 0.49 0.90 0.50 0.46 0.04 0.99 0.21 0.71 0.18 0.50 0.30 0.45.
- Open (the user's check):
  - the look of partial carpets: edge softness, patch size, the crust boundary, a spotless rug under the 3x sun;
  - whether the old recipes at `Reset(1)` still match their numbers, which needs a dispatch (Play) to confirm.

## 2026-09-24 (evening) - brush v2 model: still grey top, blue bristles spin under it; spin doubled to 350 rpm

- Request (RU):
  - the ring-only brush looked poor, so the user asked for a Tripo prompt; they generated `Tool_Brush_v2.glb` themselves;
  - the top must stand still and only the brush (the bristles) spin;
  - then, after looking at it: fine, stop checking, just spin twice as fast.
- Model (`TOOL_MODELS.md` 2, 7.1):
  - v2 came back part-segmented: 71 meshes, one material each, upright.
  - New `ToolModelPostprocess.SplitByParts`: a part spins when its area-weighted albedo passes the new shared
    `IsBristle` chroma rule (max - min > 0.12, max > 0.25).
    - 42 blue parts (2313 tri) spin; 29 grey parts (body, clutch plate, latches, bolts; 2246 tri) stay still.
    - Nothing is cut or saved: the parts move into a container under `Spin`.
    - The stale single-mesh cut assets and the empty `Prefabs/Tools/Meshes` folder were deleted.
  - Other changes:
    - `UpsideDown` is now keyed by source file (only the first take is flipped);
    - `SpinOnly` is empty;
    - the first-take (single-mesh) path uses the same `IsBristle` rule, which reproduced its 113 / 32 pieces exactly.
  - Prefab root fileID kept, so `ToolModelSet` still resolves.
- `ToolModelView`: smear copies are nested `SmearShrink` 0.4 % radially per copy, so a continuous skirt no longer
  z-fights its turned copies.
- Checked:
  - edit-mode preview: turning `Spin` 30 deg changed 0 pixels of the top and 16k of the skirt;
  - Play, 0.3 m close-up at full spin, consecutive frames: only the skirt band changes (up to 8k px per 50 rows); the
    top changes 9-21 px per band, which is the pile around it;
  - the game-camera crop reads as a light disc over a blue skirt.
  - Screenshots: `brush_v2_game_idle.png` and `brush_v2_close_{idle,spin_a,spin_b}.png`.
- Speed doubled (user): `ToolModelSet` brush `spinRpm` and `SimSettings.BRUSH_RPM` 175 -> 350 (assets and C# defaults,
  `CarpetSceneBuilder.DefaultSpinRpm`); recompiled.
  - At 60 fps a frame now turns 35 deg; the 4 smear copies sit 7 deg apart.
  - The foam swirl reaches ~460 mm/s at r 31.5 mm, and the ±600 mm/s foam clamp caps it under a fast stroke.
  - Not re-checked, per the user.

## 2026-09-24 (later) - rotary brush: bristle ring only, pad swirls foam and water, motor hum, sinks into the pile (NOT run in Play mode, per request)

- Request (RU):
  - drop the upper part of the brush mesh and keep only the spinning part;
  - do all three follow-ups offered before (rotary swirl in the sim, a motor sound, a deeper sit in the pile);
  - do not check in Play mode: the user will look.
- Model (`TOOL_MODELS.md` 7):
  - `ToolModelPostprocess.SpinOnly` keeps only the 113 warm pieces (3381 tri, tufts whole, no plate ceiling needed);
  - the imported node and `Tool_Brush_Body.asset` are deleted; the ring is lowered 0.35 mm onto y = 0;
  - the prefab is `Tool_Brush` -> `Spin` -> `Tool_Brush_Spin`; root fileID kept; the other six prefabs are
    byte-identical.
- Pile sink: `ToolModelSet.Entry.pileSinkM` (brush 0.008, seeded by the builder like `spinRpm`).
  - Applied over the carpet only: eased by the cursor height between the tile (0.009) and carpet (0.03) hover.
  - Tips at 10 mm above the backing, half way into the pile.
- Simulation (`ROTARY_BRUSH.md`):
  - `BrushPadVelocity` (DebrisCommon.hlsl), clockwise from above like the head.
  - Carpet `K_Stamp` brush: agitation, pile bend, flick and abrasion follow the contact velocity pad + stroke (so it
    scrubs held still). `vTool` adds `BRUSH_SWIRL` x pad and `BRUSH_FLING` x |pad| outward, with weight
    `sat(|vTool| / SPEED_REF)`.
  - `K_Fiber`: brush set direction and mech from the contact velocity, so wet pile keeps arcs.
  - Floor `K_FInject` brush: same agitation and flow.
  - `SimSettings` `BRUSH_RPM` 175 / `BRUSH_SWIRL` 0.4 / `BRUSH_FLING` 0.1 (asset saved in sync); `FloorSimulation`
    copies them from the carpet's settings.
  - No new UAVs.
- Sound (`AUDIO.md` 2):
  - `SfxRecipe.RotaryMotor` = `MotorLoop(340, 100, 520, 0.5, 1.6)`.
  - Brush entry: powered, 0.40, spin 4 / 1.2 (= the visual), pitch min 0.8, idle cut 700 Hz, contact idle 0.60,
    55 Hz power-on thud.
  - New `ToolSoundBaker.BakeTool(ToolId)` and `ToolSoundBank.ResetVoicingKeepClips(ToolId)`. Only the brush's four
    WAVs and its bank entry changed (diffed).
- Checked (edit mode only):
  - C# recompile clean; `CarpetSim` / `FloorSim` / `Debris.compute` import 0 msgs;
  - `IsSupported` true for K_Stamp, K_Fiber, K_Flux, K_Integrate, K_FInject, K_FFlux, K_FIntegrate;
  - no console errors; Build Scene OK;
  - `tool_brush_motor.wav`: tonal 6.7 dB @ 334 Hz (vacuums 8.1-10.8), am 5.8 dB, st90 11.8, -13.2 dBFS, seam 0.52.
- Not checked (Play): whether the foam swirl reads, the flung-foam ring strength, the arcs on wet pile, the held-still
  scrub balance, the hum level. Debris is still flicked along the stroke, not thrown off the pad.

## 2026-09-24 - brush (tool 5) turned the right way up; its bristles spin while it works

- Request (RU): the brush model was upside down; its lower part should spin fast, like a rotary floor machine in a
  rug-cleaning video. User chose: only the bristles spin (the plate stays still), and only while cleaning (spin up
  on press, coast down on release).
- Changed (full notes: `TOOL_MODELS.md` 3 and 7):
  - `Editor/ToolModelPostprocess.cs`:
    - `UpsideDown` flips the brush 180 deg about X before normalising;
    - `SpinSplit` cuts it into a body and a spinning part by warm-coloured connected pieces (113 tufts / 32 grey
      pieces, 0 mixed), trimmed under a per-radius plate ceiling;
    - `Normalize(ToolId)` rebuilds one head;
    - meshes are saved in `Prefabs/Tools/Meshes/`.
  - `ToolModelSet`: `Entry.spinRpm` (brush 175) + set-wide `spinUpHz` 4, `spinDownHz` 1.2, `spinReleaseGraceS` 0.12,
    `smearMaxStepDeg` 4.5, `smearMaxCopies` 4.
  - `ToolModelView`:
    - `tools` ref (wired by the builder);
    - spin state driven by `IsStroking` / `StampCount`;
    - world-locked `Spin` pivot;
    - anti-strobe smear copies;
    - `StepFrame(dt)` (called by `StepAll`) and `DebugPin`.
  - `CarpetDebugApi`: `PinToolHead`, `UnpinToolHead`, `ToolHeadJson`, `Heads`.
  - `CarpetSceneBuilder`: `mv.tools = tc`, preserves or seeds `spinRpm`.
- Verified in Play mode (scripted 1/60 s frames; recompile clean; 0 new console errors; only `Tool_Brush.prefab` changed,
  the other six prefabs are byte-identical; root fileID kept, so `ToolModelSet` still resolves; all 7 heads `Has()`):
  - spin-up 0.91 @ 0.6 s, full step 17.5 deg/frame (= 175 rpm) with 3 smear copies;
  - coast-down 0.35 @ 1 s, 0.10 @ 2 s;
  - a 36-frame `StrokeNow` alone spins it to 0.91; a tool switch resets it to 0;
  - pixel test (> 40/255 changed), idle -> full spin and frame -> frame:
    - plate far half: 0 / 0 at the game camera, 0.14 % / 0 % at a 0.3 m close-up;
    - bristle skirt: 27.5 % / 23.9 % at the game camera, 23.5 % / 15.9 % at the close-up.
  - Screenshots `brush_game_*.png`, `brush_close_*.png`.
- Iterations:
  1. The tuft tops were flush with the plate, so spinning slid beige specks round a still plate. A flat cut at the
     plate underside (25.6 mm) was not enough, because the dish is lower at the ring's edges. The cut now follows a
     per-radius ceiling (min over all angles, 0.5 mm grid).
  2. A dark cover ring under the plate's holes was built and then removed: the ceiling alone is enough.
  3. Gotcha: `EditorUtility.CopySerialized` over an existing Mesh asset left its GPU copy stale. Three rebuilds rendered
     pixel-identical until `UploadMeshData` was forced. `SaveMesh` now writes through the Mesh API.
  4. Gotcha: `capture_game_view` renders at 1280x720 while `Camera.main.pixelWidth` is 2560 (the Game view size).
     Scale `WorldToScreenPoint` results before cropping.
- Open:
  - the user to judge the speed and look live; `spinRpm` and the smear knobs can be edited in `ToolModelSet.asset` in Play;
  - the brush's simulation effect is unchanged (no rotary swirl of the foam, as in the reference video);
  - no motor sound for the brush yet.

## 2026-09-24 - squeegee (tool 6) rewritten as a plow: nothing behind the blade at any speed, pile left damp

- Request (RU): the squeegee (hotkey 6) left ribbed foam; only at some drag speeds did it work almost right.
  Wanted: no foam and no water behind it at all, carpet left damp. User chose "push a roll ahead" over "just remove it".
- Root cause:
  - The squeegee's only transport was the `_ToolFlow` velocity push, which exists only on frames where a stamp lands (every
    15 mm). Hold stamps between them carry speed 0, so slow drags pushed 1 frame in N: ribs at 15 mm.
  - Fast drags hit the ±600 mm/s foam clamp and the CFL limit.
- Changed (full design: `SQUEEGEE.md`):
  - `K_Stamp` tool 5: wrings / presses the pile to `SQ_W_DAMP` 0.3; no push, no agitation.
  - New kernels:
    - `K_PlowScatter` moves the surface water + foam of every cell the blade swept (hull of a sliding / rotating blade) into a
      packed roll ahead, with fixed-point `InterlockedAdd`, exactly conservative;
    - `K_PlowApply` deposits it, soaking into the pile first (`SQ_W_ROLL`);
    - `K_PlowDrain` pours the off-carpet ghost-ring reservoir onto the floor via `_EdgeOut` at a capped rate;
    - `K_PlowClear` zeroes the accumulators.
  - `ToolController.EnqueuePlow` (sweep per segment, continuity-gated axis, `SQ_SUB_MM` split).
  - `CarpetSimulation` queue, dispatch, reset and `CarpetAway` skip.
  - 9 `SQ_*` settings (C# and `.asset` in sync).
  - `CarpetDebugApi.SurfaceProbeJson` / `PlowSpillJson`.
- Iterations inside the session:
  - A free 4 mm roll ran back into the cleaned strip when the blade lifted (46 % of the strip wet after 2.5 s). The pile soak
    brought it to 20 %; the drain-to-floor numbers stayed exact.
  - Counting the soaked pile as roll room made the whole wet carpet ahead "roll" (stripes to the edge). Rejected.
  - A 0.5 mm film made 1 mm puddles denser than packed, and they were shoved 300 mm ahead. Room per cell is now capped at 1 and
    `SQ_H_PACK` = 1.5 mm.
- Verified in Play mode (scripted `StepAll` frames, 0 console errors):
  - compute import 0 msgs; dry-run and real recompile clean.
  - 720 mm stroke through hose + foam + brush (band fully covered), behind the blade, foam / water cells: 2.7 % / 11 % at
    150 mm/s, 1.6 % / 5.9 % at 600, 0.1 % / 0 at 2400; w 0.29 everywhere. Before the stroke: 100 % / 100 %, h 1.27 mm, F 1.26.
    The slow-speed residue is seepage from the unswept wet carpet during the 4.8 s stroke; the image of the strip is clean.
  - Whole carpet foamed + hosed + brushed, then six overlapping strokes off the top edge: foam 0 %, water 0 %, max h 0.016 mm,
    max F 0.012, w 0.23 mean.
  - Conservation with evaporation off: carpet + floor + ring water 1,094,385 -> 1,094,377 mm^3; dirt 328,938.2 -> 328,937.4
    (carpet + floor); 154 mL pushed onto the floor.
  - Edge cases (diagonal, reversal, 2 s hold, circle, 30 fps flick off the edge and onto the floor, CarpetAway): no NaN, ring drained.
  - Cost: 120 frames 230 ms with the squeegee vs 239 ms idle.
  - Regression chain scraper -> dry vac -> hose/foam/brush -> squeegee -> wet vac -> dryer runs; after the squeegee, water
    0.246 -> 0.006, foam 0.319 -> 0.009.
  - Capture: `Screenshots/sq_strokes.png` (two clean strips, a roll, foam pushed onto the floor at the top edge).
- Open:
  - water from unswept wet carpet seeps into the strip over seconds (pressed pile is ~1 mm lower; global pile model, left as is);
  - a roll dropped mid-carpet relaxes back a little;
  - floor squeegee and debris push still use the per-stamp velocity;
  - the user has not judged it live yet.

## 2026-09-23 - carpet pictures replaced by five real rugs (public domain / CC0)

- Request: replace the carpet pictures with 5 copyright-free photos of real carpets, found online. Details and credits: `HANDOVER.md` 5.
- Sources:
  - Met Open Access: API search "carpet", 950 hits, public-domain records filtered;
  - Art Institute of Chicago API (`is_public_domain`).
- Chosen: `rug_anatolian_star`, `rug_transylvanian_medallion`, `rug_european_green`, `rug_salor_turkmen` (Met) and `rug_finnish_rya` (AIC). Each is 1024x1280 JPEG, 0.4-0.8 MB.
- Processing (PIL, per rug): cut out from the backdrop, fit a line to each edge, warp the quad to a rectangle, inset away fringe/backdrop, resample to 4:5 (stretch <= 4.3 %).
- Removed: `spongebob.png`, `cat_meme.jpg` (to the Windows recycle bin via `AssetDatabase.MoveAssetToTrash`).
- Changed:
  - Pictures now fill the whole carpet instead of the old 1000x1300 mm print window: `CarpetHandover.PictureFit` (public static, cover fit), shared by the builder.
  - `CarpetFibers.shader` lost the 3 % print feather; `_PicOffset`/`_PicScale` defaults are 0/1.
  - `CarpetSceneBuilder.PicPath` = the Transylvanian medallion.
  - New `LoadPicture` forces `npotScale = None` before the builder reads the first rug's size.
- Verified in the Editor:
  - shader import 0 msgs; recompile 0 errors;
  - Build Scene: `CarpetHandover.pictures` = 5, all 1024x1280, importer npot None / sRGB / mips; material offset (0,0) scale (1,1).
- Verified in Play mode (dirt and crust RTs cleared in a test-only eval to see the print):
  - forced hand-over x5: medallion -> anatolian_star -> finnish_rya -> salor_turkmen -> european_green -> medallion, i.e. all five once before any repeat;
  - every rug fills the carpet edge to edge under the pile, no blown highlights (white motifs included), the rya's figures upright;
  - `Screenshots/rugs_1..6.png`, dirty start `rugs_0_dirty.png`.
- Real cleaning pass: scraper + dry vac + hose + brush + wet vac zigzags on the left half give `clean` 0.10, and the red print starts to show (`rugs_cleaning.png`). No new console errors.
- Open: none for the pictures. More rugs: drop a 4:5 image into `Textures/` and re-run Build Scene.

## 2026-09-23 - Steamworks integration (App ID 5304930): SteamService, Rich Presence, Steam builds, SteamPipe upload

- Request: integrate the Steam SDK for a later Steam release. Agreed scope: core Steamworks + build + upload + Rich Presence (no achievements, stats or cloud). Full doc: `STEAM.md`.
- Package: `com.rlabrecque.steamworks.net` 2025.164.1 (SDK 1.64) as a UPM git dependency. Its asmdef is autoReferenced, so Assembly-CSharp sees it without an asmdef of ours. `steam_appid.txt` = `5304930` (7 bytes, ASCII, no newline).
- New:
  - `Scripts/Steam/SteamConfig.cs`, `SteamService.cs` (-1000), `SteamRichPresence.cs` (220);
  - `Editor/SteamBuild.cs` (menu CarpetCleaning/Steam);
  - `Tools/Steam/`: `app_build_5304930.vdf`, `depot_build_5304931.vdf`, `upload.ps1` + `upload.bat`, `rich_presence_english/russian.vdf`.
- Changed:
  - `ToolController.Suspended` (overlay gate, separate from the hand-over's `InputEnabled`);
  - `CarpetHandover` ignores H/N while suspended;
  - `CarpetDebugApi.SteamJson()` / `SteamSimulateOverlay(bool)`;
  - `CarpetSceneBuilder.BuildSteam` (a `Steam` object with both components, wired to CarpetRoot).
  - Build Scene re-run, scene saved.
- Verified in the Editor with the Steam client running:
  - recompile has 0 errors, and the new files add no new warning kinds (CS0618 avoided with `FindAnyObjectByType`);
  - `[Steam] ready: app 5304930, user <persona>`, then `[Steam] shut down` on Play stop, no exceptions;
  - the next Play inits again.
- Overlay (via `SteamSimulateOverlay`, real frames ticked by CLI calls):
  - ON: `simPaused/toolsSuspended` true, `simTime` frozen at 11.2011 over 5 ticked frames;
  - OFF: `simTime` resumes 12.68 -> 21.27;
  - a pause set by someone else survives an ON/OFF cycle.
- Rich Presence:
  - after scraper + dry-vac zigzags, `#Status_Cleaning` / `8%`;
  - forced hand-over gives `#Status_BetweenCarpets`; take-new gives `#Status_Cleaning` / `0%`;
  - 5 pushes in the whole session (throttle works).
  - Reading our own presence back from Steam: `clean=0%`, `steam_display` empty. That is expected until the loc files are uploaded in Steamworks.
- Without Steam (`initInEditor=false`): `ready:false`, error "disabled in the Editor", 0 pushes, the game and the overlay path still work.
- Builds (Mono, StandaloneWindows64):
  - `BuildRelease`: `OK Builds/Steam/content/windows: 208 files, 483.3 MB, 86 s`.
    `steam_api64.dll` (317 KB) is in `CarpetCleaning_Data/Plugins/x86_64/`. There is no `steam_appid.txt` and no DoNotShip folder in the content; `CarpetCleaning_BackUpThisFolder_ButDontShipItWithYourGame` moved to `Builds/Steam/symbols/0.1.0/`.
  - `BuildLocalTest`: `OK Builds/Steam/local: 209 files, 9 s, steam_appid.txt=5304930`.
    Run windowed for 20 s with `-logFile`: `[Steam] ready: app 5304930, user <persona>`, no exceptions.
  - Not tested: `RestartAppIfNecessary` on the release exe, which needs the game installed through Steam.
- Size finding (not Steam; left alone): textures are 352 MB of the 484 MB. Each of the 7 Tripo tool `.glb` (`Models/Tools/Tool_*.glb`) ships about 48 MB of textures. Compressing or downsizing them is the obvious win before release.
- `upload.ps1` parses cleanly (PowerShell 5.1), is ASCII-only, and refuses to run without a release build (exit 1). The steamcmd URL answers 200.
- Pitfall found and fixed: a define-symbol loop.
  - The package's `RedistInstall` re-added `STEAMWORKS_NET` on an Editor tick, and the active build profile dropped it again on each Play stop.
  - Every Play session therefore recompiled mid-play, wiping the RTs and orphaning SteamService (seen twice: `service:false`, NRE flood).
  - Fix: `ProjectSettings/SteamworksNETSettings.json` `canManageDefineSymbols:false` and the define cleared. The next Play session had 0 compiles.
- Final Play session after the fix and both builds (reset, dry-vac stroke, ticked frames, stop): `ready:true`, 0 exceptions in Editor.log, 0 compiles.
  The CLI console buffer still lists the ~1900 NREs from 20:02Z, which is the pre-fix mid-Play recompile. `LogEntries.Clear` does not clear that buffer.
- Tooling lesson: `EditorApplication.delayCall` never fired from CLI ticks (the UIToolkit rewalk sat in the queue too). Player builds go through `unity command menu --path ... --timeout 1800`.
- Open (user, partner site; checklist in `STEAM.md` 8):
  - confirm depot 5304931;
  - set launch option `CarpetCleaning.exe`;
  - upload the two Rich Presence loc files, then publish;
  - run `upload.bat -Account <builder> -Preview`, then the real upload, then set the build live on a branch;
  - test the real overlay on a Steam-installed build.
- Before public release: `companyName` DefaultCompany, the unity-mcp runtime dll in the player, cat_meme attribution, placeholder audio.

## 2026-09-21 (late 6) - wet vac leaves the pile damp, the dryer dries and straightens it; dry pile is always upright

- User correction: this replaces "dries pressed".
  - The wet vac takes all the foam and free water but cannot dry the carpet: the pile stays damp and dark.
  - The dryer finishes the drying and straightens the fibres.
  - A fully dry fibre at rest must always be upright.
- `K_Stamp` wet vac:
  - `w` stops at `WETVAC_W_MIN` 0.25;
  - `WETVAC_FLUSH` 4/s keeps extracting slurry/soap from the damp pile (otherwise slurry would be trapped and the HUD would loop on "wet vac").
- `K_Fiber`: `SET_DRY_RELAX` 4/s gated by wetness (SET_W_LO 0.1 -> 0.02) releases the set, so dry pile is upright.
  `SET_FLUFF_DRYER` was removed: the dryer only dries, and drying straightens.
- Verified in Play (FIBER_SET.md table):
  - after the wet vac: water 1.23 -> 0.011, foam 0.21 -> 0.0004, pile w 0.25, Wetness 0.25 (hint -> dryer), stripes held at 0.57;
  - dryer: the set holds down to w ~0.1, then falls 0.52 -> 0.03 over 3 passes; fully dry: |b| ~0;
  - colour matches a fresh carpet.
  - Captures `damp_after_wetvac.png`, `dried_upright2.png`.
  - Compute 0 msgs, recompile clean, 0 console errors. (The Editor had restarted mid-session; Play was re-entered.)

## 2026-09-21 (late 5) - pressed pile turned milky white: now darker, like real matted pile

- User screenshot: pressed/laid fibres read too white. The user expects them slightly darker, and that is the realistic look.
- Reproduced on a clean carpet with a scripted scene (dirt blitted to 0, hose, 2 squeegee + 2 brush stripes): `sheen_before.png`.
- Cause 1, `CarpetFibers.shader`: the bend tilted the normal by 1.4·bend, nearly horizontal on laid pile, so the wet
  rim and spec blew out. The velvet term also added up to +36 %.
  - Fix: tilt 0.45. Rim, spec and aniso are scaled down by `laid`. The albedo goes deeper (`base^1.15`).
  - Velvet is now darkening only, 12-28 % depending on the nap direction.
- Cause 2, `PC_RPAsset` `m_OpaqueDownsampling` 1 -> 0: the CarpetWater film (the pressed stripes collect 0.03-0.3 mm) composited a
  half-res blurred opaque copy, i.e. a grey veil. Found by hiding WaterSurface: the veil vanished.
- Also fixed: banding in the set across squeegee stripes. The capture is now swept over the real `spacingMm` (`_ToolSpacing`),
  SET_CONTACT_S 0.08 -> 0.25 per full pass, SET_FLUFF_VAC 4 -> 10, SET_FLUFF_BRUSH 3 -> 4.
- Verified: compute + shader 0 msgs, recompile clean, Play re-run (numbers in FIBER_SET.md). After: `sheen_after4.png`, map `fiberset_map3.png`.
  The opaque-copy cost was not measured.

## 2026-09-21 (late 4) - wet pile stays pressed (fiber set)

- User request: wet fibres a tool pressed should stay bent. Decision (asked): they DRY pressed, until a dry vacuum,
  dry brushing or the dryer on already-dry pile lifts them. Design and numbers: `FIBER_SET.md`.
- New `_FiberSet` RT (bSet.xy, fSet), written only by `K_Fiber`, which now loops `_Stamps` itself (SRV, 5 UAVs).
  `K_Stamp` is unchanged (8/8). The spring rests at bSet, and f relaxes to at least fSet. `K_ClearFiberSet` runs in ResetCarpet.
  Seven `SET_*` fields in SimSettings (+ .asset synced). New `CarpetDebugApi.FiberProbeJson` and the `fiberset` debug view.
- Iterations, all measured in Play:
  - A per-frame capture only reached bSet 0.12: the squeegee blade covers a cell for 1-2 frames. Switched to a per-stamp
    capture toward the tool's own target (`SET_CONTACT_S`): bSet 0.50 of the 0.6 max.
  - Dryer fluff gated by (1-wetSet) erased the set while still wet (0.50 -> 0.36 at w 0.23). Now gated by dryGate
    (wetness < 0.02-0.05) and cut to 0.08: the set is untouched through drying and loses ~15 % per extra pass on dry pile.
- Verified: compute 0 msgs, recompile clean, no runtime errors (the 2 console errors are CLI 5 s eval_file timeouts).
  - Dry squeegee: no set, as before.
  - Wet squeegee: |b| 0.498 held 5 s, neighbours 0.
  - Dried: the set holds 40 s idle.
  - Dry vacuum: 93 % removed in 1 pass.
  - Wet brush sets 0.79 and a reverse pass restyles it.
  - Captures `Assets/Screenshots/fiberset_wet_stripes.png`, `fiberset_map.png`.
- Open: the up/down nap sheen is subtle on screen (stripes read mostly via flattening). A stronger velvet term in CarpetFibers is a possible follow-up.

## 2026-09-21 (late 3) - rattle +50 % higher, band-limited, tuning knobs moved to the top

- User could not find `debrisPitch` (it sits inside each entry of the Entries array). Added bank-level `rattlePitch` /
  `rattleVolume` at the top of ToolSoundBank.asset, multiplying the per-entry values, applied live.
- Rattle +50 %: wall modes 9.3-12.3 kHz small / 6.3-8.8 kHz large, body 2.8-3.9 kHz. Measuring showed 35 % of the energy
  above 18 kHz (white burst + Chamberlin SVF near Nyquist): modes now fade out over 15-17 kHz and an RBJ biquad pair at
  14.5 kHz band-limits the clip -> 0.04 %. That junk had inflated the earlier centroid readings, so the pitch is now
  reported as the dominant resonance of the first strike: 6.2-13.6 kHz in 15/16 clips after halving the body band,
  which had been out-powering the wall mode in half the clips. Onset 1.5 ms on all 16.
- Found the scene still serialized `rattleVoices: 10` (the old default; changing a C# default does not touch a saved
  component) -> Build Scene re-run -> 14. Probe: 42/42, 0.00 ms, 40 scheduled, 0 late, console 0 errors.

## 2026-09-21 (late 2) - tube rattle raised again; ToolAudio made hot-reload safe

- User: "higher still, right direction". Rattle resonances x~1.6 more (x~2.8 over the first voicing): modes 6.2-8.2 kHz small /
  4.2-5.9 kHz large, body 1.85-2.6 kHz, low-pass 16 -> 6.5 kHz. Centroid dry 7.6-11.2 kHz, wet 6.5-12.3 kHz; onset 1.5 ms on all 16.
- Found 1948 console errors: the user was in Play mode listening while this session recompiled; the reload wiped the floor sim's
  RTs (known) and ToolAudio's runtime sources (NullReferenceException each frame in StepFrame). ToolAudio now rebuilds its
  sources in OnEnable when they are missing (reload runs OnEnable, not Awake) and replaces the orphaned child. Verified:
  recompile mid-Play -> 0 errors, AudioJson answers. Play restarted for a clean session; SuckTiming probe re-run: 42/42
  captures, 0.00 ms error, 39 scheduled, 0 late; carpet reset; console 0 errors.
- Lesson: check `editor_status` before recompiling - if Play is running the user may be in it.

## 2026-09-21 (late) - tube rattle pitched much higher (user request)

- All rattle resonances x~1.75 in `ProceduralSfx.TubeRattle` (wall modes 3.9-5.1 kHz small / 2.6-3.7 kHz large, body
  1.15-1.6 kHz, ricochet low-pass 11 -> 4 kHz); timing untouched. Measured (`analyze_sfx.py --hits`): centroid dry 3.8-6.4 ->
  5.9-8.8 kHz, wet 2.8-6.4 -> 5.0-9.3 kHz; onset still 1.5 ms on all 16 clips, so the capture sync is unchanged.
- New `ToolSoundEntry.debrisPitch` (0.5-2.5, default 1) multiplies rattle playback pitch: tune by ear in the bank, no rebake.
- Rebaked; bank holds 8 clips per vacuum, debrisPitch 1. Not judged by ear on this side.

## 2026-09-21 (evening) - vacuum tube rattle, driven by the crumbs actually sucked up (verified to 0.00 ms)

- Request: the vacuum should tick/rattle like a real one when grit goes up the tube, exactly when crumbs are really
  sucked in. Design + numbers: `AUDIO.md` 8.
- Truth source: `Debris.compute`, the chunk's switch to DEB_ST_SUCKED. At that instant the GPU now writes an event
  `{particle, size, tArrive, tool}` into `_SuckEvents` (new, K_DebSim 3 UAVs; new 1-thread `K_DebClearEvents`).
  tArrive is the frame the chunk will vanish into the nozzle, solved exactly from the SUCKED integrator
  (min(floor(t/dt)+1 over distance/size, ceil(0.4/dt)) * dt). `DebrisSystem` reads the buffer back async every
  step, stamps AudioSettings.dspTime at dispatch and raises `DebrisSucked`; `ToolAudio` schedules
  `PlayScheduled(captureDsp + tArrive + 8-22 ms tube flight)` on 14 voices (steals only voices >= 60 ms into
  their clip). Dry + wet vacuum; the mop's crumb pickup stays silent (no tube).
- Sound: `ProceduralSfx.TubeRattle`, 8 variations per vacuum baked to `Audio/Generated/*_debris_*.wav`; 2-5 strikes,
  first at 1.5 ms, ricochets quieter/duller; wall modes 1:1.47:2.09 Q 12 over an air-column body. Iterated by
  measurement (`Tools/analyze_sfx.py --hits`, new): centroid 7-12 kHz ("tss") -> 3.8-6.4 kHz after weighting the
  upper modes down; wet slosh first mixed before normalising matched the strikes (12-15 events), now normalised
  separately 24 dB under.
- Verified with the new permanent probe `CarpetDebugApi.SuckTimingBegin/Run/Report` (per-step particle readback):
  42 captures, GPU total 42 = received 42, 0 overflow, 0 duplicates; predicted vs actual vanish error 0.00 ms for
  all 42 (continuous-t first cut: 0-16.6 ms early); arrival 133-167 ms after capture, far above the 17-50 ms
  readback latency, rattleLate 0. Debris.compute 0 messages before and after dispatch; no console errors.
- Open: live readback latency only checkable in a focused editor (`AudioJson` rattleLate / lastLeadMs); fine grit
  removed per cell by K_Stamp is silent (K_Stamp is at the 8-UAV limit); the full bake now exceeds the CLI 5 s
  limit (completes anyway).

## 2026-09-21 (later) - tool audio v3: the "alien / cosmic" sound fixed (verified by measurement + Play)

- User verdict on v1: sounds "alien, cosmic". Built an objective analyser first (`Tools/analyze_sfx.py`, now in
  the project root outside Assets; numpy + scipy) with a white-noise / tone calibration, then measured v1:
  both vacuum motors 34-36 dB tonal peaks with 8 harmonics (a locked sine stack) plus 17-21 dB of 6.2 Hz
  vibrato; the dryer 24 dB of 2.3 Hz periodic "breathing"; scraper/hose/mop/wet vac 17-27 dB per-frame
  tonality from sine grains (white noise = 12.4); squeegee a 1448 Hz sine squeak. On top of that the driver
  carried speed and spin in PITCH (+-22 % slides, 0.45x -> 1x vacuum sweeps in 0.3 s) - the sci-fi whoosh.
- `ProceduralSfx` rewritten noise-first: filtered noise bursts with heavy-tailed amplitudes instead of sine
  grains, pink air/water beds, machine whine as a Q-16 band of noise, random `Drift` instead of LFOs, and
  physically-modelled bubbles (Minnaert + van den Doel damping/chirp) as the only tonal element.
- Driver: every loop source got a non-resonant `AudioLowPassFilter`; speed and spin now open the filter
  (log-linear, `contactCutoffSlowHz` / `motorCutoffIdleHz` per tool, `tileBrightness` 1.6), pitch ranges
  cut to 5-8 % (surfaces) and 0.70-0.72 minimum (motors), impeller spin-down slowed to 1.5-1.8 /s.
- Bugs found on the way: the Chamberlin SVF band has gain Q at its centre, so the first rewrite's Q-38
  whine was 38x hot and still drowned the roar (bands are now divided by Q; `WhineGain` 14 set by
  measurement -> whine 8-10 dB over the roar floor, st90 15.6-15.7); the SVF goes unstable for
  f > -q + sqrt(q^2+4) (a Q-0.35 band above ~4.5 kHz) - clamped, hiss moved to shelves; the mute key sat
  in `StepFrame`, which `StepAll` calls many times per frame (moved to `Update`).
- Result, shipped clips: all surface/air/water loops st90 11.8-13.1, tonal 2.6-3.4 dB (white-noise level);
  envelope autocorrelation shows no periodic wobble anywhere (0.13-0.26 at the minimum lag vs 0.64 at
  0.41 s for the v1 dryer). Play mode, scripted: vacuum spin-up opens the filter 3.7 -> 21.6 kHz with pitch
  0.86 -> 1.02; 40 frames after release spin 0.37, pitch 0.82, 2.9 kHz; brush slow 90 mm/s = vol 0.11 /
  6 kHz, fast = vol 0.59 / open. No new console errors.
- Tooling: baker renders with `Parallel.For` (25 clips in ~350 ms, the bake now fits the CLI 5 s limit),
  `WritePreview(dir)` for fast edit -> listen -> analyse loops, menu `Reset Voicing (Keep Clips)` pushes C#
  defaults into the .asset without losing assigned recordings (used this session).
- Open: still needs the user's ears in a focused editor; synthesis cannot give a vacuum a specific
  character - real recordings (AUDIO.md 5) remain the upgrade, and the analyser will vet them.

## 2026-09-21 - tool audio (ToolAudio + procedural placeholder bank, verified in Play mode)

- Request: take carpet-cleaning sounds off YouTube for the tools. Declined the source (someone else's
  copyrighted audio, and it would follow the project into a release) and built the system instead, with
  licence-free synthesised placeholders and clip slots for real recordings. Design: `AUDIO.md`.
- New: `Scripts/Audio/ToolAudio.cs` (driver, `[DefaultExecutionOrder(120)]`), `ToolSoundBank.cs`
  (`ToolSoundEntry` per tool + `SfxRecipe` + `ToolSfx`), `ProceduralSfx.cs` (noise/oscillator/SVF synth),
  `Editor/ToolSoundBaker.cs` (WAV bake + measurement, menu `CarpetCleaning/Audio/*`),
  `Settings/ToolSoundBank.asset`, 25 clips in `Audio/Generated/`.
- Changed: `ToolController` exposes `StrokeSpeedMmPerSec`, `StrokePressure`, `StampCount` (the reset of the
  first two is now guarded by the stamp frame, because `DebugLiveRunner` stamps from its own Update with
  `InputEnabled` off and intra-frame component order is undefined); `CarpetDebugApi` gained `Audio` +
  `AudioJson()` and `StepAll` now steps the audio with the sims; `CarpetSceneBuilder` builds the bank, adds
  `ToolAudio` and ensures an `AudioListener` even when the camera already existed (it did not before).
- Two layers per tool: machine (spins up on the first stamp, down `strokeReleaseGrace` 0.12 s after the last,
  pitch sags to `spinPitchMin`) and surface (volume/pitch follow stroke speed over `contactRefSpeedMm`).
  Each layer owns two AudioSources and crossfades, because assigning a clip to a playing source clicks.
- Verified in Play (baked bank, `StepAll`-driven): dry vacuum idle all-zero -> 20 stamps `spin 0.777,
  motorVol 0.245, contactVol 0.262` -> 60 stamps `spin 0.989, motorPitch 1.054, contactVol 0.293` -> +20
  frames after release `machineOn false, spin 0.497, motorPitch 0.729` -> +140 frames all zero. Sources
  inspected directly: `MotorA`/`ContactA` playing, baked clips `Loaded`, `audioMasterMute` false,
  `m_DisableAudio 0`. `generated:0, pending:false` = nothing synthesised at runtime. No console errors.
- Clip measurements (`ToolSoundBaker.Report()`, 25 rows): loops 2.00 s, one-shots 0.05-0.36 s, peaks
  0.50-0.85 (no clipping), RMS -10.9 .. -28.8 dBFS, loop seam 0.08-2.12 x RMS (audible from ~3).
- Cost found and designed around: synthesising one 2 s loop takes **63-192 ms**, so generating on a tool
  change would freeze a third of a second. Empty bank slots are now rendered on a background thread
  (`ProceduralSfx` is pure maths; only `ToClip` touches the engine) at 2 clips/frame; the shipped bank is
  baked to WAV so nothing is generated at all.
- Gotchas: `AudioImporter.preloadAudioData` is obsolete in 6000.7, it moved into `defaultSampleSettings`;
  a ru-RU editor makes `ToString("F3")` emit decimal commas, which produced invalid JSON from
  `DebugStateJson` until it was switched to `CultureInfo.InvariantCulture` like `CarpetDebugApi`;
  `FindObjectsByType<AudioSource>` does not return the `HideFlags.DontSave` sources, so an early "nothing is
  playing" reading was the query's fault, not the audio's.
- Open: no HUD volume slider (mute is **M**); no ambience or hand-over sound; `AudioListener.GetOutputData`
  reads zero from the CLI because an unfocused editor pumps neither frames nor audio, so the mix has not
  been judged by ear; placeholders sound like *a* machine, not like a specific one - real recordings are the
  intended upgrade (`AUDIO.md` 5).

## 2026-09-20 - tool heads stopped trembling (heading filter)

- Report: every tool shakes while you drag it, the angle jumps instead of turning, and it shakes even on a
  drag that is dead straight. Design + numbers: `TOOL_MODELS.md` 6.
- Root cause, measured on the live camera with `TryGetCarpetUV`: **one screen pixel is 1.06 mm of carpet**, so
  a straight drag is a pixel staircase and the raw per-frame segment direction swings **51.5 deg** at
  1.5 px/frame (17 deg at 3 px/f, 11 deg at 8 px/f). `ToolController` fed that segment straight into the
  `ToolStamp` dir, the cursor quad and `ToolModelView`, with no easing at all - so the noise was applied
  whole, every frame. Nothing was wrong with the models or the placement maths.
- Fix: `ToolController.UpdateHeading`, one call per frame, fed by the stroke while a button is down and by
  the hover otherwise (so pressing no longer snaps the head round from a stale heading). Travel accumulates
  until it is worth 2.5 mm before it counts as a direction; the target then blends by `1-exp(-len/22mm)`
  (distance, not time); the shown angle eases at 20 Hz under a 360 deg/s cap.
- Two headings on purpose at first: a **signed** `_headAngle` for the stamp `dir`/`vStroke` (the squeegee
  must still push water the way it is moving) and a `_viewAngle` folded mod 180 deg for blades, so pulling
  a squeegee back would not spin the head. **That split was wrong - see the follow-up below.**
  - Got the fold wrong first too: folding the *eased* angle never fires, because it follows a reversal round
    degree by degree and never sees a jump bigger than 90. The fold has to be on the measurement.
- `StrokeSegment` now places stamps along `stepDir = seg/len` (the path the pointer really took) and uses the
  filtered heading only for the stamp's own `dir` - filtering both would drag stamps off the pointer on turns.
- Cursor hover height eased too (`heightEaseHz`): carpet 0.03 m vs tile 0.009 m is a 21 mm pop at the rim.
- Verified by reflection on the live component (`scratchpad/heading_test.cs`, `corner_test.cs`, `reverse_test.cs`):

  | | before | after |
  |---|---|---|
  | straight drag 1.5 / 3 / 8 px per frame | 51.5 / 17.0 / 11.0 deg of swing | 2.3 / 1.8 / 0.9 deg |
  | 90 deg corner at 250 mm/s | instant | 50% in 7 frames, 90% in 16 (0.27 s) |
  | biggest single-frame step | unbounded | 6.0 deg |
  | blade stroke reversed | 180 deg spin | 0.0 deg |
  | 135 deg corner, blade | 135 deg spin | 45 deg (same axis, short way) |

- `headingEaseHz = 20` picked from a 3x3 sweep (`headingTurnMm` 12/16/22 x `headingEaseHz` 14/20/28):
  tremble 1.3-3.4 deg, 90% corner 15-22 frames. All five knobs are public on `ToolController` for tuning.
- Regression: recompile clean, 0 console errors. Play-mode scripted run (scraper -> hose -> squeegee,
  `Reset(1)`) still behaves - crust 0.8815 -> 0.8570, grit 0.0080, clean 0.0079, water 0.169,
  pooledCells 0.085, slurry 5.1e-4. Screenshot `Assets/Screenshots/tool_smoothing.png`.
- Open: not yet confirmed by hand in a focused Editor - the numbers say the tremble is gone, but the feel of
  `headingTurnMm`/`headingEaseHz` is a judgement call and the knobs are in the Inspector for that.

### Follow-up the same day: the effect had come off the head

- Report: the head itself is now steady, but **the effect on the carpet moves when the head does not**, and
  fast repeated reversals turn the treated area into a thin stripe that does not match the head at all.
- Cause: splitting the signed heading from the drawn axis split the two readers of `ToolStamp.dir` apart.
  `FootprintWeight` (`SimCommon.hlsl` 71-79) reads `dir` through `abs(dot(...))`, i.e. as an **axis** - so
  the footprint followed the *signed* angle, which eases 180 deg round a reversal and passes through the
  perpendicular on the way. For those ~0.3 s the 220 mm squeegee lay ALONG the stroke, covering twice its
  15 mm radius instead of its blade. Reverse faster than the sweep finishes and it never recovers.
- Fix: one angle, not two. `_headAngle` is the tool **axis** mod 180 deg (what is drawn), `_travelSign` is
  which way along it the tool is going, and the stamp gets `StrokeDir = ViewDir * _travelSign`. The
  footprint reads it as an axis and gets the drawn axis; the fibre bend, the debris throw and `vStroke`
  read it signed and get the real direction. The fold now applies to every tool, not just blades - for a
  round head it is invisible, and it keeps `vStroke` from swinging through the perpendicular on a brush
  reversal too. `_viewAngle`/`_viewTarget` and the blade/disc resync in `SetTool` are gone.
- Verified end to end on the carpet, not just in the filter. Scraper (120 mm blade), 96 stamping frames at
  1350 mm/s over the same span, touched width read from the damage channel of the `crust` debug view (it is
  written into the footprint and does not flow):

  | | touched width across the stroke |
  |---|---|
  | 12 passes, always one way | 125 mm |
  | 12 passes, reversing every 8 frames | 125 mm |
  | control: stamps with the axis turned 90 deg off the stroke | **25 mm** |

  The control is the bug reproduced on purpose; without it the 125 mm would prove nothing. Screenshot of the
  reversing scrub: `Assets/Screenshots/tool_axis_reversals.png` - a clean rectangle the width of the blade.
- Filter behaviour unchanged where it was already good: tremble 51.5 -> 2.3 deg at 1.5 px/frame, 17.0 -> 1.8,
  11.0 -> 0.9; 90 deg corner 90% in 16 frames; max step 6.0 deg. Reversal now moves the axis **0.0 deg** and
  the direction handed to the sim is exact from the first frame (was a 180 deg sweep through 90).
- Regression: recompile clean, 0 console errors, and the scraper -> hose -> squeegee run after `Reset(1)`
  matches the numbers from before the change (crust 0.8569, grit 0.0080, water 0.1687, pooledCells 0.0852).
- Pitfall for the next session: measuring a footprint through the **fluid** channel does not work - water
  flows, and the "swath" came out 450-600 mm wide for a 160 mm head. Use a channel the tool writes and
  nothing transports (damage, crust, fibre bend).

## 2026-09-20 - tool head models (Tripo AI)

- Goal: one low-poly 3D **working head** per tool, replacing the flat outline cursor; must read as the tool
  without pulling attention off the carpet. Design: `TOOL_MODELS.md`.
- `Tripo3d_Unity_Bridge` (project root) was **not** installed: reading it showed a WebSocket receiver on port
  60610 for models pushed from the Tripo web frontend, with no generation API and no API key. Generation went
  through the already-installed `com.coplaydev.unity-mcp` `generate_model` tool (provider `tripo`,
  `configured: true`, key in Windows Credential Manager), driven over JSON-RPC to `http://127.0.0.1:8080/mcp`.
- Prerequisite installed: `com.unity.cloud.gltfast` 6.20.0. Tripo returns `.glb` and `AssetGenJobManager.WriteFile`
  prefers the result URL's extension over the requested `format`, so GLB import is unavoidable.
- **8 of 10 heads generated.** Tripo credits ran out mid-run; `Tool_DryVacuum` and `Tool_FoamSprayer` are missing
  (403 `code 2010`). They failed in the first batch because a C# write triggered a domain reload that killed the
  in-flight jobs, and the retry hit the exhausted balance.
- Prompt finding: Tripo ignores negatives. "no handle, no stick" still produced a scraper with a full handle plus
  blobby surface junk (`Screenshots/prev_Tool_Scraper.png`). Rephrasing positively as "a single detached
  replacement head ... with a short thick mounting collar on top, isolated spare part lying flat alone" gave a
  clean head-only mesh on the first try (`Tool_Scraper_v2`).
- Measured: 4713 tris / 5169 verts for the scraper head, 1 mesh, 1 material (`Shader Graphs/glTF-pbrMetallicRoughness`)
  with Color + ORM + NormalGL textures. Normalised footprints (m), x/y/z after `CarpetCleaning/Normalize Tool Models`:
  Scraper (0.052, 0.035, 0.120), Hose (0.064, 0.071, 0.080), Brush (0.090, 0.035, 0.090), Squeegee (0.043, 0.129, 0.220),
  WetVacuum (0.039, 0.082, 0.160), Dryer (0.220, 0.153, 0.220), FloorCleaner (0.170, 0.089, 0.170), Mop (0.280, 0.065, 0.320).
  The z extent matches the tool footprint from `ToolTable` in every case.
- New files: `Scripts/ToolModelSet.cs`, `Scripts/ToolModelView.cs` (order 205), `Editor/ToolModelPostprocess.cs`.
  `CarpetRenderer` gained a `toolModels` reference and drives it from `SetCursor`; `CarpetSceneBuilder` gained
  `Models/Tools` + `Prefabs/Tools` folders, `BuildToolModelSet()` and the `ToolModels` child under `CarpetRoot`.
- `hoverMarginM = -0.012`: `ToolController` builds both hover heights as "surface + 0.012 m", so one constant seats
  the head 2 mm into the 20 mm pile on the carpet and flush with the tile on the floor. Verified in both domains.
- Fallback kept deliberately: a tool with no prefab still shows the outline quad (`SetCursor` enables it only when
  `ToolModelView.Has(tool)` is false). Verified live - tools 1 and 3 report `hasModel=False outlineEnabled=True`,
  so the two missing heads do not leave those tools blind.
- Verified in Play: recompile clean, `Build Scene` re-run, `Reset(1)` + strokes, `consoleErrors: 0`, stats `valid: true`.
  Captures: `Screenshots/tool_sheet.png` (8 prefabs), `Screenshots/play_sheet.png` (8 heads on the carpet),
  `Screenshots/tool_0..9.png`, `Screenshots/tool_9.png` (mop flush on the tile).
- Bug found and fixed during the pass: `ToolModelView.Awake` instantiated the heads, but `AddComponent` runs `Awake`
  in edit mode too, so `Build Scene` left 8 real GameObjects in the scene and the play-mode dictionary came up empty
  (`children=8 hasScraper=False`). Heads are now built lazily under `Application.isPlaying`; `Awake` only clears.
- Gotchas for next time: do not write C# while a generation is in flight (domain reload kills the job, though the
  file may still land on disk); `generate_model status` reports `data.state`, not `data.status`; `Camera.Render()`
  into a RenderTexture is empty under URP - use `PreviewRenderUtility`; `eval` wraps code in a method body so
  `using` directives fail to compile and bare `Object` is ambiguous.
- Open: `Tool_DryVacuum` and `Tool_FoamSprayer` need Tripo credits, then `CarpetCleaning/Normalize Tool Models` +
  `Build Scene` picks them up with no code change. Squeegee (129 mm) and Dryer (153 mm) heads are on the tall side;
  the hose head is a saturated blue and the mop has a stray hook - all tunable via `ToolModelSet` offsets or a regen.

## 2026-09-20 (earlier) - HUD rebuilt on UI Toolkit, production restyle (NOT verified in Play mode, per request)

Ask: make the UI production-ready - pleasant, light, honest vertical rhythm - keeping the conceptual placement
exactly as it was and changing only the style; and do it in UI Toolkit (mandatory), without checking it in Play mode.

- uGUI Canvas/CleaningHUD replaced by a `UIDocument`. New files: `UI/CarpetHud.uxml` (structure), `UI/CarpetHud.uss`
  (all style, tokens as USS variables on `.hud`), `UI/UnityDefaultRuntimeTheme.tss`, `UI/CarpetHudPanel.asset`
  (PanelSettings, 1920x1080, MatchWidthOrHeight 0.5, sortingOrder 10), `Editor/HudArtBaker.cs`
  (bakes `UI/Textures/hud_shadow_{lg,sm}.png` - 9-sliced soft shadows, USS has no box-shadow).
  `Scripts/CleaningHUD.cs` rewritten as a binder only; `Editor/CarpetSceneBuilder.BuildUI()` creates the panel asset,
  bakes the art and wires UIDocument + CleaningHUD. Docs/HUD.md rewritten.
- Placement unchanged: meters top left, hint top centre, Reset + fps top right, carpet tools left edge, floor tools
  right edge, hand over / take new bottom right. 24 px gutter on all four sides.
- Style: light "spa glass" - 92 % white translucent surfaces, hairline rim (8 % ink), soft baked shadow, deep-slate
  ink, three accents (teal active tool, emerald hand over, blue new carpet) + mint/amber meters. The 10 loose grey
  squares became two grouped panels of 44 px rows with keycap chips; the two text-on-bar bars became one card with
  CLEAN/DRY captions, right-aligned percentages and 8 px tracks. 4 px spacing grid, type 11/12/13/14/18.
- Behaviour kept 1:1: chrome fades (USS transition on `.layer`) and stops picking on the FIRST stroking frame,
  restores after 0.07 s; meters only dim (0.45) and never pick; hand-over button breathes by flipping `.is-lit`
  against a 0.55 s transition, so even the pulse colour lives in the stylesheet.
- New plumbing: `ToolController.PointerOverUI` (Func<Vector2,bool>, instance field). UI Toolkit panels are invisible
  to `EventSystem.IsPointerOverGameObject()`, so CleaningHUD supplies `panel.Pick()` + a walk up to the class
  `ui-blocking`. Only the tool panels, tool rows, Reset and the action buttons carry it.
- Trap found while building: `UIDocument` instantiates the UXML into a `TemplateContainer` that neither stretches nor
  ignores picking. Left alone it (a) collapses every `bottom: 24px` to a zero-height parent and (b) answers
  `Pick()` over the whole screen, which would refuse every stamp everywhere. `CleaningHUD.Build()` forces
  `position: absolute` + zero insets + `PickingMode.Ignore` on every root child.
- Second trap: `background-image: url("project://database/Assets/.../x.png")` logged "Invalid asset path"; a path
  relative to the .uss (`url("Textures/hud_shadow_lg.png")`) is what actually resolves. Confirmed via
  `AssetDatabase.GetDependencies` on the imported stylesheet, which now lists both PNGs.
- Verified (no Play mode, per request): `unity command recompile` clean (0 errors); `Build Scene` runs clean;
  asset probe returns uxml/uss/tss/panel/panelTheme/texture 128x128 all present, UIDocument has panelSettings +
  visualTreeAsset, hud.sim/tools/handover wired, old Canvas gone, UXML instantiates with `chrome`, `hint-label`,
  `carpet-tools` and `reset-button` found and 1 stylesheet attached.
- Open: nothing has been seen on screen yet - awaiting the user's look. Likely first adjustments: contrast of the
  white cards over the white tile floor, hint pill size, whether the floor strip should mirror the carpet strip.

## 2026-09-20 (earlier) - white glossy clean floor, HUD gets out of the way, decisive detergent + mop (NOT verified in Play mode, per request)

Three asks from the user, all floor-side. Compile-verified only: FloorSurface.shader 0 msgs, FloorSim.compute 0 msgs,
full `unity command recompile` clean (0 console errors), material/settings values read back from the imported assets.
No Play-mode run and no screenshots - the user asked for feedback on the live build instead.

- **Clean floor reads white and shiny** (`FloorSurface.shader`, `Materials/FloorSurface.mat`, `CarpetSceneBuilder`).
  `_TileColor` 0.46 -> (0.82, 0.83, 0.85), `_GroutColor` 0.27 -> 0.60, and the dirt got *darker* to open the gap:
  `_DustColor` (0.50,0.44,0.35) -> (0.40,0.33,0.23), `_ResidueColor` (0.24,0.17,0.10) -> (0.19,0.13,0.07).
  New `_CleanGloss` (0.74) replaces the flat 0.30/0.12 base smoothness and new `_CleanSheen` (0.45) drives three
  polish terms gated on `polish = 1 - soil`: a dry fresnel sheen next to the wet one, a `pow(NdotH, 260)` sun glint
  excluded from the grout, and a ~6 mm micro-twinkle. Dust still forces smoothness to 0.08, so dirty = dark + matte,
  clean = white + glossy + (just) above the 1.2 bloom threshold. Knobs and the reasoning: `FLOOR_SYSTEM.md` 5.
  Deliberately contradicts the old "the 3x sun would blow out anything lighter" note - white is the brief.
- **HUD hides while a tool works** (`Docs/HUD.md`, `CleaningHUD.cs`, `ToolController.cs`). The canvas now has two
  full-screen containers: `Chrome` (strips, hint, Reset, FPS, hand-over buttons) and `Bars` (Clean/Dry only).
  `ToolController.IsStroking` latches while a button is held and the stroke has started; `Chrome` fades to 0 over
  0.12 s and **drops `blocksRaycasts` on the first stroking frame**, not when the fade ends, so the fade cannot eat
  stamps at the screen edges. The bars stay (alpha 0.45 while working) and are non-blocking by construction - the
  group has `blocksRaycasts = false` on top of the per-graphic `raycastTarget = false`, so debris under them is both
  visible and clickable. This was the reported bug: the strips and the 360x66 hand-over button sit over real floor.
- **Detergent and mop are decisive now** (`FLOOR_TOOLS.md` 3/5, `FloorSettings` + `.asset` in sync, `FloorSim.compute`).
  Spray: `F_DET_RATE` 3 -> 7, `F_DET_REF` 0.30 -> 0.16 (one fast pass arms the mop at full rate), `F_DET_K_RES`
  0.08 -> 0.60 (the film visibly eats dried rings while it sits), `F_DET_WATER` 0.5 -> 0.35 and `F_DET_TO_RES`
  0.25 -> 0.10 so a stronger film does not leave more to dry or more haze. Mop: `F_MOP_K` 60 -> 260,
  `F_MOP_K_DRY` 1.5 -> 8, `F_MOP_EAT` 25 -> 45, `F_MOP_FOAM` 40 -> 120, `D_MOP_PICK_MM` 7 -> 13 (above
  `D_CHIP_SIZE_MAX` 12, so a pass really leaves the tile bare).
  The dryness fix is the interesting one: `wetFilm` used to be `max(wetFilm, F_MOP_WET 0.9 * ...)`, i.e. the head
  *stamped* a dark damp memory that then took `F_TAU_WET` 45 s to fade, so a mopped floor read soaked for a minute.
  It is now a **cap** - `wetFilm = lerp(wetFilm, min(wetFilm, F_MOP_WET 0.22), mp)` - so the head wrings the tile,
  and the soft footprint edge dries less than the centre rather than more. With `F_MOP_SOAK` 60 -> 320 mm/s,
  `F_MOP_FILM` 0.03 -> 0.008 mm (FloorWater alpha 0.06, barely a sheet), `F_TAU_WET` 45 -> 16 s and `F_E_SURF`
  0.004 -> 0.008 mm/s the tile should read dry within ~2 s of the pass. `F_K_DUST_DIS` 3 -> 6 so a wet/sprayed
  tile gives its dust up to the head faster.
- Open / to check with the user: whether the near-white floor overpowers the carpet or blooms too much (raise
  `Bloom.threshold` on `Settings/SampleSceneProfile.asset` from 1.2, do not darken the tile); whether
  `BarsWorkAlpha` 0.45 is readable enough mid-stroke; whether the mop is now *too* strong to be satisfying.

## 2026-09-20 (later) - every tool always available, the bare carpet rect is live floor, hand-over buttons moved (verified in Play mode)
- Request (user, RU): move "hand over to client" and "take new carpet" to the BOTTOM-RIGHT corner so they stop covering the carpet; make every tool available at all times, including the carpet tools while no carpet is there; and kill the blind zone - the mop and the floor detergent must work on the patch where the carpet normally lies when it is away.
- HUD (`CleaningHUD`): both action buttons are now anchored bottom-right (anchor/pivot (1,0), pos (-20, 20), 360x66, font 24, "Hand over (H)" shortened). At the 1920 reference the left edge sits at x = 1540, right of the carpet's near-right corner and below the right-hand floor strip (centred on the right edge). No tool button is greyed out any more (`BtnDisabled` and the `_btnBtn` array are gone); the away hint reads "Carpet away - clean the bare tile with any tool, or take a new one".
- Tool gating: `ToolController.CarpetToolsEnabled` and `BlockCarpetStamps` deleted. `SetTool` accepts every tool, `StrokeSegment` drops nothing. The single new flag is `CarpetSimulation.CarpetAway` (`[NonSerialized]`, written by `CarpetHandover.SetState`, true from the end of the fly-out until the next carpet LANDS). The carpet sim uploads `_StampCount = 0` to `CarpetSim.compute` while it is set, so no stamp can reach an invisible carpet, while the C# `StampCount` still reports the real count to the floor sim and the debris system.
- Blind zone gone: `DebrisCommon.hlsl` gained `int _CarpetAway` + `CarpetCoversCMM()`, and every carpet-rect test now goes through it - `FloorSim.compute` (`underCarpet` in K_FInject, `FloorWallCell`) and `Debris.compute` (`GroundAt`, the pile-edge bounce). The +40 mm wall moved OUT of the bed texture (`K_FInit` writes plain tile relief, `K_FFlux` adds it at read time in the new `BedAt`), because `_FBed` is written once and a baked wall cannot be switched off mid-play. Shaders got a `float _CarpetAway` global (set in `FloorSimulation.SetGlobals`): `FloorSurface` (`inCarpet` mask), `FloorWater` (`CarpetCoversMm` in the vertex clamp, the normal helper and the discard) and `CarpetFoam` `_FLOOR_DOMAIN` (the rect discard plus an early-out in `FieldAt`/`FAt` that returns the pure floor field, so foam inside the rect is seated on the tile instead of on a phantom pile top).
- Verified in Play (scripted frames, editor unfocused):
  - Away + hose (a CARPET tool) held 1 s at uv (0.5, 0.5), dead centre of the rect: floor water 0 -> 0.0423, pooledCells 0.0098, carpet stats untouched (water exactly 0). One mop pass over it: water -71 %, wetFilm x2. Detergent in the rect: 0 -> 1.45e-3. Foam sprayer + brush in the rect: foam 0.0174.
  - `TakeNewCarpet` -> landing: the rect reads 0 in every channel (the wall wipes it under the arriving carpet), a hose hold then puts water 0.18 on the CARPET with the floor still at 0.
  - Floor state OUTSIDE the rect survives a full hand-over cycle: detergent 1.5009e-3 -> 1.4981e-3 (its normal drying only).
  - Captures: `Assets/Screenshots/away_tools.png` (foam mound + wet patch inside the bare rect, all 8 carpet tools lit, "Take new carpet (N)" bottom-right), `ui_idle3.png` ("Hand over (H)" clear of the carpet).
  - 0 shader / compute messages after `ImportAsset` AND after dispatching (the lazy-error gate), recompile clean, 0 console errors.
- Open: the rect is wiped when a carpet lands on it, so a puddle left there is lost (the carpet sim has no "soaks up what it is laid on" term). The HUD still refreshes the action buttons only on stats / state events, so `SetForceWin` alone does not make the button appear until the next event.

## 2026-09-20 - floor detergent + mop, and handing the carpet over to the client (verified in Play mode)
- Request: floor tools in the HUD on the RIGHT, mouse only - a floor detergent and a mop that cleans the tile perfectly where the detergent was sprayed; plus a "Hand over to client" button that appears when the carpet is clean and dry, flies the carpet off screen, and (user follow-up) then offers a second button "Take new carpet" so the player can mop the empty floor first. The floor mess must survive the swap. New carpets cycle the pictures in `Textures/` at random without repeats.
- Design docs written first and kept: `Docs/FLOOR_TOOLS.md`, `Docs/HANDOVER.md`.
- Tools: `ToolId` 8 Detergent / 9 Mop, `ToolDefinition.floorTool` splits the two HUD strips (never an index range), `ToolTable.StripCount/NextOnStrip/FirstOnStrip`. The scroll wheel stays on the strip of the current tool, so the eight carpet tools cycle exactly as before. Floor tools have no hotkey and no branch in `CarpetSim.compute`.
- Detergent film lives in the free 4th channel of `_FDeposit`: zero new RTs, zero new UAVs (`K_FInject` stays 6, `K_FChem` 3). `K_FInject` gained tools 8/9, `K_FChem` the film ageing, `K_FReduce` two sums (detergent, dirtyCells), `Debris.compute` a mop branch for grounded shards plus a one-shot `_KillInCarpet` cull and an explicit `[loop]` on its stamp loop.
- Hand-over: new `Scripts/CarpetHandover.cs` (order 210) animating a new visual-only `CarpetStage` child; `CarpetSimulation.ResetCarpet(seed, notify)` allows a carpet-only reset; `ToolController` gained `CarpetToolsEnabled` + `BlockCarpetStamps` (per-stamp) and only reports `PointerOnCarpet` while the collider is enabled; `DebrisSystem.KillCarpetRect()` / `CountAliveSync`; two bottom-centre HUD buttons; `CarpetSceneBuilder` builds the stage, wires the component and collects the picture library.
- Verified (scripted `StepAll` frames, captures in `Assets/Screenshots/`):
  - Spray: detergent 0 -> 1.33e-3 mean, foam stays 0 (the spray deliberately adds no agitation), carpet untouched; the film no longer washes itself away (only standing water deeper than 0.6 mm dilutes it).
  - A/B on an identical mess: residue 4.30e-4 -> 1.13e-4 with detergent + one mop pass (-74 % of the domain mean) vs 4.41e-4 (-0.5 %) for a dry pass, and -8 % after three dry passes. Per pixel inside the residue footprint: mean 39.2 -> 7.9 (-80 %, dirty pixels 100 % -> 27 %) for one detergent pass; a dry mop instead SPREADS it (nonzero pixels 3406 -> 5819). Two overlapping spray+mop passes: 40.5 -> 0.99 (-97.6 %, dirty pixels 5 %), damp trail 216 -> 71 over the next 50 s.
  - Mop collects tile shards: live particles 152 -> 101 over one pass, count inside the carpet rect unchanged.
  - Hand-over: all four carpet corners reach viewport y >= 1.136 at the end of the fly-out (fully off the top; at rest a corner sits at 0.033). While away: tool auto-switches to Detergent, `SetTool(Hose)` is refused, a hose hold on the rect leaves carpet `water` at exactly 0, floor stats and the debris ring head are unchanged (so `CarpetReset` did not fire) and live particles inside the rect are 0. Take new carpet: seed 1 -> 2, picture spongebob -> cat_meme, base colour -> forest, shells on a runtime material instance while `CarpetFibers.mat` keeps its authored values. Captures `ho_10_idle`, `ho_12_fly60`, `ho_13_away`, `ho_16_newcarpet`, `ho_20_band` (cat print visible in a washed band), `ft_0_mess`/`ft_1_sprayed`/`ft_2_mopped_half`, `mop_*`/`mop2_*` deposit views.
  - Compute + shader imports 0 messages, re-checked after a dispatch (lazy-error gate); C# dry run 0 errors; recompile clean; `Build Scene` run twice leaves `CarpetRoot` with exactly `CarpetStage` + `ToolCursor`.
- Tuning after the first Play run: `F_DET_RATE` 1.2 -> 3.0 and `F_DET_REF` 0.35 -> 0.30 (a brisk 630 mm/s spray pass only reached det 0.32, so the mop ran at a fraction of its rate); the film wash term now needs standing water deeper than 0.6 mm.
- Bugs found and fixed while testing: a drying film re-deposited the dirt it had just dissolved as residue at the spray fringe (spraying visibly darkened the floor) - guarded with `det < 0.02`; `Texture2D.width/height` report the IMPORTED size, and Unity's default NPOT scaling turned the 750x499 cat into 1024x512, so the aspect fit squeezed the print into a 648 mm column - `LoadPictures` now forces `npotScale = None`.
- Picture added: `Textures/cat_meme.jpg` - "Cat Meme.jpg" by Jennifer Williams, Wikimedia Commons, CC BY-SA 2.0.
- Gotchas: while the Editor is FOCUSED it advances real frames between CLI calls, so a 0.85 s flight completes in one tick - set the flight durations high, `HandoverSeek`, capture, restore. Overlay UI never appears in `capture_game_view --source screen` while the Editor is unfocused (the Game View does not repaint); switch the canvas to `ScreenSpaceCamera` for a HUD shot. Long scripted evals exceed the 5 s main-thread budget of the pipeline server and report a timeout although the work completes - split them.
- Open: floor dust is still hard to produce (scraper dust puffs are born over the carpet and die there), so the mop's dust term is untested against real data; no floor readout in the HUD (needs an async floor stats path, `FloorStatsSync` blocks); the detergent is unlimited; the rectangle under the carpet stays clean and tool-proof while the carpet is away (deliberate, `HANDOVER.md` 7).

## 2026-09-19 — carpet edge: the bare strip along the rim is gone (verified in Play mode; user approved the look live)
- Request: at every carpet edge a strip that looks untouched by foam/water always remained (user screenshots of the near and right edges; user guessed the fibres seen from the side).
- Diagnosis (readbacks after a spray + brush at the near edge): the rim cell held F 0.004 and h 0.001 mm, ramping to F 1.2 only 12 cells (37 mm) in, while the first floor cell outside held F 4.9. The open edge (ghost bed 5 mm lower, h 0) drained the rim cells' film every substep and the foam followed via FOAM_SLIP * (flux / tiny depth); the floor mound outside sat on the tile because the cross-domain field lifted its base only by the carpet's own (empty) rim value; from the 55-degree camera the 35 mm floor mound hides only ~10 mm behind it, so the bare rim cells + the fibre wall (rays entering through the open shell side see the caked lower shells for ~2.5 mm) stayed visible. Full write-up: `Docs/CARPET_EDGE.md`.
- Simulation fix: new `SimSettings.EDGE_LIP = 0.3` mm (`_EDGE_LIP`, also in `SimSettings.asset`): ghost faces move only `max(h - EDGE_LIP, 0)` (cross-section, head, tool blend); `FoamOutflow` gates the water-slip foam velocity on ghost faces by the lip (the tool push still sweeps foam off the edge); slump diffusion uses clamped taps so rim cells take part (zero-gradient, conservative). After: rim column F 1.95 1.86 1.71 1.55 1.40 1.31 ..., h 0.197 0.225 0.248 ...; hose 3 s at u=0.05 still gives floor water 0.079 mm/cell (0.08 before) so the runoff feature is intact. Side effect: foam stopped at the lip right after a stroke can form a 1-cell heap (right edge: 11.6 vs 5.3) that the diffusion relaxes in ~3 s (6.96 / 5.9 / 4.8).
- Rendering fix (`CarpetFoam.shader`): `FieldAt` now returns the surface as the union (smooth max 6 mm) of the carpet mound (spilling over the rim, 45 mm) and the tile heap (own textures outside, reaching 24 mm back over the rim only where taller than the carpet surface); `Rim()` geometry helper; `FAt` matches. Silhouette exit test = min over 4 taps (6/12/24/40 mm, a tap without film counts as clear) instead of max over 2 (that punched pinholes into every back-slope). The carpet foam's rim vertices copy the height the coarser floor foam grid rasterizes along the rim (`RimVertexHeight`, new global `_FloorFoamPitchMm` from `FloorRenderer.Awake`), so the two meshes share one rim edge (per-column check at 1440p: the two meshes meet within exactly 1 px along the whole rim; no cracks).
- Verified: compute + shader import 0 msgs, dispatched after import, C# recompile clean, no console errors. Captures `Assets/Screenshots/edge_*.png`: `edge_repro.png` (before) vs `edge_fix_v3.png` / `edge_fix_1440_v4.png` (after) for the near + right edges; `edge_sprayonly*`, `edge_hose` (wet look reaches the rim, puddle beside it), `edge_floorspray*` (tile foam only: small overhang, nothing else on the carpet), `edge_corner`: no strip anywhere. The user watched the live Game view and asked to stop there ("I like how it looks now").
- Gotchas: a 2560x1440 `Camera.Render` right after a shader reimport rendered without the carpet foam (async shader compilation) - warm up with a `capture_game_view` first, and the CLI 5 s timeout hits on that first render. When the user focuses the Editor, Play advances real frames between CLI calls, so readbacks taken minutes apart differ (h at the rim 0.197 -> 0.085 by evaporation/absorption) - probe right after the scripted frames. Bash `\"` quoting inside `unity command eval --code` broke on nested strings; long snippets go through `eval_file`.
- Review (adversarial workflow, 4 lenses -> 16 findings, 2 refuters each, 36 agents): applied to `CarpetFoam.shader` - the tile heap's film / caps no longer paint the carpet's rim band where the heap stands below the carpet surface (`show` gate; the height union alone only hid its geometry), the carpet mound's base lift is blended over the first 6 mm outside so thin films (F 0.03..0.12) stay continuous at the rim, a vertex 1e-4 mm outside the rim counts as on it, corner squares sample both rims and outside taps keep half a floor cell clear of every rim line per axis, the shadow march weights each tap by the film there (the 20 mm carpet block beside bare tile no longer casts a 55 % skirt), `FoamMmAt` returns 0 early for F <= 0.05 (exact); `FloorRenderer.Awake` warns when `foamSubdiv` is not a multiple of 24; FOAM_LOOK.md items 1-2 (stale ddx/ddy and gradient-clamp statements) and the ARCHITECTURE globals line corrected. Refuted: ghost-face momentum draining below the lip (the stored flux is reset each substep and drag kills it), the F_MAX clamp deleting the rim heap (pre-existing global cap, not rim specific), the squeegee leaving 0.3 mm at the rim (intended), the slurry/surfactant/foam-dirt "2.25x concentration jump" across the rim (contested: one refuter shows they are cell totals and exactly conserved, the other reads them as densities; left as is, noted in CARPET_EDGE.md).
- Crash and recovery: after the recompile that followed those fixes the Editor died on the first Play frame with `d3d12: Unrecoverable GPU device error` (30 s main-thread stall, then `D3D12Fence::Wait` timeouts; crash folder `Crash_2026-09-19_182428462`). The only new GPU code was the foam fragment shader, whose `[unroll]` loops (18 bubble seats + 8 shadow taps, each inlining the now larger `FieldAt`) had grown huge (the import + compile check took ~30 s). Both loops are now `[loop]` (same behaviour): the compile check takes 4 s, 0 messages, Play entered cleanly, no fence errors in the log. Cost probe after the restart (30 x Camera.Render 2560x1440 + readback, same method as the foam session): whole-carpet foam (`FoamScene(5)`) 14.0 ms/frame (12.9..14.6 before the edge work, so the dynamic loops cost nothing measurable), edge scene (`FoamScene(4)`) 10.2 ms; the first probe of the run (empty carpet, 18.5 ms) still included warm-up and is not comparable. All rim scenes re-run after the restart (`edge_*_v5.png`): no strip on the near, right and corner edges, tile-only foam no longer smears the rim, hose at the left rim wets the edge, floor water 0.079 mm/cell as before; no console errors.
- Open: the post-stroke rim heap could topple over the lip faster (`CARPET_EDGE.md` s.4); the water grids still end at the rim (a hose puddle at the rim shows the fibre wall for ~2.5 mm); foam cost near the rim +2 floor taps and +1 FoamMmAt per field tap, exit test +2 FieldAt per foam pixel (not measured separately).

## 2026-09-19 — foam redone: bright pink-violet, real 3D mound (verified in Play mode)
- Request: the foam looked like TV static (flat grey-white speckle); make it bright pink-violet and visibly volumetric, by any means.
- Root cause: the foam was a fragment effect inside `CarpetWater` / `FloorWater` (3.4 mm Worley cells = ~1.5 px, normals from `ddx/ddy` of that noise, only a `0.9 F` mm lift, grey albedo).
- Design (3-approach panel + 2 judges + critic, then an adversarial review workflow on the code): one displaced height-field surface per domain with ray-traced bubble caps. New `Shaders/CarpetFoam.shader` (keyword `_FLOOR_DOMAIN`), materials `CarpetFoam.mat` / `FloorFoam.mat`, children `CarpetRoot/FoamSurface` (384x480) and `FloorRoot/FloorFoam` (384x384) built by the renderers + `CarpetSceneBuilder` (`ApplyFoamLook`); Transparent+1, premultiplied, ZWrite On, Cull Off. Cross-domain field: the carpet mound spills over the edge onto the tile (no cliff / chevrons at the seam). Water shaders lost the foam term, the 0.9 F lift and `_MudColor/_FoamColor/_BubbleScale`. `CarpetDebugApi` gained `FoamScene(1..5)`, `FoamMaxF()`, `SetFoamVisible()`. Full model, palette, knobs: `Docs/FOAM_LOOK.md`.
- Verified (live Editor, scripted frames, captures `Assets/Screenshots/foam_*.png`, base_* = before): 3 shaders import 0 msgs; dry run + recompile clean; Build Scene x2 -> 1 FoamSurface + 1 FloorFoam, queue 3001, keyword only on the floor mat; no console errors in any test.
  Spray (`FoamScene(1)`, F mean 0.146, max 10.8): pink-violet mound ~150 mm, bubble caps with highlights, violet foot/creases, bumpy outline; 1440p probes crest (186, 86, 163), top (211, 113, 192), foot (145, 56, 146), 0 grey samples in 11.9k pink px; two captures without stepping: 0 differing pixels. Brush smear F 0.149 -> wet vac 2 s 0.086 (-42 %, clean blade hole + suction dip); squeegee ridge, dryer ring; floor foam via edge spray + outward brush 0.0115, continuous across the edge, nothing under the carpet; ageing 0.146 -> 0.070 (10 s) -> 0.021 (20 s), bubbles pop one by one; empty carpet 0 pink px; hose puddle captures differ from the pre-change baseline by 4 px (> 24/255) of 230k.
  Cost (30 x Camera.Render into a 2560x1440 RT + readback, noisy +-1.5 ms): empty 8.3..11.6 ms (before 11.5), whole carpet foamed 12.9..14.6 ms (before 9.1..11.7) -> the worst case costs about +2..3 ms (18 exact field taps per foam pixel seat the bubbles; a single mound is far cheaper).
- Review workflow (3 lenses, 2 refuters per finding) fixed: tilted-film ray test, silhouette exit test (caps break the outline), fading small bubbles never beat full domes, explicit noise mips, 2.5 mm plinth, water discard removed (hiding the foam leaves no holes). Refuted: white specular blob (already tinted). Visual iteration on the carpet-edge spill (crops at 3x): bubbles seated on the exact field height (a tangent-plane seat or a clamped gradient smeared spheres into slivers on the ramp), footprint from view distance instead of screen derivatives (per-triangle derivatives made a comb along quad diagonals), horizon-style shadow test with a wide soft transition (per-tap banding), back-face branch removed (on the grazing ramp alternate triangles of each floor quad face the other way), and the real cause of the diagonal streaks along the seam: the bubble ray started at the analytic surface height while the rasterized point lies on the flat triangle (several mm off on the curved ramp) - the ray now starts at `posWS`. Final seam crop: round bubbles continuous from the mound onto the tile. Documented gaps: chunks buried in mounds, skirt writes depth (tiny resting dust inside a shadow crescent can be hidden), 3x3 search valid while r <= 0.6 cell.
- Gotchas: `AssetDatabase.SaveAssets()` during Play triggered an asset refresh that destroyed the ping-pong RTs (Fluid/Soap/Fiber) and the floor RTs ("Value cannot be null: texture" from the next Reset) -> only SetFloat/SetColor in Play, save when stopped, or stop/start Play. Bash heredocs in this harness break on apostrophes -> code files go through Write/Edit. Findings: SSAO feature + default-profile override run a useless DepthNormals prepass; `SetupVolume` never persists `ColorAdjustments` (saturation +12 inactive); `_NoiseTex` inline samplers ignore the texture filterMode (the foam uses `sampler_trilinear_repeat`).
- Open: user to judge the look (knobs in `FOAM_LOOK.md` s.4); optional stage 2 = instanced sphere bubbles; chunk seating on foam; the whole-carpet cost (+1..2 ms) could drop by trimming shadow taps.

## 2026-09-19 — water murk = carpet dirt, wet pile darkening (shaders only; NOT checked in Play mode, per request)
- Request: spilled water looked blue (took the carpet's colour); it must be dirty in proportion to the carpet's dirt and clear over a clean carpet; the pile must be darker where wet and bright where dry.
- Root causes: `_AbsorbDirty = (0.9, 0.5, 0.25)` absorbed red 15x more than blue after the sRGB->linear conversion (dirty water shifted to blue); the suspended-dirt term was an unlit `_MudColor*0.6` (dark hole, not brown); murk keyed only on slurry C while crust hydrates at 0.01/s; wet darkening keyed on `w`, which stays 0.1..0.2 after a hose pass (K_ABS 0.25 mm/s), and was capped to 88..94 % brightness.
- Fix (design + numbers in `WET_LOOK.md`): `CarpetWater` / `FloorWater` murk = saturate((C + pileDirt/waterTot) / _MurkCRef) with pileDirt = mud + 0.5 (crust + stain + grit) -> per-channel absorption (blue most), scalar silt extinction `_MurkDensity`, lit `_SiltColor` in-scatter; meniscus term made hue-preserving (`meniscus * 0.2 * body`, was a flat +0.12 grey).
  New material props `_SiltColor (0.26, 0.18, 0.10)`, `_MurkDensity 0.8`, `_MurkCRef 0.4`, `_MurkCarpet 1.0`; `_AbsorbDirty` -> (0.15, 0.30, 0.60). Values live in `CarpetSceneBuilder` and were written into `CarpetWater.mat` / `FloorWater.mat` via eval (no scene rebuild needed).
  `CarpetFibers`: `wetV = sat(1 - exp(-3w) + sat(h/0.3))` drives darkening `lerp(base, base^1.25, wetV) * lerp(1, _WetDarken, wetV)`, spec/aniso, rim (x (1 - crustCover)), dirty-water tint; hard `h > 0.2 -> *0.85` step removed.
- Verified: 3 shaders import with 0 messages (D3D12), builder dry-run compiles (known CS0618/CS0436 warnings only), no console errors. Fragment maths evaluated in python IN LINEAR SPACE (Color properties are sRGB->linear converted; the first pass forgot this): clean water over the clean carpet = the wet blue fibres (display (0.13, 0.28, 0.55), no cast); water on the initial carpet = brown (0.40, 0.28, 0.17) at 1 mm; half-clean patch = greyish (0.30, 0.28, 0.37); wet blue pile luminance 0.22 -> 0.17 (w 0.1) -> 0.11 (w 0.4) -> 0.07 (film).
- Adversarial review workflow (3 lenses x 2 refuters per finding, 27 agents): 4 confirmed + fixed (murk fell as brushing dissolved mud because the pile term was an amount and the slurry term a concentration; pile term saturated over the whole initial carpet; silt too light vs the wet pile; flat grey meniscus), 8 refuted; rim-off-crust and the soft wetness curve applied anyway as cheap insurance.
- Open: user to judge the look in Play mode (knobs: `_MurkDensity`, `_SiltColor`, `_MurkCRef`, `_MurkCarpet`, `_WetDarken`); dry-pile brightness left unchanged; splash droplets (`DebrisDrop`) still use their own tint.

## 2026-09-19 — HUD: tool strip moved to the left edge (NOT verified in the Editor, per request)
- `Scripts/CleaningHUD.cs` `BuildToolbar`: the 8 tool buttons are now a vertical strip anchored to the left screen edge (anchor/pivot (0, 0.5), 16 px margin, buttons 96x72 stacked top-down with an 8 px gap, total 632 px, centred vertically) instead of the horizontal row at the bottom centre. Button contents (hotkey + name), colours, outline and click wiring unchanged; Clean/Dry bars (top-left) and Reset/FPS (top-right) untouched.
- Not compiled or screenshotted this session (user asked for the change only). To verify: `unity command recompile`, stop/start Play, `capture_game_view`.

## 2026-09-19 — floor + debris system (integrated, verified in Play mode)
- Request: real floor; scraped crust chunks scatter over the floor; water/foam spill onto the floor; every dirt interaction leaves traces on the floor; tools work on the floor too; realistic look.
- Design written first: `FLOOR_SYSTEM.md` (binding contract). Shared files written by the main agent: `Compute/DebrisCommon.hlsl`, `Scripts/DebrisStructs.cs`, `Scripts/FloorSettings.cs`,
  `CarpetSimulation.cs` API (EdgeOut/EdgeOut2 RTs, emit append buffer, K_ClearEdge/K_Emit dispatch, CarpetReset event, floorSettings, `_CarpetToWorld` global).
- Implementation split into three parallel agent lanes (A floor sim + render, B debris particles, C carpet compute hooks + input + scene builder + debug API), then integrated in the live Editor by the main agent.
- Integration results (main agent, same day; all via `CarpetDebugApi` scripted frames = `StepAll` chain carpet -> debris -> floor, captures in `Assets/Screenshots/g*.png`):
  - Recompile clean, `Build Scene` wires FloorRoot (FloorSimulation/FloorRenderer/DebrisSystem) + 5 materials + FloorSettings.asset; no console errors during any test.
  - Scraper at the left edge pushing outward (6 x 2 strokes): 1382 debris spawned, chunks rest on the floor up to ~30 cm from the edge and on the carpet; dust puffs visible mid-stroke (`g1_scrape.png`, `g3_mid_scrape.png`).
  - Hose held 3 s at u=0.05: floor water mean 0.08 mm/cell (~1 L) puddle beside the carpet, carpet keeps 0.19 mm/cell; droplets spawn (660) (`g2_hose.png`, `g6_puddle.png`).
  - Foam sprayer + brush pushing outward: floor foam 0.016, then dryer 12 s: foam 0.0013, residue 0 -> 5.5e-4 (rings), water 0.035 -> 0.022 (`g4_foam.png`, `g5_dried.png`).
  - Tools directly on the floor: hose puddle 0.105 -> squeegee reshapes it (wet film streak stays) -> wet vac hold removes only what is under the blade (`g7_squeegee.png`). Tools 60 cm away from the carpet leave carpet stats bit-identical.
  - Cost: 120 full StepAll frames incl. a sync readback = 437 ms -> 3.6 ms/frame for carpet + debris + floor on the RTX 3070 Ti laptop.
- Tuning after the first run: F_H_REST 0.35 -> 1.2 mm (puddles were 4x too wide), chunk launch 0.9..2.2x stroke speed + D_CHIP_UP 1.5 (was 0.55..1.25x, 0.6: chips only hopped 10 cm), D_CHIPS_PER_UNIT 0.12, D_DUST_PER_UNIT 0.06, dust puff 35 mm / opacity 0.55, chunk sizes 4..12 mm,
  sliding friction 3x on the pile vs tile, palette darkened (tile 0.46 grey, grout 0.27, chunks 0.34/0.18) because the 3x sun blew out the original values; FloorWater gets a faint standing shimmer and a slightly boosted fresnel.
- Gotchas found: the Editor does not advance Play-mode frames while unfocused (autotick / runInBackground did not help), so `LiveStroke` only progresses while CLI commands tick it — use scripted `StepAll` frames for tests and capture right after a partial stroke for mid-flight shots. A recompile during Play resets private RTs; stop/start Play after every recompile.
- Adversarial review (3 lenses -> 8 confirmed, 0 refuted, 8 unverified lows) and the fixes applied the same day:
  - HIGH fixed: tool-driven flux (hose radial push, squeegee/brush vStroke) moved floor water into the under-carpet cells where H_REST trapped it invisibly (wet vac could never remove it); foam leaked the same way through FFoamOutflow and the slump laplacian. Fix: `FloorWallCell()` closes every face into the carpet rect for water and foam, zero-gradient diffusion taps, under-carpet cells are zeroed in K_FInject/K_FIntegrate. Verified: hose/squeegee/foam held at u=-0.03 -> 0 under-carpet cells, 0 volume.
  - Floor water was hard-clamped to F_H_MAX at four write-backs, discarding most of the runoff arriving in the 10 mm drip band; clamps removed (flux limiters bound the export), FloorWater relief capped at 6 mm for rendering only.
  - Chunks sliding/flying from the floor into the carpet rect snapped 20 mm up onto the pile: the carpet side now reflects them (restitution 0.3, tangential slide kept). Dust puffs died on first ground contact after ~0.3 s: they now hug the ground and live out D_DUST_LIFE. Resting shards sit at 0.25*size (flat side down) instead of hovering. Foam age crossing the edge is exported in F*age*mm^2 like the other extensive channels. Corner carpet texels are halved in EdgePull so the four corners are not delivered twice.
  - Lows fixed in the C# batch: dust quad single winding (opacity 0.8), FloorSettings.Apply caches property ids, cursor sits 9 mm over the tile when the pointer is off the carpet, FloorRenderer pushes default tile globals in Awake. Not done (documented): centralised dt clamp across the three sims, drift only matters at TimeScale > 4.
  - Lesson: `ShaderUtil.GetComputeShaderMessages` right after ImportAsset reported 0 messages for a K_FFlux that failed to compile ("forced to unroll loop, but unrolling failed" from a `continue` inside an [unroll] loop); the real errors only showed after the first dispatch ("Kernel at index (3) is invalid" in the console). Always dispatch once (Reset + RunFrames) and re-read the messages / console after importing a compute.
- Final state after the C# batch (recompiled, Build Scene re-run, scene saved, Play mode restarted): no console errors; scraper test 1382 spawned; wall test 0 under-carpet cells; captures i1_mid_scrape.png / i2_scrape.png.
- Open: puddle reads as a dark translucent disc (no environment to reflect); chunk contrast on the carpet is low; hose flow (0.75 L/s) is generous so puddles get big fast; floor cleanliness is not part of the HUD/win condition.
- Lane C (carpet-side hooks, input, scene builder, debug API) done 2026-09-19: `CarpetSim.compute` + `SimCommon.hlsl` gained K_ClearEdge, K_Emit, edge runoff capture in K_Integrate (UAVs 7), scraper chip/dust split + dryer foam push in K_Stamp (UAVs 8), K_Emit UAVs 6; compute imports with 0 messages, all 10 kernels resolve.
  `ToolController.TryGetCarpetUV` falls back to the y=0 plane and accepts u in [-1,2], v in [-0.7,1.7] unclamped (`PointerOnCarpet`); `CarpetSceneBuilder` builds FloorRoot (FloorSimulation/FloorRenderer/DebrisSystem), 5 floor/debris materials, `Settings/FloorSettings.asset`, deletes the old `Floor` quad (stale `Materials/Floor.mat` left on disk);
  `CarpetDebugApi` gained `Floor`, `Debris`, `FloorStatsJson()`, `SaveFloorView(ffluid|fsoap|fdeposit|fbed)`, `DebrisCount()`. Dry-run compile of the lane files clean (only CS0436/CS0618 warnings). Not yet run in Play mode: emission rates (D_CHIPS_PER_UNIT etc.) are untuned.
  Re-verified by a second lane-C pass (same day): `CarpetSim.compute` import = 0 msgs, kernel indices K_Init 0 .. K_ClearEdge 8, K_Emit 9; UAVs counted from code (comments stripped): K_Init 7, K_Stamp 8, K_Integrate 7, K_Emit 6, K_ClearEdge 2.
  Dry-run gotchas: `unity command run_script --file` needs a Windows path (`C:/Users/...`), a `/c/...` path gets the cwd prepended even with MSYS_NO_PATHCONV; any dry run that includes `CarpetSceneBuilder` must also include `Scripts/CleaningHUD.cs`, otherwise `hud.sim = root.GetComponent<CarpetSimulation>()` fails with CS0029 (assembly type vs file-local type). CS0618 = `FindFirstObjectByType` is marked obsolete in 6000.7 (kept; warning only).

## 2026-09-19 (earlier) — baseline prototype
- Carpet sim + shells + water/foam + HUD working; one S0..S7 debug pass reaches ~80 % clean; ~135 fps at 2560x1440 game view on an RTX 3070 Ti laptop.
- Self-drying almost off (E_PILE relative 0.006, E_SURF 0.002, K_ABS 0.25, DRYER_BOOST 500). Known gaps: subtle brush stroke on dry pile, stains only partially dissolve, cursor quad shows in captures.

## 2026-10-18 — short-form creator outreach research (no outreach sent)

- Researched 20 candidates for Refluff: Carpet Cleaning ASMR across indie discovery, cozy gaming and cleaning/ASMR channels; report: `reports/Авторы коротких видео для Refluff.md`, source notes: `research_notes/Авторы коротких видео для Refluff/`.
- Report links public profiles and contact sources, groups priorities and marks evidence limitations: 12 published emails, 3 business forms, 4 social/Reddit contact routes and 1 official route to a gated/unextracted YouTube business contact. Rates/availability and current short-form cadence remain unverified unless explicitly stated; historical examples are labelled. Jarrod's published $300 rate is a snapshot, not a quotation. Jestr's generative-AI policy may conflict with the project's Tripo tool models.
- Verified the report contains exactly 20 numbered creator rows and the existing vertical fibers MP4 / Steam trailer files exist. No creator messages sent, no new video export or external file publication, no runtime/game changes or Unity tests.
## 2026-10-18 — local creator outreach kit and individual-key drafts

- Prepared `SteamStore/outreach/2026-10-18/`: public-only creator kit + ZIP (253,645,982 bytes), separate private workflow folder, 20 individualized letters (18 EN / 2 JA), 12 single-recipient X-Unsent email drafts, private form/DM text, tracker and one manual follow-up per creator. No messages sent or files uploaded; user will host the public folder.
- Public pack: new 20 s 1080x1920/60 clean edit, existing captioned 25.7 s vertical, five 1920x1080/60 fixed-camera b-roll clips (3.5 / 3.5 / 3.5 / 4 / 4.5 s), four ACES screenshots, three logos and audio attribution. Cinematic macro camera/light/shell settings are disclosed. Existing licensed museum-rug takes were sufficient; no new Unity capture, source/master/EDL or game changes. Unity Pipeline list saw the running project but no reachable server; no live mutation attempted.
- Steam publicly lists November 2026 as of this check. Rendered only trailer shot 21 again with the November card in an outreach copy; original 60 s master and original EDL untouched. Public source: https://store.steampowered.com/app/5304930/.
- User explicitly requires a different key inside each first letter and will provide the keys later. Created stable per-creator key slots plus private allocation helper; no real keys supplied or invented. Key assignment rejects duplicates, survives reordered reruns, preserves tracker history and is excluded from public material. Public share URL is also pending. Joe gets a private email for the key, never a public Discord post; reserve/contact/AI-policy holds retained.
- Verification: all 8 MP4s fully decode, exact expected frame counts/sizes, H.264/yuv420p/60 with audio, no black intervals or clipping. Measured -14.1..-16 LUFS (foam/brush/squeegee -15.2..-15.8), true peaks -1.9 dBFS or lower. Public ZIP CRC and SHA256 manifest pass. Temporary fixtures verify 20 unique assignments, one own key per letter, stable reruns, duplicate rejection without mutation and tracker retention; fixtures deleted, no fake keys in campaign drafts. Visual contact sheet and November card inspected. `QA_REPORT.json` records pending real keys/share URL.
- New docs/tooling: `Docs/OUTREACH.md`, `Tools/steam_art/outreach/prepare_media.py`, `build_bundle.py`, `verify_bundle.py`. No Unity compile or runtime tests needed for external packaging/text.
## 2026-10-18 — outreach service options (read-only comparison)

- Checked official pages for Mailmeteor (personalized Gmail/Sheets merge fields), Keymailer/Partnier (free inbound workflow, paid plans from $24.99/mo; outreach scope requires checking), Lurkit (creator CRM, key campaigns and Shorts/TikTok; pricing by quote) and PressEngine (free Starter key management, paid outreach Lite from GBP300/mo excluding tax).
- Recommendation for the already prepared 12 direct-email recipients: Mailmeteor plus per-row key/personalization fields; the 8 private forms/DM/contact-discovery cases still need their own route. Dedicated creator platforms are an option for broader discovery and coverage tracking. No signups, purchases, connections, uploads, keys or message sending performed. Sources: mailmeteor.com/products/google-sheets, mailmeteor.com/pricing, partnier.com/pricing, account.keymailer.co/allfaq, lurkit.gg/pricing, lurkit.gg/blog/paid-quests-for-shorts-and-tiktok-are-here-release-notes-december-2024, pressengine.net/pricing.
## 2026-10-19 — first eight keys inserted; Gmail drafts and developer form prepared

- User supplied a Drive folder and eight Steam keys. Imported only into PRIVATE_DO_NOT_UPLOAD, assigned in agreed first-wave order (Joe, CGDannyB, Becsters, steamexplays, Cozy Tea Games, Cozy Gaming Club, JustKaalin, CozyReni). Eight unique keys, no substitutions or activation; remaining 12 recipients unassigned. No key values in this log/docs/public pack.
- Drive UI shows the uploaded PUBLIC_REFLUFF_CREATOR_KIT structure; share dialog confirms Anyone with the link / Viewer. No settings or cloud files changed. URL stored in private campaign config and letters.
- In existing Gmail account denisvaho@gmail.com, created seven single-recipient unsent drafts via browser; each body checked for exactly its own key and media URL, then saved and closed. Search in:drafts subject:Refluff confirms 1–7 of 7. Developer Contact Form at cgdannyb.com/contact filled privately with its own assigned key and reply address, not submitted. Browser tabs retained for review/continuation; screenshot in private folder.
- Full verification rerun: 8 videos decode and retain correct sizes/frame counts/audio; 0 media failures. Public ZIP CRC/SHA256 and exclusion of actual assigned keys pass. Unique/stable assignment and tracker-persistence fixtures pass. FIRST_WAVE_READY.md and TRACKER.json record browser draft/form state. No external recipient messages sent; explicit send instruction pending. No runtime/Unity changes.
## 2026-10-19 — studio sender vahovgames; new drafts awaiting explicit approval

- User added vahovgames@gmail.com and explicitly requested copies of the seven drafts in that account, with sending only after their approval. Verified the new Gmail account and no prior Refluff drafts, then created seven separate drafts; checked recipient, personal intro, original own key and media URL before saving/closing. New account search confirms seven saved drafts.
- CGDannyB private developer form reply-to changed to vahovgames@gmail.com; checked the address visually (browser DOM redacts email-field values), original assigned key retained. Form not submitted. New Gmail/form screenshots saved only under PRIVATE_DO_NOT_UPLOAD and tabs retained.
- Updated private campaign sender, local .eml From headers, TRACKER.json, FIRST_WAVE_READY.md, QA approval state and OUTREACH.md. Targeted verification passes for seven .eml sender/recipient/key bodies; key_assignments.json SHA256 unchanged. Old personal-account drafts remain untouched and are recorded as superseded/do-not-send. No message sent, form submitted, key activated, sharing changed or game/runtime modified. User send approval remains pending.
## 2026-10-19 — first eight outreach messages sent after explicit user approval

- User explicitly said to send. Sent seven existing, reviewed drafts from vahovgames@gmail.com (CozyReni, JustKaalin, The Cozy Gaming Club, Cozy Tea Games, steamexplays, Becsters, Indie Game Joe). Before each click verified account, recipient, its assigned key, absence of other recipients keys and the media link. Gmail displayed Message sent for all seven; final in:sent subject:Refluff search confirms 1–7 of 7 and no remaining studio drafts.
- Submitted the existing CGDannyB Developer Contact Form with reply-to vahovgames@gmail.com and its original assigned key. Page confirms Message Sent!. No CAPTCHA or additional approval required.
- Persisted each send result immediately to private SEND_RESULTS_2026-10-19.json, then marked first eight SENT in tracker with Moscow UTC+03:00 timestamps. Added FIRST_WAVE_SENT.md, final Gmail/form screenshots and updated QA/preparation status, docs and consumed first-wave authorization. No key values in this log or public assets. Old personal-account drafts left superseded and unsent; no second/third-wave messages or key activation.
- Adjusted local package reporting to retain actual sent state on rebuild, and refreshed tracker Markdown from the persisted results. Targeted verification: 8 unique recipient results, 8 SENT rows with timestamps, 12 remaining rows unsent. Public media unchanged; no Unity/runtime changes. Sending provider confirmations do not establish recipient reading/delivery. Follow-up files remain manual; no monitor or scheduled send created.

## 2026-10-19 — creator outreach process and tools research

- Completed an informational report in `reports/Работа с авторами об играх.md` with three research notes in `research_notes/Работа с авторами об играх/` (`workflow.md`, `tools.md`, `keys_metrics.md`). The coordinator read all notes and the finished report; source references use Markdown links and contain no actual keys.
- Checked official Steamworks documentation and service pages for creator outreach, key distribution, campaign measurement and current tool capabilities. Recommendations cover preparation, relevant creator selection, personal pitches, manual follow-up, a single contact/status register, private key accounting and the distinction between organic access and paid integrations; a gifted key does not establish an agreed publication.
- Campaign state retained from the previous send log: 8 messages sent on 2026-10-19 (7 studio Gmail messages and 1 developer form), 12 remaining contacts unsent. This research session created no new sends, keys, registrations or schedules and made no Unity/runtime changes. No new delivery, reading, response or publication claims; earlier sending-provider confirmations remain the only recorded send evidence.

## 2026-10-19 — new-carpet balance: minimum 50% dust / 20% crust coverage

- User requested every new gameplay carpet to start at least 50% dusty and 20% covered in dried-mud crust, with no Unity checking. Natural rolls now floor dust/crust mix coefficients at 0.5/0.2; negative-level resets also enforce actual visible coverage, including the session's first carpet, R/HUD reset and handover.
- `K_Reduce` adds visible dust (`grit > 0.04`) and crust (`crust > 0.1`) cell fractions in spare stats slots 12/13 (x256 fixed point). Buffer length 256, histogram offsets and UAV bindings are unchanged. If the initial mask falls short, at most eight initialization retries raise the original profile's level/crust toward 1, stopping when both area minima pass. Stock dust tuning guarantees the final fallback; retries may add initialization latency, not measured in Unity.
- Explicit forced-level/profile staging and `FullySoiled` retain previous behavior. Updated the offline preview to use the same natural-roll and coverage adjustment; documented the balance and marked old seed tables as historical in `DIRT_LEVEL.md` / `ARCHITECTURE.md`.
- Offline NumPy check at 384x480: seeds 1, 2, 5, 6, 10, 17, 18, 19, 20, 712, 860, 999 all pass both coverage floors with finite fields; minimum dust coverage 0.631489, minimum crust coverage 0.213466. Seeds 712/860 cover the highest/lowest base level in 1..999. Nine explicit-level cases (seeds 1/5/68 at levels 0/0.36/1) match direct initialization arrays exactly. Read-only code review found no reset-route, stats-layout or new binding issues.
- No Unity tools/CLI calls, imports, compilation, Play-mode tests or screenshots this session, as requested. Rendering and runtime behavior remain for the user's check.

## 2026-10-19 — remaining unpaid editorial outreach sent

- User authorized sending the remaining unpaid submissions and supplied 23 fresh keys. Validated uniqueness/no overlap, reserved 11 without changing the original eight assignments, then sent 10 individual Gmail messages from vahovgames@gmail.com: Cozy K, Payton'sCorner, Papa Pupp, Eeowna, ChromaGlitch, Eggmilk, Manechiku/unauyo, Jestr, JehNotJay and JubileeWhispers. Each body verified against its own key, recipient and public media link before sending; Gmail showed Message sent for each, and final Sent search shows 17 Gmail messages including the prior seven.
- Verified previously missing emails on creators' official Carrd, WordPress, linked website and YouTube descriptions. Reddit -> YouTube/Linktree links establish JehNotJay's identity. The user completed YouTube/Reddit CAPTCHAs; Jubilee's exposed YouTube business email was then used. Jestr sent to the official team business email with greeting to Mahdeen/team. No private keys in public comments or social posts. All second-wave messages explicitly request unpaid editorial consideration; no paid service ordered or posting commitment assumed. Tripo-generated tool heads explicitly disclosed to Jestr and JubileeWhispers.
- Excluded ImJustJarrod due to published paid rate. Excluded 2BRO because the official YouTube About asks publishers not to email Steam keys. Total original list: 18 provider-confirmed sends (17 Gmail + 1 CGDannyB form), 2 excluded; no eligible listed contact remains pending. Of 23 new keys, 10 sent, 12 unassigned and 1 reserved/not transmitted for 2BRO. Original eight key allocations unchanged. No key values in this log or public assets.
- Saved each send immediately in private SECOND_WAVE_SEND_RESULTS_2026-10-19.json, exact sent letters in SECOND_WAVE_SENT_LETTERS/, final Gmail screenshot, SECOND_WAVE_SENT.md and KEY_POOL_STATUS.json; refreshed tracker timestamps/status, QA/preparation summaries, contact routes and OUTREACH.md. Kept obsolete DM/form records under superseded_contact_routes, marked do-not-send. Helper preserves SENT history/exclusion state and derives configured email-copy count (17). Targeted checks: 10 unique new confirmations, exactly one own key per actual letter/no other recipient keys, correct sender/recipient/media URL, 19 unique stable assignments, 18 SENT + 2 EXCLUDED. Public media unchanged; existing full-decode QA retained. No Unity/runtime changes, automated follow-ups or monitoring. Provider confirmation does not establish reading, activation or publication.

## 2026-10-19 — outreach documentation consolidated at the user's request

- Reworked OUTREACH.md to lead with the current completed campaign instead of obsolete preparation/pending-approval statements: 18 submissions sent (first 8 + next 10; 17 Gmail + 1 CGDannyB form), sender vahovgames@gmail.com, 2 excluded and their reasons. Documented 31 supplied keys: 18 transmitted, 12 unassigned, 1 reserved/not transmitted for 2BRO; no key values copied into project documentation.
- Added direct links to the private tracker, both wave results and final Gmail screenshot, with paths to exact sent copies and key-pool counts. Clarified that local .eml copies are not pending Gmail drafts, SENT/EXCLUDED must not be resent, follow-up is manual from 26 October if unanswered, and provider confirmation does not establish reading or publication. Earlier preparation history remains in this session log and private wave records.
- Added navigation/status entries to AGENTS.md and SteamStore/README.md so future project sessions can find the campaign record and public/private package boundary. Verified all 5 local Markdown links in these three updated files resolve, totals match TRACKER.json / KEY_POOL_STATUS.json, and none of the 31 supplied keys appears in those documents. Documentation only: no additional emails, browser actions, scheduled tasks, media rebuilds or Unity/runtime changes.

## 2026-10-19 — Steam release built and uploaded after balance change

- User requested a release build, then explicitly requested Steam upload. Used the live Unity Pipeline and the existing `CarpetCleaning/Steam/Build Windows (release, for upload)` menu with Play stopped. `[SteamBuild] OK`: 210 files, 556.6 MB (583,618,823 bytes), 42 seconds, bundleVersion 0.9.1, output `Builds/Steam/content/windows`; symbols kept outside the depot in `Builds/Steam/symbols/0.9.1`.
- File checks passed for Refluff.exe, UnityPlayer.dll, MonoBleedingEdge, Refluff_Data, steam_api64.dll, CREDITS.txt and THIRD_PARTY_NOTICES.txt; recursively no steam_appid.txt or DoNotShip folders. Exe icon checker: all six comparable frames exactly match the art. Unity reports no console errors / no compilation failure; build emitted shader warnings in CarpetFoam (potentially uninitialized FieldAt/FAt). No gameplay tests, Play-mode entry, player launch or code changes in this build session.
- Opened the project's interactive `upload_window.ps1` for the user to authenticate, per STEAM.md 7.2. Description `Refluff 0.9.1 2026-10-19: minimum dust 50pct and crust 20pct`. Fresh status `exit=0`; app log confirms **BuildID 26044364**, AppID 5304930, completed 12:11:29 Moscow. Depot 5304931 log confirms manifest **5526638028200978727**, 8 changed files / 5.20 MB against baseline 8316280809583961206, 13 missing chunks uploaded.
- Upload only (`SetLive ""`): no branch assignment or Steamworks publication by the agent. Updated STEAM.md 7.3 with the result. Authentication cache stayed local; no credentials copied into docs or build content.

## 2026-10-19 — new YouTube shortlist and private unsent preparation

- Researched and synthesized 20 new YouTube candidates for complimentary Steam keys and voluntary unpaid editorial reviews. The Russian report links specific creator-controlled videos/posts and distinguishes seven verified initial email contacts, the separate Her Cozy Gaming form, contact holds and reserve/policy-review cases. Old video dates are evidence of fit rather than claimed latest activity; Sarah's current official stream and the final About checks override earlier research holds where applicable. Grillmastah's address was copied by the user from the official About after CAPTCHA (VERIFIED_USER_ABOUT), not directly observed by the agent.
- Preserved all previous campaign assignments and 18 SENT / 2 EXCLUDED records; no duplicate old creator identity enters the new 20. Saved 18 additional user keys only in the new PRIVATE_DO_NOT_UPLOAD intake: valid format, unique within intake, zero overlap with 31 prior keys, all AVAILABLE/unallocated. Combined counts: 49 supplied, 18 transmitted, 30 available, 1 reserved/not transmitted. No values in report, notes, drafts or documentation.
- Created private CANDIDATES.json with stable IDs, evidence/contact sources, status, proposed_wave and sent_at=null. Wrote seven initial local email texts plus one official-form text, each with its own key placeholder and individual youtube_20261019 UTM link; sender vahovgames@gmail.com / Vahov Games. Ari remains POLICY_REVIEW with required future Tripo disclosure; no AI-free claim. Nema remains on contact hold; СТОДНЕВКА's separate Telegram route was not used.
- QA checks candidate/draft counts, old-identity exclusion, placeholder uniqueness, no actual supplied keys outside the private intake, valid local links and unchanged original private tracker/assignments/pool hashes. Saved sanitized QA_REPORT.json and START_HERE_RU.md. Key access to the intended pre-release build was not tested; confirm the supplied key type before later unique allocation and sending. No new email, service draft, form/DM submission, activation, scheduling, media rebuild or Unity/runtime change.

## 2026-10-19 — seven YouTube emails and Her Cozy Gaming form sent

- The user explicitly instructed sending the selected seven emails and Her Cozy Gaming form, each with a separate key and materials, then confirmed access before release (USER_CONFIRMED_PRERELEASE_ACCESS). Reserved eight stable assignments from the new 18-key pool, leaving old assignments untouched. The root browser operator sent seven individual Gmail messages from vahovgames@gmail.com: Grillmastah, CozyGameNight, Kanzalone, Z1 Gaming, Kimchica, Sarah Sunstone and Сестра Винчестер, plus the official Her Cozy Gaming form. Each own key, recipient, media URL and personal intro was checked; texts retain optional unpaid editorial coverage with no posting/positive-review obligation.
- Saved each outcome immediately in private SEND_RESULTS_2026-10-19.json with exact Moscow timestamps, key IDs and evidence paths, no key values. Six Gmail Message sent confirmations were captured; Grillmastah's transient first toast was missed, but a new Sent match after an empty pre-send search and final 1–7 of 7 exact-recipient search confirm submission. Do not resend. Her form showed “Submission Success! I will be in touch shortly.” Email DOM was masked; operator supplied the exact studio reply address and server accepted/cleared the form. These are submission confirmations, not proof of delivery, reading, activation or publication.
- Reconciled eight candidates, new key entries, assignments and actual payload copies to SENT with exact ledger sent_at. New pool: 8 transmitted + 10 AVAILABLE. Original pool/history remain 31 supplied, 18 transmitted, 12 available, 1 reserved/not transmitted for excluded 2BRO, and 18 SENT / 2 EXCLUDED. Combined: 49 supplied, 26 transmitted, 22 available, 1 reserved; 26 total submissions = 24 Gmail + 2 forms. Actual key values and sent letters remain private; no agent activation or new Steamworks inspection.
- Updated Russian report, START_HERE_RU.md, SENT_SUMMARY_RU.md and OUTREACH.md to actual status; preparation QA is marked historical, current SEND_QA_REPORT.json links final Gmail/form evidence. Targeted QA: eight unique results and keys, 7 email + 1 form, only own key per exact letter, no placeholders, correct media/individual UTM, unchanged original private hashes and no keys in docs. The preparation generator refuses to overwrite allocated state. No other creator sends, scheduling, follow-up automation, monitor, media rebuild or Unity/runtime changes. Optional one manual follow-up no earlier than 26 October if unanswered.


## 2026-10-19 — five original cartoon rug designs and Editor-only Reset button

- User requested five bright, cute cartoon designs, inclusion in the carpet picture pool, and hiding the Reset button in release players while retaining it in the Editor. User explicitly requested no Unity verification.
- Generated five independent illustrations with the built-in imagegen tool, without reference images or franchise characters: orange fox in a flower meadow, pink axolotl underwater, mint dragon hugging a star, capybara with an orange, and lavender kitten with yellow yarn. Saved unchanged native 1122 x 1402 RGB PNGs in `Textures/Resources/CarpetPictures/rug_cartoon_{fox,axolotl,dragon,capybara,cat}.png`. Added stable texture/folder metadata: NPOT scaling disabled, sRGB, mipmaps and clamp wrapping. Exact prompts, generation mode, original filenames and SHA-256 hashes are in `Tools/rug_art/cartoon_rugs.json`. These are generated originals requested by the user, not claimed museum open-access / CC0 assets.
- `CarpetHandover.Awake` merges `Resources.LoadAll<Texture2D>("CarpetPictures")` into the serialized list before material setup, preserving existing entries and appending new designs in ordinal name order. Asset-reference deduplication handles a later Build Scene that already includes the resources. Existing scene's five museum rugs plus five additions give a ten-design pool without editing SampleScene or running the builder. Awake clears selection bookkeeping and re-identifies the current material picture when reloads are disabled. Updated HANDOVER.md, ARCHITECTURE.md and the texture-pool entry in AGENTS.md.
- `CleaningHUD.Build` keeps Reset binding under `UNITY_EDITOR`; players remove the named `reset-wrap` with its shadow and layout space before collecting UI blockers. Development players also hide the button. The R shortcut is unchanged. Updated the UXML wrapper and HUD.md; no new localization strings.
- Offline verification only: visually inspected all five generated images; each PNG is readable, opaque RGB, 1122 x 1402, distinct and byte-identical to its generation source; all five texture GUIDs are unique and metadata settings match. Counted ten image files in the picture tree and parsed the HUD XML, confirming one wrapper containing Reset. Independent source review found no blocking issue in the merge or HUD branches. No Unity tools/CLI calls, imports, refreshes, compilation, Play-mode checks, builds or game screenshots; runtime and rendering verification remain for the user.


## 2026-10-19 — cartoon rug release built and uploaded to Steam

- User explicitly requested a release build and Steam upload after the five new cartoon rug designs / Editor-only Reset change. Used the connected Unity 6000.7.0b1 Pipeline with Play already stopped and the existing `CarpetCleaning/Steam/Build Windows (release, for upload)` menu. No gameplay/Play-mode or player smoke tests.
- `[SteamBuild] OK`: version 0.9.1, 211 files, 586.6 MB (615,073,531 bytes), 30 seconds, output `Builds/Steam/content/windows`; symbols outside the depot in `Builds/Steam/symbols/0.9.1`. Console reports zero errors, no compilation failure. Existing CarpetFoam warnings about potentially uninitialized FieldAt/FAt remain.
- Independent read-only output QA passed: required executable, UnityPlayer, steam_api64.dll and both legal files present; no steam_appid.txt, PDB or DoNotShip/backup paths. Exe icon check OK, all six authored sizes pixel-identical. All five cartoon resource names found in the built resources.assets; no scene rebuild or hand-edit was needed.
- Opened the project's interactive `upload_window.ps1` for the user's Steam authentication, per STEAM.md 7.2. Description `Refluff 0.9.1 2026-10-19: 5 cartoon rugs and Editor-only Reset`. Fresh status `exit=0`; app log confirms BuildID **26046043**, AppID 5304930, completed **13:31:40 Moscow**. Depot 5304931 confirms manifest **8343041185643269779**: against baseline 5526638028200978727, one 30.00 MB resources.assets.resS file added, five files changed (2.31 MB), 36 new chunks uploaded.
- Upload only: SetLive stayed empty, no branch assignment or Steamworks publication. Updated STEAM.md 7.4. Authentication cache remained local; no credentials copied into documentation or build content.

## 2026-10-19 — gameplay timelapses, original and vertical, with Hand Over focus

- User requested 30 s and 60 s edits of the root OBS recording `2026-10-19 14-07-18.mp4`, ending at source 09:06, retaining sound, with both original and 9:16 versions. Delivered four root MP4s named `2026-10-19 14-07-18_timelapse_{30,60}s_{original,9x16}.mp4`: originals 2560x1600; verticals 1080x1920; all H.264, BT.709 limited range, 60 fps CFR, AAC stereo 48 kHz / 192 kb/s, faststart.
- To make the requested button focus readable, source 00:00-08:58 occupies 22/52 s (approximately 24.45x / 10.35x), followed by the final eight source seconds at normal speed. A smooth 5x bottom-right zoom starts at 08:58.8, reaches full focus at 09:00.3, holds across the Hand Over activation near 09:00.95, and returns to the base frame over 09:01.0-09:01.8. The payout remains visible through the last source frame at 09:05.9833; no frames from 09:06 onward are included.
- Vertical versions retain the full rug in a central 1080x864 foreground over a blurred 9:16 background. A central 2000x1600 source ROI shifts right during the button focus so the complete Hand Over button, including the H key chip, remains visible during the hold. Visually checked source, focus previews and final contact sheets; independent read-only review found no blockers in timing, crop or sample counts.
- Original sound follows the edit using chained pitch-preserving atempo filters in the accelerated portion and normal-speed audio in the final eight seconds. Recording audio was very quiet (exported integrated loudness -38.38/-38.72 LUFS), so normalized both timelines to measured -16.1 LUFS / -1.3 dBTP after AAC, with an 80 ms final fade. Audio was remuxed into both aspect ratios without video re-encoding; encoded video packet SHA-256 hashes match before/after for all four files.
- Verified exact 30.000/60.000 s durations, 1800/3600 frames, correct dimensions/color, audio presence, and successful full video/audio decode for all four exports before audio remux. Native final-frame comparison against the last source frame gives 53.11 dB PSNR. Final remux checks confirm unchanged video packets, exact durations/frame counts and the normalized audio. Reproducible render/normalization scripts, filter graphs, commands, source/final contact sheets, decode logs, `render_report.json`, `verification.json` and `audio_normalization.json` are under `SteamStore/_work/timelapse_20261019_140718/`.
- Source recording left unchanged. No Unity/runtime changes, Editor calls, builds, uploads or publication.
## 2026-10-21 — temporary user-picture-only carpet pool

- User requested adding the attached desktop `test.png` and temporarily hiding all other carpets. Added `Textures/Resources/CarpetPicturesOverride/rug_user_test.png`; the ten previous assets remain unchanged. Source was actually WebP under a PNG extension, so converted to a real RGB PNG, 850 x 973, with byte-identical decoded pixels. Unity import: NPOT None, mipmaps, sRGB, clamp, max size 2048. Initial failed imports were resolved by the format conversion; later console error query returned zero entries.
- `CarpetHandover.ActivePictures` uses the sorted nonempty override Resources folder, while preserving the full serialized/resource catalogue. First material validation, reset and handover selection use that active array. Builder's first material also honors the override; debug JSON reports active count. Updated the material through the live Editor, plus HANDOVER.md, ARCHITECTURE.md and AGENTS.md. Move the new image through Unity to normal `Resources/CarpetPictures` and restart Play to restore all designs.
- Pipeline CLI was unreachable; existing UnityMCP connection controlled the live 6000.7.0b1 Editor instead. C# compilation completed; active texture imported at 850 x 973, override count 1. Play check: first image, three resets and one forced handover/new-carpet cycle all selected `rug_user_test`; active count 1, retained catalogue count 10 in the existing scene. Independent read-only source review found no blockers. No scene rebuild, player build, upload or screenshot.
- The forced handover test unintentionally persisted an Editor-only economy payout: $89,950 -> $90,150 (+$200), delivered count 11 -> 12, lifetime earnings +$200, perfect-dry count +1. The rule also writes bestSeconds=0 for this zero-second test; its previous value was not snapshotted. An attempted reversal was rejected by automatic safety review because no pre-test backup existed; the rejected call did not run, so no rollback, clean staging or screenshot occurred. User was informed; do not assume the save was restored. The user then requested ending validation to check personally; stopped our Play session and performed no further validation.


## 2026-10-21 — automatic light/dark carpet backing

- User requested white backing when a carpet picture is mostly light, and the existing black backing for mostly dark pictures. Added shared `CarpetPictureBacking.Choose`: 64 x 80 GPU copy of the visible cover crop, authored-sRGB brightness threshold 0.5, alpha-weighted strict majority. White `(1,1,1)` for a light majority; existing `(0.13,0.12,0.11)` for dark, ties, missing/fully transparent artwork. Imported image Read/Write remains disabled.
- Runtime applies `_BackingColor` to the initial recognized material and each new picture. Instance cache avoids repeated readbacks and is cleared in Awake for disabled scene/domain reload. Builder uses the same rule. Temporary readback textures/RTs and graphics state are cleaned/restored in finally. Minified shell-0 fallback retains antialiasing: full print brightness for white backing, previous half brightness for dark; dirt/stain/crust shading is unchanged.
- Added BACKING_TONE.md and metadata, updated HANDOVER.md, ARCHITECTURE.md and AGENTS.md. Independent read-only source review found no blockers in cover mapping, sRGB/readback path, alpha votes, first/next-picture wiring, cache or cleanup. Offline whole-image reference: user picture about 96.5% light, all five cartoons light (62.6-74.9%), all five museum rugs dark (0-28.1%); runtime crop/filtering may shift exact percentages.
- Honored the user's instruction to verify personally: no agent-requested Unity calls, imports, refreshes, compilation, Play tests, screenshots, player builds or uploads for this feature. Rendering and compilation remain for the user's check. No economy/save modifications; the earlier pool-test save side effect and rejected reversal remain as recorded in the preceding entry.

## 2026-10-21 — backing repeats the local carpet picture

- User refined the backing rule: match the nearby fibre/picture colour at each location, instead of choosing one white or black backing per image. Removed the shader's shell-0 colour override and distance-dependent brightness fade. All shells now use the existing local `_PictureTex` albedo, alpha fallback and cover fit. At t=0 strand-root UV equals carpet UV, so the backing stays fixed while bent fibres preserve their root colours.
- Existing backing occlusion (AO 0.45), normal lighting and downstream mud/stain/crust/wetness shading remain. The backing can appear naturally darker in gaps while its underlying artwork colour matches the fibres. No added texture samples or GPU readbacks.
- Removed the superseded CarpetPictureBacking.cs + metadata, Handover cache/application code and builder call, plus `_BackingColor` shader property/uniform. Existing serialized material colour is harmless unused data; no material/scene YAML edits. Updated BACKING_TONE.md, HANDOVER.md, ARCHITECTURE.md and AGENTS.md. The single user-image pool remains active.
- Read-only source review found no blockers in local UV/root mapping, shared sample, removed classifier references or downstream dirt/wet/lighting behavior. Honored the user's request to check personally: no Unity calls, imports, refreshes, compilation, Play-mode tests, captures, builds, uploads or save modifications for this change. Final rendering/compilation remain for the user's check.

## 2026-10-21 — backing follows the fibres' local dirt and cleaning

- User requested dirty backing under locally dirty fibres and clean backing after local cleaning. Source inspection found stains/mud/crust/dirty water already shade shell 0 from the live shared simulation fields; dust and crumb masks were instead forced to zero by their shell-height smoothsteps at t=0.
- Added `looseSoilHeight = t <= 0 ? 1 : t` only for the two loose-soil height masks in CarpetFibers.shader. Backing now receives full tip-level dust/crumb coverage from the same local `_Dirt.w`, colours, thresholds and per-strand variation. Both contributions vanish at zero grit; changes made by the existing vacuum/water simulation therefore reveal the backing print together with the fibres. Every t>0 fibre mask and the existing mud/crust geometry, wet shading and occlusion remain unchanged. No extra texture samples, RTs, compute channels or independent backing-dirt state.
- Updated BACKING_TONE.md, DIRT_LEVEL.md section 4, ARCHITECTURE.md, HANDOVER.md and AGENTS.md. Read-only source review covers zero-dirt/positive-dust behavior, shared live UV/dirt sampling and unchanged positive-height fibre rules.
- User is checking personally: no Unity calls/imports/refreshes, compilation, Play-mode tests, screenshots, builds, uploads or save changes. Actual shader compilation and appearance remain for the user's check.

## 2026-10-21 — gameplay carpet pile increased from 24 to 48 layers

- User requested 48 fibre shell layers instead of 24. Added `CarpetRenderer.DefaultShellCount = 48` and used it for the renderer default and both builder count writes; shader default is 48 too. Layer count includes shell 0, as before. Existing scenes serialize 24, so Awake migrates that legacy value to 48 before building the shell mesh; no scene YAML edit or scene rebuild is required. Change takes effect on the next Play/player initialization.
- Handover sets `_ShellCount` on its cloned runtime material from the renderer's actual count. The existing material asset's old serialized value is otherwise unused by the shader and will be rewritten by the updated builder if it is run. Kept pile height 20 mm, shell grid 96 x 120, normalized height distribution and all simulation settings unchanged. At 48 layers the source geometry gives 563,376 vertices / 1,105,920 triangles versus half that at 24; runtime performance was not measured.
- Updated current counts in ARCHITECTURE.md and AGENTS.md; qualified STORE_ART.md's old 24-layer gameplay comparison as historical capture context. Source review confirms the mesh uses the migrated field and UInt32 indices already support the larger mesh.
- Honored personal verification preference: no Unity calls, imports/refreshes, compilation, Play-mode tests, screenshots, builds, uploads or save edits. Appearance/performance and compilation remain for the user's check.

## 2026-10-21 — 60-second portrait timelapse from the user's OBS recording

- User requested a 60 s 9:16 timelapse of `C:/Users/denis/Videos/2026-10-21 16-22-46.mp4`, then clarified that the final reward scene must retain original speed. Exported `2026-10-21 16-22-46_timelapse_60s_9x16.mp4` in the project root: 1080 x 1920, H.264 High / yuv420p / BT.709, 60 fps, AAC stereo 48 kHz, faststart; 117,485,693 bytes. Source recording remains unchanged.
- Source 0-418.35 s occupies output 0-53 s at approximately 7.8934x. Source 418.35-425.35 s occupies the final 7 s at 1x, including handover and the complete recorded reward animation. A central source ROI (1260 x 1080 at x=330) preserves the full visible carpet width, centered over a slightly darkened blurred 9:16 background. No captions, music, invented reward values or gameplay changes.
- OBS's MP4 contains 25,521 video packets but only 25,520 displayed frames: one CTTS presentation timestamp is exactly on the exclusive edit-list boundary. Held the last displayed frame for one 1/60 s tick to fill that missing terminal tick. The output is exactly 60.000 s / 3600 frames with regular timestamps, without changing the real reward frames' 1x cadence.
- Accelerated audio uses chained pitch-preserving atempo; reward audio retains original speed. Two-pass loudness normalization yields measured -16.1 LUFS / -1.3 dBTP after AAC. An independent MP4 audit found initial atempo/loudnorm timestamp jitter; regenerated only AAC with an explicit sample-count clock (`asetpts=N/SR/TB`) and remuxed, preserving the encoded video packet SHA-256. Final audio STTS is 2813 x 1024 + 512 samples, with standard 1024-sample AAC priming and a 60 s edit.
- Verification: full video/audio decode, CFR (VFR=0), no black intervals, visual source/final contact sheets, and five distinct 1x-tail frame comparisons against the corresponding source moments (41.46-43.62 dB PSNR). Final remux audio decoded successfully; independent container audit confirms 3600 regular video samples, SAR 1:1, exact 60 s movie/video/audio edits and faststart. Scripts, filters, commands, logs, contact sheets and JSON reports are in `SteamStore/_work/timelapse_20261021_162246/`. No Unity calls, scene/runtime edits, builds, uploads, publication or save changes.

## 2026-10-21 — narration and centered upper subtitles for the 60-second timelapse

- User requested adding their supplied Downloads MP3 at the start and subtitles at the top, strictly centered horizontally. Created a separate `2026-10-21 16-22-46_timelapse_60s_9x16_vo_subs.mp4` in the project root (112,780,577 bytes); kept the previous export and original video/MP3 unchanged. Format remains 60.000 s, 1080 x 1920, 3600 frames / 60 fps, H.264 High / BT.709, AAC stereo 48 kHz, faststart. Final reward scene remains output 53-60 s at 1x.
- Placed the 6.008 s supplied read at t=0 with original timing and pitch. Local offline faster-whisper base.en provided word timings; six independent cropped passes confirmed the spoken ending “soft and touchy again.” Five burned-in English caption cues cover 0-6 s. Arial Bold 64 px, white with dark outline/shadow, one or two lines in the blurred upper area. Each line is centered independently at x=540; independent alpha-mask measurements confirm all seven actual glyph lines within 0.5 px. Outlined/shadow bounds x=215-864, y=206-372, fully above the gameplay ROI starting at y=496. Caption transitions match word boundaries within 60 ms. Editable SRT, transparent PNGs, exact layout and ASR JSONs retained.
- Applied constant -2 dB narration gain to keep its true peak at -3 dBFS; ducked game sound 10 dB during narration and restored it smoothly over source-read end +0.10 to +0.55 s. The 60 s mix is built on a 48 kHz sample clock, followed by a peak limiter; final measured loudness -16.4 LUFS / -1.4 dBTP. Waveform correlation verifies narration alignment at exactly zero samples of offset (correlation 0.928 with the source despite the game bed/AAC).
- Verified full A/V decode, regular CFR timestamps and no black intervals; visually inspected all five caption states, disappearance after narration, and the reward scene. Same-time comparisons against the previous timeline at 6.5, 53 and 59.5 s give 51.01-55.59 dB PSNR, confirming retained edit timing. Reproducible render/mix/verification scripts, filters, logs, contact sheet and reports are in `SteamStore/_work/timelapse_20261021_162246/voiceover/`. No Unity calls, game/UI/localization edits, builds, uploads or publication.
- Independent final-mix ASR recovered the entire supplied read verbatim through 5.84 s; independent visual/layout and container checks passed. AAC has standard priming and a 60 s edit; an isolated mutually compensating 1023/1025-sample timestamp pair differs by only one sample (20.8 microseconds), with no material synchronization issue.

## 2026-10-23 — between-fibers short with Syntx narration

- User requested a macro-to-wide story, two Russian spoken lines, exactly two seconds of dry-vac action and future TikTok/Reels/Shorts publication after approval. Generated the 5.2 s source in Syntx Eleven v4 / Agassi Roux - Friendly and Engaging for 0.8 account tokens; original MP3 retained. Extended only the phrase gap, with no speed or pitch change.
- Captured actual Unity Steps: 720 frames / 60 fps / 12 s, pristine Reset(394,0), current user picture. Temporary 384-shell capture mesh replaces gameplay's 48 only during this microscopic free-camera take; physics/strand layout unchanged. Exactly 120 active dry-vac frames at 6.4–8.4 s, then real spring-back. Actual tool display is its suction ring, no 3D dry-vac model. Original camera/light/mesh restored and Play stopped; no game source, scene asset, build or release settings changed.
- Master: SteamStore/tiktok/between_fibers/refluff_between_fibers_v1.mp4, 1080x1920 H264/AAC, 12.000 s, 720 frames, 12,601,540 bytes; SHA256 820cf109104834b0d87fdfde07fa4dd93dea08cfe5d1e1dd1f1d0991b7a774c4. Full decode and frame inspection passed; final AAC -15.90 LUFS / -1.50 dBTP, finite, no clipping. Subtitles and end title verified. ASR supports timing but is imperfect on the second Russian phrase; listening quality awaits owner review.
- Foley rebuilt from the actual take's virtual-clock log, InspectorJ CC BY 4.0 Dyson + crcavol CC0 carpet drag. Required attribution is in all caption files; no music or NC recording. Raw PNG/audio logs retained in SteamStore/_work/clips/between_fibers_20261023_v1; reproduction scripts in Tools/steam_art/tiktok/between_fibers; detailed doc TIKTOK_BETWEEN_FIBERS.md.
- Registered refluff, Clip clip_e5afcde9-ff28-44d1-b4e8-95b87e6c910e, and three local draft posts (IDs/hash in social_studio_records.json). Accounts verified against the real local registry: TikTok/YouTube vahovgames, Instagram vahov_games. Public/immediate after owner approval proposed; no approve/upload/publish/schedule calls. Final video and exact post details presented for review.

## 2026-10-23 — between-fibers short revision 2: traditional red rug

- User requested an ordinary red rug image. Selected bundled CC0 rug_transylvanian_medallion.jpg (classic red field, central medallion and cream ornamental border); re-shot the same720-frame/12-second timeline to SteamStore/_work/clips/between_fibers_20261023_v2_red. Selected the texture and cover fit only on the runtime material, then restored them; picture override folder and shipped catalogue/settings unchanged. Original camera/light/mesh restored and Play stopped.
- Latest master: SteamStore/tiktok/between_fibers/revision2/refluff_between_fibers_red_v2.mp4, 29,910,020bytes; SHA256 330973c31bdd1c79d6f828473bb8b16547438e8eb40eb74cf5c9f82ba17bf601. Same Syntx narration (aligned WAV byte-identical), captions,2-second dry-vac pass and recovery. v1/raw retained; no new voice generation.
- Raw capture0errors,pendingPng0,720frames/steps. Final1080x1920/60fps,H264/AAC,12.000s/720frames,full decode passed,-15.90LUFS/-1.50dBTP,finite/no clipping. Source and final sheets inspected; suction and recovery read clearly on red/cream/teal pile. Details in TIKTOK_BETWEEN_FIBERS.md and revision2/qa.
- Updated the existing SocialStudio Clip to v2; same three posts/captions/accounts remain draft,approvedHash=null,url=null. No approve/upload/publish/schedule calls. Presented corrected video for owner review.

## 2026-10-23 — between-fibers revision 3: English narration, logo and November CTA

- User made English narration a standing rule and requested actual logo, Wishlist on Steam and a November release line. Saved the language preference in TIKTOK.md and the shared SocialStudio.md guide. November is the owner's explicit current direction for this video; no Steamworks release setting or unrelated release checklist changed.
- Syntx Eleven v4 / A Carlisle - Charismatic Storyteller (English filter) generated a6.240s read for0.8 account tokens: Hmm… where am I? Wait… I'm between the carpet fibers… in my own game! Original MP3+metadata retained in revision3/voice. Source and assembled English ASR matched every word. Original spoken speed/pitch retained; only0.26s near-silence after Wait removed, narration ends6.34 before real dryvac6.4–8.4.
- Reused red v2 raw take0–12 unchanged and held its actual last frame12–14. Packshot11–14: approved SteamStore/logo/refluff_logo.png plus Wishlist on Steam and Coming this November in the upper tile area, short fade/dark gradient. English subtitles replace Russian ones. No Unity calls/capture or game assets/settings/save edits this session.
- Latest master: SteamStore/tiktok/between_fibers/revision3/refluff_between_fibers_en_v3.mp4,14.000s/840frames,1080x1920/60,H264/AAC,30,730,142bytes; SHA2560ca66e84f524d5102f93fbdb13b41484bc2ddc1614b31e1cb18f030b0b76b692. Full decode/frame QA passed; final AAC-15.70LUFS/-1.47dBTP,finite,no clipping. Checked English cues, preserved pile action, actual logo/CTA/November text and hold. Scripts/reports/source/previous masters retained.
- Updated existing Clip file and same three draft posts with English titles/captions, November and required InspectorJ credit. Visibility public, same accounts, immediate after explicit approval; all approvedHash/remote URLs remain null. No approve/upload/publish/schedule. Presented new file and updated details for owner review.

## 2026-10-25 — proposed macro-focused trailer reshoot (planning only)

- User requested a new trailer centered throughout on close views of pile behavior, demonstrated visually rather than explained in words, and suggested the opening of the latest short. Identified the reference as the red-rug between-fibers revision 3 from 2026-10-23; reviewed its documented timing and final contact sheet, the previous trailer storyboard, and the pile/wet-look capture constraints.
- Proposed in chat a roughly 50-second story on one red rug: between-fibers opening, matching cut to dusty pile and dry-vac response, water-front lay, foam/rotary brush, squeegee, extraction, sustained drying/recovery, clean reveal and logo/Steam CTA. This remains a proposal, not an approved shot list. Emphasis is on visible cause and reaction, brief holds after tool passes, and keeping the pile visible at water/foam boundaries.
- Source opening is vertical 1080x1920; suggested repeating its camera setup for a wide trailer using the original recipe rather than enlarging a crop. Read-only review plus this session-log entry; no capture, Unity operations, asset/settings/save changes or publication.

## 2026-10-25 — macro trailer reshot and assembled for review

- Owner approved shooting the proposed close-pile scenario. New review master: SteamStore/trailer/macro_20261025/refluff_macro_trailer_v1.mp4, 50.000 s / 3,000 frames, 1920x1080 at 60 fps CFR, H.264 High / yuv420p / BT.709 TV, AAC stereo 48 kHz. 113,711,946 bytes; SHA256 ec69e9bf749aad350a7284132a2675a5368219197985c6ee467c6c2d2908457f. Previous trailer/master and Steam publication remain unchanged.
- Native horizontal repeat of the latest red-rug between-fibers opening, then dusty-pile suction/recovery, water flow, thin foam/rotary brush, squeegee toward the edge, extraction, nine-second dryer/recovery, clean reveal and actual Refluff logo with Wishlist on Steam. No narration, explanatory titles, music or release date. Camera holds long enough to observe the pile after tools pass; all actions retain original speed.
- Selected takes mf01_intro, mf02_vacuum, mf03_hose, mf04b_brush, mf05_squeegee, mf06_extract, mf07_dryer, mf08_reveal, mf09_end: every SeqEnd errors 0 / pendingPng 0. Raw PNGs/audio logs retained in SteamStore/_work/clips. Original mf04_brush retained; it hid too much pile. Retake uses a short foam sweep offset toward the brush path's far edge, after a completed 720-Step replay of 02+03. Independent contact-sheet review confirms suction lay/recovery, radial water lay, brush-edge action, plow trail and dryer height recovery. Extraction is subtler.
- Temporary rendering samples: 384 shells for the microscopic intro, 96 for other close-ups; gameplay remains 48. Warm raking light and free camera, no added DoF; simulation constants/strand layout/pile height unchanged. The visible cleaning chain uses factory tools. Rest of the same rug was cleaned off camera using actual scripted tools before the final reveal, without a clean-state reset; final clean 0.989596 / wet 0.0011709, no free water, foam or slurry, finite valid state. Exact preparation/tuning in prepare_final.cs.
- Genuine recorded game foley rebuilt from each take's virtual-clock log. Dry-vac motor prelap ~0.5 s; quiet genuine dryer loop carries offscreen into the final card. Hose/brush shots -4 dB avoid excessive limiting: max reduction 4.62 dB, encoded mix -14.0 LUFS / -2.3 dBTP, no clipping, finite decoded samples. No CC BY-NC source; source inventory and AUDIO_CREDITS.txt retained beside output.
- Final check_trailer: 18/18 PASS, including exact duration/frame count, CFR VFR=0, no black frames, codec/colour/audio format, loudness/true peak, title-safe logo/CTA and audio licence exclusion. Final card and source/final contact sheets visually inspected. Cover, stems, reports, delivery.json and review clips are adjacent to master. Recipes/EDL/scoped composer in Tools/steam_art/trailer/macro_20261025; new Docs/TRAILER_MACRO_20261025.md linked from TRAILER.md.
- Unity CLI eval timed out, so Unity MCP execute_code/codedom drove the existing real-Step capture in memory. No runtime source edit, shader change, project recompile, scene save or build. Console records a PlayerLoop recursion diagnostic from MCP's Step invocation; actual captures have regular 1/60 timing and verified evolving simulation. An earlier SESSION_LOG import mtime error was also present; documentation was explicitly reimported after stopping Play. Console history was not cleared.
- Restored original camera, light, mesh, picture/fit and input, removed temporary capture meshes/hooks, stopped Play. Restored both carpet save files from pre-session copies and verified SHA256 equal: editor 6d7286990aae130a7ed497ff9312c7e6f0d148731c59f0d1db239e59faed7e8e; player 22fe4f92a1a2190083daff17f1450e392cfd0ca9bab8f5d812108757d26a0bfe. Private backup files remain local only. No upload, replacement, social registration or publication.

## 2026-10-25 — macro trailer v2: full-rug reveal and gradual soil transition

- Owner requested expanding the opening to show the entire rug, then a smooth return to the existing vacuum framing while the carpet becomes dirty gradually. Captured new mf01b_intro: 9.000 s / 540 real Steps/frames, 1920x1080/60; SeqEnd pendingPng 0, errors 0. First microscopic hold 0–0.25 s, full-rug view reached 3.3 s and held to 4.0 s, return 4.0–8.7 s, final hold to 9.0 s. Full-rug pose (0,1.7,-1.7), target (0,.01,0), FOV43; final pose exactly matches the original vacuum camera.
- Explicitly staged the requested soiling transition in the capture: snapshot target Dirt from seed394/profile(.70,0,.40,.85,.12), hold seed-derived Crust/Bed, scale Dirt 0 to1 with smoothstep between4.2 and8.5 s. Linear RGBAFloat CPU/RT roundtrip and final Dirt vs target have max difference0. Ramp CSV is monotonic, maximum increment0.005814 per frame. Rendering uses384 shells until2.2 s then96 at a distance and through the return. No gameplay source/shader/settings change and no whole-frame dissolve.
- Independent QA confirms all4 carpet edges visible during full-rug hold, smooth buildup during approach, no visible mesh-change pop. Last intro/first vacuum frame mean absolute RGB difference0.000765/255, p99=0; only0.0651%pixels differ at all. Contact sheets and numeric audit in SteamStore/_work/macro_20261025/revision2/qa.
- New master SteamStore/trailer/macro_20261025/revision2/refluff_macro_trailer_v2.mp4:56.000s/3360frames,1080p60CFR,H.264 High/BT.709,AAC48k stereo,127144526bytes; SHA2561369b55e755ffd388ff46c01fc4a0d0673ccb20f54b92be64cddac604d32614b. All18 final QA checks PASS; encoded audio-14.0LUFS/-2.3dBTP, no clipping/black frames. Existing action takes02–09 and their timing/speeds/gains/foley preserved, timeline shifted6s. No voice/music/date. Final13.5s opening preview (810frames,31324448bytes) includes final-master AAC unchanged.
- Own EDL/recipes/composer/README under Tools/steam_art/trailer/macro_20261025/revision2; delivery, credits, cover, sheet, stems and QA beside output. V1 preserved. Restored original runtime view/input, removed ramp texture and capture meshes/hooks, stopped Play. Both save files restored from fresh revision2 backups and hash equality verified. No upload or publication.

## 2026-10-25 — macro trailer v3: two seconds moving forward between fibers

- Owner requested a slow forward camera move for the first two seconds, with pullback starting afterward. Replaced v2's0.25-second microscopic hold with a2-second real dolly: camera and target translate+z3mm, constant x/y, viewing direction and FOV50; smoothstep eases to zero velocity at the reversal. New mf01c_intro645Steps/frames,10.750s,1080p60; capture pendingPng0/errors0.
- Preserved the full-rug and gradual-soiling sequence, shifted+1.75s: whole rug5.05–5.75s, approach5.75–10.45s, dirt smoothstep5.95–10.25s, final hold to10.75s. All action takes02–09 reused unchanged. Endpoint Dirt again matches the target exactly(maxdiff0); final intro/vacuum image junction meanabsRGB0.000765/255,p99=0.
- Independent raw QA verifies visible differential parallax with no full-frame fiber occlusion, no backward steps before frame120, and a smooth reversal. Camera CSV records exact+3mm translation over0–2s at unchanged height/angle/FOV. Full-rug framing, monotonic dirt ramp and mesh change at frame237 also pass. Reports/contact sheets in SteamStore/_work/macro_20261025/revision3/qa; camera.csv/dirt_ramp.csv retained beside take.
- Master SteamStore/trailer/macro_20261025/revision3/refluff_macro_trailer_v3.mp4:57.750s/3465frames,1920x1080/60CFR,H.264 High/BT.709,AAC48k stereo,131721097bytes;SHA2562f87d5ac05d85ff38c73ea6e0a390bcd6b713bf3a973394285c8d1f132d56e79. All18 final QA PASS; encoded audio-14.0LUFS/-2.4dBTP,no clipping or black frames,maxlimiter4.64dB. Preview first3.5s(210frames,4685999bytes) and fullintro+vacuum15.25s(915frames,32637587bytes) adjacent; masterAAC copied unchanged.
- Recipes/EDL/composer in Tools/steam_art/trailer/macro_20261025/revision3; current trailer docs updated. V1/V2 preserved. Restored runtime view/input, removed temporary meshes/ramp texture/hooks/log references, stopped Play, restored fresh revision3 save backups and hash-verified both files. No runtime source/shader/settings edits, upload or publication.

## 2026-10-25 — macro trailer v4: triple initial camera speed

- Owner requested3x faster camera travel during the initial between-fibers moment. Kept the prior two-second duration and tripled camera/target forward displacement from3mm to9mm, with the same height,x,FOV50,view direction and easing. New mf01d_intro645realSteps/frames,10.750s,1080p60;SeqEnd pendingPng0/errors0.
- Independent cameraCSV comparison gives median speed ratio3.0 and+9.000mm in2s; path error vs triple displacement<=0.0002mm,no reverse steps. Frame review shows stronger parallax and forward clearance with no full-frame fiber occlusion. Full-rug hold, gradual soil timing, mesh transition and vacuum junction pass; endpoint Dirt maxdiff0, junction meanabsRGB0.000765/255,p99=0. Reports in SteamStore/_work/macro_20261025/revision4/qa.
- New master SteamStore/trailer/macro_20261025/revision4/refluff_macro_trailer_v4.mp4:57.750s/3465frames,1920x1080/60CFR,H.264 High/BT.709,AAC48k stereo,131705711bytes;SHA25686f0066bef312868e4ca0c43442634c480d2ab4f05de90c77ef65c40f5b9edb6. Final18QA PASS,finite decoded audio,no clipping/black frames. Initial3.5s/210frame and opening15.25s/915frame previews adjacent,masterAAC copied.
- Action/edit objects02–09 fully identical toV3, includingtiming/speed/audio. Foley and masteredmix WAV hashes byte-identical toV3, recorded in audio_identity.json. Earlier masters preserved. New capture/edit/restore recipes and current docs under revision4.
- Restored original runtime camera/light/material/mesh/input, removed temporary capture/ramp resources and hooks, stoppedPlay. Both fresh savebackup files restored and hash equality verified. No runtime source/shader/settings edits,upload orpublication.

## 2026-10-25 — restore every carpet to the normal pool

- User requested restoring all carpets to the selection pool. Moved rug_user_test.png from Textures/Resources/CarpetPicturesOverride to Textures/Resources/CarpetPictures through the live Editor's move_asset command; Unity preserved its .meta/import settings and GUID 661cc6a7c44169a4e9e7bb50316557ae.
- Verified in the open Editor using eval_file: override texture count 0; normal scene catalogue merged with Resources contains exactly 11 unique textures (five museum rugs, five generated cartoon designs and the user picture). All five original and five generated names are present alongside rug_user_test. Play was already stopped and remains stopped; the full pool is loaded on the next Play start.
- Updated ARCHITECTURE.md, HANDOVER.md and AGENTS.md to describe the restored catalogue. No scene rebuild or gameplay-code change was needed.

## 2026-10-25 — accepted macro trailer V4: Steam upload prepared, file drop pending

- Owner explicitly accepted V4 and authorized uploading it to the Refluff Steam store page and publishing it. Rechecked the full 131705711-byte master SHA256 86f0066bef312868e4ca0c43442634c480d2ab4f05de90c77ef65c40f5b9edb6 and its 1920x1080 cover. No social-platform publication or game release is in scope.
- Opened authenticated Steamworks via Dashboard > Refluff (5304930) > Trailer Uploaded. Actual Store Admin ID is 1337056; the current single trailer card is movie1357534, named Refluff - Gameplay trailer, category Gameplay, visible/before screenshots/all ages, English, no country exclusions. This supersedes historical movie1347501 in older notes; no metadata was changed.
- Clicked Replace on that existing card. Steamworks presents only a drag-and-drop movie zone, with no movie file input. Clicking the zone did not emit a filechooser (10-second check); available CUA API supports local files only through filechooser/setFiles, and native app APIs are disabled. No WebMCP tools are exposed. No upload, conversion, Save or Publish has happened. The browser is left at the replacement drop zone for one manual file drop, after which cover/save/publication can continue without renewed approval. Proof screenshot: SteamStore/trailer/macro_20261025/revision4/steam_upload_dropzone.jpg.
- No Unity operation, runtime asset, save, build, branch, pricing or release-state change this session. Browser JavaScript remained read-only; no unsupported DataTransfer/upload-function injection or raw network upload was used.

## 2026-10-25 — current paused Play frame prepared for first Steam screenshot

- Owner reported manually dropping accepted V4 and requested adding the current paused Play frame as the first screenshot, then publishing trailer and screenshot together. Steamworks confirms refluff_macro_trailer_v4.mp4 (125.60 MiB) is uploaded and Converting; a reload still reports Converting. Store diff currently contains only movie1357534 trailerid replacement 178391048 -> 1618490617. Publish was not pressed.
- Captured exact current Play camera plus existing HUD to SteamStore/screenshots/20261025_current_paused.png: RGB PNG 2560x1440, 4139083 bytes, SHA256 32f1c0879314d045e56a5d981c5af620a6b8dd2a6c984d5e4fb0ff146940a85a, SHA1 444bbebfbfc5b40917c1e35ecb860fdd63a67a89. Visual QA: capybara carpet, wet dirt around it, current Clean63/Dry16/Hose HUD, no Editor chrome. Camera and layout are unchanged.
- Before/after capture Unity playing=true, paused=true, frame2219, Time35.87123; camera position(0,1.5,-1.12), euler(55,0,0), FOV45 identical. No Step, unpause, refresh, recompile, save or gameplay change. Capture used native2560 scene/HDR render and current premultiplied UI Toolkit overlay.
- Screenshot assets (9 existing) also use a pure Drop images here to upload zone, without an associated movie/screenshot file chooser. Clicking it produced no chooser (10-second check). The separate hidden image input has no visible associated picker, so it was not repurposed. Left visible Screenshot assets tab at the drop zone; a manual drop of the prepared PNG is required before upload/category/order/save/publication can proceed. No screenshot has been uploaded or reordered yet; publication is held to satisfy the request to publish both together. UI proof SteamStore/screenshots/20261025_steam_screenshot_dropzone.jpg.

## 2026-10-25 — accepted V4 and paused Play screenshot published together on Steam

- Owner manually dropped the prepared PNG and explicitly requested placing it first and publishing together with the accepted V4. Uploaded as Screenshot/all ages, moved its thumbnail through the gallery's native drag handles, saved, and verified the saved first asset is SHA1 444bbebfbfc5b40917c1e35ecb860fdd63a67a89. The prior nine pictures and their alt text remain in the same order. New image category Gameplay; the final upgrade-shop image is classified In-Game Menu. Steam's reorder/save binds categories by numeric slot; corrected the shop category after checking the saved gallery/diff.
- Steam completed V4 conversion. Existing movie1357534 retained Refluff - Gameplay trailer, Gameplay, English, visibility/before-screenshots/all-ages flags and no country restrictions. Trailer ID178391048 -> 1618490617, saved migration ID also updated. The full accepted 131705711-byte local master remains SHA25686f0066bef312868e4ca0c43442634c480d2ab4f05de90c77ef65c40f5b9edb6. Actual Steam thumbnail is the automatically generated new V4 asset5b16076fb22b85a0dae854bded05ddebfd432fc9; the separate local cover PNG was not uploaded.
- Reviewed Store Diffs: only the added first screenshot/order/category/asset timestamp and the trailer replacement/migration ID. Pressed Store Admin Publish to public; received Successfully published! at /admin/game/publish/1337056?pubresult=success, publishing revision7 (Steam UI25Oct12:58am). No Steamworks App Admin publish, game release, build, branch, price or schedule change.
- Verified the anonymous public page https://store.steampowered.com/app/5304930/Refluff_Carpet_Cleaning_ASMR/: new macro brush footage plays, displayed duration0:57 / seek maximum58; capybara is the first still after the trailer, and selecting its first thumbnail displays the exact requested camera/HUD frame. Public image path contains444bbebfbfc5b40917c1e35ecb860fdd63a67a89. Verification recorded2026-10-25 08:01:54UTC. Separate trailer-assets preview requires storefront login; public-page verification succeeded without login.
- Evidence and complete10-image order in SteamStore/trailer/macro_20261025/revision4/publication.json; steam_publish_success.jpg, steam_live_trailer.jpg and steam_live_first_screenshot.jpg adjacent. Updated TRAILER.md, TRAILER_MACRO_20261025.md, STORE_ART.md and SteamStore/README.md, retaining historical shoot/order/upload records. No Unity operations this final publication turn; paused Play state from the screenshot capture remains untouched.

## 2026-10-25 — macro trailer published to TikTok, YouTube Shorts and Instagram

- Owner requested publishing accepted V4 to all three selected accounts, in its existing format, with an invented caption and per-platform Steam UTM links. Prepared English caption Tiny fibers. Deeply satisfying cleaning / Turn the sound up and watch this rug come back to life, Steam wishlist CTA, five tags and the six existing CC BY recording credits. Full exact captions/post plan retained under SteamStore/trailer/macro_20261025/revision4/social/. Every game URL uses utm_source matching platform, utm_medium=social, utm_campaign=macro_trailer_20261025, utm_content=v4.
- Clarified the real format conflict: YouTube Shorts requires square/vertical; owner explicitly chose a9:16 canvas with the complete169picture and black padding. New derivative1080x1920/60,57.75s/3465frames,73390903bytes,SHA25662dd2410b52b342b8451d7301d2f66f77dc18b7d1691f79e91d35d58fa0c7bca. Whole frame1080x608 at y656, no crop/titles/timing changes; AAC packet/decodedPCM hashes byte-identical to V4. Full decode passed and two frame comparisons visually inspected; shorts_qa.json records results. TikTok and Instagram received original131705711-byte V4 unchanged.
- Used CUA for all live account/upload/submit/verification actions. Confirmed TikTok@vahovgames, Instagram@vahov_games and YouTube@VahovGames. TikTok Now/Everyone/Your brand with Promotional content label; Instagram Original crop/sound on; YouTube Public/not made for kids, Copyright No issues found. Declined new automatic TikTok checks and passkey prompts rather than changing account settings. No passwords/cookies/profile links/security settings changed.
- Published once per platform: TikTok https://www.tiktok.com/@vahovgames/video/7700519431457213710 (Studio11:29Moscow; initial review cleared to Everyone); YouTube Shorts https://www.youtube.com/shorts/ds8PbL4DVsA (Studio Video published); Instagram https://www.instagram.com/p/De6R_OvPcDh/ (Your reel has been shared). Verified remote accounts, new footage, full Steam UTM captions and source duration/format. Playback streams: Instagram1280x720/57.7667s, TikTok1024x576/57.931813s; platform transcodes differ slightly in duration. Complete results verified by2026-10-25 08:36:43UTC/11:36:43Moscow.
- SocialStudio registered landscape clip_f7413026-58a6-45d0-b677-4e35ee17ddce and padded clip_81731e34-3d28-4813-bce8-5d131d40f349; posts TikTok post_fc3e4c08-fd2e-4e4b-bcfb-c95973023c68, Instagram post_d5ec6937-1da0-4314-8995-ffdca56a436f, YouTube post_cc5bc424-9576-4079-abaf-9d3f77dcf277 are all published with exact remote IDs. Explicit owner instructions authorized authoring/publication; metadata/video hashes were approved before final UI actions. Task-local manual-receipt.ts uses the existing public local DB/service APIs (browser/network disabled), claims submitting before each final click and records published only after remote CUA verification with screenshot SHA. All three temporary owner processes released/exited0. No final-submit retry, new scheduler or refresh/analytics run; automatic Playwright CLI publication remains untested by this pass.
- Result/receipts: social/publication-results.json, social/receipts/, and tiktok_published.jpg, youtube_published.jpg, instagram_published.jpg. Current captions retained. Owner's subsequent preference was explicitly for next time: no credits in descriptions. Saved in TIKTOK.md and shared StainedGlassKeeper/Assets/Documentation/SocialStudio.md; choose own/CC0 recordings for future posts so no attribution block is needed. Updated macro trailer doc. No Unity operation, source/settings/scene/save/build change or further Steam publication.



## 2026-10-25 — nine replacement screenshots: alternating macro and wide (upload pending)

- Owner requested reshooting every store screenshot except the first after the trailer, mixing real rug patterns with cartoon creatures, with dirty runoff/floor around dirty rugs and human-looking cleaning paths. Follow-up direction: alternate macro detail and full gameplay views. Preserved exact first capybara PNG, SHA256 `32f1c0879314d045e56a5d981c5af620a6b8dd2a6c984d5e4fb0ff146940a85a`; live Steam SHA1 `444bbebfbfc5b40917c1e35ecb860fdd63a67a89` remains untouched.
- Prepared nine replacement JPEGs in `SteamStore/screenshots/20261025_reshoot/`, all 2560x1440, q95 / 4:4:4. Intended slots 2-10: Anatolian extraction macro, dragon brush wide, European scraper macro, fox foam wide, Salor hose macro, cat squeegee wide, Finnish rya drying macro, axolotl finishing wide, Transylvanian dry-vac macro. All Gameplay / English / all ages; the prior shop/pay-slip/flight shots are superseded in this proposed set. The rya file retains its staging name `r08_rya_moisture.jpg`, but uses ordinary light, not a lamp overlay.
- Real curved tool strokes use heading initialized once per gesture, Catmull-Rom interpolation and modest speed/pressure changes through `ToolController.StrokeSegment` + `StepAll`. A preceding actual rinse/brush/plow sequence dirties the tile; the final jobs reset only the carpet with `notify:false`, preserving that genuinely simulated surrounding floor. No RT dirt painting, alpha erasing, extra rendered fibers or AI screenshot content. Dirty-floor coverage at the nine final captures is 12.25-18.08%; every dirty-carpet shot has dirty surrounding floor. Clean carpet examples are staged spotless then actually wetted/extracted/dried.
- Wide views use the original fixed game camera; five macro poses use temporary `StoreShotApi.RenderView`, restoring the camera afterward. Gameplay 48-shell mesh, light, postprocessing and all serialized game assets remain unchanged. 3840x2160 HDR scenes are downsampled in linear light; only the native premultiplied 2560x1440 HUD overlay is composed. Independent visual review approved macro fiber legibility, irregular work, tool framing and dirty runoff. Full ten-image sheet (including original first) is `review/contact_sheet.jpg`; `review/preview_pair.jpg` compares wide and macro. Manifest/hash checks: nine new files, all dimensions valid, ten distinct file and pixel hashes, first file exactly unchanged. Simulation stats are valid/finite for all nine; current Unity console ground truth has 0 errors and 0 warnings.
- Reproducible saved recipes/exporter: `Tools/steam_art/screens/recipes/reshoot_20261025/` (`final_order.json`, `final_run_order.json`, `final_pass.py`, `export_final.py`, README). Raw scene/HUD layers remain under `SteamStore/_work/shots/r*`; capture log and simulation QA in `SteamStore/_work/screens/reshoot_20261025/`. Restored runtime camera/material/input/HUD/achievement enablement, stopped Play, and restored both fresh save backups with SHA256 equality verified (`save_restore.json`). The initial paused simulation state itself advanced during photography and is not claimed restored. No gameplay source/shader/settings edits, builds or releases.
- Upload is blocked at the manual file-drop step. Opened the existing authenticated Screenshot assets page; its only image intake is the drop zone. The actual image input is hidden: a supported forced locator click could not find a visible target and no filechooser event occurred in 10 seconds. No WebMCP upload tools exist. No unsupported upload injection/raw endpoint was used. No delete, reorder, Save or Publish action occurred; the live gallery is unchanged. Browser left open at the screenshot zone for a manual drop of the nine root `r*.jpg` files; `UPLOAD_ORDER.txt` and manifest record the exact proposed order and pending status.


## 2026-10-25 — alternating macro/wide screenshot gallery published on Steam (revision 8)

- Owner manually uploaded all nine prepared replacement JPEGs in the intended order and saved them. Verified the ten saved asset SHA1 values against the actual local files, including the unchanged first capybara `444bbebfbfc5b40917c1e35ecb860fdd63a67a89`. Nine new slots are exactly r02 Anatolian macro, r03 dragon wide, r04 European scraper macro, r05 fox wide, r06 Salor hose macro, r07 cat squeegee wide, r08 Finnish rya dryer macro, r09 axolotl finish wide, r10 Transylvanian dry-vac macro.
- New entries had no category selected; saved all nine as Gameplay. All ten already had suitable-for-all-ages checked and English image variants. Added nine concise factual English accessibility descriptions, leaving the first asset and its blank alt text untouched. Saved changes, then reviewed Store Diffs: only nine image replacements/category/alt text plus asset_mtime; the former final shop category changed from gamemenu to gameplay. No unrelated page fields, trailer, build/release/price/settings changed.
- Pressed Store Admin Publish to public; received Successfully published! at `/admin/game/publish/1337056?pubresult=success`, creating revision 8. Anonymous public page shows the current 57-second V4 trailer followed by exactly the intended ten stills. All public thumbnail hashes match the local file bytes in exact order; selecting slot 2 displays the new Anatolian macro and its saved English accessibility description.
- Evidence: `SteamStore/screenshots/20261025_reshoot/review/publication.json`, `steam_diff.txt`, `steam_publish_success.jpg`, `steam_live_macro.jpg`. Verification timestamp is recorded in publication.json. Updated output manifest and UPLOAD_ORDER from pending to published, plus STORE_ART.md and SteamStore/README.md. Original first PNG remains SHA256 `32f1c0879314d045e56a5d981c5af620a6b8dd2a6c984d5e4fb0ff146940a85a`. No Unity operation, gameplay/assets/source/save/build changes this publication turn.

## 2026-11-15 — first-person cleaning workshop and physical upgrade table

- Converted gameplay to a walking first-person workshop: WASD/arrows, Shift faster walking, mouse look, centre-ray tool contact (2.7m reach), visible tools without arms, wheel/1–0 selection. CharacterController resolves furniture collision and bounds x/z ±2.95m. Camera eye 1.62m, FOV68; mouse sensitivity is frame-time independent. Cursor/modal/focus gates prevent stray strokes after menus. The existing compute simulations, 48 shell layers, audio, dirt profiles, upgrades and hand-over rules remain in use.
- `FirstPersonSceneBuilder` is called from `CarpetSceneBuilder`; the saved scene is generated through it, never hand-edited. Added a workshop with a physical upgrade bench/terminal and local furniture/held-tool fill lights, retaining carpet key light and ambient. E within2.1m opens the existing localized shop; the former global U/shop button are disabled in FPS. Reticle, ten-slot tool belt and controls reuse the existing UI Toolkit HUD. Five new keys translated to all30 non-English tables;105 total keys. Localization check:0 errors/0 warnings; live31-language HUD/prompt/shop checks:0 text overflow. Final Russian shop capture checks109 labels,0 overflow; stats card restored to300px after visual review.
- Created three reference images, reviewed them, generated Tripo H3.1 Best Quality models (~1.9–2.0M triangles each), reviewed, simplified, exported and inspected in Unity. Final triangles: DryVacuum39,999; FoamSprayer18,527; Dryer19,310. Foam/dryer use10k-target Smart Mesh quads. First dryvac quad export had torn/overlapping shell faces and was rejected; restored its HD version and used40k triangles with Smart Mesh OFF, yielding0 degenerate faces. Total290 existing Tripo credits, no credit/subscription purchase. References/prompts/task IDs/settings/hashes: `Tools/first_person/`; accepted geometry/previews: `reports/first_person/models/`; rejected export retained outside Assets.
- New normalized bodies are in `Prefabs/FirstPerson` and `FirstPersonToolSet.asset`. Floor detergent shares the accepted foam-gun body at0.85 scale. Existing working heads remain; shafts now attach to measured sockets, dry vacuum uses the wet-extraction head as its scaled suction foot, and the rotary brush keeps its spinning bristles. Real spray visuals leave inspected nozzle markers and terminate at the simulation contact. Legacy outline hidden in FPS. All10 tools visually reviewed in `Assets/Screenshots/first_person_tool_0.png` through `_9.png`; bench/shop captured as `first_person_workbench.png` and `first_person_shop.png`.
- Verification:11 movement/geometry/scripted-input checks;25 real `ToolController.Update`/MouseState tests for all10 tools, GPU stamps, exact centre ray (0.0000mm error), pressure, reach, occlusion, modal/overlay/focus gates and HUD picking;15 workbench/pause/payment/hand-over/new-carpet checks. Cycle paid once, changed seed/picture/soil, kept player/camera fixed and restored economy plus exact save bytes. First cold cycle exceeded the CLI5s budget but finished restoration; warmed rerun passed all15 and wrote `reports/first_person/cycle.json`. Compilation succeeded; `FirstPersonSpray`0 shader errors; final audit10 visible tool models/4 body registrations/48 shells/FOV68. No Steam build/upload/publication performed.
- Test save progress restored from fresh session backups: editor SHA256 `6D7286990AAE130A7ED497FF9312C7E6F0D148731C59F0D1DB239E59FAED7E8E`; player save unchanged SHA256 `22FE4F92A1A2190083DAFF17F1450E392CFD0CA9BAB8F5D812108757D26A0BFE`. Evidence `reports/first_person/save_restore.json`. Original scene backup: `Tools/first_person/baseline/SampleScene_before_fps.unity`. Updated `ARCHITECTURE.md`, `FIRST_PERSON.md`, `FIRST_PERSON_UI.md`, `TOOL_MODELS.md`; reproduction scripts and reports retained.

## 2026-11-15 — seven regenerated tools, larger carpet and daylight workshop

- Owner approved the initial models and requested regenerating the other seven with the same image -> high-quality Tripo model -> review -> simplified mesh -> Unity workflow. Generated and inspected seven original references, then Tripo H3.1 Best Quality meshes (1.915-1.982M triangles), reviewed from multiple angles, remeshed to a 40k-triangle target with Smart Mesh OFF, and exported GLB with 4K base textures. Final source counts: Scraper39,999; Hose40,000; Brush40,000; Squeegee39,998; WetVacuum39,990; FloorCleaner40,000; Mop40,000. All have valid finite geometry and zero degenerate faces. Used420 existing credits; no purchase. References, exact prompts, Tripo IDs, settings, hashes and browser proof: `Tools/first_person/regeneration_20261115/`.
- Imported seven independent replacements, registered five contact heads in `ToolModelSet` and two new complete guns in `FirstPersonToolSet`. Physical working widths and measured handle/nozzle/hose sockets are replayable from `import_plan.json` and `contact_imports.json`; Normalize Tool Models respects these replacements. Thin mechanical tubes fit each bore. Brush split at15.20mm retains14,323 stationary housing triangles plus26,497 rotating skirt triangles,350rpm and8mm pile sink. The accepted DryVacuum/FoamSprayer/Dryer GLBs and prefabs are byte-identical to their baselines. All10 tools were visually accepted in actual FPS working captures; maximum shaft/socket error0.0000003m, new hose inlet error0, maximum jet-axis error0.0442degrees, nearest held vertex0.2302m versus camera near clip0.025m. Reports: `reports/first_person/regeneration_20261115/`.
- Enlarged the rug three times along each axis to3.6x4.5m and the workshop to12x12x3.8m. Actual simulation coordinates changed, not a scaled transform: rug512x640/DX7.03125mm; floor9x9m/960x960/DX9.375mm. The48 layers,20mm pile,5mm strand pitch and physical tools remain. Updated shared C#/HLSL domains, render grids, collider, floor mapping, handover travel and player/bench positions. Narrow blades use3x3 subcell quadrature; EdgePull conserves runoff at the new cell spacing; particle rates/caps scale by cell area. Histogram fixed-point scale reduced1024->512 so a capped full raster fits uint32 (2,684,354,560).
- Added a solid closed north door with frame, panels, handle and hinges; two luminous factory windows; six window-column volumes. WindowLightVolume analytically clips camera rays to sheared pane prisms, integrates8+8 samples with extinction/dust noise, and clips at opaque scene depth. HDR emission/bloom is separate from the upgrade terminal. Corrected legacy camera occlusion hiding distant room objects and the HUD capture's ARGB32 scene target; captures now use the existing HDR rendering/encoding path before UI composition. The volume uses camera-depth occlusion, not secondary volumetric furniture shadows. Room source and limits: `Docs/FIRST_PERSON_ROOM.md`.
- Verification: all76 scale/input/cycle checks passed (17 offline,8 GPU,25 live input,11 movement,15 bench/pause/payment/handover). Hose target(900,1350)mm produced centroid(899.999761,1350.000798); runoff230000mm3 produced229999.999576mm3 and all transported species were conserved. Histogram saturation returned exactly2,684,354,560 across327680 cells, decoded mean16. Three computes reported0 warnings/errors after real dispatches; C# compiled successfully. Three final room views each passed18 checks, including solid-door collision, wall collision, bench reach, HDR emission, depth, six volumes and no legacy occlusion. Final room evidence: `Assets/Screenshots/first_person_room_overview.png`, `first_person_room_door.png`, `first_person_south_window.png`, `first_person_east_window.png`.
- Editor save preserved at fresh-session SHA256 `97B434B9148BB8B9AC4162E6AA3532964DACFECE5AD012824F960F73658FABA6`; player save unchanged at `22FE4F92A1A2190083DAFF17F1450E392CFD0CA9BAB8F5D812108757D26A0BFE`. Scene built/saved through the builder, Play stopped. Updated ARCHITECTURE, FIRST_PERSON, TOOL_MODELS, HIGHLIGHTS, FLOOR_SYSTEM, PHYSICAL_SCALE, FIRST_PERSON_ROOM and AGENTS. No Steam build, upload or publication.

## 2026-11-15 — local preview build

- Built the current first-person workshop and regenerated tools through `CarpetCleaning/Steam/Build Windows (local test, with steam_appid.txt)`. Output: `Builds/Steam/local/Refluff.exe`; 210 files, 1287.8 MiB, version 0.9.1, local App ID 5304930. Unity reported Build Successful and `[SteamBuild] OK` after 1998 seconds; the CLI's 1800-second timeout expired before Unity finished, so completion was verified in the actual Editor log and output files.
- Build includes CREDITS.txt and THIRD_PARTY_NOTICES.txt. Packaging warning remains for nunit.framework.dll without a notice; this is a local preview, not a release upload. No player launch, Steam upload or publication was performed in this build session.


## 2026-11-15 — centred brush, close tool reach and full-room floor

- Moved only the brush HandleSocket from its off-centre pipe to the stationary central silver hub `(0,0.0342,0)`, directly above Spin. Updated replay recipes; mesh geometry, 350 rpm and 8 mm pile sink are unchanged. FPS capture confirms zero radial offset and shaft/socket error 0.0000001 m; 15 protected asset hashes unchanged. Evidence: `reports/first_person/brush_center/`.
- Reduced non-hose tools to 2 m camera-ray reach (default and idempotent builder). Hose has unlimited reach to valid visible carpet/floor, retaining camera-ray furniture occlusion. Tile rays now intersect actual y=-3 mm, eliminating the shallow-angle height offset. Existing stamp, power, pressure, sound and floor-tool rules remain.
- Expanded active floor to all 12x12 m of the workshop:1280x1280 at unchanged9.375 mm/cell, water320x320 and foam640x640. Shared C#/HLSL coordinates and foam rim alignment remain consistent. Reduced floor statistics fixed-point scale256->128 to avoid full-domain uint overflow; decoded full12 mm layer and raw2,516,582,400 verified.
- Verification:115 checks passed (29 aiming,8 real-input hose,20 offline math,10 GPU/domain/conservation,48 effects at8 wall/corner patches). Real hose shot at10.723 m injected142038.54 mm3 into the new floor band, centroid error0.014464 mm; stream endpoint error0.096 mm. Walls/furniture block the centre ray; sky does not stamp. Independent offset nozzle collision remains outside this change. Water, foam, extraction, detergent and mop work at x/z +/-5.8 m. Three compute shaders:0 messages after dispatch; current pre-build console:0 errors/0 warnings. Captures: brush_center/gameplay/tool_4.png and interaction_20261115/hose_far.png.
- Built fresh local Windows player successfully: `Builds/Steam/local/Refluff.exe`,211 files,1270.0 MiB,42 seconds,version0.9.1. No upload/publication or player launch. Both save files preserved byte-for-byte from this request's fresh baseline: editor SHA25697B434B9148BB8B9AC4162E6AA3532964DACFECE5AD012824F960F73658FABA6; player SHA2561652BB8378EA69CEFDA96FED2B1B3012DE4406C012E512ECFE1A41E5252AC7B1. Updated FIRST_PERSON, ARCHITECTURE, PHYSICAL_SCALE, FLOOR_SYSTEM, TOOL_MODELS and AGENTS; reusable old input tests now follow the current reach rules.
- The Unity beta Editor again retained stale default-volume components after BuildPlayer, causing a null VolumeManager default-profile entry followed by Blitter initialization exceptions. This is post-build Editor state; the build itself succeeded. Requested a script/domain reload to restore the Editor, as verified before this build. No package/runtime workaround added.
- Post-build Editor recovery verified: EditorUtility.RequestScriptReload restored the render pipeline; final state stopped, not building/compiling, compilationFailed=false, currentPipeline present.

## 2026-11-16 — Editor progress reset tool

- Added Tools > CarpetCleaning > Reset Progress..., an alias under CarpetCleaning > Economy, and Reset all progress... in Test Money Window. All interactive entries confirm the reset and show the Editor save path. Clears money, POWER/AREA levels, inspection purchases, statistics, local achievements and tool-use history in carpet_save_editor.json. Language/volume, player save and Steam progress are preserved; current carpet/job clock continue.
- Play mode uses Economy.DebugResetSave with an atomic fresh-save write before changing live data, re-applies starting tool tuning and notifies wallet/shop/highlights. The method now returns success/failure; failed writes preserve live progress. Edit mode writes a fresh normalized save for the next Play. The editor command clears scripted Neutral tuning after success. Documentation: PROGRESS_RESET.md and ECONOMY.md.
- Verified: real C# compilation completed without errors; Edit and Play reset checks cleared every saved field; simulated file-lock write failure preserved the file and live data with no reset events; Play emitted wallet/item events and all ten tools matched level-0 tuning; Test Money Window contains exactly one full-reset button. Editor left stopped. Test snippets/backups: Temp/ProgressReset/ (scratch).
- User saves restored byte-for-byte after checks: editor SHA256 97B434B9148BB8B9AC4162E6AA3532964DACFECE5AD012824F960F73658FABA6; player SHA256 1652BB8378EA69CEFDA96FED2B1B3012DE4406C012E512ECFE1A41E5252AC7B1. No build or upload.

## 2026-11-16 — proportional tool heads through AREA upgrades

- Replaced blade heads' `(1,1,area)` deformation in `ToolTuning.HeadScale` with uniform `(area,area,area)`; discs retain uniform sqrt(area). Scraper, squeegee, wet-vac and mop now preserve their authored proportions in both surface contact and idle held poses. Effective blade length, stamp geometry/spacing, power, prices and save format are unchanged. Bottom pivots preserve surface placement; existing HandleSocket transforms keep shafts connected without changing the hand grip.
- Verified real C# compilation with no errors. Live probe passed 686 assertions over all 70 tool/level combinations (10 tools, levels 0..6), 49 registered surface-head cases and 84 mesh-transform cases. Maximum mesh proportionality error 7.45e-9; root-scale error 0. POWER independence and Neutral factory behavior passed.
- Captured all four changed heads at levels 0/6 in real FPS aim, plus brush/dry-vac at level 6 and all four changed heads in idle held poses. Shaft/socket distance at most 0.0000004 m. Idle heads' nearest vertices remain 0.7379 m or farther from the camera (near clip 0.025 m). Evidence: `reports/first_person/upgrade_scale_20261116/verification.txt`, `comparison.png`, `gameplay/`; scratch evals: `Temp/ToolUpgradeScale/`.
- Progress preserved byte-for-byte against this session's baseline: editor SHA256 CE70CA27418F711832A157226B24232EF29E2CBD3E9C72AA947E3EFA98B5A6F0; player SHA256 1652BB8378EA69CEFDA96FED2B1B3012DE4406C012E512ECFE1A41E5252AC7B1. Editor returned to Play with saved tuning; no build or upload. Updated TOOL_MODELS, ECONOMY and FIRST_PERSON docs.

## 2026-11-16 — warm, furnished workshop and dynamic sunlight

- Reworked the environment through FirstPersonSceneBuilder + WorkshopFinishBuilder/WorkshopDetailBuilder/WorkshopAtmosphereBuilder. Real window openings/mullions, warm shadowed sun, local bounce fills, reflection probe, cream plaster, sage panels, timber/trim, restrained ACES/bloom, revised floor shadow/AO reception and distance-filtered sparkle. Start view is 28 degrees down, existing position/FOV/eye/tool reach unchanged. Added a pictographic terminal screen without new localization strings.
- Eight furnished zones: stocked rack, utility sink and plumbing, pegboard tools/workbench/lamp/stool, service trolley, hose reel/services, radiator/plants, cabinet supplies and wall notes/clock. Original procedural dressing is combined into 70 renderers, 137201 vertices/199688 triangles with 7 furniture colliders; finite geometry and free central area audit passed with 0 errors. Corrected leaf-tip NaNs and back-face normals, actual recessed sink opening, and planar wood UVs on the stool.
- Referenced Kyle Banks' supplied video through its browser transcript and the live CQF implementation in StainedGlassKeeper read-only. No SGK code/assets copied. Six bounded volumes now integrate animated 3D density, Beer-Lambert transmittance and real sun shadow maps (24 samples), with four slowly moving exterior shadow-only foliage groups and 192 subtle motes. Final variance .65/scattering 2.55. Fixed first-pass ray artifacts by physically clipping the medium at the room floor/ceiling and decorrelating march samples. These are actual volumes, not opaque beam cards.
- Completed user-specified Tripo image -> review -> HD model -> review -> mesh optimization -> import flow for the spare extractor. H3.1 Best Quality 1924471 triangles -> Smart Mesh 16977 triangles (one PBR material), height1.05m. 90 existing credits +2 free concept slots. Original optimized export/provenance/mesh QA in Tools/workshop_20261116; normalized-name GLB in Models/Workshop. No purchases. Nonfatal glTFast/Unity6000.7 SetMainObject hierarchy warning remains, model/scene references visually verified; no third-party package modifications.
- Verified real C# compile without errors and both changed shaders after rendering. All18 environment checks pass: room/floor bounds, door/wall collision, bench near/far interaction, depth request, six volumes/HDR and shader support. Updated wall test uses an unobstructed gap because the old x2 route now correctly hits the sink. Camera inside both directions inspected. Opaque blocker rays-on/off A/B gives exactly0 pixel error after renderer warm-up. Frozen-pose time0/time8 comparison changes42904 pixels by >3/255, confirming real atmosphere/shadow motion.
- Final captures and JSONs: reports/workshop_20261116/overview.png, overview_hud.png, south_light.png, workbench.png, wash_station.png, spawn.png, rays_t0/t8.png, inside_volume_0/1.png, atmosphere_pixels.json, room_qa_1.json. Added WORKSHOP_VISUALS, WORKSHOP_DRESSING, WORKSHOP_ATMOSPHERE; updated ARCHITECTURE, FIRST_PERSON, historical FIRST_PERSON_ROOM pointer. Environment-only rebuild menu saves through Unity; no hand-edited scene YAML.
- All37 protected carpet texture/material/shader/compute files match their initial SHA256 hashes. Editor progress remains CE70CA27418F711832A157226B24232EF29E2CBD3E9C72AA947E3EFA98B5A6F0 without restoration needed. No player build, performance claim, Steam upload or publication. Existing static-cleanup analyzer warnings remain.
- Final handoff: scene saved and Editor returned to Play (original session mode), normal input restored; final new runtime errors0, compilationFailed=false, editor save hash still unchanged.

## 2026-11-16 — initial Git source snapshot

- Initialized main with origin https://github.com/dendoo/Refluff.git (remote initially empty), enabled Git LFS for binary assets, and saved the active Unity scene/assets before the baseline snapshot.
- Added source-control exclusions for Unity/IDE caches, builds, temporary captures/render intermediates, local tool dependencies, Steam login cache/upload output and PRIVATE_DO_NOT_UPLOAD correspondence/keys. Game assets, scripts, settings, tools, documentation and final store media are included.
- Baseline live Editor check: SampleScene, SimSettings.PILE_H 20 mm, renderer pileHeightM 0.02 m, 48 shell layers, collider height 0.02 m / centre 0.01 m. Pile-length changes are intentionally subsequent to this snapshot.
