# Trailer foley: `foley.py` (2026-09-26)

The `StoreGifApi` takes are silent (TRAILER.md 7): the unfocused Editor's audio clock stands still and `AudioRenderer`
records silence. A take made with `audioLog: true` logs what the audio code does, Step by Step (`AudioTap`, AUDIO.md 12;
format in STORE_CLIPS.md 5). `foley.py` rebuilds the game's own mix offline from that log and the game's own clips.

## Input

- `<take>/audio.jsonl`, as `StoreGifApi` writes it:
  - header: `{"type":"header", "stepDt", "fps", "sampleRate", "dspBuffer", "clock":"virtual", ...}`;
  - one line per Step: `{"type":"step", "s", "t", "state": ToolAudio.DebugStateJson(), "src": [...], "ev": [...]}`;
  - an event: `{src, kind: oneshot|scheduled|play|stop|playing, clip, vol, scale, pitch, delay, at, start, pan, loop, id}`;
  - an `end` line (ignored: what the take's shutdown releases is not in the game's timeline).
  - `t` is recomputed as `s * stepDt`. A `stepDt` within 1e-4 of `1/fps` counts as exactly `1/fps`, so Step s is
    video frame s.
- Event sources, both spellings accepted: the tap's `tool/powerOn|powerOff|switch|rattle`, `loop/MotorA...`,
  `rec/Recorded<Tool>/<role>`, `ui/<UiSfx>`, `ui/voice`, `pre/<GameObject>`, and the lane contract's `ToolAudio`,
  `RecordedMachine`, `UiAudio`, `Debris`, ...
- `clock:"virtual"`: the take ran RecordedMachine and the crumb rattle on a clock that advances with the Steps, so
  the logged `rec.<Tool>.phase / speed` and the rattle counters are the game's own. An older log on the frozen real
  clock (no `clock` field) shows the phases stuck in "Starting"; the fallbacks below cover it.
- `<take>/audio_clips.json` (optional): `{"name": {"asset": "Assets/..."}}` or `{"name": {"wav": "_clips/x.wav"}}`.
  Clips are resolved by name, in this order: this map, any audio file of that name under `Assets/`, then the baked
  copy of a runtime placeholder (`sfx_DryVacuum_debris_3` -> `Generated/tool_dryvacuum_debris_3.wav`).
- The per-tool numbers come from `Settings/ToolSoundBank.asset`, read at render time: `motorVolume`, `motorPitch`,
  `motorSpeedPitch`, the recording slots, the debris clips and the master volume. `ToolAudio.volume` comes from
  `SampleScene`. The machine take for a tool is found by the clip names the state shows (`rec.<Tool>.voices[].clip`),
  so Samsung or Dyson is whichever the take played. If the state shows none, the bank's slot is used. Either way the
  curves come from the matching cut JSON in `Audio/Recorded/`.

## API and CLI

```python
from foley import render_take, resolve_clip, load_wav, build_plan
y = render_take(take_dir, t0, t1, sr=48000)            # float32 (n, 2); take time 0 = SeqBegin
y, stems = render_take(take_dir, t0, t1, return_stems=True)
plan = build_plan(take_dir)                              # plan.voices, plan.warnings, plan.opts["credits"]
```

```
python Tools/steam_art/trailer/foley.py TAKE_DIR OUT.wav [--t0 S] [--t1 S] [--tail S] [--stems DIR]
       [--seed N] [--warm] [--lead-ms MS] [--latency-ms MS] [--oneshot-volume V]
python Tools/steam_art/trailer/foley_selftest.py OUT_DIR      # synthetic takes + measurements, no Unity needed
```

- **Pre-roll.** The whole log is always simulated: the machines, the loop entry points, the filter state (50 ms
  pre-roll). A render of any window is therefore sample-identical to the same stretch of a full render (tested).
- **Output.** The output is the raw game mix: master 0.85, peaks about -0.6 dBFS in the self-test. Loudness
  (-14 LUFS, -1 dBTP) belongs to the editing engine. Music is the editing engine's too.
- **Warnings** (`plan.warnings`, printed by the CLI): unresolved clips, `ToolAudio.muted` during the take, silent
  tool one-shots, the port disagreeing with a virtual-clock log, and any NON-COMMERCIAL recording in the mix (the
  CC BY-NC Samsung dry vac must not reach the trailer). `plan.opts["credits"]` lists the credit line of every
  recording used, for the video description (TRAILER.md 7).

## How each layer is emulated

| Layer (stem) | Source of truth | Emulation |
|---|---|---|
| `contact`, `motor` (LoopLayer) | state `*Clip/*Vol/*Pitch/*Cut` | Plays the logged, already-eased volume, pitch and cutoff, ramped over one Step after that Step. A (re)start enters a clip longer than 1 s at a random point (`--seed`). On a clip change, the old source keeps its pitch and filter and decays at `volumeEaseHz` 14; a source still fading from an earlier change is stopped, as `SetClip` does. Unity's `AudioLowPassFilter` is modelled as 24 dB/oct: FMOD's lowpass, 4th-order Butterworth at Q 1 ("no bump"), coefficients per 64 absolute samples. The tap's `loop/*` play / stop events are not played (the state covers them). |
| `machine_<Tool>` (RecordedMachine) | state `tool`, `recorded`, `machineOn`, `speed01` | Port of `RecordedMachine.cs` on Step time. Start / loop / stop scheduled `Lead()` ahead (42 ms at 1024 / 48 kHz, from the header's `dspBuffer` / `sampleRate`). Spin-up -> sample-exact stitch into the loop. Release / re-press enter the other clip at the current speed (`TimeAtSpeed` on the JSON curves), with 50 ms equal-power crossfades. The loop cancels while still ahead. 5 voices, the quietest fading voice stolen (judged and stopped at the call, as `FreeVoice`). Load pitch is `motorSpeedPitch x speed01`, eased at 3/s, loop only. Every machine ever used keeps ticking, so a put-down machine coasts. The `rec/*` events are not played. On a virtual-clock take the port is checked Step by Step against the logged `rec` phase / speed (warning on any difference). |
| `oneshot` (ToolAudio) | logged `tool/power*`, `tool/switch` | Played at the logged `vol`. The game's `OneShot` source is at volume 0 (AUDIO.md 12), so these clicks are silent in the game and here: `oneshot_source_volume=1` / `--oneshot-volume 1` plays them at their `scale`, as if that were fixed. An old log without such events re-derives them from state changes (tool change: switch 0.5 + the old non-recorded machine's power-off 0.8; power on / off 1.0), at the same source volume. |
| `rattle` | logged `tool/rattle` scheduled events | Each strike starts at its logged `at`. A log without rattle events rolls one strike per `rattleScheduled` increment, `lastLeadMs` ahead, size, pitch and level as `OnDebrisSucked`. Only on a FROZEN-clock log: topped up with the `rattleDropped` a Step could still have voiced (8 minus what it scheduled), because all 14 voices look busy after 14 strikes there; on a virtual-clock log every drop is the game's own. |
| `ui`, `ev_<src>` | events | As logged: clip, volume, pitch, pan (balance), `at`. `oneshot` / `play` start one Step after their call (next mix block), `scheduled` exactly at `at`. Keyed by source id: a `stop` (e.g. `ui/voice`, UiAudio's round-robin steal) cuts everything on the source, one-shots included; a new `play` / `scheduled` replaces what the source plays. Cuts get a 3 ms ramp. `playing` (`pre/*`: already sounding when the take began) is skipped: a loop the state covers, or a finished one-shot the frozen Editor still reports. |

Timing:
- A per-frame change, a one-shot or a `play` event is heard one Step (16.7 ms) after its call (`--latency-ms`).
- Machine clips are heard `Lead()` (42 ms) after their Step, as in the game.
- To sit audio tighter on the picture, pass `latency=0` / `lead_ms=0`.

`--warm` (not in the game): a machine already held at the first Step starts in its loop instead of from rest. A
virtual-clock take starts every machine from rest (StoreGifApi resets them), so prefer warming up inside the logged
take (STORE_CLIPS.md 5); `--warm` also trips the port check.

## Verified (synthetic takes, `foley_selftest.py`, 2026-09-26)

The synthetic take is 17.2 s of states from a Python port of `ToolAudio.StepFrame`, F3-rounded like
`DebugStateJson`: a dry vac press 0.3 s -> 2 s drag with speed ramps -> release; a hose burst; the brush with its
breaker one-shots; a dry vac re-press while it coasts, held into the loop; a foam burst into its loop; rattle events
plus 2 dropped strikes; UI one-shots (one through `audio_clips.json`) and a looped play / stop. The same take is
re-spelled in the tap's format (`take_tap`: virtual clock, `tool/`, `loop/`, `rec/`, `ui/`, `pre/` events, `at`).

Rendered 23.2 s in about 1.7 s. Results (main render with `oneshot_source_volume=1`, so the clacks are measured):

- **Spin-up -> loop stitches.** Dry vac and foam: bit-exact against the two clips back to back (max abs diff 0.0).
- **Joins.** Max sample jump at every join over the 95th percentile of its own ±100 ms neighbourhood: stitch 0.74
  on the stem, 0.85 in the mix; crossfades 1.06; ends 0.96 (1.0 = no worse than the signal itself).
- **One-shot attacks.** At most 1.28x the source clip's own attack played out of silence (pitch above 1).
- **Spikes** (second difference over its 10 ms RMS). Every stem's maximum equals the source clip's own: brush
  crackle 17.9 vs 19.3, hose stop 11.56 = 11.56, `ui_kaching` 14.67 = 14.67, Dyson start 5.02 = 5.02.
- **Level.** No NaN. Sample peak -0.59 dBFS, true peak -0.6 dBTP. -19.3 LUFS integrated, LRA 9.4 (ffmpeg ebur128).
- **Windows.** A 9.0-12.5 s window render equals the same stretch of the full render (max abs diff 0.0).
- **Mid-stroke take.** Cold: a spin-up from rest, -38.4 dB in the first 100 ms. `--warm`: straight into the loop,
  -24.8 dB.
- **As in the game (defaults).** 0 tool one-shot voices (OneShot source at volume 0), with a warning.
- **Tap format.** Machine, motor and contact stems bit-identical to the contract take (max abs diff 0.0); rattle =
  the 3 logged strikes at their `at` (no top-up on a virtual clock); no voice from `loop/`, `rec/` or `pre/` events;
  the 5th UI call cuts the 1st voice's one-shot on the same source id; `--oneshot-volume 1` gives the 6 clacks; one
  corrupted logged phase gives exactly one port-check warning.

## Limits

- **Mono clips** (dryer and squeegee loops, breaker clacks) play at full level on both sides (`MONO_GAIN = 1`), as
  the project's levelling assumes. Unity may centre-pan them at -3 dB instead.
- **Low-pass slope.** 24 dB/oct is FMOD's own note on its old lowpass; not measured in Unity. `_lp_sos` is the one
  function to change.
- **Timing.** Unity takes up volume / pitch changes at its own 1024-sample blocks. Here they are smooth ramps over
  one Step. The state's F3 rounding quantises loop volumes to 0.001 (the step lines' `src` has them unrounded; unused).
- **Machine volume** comes from the bank as it is when you render. Re-render if the bank changed after the take.
- **Crumb rattle and chunk knocks.** TRAILER.md 7 suggests recording the rattle and chunk knocks live (OBS) if
  these strikes sound too regular.
