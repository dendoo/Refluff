# Store page localization (2026-09-28)

Request (RU): "add the recommended localizations to the Steam page; the translation must be adequate".

Steamworks > App Admin lists under RECOMMENDED ITEMS one "<language> store localization" item for every language the
game supports that Steam's store also has: **29 items** (the game's 31 interface languages minus English and Arabic;
the Steam store has no Arabic). This doc covers the store page text in those 29 languages and the localized header
images of the description. The game's own UI translation is `LOCALIZATION.md`.

- Sources and translations: `SteamStore/localization/` (`source_en.json`, `<code>.json`, reviews in `_review/`).
- Tool: `Tools/steam_art/store_loc.py` (glossaries, checks, Steamworks upload files).
- Header images: `Tools/steam_art/make_headers.py` -> `SteamStore/description/<code>/hdr_*_<steam language>.png`.

## 1. What is localized

| Store field | Localized | How |
|---|---|---|
| Short description | yes, 29 | translated, max 300 characters (Steam's recommendation 200-300) |
| About This Game | yes, 29 | the same BBCode layout as English, assembled by `store_loc.py` from plain-text fields |
| Description header images (4) | yes, 29 x 4 | drawn per language, grouped with the English image by Steamworks (section 3) |
| Alt texts of the 9 description assets | yes, 29 | the 5 clip descriptions + the 4 header texts |
| System requirements (min + rec) | yes, 29 | processor lines stay English (product names only) |
| Legal lines | framing only | the game's own translated credit lines (`Resources/Loc/<code>.json`) + a translated "Licences:"; the 8 CC BY credits stay verbatim |
| Clips, screenshots, capsules, trailer | no | the clips are textless; screenshots / capsules / trailer show the English HUD and title |
| App name | no | "Refluff: Carpet Cleaning ASMR" everywhere (`game-title` decision) |

English changed in one line: the feature list said "English interface"; it now says "interface in 31 languages".

## 2. How the translations were made (quality process)

1. `source_en.json`: every piece of text as one field with `where` (place on the page), `note` (meaning, idioms,
   pitfalls: e.g. "Nothing is a mask being erased" = the dirt is not a picture rubbed away as in other cleaning games)
   and `max`. The page structure (images, bold lead-in, h2, list) is NOT in the translator's hands: `store_loc.py`
   assembles the BBCode, so a translation cannot break it.
2. `store_loc.py glossary` -> `_brief/glossary_<code>.json`: the game's shipped UI terms in that language (tool names,
   pay-slip lines, POWER / AREA, Hand over, the lamps, hints). The store page uses the same words the player sees, and
   the same form of address as the game.
3. Seven translator agents by language family (fr it ro / es es-419 pt-BR pt-PT / de nl da no sv / ru uk bg el /
   pl cs hu fi tr / ja ko zh-Hans zh-Hant / th vi id ms) with `_brief/BRIEF.md`: meaning over words, locale
   typography, sibling locales written separately (Spain vs Latin America, Portugal vs Brazil, Taiwan vs mainland,
   Malay vs Indonesian), hard limits for the painted headers.
4. Seven independent reviewer agents (`_brief/REVIEW.md`): back-translation, fluency, terminology, grammar and
   typography; they fixed real problems in place and logged every change in `_review/<code>.md`.
   Russian was reviewed line by line by the lead agent (`_review/ru.md`).
5. `store_loc.py check`: every field present, limits, forbidden characters (ASCII `"`, brackets, `&`, URLs, double
   spaces), kept tokens (Refluff, ASMR, 10, 31, $120, DirectX 12, GPU names, 1440p...), the game's POWER / AREA terms,
   no full stop on a header, CJK full-width punctuation, French no-break spaces.

These are machine translations with a machine review. No human native speaker has read them (only the Russian was
read by a Russian speaker, the agent). The reviews list the doubts that a native player could settle.

## 3. Localized header images

Steamworks groups description assets by a Steam language suffix: `hdr_reveal_german.png` joins the `hdr_reveal`
group, and `[img]{STEAM_APP_IMAGE}/extras/hdr_reveal[/img]` in ANY language's description shows the viewer's
language (Valve: "Store Page Extra Asset Management", Localized Image Management). So every language's BBCode
references the same four names as English, and a group keeps one alt text per language at the English asset's index
(`steam_extras.json`).

`make_headers.py` draws the localized strips with the English style (cream wool letters, teal stitched underline,
foam bubbles), but the text is shaped with HarfBuzz and rasterized with FreeType (`uharfbuzz` + `freetype-py`,
installed 2026-09-28): Pillow on this machine has no raqm, so it could not stack Thai tone marks over vowels or kern.
English still takes the old Pillow path and stays byte-identical to the uploaded strips.

| Script | Font (OFL) | Weight |
|---|---|---|
| Latin that Fredoka covers | Fredoka (the logo's face) | 700, width 106 |
| Latin Extended / Vietnamese / Cyrillic | Nunito | 900 |
| Greek | Inter | 800, opsz 32 |
| Japanese / Simplified / Traditional Chinese / Korean | Noto Sans JP / SC / TC / KR (the game's UI fonts, `Tools/loc/fonts_src`) | 800, 0.84 em |
| Thai | Noto Sans Thai | 800, 0.92 em |

Fitting (`lang_fit`): the ink bounds come from the outlines (fontTools `BoundsPen`), and all four headers of a language
share ONE type size and ONE baseline: the largest size at which every one of them keeps its ink in rows 7..95 and left
of x 1496 (the underline, bubbles and wool fringe need the rest), at most down to 0.74 of nominal, else the script
refuses and the phrase must be shortened. So the headers of one page never change size or height from block to block.
Accented capitals over descenders decide it: fr 0.87 (À ... p), hu 0.88 (Ú), pt 0.88 (Á), sv 0.87 (Ä), ro 0.81 (Î ... ș p),
vi 0.94, th 0.84 of nominal; all others 1.0. Glyphs are rasterized one by one and OR-ed (a whole line overflowed
FreeType's raster pool on "Terima upah, perkuat alatmu"). All 116 strips take about 6 minutes.
Review sheet: `python Tools/steam_art/make_headers.py --no-mock --sheet <png>`.

## 4. Commands

```
python Tools/steam_art/store_loc.py glossary          # _brief/glossary_<code>.json from the game's Loc tables
python Tools/steam_art/store_loc.py check [codes]     # 0 errors expected
python Tools/steam_art/store_loc.py show ru           # the assembled About This Game of one language
python Tools/steam_art/make_headers.py --no-mock      # all header strips (English byte-identical)
python Tools/steam_art/store_loc.py build             # check + upload/storepage_1337056_<steam language>.json (30 files)
```

## 5. Upload to Steamworks (done 2026-09-28)

Store Admin, app 5304930 (store item 1337056). Saved, not published: the app has never been public, so Steamworks has
nothing to publish yet ("This application has not yet been published"). What the reviewers and the store preview see is
the saved state. The store page and the build were already in Valve's review queue (submitted 2026-09-27) when this was
uploaded.

1. **Header images.** Description tab > "Upload Custom Images". The uploader takes files only by drag and drop: the 116
   PNGs went into a helper `<input type=file>` (the browser tool caps 10 MB per call, so two batches of 64 + 48), were
   checked against local SHA-256, then handed to the React `onDrop` of `.DragAndDropContainer`. Steam staged each as
   "German / LOCALIZED IMAGE GROUP / hdr_reveal (Group of N)"; one "Upload" click sent all 116 (about 3 minutes,
   116 x Success). The groups are server-side at once, no Save needed; after a reload the list still shows one entry
   per group.
2. **Text.** Localization tab > "Upload Localization" with the 30 `upload/storepage_1337056_<language>.json`
   (English included, for the "interface in 31 languages" line). Steam's own format: the same keys as its "Download
   Localization" export (`steamworks_export/` keeps the English export from before the change).
3. **Recommended requirements.** The import IGNORES `app[content][sysreqs][windows][rec][*]` (its export has no rec keys
   either). The 145 localized rec values were written into the form's hidden inputs
   `app[content][sysreqs][windows][rec][<field>][<language>]` and saved with the footer Save ("Changes saved").
4. **Checked after saving:** every uploaded field of all 30 languages matches the files by SHA-1 (legal, about, short
   description, min + rec requirements); 150 rec fields filled; the store pages in German, Japanese and Thai show the
   translated text, the translated alt texts and the localized header images (a German visitor gets different image
   files than an English one).

Fixed on the same day, with the owner's approval:
- **Supported Languages** had Interface ticked for 67 languages, 36 of which the game does not have (Albanian ...
  Zulu); now exactly the game's 31. The store page lists 31.
- The minimum requirements' "Additional notes" said "Requires a 64-bit processor and operating system.", which Steam
  already prints above every requirements block when 64-bit is ticked: the field is empty now in every language
  (`store_loc.py` DROPPED).

Not localized (out of the store-localization items): the generative-AI disclosure text (Content Survey), the capsules
and screenshots (English title / HUD), the trailer. Steam China (`sc_schinese`) is a separate storefront and not one of
the recommended items.

## 6. Changing the text later

Edit the English field in `source_en.json` and the same field in all 29 `<code>.json` (the `where` / `note` explain
it), run `check`, redraw headers if a `hdr_*` changed, `build`, and upload only the changed languages' JSON on the
Localization tab. A header change needs its PNG replaced inside the Steamworks image group.
