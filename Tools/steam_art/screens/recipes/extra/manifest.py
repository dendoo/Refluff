import json, os
from PIL import Image

OUT_DIR = r"C:\UnityProjects\CarpetCleaning\SteamStore\_work\screens"
PRE = ("PRE: eval_file recipes/eco_setup.cs (money $1,840, POWER/AREA levels 0-2, both lamps owned, "
       "StoreShotApi.MarketingHud(true)). Every 'eval_file X.cs' = recipes/X.cs in this folder (C# statements for "
       "`unity command eval_file`). 'steps N' = UnityEditor.EditorApplication.isPaused=true; N x EditorApplication.Step(); "
       "isPaused=false. Compose: python Tools/steam_art/compose_screens.py <name>.")

shots = [
 dict(name="01_hero_reveal",
      moment="Before/after: wide wet-vac-cleaned band across a grimy rug reveals vivid navy/red/white medallions; wet vac head in the band; HUD Clean 17%",
      tool="wet vacuum (6)", rug="rug_anatolian_star", hud=True, selfScore=8,
      recipe="PRE; eval_file pick_anatolian.cs (NewCarpetNow until rug_anatolian_star); eval: Reset(14,0.9f); Zigzag(2,0.14f,0.30f,0.86f,0.50f,6,50,1f); Zigzag(4,0.14f,0.30f,0.86f,0.50f,7,60,1f); eval: Zigzag(6,0.12f,0.29f,0.88f,0.51f,9,60,1f); eval_file hero_finish.cs (Zigzag(6,...,9,70); StrokeNow(6,0.40f,0.50f,0.66f,0.50f,40); PinToolHead(6,0.66f,0.50f,true); steps 25); eval: StoreShotApi.Shot(\"01_hero_reveal\")"),
 dict(name="02_foam_brush",
      moment="Pink-violet foam field whipped into ridges by the rotary brush; blue bristles visible at the foam edge on the orange Transylvanian rug",
      tool="brush (4) after foam sprayer (3)", rug="rug_transylvanian_medallion", hud=True, selfScore=7.5,
      recipe="PRE; eval_file foam1.cs (pick rug_transylvanian; Reset(37,0.7f); Zigzag(3,0.22f,0.38f,0.78f,0.72f,5,45); 3 brush strokes; PinToolHead(4,0.55f,0.50f,true); steps 15); eval_file foam2.cs (StrokeNow(4,0.45f,0.60f,0.62f,0.40f,60); StrokeNow(4,0.62f,0.40f,0.66f,0.33f,20); PinToolHead(4,0.66f,0.33f,true); steps 10; Shot(\"02_foam_brush\"))"),
 dict(name="03_hose_spill",
      moment="Hose nozzle with spray ring soaking the Finnish rya; wet sheen on the pile and a large puddle running off onto the tiles",
      tool="hose (2)", rug="rug_finnish_rya", hud=True, selfScore=6.5,
      recipe="PRE; eval_file hose2.cs (pick rug_finnish; its Reset/strokes are superseded); eval_file hose3.cs (Reset(14,0.3f); Zigzag(2,0.55f,0.30f,0.97f,0.62f,5,35); Hold(2,0.93f,0.45f,1.5f); StrokeNow(2,0.93f,0.45f,0.78f,0.42f,25); Hold(2,0.78f,0.42f,0.5f); PinToolHead(2,0.78f,0.42f,true); steps 12; Hold(2,0.78f,0.42f,0.3f)); eval: steps 6; Hold(2,0.78f,0.42f,0.25f); Shot(\"03_hose_spill\")"),
 dict(name="04_scraper_crust",
      moment="Scraper breaking dried crust plates: dust puffs rising and crumbs along scraped lanes, green/gold European rug showing above the crust",
      tool="scraper (0)", rug="rug_european_green", hud=True, selfScore=6,
      recipe="PRE; eval_file crust1.cs (pick rug_european; its renders are tests); eval_file scrape1.cs (Reset(36,0.6f); StrokeNow(0,u,0.06f,u,0.40f,45) for u=0.30..0.65 step 0.07; PinToolHead(0,0.47f,0.40f,true); steps 12; StrokeNow(0,0.47f,0.10f,0.47f,0.40f,40)); eval_file scrape2.cs (StrokeNow(0,0.54f,0.08f,0.54f,0.36f,36); Shot(\"04_scraper_crust\")) - Shot right after the stroke so the puffs are mid-air"),
 dict(name="05_squeegee_plow",
      moment="Squeegee plowing a water film off the soaked red Salor rug; damp clean lanes behind the blade, water spilled on the tiles",
      tool="squeegee (5)", rug="rug_salor_turkmen", hud=True, selfScore=5,
      recipe="PRE; eval_file sq1.cs (pick rug_salor; strokes superseded); eval_file sq2.cs (Reset(14,0.35f); Zigzag(2,0.15f,0.25f,0.85f,0.75f,7,40); StrokeNow(5,0.12f,0.66f,1.06f,0.66f,70); StrokeNow(5,0.12f,0.52f,1.06f,0.52f,70); StrokeNow(5,0.12f,0.38f,0.62f,0.38f,40); PinToolHead(5,0.62f,0.38f,true)); eval: steps 12; Shot(\"05_squeegee_plow\")"),
 dict(name="07_dryer_finish",
      moment="Freshly extracted, vivid Anatolian rug being dried: Clean 100%, Dry 87%, hint 'Dry it', dryer footprint ring on the pile",
      tool="dryer (7)", rug="rug_anatolian_star", hud=True, selfScore=7.5,
      recipe="PRE; eval_file dry1.cs (pick rug_anatolian; Reset(14,0.12f); Zigzag(2,0.06f,0.06f,0.94f,0.94f,14,40)); dry2.cs (Zigzag(6,0.04f,0.04f,0.96f,0.96f,13,50)); dry3.cs (Zigzag(6,...,12,50); Zigzag(7,0.06f,0.08f,0.50f,0.92f,7,60)); dry4.cs (Zigzag(7,...,7,60); moisture lamp on; SetTool(7); steps 12); dry5.cs (Zigzag(7,0.05f,0.06f,0.42f,0.94f,8,110)); dry6.cs (StrokeNow(7,0.42f,0.20f,0.46f,0.50f,60); steps 12; lamp off); dry8.cs (HUD DRESSING via reflection: CarpetSimulation._initStats mud 0.22 / grit 0.10 / stain 0.06, so the job reads as a medium-soil carpet (V 0.30, par 3:32) instead of a 0.12 one whose slurry residue ratio shows Clean 33%; Economy._clockAnchor = SimTime-161.2 -> clock 2:41; steps 15; CarpetRenderer.SetCursor(uv 0.46,0.50, dir (0,1), 135 mm, ToolId 7, true) because the dryer has no head model; Shot(\"07_dryer_finish\"))"),
 dict(name="08_spotless_handover",
      moment="Finished Transylvanian rug, 100% clean + 100% dry, 'Sparkling clean - hand it over!' and the pulsing Hand over button",
      tool="dryer (7) selected, no head", rug="rug_transylvanian_medallion", hud=True, selfScore=8,
      recipe="PRE; eval_file spot1.cs (pick rug_transylvanian; Reset(14,0f); SetTool(7); steps 30); eval_file dress.cs (HUD DRESSING via reflection: _initStats mud 0.22 / grit 0.10 / stain 0.06, valid -> V 0.30, par 3:32 instead of the 'spotless' label of a level-0 carpet); eval_file spot2.cs (Economy._clockAnchor = SimTime-187.6 -> clock 3:07; steps 20; Shot(\"08_spotless_handover\")). The rug itself is the real spotless render."),
 dict(name="09_lamp_dirt",
      moment="Inspection lamp (dirt, X): scene dimmed, remaining soil patches and spill spots glow amber; rotary brush parked on a glowing spot",
      tool="brush (4) + dirt highlight", rug="rug_european_green", hud=True, selfScore=7,
      recipe="PRE; rug_european_green (the carpet 11's TakeNewCarpetNow brought in; or loop NewCarpetNow to it); EconomyDebugApi.ShopOpen(false); EconomyDebugApi.SetOwned(1,true); eval_file lamp9.cs (SetHighlight(1,false); Reset(39,0.3f); RunFrames(3); PinToolHead(4,0.29f,0.17f,true); SetHighlight(1,true); steps 30; Shot(\"09_lamp_dirt\")). Honest HUD: Clean 0% (fresh carpet scanned with the lamp)."),
 dict(name="10_payslip",
      moment="Pay-slip card after the hand-over: Medium soil 3:08, base + speed bonus + spotless + bone dry + COMBO stamp = $120, rays behind, 'Take a new carpet' button",
      tool="none (hand-over)", rug="(carpet away)", hud=True, selfScore=7,
      recipe="Continues 14: eval_file pay1.cs (steps 40 -> state Away); eval: EconomyDebugApi.PayoutSeek(6.7f); eval: Shot(\"10_payslip\"). (t 5.6 had coins covering the numbers.) Pays a real $120 into the save ($1,840 -> $1,960)."),
 dict(name="11_upgrades_shop",
      moment="Upgrades shop 'Tune your kit': POWER/AREA pips for all 10 tools, prices, lamps (moisture owned, dirt highlight $400), wallet $1,960 over the dimmed carpet",
      tool="shop (U)", rug="rug_european_green (dimmed)", hud=True, selfScore=6.5,
      recipe="Continues 10: eval_file shop1.cs (PayoutResume; TakeNewCarpetNow; steps 45); shop2.cs (steps 5; Reset(14,0.7f); Zigzag(2,0.14f,0.55f,0.86f,0.75f,5,45); Zigzag(4,...,5,50)); shop3.cs (Zigzag(6,0.12f,0.54f,0.88f,0.76f,8,60); SetTool(4); ShopOpen(true); steps 40); shop4.cs (SetOwned(1,false); steps 20; Shot(\"11_upgrades_shop\")). Restore SetOwned(1,true) after."),
 dict(name="12_rya_half_clean",
      moment="Work in progress on the Finnish rya: left half extracted to vivid blue/gold, pink foam strip being brushed on the boundary, right half still grimy, dirty runoff on the tiles",
      tool="brush (4) in foam", rug="rug_finnish_rya", hud=True, selfScore=7.5,
      recipe="PRE; eval_file fin1.cs (pick rug_finnish); fin2.cs (Reset(14,0.85f); Zigzag(2,0.08f,0.10f,0.44f,0.90f,12,30)); fin3.cs (Zigzag(4,...,14,35)); fin4.cs (Zigzag(6,0.06f,0.08f,0.46f,0.92f,10,40); Zigzag(6,0.06f,0.12f,0.46f,0.88f,10,40)); fin5.cs (Zigzag(3,0.50f,0.20f,0.64f,0.80f,6,18); 3 brush strokes ending at (0.60,0.30); PinToolHead(4,0.60f,0.30f,true); steps 15; Shot(\"12_rya_half_clean\"))"),
 dict(name="13_floor_mop",
      moment="Floor mop pad wiping a clean lane through dirty runoff beside the Salor rug (floor tool strip shows Mop)",
      tool="mop (9)", rug="rug_salor_turkmen", hud=True, selfScore=4.5,
      recipe="PRE; eval_file mop1.cs (pick rug_salor; Reset(20,0.6f); Zigzag(2,0.03f,0.25f,0.35f,0.75f,6,30); Hold(2,0.04f,0.40f,1.5f); Hold(2,0.04f,0.60f,1.5f); RunFrames(120)); eval_file mop4.cs (StrokeNow(9,-0.36f,0.95f,-0.36f,0.05f,70); StrokeNow(9,-0.16f,-0.05f,-0.16f,0.55f,60); PinToolHead(9,-0.16f,0.55f,true); steps 15; Shot(\"13_floor_mop\"))"),
 dict(name="14_handover_flight",
      moment="Hand-over: the spotless Transylvanian rug lifts off the floor in a shower of gold/white sparkles, 'Handed over to the client!'",
      tool="none (hand-over H)", rug="rug_transylvanian_medallion", hud=True, selfScore=7,
      recipe="Continues 08 (same state): eval_file ho_fly.cs (HandoverNow(false); steps 12 -> FlyOut t 0.28); eval: Shot(\"14_handover_flight\"). NOTE: ho_fly.cs's CaptureHud(\"test/..\") resolves under Assets/ - drop that line or use an absolute path."),
 dict(name="15_medallion_swath",
      moment="No-HUD beauty frame: one extracted vertical swath through the Transylvanian medallion, grimy borders either side, wet vac head at the top of the swath",
      tool="wet vacuum (6)", rug="rug_transylvanian_medallion", hud=False, selfScore=7.5,
      recipe="PRE; eval_file tr1.cs (pick rug_transylvanian; Reset(14,0.85f); Zigzag(2,0.30f,0.10f,0.70f,0.90f,13,28); Zigzag(4,...,14,32)); tr2.cs (Zigzag(6,0.29f,0.08f,0.71f,0.92f,11,35); Zigzag(6,0.29f,0.12f,0.71f,0.88f,11,35)); eval: StrokeNow(6,0.50f,0.55f,0.50f,0.86f,40) [left a glossy pressed-pile stripe]; tr4.cs (brush + 4 short cross wet-vac strokes remove it; PinToolHead(6,0.52f,0.88f,true); steps 10); eval: Shot(\"15_medallion_swath\", true); compose with --nohud"),
]

order = ["01_hero_reveal", "02_foam_brush", "08_spotless_handover", "10_payslip", "12_rya_half_clean", "09_lamp_dirt",
         "14_handover_flight", "07_dryer_finish", "03_hose_spill", "11_upgrades_shop", "04_scraper_crust",
         "05_squeegee_plow", "13_floor_mop", "15_medallion_swath"]
byname = {s["name"]: s for s in shots}
out = []
for i, n in enumerate(order):
    s = byname[n]
    s["file"] = "SteamStore/_work/screens/%s.png" % n
    s["suggestedOrder"] = i + 1
    s["preamble"] = PRE
    im = Image.open(os.path.join(OUT_DIR, n + ".png"))
    assert im.size == (1920, 1080), (n, im.size)
    out.append(s)
with open(os.path.join(OUT_DIR, "manifest.json"), "w", encoding="utf-8") as f:
    json.dump(out, f, indent=1, ensure_ascii=False)
print(len(out), "entries ok")
