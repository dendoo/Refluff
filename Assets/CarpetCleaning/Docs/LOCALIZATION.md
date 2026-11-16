# Localization: 31 interface languages and the language menu (2026-09-27)

Requested by the user, with the Steamworks language list (Interface checked for all 31 full-platform languages):
English, French, Italian, German, Spanish (Spain), Arabic, Bulgarian, Czech, Danish, Dutch, Finnish, Greek, Hungarian,
Indonesian, Japanese, Korean, Malay, Norwegian, Polish, Portuguese (Brazil), Portuguese (Portugal), Romanian, Russian,
Simplified Chinese, Spanish (Latin America), Swedish, Thai, Traditional Chinese, Turkish, Ukrainian, Vietnamese.
Subtitles and Full Audio do not apply: the game has no spoken words.

## 1. What the player gets

- **Menu > Language**: a drop-down row under Volume. The select shows the current language in its own name; the list
  (one row per language, every name in its own script and font, the current one ticked, scrolls) switches at once: the
  whole HUD, the open menu, the shop, the pay slip and the credits re-text live, no restart.
- The choice is saved (`PlayerPrefs` `refluff.language`). Without a saved choice the game follows the language Steam runs
  it in (the per-game language in the Steam client), else the OS language, else English. An automatic pick is never saved,
  so a player who never chose keeps following Steam.
- Esc closes the list first, then the credits page, then the menu. A click beside the list closes only the list.
- Hotkey chips show the letter the player's keyboard prints (section 5).

## 2. How it works

Keys are the **English text exactly as the game shows it** (the UXML text, the C# literal, the `Credits.txt` line), so
English needs no table and an untranslated key simply shows in English.

| File | Role |
|---|---|
| `Tools/loc/strings.json` | Source of truth: every key with `where` / `note` / `max` for translators, grouped (hud, tools, hints, payslip, shop, menu, credits) + the 3 rich-presence lines. |
| `Localization/Resources/Loc/en.json` | The key list the game ships (`loc_tool.py sync` writes it). `HudLocalizer` registers only UXML labels whose text is one of these. |
| `Localization/Resources/Loc/<code>.json` | One flat `{ "English": "translation" }` per language (codes below). |
| `Scripts/Localization/Loc.cs` | `Loc.T(key)`, `Loc.F(key, args)` (invariant `string.Format`; a broken placeholder falls back to English), `Loc.Key("..")` (marks a literal that reaches T through a variable), `SetLanguage`, `Detect`, `Changed`. Language table = drop-down order. Statics reset at `SubsystemRegistration` (no domain reload). |
| `Scripts/Localization/LocJson.cs` | Reader for the flat JSON (no Newtonsoft dependency). |
| `Scripts/Localization/KeyNames.cs` | Chip letters from the keyboard layout (section 5). |
| `Scripts/UI/HudLocalizer.cs` | On `CarpetUI`, order **-50**: walks the tree once in Start (before any view adds rows), re-texts on `Loc.Changed`, puts the script's font on the root, RTL, classes `lang-<code>` / `script-<base|arabic|thai|cjk>` / `lang-rtl` on `.hud`, pins numbers LTR. Debug: `DebugJson`, `OverflowJson`. |
| `Scripts/UI/LanguagePicker.cs` | The drop-down (owned by `PauseMenu`, like `ShopView` by `EconomyHud`). |
| `Editor/LocFontBuilder.cs` | Font assets (section 4). `CarpetSceneBuilder.BuildUI` adds `HudLocalizer`, creates missing font assets and wires them. |
| `Tools/loc/loc_tool.py` | `sync`, `check`, `langs` (section 7). |
| `Tools/loc/build_fonts.py` | Subset fonts (section 4). |

Rules the code follows:
- **Unnamed UXML labels** are static text: their UXML text must be a key, `HudLocalizer` owns them.
- **Named labels** belong to C#: `CleaningHUD` (hint, meters), `EconomyHud` (clock, par), `ShopView` (tool cards,
  POWER/AREA, prices, item texts), `PayoutView` (title, details, `#{0}`), `PauseMenu` (volume value, select, credits).
  Each translates its own text and re-texts it on `Loc.Changed`.
- `sim.Hint`, `ToolTable` names, `ShopItems.Name`, `PayoutResult.SoilName` stay English (tests and recipes read them); they
  are keys and are translated where they are shown.
- Percentages go through the key `{0}%` ("85 %" in French / German / Nordic / Czech / Finnish, "%85" in Turkish).
  Money (`$1,234`), times (`2:43`) and `×1.44` stay invariant in every language.
- Credits: a heading is looked up in its uppercase form, a line as written. Attribution lines (titles, authors, licences,
  museum objects, library copyrights) have no translation on purpose and stay English (left to right, in Inter).

Language codes: `en fr it de es ar bg cs da nl fi el hu id ja ko ms no pl pt-BR pt-PT ro ru zh-Hans es-419 sv th zh-Hant
tr uk vi`. Steam API names map 1:1 (`koreana`, `brazilian`, `portuguese`, `latam`, `schinese`, `tchinese`, `malay` ...,
`Loc.Languages`); `SteamService.GameLanguage` is `SteamApps.GetCurrentGameLanguage()`.

## 3. The menu row and the list

`CarpetHud.uxml`: the row in `#menu-main` (`#menu-lang` button + `#menu-lang-value` + chevron); in the `#menu` layer after
the panel: `#lang-catcher` (full screen, ui-blocking, closes the list) and `#lang-popup` (`#lang-list` ScrollView).
`LanguagePicker.Place` puts the popup under the select in layer coordinates (right-aligned in Arabic), at least 220 px
wide; `ScrollTo` the ticked row one panel update after opening. Style: section "language drop-down" in `CarpetHud.uss`
(white card, sunken rows, teal tick, restyled scroller like the credits). Every list name gets its own script's font
(`HudLocalizer.ElementFont`; Base = `StyleKeyword.Initial`, which resolves to the theme's Inter even under a CJK root)
and its own direction (Arabic RTL, the rest LTR).

`PauseMenu` gives the VOLUME and LANGUAGE captions the width of the wider one (measured a panel update after a switch),
so the slider and the select line up in every language.

## 4. Fonts

The HUD font is Unity's built-in **Inter** (the runtime theme): it covers Latin (incl. Vietnamese, Romanian ș ț,
Turkish, Polish, Czech, Hungarian ő ű), Greek and Cyrillic, so 24 languages need nothing more. This Unity build renders
UI Toolkit text with the **Advanced Text Generator** (ICU + HarfBuzz) by default: Arabic joining and bidi, Thai mark
stacking and line breaking work without any shaping code of ours.

Arabic, Thai, Japanese, Korean, Simplified and Traditional Chinese use bundled **Noto Sans** fonts, subset:

| Family | Source (Google Fonts repo) | Kept | Size (R+B) |
|---|---|---|---|
| NotoSansArabic | `ofl/notosansarabic/NotoSansArabic[wdth,wght].ttf` | ASCII + punctuation + the Arabic block + used | 132 KB |
| NotoSansThai | `ofl/notosansthai/NotoSansThai[wdth,wght].ttf` | ASCII + punctuation + the Thai block + used | 60 KB |
| NotoSansJP | `ofl/notosansjp/NotoSansJP[wght].ttf` | + kana, JIS X 0208 level 1 kanji (2,965) + used | 2.6 MB |
| NotoSansKR | `ofl/notosanskr/NotoSansKR[wght].ttf` | + KS X 1001 Hangul (2,350) + used | 1.0 MB |
| NotoSansSC | `ofl/notosanssc/NotoSansSC[wght].ttf` | + GB2312 level 1 (3,755) + used | 2.4 MB |
| NotoSansTC | `ofl/notosanstc/NotoSansTC[wght].ttf` | + Big5 frequent (5,401) + used | 3.9 MB |

- `python Tools/loc/build_fonts.py`: instances each variable font at wght 400 and 700 (wdth 100), renames the faces,
  subsets (all layout features kept: Arabic joining, Thai marks, CJK `locl`), drops hinting, pins the head timestamps
  (byte-identical rebuilds) and writes `UI/Fonts/NotoSans<Script>-<Regular|Bold>.ttf`. The everyday sets mean most
  translation edits need no rebuild; `loc_tool.py check` reports a character the font lacks.
- Sources live in `Tools/loc/fonts_src/` (build-time only, never in Assets): the six files above plus Inter
  (`ofl/inter/Inter[opsz,wght].ttf`, the checker's stand-in for the built-in Inter), downloaded with
  `curl -L -o <name>-VF.ttf https://raw.githubusercontent.com/google/fonts/main/<path>` (brackets URL-encoded).
- **After `build_fonts.py`, run menu CarpetCleaning/Localization/Build Font Assets** (`LocFontBuilder.Build(true)`): it
  recreates the 12 dynamic SDF `FontAsset`s next to the TTFs (72 pt sampling, 8 px padding, 1024 atlas, multi-atlas),
  puts the Bold asset in the Regular one's weight table (700: `-unity-font-style: bold` draws the real bold face, 1.6x the
  ink of Regular), gives every Regular asset the other five as fallbacks, and re-wires the scene's `HudLocalizer`. A
  dynamic asset caches glyph indices of its TTF: a regenerated TTF without recreated assets can draw wrong glyphs.
- `HudLocalizer` sets the language's font inline on the HUD root (Latin letters and digits then come from the same Noto
  font, matching it); Base languages clear it back to the theme's Inter.
- Why not the Windows fonts the text system falls back to (Arial, YaHei, Malgun Gothic, MS Gothic...): Japanese and
  Traditional Chinese then draw with the Simplified-Chinese YaHei glyph forms, fallback text loses bold, and Proton /
  Steam Deck prefixes have other fonts. The OS fallback stays as the last safety net.
- Licence: SIL OFL 1.1 (CJK: Adobe, Reserved Font Name "Source", which our names do not use). The three licence files
  are kept in `Tools/loc/licenses/`; `make_notices.py` appends them to `THIRD_PARTY_NOTICES.txt`, and `Credits.txt` has a
  Fonts section.

## 5. Hotkey chips follow the keyboard layout

The game reads keys by US position (`Keyboard.current.zKey`). On a German QWERTZ keyboard the moisture lamp is the key
printed "Y", on a French AZERTY one "W". `KeyNames.Label(Key)` returns the letter the current layout prints there
(`KeyControl.displayName`), or the US letter when the layout prints no Latin letter there (Cyrillic, Greek, Arabic...:
those keyboards carry the Latin legend too). Used by every single-letter `.kbd` chip (HudLocalizer, refreshed on a
keyboard reconfiguration), the item text "press {0}" and the toast "buy it in Upgrades ({1})". Digit and Esc chips stay.

## 6. Layout rules for longer text

- Width is never fixed where a translation lands: `.strip` has `min-width` (a long tool name widens the strip),
  `.urow__caption` and `.menu__row-caption` are equalized from C#, the shop legend wraps.
- `.script-arabic` / `.script-thai` captions have no letter-spacing (it tears joined Arabic letters apart and lifts Thai
  marks); `.script-cjk` keeps a little. `.script-arabic .pline` is 62 px (Noto Sans Arabic's line box needs 61).
- `.lang-rtl` mirrors the rows that pair a text with a chip or an icon (tool rows, meter heads, pay-slip lines, shop
  heads / rows / items, menu rows, the select) and right-aligns their text; the screen layout (strips, cards) stays.
  Money, percentages, times and the ×-gains are pinned LTR in C# (`HudLocalizer.LtrNumbers`, `PayoutView`, `ShopView`),
  otherwise bidi turns "+$120" into "$120+" and reverses "×1.44 → ×1.73".
- `Refluff` (the title), `Esc`, digits and `$` are not translated.

## 7. Workflow

**Change or add a UI string**
1. Write the English in UXML / C# as usual (C#: `Loc.T("...")` / `Loc.F("...", args)`; a literal that reaches T through a
   variable: `Loc.Key("...")`; a UXML text on an unnamed label is picked up automatically).
2. Add it to `Tools/loc/strings.json` with `where`, `note`, `max`; `python Tools/loc/loc_tool.py sync`.
3. Add the translation to all 30 `Loc/<code>.json` files (same key order). Translator brief: the prompt used on
   2026-09-27 is summarized in SESSION_LOG; key points: tone, uppercase captions (Greek without tonos, Turkish İ),
   placeholders, consistency pairs (Upgrades, POWER/AREA legends, par, Spotless/Bone dry, Hand over), `max`.
4. `python Tools/loc/loc_tool.py check`: 0 errors expected. It checks every file for missing / unknown / empty keys,
   placeholders, uppercase captions, soft lengths, kept names (Vahov Games, Freesound.org, THIRD_PARTY_NOTICES.txt...),
   legend prefixes, **glyph coverage** of the language's font chain, every UXML / `Loc.*` / Credits string being a key,
   stale keys, and all 31 rich-presence vdfs.
5. New characters outside the everyday sets: `build_fonts.py`, then Build Font Assets (section 4).
6. Play: `CarpetDebugApi.LocOverflow()` in each language on the screens it touches (section 8).

**Add a language**: a row in `Loc.Languages` (C#) and `LANGS` (`loc_tool.py`), its json, its rich-presence vdf; a new
script also needs a font family in `build_fonts.py`, `LocFontBuilder.Families`, `LocScript`, `HudLocalizer` and USS.

## 8. Testing

- `CarpetDebugApi.LocJson()` (code, source cmdline / saved / steam / system / default / set, saved choice, Steam and OS
  language, keys, missing, registered texts / chips, font, rtl), `SetLanguage(code, remember=false)` (tests never touch
  the saved choice), `LanguagePick(code)` (as a click on the list, saved), `LanguageList(open)`, `LanguageForget()`,
  `LocOverflow()` (every shown label whose text is wider than its box, or taller when it wraps).
- `Refluff.exe -language ja` starts a player in that language without saving it.
- Store captures (`StoreShotApi`, `StoreGifApi`) use the current language: `SetLanguage("en")` first for the English
  store assets, or another code for localized screenshots.

## 9. Verified 2026-09-27

- `loc_tool.py check`: 30 languages, 100 keys each, 0 errors, 0 warnings (placeholders, uppercase, glyph coverage in
  the built fonts, sources, 31 rich-presence vdfs).
- Play (scripted `EditorApplication.Step` frames), every language x six screens (HUD, menu, menu + open list, shop,
  pay slip, credits): **0 overflowing texts** (`LocOverflow`; the detector itself was checked with a planted 91 px and a
  planted vertical overflow). A second sweep for text leaving its card or row found only the Arabic pay-slip lines
  (4.5 px, clipped by `.pline`), fixed with the 62 px rule; the credits lines below the fold are the scroll view.
- Contact sheets of all 31 languages (menu + list, shop, pay slip) and the credits in ar / ja / ru reviewed: fonts,
  bold, Arabic shaping and mirroring, Thai marks, CJK glyphs, ticks and scroll position correct.
- Logic: all 31 Steam names map back; OS fallback (Russian -> ru, Portuguese -> pt-BR, Chinese -> zh-Hans, Hebrew ->
  English); 31 tables load with 0 missing keys; debug switches leave the saved choice alone; a list pick saves it and
  `LanguageForget` returns to Steam's language; an unknown key shows English. The Editor starts in **English**: Steam's
  game language for this account is `english` (the OS is Russian, which only comes after Steam).
- Key chips: this machine's keyboard was on the Russian layout (`zKey.displayName` = "Я"), the chip showed "Z" (the
  Latin fallback path).
- Player build (`Builds/Steam/local`, 211 files): windowed runs with `-language ja`, `ar`, `th` captured with
  `PrintWindow`: Noto fonts and bold in the player, Arabic RTL shaping and mirrored strip, Thai marks; `Player.log` has
  no exception and no `[Loc]` warning.

## 10. Open

- Translations were written by eight parallel agents (one per language family) from `strings.json`, then checked by
  the tool, by overflow sweeps and by eye; only the Russian was reviewed line by line (by the agent, not a native
  speaker). Terms the translators flagged for a native look: fr Extracteur / Sec à cœur; es Espumador / Rasqueta;
  es-419 Jalador / Trapeador; pt-PT Puxa-água; pt-BR Extratora; ro Spumă; no Nal; da Svaber; sv Gummiskrapa /
  Golvrengöring; nl Trekker; ru Водосос / Сгон; uk Згін / ЗОНА; bg Ракел / Водосмукачка / Интервал; pl Pianownica;
  cs Napěňovač; el ΜΕΓ. (MAX); tr Çekpas; th เป้า (par) and พื้นที่ (AREA) next to พื้น (FLOOR); vi Ghi công; id Wiper;
  ms Penyapu air; ja 強化 (Upgrades); zh 功率 (POWER), 完美组合 / 完美！ (Combo).
- A real QWERTZ / AZERTY layout was not tried (only the Cyrillic fallback).
- Steam: upload the 29 new `Tools/Steam/rich_presence_*.vdf` (Community > Rich Presence) and Publish; the language
  checkboxes (Interface) must be published too. The store page text itself is localized separately
  (`RELEASE_CHECKLIST.md` 2.2). A new build must be uploaded for players to get the languages.
