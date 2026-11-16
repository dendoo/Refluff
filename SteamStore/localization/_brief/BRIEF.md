# Brief: translate the Steam store page of "Refluff: Carpet Cleaning ASMR"

You are a senior games-marketing localizer, writing as a native speaker of the target language. The owner's
requirement is that every translation reads as if it was written in that language by a person who plays games:
natural, idiomatic, correct, never machine-like. A literal translation that sounds foreign is a failure even if
every word is "right".

## Read first
- `SteamStore/localization/source_en.json`: the game summary (`game`) and every field to translate, each with
  `where` (where it appears on the page), `note` (meaning, pitfalls) and `max` (length limit in characters).
- `SteamStore/localization/_brief/glossary_<code>.json`: the game's own UI text in your language, already shipped
  in the game. When the store text names a tool, a pay-slip line, an upgrade stat (POWER / AREA), the Hand over
  button or an inspection lamp, use exactly these words (in running-text case where appropriate; POWER and AREA
  stay exactly as the game writes them). The glossary also shows how the game addresses the player (formal or
  informal "you"): use the SAME register on the store page.
- To see what the page shows (optional but useful): `SteamStore/_preview/description_mock.png` (the English
  column), `SteamStore/description/*_poster.png` (a frame of each clip), `SteamStore/screenshots/01_hero_reveal.jpg`
  ... `09_spotless_handover.jpg`.

## Write
One file per language: `SteamStore/localization/<code>.json` (UTF-8, no BOM), exactly:
```json
{
  "code": "<code>",
  "steam": "<steam api language, from the glossary file>",
  "language": "<English name>",
  "fields": { "<every id from source_en.json>": "<your text>", ... },
  "translator_notes": "<terms you hesitated over and why you chose what you chose; empty string if none>"
}
```
Plain text only in the fields: no BBCode, no HTML, no line breaks. The page structure (images, bold lead-in,
headings, list) is assembled by a script.

## Rules
1. Meaning over words. Convey what the English means for a player (the notes explain every idiom). Restructure
   sentences freely; keep every fact, add no new claims, drop nothing that matters.
2. Tone: warm, sensory, confident marketing copy for a cozy ASMR cleaning game; short sentences. No exclamation
   marks unless your language's store copy would really use one; no "!!!", no emoji.
3. Keep untranslated: Refluff, ASMR, DirectX 12, VRAM, product names (NVIDIA GeForce GTX 1060, AMD Radeon RX 580,
   RTX 3060, RX 6600 XT), Windows 10 / 11, 1909, 1440p, $120, the numbers 10 and 31.
4. Typography of your language: correct quotes if you need any (never ASCII `"`), French no-break space
   (U+00A0) before `: ; ! ?`, Spanish opening `¡ ¿`, full-width punctuation `，。：；（）` in Chinese and `、。` in
   Japanese, no sentence-final full stop in Thai. Do not use `& < > [ ] { }`, tabs, or double spaces.
5. The four `hdr_*` fields are painted into images as large letters: sentence case, no final full stop, and a HARD
   length limit (28 characters Latin / Cyrillic / Greek, 13 CJK, 24 Thai). Find a short natural phrase.
6. `short_description` max 300 characters (all languages). It is the most visible text on Steam: make it great.
7. Locale specifics: Spanish (Spain) vs Latin America and Portuguese (Portugal) vs Brazil must each use their own
   vocabulary, pronouns and verb forms (follow each glossary); Traditional Chinese = Taiwan usage and vocabulary
   (not a character conversion of Simplified); Simplified Chinese = mainland usage; Norwegian = Bokmål; Malay and
   Indonesian are separate languages with different vocabulary.
8. System-requirement lines: use the wording Steam store pages in your language customarily use.

## Process for each language
1. Draft all fields.
2. Self-review as a native gamer reading the Steam page: does each sentence sound natural? Back-translate it in
   your head: is any meaning lost, added or distorted? Are the game's terms used consistently?
3. Run `python Tools/steam_art/store_loc.py check <code>` (from `C:\UnityProjects\CarpetCleaning`) and fix
   until it prints 0 errors; look at the warnings too.
4. Write the file as soon as a language is done (do not hold several languages in memory before writing).

Do not edit any other file. Do not run Unity or any other script.

## Return (keep it short)
Per language: the check result, the four header texts, and at most two lines of notes on hard choices.
