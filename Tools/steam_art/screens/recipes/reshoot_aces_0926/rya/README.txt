ACES re-shoot 2026-09-26, slot 05 = 12_rya_half_clean. Art-director ROUND 2 (final take 13:23): crusty seed 367 at level 0.9
(seed route, no ResetWithProfile, no level 1).
Base = reshoot_0926/reshoot_d/pre2 + reshoot_d/rya (retake order of scout_runbook 4.4); changed / new files marked *.
Run: bash run.sh   (self-contained; "bash run.sh w_ryaA" skips the economy kit + picture pick; ends with compose_screens.py)
Order (one eval each, all keep EditorApplication.isPaused = true; each runs < 0.4 s):
  pre2 pick_rya *w_ryaA *ryaScrape *ryaScrape2 *ryaScrape3 *ryaScrape4 *ryaDv *ryaDv2 w_rya2 ryaS ryaS2 ryaS3 ryaS4 ryaS5
  ryaS6 ryaS7 w_ryaW ryaW2 *ryaR *ryaR2 *ryaW3 w_ryaD w_ryaF *ryaFloor1 *ryaFloor2 n_dry *n_dry2 *n_foam1 *n_foam2 n_shot
Changes vs the old retake (each file's header says why):
- w_ryaA: Reset(367, 0.9f); no whole-carpet dry-vac passes (no-HUD crop: the dirty right part keeps its crust untouched).
- ryaScrape/2/3: 3 slow DRY scraper passes (150 stamps/lane, v 1.01 <-> -0.01) over u 0.00..0.645 -> crust edge at u 0.715.
- ryaScrape4 (round 2): 4x lane 0.645 + 2x 0.625: scrapes the crust RAMP u 0.66-0.715 (crust 0.04-0.17 -> < 0.01). That thin
  crust kept the pile matted, the foam built a second ridge on it = the dark violet crease of round 1 (NOT n_foam3's fault).
- ryaDv (round 2: lanes to u 0.655, reach 0.71) + ryaDv2 (slow dry vac u 0.62-0.69: grime 0.6-0.9 -> 0.2-0.3, so the foam
  does not turn mauve over the band's right half). Hosing the band strip instead (tried: _tried/ryaB) soaks and eats the
  crust edge beside the band (crust 0.85 -> 0.3 at u 0.72-0.77): never water u > 0.5 directly.
- ryaR + ryaR2 (round 2): clear-water rinse + extraction of the clean half. Without it w_ryaD dries the dirty water left in the
  pile back into grime (mud 0.02, clean half ~6 % darker than the spotless rug); with it mud 0.001.
- ryaW3 (round 2): wet vac u 0.53 + 0.60 x2 (the washing runoff on the band strip, else n_dry bakes it into a brown seam).
- ryaFloor1/2: the near tile strips run to u 0.86 / 0.90 (scraper chunks land on the near tile at u 0.60-0.80).
- n_dry2 (round 2): dryer u 0.63 x2 - damp pile under the band's right half held the foam up into a tall crest.
- n_foam1/2 (round 2): lanes u 0.595 + 0.635 (was 0.585 + 0.62): one smooth mound u 0.51-0.72 whose flank ends on the crust
  edge. n_foam3 (round 1's extra lane) is gone; round 1's variants are kept in _round1/.
Result (sim, before the shot): clean half u < 0.47 crust 0.001 / mud 0.001 / stain 0.002 / pile w 0.02; band strip mud 0.2-0.3,
no crust; dirty strip u > 0.72 crust 0.865 (99 % caked, dry); brush spot crust 0.75 (84 %: its left edge overlaps the band
flank); 28 scraper chunks left, all on the band's right border / crust at u 0.70-0.80, 0 on the tile; floor dirtyCells 3e-5.
HUD (hidden by the store crop): Clean 66 %, Dry 95 %, 5:48 / par 10:00, $1,960, Brush, "Scrape the dry crust off".
Store crop (build_store_art SHOT_VARIANT (528,232,3072,1728), unchanged): luminance clean box 78.3 (round 1 74.5, spotless rug
under ACES ~80), dirty box 95.2.
