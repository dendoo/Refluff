# Achievement translations: editor review (fr, it, es, es-419, pt-BR, pt-PT, ro)

Checked on 2026-09-30 against source_en.json (meaning, conditions, notes), the game's Loc/<code>.json terms,
the store page register, length and consistency. `python Tools/Steam/achievements.py check`: 0 errors,
1 warning (icons/ folder missing, not a translation issue).

## Changes
- fr ACH_EXPRESS desc: "Touchez la prime de rapidité complète" -> "Encaissez toute la prime de rapidité". In "la prime de rapidité complète" both nouns are feminine, so "complète" could be read as "complete speed". "Encaissez" echoes the store page ("Encaissez, équipez-vous").
- it ACH_MAXED_OUT name: "A tutta potenza" -> "Tutto al massimo". POTENZA is the name of one of the two tracks, so the old title pointed at POWER alone. The achievement needs POWER and AREA, and "tutto al massimo" covers both.

## Checked, no change
- Register matches the game UI: fr vous (Livrez), pt-BR você with 3rd-person imperatives (Entregue), and it / es / es-419 / pt-PT / ro tu.
- Every listed term reuses the game's wording. Hand over: Livrer, Consegna, Entrega, Entregue / Entrega, Predă. Spotless and Bone dry titles are the pay-slip words. Filthy and Heavy soil are lowercased in running text. Speed bonus = prime de rapidité / bonus velocità / bonus por rapidez / bono por rapidez / bônus / bónus de rapidez / bonus de viteză. POWER / AREA / MAX are uppercase, including es / pt MÁX. with its period. Shop item names are exact.
- Conditions are intact: SPOTLESS / COMBO say "arrived dirty"; BONE_DRY is soak first, then 100% dry; EXPRESS is the full speed bonus on a heavy-soil-class carpet; MAXED_OUT is both tracks of one and the same tool (fr "d’un même outil", it "di uno stesso attrezzo", es / pt "de una/uma mesma", ro "o unealtă ... atât la PUTERE, cât și la ARIE"); FULL_KIT keeps 10 and REGULARS keeps 25 as digits.
- Percent format follows each game file: fr "100 %" with U+00A0, and all the others "100%" with no space. Romanian ș / ț are comma-below (U+0219 / U+021B). No BOM, LF.
- Kept the translator's choices, which read naturally: fr "Toute la panoplie", "Gonflé à bloc"; it "Servizio espresso" (coffee pun works), "Da lurido a morbido"; es "De escaparate", "Nada se queda en la furgoneta"; es-419 "Todo el arsenal", "Digna de vitrina", "Servicio exprés" (RAE spelling, understood everywhere); pt-BR "Freguesia fiel", "Não sobrou nada na van"; pt-PT "Clientela fiel", "A todo o gás"; ro "Ochi de vultur", "Clienți de-ai casei", "Udă leoarcă".
- es-419 "mullida" is standard and widely understood ("almohada mullida"). I kept it for the alliteration with "mugrienta".
- ro ACH_INSPECTOR keeps the exact shop names ("Evidențierea umezelii / murdăriei"). They are stiff but grammatical, and the terms rule requires them.
