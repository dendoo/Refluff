# pl (Polish) store page review

Verdict: OK as written

## Changes
None. `python Tools/steam_art/store_loc.py check pl`: 0 errors (short description 297 characters).

## Checked and kept
- tools_lead `Dziesięć narzędzi, każde w swoim czasie.` The meaning holds. It echoes the proverb "wszystko w swoim czasie"
  (there is a time for everything), so it says each tool has its proper moment, which amounts to a sensible order. The body
  right after it then lays out the sequence and ends on "a na koniec" (and finally). What is lost is the punchy ten / one
  contrast and the explicit "one right order". The literal `Dziesięć narzędzi, jedna właściwa kolejność.` is 45 characters (limit 40);
  `10 narzędzi, jedna właściwa kolejność.` (38) fits but opens a bold lead with a digit and reads flatter. The idiom is better Polish copy.
- POWER / AREA in paid_body: `ulepszenia MOC i ZASIĘG dowolnego narzędzia` keeps the labels uninflected, in apposition, the usual way Polish
  cites a UI label ("ulepszenia typu MOC"). It is grammatical and matches the game's cards exactly. Declining them (MOCY, ZASIĘGU) would
  also be correct, but the capitals would then no longer match the labels on the cards.
- Terminology matches the glossary: Skrobak, Szczotka, Ściągaczka, Mokry odkurzacz, Płyn myjący, Mop, premia za szybkość / bez skazy /
  suchy jak wiór / combo, Podrasuj sprzęt (hdr_paid), Oddaj dywan (the hand-over verb), informal "ty" throughout. `lampy inspekcyjne` matches the
  INSPEKCJA caption, and it is a real product term.
- System requirements: `Wymaga 64-bitowego procesora i systemu operacyjnego` is Steam's own Polish string, verified on a live Polish store page.
  GB is written the way Steam writes it in Polish (`Pamięć: 12 GB RAM`).

## Doubts (not changed)
- paid_body `wilgoć i brud umykające twoim oczom` is slightly literary ("umykające twojej uwadze" or "niewidoczne gołym okiem" are more
  everyday). It is correct Polish, so I left it.
- hdr_water `Prawdziwa woda i błoto`: the adjective grammatically attaches only to "woda", but Polish headline style reads it as covering both
  nouns. The doubled form does not fit in 28 characters.

## Meaning audit
no meaning errors

Back-translations checked against the audit facts (literal):
- tools_lead: "Ten tools, each in its own time." (idiom "wszystko w swoim czasie": each tool has its moment, i.e. a sensible order; not forced; kept)
- tools_body: "Crush the dried crust with the scraper, cover the dirt with foam, rub it (ją = the foam, feminine; brud is masculine, so no ambiguity) in with the rotary brush, and finally push the slurry off the rug with the squeegee like a plough."
- water_body: "... Dirt here is not a picture that simply gets rubbed off - under every fibre a fluid simulation runs."
- rugs_body: "... Every job is a random mix of dust, dirt, crust and stains, and under it a real carpet from a museum collection."
- headphones_body: "Every machine is a recording of a real device: ..." (machines only)
- bullet_nofail: "No enemies, no losing, no time limit: the speed bonus only adds." ("no time limit" = nothing ends the job; does not claim there is no clock at all)
