# Review brief: Steam store page translations of "Refluff: Carpet Cleaning ASMR"

You are the second pair of eyes: a native-speaker editor of the target locale who reviews store pages for a games
publisher. Another translator wrote these texts; you did not. The owner's one requirement is that every translation
is adequate: faithful to the English, natural to a native gamer, correct, consistent with the game. Your job is to
find and fix what falls short, not to restyle what is already good.

## Inputs
- `SteamStore/localization/_brief/BRIEF.md`: the translator's brief (rules, limits, typography). Read it.
- `SteamStore/localization/source_en.json`: the English source with `where` / `note` / `max` per field.
- `SteamStore/localization/_brief/glossary_<code>.json`: the game's own UI terms in that language (already shipped).
- `SteamStore/localization/<code>.json`: the translation under review.
- `python Tools/steam_art/store_loc.py show <code>` prints the assembled About This Game text as Steam will get it.

## Check every field for
1. Accuracy: back-translate each sentence. Anything lost, added, distorted, or over-promised? Numbers and names kept?
   (The notes in source_en.json explain every idiom; a sentence that "sounds fine" but says something else is an error.)
2. Fluency: would a native gamer notice it is a translation? Calques, English word order, unnatural collocations,
   wrong register, clumsy rhythm, overlong sentences. The short description and the four headers matter most.
3. Terminology: tool names, pay-slip lines (speed bonus, spotless, bone dry, combo), POWER / AREA, Hand over, the
   inspection lamps: same words as the glossary; same form of address as the game.
4. Correctness: grammar, agreement, cases, spelling, diacritics, typos, punctuation and typography of the locale
   (quotes, French no-break spaces, Spanish ¡¿, full-width CJK punctuation, Thai spacing, Greek accents).
5. Locale: es vs es-419, pt-PT vs pt-BR, zh-Hant (Taiwan) vs zh-Hans (mainland), nb Norwegian, ms vs id are each
   written for their own audience.
6. System requirements: phrased the way Steam pages in that language usually phrase them.

## Do
- Fix real problems directly in `SteamStore/localization/<code>.json` (keep the file format; plain text fields;
  no ASCII double quotes). Prefer the smallest change that makes the text right. Do not rewrite for personal taste.
- Run `python Tools/steam_art/store_loc.py check <code>` afterwards: 0 errors.
- Write `SteamStore/localization/_review/<code>.md`: a one-line verdict (`OK as written` / `OK after fixes` /
  `needs a human native check`), then every change as `field: old -> new (reason, in English)`, then any doubts you
  could not resolve. Write it as soon as that language is done.
- Do not edit any other file. Do not run Unity or other scripts.

## Return (short)
Per language: verdict, number of changes, the one or two most important fixes.
