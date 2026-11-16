# Macro trailer — revision 3

The opening now slowly glides forward between the clean carpet fibers for 2 s before starting the pullback.
The rest of revision 2 is preserved, shifted 1.75 s later: full rug at 5.05 s, hold to 5.75 s, approach from
5.75 to 10.45 s, then hold to 10.75 s. Gradual soil transition runs from 5.95 to 10.25 s, ending at the same
dirty state and camera as the existing vacuum capture. The camera and soil transition come from Unity's new
`mf01c_intro` take; this offline edit applies no virtual zoom or crossfade to them.

The planned final cut is 57.75 s / 3465 frames at 1080p60. Takes 02–09, source in points, playback speed, sound
timing and levels, genuine vacuum motor prelap, quiet dryer carry and final logo/CTA stay as in v2.
The revised opening and all later cuts are held in `edl.json`; the common wrapper derives duration from this EDL.

```powershell
python Tools/steam_art/trailer/macro_20261025/revision3/compose_v3.py --preflight
python Tools/steam_art/trailer/macro_20261025/revision3/compose_v3.py
python Tools/steam_art/trailer/macro_20261025/revision3/compose_v3.py --qa
```

New output: `SteamStore/trailer/macro_20261025/revision3/refluff_macro_trailer_v3.mp4`, with its own preflight,
cover, sheet, stems, render report, credits and QA. V1 and V2 media remain in their original folders.
After the master is ready, export a 3.5 s preview of the revised start plus a 15.25 s preview covering the full
intro and the first 4.5 s of vacuuming, with the final master audio copied unchanged.

## Verified output

All 18 checks pass: 57.750 s, 3465 frames, 1920×1080/60 CFR, H.264 High BT.709 tv, 18.079 Mbit/s video,
AAC-LC stereo 48 kHz, -14.0 LUFS / -2.4 dBTP. No black frames or audio clipping. Maximum limiter reduction
4.64 dB, linear normalization, no render warnings. `delivery.json`, `AUDIO_CREDITS.txt` and
`qa/check_report.json` are adjacent to the master.

Master: 131,721,097 bytes; SHA-256 `2f87d5ac05d85ff38c73ea6e0a390bcd6b713bf3a973394285c8d1f132d56e79`.

Preview exports are also verified at 1080p60 with the final master's AAC audio stream copied unchanged:

- `refluff_macro_trailer_v3_first3_5s.mp4`: 3.500 s, 210 frames, 4,685,999 bytes.
- `refluff_macro_trailer_v3_opening.mp4`: 15.250 s, 915 frames, 32,637,587 bytes.

`opening_previews.json` holds their format and hash inventory. `intro_boundary_audit.json` verifies that the new
intro's final source image and first vacuum image have mean absolute RGB difference 0.000765/255 and p99 of 0.
V1/V2 media remain unchanged.
