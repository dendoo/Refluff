# hu (Hungarian) store page review

Verdict: OK after fixes

## Changes
- sysreq_min_notes: `64 bites processzort és operációs rendszert igényel.` -> `64 bites processzor és operációs rendszer szükséges.`
  (system requirements: this is Steam's own Hungarian string for the 64-bit line, verified on a live Hungarian store page, so the note now reads
  exactly like the line Steam prints.)
- rugs_body: `…kéregből és foltokból, egy valódi múzeumi szőnyegen.` -> `…kéregből és foltokból – mindezt egy valódi múzeumi szőnyegen.`
  (fluency: the locative tacked on after a comma dangled; "mindezt" (all of it) ties the dirt mix to the museum carpet under it.
  185 of 190 characters.)

`python Tools/steam_art/store_loc.py check hu`: 0 errors.

## Checked and kept
- short_description (exactly 300 characters) is not too dense. It has the same shape as the English: one short identity sentence,
  one chain of six short imperatives (Kapard, áztasd, habosítsd, sikáld, húzd, szárítsd), then a four-beat closer. Every link is two or
  three words, and the verb-first imperatives give it the intended rhythm. It reads cleanly aloud. The only risk is that it has no margin: if
  anything is ever added, drop the anglicism "egy" in "A Refluff egy ASMR…" first (-4 characters, no loss of meaning).
- POWER / AREA: `ERŐ- és TERÜLET-fejlesztésekre` is the correct Hungarian way to attach a suffix to a capitalised label: a hyphenated compound with
  the suspended hyphen. Suffixing the capitals directly (ERŐre) would be wrong.
- Terminology matches the glossary: Kaparó, Kefe (forgókefe), Lehúzó, Vízszívó, Tisztítószer, Felmosó, gyorsasági bónusz / makulátlan / csontszáraz /
  kombó, Átadás (Add át), informal "te". The translator correctly avoided the verb "kefél" (vulgar slang).
- `DirectX 12-es`, `1909-es`, `compute shader-támogatással`, `DirectX-kompatibilis` are spelled correctly. GB is what Steam writes in Hungarian
  (`Memória: 12 GB RAM`).

## Doubts (not changed)
- paid_body `ellenőrzőlámpákra`: in Hungarian, "ellenőrző lámpa" also means a dashboard warning light. The relative clause ("amelyek felfedik …
  a nedvességet és koszt") and the shop caption ELLENŐRZÉS make the meaning clear, so I kept it. A human native may prefer naming the two items
  (nedvességjelző, koszjelző).

## Meaning audit
no meaning errors

Back-translations checked against the audit facts (literal):
- tools_lead: "Ten tools, one right order."
- tools_body: "Break up the dried-on crust with the scraper, spray foam on the dirt, work it in with the rotary brush, then push the dirty liquid off the rug with the squeegee, like a snow plough." ("dolgozd bele" = work it (the foam) in; nothing about grime into the fibres)
- water_body: "... The dirt here is not a picture that you simply wipe off: under every fibre a fluid simulation runs."
- paid_body: "On the pay slip the items appear one after another: speed bonus, spotless, bone dry, combo. ..." ("sorra megjelennek" = appear in turn; keeps the line-by-line idea)
- headphones_body: "Every machine's sound is a recording of a real machine: ..." (machines only)
- bullet_nofail: "No enemies, no defeat, the time does not run out: the speed bonus only gives a plus."
