# Meaning audit: the risky sentences, across languages

The translations were written by one agent and edited by a second one per language. A third pass found the same
meaning error in four languages at once (the speed-bonus bullet had become "the bonus only goes up"). This audit
looks for that kind of error: a fluent sentence that says something the English does not.

For each language you are given: back-translate the fields below into plain literal English, compare with the facts,
and fix only real meaning errors (wrong fact, lost key idea, over-claim, ambiguity a player would misread). Do not
touch style. Keep `python Tools/steam_art/store_loc.py check <code>` at 0 errors.

## Facts per field (what the text must and must not say)

| Field | Must convey | Must NOT say |
|---|---|---|
| short_description | Refluff; carpet cleaning; ASMR; built on a real fluid simulation; the steps scrape / soak / foam / brush / squeegee / dry until spotless; hand it over, get paid, upgrade the kit, take the next rug | anything not in the English (no "relaxing forever", no "physics of everything", no multiplayer) |
| tools_lead | ten tools, and there is one right (sensible) order to use them in | that the order is forced or that there are other counts of tools |
| tools_body | scraper chips dried crust; foam on the grime; the rotary brush scrubs the foam in; the squeegee pushes the slurry off the rug (onto the floor) | that the brush scrubs "the grime into the fibres"; that the squeegee sucks or dries |
| reveal_body | the wet vacuum pulls the murky slurry out of the pile; the pattern comes back, pass by pass, satisfyingly | that the vacuum paints / restores the pattern by itself in one go |
| water_body | water soaks in and spreads (wicks) through the pile; dry dust turns to mud when wet; foam heaps up and slowly settles; the dirt is NOT a picture that simply gets wiped away (as in other cleaning games), a fluid simulation runs under / throughout the fibres | a face mask; "nothing is erased" (cleaning does remove dirt); that every fibre is individually simulated as a claim of physics accuracy beyond "a fluid simulation runs under every fibre" |
| rugs_body | hand the clean rug over, the next one flies in; every job has its own random mix of dust, grime, crust and spills; the rug (pattern) is a real historic museum carpet | "royal carpet"; that the player visits a museum; that the mix is chosen by the player |
| paid_body | the pay slip prints line by line with speed bonus, spotless, bone dry, combo; the money buys POWER and AREA upgrades for any tool, or inspection lamps that reveal moisture and dirt the eye misses | that the lamps clean or dry anything; that upgrades are per carpet |
| headphones_body | every MACHINE sound is a recording of a real machine; motors speed up and wind down; the tube rattles exactly when it swallows a crumb; water hisses, foam fizzes, bristles drag | that ALL sounds (UI, music) are real recordings |
| bullet_tools | 10 tools: 8 for the carpet + a detergent sprayer and a mop for the floor (tiles) | 10 carpet tools, or 8 in total |
| bullet_nofail | no enemies; you cannot fail; no countdown that ends the job; the speed bonus is only ever an ADDITION to the pay (being slow never costs money; the bonus itself shrinks with time but never goes below zero) | "the bonus only grows / never decreases"; "there is no timer at all" (there is a job clock with a par time) |
| bullet_input | single player; mouse and keyboard; interface in 31 languages | controller support |
| hdr_* (4 headers) | reveal: watch the rug's pattern come back; water: the water and mud are real (simulated); rugs: every job brings a new rug; paid: get paid and upgrade your equipment | — |
| alt_clip5_paid | after a hand-over the pay slip prints line by line with a combo stamp and a $120 total, then the upgrade shop opens and two upgrades are bought | — |

## Output
- Fix in `SteamStore/localization/<code>.json` (only meaning errors), then run the check.
- Append to `SteamStore/localization/_review/<code>.md` a section `## Meaning audit` with either `no meaning errors`
  or each change as `field: old -> new (what was wrong)`. Also list the back-translation of any sentence you were
  unsure about.
- Do not edit any other file.

Return (short): per language `clean` or the list of fixes (one line each).
