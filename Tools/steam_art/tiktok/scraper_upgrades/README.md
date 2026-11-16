# Scraper upgrades TikTok — cut 1 (2026-10-17)

The latest version is **cut 2 with human mouse performances and centered captions**:
see `retake_human/README.md`. Its 33.7 s master is in
`SteamStore/tiktok/scraper_upgrades/human_cut/`; this page preserves the cut 1 recipe.

The supplied 19.28 s English read is preserved at its original speed. Pauses are
repositioned around the game actions; the picture is 26.8 s, 1080×1920 at 60 fps.
All files for this cut live in this folder, `SteamStore/tiktok/scraper_upgrades/`,
and `SteamStore/_work/tiktok_scraper/`; previous TikTok cuts are untouched.

## Re-shoot

Use the live Editor, initially stopped. Back up the editor save before ANY
capture purchase or handover; those are real economy actions. The picture is
loaded from the film folder into a temporary texture, never imported into the
shipped rug bag. `CaptureRig.cs` runs as an ephemeral assembly outside Assets;
only its compute is temporarily imported into Editor/_CaptureOnly.

```powershell
$taskSave = 'C:/Users/denis/AppData/LocalLow/VahovGames/Refluff/carpet_save_editor.json'
Copy-Item -LiteralPath $taskSave -Destination 'SteamStore/_work/tiktok_scraper/carpet_save_editor.before.json'
unity command editor_play
python Tools/steam_art/tiktok/scraper_upgrades/drive.py install
python Tools/steam_art/tiktok/scraper_upgrades/drive.py prepare
foreach ($taskTake in @('arrival','weak','shop','power','reveal')) {
    python Tools/steam_art/tiktok/scraper_upgrades/drive.py $taskTake --take
    if ($LASTEXITCODE -ne 0) { throw "Capture failed: $taskTake" }
}
python Tools/steam_art/tiktok/scraper_upgrades/fix_shop_ui.py
python Tools/steam_art/tiktok/scraper_upgrades/finish_capture.py
```

If interrupted, run `finish_capture.py` before doing anything else in Play.
It records final stats/kernel messages, removes the rig, stops Play and restores
the original save with a SHA256 comparison. Rebuild uses only the finished takes.
All staging details are in `STAGING.md` and `Assets/CarpetCleaning/Docs/TIKTOK_SCRAPER_UPGRADES.md`.

## Rebuild

```powershell
python Tools/steam_art/tiktok/scraper_upgrades/align_voice.py
python Tools/steam_art/trailer/make_trailer.py --edl Tools/steam_art/tiktok/scraper_upgrades/edl.json --out SteamStore/tiktok/scraper_upgrades/refluff_tiktok_scraper_upgrades.mp4
python Tools/steam_art/trailer/check_trailer.py SteamStore/tiktok/scraper_upgrades/refluff_tiktok_scraper_upgrades.mp4 --edl Tools/steam_art/tiktok/scraper_upgrades/edl.json --out-dir SteamStore/_work/tiktok_scraper/checks --expect-seconds 26.8 --safe 0.06,0.10,0.84,0.78
```

The take directories are the existing renderer's standard
`SteamStore/_work/clips/scu_arrival`, `scu_weak`, `scu_shop`, `scu_power`,
`scu_reveal`. Each is portrait, with audio.jsonl. Only the shop composites its
portrait UI panel (`hud: true`). The five shots start at take time zero.

## Picture and voice timing

| Shot | Timeline | Action / sync |
|---|---|---|
| scu_arrival | 0.0–5.0 | Empty floor; crusted carpet flies in. “Oh, come on…” 0.35–3.12. |
| scu_weak | 5.0–10.5 | Four feeble scraper strokes. “At this rate…” 5.50–7.51. LEVEL 1. |
| scu_shop | 10.5–16.5 | “Wait” 10.70–11.37; “I can upgrade…” 11.80–13.23. Max power 14.00–15.03; Max size 15.42–16.49. |
| scu_power | 16.5–19.7 | Close shop; three fast continuous real simulation passes. MAXED OUT. Three seconds of unobscured scraper foley. |
| scu_reveal | 19.7–26.8 | “Okay…” 19.80–22.42. Logo on Refluff 23.00; CTA on Wishlist 24.00. Last word ends 25.29. |

The final POWER purchase lands at shop local 3.50 and final AREA purchase at
local 4.92. The power shot begins directly after “Max size” and has no spoken
words. The aligned file contains absolute silence 16.63–19.74, including the full
three seconds 16.70–19.70. The audible edges are 16.49 and 19.80; the extra padding
only retains the original quiet phrase tails.

## Captions and audio

- Visible captions use the established Fredoka HUD pills. Long phrases use two
  lines. LEVEL 1 and MAXED OUT sit at y 0.17, speech around y 0.68. The weak-tool
  shot instead puts speech at y 0.30 / 0.352 to leave its low scraper action
  unobstructed (checked against the captured action); all boxes fit
  TikTok safe x 0.06–0.84, y 0.10–0.78. All remain at least one second per three words.
- Refluff and Wishlist are represented by the end logo/CTA; the complete spoken
  transcript is also provided as `subtitles.srt`, `voiceover_en.txt`, and timing.json.
- No music. Existing game foley, the existing licensed room tone (−9 dB), and
  existing generated UI sparkle/clink are the only added sounds. Voice ducks game
  sound by 4 dB while speaking; ASMR can return promptly after each phrase.
- Master targets −14 LUFS, −2 dBTP; only the final 0.3 s is faded to silence.
- `align_voice.py --scan` regenerates the −38 dBFS RMS phrase edges used for cuts.
  Source SHA256 is written to timing.json. The final aligned WAV is PCM16 stereo
  at 48 kHz, 26.8 s, peak −4.26 dBFS before the final mix; source pitch/speed unchanged.

Local ASR was an audit aid only (`SteamStore/_work/tiktok_scraper/source_asr.json`).
It confirms the planned words but spells the brand “Reef fluff” and the CTA
“wish listed”; the authored transcript uses the intended Refluff and “Wishlist
it on Steam.” The reproducible alignment requires numpy and the project's
existing imageio-ffmpeg setup, not the temporary ASR packages/model.

## Verified master

2026-10-17: `check_trailer.py` **18/18 PASS**. 1080x1920,60fps CFR,1608frames,26.800s,
H.264 High/yuv420p/BT.709,13.42Mbit/s video, AAC LC48k stereo200kbit/s,45.7MB.
Final AAC measures **-14.1 LUFS / -2.3dBTP**. All14captions +logo/CTA safe; no black frames.
Source/voice alignment and ASR of the final mix pass (`audio_audit.json`).
Editor save SHA256 is identical before/after; temporary compute removed, Play stopped.

