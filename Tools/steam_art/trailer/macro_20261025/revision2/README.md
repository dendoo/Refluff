# Macro trailer — revision 2

The new 9 s opening starts between the clean red pile fibers (0–0.25 s), pulls back to show the entire rug by 3.3 s,
holds the full rug to 4 s, then eases back into the existing vacuum camera from 4 to 8.7 s and holds to 9 s.
The carpet gradually becomes dirty from 4.2 to 8.5 s. The new capture finishes with the same camera and dirty state
as the first vacuum frame (root's endpoint RT audit: maximum absolute difference 0).
These camera and state changes are captured in Unity; the offline edit does not crossfade or fabricate them.

The revised cut is 56 s. Takes 02–09, source in points, action speed, tool sound timing, sound levels and final
logo/CTA are unchanged; their cuts move 6 s later. The genuine vacuum motor prelap moves with the vacuum cut;
the genuine quiet dryer carry moves with the reveal/end. No voice, music or release date.

```powershell
python Tools/steam_art/trailer/macro_20261025/revision2/compose_v2.py --preflight
python Tools/steam_art/trailer/macro_20261025/revision2/compose_v2.py
python Tools/steam_art/trailer/macro_20261025/revision2/compose_v2.py --qa
```

The EDL uses `mf01b_intro` for shot 01 and the locked v1 takes for the rest. The common wrapper validates and checks
the duration derived from the EDL, rather than assuming 50 s. Extra renderer flags work as before.

New output: `SteamStore/trailer/macro_20261025/revision2/refluff_macro_trailer_v2.mp4`, with its own preflight,
cover, sheet, stems, render report and QA. The v1 master and its reports remain in their original folder.

## Verified output

All 18 checks pass: 56.000 s, 3360 frames, 1920×1080/60 CFR, H.264 High BT.709 tv, 17.990 Mbit/s video,
AAC-LC stereo 48 kHz, -14.0 LUFS / -2.3 dBTP. No black frames or audio clipping. Limiter maximum reduction
remains 4.62 dB, with linear normalization and no render warnings. Final logo/CTA stays visible.

Master: 127,144,526 bytes; SHA-256 `1369b55e755ffd388ff46c01fc4a0d0673ccb20f54b92be64cddac604d32614b`.
`delivery.json`, `AUDIO_CREDITS.txt` and `qa/check_report.json` sit beside the video.

The source boundary is effectively identical: final new-intro frame vs first vacuum frame has mean absolute RGB
difference 0.000765/255 and 99th percentile 0. The full clean rug is visibly framed at 3.3 s before the return.

`refluff_macro_trailer_v2_opening.mp4` is a 13.500 s / 810-frame, 1080p60 review of the new opening through the start
of vacuuming (31,324,448 bytes). Its AAC audio stream is copied from the final master without normalization.
