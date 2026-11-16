# Store description clips (2026-09-26)

Request (RU): finish RELEASE_CHECKLIST item 2.1 ("GIFs"), capture them and put them in the game description on Steam.
Later the user asked for clip 3 (the tool relay) to open the page.

Result: five looping MP4 clips and four image headers in "About This Game", uploaded to Steamworks on 2026-09-26.
Screenshots and capsules are in `STORE_ART.md`; this doc covers only the description media.

- Files for upload: `SteamStore/description/` (clips, headers, `about_this_game.bbcode`).
- Take frames: `SteamStore/_work/clips/<take>/` (PNG per frame + `seq.json`).
- Capture API: `Editor/StoreGifApi.cs`. Encoder: `Tools/steam_art/make_clips.py`. Headers: `Tools/steam_art/make_headers.py`.
- One recipe folder per clip: `Tools/steam_art/clips/recipes/clip*/` (`run.sh` + `README.txt` + eval scripts).
- Everything was captured under the ACES tonemap (the scene is ACES since 2026-09-26, `STORE_ART.md` 3).

## 1. Why MP4, not GIF

Steam has accepted video in the written description since the 2025 "wider store pages" change. Sources:
- Valve, Steamworks announcement 2025-08-14 "Beta: Wider store pages; Video support for written game descriptions":
  https://steamcommunity.com/groups/steamworks/announcements/detail/532106189043925406
- Steam News 2025-11-07 "Store pages, now officially wider":
  https://store.steampowered.com/news/app/593110/view/506217467911081528
- Valve, Store Page Written Description: https://partner.steamgames.com/doc/store/page/description
- steam-izdat store-page guide (RU): https://steam-izdat.github.io/04-marketing/store-page/

What they say, and what we took from it:

| Rule | Source | Our choice |
|---|---|---|
| Main column is 780 px; for stills target 2x = 1560 px | Valve 2025-08 | clips and headers 1560 px wide |
| PNG, JPG, GIF, WEBP, WEBM, MP4 accepted; up to 100 MB source | Valve 2025-08 | MP4 (H.264) |
| Animated assets "must be under 12 seconds" | Valve 2025-08 | hard cap 11.5 s in the encoder and in `verify_assets.py` |
| Every GIF / video is transcoded; the smallest of MP4 / WEBM / WEBP is served | Valve 2025-08 FAQ | no GIF palette: it would band the rug dyes and the foam |
| Live pages play `<video autoplay muted loop playsinline>` WebM VP9 with an AVIF poster | a live store page's HTML | no audio track; the first frame is the poster, so it must be a good still |
| "Smaller than 5MB" per image, "below 15MB" in total | Valve description doc (older text) | kept as a page-weight target: <= 5 MB per clip, <= 15 MB for all description media |
| Text in images must be re-made per language; set alt text | Valve 2025-08, description doc | clips are textless; alt text on every asset |
| Put the first GIF above "Read More"; the first two get watched | steam-izdat | the two strongest clips sit above the fold (section 2) |
| Image headers instead of text headings; 4-5 features; 1-2 lines of text each | steam-izdat | 4 headers, 5 clips, 1-2 lines per block |
| No links, no fake Steam buttons or prices, no time words ("Coming October") | Valve description doc, 2024 rules | none in the clips, headers or text |

So the "GIFs" of the checklist are MP4 clips. `make_clips.py --gif` still writes a GIF fallback (`gif_encoder.py`), but
nothing on the page uses it.

## 2. Page layout and the fold

The live page limits `#aboutThisGame` to `max-height: 850px`, then shows "Read More". The 850 px include the "About This
Game" heading (~40 px). steam-izdat: 90-95 % of visitors never open "Read More", and the first two clips are watched
closely. So the page is built to show two clips above the fold.

Heights at the 780 px column:

| Item | Source size | Shown at 780 px |
|---|---|---|
| 2.4:1 clip (1, 2, 3) | 1560x650 | 325 px |
| 16:9 clip (4, 5) | 1560x878 | 439 px |
| Header strip | 1560x120 | 60 px |

A 16:9 clip first would leave room for only one clip above the fold. Two 2.4:1 clips fit. Clips 4 and 5 are below the
fold, so they keep the full 16:9 frame: the rug's flight crosses the whole frame, and the shop panel needs the whole HUD.

Page order (`about_this_game.bbcode`, 1845 chars, 9 `[img]` tags):

| Pos | Block | Header |
|---|---|---|
| 1 | `refluff_clip3_tools` + bold lead "Ten tools, one right order." | none (the page opens with the clip) |
| 2 | `refluff_clip1_reveal` + 1 line | `hdr_reveal` "Watch the pattern come back" |
| 3 | `refluff_clip2_water` + 2 lines | `hdr_water` "Real water, real mud" |
| 4 | `refluff_clip4_rugs` + 2 lines | `hdr_rugs` "A new rug every job" |
| 5 | `refluff_clip5_paid` + 2 lines | `hdr_paid` "Get paid, upgrade your kit" |
| - | `[h2]Headphones on[/h2]` (the clips are muted, so the text sells the real recordings) + a 5-item feature list | text |

Live check in the store preview (beta mode): `#aboutThisGame` max-height 850 px; the first clip sits at 68-393 px,
`hdr_reveal` at 462, the second clip at 528-853. Both clips are above "Read More" (the second one's last rows run into the
fold's gradient). The videos autoplay muted and loop, and the alt text is present.

The old text is backed up as `SteamStore/description/about_before_2026-09-26.bbcode` (2147 chars, 7 text blocks).
Mock: `SteamStore/_preview/description_mock.png` = the column at 780 px and at 390 px (phone), fold at 850 px in red,
real clip posters. `make_headers.py` draws it (section 4).

## 3. The five clips

File numbers are the order the clips were planned in. The page order differs: the user moved clip 3 to the top.
All are 25 fps (one frame every 2 Steps), H.264, muted, looping, 1560 px wide. Sizes are after the final package step
(preset veryslow, crf picked per clip so the total stays under 15 MB).

| Page | File (Steam asset name = base name) | Content | Dur | Size | crf | Aspect | Recipe | AD rounds |
|---|---|---|---|---|---|---|---|---|
| 1 | `refluff_clip3_tools.mp4` | One continuous take on one crusty patch of the Transylvanian rug: the scraper breaks the crust (chunks fly onto the tiles), a foam pass, the rotary brush (AREA 6 so the bristles show), the squeegee pushes the foam wall off the rim | 8.76 s | 3.47 MB | 22 | 1560x650 | `clip3_tools/` | 1 |
| 2 | `refluff_clip1_reveal.mp4` | The wet vac pulls flooded slurry out of a scraped and brushed band on the crusty Anatolian rug (`Reset(394, 0.9f)`, screenshot 01's dirt). Two horizontal passes, the turn happens off frame, the clean band holds 2.1 s | 9.96 s | 3.46 MB | 16 | 1560x650 | `clip1_reveal/` | 3 |
| 3 | `refluff_clip2_water.mp4` | Hose: three half-pressure bursts on a dry, dust-only rya (`ResetWithProfile(68, 0.36f, 0, 0, 1, 0)`), the mud patch wicks, a pink foam mound heaps up and settles | 7.20 s | 2.04 MB | 20 | 1560x650 | `clip2_water/` (`r3/`) | 3 |
| 4 | `refluff_clip4_rugs.mp4` | The spotless Transylvanian rug is handed over with sparkles and flies out; the dirty Anatolian (seed 15 @0.95) flies in. Two jobs back to back, hard cuts, no dissolve | 6.56 s | 2.87 MB | 27 | 1560x878 | `clip4_rugs/` (with `c4_clamp_on.cs`, section 9) | 3 |
| 5 | `refluff_clip5_paid.mp4` | A real hand-over: the pay slip prints line by line (COMBO, $120, coins), the card leaves, the shop opens, two `ShopBuy` purchases (brush POWER, wet vac AREA). HUD take with a virtual camera push-in | 9.68 s | 2.24 MB | 24 | 1560x878 | `clip5_paid/` | 2 |

Total description media: 14.66 MB with the 4 headers (`verify_assets.py` limit 15 MB, ALL OK).
Recipe folders are under `Tools/steam_art/clips/recipes/`. Each `README.txt` lists the staging, the take timeline with
second-by-second beats, the crop and the rejected rounds.

Why each clip looks the way it does (the full reasoning is in the READMEs):
- **Clip 3 (tools).** One lane, four tools, no cuts: the tool relay is the game's core loop. The brush got AREA 6 because
  at AREA 3 its pad was buried in the foam and only the grey top showed. Four fast scraper passes, because one pass left a
  lace of plate.
- **Clip 1 (reveal).** A second hose pass floods the band before the take. The wet vac then takes the free water with its
  slurry in one pass (slurry left 0.09 instead of 0.26), so a single real-time pass reads clean. The turn between the two
  rows happens beyond the crop: any travel across the rows swings the bright wand 90-180 degrees in view.
- **Clip 2 (water).** Cause, then effect: nozzle over the dry rug, three bursts, mud, foam, settle. The take is rotated to
  start on take frame 22 (the poster = the nozzle arriving over the dry rug). The reset dissolve sits at the loop wrap,
  where no tool is in view.
- **Clip 4 (rugs).** Every discontinuity is a hard cut from a dirty rug to the same rug spotless ("job done"). The new
  carpet is taken on the very Step the stage turns Away (an `Each` hook), with the dirt forced in the same hook.
- **Clip 5 (paid).** The card plays in real time (`udtMean` 0.02 per Step, no `PayoutSeek`). `c5_frames.py` adds an offline
  virtual camera over the full-frame HUD capture: full frame, push in on the card, pull out to the wallet while the coins
  fly, full frame for the shop. Round 1 held the card too long: the coins flew off into nothing and the wallet count was
  never seen.

## 4. Header images (`Tools/steam_art/make_headers.py`)

steam-izdat recommends image headers instead of text headings. Each header is one transparent strip, 1560x120 RGBA, shown
at 780x60 (about 390x30 on a phone):
- the feature name in sentence case, cream Fredoka 700 with a soft pillow roll and a short wool pile (the logo's letters);
- a soft cool drop shadow that lifts the cream off Steam's `#1b2838`;
- a thin teal underline (the logo runner's colour) with a cream running stitch, a madder / gold lozenge start cap and the
  logo's pink foam bubbles at its end.

This style won against two other directions. No frame, buttons, prices or URLs (Valve's rules for description images).
The alt text on Steam is the header text.

| File | Text |
|---|---|
| `hdr_reveal.png` | Watch the pattern come back |
| `hdr_water.png` | Real water, real mud |
| `hdr_rugs.png` | A new rug every job |
| `hdr_paid.png` | Get paid, upgrade your kit |

`hdr_tools` was dropped when clip 3 moved to the top: the page opens with the clip itself.

Run `python Tools/steam_art/make_headers.py [--lang en] [--no-mock] [--check DIR]`. The output is deterministic
(byte-identical on every run). It also redraws `SteamStore/_preview/description_mock.png` from the clip posters;
`--check DIR` adds a 2x mock and a strip sheet for review. Texts and languages live in `HEADERS` at the top of the script.

**Localised headers (2026-09-28).** All 29 store languages have their four headers in `SteamStore/description/<code>/`
(`hdr_*_<steam language>.png`), texts from `SteamStore/localization/<code>.json`, shaped with HarfBuzz, one size and
baseline per language, Fredoka / Nunito / Inter / Noto per script: `STORE_LOCALIZATION.md` 3. English still renders
byte-identical through the old path.

## 5. Capture pipeline (`Editor/StoreGifApi.cs`)

A static Editor-assembly API, driven with `unity command eval_file` in Play mode.

### Real Steps, not StepAll

Two ways to advance the game are used, for different jobs:

| | `CarpetDebugApi` StepAll chain (`StrokeNow`, `Hold`, `RunFrames`) | `StoreGifApi` take |
|---|---|---|
| What runs | the sims, debris and audio frame step | a full player-loop frame: `EditorApplication.Step()` (Update, LateUpdate, UI Toolkit, render) |
| Speed | fast; up to ~1000 scripted frames per eval | one Step 18.6-20.7 ms (`Probe` mean), plus ~48 ms per captured 4K frame |
| Used for | staging (scrape, wash, dry the carpet before the take) | the take itself |
| Looks right for | the carpet state | the tool head, heading filter, brush spin, HUD fades, hand-over flight, sparkles, water ripples (`_Time`) |

One Step advances `Time.deltaTime` and `unscaledDeltaTime` by `Time.fixedDeltaTime` (the take's `StepDt`), even while the
Editor is unfocused. That is 0.02 s with the project default; `SeqBegin(..., fps: 60)` sets `Time.fixedDeltaTime = 1/60`
for the take and puts it back in `SeqEnd` / `SeqAbort` (so also on leaving Play mode and before a script reload).
`Time.captureDeltaTime` is ignored by `Step` (verified 2026-09-26). The queue builders turn seconds into Steps with the
take's step, so queue them **after** `SeqBegin`. Every capture renders the
game camera like `StoreShotApi` does: an `ARGBHalf` HDR target, then an sRGB blit (`StoreShotApi.RenderHdr`, its capture
core made public this session). Frames are written as `f0001.png ...` into `SteamStore/_work/clips/<take>/`.

### The scripted pointer

Paused `EditorApplication.Step()` frames read the real OS mouse: without care, a stray tool head or cursor ring appears
wherever the mouse happens to be. `ToolController` has `[NonSerialized]` fields that replace only the mouse ray, buttons
and wheel (the real mouse path is unchanged; cleared in `Awake`):
- `ScriptedPointer` (on / off), `ScriptedUv` (carpet UV), `ScriptedDown` (left button), `ScriptedRight` (right button =
  half pressure for the hose and foam).
- A UV outside the floor domain = the pointer is off the play area: no head, no cursor ring.

`SeqBegin` turns the scripted pointer on and parks it off the play area. `SeqEnd` / `SeqAbort` give the mouse back.
Store screenshots use the same trick (`08_spotless_handover/s_spot2b.cs` parks it at uv (-20, -20), `STORE_ART.md` 3).

### API reference

A take is a queue. Nothing runs until `Run`.

| Call | What it does |
|---|---|
| `SeqBegin(name, w=3840, h=2160, cx, cy, cw, ch, hud=false, every=2, asyncPng=true, uiW=2560, uiH=1440, fps=0, audioLog=false)` | Starts a take: render size (<= 4096), crop (top-left origin; 0 = whole frame), cadence (2 = 25 fps at the default step). `fps` > 0 = one Step is 1/fps s for this take (`fps: 60, every: 1` = a 60 fps take; 0 keeps the current step). `audioLog` = also write `audio.jsonl` + `audio_clips.json` (below). Clears the take folder (frames, `seq.json`, the audio log and `_clips/*.wav`), pauses the Editor, mutes its audio, parks the pointer. Returns `stepDt`, `stepFps`, `audioLog`. |
| `Tool(tool)` | Switches the tool at this point, like its hotkey. |
| `Hover(u, v, s)` | Button up at (u, v) for s seconds: the head rests there. |
| `Stroke(u0, v0, u1, v1, s, approachMm=40, right=false)` | A player's straight drag: snaps the heading, hovers in along the line for `approachMm`, then presses and drags. |
| `Move(u0, v0, u1, v1, s, down=true, right=false)` | The pointer glides in s seconds, pressed while `down`. |
| `Hold(u, v, s)` | Pressed and still (hose puddle, foam mound). |
| `Up(s)` | Button up where the queue left the pointer (a spinning pad coasts down). |
| `Away(s)` | Pointer off the play area: no head, no cursor. |
| `Call(action)` | Runs an action before the next Step (`HandoverNow`, `TakeNewCarpetNow`, `ShopOpen`, ...). |
| `Each(s, perStep)` | Calls `perStep(localTime)` before every Step for s seconds (clip 4's "take the new carpet on the Away Step" hook). |
| `Every(n)` | Capture cadence from here on (at 0.02 s: 2 = 25 fps, 3 = 16.7, 4 = 12.5; at `fps: 60`: 1 = 60, 2 = 30); `seq.json` keeps the real Step of every frame. |
| `Run(budgetMs=3500)` | Plays the queue with real Steps until the wall-time budget; call again until `"done":true`. Reports `dtMean`, `udtMean`, `stepMsMean`, `captureMsMean`, `pendingPng`, `errors`. |
| `SeqFrame()` | One capture now, outside the cadence. |
| `SeqEnd(waitMs=4000)` | Waits for the PNG writers, restores pointer, audio, fps label and `Time.fixedDeltaTime`, closes the audio log (writes `audio_clips.json` and the WAV dumps), writes `seq.json`. Call again while `pendingPng` > 0 (until `"seqJson":true`). Stays paused. |
| `SeqAbort()` | Drops a running take without `seq.json` (the audio log gets an `"aborted":true` end line, no `audio_clips.json`). Also runs on leaving Play mode and before a script reload. |
| `UvPixelJson(u, v, w, h)` | Where carpet point (u, v) lands in a w x h render (to place a crop). |
| `CropAround(u, v, cw, ch, w, h)` | A crop centred on (u, v), shifted inside the frame: the numbers for `SeqBegin`. |
| `ShopBuy(tool, stat)` | A shop click through `ShopView.OnBuy` (reflection), so the pip pop, sparkles and wallet roll play. Buys for real. |
| `Probe(steps=10, w=1280, h=720, hud=false, uiW=2560, uiH=1440, fps=0)` | Cost probe: Step ms, dt, render / read / encode ms; checks the PNG orientation. `fps` > 0 runs the Steps at 1/fps s (restored before it returns); the result has `stepDt`. |
| `SetFlip(on)` | Flips PNG rows; only if `Probe` reports `"arrayPng":"flipped"`. |

An eval times out after ~5 s of main-thread work, so `Run` stops at `budgetMs` and the next `Run` carries on. Keep
`EditorApplication.isPaused = true` between evals (`SeqBegin` sets it); unpause before `editor_stop`. PNGs are encoded
off the main thread (`EncodeArrayToPNG`); at most 2 x cores encodes are in flight.

Example (clip 3's take, shortened). One eval stages, queues and starts the take:

```csharp
CarpetCleaning.CarpetDebugApi.ResetWithProfile(154, 0.6f, -1f, 0.1f, -1f, -1f);   // crust = seed's, mud 0.1
CarpetCleaning.EconomyDebugApi.LiveTuning(true);
CarpetCleaning.CarpetDebugApi.SetTool(0);
CarpetCleaning.CarpetDebugApi.RunFrames(3);
CarpetCleaning.StoreGifApi.SeqBegin("c3_tools_r1", 3840, 2160, 1560, 1088, 1680, 700);
CarpetCleaning.StoreGifApi.Hover(0.585f, 0.26f, 0.20f);
CarpetCleaning.StoreGifApi.Stroke(0.62f, 0.26f, 0.98f, 0.26f, 0.50f);       // scraper
CarpetCleaning.StoreGifApi.Up(0.12f);
CarpetCleaning.StoreGifApi.Tool(3);
CarpetCleaning.StoreGifApi.Stroke(0.62f, 0.26f, 0.97f, 0.26f, 0.80f, 40f, true);   // foam, half pressure
CarpetCleaning.StoreGifApi.Away(1.30f);
return CarpetCleaning.StoreGifApi.Run(3000);
```

Then `return CarpetCleaning.StoreGifApi.Run(3500);` until `"done":true`, then `return CarpetCleaning.StoreGifApi.SeqEnd(4000);`
until `"seqJson":true`. The `run.sh` files loop exactly like this.

### 60 fps takes (`fps: 60`)

What a 1/60 s Step changes in the game (grep of `Scripts/` and `Compute/`, 2026-09-26). Everything that moves is
driven by `Time.deltaTime` / `unscaledDeltaTime` with bounded `1 - exp(-k dt)` eases, nothing uses `FixedUpdate` or
physics, so the look is the same at either step. The differences:
- The carpet and floor sims substep at a fixed `DtSim = 1/240` with `nSub = round(dt / DtSim)`: 0.02 s gives 5 substeps
  (the water moves 4 % faster than the chemistry and the clock), 1/60 gives exactly 4. 60 fps is the rate the scripted
  `StepAll` numbers (`CarpetDebugApi.FrameDt = 1/60`) were measured at, so it matches SESSION_LOG better than the 50 Hz takes.
- Per-frame counts scale with the frame rate: the stats readback every 10 frames (HUD bars refresh at 6 Hz instead of
  5), at most 8 crumb-rattle voices per frame, at most 8 squeegee plows per frame, one tool stamp per frame (shorter
  segments, the same mm/s).
- `Time.fixedDeltaTime` is global: it is restored by `Release` (`SeqEnd`, `SeqAbort`, leaving Play, script reload). If an
  Editor dies mid-take nothing restores it in that session; the project setting on disk stays 0.02. `Probe` and
  `SeqBegin` report the step they ran at (`stepDt`).

### Audio log (`audioLog: true`)

The Editor cannot record the game's audio here: `AudioRenderer` gives silence because the unfocused Editor's
`AudioSettings.dspTime` stands still. An `audioLog` take instead writes what the mix does, for the offline foley render
(`TRAILER.md` 7). How it works:
- **`AudioTap`** (`Scripts/Audio/AudioTap.cs`, `AUDIO.md` 12) reports every `PlayOneShot`, `Play`, `PlayScheduled`,
  `SetScheduledEndTime` and `Stop` of the game's sound code. Nothing audible changes; with the tap off each site costs
  one bool read.
- **Virtual audio clock.** `AudioTap.ClockOverride` = `dspBase + t`, where `t` = Steps x `stepDt` and `dspBase` = real
  dsp + 1000 s (so nothing scheduled on it can sound). The recorded machines (spin-up -> loop -> coast-down), the crumb
  rattle and the debris capture stamp all run on it, so their phases and delays behave as in the game. `SeqBegin` calls
  `ToolAudio.ResetScheduling()` (recorded machines silenced, rattle voices freed) before the tap listens, so whatever
  staging scheduled on the frozen real clock does not blip at t = 0; `Release` puts the real clock back and calls it
  again. Each clock switch bumps `AudioTap.ClockEpoch`, and `DebrisSystem` drops crumb captures read back across one
  (a capture from a take's last frames would otherwise be scheduled into the next take's timeline).
- A take starts every recorded machine from rest. For a machine already running in the first frame, warm up **inside**
  the logged take (`SeqBegin(..., every: 100000, fps: 60, audioLog: true)`, the warm-up items, then `Every(1)`): the log
  covers the warm-up Steps and `seq.json` `frameSteps` say where the frames begin. A separate warm-up take does not
  carry the machine over.
- Every take's timeline: step line `s` is the state right after Step `s`, the same numbering as `seq.json` `frameSteps`;
  `t = s x stepDt`. An event's `at` is the take second it starts (or stops) on that same clock.

`<take>/audio.jsonl`, one JSON object per line (InvariantCulture numbers):

| Line | Fields |
|---|---|
| header (first) | `type:"header"`, `take`, `stepDt`, `fps` (1/stepDt), `sampleRate`, `dspBuffer`, `speakerMode`, `clock:"virtual"`, `dspBase`, `startStep`, `listenerVolume`, `listenerPaused`, `mixerGroups` (sources routed to an AudioMixer group: 0, the project has no mixer) |
| step (one per Step) | `type:"step"`, `s`, `t`, `state` (the raw `CarpetDebugApi.AudioJson()` object: `motorClip/Vol/Pitch/Cut`, `contactClip/Vol/Pitch/Cut`, `spin`, `speed01`, `rec.<Tool>.phase/speed/voices[role, clip, pos, gain, volume, pitch]`, ...), `src` (every playing source: `id`, `clip`, `vol`, `pitch`, `cut` = its low-pass cutoff when it has one, `loop`), `ev` (the events since the previous line) |
| end (last) | `type:"end"`, `s`, `t`, `aborted`, `events` (total), `ev` (events after the last Step), `sources` (`id` -> GameObject name), `mixerGroups` |

An event: `src` (who: `tool/powerOn`, `tool/powerOff`, `tool/switch`, `tool/rattle`, `loop/MotorA|MotorB|ContactA|ContactB`,
`rec/Recorded<Tool>/start|loop|stop|release`, `ui/<UiSfx>`, `ui/voice`, `pre/<source>`), `kind` (`oneshot`, `scheduled`,
`play`, `stop`, `playing` = already playing when the take began), `clip` (key into `audio_clips.json`), `vol` (final:
source volume x volumeScale), `scale` (the volumeScale alone), `pitch`, `delay` (s from the call; a PlayScheduled's
scheduled dsp - now, a `SetScheduledEndTime` stop's end - now), `at` (take seconds = call clock - dspBase + delay),
`start` (clip seconds it starts from: the loops' random entry point, a recorded transition's offset), `pan`, `loop`,
`id` (source id, stable within the take). Events emitted outside `Run` (a `Call` item, `SeqBegin`) land on the next
step line.

Rendering rules the log assumes (Unity semantics):
- `play` / `scheduled` replace what that source plays (a PlayScheduled on a playing source cuts it at the call); its
  one-shots keep ringing. `stop` cuts everything on the source, one-shots included (UiAudio steals its oldest voice so).
- `vol` of `play` / `scheduled` is the level at the call; continuous voices (the loops, the recorded machines' voices,
  which start at 0 and are faded per frame) take their level, pitch and cutoff from the step lines' `src` (or `state`).
  A one-shot keeps `vol`, scaled by later changes of its source's volume (none in this project).
- A one-shot or non-looping voice ends at its clip's end (`length / pitch`); in the frozen Editor `isPlaying` never
  turns false by itself, so `src` may still list a finished rattle voice.
- `tool/power*` and `tool/switch` log `vol` 0: ToolAudio's `OneShot` source is created at volume 0, so these clicks
  should be silent in the game too (a live bug, left as is and still to confirm by ear; `scale` says what they asked for).

`<take>/audio_clips.json` (written by `SeqEnd`): clip key -> `{channels, frequency, samples, length, loadType}` plus
`asset` (`Assets/...`, an imported clip) or `wav` (`_clips/<key>.wav`, the 16-bit PCM of a clip made at runtime:
the ProceduralSfx / UiSfxSynth placeholders) or `error`. A second, different clip with the same name is keyed `name#2`.

### HUD takes (2560x1440)

The UI Toolkit panel lays out only at the Game view size (2560x1440), whatever size the camera renders at. A HUD take
(`hud: true`) therefore captures two layers per frame:
- `f0001.png`: the scene crop of the camera render;
- `f0001_ui.png`: the panel (premultiplied RGBA) captured at 2560x1440 and cut with the same crop scaled by `uiW / w`.

The render must have the Game view's aspect. At 3840 -> 2560 the crop is widened to multiples of 3 render pixels, so it
lands on whole panel pixels (`SeqBegin` returns both `crop` and `cropAsked`). `make_clips.py` composites the layers.
Clip 5, the only HUD clip, renders at 2560x1440 (full frame, 1:1 with the panel) and gets its framing from
`c5_frames.py`. A HUD take also turns on `StoreShotApi.MarketingHud(true)` (no fps label) and restores it at the end.

### Probe numbers

`Probe` before the first take (Play mode, D3D12, this machine):

| Measure | Value |
|---|---|
| One real Step | 18.6-20.7 ms mean |
| `Time.deltaTime` / `unscaledDeltaTime` per Step | 0.02 / 0.02 s (= `Time.fixedDeltaTime`; 1/60 with `fps: 60`, verified 2026-09-26) |
| One captured 4K frame (render + read + async PNG) | ~48 ms |
| `EncodeArrayToPNG` output | upright (no `SetFlip` needed) |

The takes measured their own numbers in `seq.json`: clips 1-3 (a crop of the 4K render) Steps ~7-8 ms and 25-28 ms per
capture; clip 4 (full 4K frame) Steps 11-13 ms and 72-76 ms per capture; clip 5 (2560x1440 + HUD layer) Step ~8 ms and
~60 ms per capture. Every take has `dtMean` = `udtMean` = 0.02, so unscaled-time UI (the pay-slip card, USS transitions)
plays in real time too.

### Before a take

- Snapshot the economy save first (`carpet_save_editor.json`): every clip writes tool levels, clips 4 and 5 add real
  hand-overs, and clip 5 buys for real (`ShopBuy`). This session restored it byte for byte after `editor_stop` (md5 checked).
- Nothing in a take recompiles: C# edits during Play wipe the private RenderTextures (`CLAUDE.md`).
- Staging is fast scripted calls; only the take uses real Steps. Warm up with a short uncaptured take
  (`SeqBegin("c1_warm", ..., every: 100000)`) when the head must already be moving in the first frame.

## 6. The encoder (`Tools/steam_art/make_clips.py`)

```
python Tools/steam_art/make_clips.py OUT.mp4 TAKE [TAKE ...] [options]
```

TAKE is a folder written by `StoreGifApi` or just the take name (looked up in `SteamStore/_work/clips/`). Several takes
play back to back (clip 4). OUT without a folder goes to `SteamStore/description/`. It writes OUT, `<stem>_poster.png`
(the first frame) and `<stem>_sheet.png` (12 evenly spaced frames), and prints one JSON line with the duration, fps, frame
count, bytes and size, read back from the file.

Per frame, in this order:
1. **composite** (HUD takes): the scene crop is downscaled (Lanczos, linear light) to the panel crop's size, and the
   premultiplied panel is laid over it in 8-bit sRGB, exactly as `compose_screens.py` builds the store screenshots.
2. **`--crop x,y,w,h`**: an optional extra crop in the take's frame pixels.
3. **fit**: centre-crop to the output aspect (only when `--height` forces one, or a later take's aspect differs).
4. **downscale**: Lanczos in linear light to `--width`, so thin bright fibres and the grout do not darken.

Timeline: each frame is shown from its Step (`seq.json` `frameSteps` x `stepDt`) until the next one, sampled at a constant
`--fps`. A cadence change (`Every`) keeps real time by repeating frames.

| Option | Default | Use |
|---|---|---|
| `--width` | 1560 | 2x Steam's 780 px column |
| `--height` | 0 (= from the aspect) | force an aspect |
| `--crop x,y,w,h` | none | extra crop (clip 1: `174,82,1812,755`) |
| `--fps` | 25 | one frame per 2 Steps |
| `--hold-last` | 0 | a still at the end |
| `--fade-loop` | 0 | crossfade the end into the start; the clip starts that many seconds in and gets that much shorter |
| `--crf` / `--preset` | 18 / slow | the final clips use crf 16-27 at `veryslow` |
| `--max-seconds` / `--force` | 11.5 | refuses clips of 11.5 s or more unless `--force` |
| `--jobs` | min(8, cores) | parallel frame work |
| `--gif OUT.gif`, `--gif-width 780`, `--gif-budget-mb 5`, `--gif-dither blue\|bayer\|none\|fs` | off | GIF fallback via `gif_encoder.py` (a bare name goes to `_work/clips/`) |

Output format: MP4, H.264 High, `yuv420p`, BT.709 primaries / transfer / matrix, TV range (tagged in the stream and the
container), no audio track (`-an`), `+faststart`. Scaling to yuv uses `out_color_matrix=bt709:out_range=tv` with
accurate rounding and full chroma interpolation.

Dependency: `pip install --user imageio-ffmpeg` (bundled ffmpeg 7.1 with libx264 and libvpx-vp9). ffprobe is not bundled,
so `clip_probe.py` inspects a clip by parsing `ffmpeg -i` and a stream-copy pass to a null muxer
(`python Tools/steam_art/clip_probe.py CLIP ...` prints one JSON line per clip).

Per-clip helpers: `clip2_water/r2/rotate_take.py` rotates a take to start on a chosen frame and blends a dissolve at the
wrap (clip 2); `clip5_paid/c5_frames.py` is the virtual camera (clip 5); `clip5_paid/build_take.py` assembles the take eval.

**Checks.** `python Tools/steam_art/verify_assets.py` also checks `SteamStore/description/`: every clip under 11.5 s, at
least 780 px wide, no audio stream, each file <= 5 MB, all description media together <= 15 MB, headers at least 780 px
wide. The `_poster.png` / `_sheet.png` previews are skipped (not uploaded), as is a GIF next to a same-stem MP4 / WebM.
Last run: ALL OK, description total 14.66 MB.

## 7. Reproducing a clip

Each recipe folder is self-contained. From Git Bash, with the Editor in Play mode (nothing recompiles; every script keeps
`EditorApplication.isPaused = true`):

```
bash Tools/steam_art/clips/recipes/<clip folder>/run.sh
```

`run.sh` runs the staging evals, the take eval, the `Run` loop, the `SeqEnd` loop, and then `make_clips.py` with the
clip's exact options. It writes the MP4, poster and sheet straight into `SteamStore/description/`.

| Clip | Folder | Time | Re-run gives | Economy save |
|---|---|---|---|---|
| 1 reveal | `clip1_reveal/` | ~1-1.5 min | the same frames (deterministic staging; frames matched round 2 to the pixel mean) | pre-kit levels (`eco_hero`) |
| 2 water | `clip2_water/` | ~1 min | the same look; spray droplets are random | pre-kit levels, hose POWER 0 |
| 3 tools | `clip3_tools/` | ~1 min | the same look; chunks and dust are random | PRE2 mid-game levels + brush AREA 6 |
| 4 rugs | `clip4_rugs/` | ~2.5 min | the same look; sparkle positions differ | PRE2 levels + two real hand-overs ($0 each) |
| 5 paid | `clip5_paid/` | ~1.5 min | the same timeline (scripted; card in real time) | pre-kit levels, $1,840, 6 carpets delivered, one hand-over +$120, two purchases -$140 |

Snapshot `carpet_save_editor.json` before, restore it after (section 5). After a re-encode:
- run `python Tools/steam_art/verify_assets.py` (the 15 MB total is tight: 14.66 MB now);
- run `python Tools/steam_art/make_headers.py` to redraw the mock from the new posters;
- upload the changed clip again (section 8).

Clip-specific steps:
- **Clip 2** encodes `c2_water_r3_loop`, a rotated copy of the take (`r2/rotate_take.py c2_water_r3 c2_water_r3_loop 22 --fade 0.4`).
- **Clip 4** runs `c4_clamp_on` first and `c4_clamp_off` last (section 9, item 1). Two takes, `c4_rugs_r3a` + `c4_rugs_r3b`,
  are encoded back to back.
- **Clip 5** builds its take eval with `build_take.py` (header + `c5_dress.cs` + `c5_take_body.cs`), then applies the
  virtual camera with `c5_frames.py` and encodes the `c5_paid_r1_cam` take.

Rejected rounds stay in each folder (`_rejected/`, `_trials/`, `_r1/`, `_r2/`, `r2/`) with their own READMEs.

## 8. Uploading to Steamworks

Done on 2026-09-26. Store Admin, the store page's Description tab ("About This Game"):

1. **Upload the media.** "Upload Custom Images": the 5 MP4 and 4 header PNGs from `SteamStore/description/` (not the
   `_poster` / `_sheet` previews). Each lands in the page's image library under its base name.
2. **Paste the text.** `about_this_game.bbcode` goes into the About This Game field. It references each asset as
   `{STEAM_APP_IMAGE}/extras/<file base name>` (no extension). A replacement file must keep the same base name, or the
   bbcode must change with it.
3. **Alt text** for all 9 assets: clips get a one-sentence description, headers get their own text. The alt text lives in
   Steam only; it is not in the bbcode or the files.
4. **Save** with the sticky footer "Save". The page reloads with "Changes saved".
5. **Check** in the store preview (beta mode): the fold numbers of section 2, autoplay, loop, alt text.
6. **Do not publish.** "Publish" and "Mark as ready for review" were deliberately not pressed (the user's decision).

Quirks learned (useful when the upload is scripted, e.g. from a browser automation):
- "Select file" makes the page create and click a hidden `<input type=file>` in a form appended to `<body>`. To automate:
  patch `HTMLInputElement.prototype.click` to capture that input, or call the drop zone's React `onDrop` (the div whose
  class contains `DragAn`) with a `DataTransfer`.
- PNGs stage at once. An MP4 stages only in a VISIBLE tab (`document.visibilityState` `visible`): Chrome does not load
  video metadata in a hidden tab, and the staged card never appears.
- Deleting from the image library opens a Steam modal, not a native `confirm()`.
- The About This Game textarea is React-controlled and mirrors into a hidden input `app[content][about][english]`. Only the
  sticky footer "Save" (the submit input of form `gameform`) saves; a click on a hidden duplicate Save does nothing.
- The Steamworks session expired once mid-work; log in again and re-check what was staged.

The screenshot upload has its own quirks (drop-only zone, age flags, SHA1 check): `STORE_ART.md` 3.

## 9. Known issues

1. **Hand-over corner clipped through the tiles: FIXED in code 2026-09-26, pending Play verification.** In
   `CarpetHandover.ApplyPose` the FlyOut position eases `InQuad(t)` while the tilt eases `Smooth(t/0.70)` around the rug
   centre, so `outEuler(-18, 4, 6)` dropped the near-left corner before the lift caught up. Measured with real Steps
   (`clip4_rugs/_diag/c4_clamp_probe.cs`): -77.9 mm on the way out, -5.6 mm on landing. Clip 4 hid it with a capture-time
   `RenderPipelineManager.beginCameraRendering` hook (`c4_clamp_on.cs` / `c4_clamp_off.cs`; 0.0 mm with the hook).
   - The game now applies the same rule. `ApplyPose` ends with `pos.y = Mathf.Max(pos.y, CornerLift(rot))` before
     `st.localPosition`, where `CornerLift` = how far the stage must rise so that no corner turned by `rot` sits below its
     resting height.
   - It covers FlyOut, FlyIn and `Seek`, and it is a no-op for a flat rug.
   - `c4_clamp_on.cs` is now a no-op and can be dropped.
   - Verify: `Tools/steam_art/trailer/fix_probes/v1_corner_lift.cs` must print out / in worst 0.0 mm; details in
     `HANDOVER.md` 10.
2. **Floor water is matte.** It has no gloss, so a puddle on the tiles reads as a cast shadow at store size (this sank the
   squeegee screenshot, `STORE_ART.md` 3). A renderer fix, not a capture fix.
3. **Loop dissolves.** Cleaning cannot run backwards, so no clip loops by itself. Clips 1, 3 and 5 crossfade their last
   0.5 / 0.4 / 0.4 s into the start (`--fade-loop`): for that moment two states show at once (clips 1 and 3: the clean
   result and the dirty start with the tool fading in; clip 5: the shop and the rug at rest). Clip 2 puts its 0.4 s
   dissolve at the wrap, where no tool is in view (`rotate_take.py`). Clip 4 uses hard cuts only (dirty rug -> the same
   rug spotless = "job done").
4. **Paused Steps read the OS mouse.** Without `ToolController.ScriptedPointer` a stray tool head or cursor ring shows up in
   captures (it happened in the first ACES take of screenshot 09). Editor-only; `SeqBegin` handles it.
5. **ACES darks.** Dark dyes and wet pile go near-black under ACES; the user chose to keep ACES as is, with no shadow
   lift. The clips were captured under ACES like the screenshots.
6. **Crust plates** have one size, one light greige tone and a visible shell grid on top, so from a distance they read as a
   pebble / cork mat under ACES. Size / height / tone variation is a later renderer task.
7. **No head models** for the foam sprayer, dry vac and dryer (ring cursor only), so clip 3's foam pass shows a ring.
8. Small nits, not fixable in the capture:
   - seed 394's spill streaks stay in clip 1 (whites slightly creamy);
   - in clip 5 the shop price labels sat at the top of their pills and the floor-tool key chips showed '·'. **Both are
     fixed in code 2026-09-26, pending Play verification** (`HUD.md` 9):
     - `.ubuy` now centres its label;
     - Detergent / Mop (mouse-only, no hotkey) now show no key chip instead of "·".
     - Probes: `Tools/steam_art/trailer/fix_probes/v2_shop_pills.cs`, `v3_key_chips.cs`.
     - Clip 5 itself still shows the old look until it is re-rendered (the trailer's #16 / #17 re-render at 60 fps
       anyway).

## 10. Files

| Path | What |
|---|---|
| `Assets/CarpetCleaning/Editor/StoreGifApi.cs` | the take API (section 5) |
| `Assets/CarpetCleaning/Scripts/Audio/AudioTap.cs` | the audio tap + virtual dsp clock behind `audioLog` (section 5, `AUDIO.md` 12) |
| `Assets/CarpetCleaning/Scripts/ToolController.cs` | `ScriptedPointer` / `ScriptedUv` / `ScriptedDown` / `ScriptedRight` |
| `Assets/CarpetCleaning/Scripts/StoreShotApi.cs` | shared capture core (`NewCaptureTarget`, `RenderHdr`, `FreeCaptureTarget`, `MarketingHudOn`) |
| `CarpetDebugApi.ResetWithProfile(seed, level, crust, mud=-1, dust=-1, spill=-1)` | a seed's dirt with chosen layers (negative = keep the seed's roll); backed by the new `CarpetSimulation.ResetCarpet(int, bool, DirtProfile)` overload |
| `Assets/CarpetCleaning/Scripts/CarpetHandover.cs` | `ApplyPose` corner lift (`CornerLift`, section 9, item 1; fixed 2026-09-26, pending Play verification) |
| `Tools/steam_art/make_clips.py`, `clip_probe.py`, `gif_encoder.py` | encoder, ffmpeg probe, GIF fallback |
| `Tools/steam_art/make_headers.py` | headers + description mock |
| `Tools/steam_art/verify_assets.py` | size / length / audio / budget checks |
| `Tools/steam_art/clips/recipes/clip*/` | one recipe per clip (`run.sh`, `README.txt`, evals, rejected rounds) |
| `SteamStore/description/` | upload set: 5 MP4, 4 headers, `about_this_game.bbcode` (+ previews, old text backup) |
| `SteamStore/_work/clips/` | take frames (`c1_reveal_r3`, `c2_water_r3`, `c2_water_r3_loop`, `c3_tools_r1`, `c4_rugs_r3a/b`, `c5_paid_r1`, `c5_paid_r1_cam`) |
| `SteamStore/_preview/description_mock.png` | the page column mock (780 + 390 px, fold at 850 px) |
