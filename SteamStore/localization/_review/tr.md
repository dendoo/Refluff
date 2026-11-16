# tr (Turkish) store page review

Verdict: OK after fixes

## Changes
- sysreq_min_osversion: `Windows 10 64 bit (sürüm 1909 veya üzeri)` -> `Windows 10 64-bit (sürüm 1909 veya üzeri)`
- sysreq_min_notes: `64 bit işlemci ve işletim sistemi gerektirir.` -> `64-bit işlemci ve işletim sistemi gerektirir.`
- sysreq_rec_osversion: `Windows 11 64 bit` -> `Windows 11 64-bit`
  (system requirements, all three: Steam's own Turkish string is `64-bit işlemci ve işletim sistemi gerektirir`, verified on a live Turkish store
  page, and it is printed in the same block. Both spellings exist in Turkish, but the page should not show both.)
- reveal_body: `…desen de geçiş geçiş geri gelir. Her geçiş ayrı bir keyif.` -> `…desen de santim santim geri gelir. Her geçiş ayrı bir keyif.`
  (fluency: "geçiş geçiş" is a calque of "pass by pass". The reduplication works for concrete nouns (satır satır, adım adım) but reads oddly here.
  "santim santim" (inch by inch) is the idiom for a gradual reveal, and "pass" stays in the next sentence.)

`python Tools/steam_art/store_loc.py check tr`: 0 errors.

## Checked and kept
- POWER / AREA: `istediğin aletin GÜÇ ve ALAN yükseltmelerine` is correct. The labels are bare modifiers in a noun compound, and the case ending sits on
  "yükseltmelerine", so no suffix touches the capitals and no apostrophe is needed.
- Units and numbers match Steam's Turkish pages: GB with a space (`Bellek: 12 GB RAM`, `Depolama: 60 GB kullanılabilir alan`), `sürüm 1909 veya üzeri`
  (Steam prints `DirectX: Sürüm 12`), `60+ fps`, and `1440p'de` with the apostrophe and correct vowel harmony ("pe" -> -de). `$120` is kept as the brief requires.
- Terminology matches the glossary: Kazıyıcı, Fırça, Çekpas, Islak süpürge, Deterjan, Paspas, hız bonusu / tertemiz / kupkuru / kombo, Ekipmanını
  geliştir (hdr_paid), Teslim et, inceleme lambaları for the İNCELEME items, informal "sen". `ödeme fişi` is the right choice over `maaş bordrosu`.

## Doubts (not changed)
- alt_clip5_paid `bir KOMBO damgası ve $120 toplamla`: "120 dolarlık toplamla" would be more idiomatic, but a suffix after "$120" is awkward because it is
  read as "dolar". The text is alt text only, and it is understandable as written.
- `köpükle` as an imperative (short description, tools_body) is understood from the chain of verbs. "köpürt" is the more standard verb, but the
  choice is not wrong.

## Meaning audit
no meaning errors

Back-translations checked against the audit facts (literal):
- tools_lead: "Ten tools, a single right order."
- tools_body: "Break the dried crust with the scraper, foam the dirt, work the foam well into the rug with the rotary brush, then push the slurry out off the rug with the squeegee like a snow shovel." (the FOAM is scrubbed in)
- water_body: "... Here the dirt is not a picture that you wipe over: under every fibre a fluid simulation is running."
- rugs_body: "The moment you hand over the clean rug, the next one comes flying. Every job brings a random mix of dust, dirt, crust and stains onto a real museum rug."
- headphones_body: "Every machine sound was recorded from a real machine: ..." (machines only)
- bullet_nofail: "No enemies, no defeat, no time limit: the speed bonus only makes you earn." (adds money; does not say the bonus grows)
