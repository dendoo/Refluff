# Achievement review: ru, uk, bg, pl, cs, el

Editor pass of 2026-09-30, checked against `source_en.json`, the game's `Loc/<code>.json` terms and the store page texts.
All six files pass `python Tools/Steam/achievements.py show <code>` with 0 errors and 0 warnings.
They are UTF-8 without a BOM, in json indent=2 layout, with the APIs in source order.

## Changes

- ru ACH_BONE_DRY desc: "намочи" became "промочи" (soak, not just wet), and "его" was added ("отдай его клиенту"), which reads more naturally.
- ru ACH_MAXED_OUT desc: "Прокачай улучшения" became "Доведи улучшения ... до уровня МАКС.". "Upgrade the upgrades" was a tautology and repeated the title's "Прокачано".
- uk ACH_BONE_DRY desc: "намочи" became "промочи" (soak), to match ru.
- uk ACH_MAXED_OUT desc: "Прокачай покращення" became "Доведи покращення ... до рівня МАКС.". This fixes the same tautology and title repeat as in ru.
- pl ACH_FILTHY name: "Z brudu w puch" became "Od brudu do puchu". "w puch" reads as "rozbić w puch" (crush). The new title follows the rhyming "od zera do bohatera" pattern.
- cs ACH_SPOTLESS / BONE_DRY / COMBO / FILTHY desc: "100 %" now uses a no-break space (U+00A0). It looks the same as the game's "čistota {0} %", matches the game's HUD "{0} %" (which already uses U+00A0), and stops Steam from wrapping a lone "%" onto the next line.
- cs ACH_FULL_KIT name: "Celá dodávka v akci" became "Celá výbava v akci". "dodávka" also means "delivery", and "the van in action" suggests driving. "výbava" is the game's own word for kit ("Vylaď si výbavu").
- cs ACH_MAXED_OUT name: "Vylepšeno nadoraz" became "Vyladěno nadoraz". This avoids the repeat of the description's "Vylepši" and echoes the shop title "Vylaď si výbavu".
- el ACH_FULL_KIT name: "Όλο το βαν σε δράση" became "Όλος ο εξοπλισμός σε δράση" (26 chars). "The van in action" reads as the van driving. "εξοπλισμός" is the game's word for kit ("Βελτίωσε τον εξοπλισμό σου").
- el ACH_MAXED_OUT desc: "την ΙΣΧΥ και το ΕΥΡΟΣ" became "τις αναβαθμίσεις ΙΣΧΥΣ και ΕΥΡΟΣ". This keeps the game's exact label ΙΣΧΥΣ uninflected, as the store page does ("ΙΣΧΥΣ και ΕΥΡΟΣ μένουν άκλιτα").

## Confirmed as is (translator flags)

- ru/uk ACH_FILTHY "Из грязи в пух" / "З грязі в пух": the frame "из грязи в князи" is instantly recognisable, and "в пух" also recalls "разодеться в пух и прах". Keep. In uk, "грязь" is fine inside the idiom.
- ru/uk ACH_INSPECTOR: the accusative inside the quotes matches the game's «Улучшениях» / «Покращеннях». Keep.
- ru/uk ACH_FULL_KIT: the tradesman's collective "инструмент" / "інструмент" works. Keep.
- bg ACH_FILTHY "От прах до пух": the rhyme carries the idea, and dust is one of the game's soil types. Keep.
- bg ACH_MAXED_OUT "До дупка": colloquial ("volume all the way up") but not rude. It fits the playful tone. Keep.
- bg ACH_FILTHY desc: the quoted label „Мръсен до немай-къде“ is clearer than an unquoted, heavy adjective phrase. Keep.
- pl ACH_FULL_KIT "Cały sprzęt z busa w akcji": "bus" for a tradesman's van is natural, and "sprzęt" is the game's word ("Podrasuj sprzęt"). Keep.
- pl no quotation marks and capitalised item names: this matches the game's "kup w Ulepszeniach". Keep.
- cs ACH_FILTHY "Z bláta do chmýří": a good positive twist on "z bláta do louže". Keep.
- el ACH_FILTHY "Από τη λάσπη στο πούπουλο" (26 chars): vivid and within the limit. Keep.
- el "Παράδωσε" for Hand over (game: "Παράδοση"): same root, as on the store page. Keep.

## Conditions verified

- ACH_MAXED_OUT: every language says both tracks of ONE tool ("одного инструмента", "на един инструмент", "jednego narzędzia", "jednoho nástroje", "ενός εργαλείου").
- ACH_BONE_DRY: soak first, then 100% dry, in all six. There is no "dirty" condition, which matches `Achievements.PayoutUnlocks` (it checks perfectDry and peak wetness only).
- ACH_EXPRESS: full speed bonus plus the game's Heavy soil wording, in all six.
- ACH_SPOTLESS / ACH_COMBO / ACH_FILTHY: the carpet is dirty or Filthy, and the percentages use the game's clean/dry wording.
