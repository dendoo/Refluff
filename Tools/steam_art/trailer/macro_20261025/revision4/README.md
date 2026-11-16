# Macro trailer — revision 4

The first 2 s forward glide between the clean pile fibers now travels 9 mm rather than 3 mm, tripling its speed
while preserving its duration, fixed camera height, angle and FOV. The subsequent pullout starts from the new
endpoint. The full-rug/return/soil key times stay unchanged: full rug at 5.05 s, hold to 5.75 s, approach
5.75–10.45 s, soil transition 5.95–10.25 s, final camera hold to 10.75 s.

`mf01d_intro` contains the actual revised camera capture. The offline edit applies no source retiming, virtual zoom
or crossfade. The final cut remains 57.75 s / 3465 frames at 1080p60. Takes 02–09 retain their existing files,
timeline positions, source in points, playback speeds, sound timing/levels, genuine motor prelap, quiet dryer
carry and final logo/CTA. No voice, music or release date is added.

```powershell
python Tools/steam_art/trailer/macro_20261025/revision4/compose_v4.py --preflight
python Tools/steam_art/trailer/macro_20261025/revision4/compose_v4.py
python Tools/steam_art/trailer/macro_20261025/revision4/compose_v4.py --qa
```

New output: `SteamStore/trailer/macro_20261025/revision4/refluff_macro_trailer_v4.mp4`, with independent reports,
credits, cover, sheet and audio stems. V1–V3 remain in their original folders. After the master is ready, export
the first 3.5 s and a 15.25 s full-intro/vacuum preview with the final master AAC stream copied unchanged.

## Verified output

All 18 checks pass: 57.750 s, 3465 frames, 1920×1080/60 CFR, H.264 High BT.709 tv, 18.076 Mbit/s video,
AAC-LC stereo 48 kHz, -14.0 LUFS / -2.4 dBTP. No black frames or audio clipping. Limiter maximum reduction
4.64 dB, linear normalization, no render warnings. `delivery.json`, credits and the QA report are adjacent.

Master: 131,705,711 bytes; SHA-256 `86f0066bef312868e4ca0c43442634c480d2ab4f05de90c77ef65c40f5b9edb6`.

- `refluff_macro_trailer_v4_first3_5s.mp4`: 3.500 s / 210 frames / 5,071,515 bytes.
- `refluff_macro_trailer_v4_opening.mp4`: 15.250 s / 915 frames / 32,971,662 bytes.

Both previews pass 1080p60/frame-count checks and copy the final master's AAC stream unchanged. Their hash and
format inventory is `opening_previews.json`. `audio_identity.json` verifies the Foley stem and mastered PCM are
byte-identical to V3. `intro_boundary_audit.json` records mean absolute RGB difference 0.000765/255 and p99 of 0
between the new intro's final source frame and the first vacuum source frame. V1–V3 remain unchanged.
