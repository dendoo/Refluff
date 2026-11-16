ACES re-shoot 2026-09-26, slot 05 = 12_rya_half_clean, art-director round 1: crusty seed 367 at level 0.9 (seed route).
Base = reshoot_0926/reshoot_d/pre2 + reshoot_d/rya (retake order of scout_runbook 4.4); changed / new files marked *.
Order (one eval each, all keep EditorApplication.isPaused = true; each runs < 0.4 s):
  pre2 pick_rya *w_ryaA *ryaScrape *ryaScrape2 *ryaScrape3 *ryaDv w_rya2 ryaS ryaS2 ryaS3 ryaS4 ryaS5 ryaS6 ryaS7
  w_ryaW ryaW2 w_ryaD w_ryaF *ryaFloor1 *ryaFloor2 n_dry n_foam1 n_foam2 *n_foam3 n_shot   (then compose_screens.py 12_rya_half_clean)
Changes vs the old retake:
- w_ryaA: Reset(367, 0.9f); the two whole-carpet dry-vac zigzags and w_dvB x2 are gone (no-HUD crop: the dirty part keeps its crust).
- ryaScrape/2/3 (new): 3 slow DRY scraper passes (150 stamps per lane, v 1.01 <-> -0.01) over u 0.00..0.645 -> crust edge at u ~0.72,
  i.e. the clean half AND the strip under the foam band are scraped. Crust keeps sprayed foam on top while bare pile soaks it in, so a
  crust edge (u 0.585 or 0.53 tried) or seed 367's grime islands under the band split it into mounds with dark steps.
- ryaDv (new): dry vac on u 0.03..0.47 only (lifts the scraper grit, sucks the chunks in the half that gets cleaned).
- ryaFloor1/2: the near (bottom) tile strips run to u 0.86 / 0.90 (scraper chunks land on the near tile at u 0.60-0.80).
- n_foam3 (new): extra sprayer lane u 0.655 so the band's right flank covers the crust edge (no bare scraped strip between foam and crust).
Result (sim): clean half (u < 0.47) crust 0.002 / mud 0.02 / stain 0.002; dirty strip u > 0.70 crust 0.82 (97 % caked);
brush spot crust 0.79 (95 %); floor dirtyCells 0, 3 small chunks at the rim (v -0.004 / 1.007) under the band ends.
