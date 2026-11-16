# Steam gallery reshoot — 2026-10-25

Owner brief: preserve the first screenshot after the trailer; replace the other nine. Mix museum rug patterns with original cartoon characters. Dirty rugs must have dirty water or a dirty floor around them. Strokes should resemble a person's work. Latest direction: alternate macro detail and full gameplay views.

`final_order.json` is the replacement order (slots 2–10); slot 1 remains the exact existing `SteamStore/screenshots/20261025_current_paused.png`. Do not rebuild or replace that file.

Sequence: Anatolian extraction macro, dragon brush wide, European green scraper macro, fox foam wide, Salor hose macro, cat squeegee wide, Finnish rya dryer macro, axolotl finish wide, Transylvanian dry-vac macro. All are Gameplay, English, suitable for all ages.

No gameplay source, shaders, serialized assets, scene, lighting, shell mesh/count or postprocessing are edited. The actual gameplay mesh retains 48 shells. Wide captures use the existing game camera. Macro captures use `StoreShotApi.RenderView` with the explicit poses in `final_order.json`, with camera restoration after each render. No additional fibers, dirt compositing, sharpening, LUTs, AI imagery, or photographic overlays are added to screenshots.

Preparation and strokes are actual simulation dispatches. The continuous paths use Catmull–Rom interpolation with modest speed/pressure variation, tool heading initialized once per stroke, `ToolTuning.Effective` through `ToolController.StrokeSegment`, and `CarpetDebugApi.StepAll(1/60)`. Tools remain at factory tuning. Unequal overlaps, imperfect endpoints and missed dirty patches remain visible.

`floor_start.cs` and `floor_mess_*.cs` simulate rinsing and plowing a preceding dirty carpet. Later jobs reset the carpet with `notify:false`, preserving the genuinely simulated surrounding floor as normal handovers do. The job clock is restarted explicitly. No floor texture or dirt/water value is painted or synthesized. Later strokes add further runoff and debris. The two final-clean examples start from spotless carpet, then receive real water, extraction and drying strokes.

Capture: 3840×2160 HDR scene -> linear-light Lanczos downsample to 2560×1440. The real 2560×1440 UI Toolkit premultiplied HUD is layered at its native layout. FPS label hidden; other HUD text remains native English and the guidance remains the game's actual guidance. No title or explanatory copy appears on a screenshot.

Run in the open Play-mode Editor (no compile/refresh):

1. `python run_eval.py setup.cs` once, from the project root with full paths to these scripts.
2. `python final_pass.py` to generate saved recipes, then run `final_run_order.json` in order with `run_eval.run`.
3. Run `export_final.py` after every capture has completed. Keep its contact sheet and verification manifest.
4. `run_eval.py restore.cs`, unpause the Editor and stop Play. Restore both save files from `.before` copies in `SteamStore/_work/screens/reshoot_20261025/`, verifying SHA256 equality afterward. The simulation advances during photography; its initial state is not claimed to be restored.

`make_recipes.py` and `make_remaining.py` preserve the first wide-only staging iteration. `final_pass.py` is authoritative for the accepted alternating macro/wide direction. Diagnostic renders and the first iteration are working material, not upload assets. Final replacement JPEGs go into `SteamStore/screenshots/20261025_reshoot/`.

Publication status and final file hashes are recorded in the output manifest and session log after verification. Do not infer upload/publication from capture completion.
