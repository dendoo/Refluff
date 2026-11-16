# Trailer editor (lane "editor", 2026-09-26)

The offline editing engine for the Steam trailer in `Assets/CarpetCleaning/Docs/TRAILER.md`. It turns an EDL
(edit decision list) plus StoreGifApi takes into the upload master. There is no NLE step: cuts, virtual camera,
captions, logo, end card and the audio mix are all code, so a re-shot take is re-cut in one command.

| File | Role |
|---|---|
| `make_trailer.py` | renderer + mixer + encoder (EDL -> MP4, cover, contact sheet, stems, render report) |
| `titles.py` | captions (HUD pill style), #03 logo whip with foam bubbles, #21 end card, title-safe geometry |
| `check_trailer.py` | TRAILER.md 10 checks + review clips (600 px, first 5 s, festival 0:12-0:35, micro trailer) |
| `edl.json` | the trailer's EDL: 21 shots (25 cut entries: #18 and #19 are split), DRAFT with placeholder takes |
| `foley.py` | NOT this lane: `render_take(take_dir, t0, t1, sr=48000) -> float32 (n, 2)`, imported lazily |

## Commands

```
python Tools/steam_art/trailer/make_trailer.py                         # full master -> SteamStore/trailer/
python Tools/steam_art/trailer/make_trailer.py --preview               # 960x540, veryfast, no cover (~45 fps)
python Tools/steam_art/trailer/make_trailer.py --only 06,07,08 --out review.mp4   # a review cut of some shots
python Tools/steam_art/trailer/make_trailer.py --frames-at 1.5,5.6,33.0  # PNG stills, no encode
python Tools/steam_art/trailer/make_trailer.py --date "Out now"        # swap only the end card's date layer
python Tools/steam_art/trailer/check_trailer.py                        # checks the master -> _work/trailer/checks/
```

Other flags: `--edl`, `--out` (a bare name goes to `SteamStore/_work/trailer/`, only the master's own name to `SteamStore/trailer/`), `--no-audio` (no audio stream), `--jobs N`
(threads, default 16), `--crf N` (CRF with the 24M cap instead of the 20 Mbit/s ABR), `--preset`, `--stems DIR`,
`--procs N --chunk 6` (process pool; measured slower than threads on this machine, kept for other hardware).

Outputs for the default `--out SteamStore/trailer/refluff_trailer_main.mp4`:
`refluff_trailer_main.mp4`, `refluff_trailer_cover.png` (1920x1080, the frame at `cover.t` = 1.5 s, no overlays),
`refluff_trailer_sheet.jpg` (middle frame of every cut with timecode, take, fx and caption),
`SteamStore/_work/trailer/stems/{foley,sfx,music,mix_pre,mix}.wav` (float 48 kHz; `mix.wav` = the normalised master),
`SteamStore/_work/trailer/refluff_trailer_render.json` (everything printed + the audio sources used; check_trailer
reads it for the licence check), stills in `SteamStore/_work/trailer/refluff_trailer_stills/`. For any other `--out`
the side files sit next to it (`<base>_cover.png`, `<base>_sheet.jpg`, `<base>_stems/`, `<base>_render.json`).

## Master format (TRAILER.md 9, user decision 2026-09-26)

1920x1080, 60 fps CFR, H.264 High yuv420p, BT.709 tagged tv range (make_clips.py's `scale=out_color_matrix=bt709` +
`setparams` chain), `-b:v 20M -maxrate 24M -bufsize 40M`, GOP 2 s, `+faststart`; AAC-LC stereo 192 kbit/s 48 kHz;
-14 LUFS integrated, true peak <= -1 dBTP. One ffmpeg process takes raw RGB frames on stdin (no temp PNGs) and the
normalised `mix.wav` as its second input.

## EDL schema (`edl.json`)

Top level:

| Key | Meaning |
|---|---|
| `fps`, `size` | 60, [1920, 1080] (the CLI `--preview` renders 960x540 from the same EDL) |
| `bpm` | 90 (informational; `bars` below use the 90 BPM grid: 1 bar = 8/3 s = 160 frames at 60 fps) |
| `cover` | `{"t": 1.5}`: trailer second of the cover frame (rendered without captions / logo) |
| `date` | end card date layer ("October 2026"); `--date` overrides it for one render |
| `audio` | `sr` 48000, `lufs` -14, `tp` -1, `cut_fade_ms` 8, `tail_silence` 0.5 (the last 0.5 s are silent), `tail_fade` 0.6, `max_limit_db` 12 (the most the limiter may take off a peak), `limiter_release_db_s` 30 (the main EDL uses 120: ASMR crackle at 30 dB/s made the bed pump after every transient) |
| `music` | `null` now (no licence). Slot: `{"wav", "gain_db" (-16), "start" (trailer s where the file starts; 5.333 = the #03 cut), "in", "fade_in", "fade_out" (1.5), "duck_db" (null = silence), "ramp" (0.06 s)}` |
| `shots` | the cut list, in any order (sorted by start) |

Per shot:

| Key | Meaning |
|---|---|
| `id` | "01".."21" (sub-cuts "18a", "19c" ...); `--only` takes these |
| `start` + `dur`, or `bars: [first, length]` | seconds (number, "m:ss.sss" or "Nb" = N bars) or bars on the grid; cuts land on `round(t * 60)` |
| `take` | folder name in `SteamStore/_work/clips/`, any take folder path, or a still image path (held; a sibling `<stem>_ui.png` is its HUD) |
| `in` | take seconds (from the take's first frame, seq.json timing) where the shot starts |
| `speed` | 1.0; 0 = freeze on `in`; other values retime (the take audio is resampled and warned) |
| `cam` | `[{"t", "rect": [x, y, w, h], "ease"}]` in the take's scene-frame pixels. Every rect is forced to the largest 16:9 inside it and kept inside the frame; between keys the centre moves linearly and the size geometrically in the eased parameter (clip 5's virtual camera). `ease` belongs to the segment that ENDS at that key: `smoothstep` (default), `linear`, `smootherstep`, `easeInOut`, `easeOut`, `easeIn`, `hold`. No `cam` = the full frame (centre-cropped to 16:9). `t` is shot-local; `"cam_clock": "take"` makes it take time |
| `hud` | composite the take's premultiplied `fNNNN_ui.png` panel (same rect scaled to the panel, resampled as RGBa, laid over in 8-bit sRGB) |
| `title` / `titles` | `{"text", "style", "num", "pos", "at", "dur", "shadow", "alpha"}`; styles `verb` (teal chip with `num`), `line`, `refluff` (inverted, 1.3x), `headphones`, `wishlist`, `date`, `studio`; `pos` `bottom` (default: centre, 66 px above the edge), `br`, `bl`, `under_logo`, or `{"x", "y"}` fractions; `at` = seconds after the cut (default 0); `dur` = how long it stays (default: to the shot's end; longer values carry it across the next cut without re-animating, as #18 and #19 do); `shadow` = a USS shade token (`shade` default, `shade-strong` for a white pill on the white tile) |
| `fx` | list of names or objects: `"hold_last"` (a take that ends early holds its last frame, no warning); `{"type": "logo_whip", "width": 0.60, "cy": 0.43, "at": 0, "bubbles": true, "darken": 0.30, "blur": 3.0, "ramp": 0.25}`; `{"type": "endcard", "cta", "date", "studio", "width": 0.56, "cy": 0.36, "cta_y": 0.63, "logo_in": 0, "cta_in": 0.45, "date_in": 0.70, "studio_in": 0.90, "darken": 0.45, "blur": 7.5, "ramp": 0.6}`; `{"type": "wipe", "take", "in", "speed", "t0", "t1", "ease", "feather": 24, "line": true, "dir": "ltr"}` (left of the edge = the wipe take = the clean state, same camera); `{"type": "grade", "darken", "blur", "ramp"}` for any other background dim |
| `audio` | `{"take_audio": true (default false on a `speed` 0 freeze: its take sound would run on under a still), "gain_db": 0, "carry": 0 (s the take audio runs past the cut, fading out; true = 0.5), "carry_in": false (no fade-in at the head), "duck_music": false, "extra": [{"wav", "at" (shot s), "gain_db", "in", "dur", "fade_in", "fade_out", "bus": "sfx"}]}` |
| `placeholder`, `plan`, `note` | ignored by the renderer: which real take replaces the placeholder |

Blur px are at 1080p and scale with the output. All sizes in `titles.py` are 1080p values scaled by height / 1080.

## How a frame is made

1. Shot = the cut covering output frame k; shot-local `tl` = frames since its cut / 60.
2. Take frame = the last frame whose seq.json time <= `in + (tl + 0.5/60) * speed` (the middle of the output frame:
   a 60 fps take maps 1:1 even when its stored `stepDt` is rounded, verified on a synthetic 0.016667 take; the
   25 fps placeholders hold frames exactly like make_clips.py).
3. Camera rect at `tl` -> the integer box around it (+ Lanczos support) is cut from the uint8 frame, turned linear
   (make_clips' LUT) and resampled per channel with PIL's sub-pixel `box` straight to 1920x1080, back to sRGB through
   a 65536-entry table (within 1 level of make_clips.linear_to_srgb_u8, 3x faster).
4. HUD panel (optional), wipe, background grade (darken toward rgb(8,10,14), Gaussian blur); the result is cached
   (a held frame under a still camera renders once).
5. Overlays on a copy: logo whip / end card, then every caption active at k (straight-alpha sprites composited
   premultiplied in 8-bit sRGB, the way the game lays its UI Toolkit panel over the scene).

Captions (`titles.py`): Fredoka SemiBold (variable font, named instance), colours read from the `.hud` tokens in
`CarpetHud.uss` (`--surface` rgba(255,255,255,0.92): the USS token, TRAILER.md 6 says "93 %"; `--ink`
rgb(26,33,43); `--accent` rgb(20,174,160); `--shade` rgba(16,24,36,0.16); `--rim` hairline). Pill height 1.55 x
font px, radius = half the height, padding 0.42 h, chip 0.74 h square (radius 0.26 of it) inset 0.13 h so it sits
concentric in the round end, digit Fredoka Bold. Text is centred on its cap height. Shapes are drawn at 4x and
box-filtered in premultiplied alpha. Appearance on the cut: 0.15 s, scale 0.92 -> 1 and alpha, ease-out cubic.
Logo whip: scale 0.9 -> 1.03 (0.24 s, ease-out) -> 1 (0.40 s), alpha in 0.1 s; four foam bubbles (lavender body,
violet rim, white highlight, like the logo's own) fly out from behind the last "f", drift up and pop at 1.0-1.4 s.
End card: logo fades in over 0.8 s (scale 0.97 -> 1), CTA pill at 0.45 s, date layer at 0.7 s, "Vahov Games" at 0.9 s.

## Audio (TRAILER.md 7)

No music now (no licence); the takes are silent (StoreGifApi mutes the Editor and the unfocused Editor's dspTime is
frozen, so AudioRenderer records silence), so the mix is foley built from the game's own recordings:

1. Per shot, when `take_audio` is on and the take has an `audio.jsonl` (the per-Step audio-state log):
   `foley.render_take(take_dir, in + t_origin, in + t_origin + length, sr=48000)` (the foley lane; a missing module,
   a missing log or an exception = silence plus a note / warning). `t_origin` = the first captured frame's Step x
   stepDt (1/60 s on a 60 fps take): the EDL's `in` counts from the first frame (make_clips' seq.json timing), foley's
   clock from SeqBegin (Step s at s x stepDt, the frame captured right after Step s), so without it the sound lags the
   picture by one frame (review 2026-09-27: a synthetic take's flash frame and its click now land on the same frame). The render report records the clips each take plays (foley's
   `last_sources` if it has one, else the cached `foley.build_plan(take).voices` clip names) for the licence check. Head fade `cut_fade_ms` (8 ms, off with `carry_in`), tail fade 8 ms at the cut or
   over `carry` seconds past it (a machine running across a cut).
2. `extra` wavs (any format ffmpeg decodes, path relative to the project) on the `sfx` bus (or `bus: "foley"/"music"`).
3. `music` on its own bus, ducked to `duck_db` (default silence) over every `duck_music` shot with a 60 ms ramp
   centred on the cut. For TRAILER.md 7 set `duck_music` on #11 AND #12 (out from #11, back at #13) and
   `start` 5.333 (the first strong beat on the #03 cut); keep `gain_db` 12-18 dB under the machines.
4. Master = sum; the last `tail_silence` 0.5 s are zero after a `tail_fade`.
5. Loudness: `loudnorm` pass 1 on `mix_pre.wav`. If gaining to -14 LUFS would put the true peak above -1.3 dBTP, a
   numpy lookahead limiter (5 ms lookahead, 30 dB/s recovery, gain floor reached exactly at the peak) is applied from
   the unlimited mix with a ceiling that is lowered by the measured overshoot until it fits (limiting also lowers I),
   never more than `audio.max_limit_db` (12) under the loudest peak: what still does not fit comes off the loudness
   target instead (`I_eff` in the report, warned; check_trailer then fails the -14 LUFS check), because on a sparse
   mix (a few clicks) limiting lowers I as much as the peaks and the loop would squash forever (a lone click crashed
   the old loop when loudnorm returned -inf). A round loudnorm cannot measure is dropped. Its gain reduction lands in
   the report and more than 6 dB warns ("lower the transient sfx / foley gains": for
   ASMR, crushed crunches are worse than a quieter bus). Pass 2: `loudnorm` with the measured values, `linear=true`,
   LRA target = max(7, measured + 1) so the dynamics are left as they are, `-ar 48000` -> `mix.wav` (float).
   Then `mix.wav` is measured with ebur128 (the meter check_trailer uses; loudnorm's own meter read a short foley
   mix 0.4 LU low) and trimmed linearly to the target when it is off by more than 0.1 LU and the peak allows it.
   A silent mix is written as is (not normalised). `--only` cuts the full-timeline mix to the chosen shots (5 ms
   fades at the joins) before mastering.

## Checks (`check_trailer.py`, TRAILER.md 10)

PASS / FAIL lines + `SteamStore/_work/trailer/checks/check_report.json`: size, H.264 High, yuv420p, tv/bt709, 60 fps,
frame count = duration x 60, CFR (ffmpeg `vfrdet`), video bitrate 12-26 Mbit/s, AAC LC 48 kHz stereo, audio bitrate
96-208 kbit/s (ffmpeg's AAC is ABR: 192k undershoots on sparse audio: 134 kbit/s on the test mix), duration
+-0.25 s of `--expect-seconds` (62), no black frame (`blackdetect d=0.016`), ebur128 I = -14 +-1 LUFS and true peak
<= -1 dBTP (LRA printed), captions + logo + bubbles + end card inside the 5 % title-safe area (geometry from
`titles.py`, i.e. exactly what is drawn), reading time >= 1 s per 3 words (the verb chip digit counts as a word), no
Samsung dry-vac audio (EDL text + the render report's audio sources; FAIL when a video with audio has no render
report or a take's clip list is unknown), a licence file for any music. `check_report.json` is strict JSON (-inf
loudness values are strings). Review files:
`t600.mp4` (muted, 600 px), `first5.mp4` (with sound), `festival_12_35.mp4` (muted), `micro.mp4` (6 x 1 s at 5..55 s,
muted) and `micro_frames.jpg`. The end card's date is printed for the Steamworks comparison.

## Verified 2026-09-26 (lane test, scratchpad)

- Test EDL (6 cuts, 10.67 s: `1 SCRAPE` on c3_tools_r1 with a push, #03 logo whip + "Headphones on" over c4_rugs_r3b,
  `8 REFLUFF`, c5_paid_r1 HUD card push with "Get paid." bottom right, the #14 wipe c4 A -> B, the #21 end card;
  plus a recorded wav on the sfx bus and a synthetic music bed ducked on the REFLUFF cut): 640 frames, 1920x1080,
  60.0 fps, H.264 High yuv420p tv/bt709, AAC LC 48 kHz stereo, 14.8 Mbit/s (ABR undershoots on held placeholder
  frames), loudnorm linear -> -14.0 LUFS / -1.6 dBTP. check_trailer: every check PASS except "music licence" (the
  synthetic bed has none, correctly flagged). The limiter took 17 dB off one click of the test mix and warned; since
  the review cap (`max_limit_db` 12) the same mix is limited 12 dB and lands at -19 LUFS, warned (a real foley mix
  needs less: 8.2 dB on the foley self-test take, which still reaches -14.0 LUFS / -1.7 dBTP).
- Foley path: a synthetic 60 fps take carrying the foley lane's self-test `audio.jsonl` (dry vac, Dyson machine,
  crumb rattles), two cuts with `carry` 0.4: `foley.render_take` mixed, clips listed in the render report (Dyson, no
  Samsung), limiter 7 rounds / 8.0 dB (warned), ebur128 trim -0.4 dB -> mix.wav -14.0 LUFS / -1.7 dBTP, the AAC in the
  MP4 -14.1 LUFS / -2.0 dBTP (check_trailer PASS).
- Full draft `edl.json` as `--preview`: 3720 frames = 62.000 s, CFR, no black frame, 18 captions + logo + end card
  title-safe, reading time OK, silent mix handled (not normalised).
- Speed (20 logical cores, other lanes running): 1080p slow preset 11-16 output fps (~60-90 ms/frame wall; a 4K frame
  costs ~120 ms decode + ~210 ms linear-light resample single-threaded; x264 slow alone runs at 43 fps), so the full
  master takes ~4-6 min; `--preview` ~45 fps. `--procs` measured slower (10 fps) than threads.
- 60 fps mapping: a synthetic take with `stepDt` 0.016667 maps 360 output frames to 360 consecutive take frames.
- Frames looked at: captions match the storyboard pills (chip concentric in the round end, cap-height centred text,
  hairline rim, soft shade), the whip mid-animation (scale overshoot, bubbles leaving the "f"), the end card, the wipe
  line, a HUD composite through a moving sub-pixel window, and the cover (no text).

## Open

- `foley.py` (other lane) landed during this lane; its `render_take` matches the call here, but no take has an
  `audio.jsonl` yet (the 25 fps placeholders predate the audio log), so the mix stays silent until the real takes.
- HUD takes: the UI panel is 2560x1440, so a camera window narrower than 1920 panel px upscales the HUD (soft text).
  #16's push stays at 1216 px of a 1280 px card window = 1.58x of the panel: acceptable on the placeholder, check on
  the real take or capture the panel larger.
- #14 "sparks at the end" come from the take, not an fx (the wipe only draws the edge light).
- Every take in `edl.json` is a placeholder; `in` / cam rects must be re-set per real take (`plan` says which).
