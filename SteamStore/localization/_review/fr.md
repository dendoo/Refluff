# fr (French) review

Verdict: OK after fixes

Faithful, idiomatic copy in the game's vouvoiement (Grattez, Livrez, Dépensez). Glossary terms are used consistently in
running-text case (grattoir, brosse rotative, raclette, extracteur, détergent, serpillière, Livrer le tapis -> Livrez,
PUISSANCE / SURFACE, prime de rapidité / impeccable / sec à cœur / combo, détecteurs d’humidité et de saleté). French
typography is correct: U+00A0 before every : and ;, curly apostrophes throughout, no ASCII quotes. The short description
keeps the verb rhythm and ends on a natural idiom (et au suivant). Headers: Le motif refait surface, Vraie eau, vraie
boue, À chaque client son tapis, Encaissez, équipez-vous. System requirements use Steam's French wording (Nécessite un
processeur et un système d’exploitation 64 bits, 64 bits, ou ultérieure, Go).

## Changes
- hdr_paid: Payé, puis mieux outillé -> Encaissez, équipez-vous
  (fluency, the flagged compromise: two bare past participles with no subject read like a stiff caption, outillé is the
  rarer, flatter word, and the header breaks the vous imperatives of the rest of the page. Encaissez, équipez-vous is
  the natural French tagline for "get paid, gear up": two imperatives like the English, encaissez echoes the short
  description, and above the clip of the upgrade shop équipez-vous clearly means spending on your equipment.
  23 characters. The hdr_paid header image must be re-rendered.)

## Doubts (left as written)
- tools_lead: Dix outils, dans le bon ordre drops the "one" of "one right order" (Dix outils, un seul bon ordre); the
  meaning, that there is a right order, is kept and the line reads naturally, so kept.
- tools_body: faites-la pénétrer follows noyez la crasse sous la mousse; la could grammatically point to la crasse, but
  every reader resolves it to la mousse (scrubbing grime in would make no sense), so kept.
- headphones_h2: Casque sur les oreilles is natural as a heading; Mettez votre casque would be a more direct invitation
  but it is a taste call.
- alt_clip5_paid: $120 kept as written per the brief (French would normally write 120 $); alt text only.

## Meaning audit

- tools_body: `noyez la crasse sous la mousse, faites-la pénétrer à la brosse rotative` -> `étalez la mousse sur la crasse, faites-la pénétrer à la brosse rotative`
  (the pronoun la most naturally takes the direct object la crasse, so the line read "drown the grime in foam, work the
  grime in with the rotary brush", i.e. the brush drives the dirt into the fibres; with la mousse as the direct object
  the brush now clearly works the foam in.)

Back-translations checked (all others match the facts):
- tools_lead: "Ten tools, in the right order." (a sensible order, not a forced one: OK)
- bullet_nofail: "Neither enemies, nor failure, nor countdown: the speed bonus can only earn you money." (compte à rebours =
  a timer that runs out, not "no clock at all"; the bonus is an addition: OK)
- hdr_rugs: "To each client his own rug." (every job brings a new rug: OK)
- headphones_body: "Every machine was recorded for real: motors rev up then slow down, the vacuum tube rattles as soon as it
  swallows a crumb, water hisses, foam fizzes, bristles rub." (machines only: OK)
