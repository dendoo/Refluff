# TikTok "Every fiber is simulated" - shoot recipes (Docs/TIKTOK.md)

`python _mk.py` writes the take evals `t1..t7_take.cs` and the staging evals `p*.cs` (idempotent; the camera keys, strokes
and levels live there). Each take = `look_on.inc` (48 shells, raking sun, DoF volume) + `StoreGifApi.SeqBegin` portrait
2160x3840 @ 60 fps with the audio log + `cam.inc` (free camera on a Catmull-Rom path, hooked on
`RenderPipelineManager.beginContextRendering`) + the StoreGifApi queue. Takes land in `SteamStore/_work/clips/tt*`.

Run from Git Bash (`export MSYS_NO_PATHCONV=1`), Play mode on, the economy save backed up first
(`SteamStore/_work/tiktok/carpet_save_editor.backup.json`, restored byte for byte after Play is stopped):

    T=Tools/steam_art/trailer/recipes/common
    R=Tools/steam_art/tiktok/recipes
    bash $T/evp.sh $R/s00_setup.cs      # the Anatolian star on the stage, hero kit levels, lamps owned + off
    bash $T/evp.sh $R/s01_reset.cs      # ResetWithProfile(394, 0.75, crust 0, mud 0.45, dust 0.85), LiveTuning on
    bash $T/take.sh $R/t1_take.cs       # tt1_hook      no tool, low dolly over the dusty upper star
    bash $T/take.sh $R/t2_take.cs       # tt2_vacuum    dry vac R->L along v 0.36
    bash $T/evp.sh $R/p2_vac.cs         #   the vac lane widened to the whole band
    bash $T/take.sh $R/t3_take.cs       # tt3_hose      three half-pressure spots
    bash $T/evp.sh $R/p3_soak_foam.cs   #   soak + foam the band, brush AREA 6
    bash $T/take.sh $R/t4_take.cs       # tt4_brush     rotary brush through the foam
    bash $T/evp.sh $R/p4_scrub.cs
    bash $T/take.sh $R/t5_take.cs       # tt5_squeegee  one pass off the right rim onto the tile
    bash $T/evp.sh $R/p5_extract.cs     #   wet vac the band, dryer POWER 6
    bash $T/take.sh $R/t6_take.cs       # tt6_dryer     dryer L->R, low raking camera
    for f in p6a1_vac p6a2_vac p6b1_hose p6b2_hose p6c1_wetvac p6c2_wetvac p6d1_wetvac2 p6d2_wetvac2 \
             p6e1_dry p6e2_dry p6f1_dry2 p6f2_dry2 p6g_settle p6h_stain p6i_stain; do bash $T/evp.sh $R/$f.cs; done
    bash $T/take.sh $R/t7_take.cs       # tt7_reveal    macro on the clean / dirty edge -> whole rug from above
    bash $T/evp.sh $R/look_off.cs       # 24 shells, game sun / ambient / camera back, DoF volume + camera hook removed

Cut 1.1 retake of T7 (crust 0.3 on the dirty half, Docs/TIKTOK.md 6.1): after `s00_setup.cs`,
`bash $R/retake_t7.sh` (= `s02_reset_crust`, `p6s1/p6s2_scrape`, the p6 chain with `p6i1` + `p6i2`, then the take and
`look_off`; `stage` / `take` run one half). `run.sh NAME...` runs single recipes.

Every p*.cs ends with `SurfaceProbeJson` + `StatsJson` of the band so a bad step shows before the next take.
Stills for picking the angles: `stills.sh` / `q_stills.body` -> `SteamStore/_work/tiktok/stills/`. `q_*.cs` are probes.
Edit: `python Tools/steam_art/trailer/make_trailer.py --edl Tools/steam_art/tiktok/edl_tiktok.json --out SteamStore/_work/tiktok/refluff_tiktok_fibers.mp4`.
