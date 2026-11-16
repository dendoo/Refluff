# Audio — tool voices

Every tool makes a sound that follows what the player is doing: the machine spins up when the button
goes down and sags when it comes up, and the surface layer gets louder and brighter the faster the
head is dragged. Nothing is taken from videos.

**Since 2026-09-25 every tool plays real recordings** (section 11). They come from Freesound (CC0 / CC BY) and
Mixkit, and each one is cut into the bank by a script that can redo it (`Tools/audio_recordings.py`). The credits
are listed in `AUDIO_CREDITS.md`.
- Six machines are whole takes played start → loop → stop by `RecordedMachine` (section 9): the dry and wet
  vacuums, the dryer, and the three sprayers.
- The brush's floor machine is a real loop on the procedural spin.
- Every surface layer is a real loop.

The synthesiser `ProceduralSfx` (sections 3-4) is still there: its clips are the fallback of every slot (a machine
with `useRecording` off, an empty slot), and the crumb rattle and the tool-switch tick are still synthesised.

**The dry vacuum's machine is InspectorJ's Dyson since 2026-09-26** (Freesound 430632, CC BY 4.0, 11.4), imported for
the trailer, which must not carry the Samsung take (Freesound 100210 by Debsound, section 9, **CC BY-NC 4.0**:
non-commercial only). The Samsung cut stays on disk as a reference and must never ship.

## 1. Files

| File | Job |
|---|---|
| `Scripts/Audio/ToolAudio.cs` | the driver: reads `ToolController`, drives two looping layers + one-shots |
| `Scripts/Audio/ToolSoundBank.cs` | `ToolSoundEntry` per tool (clip slots + numbers), `SfxRecipe`, `ToolSfx` recipe→generator |
| `Scripts/Audio/ProceduralSfx.cs` | the synthesiser: pink/white noise, SVF/one-pole filters, noise bursts, bubbles |
| `Scripts/Audio/RecordedMachine.cs` | a machine layer from one real take: spin-up → stitched loop → coast-down (section 9) |
| `Editor/ToolSoundBaker.cs` | bakes the placeholders to WAV, measures every clip, menu `CarpetCleaning/Audio/*`; `ImportMachineRecording`, `ImportClip`, `ImportShippedRecordings`, `ReimportRecordings`, `Credits` (11.5) |
| `Settings/ToolSoundBank.asset` | the authored bank (created by `CarpetSceneBuilder`, hand-tunable) |
| `Audio/Generated/*.wav` | the 41 baked placeholder clips (25 + 16 tube rattles): the fallback of every recorded slot, and the rattle |
| `Audio/Recorded/*.wav` + `*.json` | every real clip the bank plays, each with the description of its cut (source, spans, gain, credit) |
| `Audio/Sources/*` | the downloaded sources the clips are cut from (section 11); referenced by nothing, so never in a build |
| `Audio/100210__debsound__vacuum-cleaner-samsung-easy-1300.wav` | the untouched Samsung take; referenced by nothing, so never in a build |
| `Tools/analyze_sfx.py` (project root, outside Assets) | objective tonality / wobble analysis of any WAV folder; `--hits` for impact clips |
| `Tools/cut_machine_recording.py` | cuts a machine take into start / loop / stop and measures its speed curves (section 9.2, 11.3) |
| `Tools/prepare_recording.py` | a seamless surface / motor loop or a one-shot from a recording (11.3) |
| `Tools/audio_recordings.py` | the manifest: every source, licence and cut; `build` redoes them all, `credits` prints the attribution (11.2) |
| `Docs/AUDIO_CREDITS.md` | what a release must credit, and the licences |
| `Compute/Debris.compute` + `Scripts/DebrisSystem.cs` | crumb-capture events for the tube rattle (section 8) |
| `Scripts/Audio/AudioTap.cs` | a listening tap on every sound started / stopped + the dsp clock override, for offline audio of capture takes (section 12) |

`CarpetSceneBuilder` puts `ToolAudio` on `CarpetRoot`, wires it to the `ToolController` and the bank,
and makes sure the main camera carries an `AudioListener` (it now does that even when the camera
already existed, which it did not before). The `AudioSource`s (four loop sources with an `AudioLowPassFilter` each, one one-shot, 14 rattle voices) live on a runtime-only child
`ToolAudioSources` with `HideFlags.DontSave`, so they never end up in the scene file.

## 2. What drives what

`ToolAudio.StepFrame(dt)` runs once per frame (and is called by `CarpetDebugApi.StepAll`, so scripted
strokes sound exactly like real ones — section 6). The mute key is read in `Update`, not in
`StepFrame`, because `StepAll` calls `StepFrame` many times inside one Unity frame.

**Speed and spin move brightness, not pitch.** Every loop source carries a non-resonant
`AudioLowPassFilter` (Q 1: a resonant sweeping low-pass is a wah pedal). The first voicing carried
speed and spin in pitch instead — surfaces slid ±22 % with speed, vacuums swept 0.45× → 1× in 0.3 s —
and a noise loop sliding in pitch is precisely the sci-fi "whoosh". Real friction gets *brighter* when
dragged faster; a real motor gets brighter as well as faster while it spools.

**The machine layer** — vacuums, dryer, sprayers and the rotary brush. `powered` entries start on the first stamp and stop
`strokeReleaseGrace` (0.12 s) after the last one, so a pointer that leaves the carpet for a frame does
not chop the motor. Spin eases to 1 at `spinUpHz` and back at `spinDownHz` (slow for impellers, which
coast). Cutoff runs log-linearly from `motorCutoffIdleHz` to 22 kHz with spin; pitch is
`motorPitch · lerp(spinPitchMin, 1, spin) · (1 + motorSpeedPitch · speed01)` with `spinPitchMin`
0.70–0.72 for motors and 0.97–0.98 for spray valves. Starting and stopping fire `powerOn` / `powerOff`.

**The surface layer** — what the head does to the pile. Volume is
`contactVolume · lerp(contactIdle, 1, speed01) · pressure · (tile ? tileVolume : 1)`; cutoff runs
log-linearly from `contactCutoffSlowHz` to 22 kHz with `speed01^0.8` (× `tileBrightness` 1.6 on bare
tile); pitch moves only `contactSpeedPitch` (5–8 %). `speed01` is stroke speed over
`contactRefSpeedMm`, eased at `speedEaseHz` (9 /s). Right mouse button = pressure 0.5 (~0.8 of the gain).

Shipped voicing (`ToolSoundBank.ResetToDefaults`; push it into the .asset with
`CarpetCleaning/Audio/Reset Voicing (Keep Clips)`, which keeps every assigned clip):

| Tool | machine | vol | spin up/down | pitch min | idle cut | surface | vol | ref speed | slow cut |
|---|---|---|---|---|---|---|---|---|---|
| Scraper | — | | | | | Crunch | 1.00 | 450 mm/s | 3000 Hz |
| Dry vacuum | VacuumMotor | 0.42 | 3.5 / 1.8 | 0.72 | 900 Hz | Friction | 0.34 | 600 | 1800 |
| Hose | WaterJet | 0.92 | 11 / 9 | 0.97 | 6000 | — | | | |
| Foam sprayer | FoamHiss | 1.00 | 13 / 10 | 0.98 | open | — | | | |
| Brush | RotaryMotor | 0.50 | 4 / 1.2 | 0.80 | 700 | Scrub | 1.00 | 550 | 2200 |
| Squeegee | — | | | | | Squeegee | 0.60 | 700 | 1600 |
| Wet vacuum | WetVacuumMotor | 0.44 | 3.5 / 1.6 | 0.72 | 800 | Slurp | 0.66 | 500 | 1500 |
| Dryer | Blower | 0.44 | 3.5 / 1.5 | 0.70 | 1000 | — | | | |
| Detergent | SprayHiss | 0.84 | 13 / 10 | 0.98 | open | — | | | |
| Mop | — | | | | | Slop | 1.00 | 650 | 1400 |

The recipes name the placeholder each slot falls back to. The volumes are set for the recordings the bank ships with
(11.1): each recording was levelled to the loudness of the placeholder it replaced, and where its crest factor kept it
quieter the volume makes up the difference, so the mix stays the one the placeholders were balanced to (the
2026-09-21 numbers were 0.70 / 0.30 / 0.50 / 0.46 / 0.40 + 0.62 / 0.58 / 0.40 / 0.40 / 0.44 / 0.60).

Six machines are recordings (dry and wet vacuum, dryer, hose, foam sprayer, detergent): their machine columns are the
fallback for `useRecording` off. There `motorVolume` still sets the level and `motorSpeedPitch` the load pitch; the spin
rates, pitch min and idle cutoff are unused, because the recording carries its own spin-up and coast-down (section 9).
The brush's machine is a recorded loop on the procedural spin, so all its columns apply.

The brush is a rotary floor machine (`ROTARY_BRUSH.md`, since 2026-09-24):
- `RotaryMotor` = `MotorLoop(340, 100, 520, 0.5, 1.6)`: a heavy motor's 100 Hz hum, a dull cooling-air roar and a
  faint gearbox whine. Measured: tonal 6.7 dB @ 334 Hz, am 5.8 dB, st90 11.8, −13.2 dBFS RMS, seam 0.52.
- Its spin rates 4 / 1.2 match `ToolModelSet`, so the hum and the bristles wind up and coast down together.
- The power-on thud sits at 55 Hz (95 Hz for the other motors).
- Its surface idle is 0.60, not 0.03: the pad scrubs held still.

Master gain is `bank.masterVolume` (0.85) × `ToolAudio.volume` (1). **M** toggles mute.

A tool change cuts the running machine (its own `powerOff` plays, not the new tool's; a recorded machine
plays its own coast-down instead, 9.3), zeroes the spin and crossfades both layers. Each layer owns two `AudioSource`s (each with its own filter) for
exactly that reason: assigning a clip to a playing source clicks, so the old one fades out while the
new one comes up.

## 3. The placeholders

### 3.1 Why the first version sounded like a spaceship

User verdict on v1: "alien, cosmic". Measured with `Tools/analyze_sfx.py` (section 6), it was:

| Defect in v1 | Measurement | Where |
|---|---|---|
| motor = a locked 7-harmonic sine stack | 34–36 dB tonal peak, 8 peaks > 12 dB | both vacuums |
| periodic LFOs (6.3 Hz vibrato, 2.3 Hz "breathing") | 17–24 dB periodic AM | vacuums, dryer |
| every transient a sine blip | per-frame tonality st90 17–27 dB (noise = 12.4) | scraper, hose, mop, wet vac |
| a gated sine squeak | 12.6 dB peak at 1448 Hz | squeegee |
| pitch carrying speed and spin | ±22 % / 0.45× sweeps at runtime | driver |

### 3.2 The rules now

Everything is noise-first (the `ProceduralSfx` class comment has the full reasoning):

- **Transients are filtered noise bursts** (`AddBursts`): 0.3 ms attack, exponential decay, own low-Q
  band per burst, amplitude `amp · u^tail`. Tail 3–4 gives the many-faint-few-loud spread of real
  crackle; bristles, grit, fizz, spatter and rubber chatter are all this one primitive.
- **Machine tones are narrow-band noise**, not oscillators: the impeller whine is a Q-16 band on noise
  at the blade-pass frequency (1180 Hz dry, 820 Hz wet) plus a 2nd harmonic, drifting on its own.
  The Chamberlin band output has **gain Q at its centre**, so bands are divided by Q — the first rewrite
  missed this and its Q-38 whine at 38× still drowned the roar. `WhineGain` 14 was set by measurement:
  the whine sits 8–10 dB above the roar floor, audible as a tone inside the noise, not a siren.
- **Movement is random** (`Drift`: noise through two one-poles), never a periodic LFO.
- **The only tonal element is the bubble** (`AddBubbles`): Minnaert resonance f0 = 3/r, van den Doel
  damping d = 0.13 f0 + 0.0072 f0^1.5 and rising chirp f0 (1 + 0.1 d t) — a few ms each, sparse. That
  is what makes the hose, mop, squeegee and wet vac read as water rather than as bleeps.
- Air and water beds are **pink** (Kellet filter), not white.
- The **SVF is clamped for stability** (Jury: f < −q + √(q² + 4)); a Q-0.35 band is unstable above
  ~4.5 kHz, which the hiss recipe would have hit. The hiss now uses shelves only.

After (the shipped clips): every surface/air/water loop at st90 11.8–13.1 and tonal 2.6–3.4 dB — in
tonality indistinguishable from the white-noise reference — motors at st90 15.6–15.7. Envelope
autocorrelation confirms no periodic wobble (max 0.13–0.26 at the minimum lag; the v1 dryer had 0.64
at 0.41 s). The wet vac's 19 dB `am` is its random water slugs, by design.

### 3.3 Loops and cost

- **Seamless wrap.** Each loop renders `n + fade` samples (fade = 60 ms) and folds the tail back over
  the head with an equal-power crossfade, so filtered noise wraps without a tick. Bursts and bubbles
  that run off the end wrap to the head.
- **Cost.** Everything except `ProceduralSfx.ToClip` is pure maths with no engine calls, so it runs off
  the main thread: when a bank slot is empty `ToolAudio` renders it on a background thread (current
  tool first) and builds the `AudioClip` on the main thread, `clipsPerFrame` (2) per frame. The baker
  renders all 25 clips with `Parallel.For` in ~350 ms. With the bank baked, as it ships, nothing is
  generated at runtime.

## 4. Baking and previewing

| Menu `CarpetCleaning/Audio/…` | Does |
|---|---|
| Bake Placeholder Sounds (WAV) | writes the 25 clips to `Audio/Generated/` (mono 16-bit PCM, imported PCM / DecompressOnLoad / preloaded) and assigns them into the bank |
| Clear Baked Sounds | empties the slots (back to runtime generation; files stay) |
| Report Placeholder Levels | length / peak / RMS / loop seam of whatever is assigned |
| Reset Voicing (Keep Clips) | pushes the C# default numbers into the .asset without touching clips |

For one tool only (from `eval`):
- `ToolSoundBaker.BakeTool(ToolId)` writes and assigns just that tool's clips. The full bake re-assigns every slot,
  recordings included.
- `ToolSoundBank.ResetVoicingKeepClips(ToolId)` resets just that entry's numbers.

`ToolSoundBaker.WritePreview(dir)` writes all clips to any folder outside `Assets` (no import) — the
fast loop for tuning the synthesis: edit `ProceduralSfx`, recompile, preview, run the analyser. The
whole bake now fits inside the CLI's 5 s main-thread limit.

## 5. Replacing a placeholder with a real recording

This was the intended end state, and since 2026-09-25 every tool is there (section 11). The steps below are for the
next recording, e.g. your own of a real machine.

1. Record or download a clip. **Do not take audio from YouTube videos** — that is someone else's
   copyrighted material and it would follow the project into any release. Phone recordings of a real
   vacuum, spray bottle, brush and squeegee are both legal and better; CC0 / CC BY on Freesound and the Mixkit
   licence are the fallback. Read the licence on the sound's own page: CC BY needs a credit
   (`AUDIO_CREDITS.md`), and CC BY-NC rules out a paid release (the Samsung vacuum is NC, 9.6).

A whole take of a machine (switch-on, a stretch of running, switch-off and the coast-down) beats a loop:
cut it with `Tools/cut_machine_recording.py` and import it with `ToolSoundBaker.ImportMachineRecording`,
and it replaces the tool's entire machine layer (section 9). A sprayer works the same way with `--speed envelope`
(11.3). The steps below are for single clips.

2. Put the source in `Assets/CarpetCleaning/Audio/Sources/` (Freesound names: `<id>__<author>__<title>`).
3. Cut it with `Tools/prepare_recording.py loop|oneshot ... --slot contact|motor|on|off --match <placeholder> --credit
   "..."` (11.3). It writes a seamless loop (or a faded one-shot) levelled to the placeholder, and a `.json` next to
   it. A raw cut ticks once per loop.
4. `ToolSoundBaker.ImportClip(ToolId, "<Recorded>/<name>.json")` imports it (PCM, stereo kept), puts it into the slot
   and writes the credit into the entry's `clipCredits`. An assigned clip always wins over its recipe.
5. Add the cut to `Tools/audio_recordings.py` and the tool's line to `ToolSoundBaker.ImportShippedRecordings`, so it can
   be rebuilt and a fresh bank gets it.
6. Re-tune that entry's `motorVolume` / `contactVolume` if the recording ends up quieter than the placeholder (the
   cutter prints its loudness against the reference), and run `Tools/analyze_sfx.py` on it: a loop with a periodic
   wobble or a whistle shows up there before it annoys anyone. `Report()` prints the RMS and the loop seam of whatever
   is assigned.

`Bake` and `Clear` leave recorded clips alone (anything outside `Audio/Generated`); they only (re)assign placeholders.
Re-running `CarpetCleaning/Build Scene` does **not** overwrite the bank: defaults are written only
when the asset has to be created, and then the shipped recordings are imported with them.

## 6. Testing

`CarpetDebugApi.AudioJson()` returns the live mixer state (volume, pitch and filter cutoff per layer),
and `StepAll` advances the audio with the simulations, so a scripted stroke is measurable:

```
unity command eval --code "CarpetCleaning.CarpetDebugApi.Reset(1); CarpetCleaning.CarpetDebugApi.SetTool(1);
  CarpetCleaning.CarpetDebugApi.StrokeNow(1, 0.2f,0.3f, 0.8f,0.3f, 60, 1f);
  return CarpetCleaning.CarpetDebugApi.AudioJson();"
```

Measured 2026-09-21 (v3 voicing, baked bank):

| Moment | spin | motor pitch | motor cut | surface vol | surface cut |
|---|---|---|---|---|---|
| vacuum, 10 stamps in | 0.44 | 0.857 | 3.7 kHz | 0.174 | open |
| vacuum, 90 stamps in | 0.99 | 1.017 | 21.6 kHz | 0.266 | open |
| vacuum, 40 frames after release | 0.37 | 0.824 | 2.9 kHz | 0 | — |
| brush, slow (90 mm/s) | — | — | — | 0.114 | 6.0 kHz* |
| brush, fast (~550 mm/s) | — | — | — | 0.593 | open |

\* scripted strokes never update `PointerOnCarpet`, so they always get the tile variant (×1.6 cutoff).
All four loop sources carry their low-pass; no console errors.

**Clip analysis:** `python Tools/analyze_sfx.py Assets/CarpetCleaning/Audio/Generated` (numpy + scipy).
Columns: `tonal_dB` strongest narrow peak over its 1/3-octave floor, `peaks12` count of peaks > 12 dB,
`flat` spectral flatness, `am_dB` amplitude modulation 0.5–20 Hz, `st90` 90th-percentile tonality of
23 ms frames (catches sine grains the long-term spectrum hides). Reference: white noise 2.7 / 4.9 /
12.4, a tone in noise 52 / – / 46. Run it on your own recordings too — a good surface loop lands near
the noise reference.

**What a scripted test cannot check:** the editor does not advance Play-mode frames while it is
unfocused (autotick and `editor_focus` do not help — same limitation as the rest of the project), and
with no frames the audio engine is not pumped either, so `AudioListener.GetOutputData` reads zero.
Whether the mix is pleasant needs a focused editor and ears.

## 7. Open

- No HUD volume control: mute is the **M** key, level is `ToolAudio.volume` / `bank.masterVolume` in
  the inspector. A slider belongs in `CarpetHud.uxml` when the HUD is next touched.
- No ambience, no UI sounds beyond the tool-change tick, nothing on the hand-over animation.
- The rattle covers the visible crumbs (debris chunks). The fine grit the dry vacuum lifts out of the pile
  cell by cell (`CarpetSim.compute` K_Stamp) is silent: K_Stamp already binds 8 UAVs, the hard limit, so it
  cannot count what it removes without restructuring the kernel. A soft sandy hiss driven by that would be
  the next step.
- `ToolAudio` survives a script reload in Play (rebuilds its sources in OnEnable; verified: recompile mid-Play,
  0 errors), but the simulations still lose their RTs then — restart Play after any recompile, as CLAUDE.md says.
- The full bake (41 clips + import) now takes longer than the CLI's 5 s main-thread limit: the call
  reports a timeout, the editor finishes anyway (check `Audio/Generated/`).
- `PointerOnCarpet` is only updated by the mouse path, so scripted strokes always use the tile
  variant (×1.04 pitch, ×1.6 cutoff). Harmless in gameplay, but scripted readings are brighter.
- Every tool plays real recordings since 2026-09-25 (section 11); its open points are in 11.7. The placeholders
  stay as the fallback of every slot. The crumb rattle stays synthetic on purpose (the user asked to keep it as it is).
- The dry vacuum's CC BY-NC Samsung take was replaced by the CC BY Dyson on 2026-09-26 (11.4).

## 8. Debris in the tube — the crumb rattle

A real vacuum ticks and rattles when it swallows grit: every crumb strikes the wand wall a few times on
its way to the bag. Here that sound plays **only for crumbs the nozzle actually picks up, at the moment
each one disappears into it** — it is driven by the particle simulation, not by a timer or by the
stroke.

### 8.1 Where the truth lives

Visible crumbs are the debris chunks of `Debris.compute`. When a dry or wet vacuum stamp covers a grounded
chunk (footprint weight > 0.15) the chunk switches to `DEB_ST_SUCKED`, flies to a point 30 mm above the
stamp centre and dies there. That transition is the only "captured" event there is, so the event is
written right there, by the GPU (a mop also captures crumbs, but into its cloth: no tube, no event).

The chunk's flight is deterministic from that instant: it does not move on the capture frame, then each
frame the remaining distance is multiplied by exp(-k·dt) (k = `D_SUCK_SPEED` 12 /s) and its size by
exp(-4·dt), and it is killed, after the update, when the distance drops under 8 mm, the size under
0.3 mm or 0.4 s have passed. Each of the three first holds after m = floor(t/dt) + 1 frames (life:
ceil(0.4/dt)), so **the frame it vanishes on is computed at capture**, exactly:

```
tDist = ln(max(d0, 8) / 8) / k        tSize = ln(max(size, 0.3) / 0.3) / 4
tArrive = min(floor(tDist/dt)+1, floor(tSize/dt)+1, ceil(0.4/dt)) · dt
```

(The first cut used the continuous t and measured 0–16.6 ms early, mean 9.8 — always less than a frame.)

### 8.2 The pipeline

1. **GPU** (`Debris.compute`): at the SUCKED transition, `InterlockedAdd` a slot in `_SuckEvents` and
   write `{particle index, size mm, tArrive, tool}`. `[0]` = events this step (cleared by the new
   1-thread kernel `K_DebClearEvents` before every `K_DebSim`), `[1]` = total since reset (never cleared
   on the GPU), up to 64 records per step. K_DebSim now binds 3 UAVs.
2. **CPU** (`DebrisSystem.RequestSuckEvents`): after every step an `AsyncGPUReadback` of that 1 KB buffer.
   GPU commands run in order, so the copy sees exactly this step's writes and the next clear cannot race
   it. `AudioSettings.dspTime` is captured **at dispatch**, because the arrival counts from the capture,
   not from when the data comes back. The callback raises `DebrisSystem.DebrisSucked` per record
   (arrival converted to real seconds with `TimeScale`); a reset bumps a generation counter so stale
   readbacks are ignored.
3. **Audio** (`ToolAudio.OnDebrisSucked`): `PlayScheduled(captureDsp + tArrive + tube delay)` on a pool of
   14 voices. The tube delay (8–22 ms, random) is the crumb's flight from the nozzle mouth to the first
   bend of the wand. The readback arrives 1–3 frames (17–50 ms) after capture while the crumb needs
   133–167 ms to reach the nozzle, so the strike is always scheduled in the future; only a readback slower
   than the flight itself would play at once (`rattleLate`, 0 in every test).

Voices: at most `rattleMaxPerFrame` (8) new crumbs per frame; if all voices are busy (the nozzle ran into
a pile) the oldest voice is stolen, but only once it is 60 ms into its clip — `PlayScheduled` on a playing
source stops it at once, and a voice still waiting for its own future strike must keep it. Otherwise the
crumb is dropped (`rattleDropped`).

### 8.3 The sound

`ProceduralSfx.TubeRattle` — one clip per crumb, 0.2 s: 2–3 strikes for a small crumb, 3–5 for a large
one (≥ 7 mm, `largeCrumbMm`), **the first at 1.5 ms**, each ricochet later (6–25 ms apart, spreading),
quieter (×0.45–0.65) and duller (low-pass 6 → 2.2 kHz) as the crumb travels into the hose. A strike is a
0.15–0.4 ms noise burst ringing three wall modes (1 : 1.47 : 2.09, Q 12, ~1–2 ms: a tick, never a bleep)
over the hollow body of the air column (650–900 Hz). The upper modes are attenuated because a white burst
feeds each mode in proportion to its bandwidth — without that the first cut sat at a 7–12 kHz centroid
("tss"); that fix brought it to 3.8–6.4 kHz, then the user asked three times for a higher rattle: every
resonance is now ~4.2× the first voicing (wall modes 9.3–12.3 kHz small / 6.3–8.8 kHz large, body 2.8–3.9 kHz at
half its old gain so the wall ring leads), strike timing unchanged. Modes above 15–17 kHz fade out, and two RBJ
Butterworth sections at 14.5 kHz band-limit the clip (energy above 18 kHz was 35 %, now 0.04 %; that junk had
also inflated the centroid readings of the earlier rounds). Measured: the dominant resonance of the first strike
is 6.2–13.6 kHz in 15 of 16 clips (one large variant keeps a 3.5 kHz body).

**Quick tuning:** `Settings/ToolSoundBank.asset`, top of the inspector, "Vacuum crumb rattle - quick tuning":
`rattlePitch` and `rattleVolume` for both vacuums, applied live. Per vacuum there is also `debrisPitch` /
`debrisVolume` inside its entry (Entries → Dry/Wet vacuum → "Debris in the tube"). `debrisPitch` in the bank entry (default 1)
scales playback pitch for tuning by ear without a rebake. Wet vacuum: modes ×0.85, damped, plus a water film of
bubbles held a fixed 24 dB under the strikes. At playback: pitch 1.12 (3 mm) → 0.88 (12 mm) ±4 %,
volume `debrisVolume` × 0.45–1 by size. 8 variations per vacuum, baked to
`Audio/Generated/tool_{dryvacuum,wetvacuum}_debris_{0..7}.wav` (0–3 small, 4–7 large).

**Own recordings:** drop them into the entry's `debrisClips` (small crumbs in the first half). The first
strike must be at the very start of the file — leading silence shifts the sync by exactly its length.
`python Tools/analyze_sfx.py <folder> --hits` prints each clip's onset, strikes and centroid and flags an
onset over 3 ms.

### 8.4 Verified (2026-09-21)

`CarpetDebugApi.SuckTimingBegin / SuckTimingRun / SuckTimingReport` is the permanent regression test: it
scrapes crumbs loose, vacuums 5 rows over them one frame at a time, reads the particle pool back after
every step and compares each captured chunk's predicted arrival with the step it actually vanished on.

| Check | Result |
|---|---|
| captures on the GPU / delivered to the CPU / overflow / duplicates | 42 / 42 / 0 / 0 |
| predicted vs actual vanish (all 42 crumbs) | **0.00 ms** error, min = max = mean |
| arrival after capture | 133–167 ms (8–10 frames) |
| rattles scheduled late | 0 |
| rattle clips: onset / strikes / dominant resonance | 1.5 ms / 2–5 / 6.2–13.6 kHz (15 of 16) |

What it cannot show: the scripted run delivers every event inside one Unity frame with the audio clock
standing still, so it measures the prediction and the bookkeeping, not live readback latency (the 4
drops in that run are that artefact). In a focused Play session `AudioJson()`'s `rattleLate` and
`lastLeadMs` are the live check: lead should stay positive. After the third pitch raise and the voice fix: 42/42,
0.00 ms, 40 scheduled, 0 late.

## 9. The recorded vacuum: a real machine instead of a synthetic one (2026-09-24)

Request: the user dropped `Audio/100210__debsound__vacuum-cleaner-samsung-easy-1300.wav` into the project and asked
for it to be the dry vacuum (hotkey 2), with the high crumb rattle (section 8) left exactly as it was. The rattle
and the surface layer (nozzle friction) are untouched; the machine layer is now the recording.

### 9.1 The take

| | |
|---|---|
| Source | Freesound 100210, "Vacuum Cleaner-Samsung Easy 1300.wav" by Debsound, Marantz PMD661 internal mic |
| Licence | **CC BY-NC 4.0**: credit "Debsound", non-commercial use only (9.6) |
| Format | 96 kHz / 24-bit stereo, 29.56 s; peak 0 dBFS from one sample at 18.88 s, outside every cut |
| Content | switch-on already under way at 0 s (motor at 28 % speed), 99 % by 1.5 s, running to 20.40 s with one natural load change (13.4-16.5 s: +2 dB, whine -0.7 %) and one low thump (17.9 s), switch-off at 20.40 s, coast-down to silence at 29.5 s |
| Motor line | the impeller's blade-pass whine, 5040 Hz = 9 blades x 560 Hz rotation (about 33,600 rpm), 28-35 dB over the noise |

The whine follows the rotation exactly, so it is the speed gauge: 5040 Hz = speed 1.

### 9.2 The cut (`Tools/cut_machine_recording.py`)

```
python Tools/cut_machine_recording.py <source.wav> --scan        # level + motor line every 0.1 s, to pick the points
python Tools/cut_machine_recording.py Assets/CarpetCleaning/Audio/100210__debsound__vacuum-cleaner-samsung-easy-1300.wav
    --out Assets/CarpetCleaning/Audio/Recorded --name dryvacuum_samsung --start 0 --loop 6.2 17.6 --stop 20.36
    --match Assets/CarpetCleaning/Audio/Generated/tool_dryvacuum_motor.wav
    --credit "Vacuum Cleaner-Samsung Easy 1300.wav by Debsound, https://freesound.org/s/100210/, CC BY-NC 4.0"
unity command eval --code "return CarpetCleaning.ToolSoundBaker.ImportMachineRecording(CarpetCleaning.ToolId.DryVacuum,
    \"Assets/CarpetCleaning/Audio/Recorded/dryvacuum_samsung.json\");"
```

| Clip | Source span | Length | What |
|---|---|---|---|
| `dryvacuum_samsung_start` | 0 - 6.200 s | 6.20 s | the spin-up plus 4 s of running; 5 ms fade-in, no fade-out |
| `dryvacuum_samsung_loop` | 6.200 - 17.608 s | 11.41 s | running, with the natural load change; seamless |
| `dryvacuum_samsung_stop` | 20.36 s - end | 9.20 s | 40 ms of running, the switch-off and the coast-down; 0.3 s fade-out |

- **The points.** The whine is at 5040 Hz both at 6.2 s and at 17.6 s, so the wrap does not jump in pitch. The
  thump at 17.9 s stays outside. 20.36 s is 40 ms before the speed starts to fall.
- **The wrap.** The loop's tail is crossfaded (80 ms) into the audio just BEFORE 6.2 s. So the wrap continues
  into loop[0] = source[6.2 s] exactly as the start clip does, and the head stays identical to the source.
  - The end point moved +7.8 ms to the best match (correlation 0.61), and the crossfade gain is corrected for
    that correlation, so the level does not dip.
  - Measured: the wrap step is 0.41 x RMS, against 0.61 for neighbouring source samples.
  - Energy above 6 kHz in 1 ms frames: +1.8 dB over the local median at the wrap, while the loudest frame
    within 200 ms either side is +3.3 dB. So there is no click. The start-to-loop stitch: +1.6 against +3.1 dB.
- **Level.** One gain for all three clips (-1.82 dB), so the loop's K-weighted loudness equals the placeholder
  motor's (-11.6 LUFS) and `motorVolume` 0.42 keeps its meaning.
  - RMS -17.2 / -16.5 / -28.7 dBFS, peaks -2.7 / -2.9 / -4.5 dBFS.
  - 48 kHz, 16-bit dithered, stereo kept (a real recording's width).
  - Imported PCM, DecompressOnLoad, preloaded: a codec would smear the whine and could move the stitch.
- **Speed curves** (26 + 31 keys after Douglas-Peucker at 0.004 speed):
  - Tracked on the whine by dynamic programming over a spectrogram with 21 / 43 / 85 ms windows, walked away
    from the running motor. A frame-by-frame search lost the line where it doubles in 0.15 s at the start.
  - Spin-up: 0.28 at 0 s, 0.56 at 0.15 s, 0.72 at 0.24 s, 0.85 at 0.44 s, 0.93 at 0.66 s, 0.98 at 1.17 s,
    0.99 at 1.5 s.
  - Coast-down: 0.997 to 0.04 s, 0.89 at 0.21 s, 0.71 at 0.6 s, 0.58 at 1.0 s, 0.44 at 1.66 s, 0.32 at 2.6 s,
    0.21 at 3.9 s. The line is lost in the noise at ~4.4 s (speed 0.18); after that an exponential tail runs
    to 0.04 at 9.2 s.
- `analyze_sfx.py` on the loop: tonal 31.4 dB at 5045 Hz, st90 31, so it is flagged "tonal / bleepy". Here that
  is the real whine, not a synthesis fault. am 4.8 dB, so no wobble.

### 9.3 Playback: `RecordedMachine`

`ToolSoundEntry.recording` (a `MachineRecording`: start / loop / stop clips, `startSpeed` / `stopSpeed` curves,
`crossfade` 0.05 s, `credit`) replaces the machine layer while `useRecording` is on and all of it is present
(`UsesRecording`). `ToolAudio` then leaves its procedural motor layer empty and skips the synthetic `powerOn` /
`powerOff` one-shots, and its `MachineSpin` is the speed read off the curves. The surface layer and the rattle
run exactly as before.

| Moment | What plays |
|---|---|
| press, motor at rest | start clip from 0, `PlayScheduled` 42 ms ahead; the loop `PlayScheduled` for the exact end of the start clip (6.2 s later) |
| hold | the recording's own spin-up, then the loop: a sample-exact stitch of two contiguous cuts, no crossfade, so the whine cannot beat |
| release | the stop clip entered where its speed equals the motor's at that moment; the start clip or loop fades out over 50 ms (equal power) |
| release during the spin-up | the scheduled loop is cancelled while it is still ahead of the mixer, and the coast starts from the half-spun speed |
| press while it coasts | the start clip entered where its speed equals the coasting motor's (0.558 at 0.15 s, for instance); back at full speed (a re-press within ~40 ms of a release) it goes straight to the loop |
| tool change | the machine gets "off" and coasts down in full (one `RecordedMachine` per recorded tool, ticked every frame), while the new tool plays |
| `useRecording` off | fades out in 50 ms with no coast, and the procedural machine takes over; use it to compare by ear, it works in Play |

- **Clock.** Everything runs on `AudioSettings.dspTime`, because a scheduled start, the stitch and a crossfade line
  up only on the clock the clips play on.
- **Lead.** 1.5 DSP blocks + 10 ms (42 ms at 1024 samples / 48 kHz). A start scheduled inside the block being
  mixed would slip to the next block, and the loop behind it would then overlap. `SetScheduledEndTime` on the
  start clip is a second guard.
- **Load pitch.** `motorSpeedPitch` x stroke speed (2 %) acts on the loop only, eased in at 3 /s from 1, so the
  stitch never jumps in pitch. `motorPitch` shifts all three clips.
- **Voices.** Five `AudioSource`s per recorded machine, on `ToolAudioSources`. Enough for a release and a
  re-press inside one crossfade; if they run out, the quietest fading voice is taken.
- **Test hook.** `RecordedMachine.ClockOverride` swaps the clock for a virtual one in
  `CarpetDebugApi.RecordedMachineScenario`, and is null otherwise (`ToolAudio.Awake` clears it).

### 9.4 Verified (2026-09-24)

- **Compile.** `recompile`: 0 errors. Switching tools in Play (scraper, hose, dry vacuum): no warnings.
  - That run first surfaced "Attempting to set `time` on an audio source ... not a clip". `LoopLayer.SetClip(null)`
    set `time` on an empty source, which would now have happened on every switch to the vacuum. Fixed.
- **Import.** Three clips: PCM, DecompressOnLoad, preloaded, stereo, 48 kHz. The bank entry has `useRecording` 1
  and linear curves. `Report()`: start 6.20 s -17.2 dBFS; loop 11.41 s -16.5 dBFS, seam 0.41; stop 9.20 s
  -28.7 dBFS (the placeholder motor: -15.9 dBFS, seam 0.13).
- **Transitions on a virtual clock**, `CarpetDebugApi.RecordedMachineScenario(1, 1, 7.5)`:

| t | event | phase | speed | voices |
|---|---|---|---|---|
| 0.00 | press | Starting | 0.284 | start @0 in +42 ms; loop in +6242 ms |
| 1.00 | release | Stopping | 0.970 | stop @0.09 s in +42 ms (fading in); start fading; loop cancelled |
| 2.00 | press again | Starting | 0.558 | start @0.15 s (its speed there: 0.558); loop in +6092 ms = 6.2 - 0.15 + 0.042 |
| 8.57 | (stitch passed) | Running | 1.000 | loop only |
| 9.50 | release | Stopping | 0.997 | stop @0; loop fading |
| 18.80 | coast ends | Off | 0 | none |

- **ToolAudio integration with the real clock**, which stood still. It shows the decisions, not the sound.
  - Stroke: `recorded` true, motor layer empty, start + loop scheduled at 0.357 = 0.85 x 0.42.
  - Release: stop voice at `timeSamples` 141601 (= 2.95 s x 48 kHz, where the stop curve is at the start's 0.284).
  - Switch to the hose mid-run: the vacuum machine goes to Stopping while the hose's own motor plays.
  - `useRecording` false: the procedural `tool_dryvacuum_motor` takes over; true again: the recording is back.
- **The real engine**, during minutes the editor had focus:
  - The stop voice's `timeSamples` read 122257, against the predicted position 2.547 s x 48000 = 122256.
  - So `PlayScheduled` honours a preset `timeSamples` offset, and the dsp-derived position is where the clip
    really plays.
- **Rattle untouched.** `SuckTimingBegin/Run/Report`: 42/42 captured and delivered, arrival 133-167 ms.
  - 1-2 crumbs now vanish up to 50 ms earlier than predicted (0.00 ms on 09-21). This is on the particle side,
    since the audio cannot touch it; suspects are the 09-24 sim changes.
  - A first probe run straight after the integration test captured 0; the rerun gave 42/42. A direct dry-vacuum
    stroke captured 8/8.
- **Not verified: listening.** With the editor unfocused no frame runs and no audio plays (6). The stitch at
  6.2 s, a quick tap (coast from a half-spun motor) and a re-press while it coasts all need a focused editor and ears.

### 9.5 Another recording

1. A good take: switch-on from rest, 10-20 s of steady running, switch-off, the full coast-down, and no handling
   noise. Stereo is fine. A machine with a clear tonal line (a vacuum's whine, a motor's hum) gives the best speed
   curves.
2. `--scan` it, then pick the points:
   - the loop starts and ends where the line has the same frequency;
   - thumps stay outside the loop;
   - the stop starts ~40 ms before the line starts to fall.
3. Cut with `--match` against the placeholder the take replaces, so its level fits the mix.
4. `ToolSoundBaker.ImportMachineRecording(ToolId, "<dir>/<name>.json")`. It imports the clips, fills the entry
   and turns `useRecording` on. Placeholders and bake/clear never touch the recording.
5. Listen in Play, then tune `motorVolume`. `RecordedMachineScenario` checks the transitions (it is written for
   the dry vacuum, so change the ToolId for another tool).

### 9.6 Licence (read before shipping)

- The take is **CC BY-NC 4.0**. Using it needs the credit (Debsound, title, freesound.org/s/100210, the licence),
  and it may only be used non-commercially. The credit is stored in the bank entry (`recording.credit`).
- **A paid Steam release must not ship it.** Options:
  - your own recording of a vacuum, which is legal and the best;
  - a CC0 recording;
  - written permission from Debsound.
- `SteamBuild` writes a warning to the console and into its result line while a recording whose credit names an
  NC licence is in the bank (STEAM.md 10).
- A free, non-commercial release may use it, as long as the game's credits carry: "Vacuum cleaner sound:
  'Vacuum Cleaner-Samsung Easy 1300.wav' by Debsound (https://freesound.org/s/100210/), licensed under CC BY-NC 4.0".

## 10. UI sounds: the payout card and the upgrade shop (2026-09-24)

**Files**

| File | Job |
|---|---|
| `Scripts/Audio/UiAudio.cs` | on `CarpetUI`, order 125: a pool of 12 2D voices, so ticks can climb in pitch and coin clinks overlap |
| `Scripts/Audio/UiSfxSynth.cs` | the 11 recipes, pure maths with its own SVF / modal / pink helpers, independent of `ProceduralSfx` |
| `Scripts/Audio/UiSoundBank.cs` + `Settings/UiSoundBank.asset` | clip slot and gain per `UiSfx`, plus `masterGain` 0.8 |
| `Editor/UiSoundBaker.cs` | menu `CarpetCleaning/Audio/Bake UI Sounds` |

- **Mix.** Silent while `ToolAudio.muted` (M); scaled by `ToolAudio.volume × ToolSoundBank.masterVolume`.
- **The bank asset** is created by the builder only when missing. The recipes and the bank are separate from
  `ToolSoundBank` and `ProceduralSfx`, which were being edited in parallel that day.
- **Baking.** `UiSoundBaker` writes `Audio/Generated/ui_*.wav` and fills only EMPTY bank slots or slots that already hold
  a generated clip. A recording in a slot is never overwritten. `CarpetSceneBuilder.Build` runs it.
- **Fallback.** A slot without a clip is rendered on a worker thread at startup.

**Sounds and triggers** (Docs/ECONOMY.md 4):

| Sound | Plays on | Recipe |
|---|---|---|
| tick | count-up steps, pitch 1 → 1.45 (total: → 1.9), rate-limited to 30 ms | 0.3 ms noise tick, 3.2 kHz band + faint ring |
| clink | a line's amount lands; coin landings (quiet, 35 ms gap) | struck coin: noise-excited disc modes 1 : 1.47 : 2.09 : 2.56 : 3.01, T60 0.32 – 0.11 s, bounce at 45 ms |
| whoosh | card in, line slide-in | pink noise, non-resonant band sweep 520 → 2300 Hz |
| thud | a missed bonus line | low-passed noise "tok" + fast-damped body modes |
| sparkle | perfect lines, combo | glitter: 44 noise grains 1.5 – 5.5 ms, 5 – 11 kHz, Q 2 (non-resonant) |
| stamp | COMBO lands | low-passed noise thump + damped 78 Hz body + slap + glitter |
| kaching | the total | two register clicks + noise-struck bell (inharmonic, T60 0.6 s) + coin |
| buy / deny | shop purchase / no money | rising whoosh + clink / two short thuds |
| open / close | shop | soft band sweep up / down |

**Measured** (`Tools/analyze_sfx.py`, one-shots; ignore am_dB on one-shots, a decay is AM by definition):

| Clip | tonal dB | st90 | Note |
|---|---|---|---|
| sparkle | 6.9 | 10.7 | near the white-noise reference st90 12.4 |
| stamp | 11.6 | 11.3 | |
| thud | 10.4 | 14.3 | |
| tick | 9.2 | 13.3 | |
| whoosh | 8.2 | 11.8 | |
| open | 13.6 | 11.4 | |
| close | 10.9 | 12.3 | |
| deny | 12.8 | 11.4 | |
| clink | 12.9 | 54 | struck metal is tonal by nature (the TubeRattle technique) |
| kaching | 7.4 | 47 | struck metal is tonal by nature |
| buy | 10.2 | 48 | struck metal is tonal by nature |

- The coin-based clips are the only ones where the analyzer's "bleepy" flag is expected.
- **First pass, for the record:** the sparkle was 11 high-Q modal pings, which measured tonal 22.5 dB with 9 peaks, the
  same "sine grain" failure as the old tool sounds. It was replaced by the noise glitter. The stamp's and thud's long
  low modes (19.9 / 16.1 dB hum) were damped.
- **Not heard yet by the user:** only their ears decide (AUDIO.md 6). A real recording goes into the `UiSoundBank` slot.

## 11. Real recordings for every tool (2026-09-25)

Request: find realistic sounds for every tool and put them in place of the generated ones. The sources are Freesound
(CC0 and CC BY only, each licence read on the sound's own page) and one Mixkit sound. No NonCommercial source was added,
and nothing comes from videos. Credits: `AUDIO_CREDITS.md`.

How they were chosen, without ears: search, then the licence on the page, then the HQ preview's level timeline,
tonal lines and a spectrogram of every serious candidate. Speech, birds, traffic, hum, handling bumps and clipping all
show up there, and so do a motor's spin-up and coast-down. About 300 candidates were looked at. Whether they sound
right is the user's call (11.7).

### 11.1 What plays now

| Tool (hotkey) | Layer | Recording | How it is cut | Level vs the placeholder | Volume |
|---|---|---|---|---|---|
| Scraper (1) | surface | akent, *BerberCarpetScratching* (fingernails through Berber pile), CC BY 3.0 | two takes 1.05-5.75 + 8.05-14.6 s joined, 11.1 s | -16.2 vs -12.6 LUFS | 0.70 → 1.00 |
| Dry vacuum (2) | machine | Samsung (unchanged, section 9), CC BY-NC | — | — | 0.42 |
| | surface | crcavol, *Dragging_On_Carpet*, CC0 | two drags 1.3-6.2 + 9.3-13.4 s, 8.8 s | -13.0 vs -12.0 | 0.30 → 0.34 |
| Hose (3) | machine | richwise, *Pressure Washer* (one trigger press on a patio), CC0 | press 113.0 s, loop 121.8-128.75, release 133.70-137.6 (+ drips) | -13.1 vs -7.8 | 0.50 → 0.92 |
| Foam sprayer (4) | machine | kev_durr, *Whipped Cream Spray Can* (Neumann shotgun), CC BY 4.0 | press 6.30, loop 6.9-10.95 (4.05 s), release 11.00-11.58 | -13.9 vs -6.5 | 0.46 → 1.00 |
| Brush (5) | motor loop | prometheus888, *Floor Buffer Sound*, CC BY 4.0 | exactly 30 pad turns (0.3197 s = 188 rpm), 9.59 s | -12.1 vs -10.1 | 0.40 → 0.50 |
| | power on / off | kyles, *switch big breaker metal click on, off*, CC0 | the two clacks, 0.38 / 0.54 s | -20.4 / -20.1 = placeholder | — |
| | surface | RossJuterbock, *Hair brush on carpet*, CC0 | three drags' loud cores, 10.3 s | -17.0 vs -8.3 | 0.62 → 1.00 |
| Squeegee (6) | surface | adda_records, *shovelling scraping water* (a wide blade pushing water), CC BY 4.0 | the loud middles of 27 strokes re-joined, 10.1 s | -12.1 vs -11.8 | 0.58 → 0.60 |
| Wet vacuum (7) | machine | joedeshon, *bissell_carpet_cleaner_vacuum_01* (a carpet extractor), CC BY 4.0 | switch-on at rest, loop 3.8-14.5, coast 15.18-23.0; motor line 439.7 Hz | -10.8 = placeholder | 0.44 |
| | surface | yehdawgo, *Slurping Water*, CC0 | 1.4-12.9 s, 11.4 s | -17.4 vs -13.1 | 0.40 → 0.66 |
| Dryer (8) | machine | busabx, *loud metal fan turned on and off* (MKH 416, treated room), CC BY 4.0 | from the last knob click 1.80, loop 5.2-16.3, stop 16.58-24.5 | -11.0 vs -10.2 | 0.40 → 0.44 |
| Detergent | machine | Mixkit 3215, *Spray water or liquid* (a pump-sprayer mist), Mixkit licence | press 0-0.2, loop 0.2-1.85 (1.66 s), release 1.86-2.5 | -12.0 vs -6.4 | 0.44 → 0.84 |
| Mop | surface | byronabadia, *26_Fregona_Agua* (a wet mop), CC BY 4.0 | three takes 1.55-4.15 + 5.25-7.05 + 8.6-10.75 s, 6.4 s | -16.5 vs -12.3 | 0.60 → 1.00 |

The "Level vs the placeholder" column shows the loop's loudness (K-weighted, LUFS) against the placeholder's. Every
clip was levelled to its placeholder, so the mix the placeholders were balanced to survives. Where a recording's crest
factor capped the gain first, the bank volume makes up the rest:
- crackle (scraping, scrubbing, slurping) has peaks 23-26 dB over its RMS;
- water spray has peaks 14-18 dB over its RMS.

Three stay quieter even at volume 1:
- the foam can, by 0.7 dB;
- the mop, by 0.2 dB;
- the brush scrub, by 4.6 dB. Its hairbrush crackle now sits level with the motor, where the synthetic scrub sat
  5.6 dB above it.

Still synthesised: the crumb rattle in both tubes (as asked, section 8) and the tool-switch tick.

Alternatives looked at and ranked second, in case a first choice does not sound right (all CC0 / CC BY):
- scraper: 464121, griptape dragged across carpet;
- brush scrub: 807200, a cat brush on a sofa; use its Mid channel only;
- dry-vacuum nozzle: 10405, a carpet scratch set;
- wet-vacuum nozzle: 707321 / 707322, a dental suction tube in mouthwash;
- hose: 353641, a hose on an indoor floor; it has no real release;
- foam: 147598, aerosol cream;
- detergent: 811446, a hose on mist; 720943, a spray bottle, about 3 squirts per second;
- squeegee: 791454, shower squeegee swipes;
- mop: 515158, a Swiffer on tile;
- wet-vacuum machine: 808837, a shop vac with on / run / off;
- dryer: 476832, a fan heater with clean spin-up and coast lines.

No CC0 or CC BY floor-machine take with a switch-on and a coast-down exists on Freesound (searched: floor buffer /
polisher / scrubber / burnisher / buffing). That is why the brush machine is a loop on the procedural spin and not a
`RecordedMachine`.

### 11.2 The manifest: `Tools/audio_recordings.py`

Every source (author, title, licence, page, download URL) and every cut (tool, slot, exact arguments) is in one file:

```
python Tools/audio_recordings.py list        # tool, slot, clip, licence, source
python Tools/audio_recordings.py credits     # the attribution block of AUDIO_CREDITS.md
python Tools/audio_recordings.py build [names] [--sources <dir>]
python Tools/audio_recordings.py fetch       # downloads the sources into Audio/Sources (fresh checkout)
```

`build` is deterministic (seeded grains and dither): the 15 clip sets rebuilt from `Audio/Sources` came out
bit-identical (md5) to the imported ones.

**Previews, not originals.** The sources are Freesound's HQ previews (OGG, about 192 kbps, 44.1 kHz), which download
without a login, and Mixkit's WAV. The originals (WAV / FLAC, often 96 kHz) need a Freesound account and have the same
timeline. For the release:
1. download them into a folder (Freesound names them `<id>__...`);
2. run `build --sources <folder>`;
3. then `ToolSoundBaker.ReimportRecordings()`: WAV names and GUIDs stay, the speed curves and credits are re-read.

### 11.3 The cutting tools

`Tools/prepare_recording.py` (new) makes every clip that is not a machine take:
- **`loop --span A B [--span ...]`**: joins steady stretches with equal-power crossfades and folds the tail over the
  head (seamless wrap).
  - The fold eats one crossfade. For a periodic machine make B - A = N periods + xfade: the brush loop is exactly 30
    pad turns, measured on the envelope's autocorrelation (0.3197 s).
- **`loop --grains --keep dB`**: for hand strokes. It keeps the loud middle of every stroke (the frames within `--keep`
  dB of the span's loud frames), shuffles grains of 0.35-0.9 s (never the same stroke twice in a row) and crossfades
  them into `--len` seconds.
  - The game fades the layer with the player's own strokes, so the recorded stroke rhythm is dropped and the texture
    is kept.
  - Crackly sources break into fragments at the default `--keep 8`. Use `--keep 14`-18, or plain `--span`, which
    every crackle loop here uses.
- **`oneshot --span A B`**: a trimmed one-shot with a 2 ms fade-in and a fade-out.
- **`scan`**: the level every 0.1 s, with the frames `--grains` would keep.
- **Level.** `--match <placeholder>` sets the K-weighted loudness equal to the placeholder's, and peaks cap the gain at
  -1 dBFS.
  - `--limit DB` lets a look-ahead limiter (5 ms) take up to DB off SPARSE peaks instead. The report says how much of
    the clip it touched.
  - `--release` is 0.08 s by default; use 0.02 for crackle, so each tamed tick does not duck the next.
  - Used here with 6 dB on the squeegee (4.4 % of the samples more than 1 dB down), the scraper (8.8 %), the brush
    scrub (2.5 %) and the slurp (2.2 %), and with 3 dB on the mop (6.2 %).
  - On dense material it pumps: the mop at 6 dB had 30 % of its samples more than 1 dB down.
- **Clean-up.** `--hp` is a high-pass (handling rumble; 60-120 Hz here), `--duck T0 T1 dB` turns a click or knock
  down, `--mono` folds to mono.
- Output: 48 kHz 16-bit WAV (TPDF dither), the source's channels, and `<name>.json` (slot, file, source, spans, gain,
  loudness, seam figures, credit).

`Tools/cut_machine_recording.py` gained:
- **Any input format.** It reads OGG / FLAC / MP3 via soundfile (`pip install soundfile`).
- **`--speed envelope`** for takes with no steady tonal line (sprays, the fan): the "speed" is the level, the smoothed
  RMS against the loop's, made monotonic. A release during the opening still enters the closing where it is as loud.
  `--env-smooth 0.25` keeps switch clicks out of that curve (the fan's knob).
- **`--from-rest`**: the spin-up curve ramps from 0 at `--start` up to its first confident frame. The wet vacuum's
  line sweeps 0 → 75 % in 0.3-0.5 s, too fast to track, and the curve used to hold 0.775 back to the switch-on.
- **`--line HZ`** tracks a chosen line instead of the strongest one.
- **`--hp`** and **`--duck`**: joedeshon boosted the extractor's switch clicks to 0 dBFS, which capped the gain of the
  whole take. They are 8 dB down now. The fan's rotary knob clicks 3 times on and 5 times off; the start begins at the
  last click, and the four inner off-clicks are 12 dB down.

### 11.4 The dry vacuum: a licence-safe machine is ready

`Recorded/dryvacuum_dyson_*` is InspectorJ's *Vacuum Cleaner, On Idle Off, Close, A* (a Dyson, 15 cm away,
CC BY 4.0), cut like the Samsung:
- start at 0.25 s from rest, loop 3.0-10.6 s, coast 10.90-19.5 s;
- motor line 671.5 Hz, coast tracked to 0.019 at 8.6 s;
- -12.0 LUFS against the placeholder's -11.6.

**In the bank since 2026-09-26** (for the trailer, `TRAILER.md` 7, and the paid release): imported with
`ToolSoundBaker.ImportMachineRecording(ToolId.DryVacuum, "Assets/CarpetCleaning/Audio/Recorded/dryvacuum_dyson.json")`
(start 2.75 s, loop 7.59 s, stop 8.60 s, motor line 671.5 Hz), and `ImportShippedRecordings` now lists it. The rattle and
the nozzle surface are separate and stayed. The Samsung take (the user's earlier pick) is a reference on disk only.

### 11.5 Code

- **`ToolSoundEntry.clipCredits`**: a "slot: credit" line per recorded clip slot.
  - `ResetVoicingKeepClips` now keeps it, and keeps `useRecording` too.
  - `SteamBuild`'s NC warning reads it along with the machine takes' credits.
- **`ToolSoundBaker`**:
  - `ImportClip(ToolId, json)` imports a clip (PCM, stereo kept, preloaded), fills the slot named in the JSON and
    writes its credit. `ImportClipsIn(tool, dir, prefix)` does a folder.
  - `ImportShippedRecordings()` (menu *CarpetCleaning/Audio/Import Shipped Recordings*) holds the tool-by-tool list of
    what the bank ships with. `CarpetSceneBuilder` runs it when it has to create the bank, so a fresh bank sounds like
    this one, and its default volumes are set for these recordings.
  - `ReimportRecordings()` re-reads everything assigned from the JSONs next to the clips.
  - `Credits()` prints every credit in the bank.
  - `Bake` / `Clear` no longer touch a slot that holds a recording (anything outside `Audio/Generated`).
- **`ToolAudio.LoopLayer`**: every (re)start of a loop longer than 1 s enters it at a random point. A recording has
  landmarks (one scrape louder than the rest), and starting every stroke on the same one is what gives a loop away. It
  is harmless for the placeholders.
- **`CarpetDebugApi.RecordedMachineScenario(ToolId, ...)`** runs for any recorded machine. The old signature is the dry
  vacuum.

### 11.6 Verified (2026-09-25)

- **Compile.** `recompile` twice, 0 errors. The only console errors are the CLI's 5 s timeouts of the long imports.
  The new static RNG first added an analyzer warning (UAL0010); it is an instance field now.
- **Import.** 6 machine takes and 9 clips (`Report()`). Loop seams, |last - first| / RMS:

  | Clip | Seam |
  |---|---|
  | scraper | 0.02 |
  | brush motor | 0.05 |
  | brush scrub | 0.05 |
  | squeegee | 0.05 |
  | mop | 0.07 |
  | detergent | 0.13 |
  | slurp | 0.14 |
  | foam | 0.16 |
  | hose | 0.17 |
  | dryer | 0.22 |
  | dry-vacuum nozzle | 0.44 |
  | wet-vacuum machine | 0.51 |

  The placeholders measured 0.08-1.95. For every clip from `prepare_recording.py`, the 1 ms high-band energy at the
  wrap stays below the loudest frame within 200 ms (scraper -5.1 against +18.8 dB, dry-vacuum nozzle +4.0 against
  +5.2).
- **`analyze_sfx.py`.**
  - The six surface loops measure tonal 2.8-5.8 dB and st90 11.0-14.2, like the noise reference (12.4): no whistles,
    no sine grains.
  - Tonal flags only on real lines: the motors (Bissell 439.7 / 3961 Hz, fan 164 Hz, floor buffer 316 Hz, Dyson
    674 Hz), the pressure washer's pump hum (281 Hz) and the detergent nozzle's 346 Hz resonance (its loop's st90 18.7).
- **Every tool in Play.** A scripted stroke (`StrokeNow`) with each of the 10 tools, then `AudioJson`: each one plays its
  recording (machine voices `*_start` + `*_loop` scheduled, surface clip as listed in 11.1).
- **Transitions on the virtual clock** (`RecordedMachineScenario`):

  | Machine | Tap | Release enters the stop clip at | Re-press enters the start clip at |
  |---|---|---|---|
  | wet vacuum | 1 s | 0.15 s (speed 0.942) | 0.33 s (speed 0.504) |
  | dryer | 1 s | 1.54 s (level 0.235) | — |
  | hose | 0.1 s | 0.39 s (0.237) | 0.07 s, 0.05 s later |
  | foam | 0.1 s | 0.09 s | — |
  | detergent | 0.1 s | 0.06 s | — |

  A full hold reaches Running and a release coasts out to Off in every case.
- **Rattle untouched.** `SuckTimingBegin / Run / Report`: 42 captured, 42 measured, prediction error 0.00 ms (min = max),
  arrival 133-167 ms, 42 scheduled, 0 late, 0 dropped. The 1-2 crumbs 50 ms early of 2026-09-24 are gone too.
- **Not verified: listening.** The editor was unfocused, so no audio ran (section 6).

### 11.7 Open

- **Listen to every tool.** Then tune `motorVolume` / `contactVolume` in the bank; each is one number and applies
  live in Play. Things to listen for:
  - the brush scrub, 4.6 dB under the old scrub;
  - the detergent's 1.66 s loop with its 346 Hz resonance, where repetition may show;
  - the foam's 4 s loop;
  - the hose, recorded on stone and harder than on carpet: a low-pass is one `--lp` away;
  - the wet-vacuum slurp, made by mouth, so it may read as drinking. The dental-suction alternative is ranked in 11.1;
  - the scraper, which is fingernails, not a blade.
- **Credits in the game.** CC BY needs the credits of `AUDIO_CREDITS.md` in the game and on the store page, and there is
  no credits screen yet.
- **Before release.** Rebuild from the Freesound originals (11.2). (The Dyson swap, 11.4, is done.)
- **The brush is not a `RecordedMachine`.** If a floor-machine take with a switch-on and a coast-down turns up, cut it
  with `cut_machine_recording.py` like the dryer. `ToolModelSet`'s spin rates would then need to follow its curves, or
  the bristles and the hum part.

## 12. AudioTap: logging the mix for offline audio (2026-09-26)

Store and trailer takes (`StoreGifApi`, `STORE_CLIPS.md` 5) step real frames in an unfocused Editor, where
`AudioSettings.dspTime` stands still and `AudioRenderer` records silence. So the trailer's sound is rendered offline
from a log of what the game's audio code does, and `AudioTap` is where that log comes from.

- **The tap.** `AudioTap.Emit(src, kind, source, clip, volumeScale, delay, start)` sits behind `if (AudioTap.Active)` at
  every site that starts or stops a sound. With the tap off a site costs one static bool read and no string is built;
  with it on nothing audible changes (the calls to the engine and their order are the same). A listener that throws is
  logged, never passed back into the sound code. Sites:
  - `ToolAudio`: power on / off and tool-switch one-shots (`tool/powerOn|powerOff|switch`), every crumb-rattle
    `PlayScheduled` (`tool/rattle`), every `Play` / `Stop` of the four loop sources (`loop/MotorA|MotorB|ContactA|ContactB`;
    a `play` carries the random entry point as `start`).
  - `RecordedMachine`: each `PlayScheduled` of a start / loop / stop clip with its offset, the `SetScheduledEndTime` of a
    spin-up (`stop` with a delay), and every voice `Release` (`rec/Recorded<Tool>/start|loop|stop|release`).
  - `UiAudio.Play`: the voice `Stop` (`ui/voice`) and the one-shot (`ui/<UiSfx>`). The pay slip, the shop and the
    inspection lamp (`SurfaceHighlight`) all play through it; `CarpetHandover`, `Economy`, `CelebrationFx` and `UiFx`
    play no sound of their own.
- **Events.** `AudioTapEvent`: `src`, `kind` (`oneshot` / `scheduled` / `play` / `stop` / `playing`), `clip`, `volume`
  (source volume x volumeScale), `scale`, `pitch`, `delay` (dsp-timed calls: scheduled time - now), `pan`, `loop`,
  `sourceId` (`AudioTap.IdOf`, stable per source; no `GetInstanceID`), `start`, `clock`.
- **The clock.** `AudioTap.DspTime` = `ClockOverride()` if set, else `AudioSettings.dspTime`. `ToolAudio`'s rattle,
  `DebrisSystem`'s capture stamp and `RecordedMachine` (when its own test hook is null) read it, so a take can give them
  a virtual clock that advances with its Steps. `ToolAudio.ResetScheduling()` silences the recorded machines and frees
  the rattle voices at the start of a logged take and again after it, because whatever was scheduled on the other clock
  lies 1000 s off. Setting `ClockOverride` bumps `AudioTap.ClockEpoch`; `DebrisSystem` stamps each crumb-capture
  readback with it and drops one that comes back on another epoch (counted in `received`, not delivered). The epoch
  never changes in the game.
- **Mixer.** None: the project has no `.mixer` asset and no source sets `outputAudioMixerGroup`; `AudioListener.volume`
  is never changed and the AudioManager volume is 1. The logged `vol` is therefore the final gain; the take header
  still records `listenerVolume` and a `mixerGroups` count.
- **Found on the way (not changed):** `ToolAudio`'s `OneShot` source is made by `LoopLayer.MakeSource`, which sets
  `volume = 0`, and nothing raises it. Unity scales a one-shot by its source's volume, so the power-on / power-off /
  tool-switch clicks should be silent in the game (to confirm by ear in a focused Editor). The log shows them with
  `vol` 0 and their requested `scale`. Fixing it (`_oneShot.volume = 1f`) would make them
  audible for the first time: a voicing decision, left open.
