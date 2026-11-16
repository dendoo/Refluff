# el (Greek) review

Verdict: OK after fixes

Good, idiomatic Greek. It uses the singular «εσύ» as the game does. A script scan found no mixed Latin/Greek letters
and no missing or medial final sigma. Accents follow monotonic rules: one accent per word, the correct second accent
before enclitics (Παράδωσέ το, δούλεψέ τον), and the capitals ΙΣΧΥΣ / ΕΥΡΟΣ without accents. The glossary terms are
exact: ξύστρα, βούρτσα, ράκλα, σκούπα υγρών, απορρυπαντικό, σφουγγαρίστρα, the Παράδοση verb, and the pay-slip
lines in «» (μπόνους ταχύτητας, πεντακάθαρο, κατάστεγνο, combo in Latin as the game writes it).

The short description reads like real Greek store copy:
- The chain of imperatives is natural (Ξύσε, μούσκεψε, βάλε αφρό, βούρτσισε, τράβηξε, στέγνωσε).
- «ώσπου το χαλί να γίνει πεντακάθαρο» echoes the Spotless line.
- «Παράδωσέ το, πληρώσου, αναβάθμισε τον εξοπλισμό σου και πιάσε το επόμενο» has the right rhythm.

The system-requirement lines use the usual Greek Steam wording (Απαιτεί επεξεργαστή και λειτουργικό σύστημα 64-bit,
έκδοση 1909 ή νεότερη, compute shaders kept in English).

Stat names: «σε αναβαθμίσεις κάθε εργαλείου — ΙΣΧΥΣ και ΕΥΡΟΣ —» uses a dash apposition. Declining them (ΙΣΧΥ,
ΙΣΧΥΟΣ) would change the game's labels, so the apposition is the right choice. Left as written.

## Changes
- hdr_water: Αληθινό νερό και λάσπη -> Αληθινό νερό, αληθινή λάσπη
  (accuracy and fluency: the parallel form is 27 characters, within the 28 limit; the translator's note miscounted it
  as 29. In the old header «αληθινό» agreed only with νερό, so the mud lost its "real".)
- reveal_body: το σχέδιο επιστρέφει, πέρασμα με πέρασμα, -> το σχέδιο ξαναφαίνεται με κάθε πέρασμα,
  (fluency: «επιστρέφει» plus «πέρασμα με πέρασμα» is a word-for-word calque of "comes back, one pass at a time". A
  pattern «ξαναφαίνεται» (reappears) from under the dirt.)
- water_body: δεν είναι ζωγραφιά πάνω στο χαλί που απλώς σβήνεις -> δεν είναι ζωγραφιά που απλώς σβήνεις από το χαλί
  (clarity: «που απλώς σβήνεις» attached to «το χαλί», so it read as "the rug that you simply erase")
- rugs_body: χυσίματα -> λεκέδες
  (register: «χυσίματα» carries a well-known vulgar slang reading that gamers would snigger at. Spills on a rug are
  «λεκέδες».)
- headphones_body: Κάθε μηχάνημα είναι ηχογράφηση αληθινού -> Ο ήχος κάθε μηχανήματος είναι ηχογράφηση αληθινού
  (fluency: in Greek a machine cannot "be a recording". The sound is the recording. 227 characters, within 230.)

## Doubts (left as written)
- translator_notes still gives the old hdr_water reasoning (29 characters). The notes are not published, so they
  were left untouched.
- short_description: «είναι ASMR προσομοιωτής» without «ένας» is correct predicate usage. «ένας ASMR
  προσομοιωτής» would be slightly more conversational, but this is a matter of taste.
- headphones_body: «ο αφρός τσιτσιρίζει» (sizzles) for "foam fizzes" is vivid and acceptable. «σκάει» (pops) is
  the alternative.
- alt_clip1_reveal: «ρύγχος» for the nozzle is technical but correct. «στόμιο» is more everyday. This is alt text only.

## Meaning audit
2026-09-28, back-translation of every listed field against AUDIT.md: no meaning errors.

Checked closely and kept:
- tools_body «βάλε αφρό στη βρομιά, δούλεψέ τον με την περιστροφική βούρτσα»: «τον» is masculine, so it can only be
  «αφρό» (βρομιά is feminine): the brush works the foam in, as in the English.
- water_body «η βρομιά δεν είναι ζωγραφιά που απλώς σβήνεις από το χαλί» = "the grime is not a drawing that you simply
  erase from the rug" (the not-a-picture idea; no face mask).
- rugs_body «πάνω σε ένα αληθινό χαλί από μουσείο» = "over a real rug from a museum" (same claim as "a real museum
  carpet"; no museum visit).
- hdr_paid «Αμοιβή και αναβαθμίσεις» = "pay and upgrades" (nominal header, both halves kept).
