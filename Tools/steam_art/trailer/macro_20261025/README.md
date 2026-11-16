# Macro trailer — 25 October 2026

Fresh 50 s, 1920×1080, 60 fps cut. Nine real-time takes from one red carpet show the pile's visible reaction to suction,
flowing water, foam and rotary brush, squeegee, extraction and drying. No explanatory titles, voice or music.
The only overlay is the actual approved logo and **Wishlist on Steam** during 46–50 s. No release date.
The final image stays visible; the audio fades over 0.6 s and is silent for the last 0.1 s.

`edl.json` is the cut, with source folders in `SteamStore/_work/clips/`. Captures can include extra tail handles;
the cut initially uses the first 3, 6, 6, 6, 6, 5, 9, 5 and 4 seconds. The camera motion is already in the captures.
All tools remain at original speed. No HUD, post-capture virtual zoom, artificial visual transition or water/pile effect.
Final shot 04 uses `mf04b_brush`: 0.15 s foam sweep, 0.85 s hold, 4.5 s brush, then 0.5 s hold.

The genuine `mf02_vacuum` motor alone starts about 0.5 s before its picture cut. Its first recorded motor segment
crossfades into the take's original motor over 0.3–0.8 s after the cut; contact, suction and crumb rattle retain their
original sample timing. No external source is used for this small J cut. `motor_prelap` in the EDL records the edit.

`dryer_carry` extends mf07's genuine running dryer loop continuously, at its recorded sample phase and speed,
offscreen under the reveal and final card. It enters at -10 dB relative to the source level, then falls smoothly to
-24 dB before the final fade. It eases in below the genuine take's coast-down during the preceding 0.6 s; the source image/actions are unchanged.
Hose and foam/brush shots are level-balanced down to preserve headroom
before the common limiter; exact gains remain in the EDL. This is an editorial sound carry, recorded explicitly in
the render report, rather than a claim that the silent reveal/end take logs contained an active tool.

```powershell
python Tools/steam_art/trailer/macro_20261025/compose.py --preflight
python Tools/steam_art/trailer/macro_20261025/compose.py --card-preview
python Tools/steam_art/trailer/macro_20261025/compose.py
python Tools/steam_art/trailer/macro_20261025/compose.py --qa
```

The wrapper validates complete 60 fps native horizontal captures and virtual-clock `audio.jsonl` files, and builds the
actual gameplay foley plan with credit/clip inventory before rendering. Missing takes stop the render.
The existing `make_trailer.py` engine preserves its source frame/sample timing, 8 ms cut fades, linear-light sampling,
true-peak-controlled mastering (-14 LUFS target, -2 dBTP headroom), H.264 High BT.709 encoding and AAC stereo sound.
No new external recordings are added. `preflight.json`, source clips and credits stay beside this version.

The master is `SteamStore/trailer/macro_20261025/refluff_macro_trailer_v1.mp4`. Cover, contact sheet, audio stems and
render report use the same prefix. `--qa` runs the established Steam trailer checks at a 50 s expected duration and
creates review clips in `qa/`. `--frames-at`, `--only`, `--preview`, `--jobs`, `--preset` and `--audio-only` are forwarded
to the existing engine for review/iteration. A layout-only red slate from `--card-preview` checks logo/CTA placement;
the real end card renders over the moving clean-carpet take.

This workflow does not publish or replace an existing Steam trailer.

## Verified master

The final audio-only remux passes all 18 checks in `qa/check_report.json`: 50.000 s, 3000 frames, 1920×1080/60 CFR,
H.264 High, BT.709 tv, 18.002 Mbit/s video, AAC-LC stereo 48 kHz (178 kbit/s), -14.0 LUFS / -2.3 dBTP. No black
frames, overlay geometry and reading time pass, no CC BY-NC recording. Decode: finite samples, no clipping.
Hose and foam/brush are each -4 dB; limiter maximum reduction is 4.62 dB, with linear loudness normalization.
The final 0.08 s decode to silence. The visible final frame retains the real logo and exact CTA.

Master: 113,711,946 bytes. SHA-256:
`ec69e9bf749aad350a7284132a2675a5368219197985c6ee467c6c2d2908457f`.
`delivery.json` records the byte/hash/decode audit; `AUDIO_CREDITS.txt` lists the actual gameplay recordings.
