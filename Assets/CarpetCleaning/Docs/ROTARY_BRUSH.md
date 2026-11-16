# Rotary brush (tool 5)

Since 2026-09-24 the brush is the brush of a **rotary floor machine** (user request, from a rug-cleaning video). Rug
cleaners run such machines at about 175 rpm; ours turns at 350, doubled so the spin reads. This document covers what
the spinning pad does to the carpet and the floor, and its motor sound.

The head is covered in `TOOL_MODELS.md` 7:
- the v2 model: a still grey top (clutch plate, bolts) over blue bristles;
- the bristles spin while the tool works;
- the smear copies keep them from strobing;
- the head sinks into the pile.

**Status: the pad physics and the sound were built and compiled, never examined in Play mode.** The user judges them
live; only the v2 head's look was captured in Play. All numbers below are design values or offline measurements, not
in-game results.

## 1. The pad

`BrushPadVelocity(p)` in `Compute/DebrisCommon.hlsl` is shared by the carpet and floor sims:

```
omega = BRUSH_RPM * 2 pi / 60                 // 350 rpm -> 36.7 rad/s
vPad(p) = omega * (p.y, -p.x)                 // p = CMM offset from the stamp centre, mm
```

- **Direction.** This is clockwise seen from above (CMM x = world +X, CMM y = world +Z), the way `ToolModelView` spins
  the head.
- **Speed.** At 350 rpm the pad moves 1.15 m/s at r 31.5 mm, where the disc footprint stops being full weight, and
  1.65 m/s at the 45 mm rim.
- **Stamps.** Every frame with the button down carries at least one stamp. A stationary pointer gets a hold stamp
  (`ToolController.StrokeSegment`), so the pad acts while held still too.

## 2. Carpet (`CarpetSim.compute`)

### K_Stamp, tool 4

The bristles rub the carpet at `vRub = vPad + vStroke`. Everything the brush does by rubbing now keys off that
velocity instead of the stroke's:

| effect | before | now |
|---|---|---|
| agitation | `1.5 mp sat(speed / AGIT_SPEED)` | `1.5 mp sat(|vRub| / AGIT_SPEED)` |
| pile bend `b`, flick `vb` | along the stroke `dir`, x `mech` | along `vRub`, x `sat(|vRub| / SPEED_REF)` |
| wet-crust abrasion | x `mech` | x `sat(|vRub| / SPEED_REF)` |
| tool flow `vTool` | `0.8 vStroke` | `0.8 vStroke + BRUSH_SWIRL vPad + BRUSH_FLING |vPad| radial` |
| flow weight `wt` | `mp mech` | `mp sat(|vTool| / SPEED_REF)` |

`wf` stays 0.35, so the water follows the tool flow at 0.35 through the `K_Flux` blend.

Consequences:
- **Held still.** The brush now scrubs, foams and abrades while held still. That is what a real machine does, and it is
  the one gameplay change: before, a still brush did nothing.
- **Pad centre.** The centre of the pad moves slowest, so it scrubs least, as on a real pad.
- **Foam swirl.** The foam rides the swirl through `FoamOutflow`: `wt * vTool`, clamped to ±600 mm/s.
  - With `BRUSH_SWIRL` 0.4 it circles at about 460 mm/s at r 31.5 mm, about 2.3 turns per second. Add a fast
    stroke's `0.8 vStroke` and the ±600 mm/s foam clamp caps it there.
  - `BRUSH_FLING` 0.1 adds up to about 115 mm/s outward inside the full-weight disc. Beyond r 31.5 mm the push fades
    with the footprint weight (x0.3 at r 40 mm), so the thrown foam slows and heaps into a ring at the footprint's
    edge. As the pad travels, it leaves arcs behind.
- **No foam pile-up.** A pure swirl is divergence-free and cannot pile foam up. Only the fling term concentrates it.
- **Stability.** The substep is 1/240 s and the cell 3.125 mm, so the CFL limit is 750 mm/s. The foam clamp of
  600 mm/s stays under it.

### K_Fiber, the fibre set

For the brush, the set direction `dirB` is the direction of `vRub`, and `mechB` and the dry-pile fluff use
`|vRub| / SPEED_REF`. Wet pile therefore keeps the machine's arcs (cycloids along a stroke) instead of straight stripes,
and a pad held still presses the set and fluffs dry pile. The swept-contact time is unchanged (`FIBER_SET.md`).

## 3. Floor (`FloorSim.compute` `K_FInject`, tool 4)

- Same agitation and tool flow as the carpet, against `F_AGIT_SPEED` / `F_SPEED_REF`. The floor has no pile, so there
  is no styling.
- `FloorSimulation.UploadUniforms` copies `_BRUSH_RPM` / `_BRUSH_SWIRL` / `_BRUSH_FLING` from the carpet's
  `SimSettings`, so it is one machine on pile and tile. Without a carpet sim they are 0: no swirl.

## 4. Knobs (`SimSettings`, header "Rotary brush")

| field | default | what |
|---|---|---|
| `BRUSH_RPM` | 350 | pad speed; keep equal to `ToolModelSet` brush `spinRpm` (the visual twin). Doubled from 175 on 2026-09-24 |
| `BRUSH_SWIRL` | 0.4 | share of the pad speed the foam (and, at `wf`, the water) is dragged round with |
| `BRUSH_FLING` | 0.1 | outward throw as a share of the pad speed at that radius |

- `BRUSH_RPM` 0 turns the whole rotary behaviour off: the stroke-only brush, except that the flow weight is keyed to
  `|vTool|` instead of `mech`.
- If held-still scrubbing turns out too strong for the game, lower `BRUSH_RPM` for the physics only.

## 5. Sound (`AUDIO.md` 2)

The brush is now a `powered` entry with the new `SfxRecipe.RotaryMotor`, which is `MotorLoop(340, 100, 520, 0.5, 1.6)`:
a heavy motor's 100 Hz hum, a dull cooling-air roar and a faint gearbox whine. It is noise-first, like every other
recipe.

| | value |
|---|---|
| machine | volume 0.40, spin up / down 4 / 1.2 (= `ToolModelSet`), pitch min 0.80, idle cutoff 700 Hz, speed pitch 0.01 |
| power-on thud | 55 Hz (other motors 95 Hz, wet vac 70 Hz) |
| surface (Scrub) | idle 0.03 -> **0.60**: the pad scrubs held still |
| `tool_brush_motor.wav`, measured | tonal 6.7 dB @ 334 Hz (vacuums 8.1-10.8), am 5.8 dB, st90 11.8, -13.2 dBFS RMS, loop seam 0.52 |

- **Baking.** `ToolSoundBaker.BakeTool(ToolId.Brush)` wrote and assigned only the brush's four clips. The voicing
  went in with `ToolSoundBank.ResetVoicingKeepClips(ToolId.Brush)`. Only the brush entry of `ToolSoundBank.asset`
  changed.
- **Three spin states.** Each has its own copy of the same numbers:
  - `ToolModelView` (the bristles);
  - `ToolAudio` (the hum);
  - the sim, which runs at full speed while stamping.

  They start from the same events (a stamp or the button) with the same 0.12 s grace and the same 4 / 1.2 easing.

## 6. Not done

- Debris is still flicked along the stroke (`Debris.compute` brush branch), not thrown off the pad tangentially.
- The pad physics and sound were never examined in Play mode. To check:
  - that the foam swirl reads;
  - that the ring of flung foam is not too strong;
  - the arcs on wet pile;
  - the held-still scrubbing balance;
  - the hum level against the scrub.
