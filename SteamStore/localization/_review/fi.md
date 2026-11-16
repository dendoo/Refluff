# fi (Finnish) store page review

Verdict: OK after fixes

## Changes
- sysreq_min_graphics: `…4 Gt VRAM-muistia (NVIDIA GeForce GTX 1060 6 Gt / AMD Radeon RX 580 8 Gt)` -> `…4 GB VRAM-muistia (NVIDIA GeForce GTX 1060 6 GB / AMD Radeon RX 580 8 GB)`
  (locale: "Gt" is the Kielitoimisto / tech-press abbreviation (io-tech, Muropaketti), but Steam's own Finnish page prints GB in the lines it
  generates, verified on a live Finnish store page: `Muisti: 12 GB RAM`, `Tallennus: 60 GB kiintolevytilaa`. Finnish retailers also write GB.
  With Gt, our graphics line would sit between two Steam lines that say GB. The translator's note now records the change.)
- bullet_nofail: `…: nopeusbonus tuo aina vain lisää` -> `…: nopeusbonus on pelkkää plussaa`
  (meaning: "aina vain lisää" is the idiom "more and more", so the line read "the speed bonus keeps bringing more". It lost the point that the
  bonus can only add and never takes money away. "pelkkää plussaa" (nothing but a plus) says exactly that, colloquially.)

`python Tools/steam_art/store_loc.py check fi`: 0 errors.

## Checked and kept
- POWER / AREA: `TEHO- ja ALUE-päivityksiin` is the correct Finnish compound for a capitalised label: a hyphen compound with the shared hyphen,
  and the case ending on the head word. The labels stay intact.
- Terminology matches the glossary: Kaavin, Harja, Vesilasta, Märkäimuri, Pesuaine, Moppi, nopeusbonus / tahraton / rutikuiva / kombo, Viritä
  välineesi (hdr_paid), Luovuta matto, tarkastuslamput for the TARKASTUS items, informal "sinä".
- `DirectX 12 -näytönohjain`, `compute shader -tuella`: the space before the hyphen is correct for a multi-word first part.
  `Vaatii 64-bittisen suorittimen ja käyttöjärjestelmän` is Steam's own Finnish string.
- The short description (289), tools_lead (40 of 40) and the headers are natural. hdr_reveal `Kuvio palaa esiin` is shorter than the English,
  and good.

## Doubts (not changed)
- headphones_body `moottorit kiihtyvät käyntiin ja hiljenevät`: "kiihtyä käyntiin" is a slightly unusual collocation ("hurahtavat käyntiin"
  is more idiomatic). The meaning is clear, so it stays.

## Meaning audit
no meaning errors

Back-translations checked against the audit facts (literal):
- tools_lead: "Ten tools, one right order."
- tools_body: "Break the dried crust with the scraper, foam the dirt, rub the foam deep into the pile with the rotating brush and push the dirty water off the rug with the squeegee like a snow plough." (the FOAM is scrubbed in)
- water_body: "... The dirt is not a picture that you just wipe away: under every fibre runs a fluid simulation."
- rugs_body: "... Every gig draws its own mix of dust, dirt, crust and stains on top of a real museum rug."
- headphones_body: "Every machine is recorded from a real device: ..." (machines only)
- bullet_nofail: "No enemies, no losing, no time limit: the speed bonus is pure plus."
