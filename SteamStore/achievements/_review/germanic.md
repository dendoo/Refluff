# Achievements review: de, nl, da, no, sv, fi (editor pass, 2026-09-30)

I checked all 60 entries against source_en.json (conditions, terms) and the game's Loc/<code>.json. All game terms are reused verbatim: Hand over, Spotless, Bone dry, Filthy, Heavy soil, Speed bonus, POWER, AREA, MAX, the two highlight names and the tool word. Address is informal throughout, as in each game UI. The percent format copies each language's '{0}% clean': a plain space in de/da/no/sv/fi, no space in nl.
`python Tools/Steam/achievements.py check`: 0 errors and no warnings in these six files. Longest name is 25 characters and longest description is 73.

## Changes (one line each)
- de ACH_BONE_DRY desc: added "dann" ("... und gib ihn dann 100 % trocken ab") so that the order "soak first, then hand over dry" is explicit, as in the English "then".
- nl ACH_EXPRESS desc: "met een zwaar vervuild tapijt" -> "bij een ...". The bonus is earned on that carpet, and "bij" is the natural preposition here.
- no ACH_BONE_DRY desc: "lever det så 100 % tørt" -> "lever det deretter 100 % tørt". "så" in front of a degree phrase can briefly read as "so (100 % dry)". "deretter" is unambiguous and matches da "bagefter".
- sv ACH_MAXED_OUT desc: "på ett verktyg" -> "på samma verktyg". Swedish "ett" is both "a" and "one", so this makes the SAME-tool condition unambiguous. (no "ett" and da "ét" already mean "one".)
- fi ACH_SPOTLESS / ACH_COMBO / ACH_FILTHY desc: "Luovuta likainen matto, kun puhtaus on 100 %" read as "hand over a dirty carpet while it is 100 % clean", which is self-contradictory in Finnish. They are now "Puhdista likainen/törkyinen matto ja luovuta se, kun puhtaus [ja kuivuus ovat | on] 100 %". This matches ACH_BONE_DRY's "Kastele ... ja luovuta se, kun ...". The HUD wording is kept, and COMBO drops the redundant "molemmat".

## Kept after review (translator's open questions)
- nl ACH_FIRST_CARPET "Donzig debuut": kept. "donzig" (downy, as in donzige handdoeken) is positive and the alliteration reads well as a playful title. "Zacht begin" would be flatter.
- nl ACH_FULL_KIT "gereedschappen": kept. The plural is standard (Van Dale) and consistent with the game's GEREEDSCHAP.
- da ACH_COMBO "Som nyt fra butikken": kept. The neuter matches the game's own neuter titles (Pletfrit, Knastørt), which agree with tæppe.
- de ACH_INSPECTOR "Qualitätskontrolle", de "Blitzservice", sv "Snabbservice", fi "Pikapesu" / "Tapissa", and the idioms for ACH_FULL_KIT (Alle Register gezogen / Alles uit de kast / Hele molevitten / Hele sulamitten / Hela baletten / Koko kalusto käytössä): kept. All are natural and match the tone.
- ACH_INSPECTOR item names as capitalised bare names (da/no/sv/fi): kept. This is the usual convention for shop-item names in Nordic game text and keeps the game's exact forms.

## Open for the orchestrator
- The game's bare '{0}%' HUD string uses a NO-BREAK SPACE (U+00A0) in de/da/no/sv/fi, but '{0}% clean' uses a plain space. The files follow the brief ('{0}% clean'). If Steam wraps "100 / %" across lines in the pop-up, switch every "100 %" to "100 %" (also fr etc.).
