# cs (Czech) store page review

Verdict: OK after fixes

## Changes
- tools_body: `…, špínu zalij pěnou, rotačním kartáčem ji vetři do vláken…` -> `…, na špínu nastříkej pěnu, rotačním kartáčem ji vetři do vláken…`
  (meaning error: "špínu" was the object of the previous clause, so "ji" read as "rub the GRIME into the fibres", the opposite of cleaning. With "pěnu"
  as the object, "ji" is the foam. "nastříkej" (spray) also fits the Napěňovač better than "zalij" (pour / flood).)
- reveal_body: `…a vzor se vrací. Tah za tahem, každý z nich je čistá radost.` -> `…a vzor se vrací tah za tahem. Každý z nich je čistá radost.`
  (fluency: the fragment "Tah za tahem," followed by a comma-spliced clause read like a calque. "Pass by pass" now belongs to the pattern coming
  back, as in the English.)
- paid_body: `…u kteréhokoli nástroje, nebo za kontrolní lampy…` -> `…u kteréhokoli nástroje nebo za kontrolní lampy…`
  (punctuation: Czech writes no comma before "nebo" that joins two alternatives within one clause.)

`python Tools/steam_art/store_loc.py check cs`: 0 errors.

## Checked and kept
- POWER / AREA: `vylepšení VÝKON a ZÁBĚR` keeps the game's labels uninflected, in apposition. This is correct and matches the cards.
- Terminology matches the glossary: Škrabka, Kartáč, Stěrka, Mokrý vysavač, Saponát, Mop, bonus za rychlost / čisťoučký / suchoučký / kombo,
  Vylaď si výbavu, Předat koberec (predej), kontrolní lampy for the KONTROLA items, informal "ty".
- System requirements: `Vyžaduje 64bitový procesor a operační systém` is Steam's own Czech string, verified on a live Czech store page.
  64bitový without a hyphen is the Czech norm, and GB is what Steam writes in Czech.

## Doubts (not changed)
- hdr_water `Pravá voda, pravé bláto`: "pravý" = genuine (as in pravé zlato), so it is correct. `Skutečná voda, skutečné bláto` (29) is one character over the
  limit, so the translator's choice stays.

## Meaning audit
- tools_body: `na špínu nastříkej pěnu, rotačním kartáčem ji vetři do vláken` -> `napěň špínu, rotačním kartáčem pěnu vetři do vláken` (both "špína" and "pěna" are feminine, so "ji vetři do vláken" could be read as "rub the grime into the fibres", the exact misreading the audit guards against; the foam is now named)

Back-translations checked against the audit facts (literal):
- tools_lead: "Ten tools, one right order."
- tools_body (new): "Break the dried crust with the scraper, foam the grime, rub the foam into the fibres with the rotary brush and then push the slurry off the carpet with the squeegee like a plough."
- water_body: "... The dirt here isn't a picture you just wipe off: under every fibre runs a fluid simulation."
- headphones_body: "Every machine is a recording of a real device: ..." (machines only)
- bullet_nofail: "No enemies, no defeat, no countdown: the speed bonus only adds."
