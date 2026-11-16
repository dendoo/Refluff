# Trailer shoot recipes (2026-09-26/27)

Every take of the main trailer (`Assets/CarpetCleaning/Docs/TRAILER.md`) is reproducible from the eval scripts in this
folder. Takes are real `EditorApplication.Step()` frames at **60 fps** (`StoreGifApi.SeqBegin(..., fps: 60, audioLog: true)`),
captured to `SteamStore/_work/clips/<take>/` (PNG per frame, `seq.json`, `audio.jsonl`, `audio_clips.json`). The edit is
`Tools/steam_art/trailer/edl.json` -> `make_trailer.py`.

## Helpers (`common/`)

| File | What |
|---|---|
| `ev.sh FILE.cs` | one eval, prints only the result |
| `evp.sh FILE.cs ...` | evals with the scripted pointer parked off the play area first (`park.inc`): paused Steps read the OS mouse otherwise |
| `take.sh TAKE.cs` | parks the pointer, runs the take eval (SeqBegin + queue + first Run), then `cont.sh`: Run until done, SeqEnd until `seq.json` |
| `snap_save.cs` / `snap_load.cs` | GPU copy of every carpet + floor sim RT (debug, from the squeegee screenshot's `_r3probe`): a retake without replaying the staging. One slot, lost on a recompile, no debris / `_PlowAcc` |
| `sheet.py TAKE OUT.jpg [--crop] [--t0 --t1] [--ui]` | review sheet of a take (take times, frame numbers; `--ui` composites the HUD layer) |
| `q_uvs.cs` | carpet UV -> 4K pixel for camera rects |

Before anything: copy `%USERPROFILE%/AppData/LocalLow/VahovGames/Refluff/carpet_save_editor.json` aside; the kits,
hand-overs and purchases write it. Restore it byte for byte at the end (this shoot: md5 `923ed7495c25dbd3883c38c39a51351c`).

## Order (each block resets the rug; never interleave inside the chain)

| Block | Scripts (run from this folder) | Takes |
|---|---|---|
| Hook, Transylvanian | `evp.sh hook/h00_prep.cs hook/h01_wash.cs`, `take.sh hook/h02_take.cs`; then the squeegee screenshot's round-4 chemistry (`../../screens/recipes/reshoot_aces_0926/squeegee/`: `r3_s2_foam r3_b1_boost r3_s3_scrub x2 r3_s2_foam r3_s3_scrub r3_s4_soak x4 r3_s5_dry x7 r3_b2_boost r3_s6_flush x5 r3_b9_restore r3_f0_boost r3_f1_det r3_f2_mop r3_f3_mop r3_f9_restore r3_f8_settle r3_b2_boost r3_s6_flush x4 r3_b9_restore r3_t0_prewet r3_f0_boost r3_f6_mop r3_f9_restore r3_f8_settle` through `evp.sh`), `evp.sh hook/h03_lanehose.cs hook/h04_gates.cs common/snap_save.cs`, `take.sh hook/h05_take01.cs` | `t02_swirl` (#02), `t01_hook` (#01) |
| Arrival | `cat payoff/kit.inc payoff/trA_stage.cs > payoff/_trA_stage_full.cs`, `evp.sh` it, `take.sh payoff/trA_take.cs` | `tr04_arrival` (#04, #05, #14 dirty plate) |
| Chain, Anatolian 394 @0.9 | `python chain/src/_mk.py`; `evp.sh chain/src/c00_reset.cs`; then per take: staging with `evp.sh`, `common/snap_save.cs`, `take.sh chain/src/trNN_take.cs`. Staging between takes: after #06 clip 1's `c1_scrape1 2 3 4a 4b` + `p06b_settle` + `p07a_dv`; after #07 `p07b_dv` + `c1_dv`; after #08 `c1_hose` + `p08_settle`; after #09 `p09_foam` (brush AREA 6); after #10 `c1_brush` (from the pre-#10 snapshot), `c1_dv` x4, `c1_hose`, `p08_settle`, `p10_wait7` (foam gone, water 0.6 mm); after #11 `p12_row1`; #13 sets dryer POWER 6 itself | `tr06_scrape` `tr07_vacuum` `tr08_soak` `tr09_foam` `tr10_scrub` `tr11_squeegee` `tr12_extract` `tr13_refluff` |
| Clean plate | `evp.sh payoff/trB_stage.cs`, `take.sh payoff/trB_take.cs` | `tr14_clean` (#14 clean side + sparkles, #03 / #20 / #21 plates) |
| Hand-over, pay slip, shop | `cat payoff/kit.inc payoff/trC_stage.cs > payoff/_trC_stage_full.cs`, `evp.sh` it, `take.sh payoff/trC_take.cs` (dressing + take in one eval) | `tr15_payoff` (#15, #16, #17): pays $120, buys brush POWER $75 |
| Lamp | `python lamp/_mk.py`; `evp.sh hook/h00_prep.cs lamp/l1_pick.cs lamp/l3a_dust.cs lamp/l3b_dust.cs lamp/l3c_dust.cs lamp/l4_wash.cs lamp/l5_extract.cs lamp/l6_floor.cs lamp/l4_wash.cs lamp/l5_extract.cs lamp/l6b_floor.cs lamp/l4b_strip.cs lamp/l6c_floorvac.cs lamp/l6d_floordet.cs lamp/l6e_edge.cs lamp/l6h_residue.cs lamp/l6i_dry10.cs lamp/l6j_floordry.cs lamp/l8_money.cs`; `lamp/l2_probe.cs` renders X / Z probes; `take.sh lamp/t18_take.cs`; `evp.sh lamp/l9_off.cs` | `t18_lamp` (#18) |
| Montage | `python montage/_mk.py`; rya: the rya screenshot's `run.sh` ORDER from `pre2` to `n_dry2`, then `evp.sh montage/a0_band.cs`, `take.sh montage/a1_take.cs`; Salor: `hose_final` `h1`-`h6`, `montage/b0_settle.cs`, `take.sh montage/b1_take.cs`; European: `scripts_scraper_lamp_spot` `l_st1`-`l_st5`, `take.sh montage/c1_take.cs`; Transylvanian: `evp.sh montage/d1_stage.cs montage/d2_lane.cs`, `take.sh montage/d3_take.cs` | `t19a_rya` `t19b_salor` `t19c_eu` `t19d_mop` |

## Retake 2026-09-28: #07 and #08 with the pile lay (`retake_0928/`)

The dry vac and the hose's water lay the pile since 2026-09-27 (`FIBER_SET.md` 4-7), which came after the chain was
shot. Both takes were shot again with the same Steps from the same state; the edit uses `tr07b_vacuum` / `tr08b_soak`
with the old in-points and cameras, and nothing else changed. The old takes and the 09-27 master are kept
(`SteamStore/_work/trailer/master_0927/`).

| Step | Scripts (run from this folder, Play mode) | Printed |
|---|---|---|
| save aside | copy `carpet_save_editor.json` (the kit writes tool levels) | |
| kit + chain S0 | `cat payoff/kit.inc chain/src/c00_reset.cs > retake_0928/r00_kit_reset.cs`, `evp.sh retake_0928/r00_kit_reset.cs` | star, 394 @0.9: dust .648 mud .915 crust .612 spill .889 |
| #06, no frames | `take.sh retake_0928/r06_replay.cs` (tr06's queue, every 100000, no audio log; delete `_rs06` after) | 211 Steps, as tr06 |
| before #07 | `evp.sh ../../clips/recipes/clip1_reveal/c1_scrape1.cs` (`2 3 4a 4b`) `chain/src/p06b_settle.cs chain/src/p07a_dv.cs common/snap_save.cs` | crust 0.387, grit 0.130 |
| #07 | `take.sh retake_0928/tr07b_take.cs` | `tr07b_vacuum`, 226 frames, 0 errors |
| before #08 | `evp.sh chain/src/p07b_dv.cs ../../clips/recipes/clip1_reveal/c1_dv.cs common/snap_save.cs` | grit 0.098 |
| #08 | `take.sh retake_0928/tr08b_take.cs` | `tr08b_soak`, 186 frames, 0 errors |

Check: the crust slabs match the 09-27 takes to the pixel (MAD 0.00 over slab areas), so the state is the same; only
the pile differs (band MAD 7-11 in #07, window MAD 1-2 in #08). Hose variants tried from the pre-#08 snapshot (full
pressure spots, full / half pressure sweeps, five short taps, spot-drag-spot): on this band every one is a flat murky
sheet, because `CarpetWater`'s murk counts the pile's mud (mud ~0.65 saturates it at once), and full pressure floods the
slabs. So #08 keeps the approved three half-pressure spots; its new look is the vac's combed lanes at the start and a
radial starburst of laid pile in each jet crater.

## Lessons (this shoot)

- The floor has no lamp level: any runoff, residue, crumb line or damp mop film glows under X / Z. The lamp staging needed
  wet-vac lanes (the mop only pushes water), detergent + mop for dried residue at the mop lanes' ends, and the dryer on the
  tile for the last damp film. Probe with `lamp/l2_probe.cs` after every change.
- Brushing soapy foam makes MORE foam (F 1.4 -> 5.1); the foam never greys past Fd/F 0.04 here. To get a brown squeegee
  wave after the scrub, the dry vac takes the foam (and the water), then a hose top-up and a 7 s wait.
- Room reflection (`Shaders/RoomReflection.hlsl`): with a 1.2 m panel grid a flooded rug showed white blotches / a
  "sheet of paper"; the shipped room has long fixtures every 3-4 m, one of them mirrored exactly where the squeegee pours.
- A `take.sh` must not treat `"errors":0` as a failure (fixed): check for `"error":` only.
