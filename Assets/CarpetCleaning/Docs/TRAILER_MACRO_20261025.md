# Macro trailer reshoot — 2026-10-25

**Current revision: v4, 57.75 seconds**, `SteamStore/trailer/macro_20261025/revision4/refluff_macro_trailer_v4.mp4`.
The opening glides forward between fibers for two seconds at triple v3's speed, then reveals the entire rug and approaches the vacuum
while dirt gradually appears. Earlier versions remain preserved; action takes 02–09 are unchanged.

The owner approved a new roughly 50-second trailer focused on close views of pile reactions, demonstrated visually,
with the opening inspired by the latest between-fibers short. No narration, explanatory captions or music.
The red Transylvanian medallion rug ties the shots together. The accepted V4 replaced the Steam trailer and was
published on 2026-10-25 together with the new first screenshot (Store Publish revision 7; details below).

## Original 50-second cut (revision 1)

| Time | Take | Visible action |
| --- | --- | --- |
| 0–3 s | mf01_intro | Repeat the short's between-fibers camera setup in native horizontal framing; begin pulling back at 0.25 s. |
| 3–9 s | mf02_vacuum | Match cut to dusty pile; suction, laid trail and spring-back. The game's dry vacuum is its suction ring, with no 3D nozzle. |
| 9–15 s | mf03_hose | Three short half-pressure touches; spreading water combs the pile radially. |
| 15–21 s | mf04b_brush | Small foam application offset toward the far edge of the brush path; three rotary-brush passes. |
| 21–27 s | mf05_squeegee | Follow the geometric plow toward the right carpet edge; flattened pile remains behind the water/foam roll. |
| 27–32 s | mf06_extract | Wet vacuum removes residual free water; the pile remains damp and laid. |
| 32–41 s | mf07_dryer | Fixed low camera; 4.8-second dryer pass followed by an observation hold. Pile height visibly recovers. |
| 41–46 s | mf08_reveal | Clean pile to a wider view of the red ornament. |
| 46–50 s | mf09_end | Actual Refluff logo and Wishlist on Steam over a gently retreating view. |

## Capture

- Recipes: `Tools/steam_art/trailer/macro_20261025/`; raw native 1920×1080 PNG frames and virtual-clock audio logs:
  `SteamStore/_work/clips/<take>/`. Final selections contain exactly 3,000 frames/Steps at 60 fps, all at original speed.
  Every selected SeqEnd returned pendingPng 0 and errors 0. No runtime C# or shader changes were needed.
- Unity CLI status found the running Editor, but its eval commands timed out on the main thread. Unity MCP
  `execute_code`, compiler `codedom`, performed the temporary staging and existing StoreGifApi real-Step capture in memory.
  No project recompile, domain reload, build or scene save was requested.
- Camera follows the existing between-fibers logarithmic-distance interpolation, hooked to
  RenderPipelineManager.beginContextRendering. Near clip 0.3 mm. Initial microscopic hold lasts 0.25 seconds.
- Temporary capture meshes: 384 shell layers for the opening, 96 for the remaining close-ups; gameplay remains 48.
  This adds rendering samples only. Strand layout, pile height and simulation constants stay unchanged.
- Temporary light: pitch 40°, yaw 30°, intensity 4.8, temperature 4800 K, soft shadows; ambient sky (0.62, 0.65, 0.72).
  Uses the game's ACES volume and no added depth of field.
- Intro is pristine. The story begins with ResetWithProfile(394, 0.70, crust 0, mud 0.40, dust 0.85, spill 0.12).
  The cleaning chain 02–07 runs on this state with factory tools. Shot 04's first take hid too much pile behind foam;
  it is retained as mf04_brush. After a complete 720-Step replay of 02+03, the selected mf04b uses a 0.15-second,
  half-pressure foam sweep at v=0.40 and brush passes around v=0.36. No simulation settings were weakened for the shot.
- Before 08, the rest of the same carpet was physically washed, scrubbed, extracted and dried with scripted strokes,
  rather than resetting to clean. `prepare_final.cs` retains those off-camera passes and their temporary tool tuning.
  Final measured cleanliness 0.989596, wetness 0.0011709, no free water/foam/slurry, valid finite state.
- Camera, light, material picture/fit, mesh and input were restored; capture hooks and temporary meshes removed;
  Editor stopped. Both carpet_save_editor.json and carpet_save.json restored from pre-session copies and hash-verified.
  Private backup copies are local under `_work/macro_20261025/save_backup`; never include them in public packages.

## Edit and verification

EDL and scoped compose.py reuse the existing trailer renderer and authentic gameplay foley. The vacuum motor anticipates
its picture cut by about half a second; surface sounds retain source timing. The final mix details and numeric verification
are recorded by the render/QA reports beside the new master.

Output: `SteamStore/trailer/macro_20261025/refluff_macro_trailer_v1.mp4`, with cover, contact sheets, stems and reports.
Rebuild and QA commands: `Tools/steam_art/trailer/macro_20261025/README.md`.

Independent raw-frame review: intro reveals the carpet promptly; vacuum lay/recovery, hose's spreading fan,
brush-edge motion, squeegee trail and dryer height recovery are visible. Extraction has a subtler before/after difference.
Final encoded-master validation is recorded in SESSION_LOG.md after the render completes.

The initial capture/review stages did not upload or publish anything. Subsequent owner-approved Steam publication
is recorded below; no social-platform registration or publication was performed.

## Steam publication — 2026-10-25

- Owner explicitly accepted V4, authorized its Steam upload/publication, then requested the current paused Play frame
  as the first screenshot and publication of both together. The owner performed the two local-file drops because
  the current Steamworks movie/image drop zones expose no file chooser supported by CUA.
- App 5304930 / Store Admin 1337056: existing movie item 1357534 retained its name, Gameplay, English,
  visibility, before-screenshots, all-ages and unrestricted-country settings. Trailer ID changed from 178391048 to
  **1618490617**. Steam completed conversion before publication; the full accepted V4 SHA256 is
  `86f0066bef312868e4ca0c43442634c480d2ab4f05de90c77ef65c40f5b9edb6` (131705711 bytes).
- The published thumbnail is Steam's automatically generated V4 asset, hash
  `5b16076fb22b85a0dae854bded05ddebfd432fc9`; the adjacent custom cover PNG remains a local deliverable.
- First screenshot: `SteamStore/screenshots/20261025_current_paused.png`, 2560×1440 / 4139083 bytes,
  SHA256 `32f1c0879314d045e56a5d981c5af620a6b8dd2a6c984d5e4fb0ff146940a85a`,
  Steam asset SHA1 `444bbebfbfc5b40917c1e35ecb860fdd63a67a89`. Exact camera/HUD frame, no Step or unpause:
  before/after playing=true, paused=true, frame2219, Time35.87123. The prior nine pictures and their alt text remain
  in their existing order. New screenshot category Gameplay; the final upgrade-shop screenshot is In-Game Menu.
- Saved changes and reviewed Store Diffs: new first screenshot/order/categories/asset timestamp plus the trailer
  replacement/migration ID. Store **Publish to public** returned **Successfully published!**, revision **7**.
  On the anonymous public storefront, the new macro trailer plays (0:57 displayed, seek maximum58), followed by
  the capybara screenshot as the first still. Public verification recorded at 2026-10-25 08:01:54 UTC.
- Evidence/IDs/order are retained in `SteamStore/trailer/macro_20261025/revision4/publication.json` alongside
  `steam_publish_success.jpg`, `steam_live_trailer.jpg` and `steam_live_first_screenshot.jpg`.
  Game release, builds, branches, prices and Steamworks App Admin publishing were not changed.

## Revision 2 — full-rug reveal and gradual soiling

- New take `mf01b_intro`: 540 real Steps/PNG frames, 9.000 seconds, native 1920×1080/60. SeqEnd errors 0, pendingPng 0.
  0–0.25 s microscopic hold; pullback to the entire clean rug by 3.3 s; hold until 4.0 s;
  approach from 4.0–8.7 s; final camera hold until 9.0 s. Full-rug camera (0,1.7,-1.7), target (0,.01,0), FOV 43.
  Endpoint exactly repeats shot 02 camera (.06,.10,-.43), target (0,.01,-.21), FOV 43.
- Staged soiling transition, explicitly requested by the owner: snapshot the target Dirt RT from the same seed/profile
  as the vacuum take, retain its seed-derived Crust/Bed metadata, and upload Dirt = target × smoothstep from 4.2–8.5 s.
  This is a capture transition, not a new gameplay dirt-accumulation mechanic. No whole-frame image dissolve is used.
  Linear RGBAFloat read/upload roundtrip and final simulated Dirt both match the target exactly (max channel difference 0).
  `dirt_ramp.csv` records every frame; coefficient is monotonic 0→1, largest increment 0.005814 per frame.
- Use 384 capture shells until 2.2 s, then 96 while distant and throughout the return, matching the vacuum take.
  Neighbor-frame comparisons around the mesh change show no anomalous jump; independent contact-sheet review passed.
- At 3.3–4.0 s all four carpet edges remain visible. Final intro frame versus first vacuum frame:
  mean absolute RGB difference 0.000765/255, 99th percentile 0, with only 0.0651% of pixels differing at all.
  No visible camera or dirt-state discontinuity. QA sheets/report: `SteamStore/_work/macro_20261025/revision2/qa/`.
- New edit/recipes in `Tools/steam_art/trailer/macro_20261025/revision2/`. Original takes 02–09 and their speed,
  gains and foley are preserved. Logo/CTA starts at 52 s. Original master is not overwritten.
- Runtime camera/light/material/mesh/input restored, ramp texture and hooks removed, Play stopped;
  both carpet saves restored from fresh revision-2 backups with matching hashes.

## Revision 3 — two-second forward glide

- Owner requested slow forward camera travel between the fibers for the first two seconds, before the pullback.
  New take `mf01c_intro`: 645 real Steps/frames, 10.750 s, native 1920×1080/60; SeqEnd errors 0, pendingPng 0.
- Initial camera translates from (.03,.019,-.235) to (.03,.019,-.232) over exactly 0–2 s. Its look target also moves
  +3 mm along z, preserving the viewing direction, height and FOV50. Smoothstep eases to zero velocity before reversing.
  This is actual camera translation with parallax, without a digital zoom or a still-frame hold.
- Pullback starts after 2 s; whole rug reached 5.05 s, held to 5.75 s. Return 5.75–10.45 s, final hold to 10.75 s.
  Dirt ramp 5.95–10.25 s. The previous wide/return/soil timing is shifted +1.75 s. Capture shells change to 96 at 3.95 s.
- Raw `camera.csv` and `dirt_ramp.csv` are retained beside the take. First-two-second QA confirms +3.000 mm along z,
  no x/y displacement, constant FOV and direction, no reverse steps before frame 120, visible differential parallax,
  and no full-frame fiber occlusion. Endpoint Dirt RT again matches the target with max channel difference 0.
  QA sheets and reports: `SteamStore/_work/macro_20261025/revision3/qa/`.
- Existing action cuts 02–09 move another +1.75 s; full trailer is 57.75 s / 3,465 frames, logo/CTA begins at 53.75 s.
  Recipes/EDL/composer under `Tools/steam_art/trailer/macro_20261025/revision3/`. No runtime source/settings/shader change.
  Runtime view/input restored, temporary assets/hooks removed, Play stopped, fresh save backups restored and hash-verified.

## Revision 4 — triple the initial forward speed

- Owner requested three times the camera speed during the between-fibers forward move. Preserve the previously requested
  two-second duration and triple camera/target travel from +z3 mm to +z9 mm. Endpoint at2 s is now camera(.03,.019,-.226),
  target(.03,.004,-.201); x/y, viewing direction, FOV50 and easing stay unchanged. Pullback still begins after2 s.
- New `mf01d_intro`:645 real Steps/frames,10.750 s,1080p60; SeqEnd errors0/pendingPng0. Camera CSV comparison with v3
  gives median speed ratio3.0, +9.000 mm in2 s, path error against triple displacement <=0.0002 mm, no reverse steps.
  Independent image review confirms faster parallax, visible forward clearance and no full-frame fiber occlusion.
- Full-rug hold5.05–5.75 s, approach5.75–10.45 s and soil5.95–10.25 s retain their v3 timings. Final Dirt vs target maxdiff0;
  final intro/vacuum image boundary meanabsRGB0.000765/255,p99=0. Mesh transition and full raw contact review pass.
- Full trailer remains57.75 s/3465 frames. Action take/edit objects02–09 are identical to v3; original speeds/gains/foley
  retained. New recipe/edit/output folders `revision4/`; v1–v3 remain available.
- Original runtime view/input restored, ramp texture/meshes/hooks/logs released, Play stopped, and both fresh save backups
  restored with matching hashes. No runtime source/shader/settings edits.

## Social publication — 2026-10-25

- Owner requested immediate TikTok, YouTube Shorts and Instagram publication of the accepted trailer, delegated the
  caption and platform-specific Steam UTM links. Caption starts: Tiny fibers. Deeply satisfying cleaning. Turn the sound
  up and watch this rug come back to life. Six CC BY recording credits were retained for this current video.
- TikTok @vahovgames and Instagram @vahov_games received the original 1920×1080 V4 unchanged. Instagram crop Original,
  sound on; TikTok marked Your brand / Promotional content. Both were submitted once and verified at their exact URLs.
- Because YouTube classifies square/vertical videos as Shorts, the owner explicitly chose a full-picture 9:16 version:
  1080×1920, complete landscape image scaled1080×608 at y656 with black padding. No crop, title, timing or audio edits.
  57.75s/3465frames/60fps, H264High/BT709, 73390903bytes, SHA256
  `62dd2410b52b342b8451d7301d2f66f77dc18b7d1691f79e91d35d58fa0c7bca`; AAC packet and decoded PCM hashes
  equal the original, full decode and two visual sample checks passed (`social/shorts_qa.json`).
- Published and verified by08:36:43UTC /11:36:43Moscow:
  [TikTok7700519431457213710](https://www.tiktok.com/@vahovgames/video/7700519431457213710),
  [YouTube Shorts ds8PbL4DVsA](https://www.youtube.com/shorts/ds8PbL4DVsA),
  [Instagram De6R_OvPcDh](https://www.instagram.com/p/De6R_OvPcDh/).
  YouTube Studio reported Video published and Copyright No issues found; Instagram Your reel has been shared;
  TikTok's temporary review cleared to Everyone before completion. Public footage/account/captions/UTM checked.
- All links use utm_source=tiktok|youtube|instagram, utm_medium=social, utm_campaign=macro_trailer_20261025,
  utm_content=v4. Complete captions, plan, published records/receipts and proof screenshots are in
  `SteamStore/trailer/macro_20261025/revision4/social/`. SocialStudio tracks three published posts on two media clips.
  CUA performed the actual UI publication; no claim is made that the automatic CLI publishing adapters were validated.
- During this task the owner requested descriptions without credit blocks for the next video. Saved in TIKTOK.md and
  the shared SocialStudio.md; future sound should be original/CC0 without attribution requirements. Current posts kept
  the prepared captions, as requested for this publication. No Unity operation, game save, Steam store/build or profile edit.
