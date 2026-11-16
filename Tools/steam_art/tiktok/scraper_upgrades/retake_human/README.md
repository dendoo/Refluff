# Scraper upgrades: human performances, cut 2 (2026-10-17)

The user performed both scraper takes. Pointer samples are replayed in the
simulation at 60 Hz and speed 1, preserving the human movement and duration.
The supplied speech also stays at its original speed and pitch. The extra
performance time increases the edit to 33.7 seconds. The original cut is kept.

| Timeline | Frames | Take |
|---|---:|---|
| 0.00–5.00 | 300 | `scu_arrival` |
| 5.00–12.25 | 435 | `scu_weak_human` (first 435 of 436 frames) |
| 12.25–18.25 | 360 | `scu_shop_human` |
| 18.25–26.60 | 501 | `scu_power_human` |
| 26.60–33.70 | 426 | `scu_reveal_human` |

All captions are centred at x 0.5. The weak-shot speech is at y 0.65 / 0.712,
clear of this human performance's upper-rug action; LEVEL 1 and MAXED OUT remain
at y 0.17. Logo and CTA also remain centred. The final shop purchases align with
“Max power” at 15.75 and “Max size” at 17.17. The reveal phrase starts at 26.70,
logo/Refluff at 29.90, CTA/Wishlist at 30.90; the last word ends at 32.19.

`build_cut.py` reads the unchanged base EDL and original `voice_source.mp3`.
It writes this folder's `edl.json` and the audio/transcript/timing files in
`SteamStore/tiktok/scraper_upgrades/human_cut/`. Successful live recordings
are referenced and hashed in timing.json: weak = 680 samples, 7.003366 s;
strong = 755 samples, 8.0316769 s. They are never resampled on disk or replaced.
Absolute silence in the aligned voice runs from 18.38 to 26.64 seconds so the
uninterrupted human strong performance can be heard.

## Recording/replay

Prepare the film rig as in the parent README, first backing up the editor save
to `SteamStore/_work/tiktok_scraper/retake/carpet_save_editor.before.json`.
Compile and load the recorder outside Assets; no project recompile is needed:

```powershell
unity command run_script --file Tools/steam_art/tiktok/scraper_upgrades/HumanRecorder.cs --dry_run true
unity command run_script --file Tools/steam_art/tiktok/scraper_upgrades/HumanRecorder.cs
python Tools/steam_art/tiktok/scraper_upgrades/drive.py human.selftest
```

Main's serialization self-test writes `human/_selftest.json` under
`SteamStore/_work/tiktok_scraper/`: verify `fixture: true` and a two-entry
`samples` array before asking the performer to start. For each live take,
call `human.stageweak` / `human.stagestrong`, then `human.armweak` /
`human.armstrong` through `drive.py`. Wait for the green ARMED badge, click
Game view and release, then hold LMB to scrape. The focus click is blocked.
Weak records seven seconds and strong records eight seconds from the first
valid press; releasing and pressing between strokes does not end either take.
Returning to chat ends an active take, so stay in Game view for the full window.

The green SAVED badge appears only after JSON disk readback verifies the sample
array. Recordings are `human/weak.json` (680 samples, 7.003366 s) and
`human/strong.json` (755 samples, 8.0316769 s). Samples also persist throughout
recording and in AppDomain data; previous files are archived before a retake.
Verify both saved arrays and preserve their hashes before capture.

Replay the existing performances in this order, keeping the same carpet from
weak through the reveal:

```powershell
python Tools/steam_art/tiktok/scraper_upgrades/drive.py human.replayweak --take
python Tools/steam_art/tiktok/scraper_upgrades/drive.py human.shop --take
python Tools/steam_art/tiktok/scraper_upgrades/drive.py human.replaystrong --take
python Tools/steam_art/tiktok/scraper_upgrades/drive.py human.reveal --take
python Tools/steam_art/tiktok/scraper_upgrades/fix_shop_ui.py --take scu_shop_human
python Tools/steam_art/tiktok/scraper_upgrades/finish_capture.py --human
```

The strong replay begins with an unpressed parked Step to consume the shop's
button-release gate. Its initial heading is seeded to 90 degrees through
`scu.human.initialheading.strong`: the original recording contains no pre-press
hover state, so this starting orientation is inferred. It reproduces the live
full crust removal at the original tool width; pointer coordinates, timing and
saved JSON remain unchanged. `--human` uninstalls the recorder, restores the rig,
stops Play and restores the separate save backup byte-for-byte; its report is
`SteamStore/_work/tiktok_scraper/retake/capture_report_human.json`.

Run from the project root:

```powershell
python Tools/steam_art/tiktok/scraper_upgrades/retake_human/build_cut.py
python Tools/steam_art/tiktok/scraper_upgrades/retake_human/verify_alignment.py
python Tools/steam_art/trailer/make_trailer.py --edl Tools/steam_art/tiktok/scraper_upgrades/retake_human/edl.json --out SteamStore/tiktok/scraper_upgrades/human_cut/refluff_tiktok_scraper_upgrades_human.mp4
python Tools/steam_art/trailer/check_trailer.py SteamStore/tiktok/scraper_upgrades/human_cut/refluff_tiktok_scraper_upgrades_human.mp4 --edl Tools/steam_art/tiktok/scraper_upgrades/retake_human/edl.json --out-dir SteamStore/tiktok/scraper_upgrades/human_cut/checks --expect-seconds 33.7 --safe 0.06,0.10,0.84,0.78
```

Final export: 1080×1920, 60 fps, 2022 frames, 33.7 s. Voice duck and final
loudness targets are inherited from cut 1: −4 dB duck, −14 LUFS / −2 dBTP.
`verify_alignment.py` checks every title and end-card box for x-centering and
TikTok safe area, contiguous frame ranges, real-time playback, WAV length and
the silent voice interval. The final renderer and checker validate the MP4.
