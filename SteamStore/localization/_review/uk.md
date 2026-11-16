# uk (Ukrainian) review

Verdict: OK after fixes

Faithful, natural Ukrainian that reads as written natively, not as Russian transposed. It follows the Russian page's
structure closely, but the wording is Ukrainian throughout. There are no Russianisms or false friends: чищення (not
чистка), багнюка (not грязь), крамниця, щоразу, всотується, здіймається шапкою, приємніший за (not чим), перетворюється
на (not в), 31 мовою, щойно намокне, and the Ukrainian path instrumental in розтікається ворсом. It uses informal «ти»
as the game does. The glossary terms are exact: шкребок, щітка, згін, водосос, пилосос, мийний засіб, швабра, Віддай
клієнту, прокачай інструменти, and the pay-slip lines in «» (бонус за швидкість, ні плямки, ні краплі, комбо). The
short description keeps the verb rhythm, and «доки на килимі не залишиться ні плямки» echoes the Spotless line. All
four headers are natural and within 28 characters. The system-requirement lines use the usual Ukrainian Steam wording
(64-розрядна, ГБ, або новіша / потужніша).

Stat names: СИЛА and ЗОНА cannot take case endings (the check needs the game's exact form). So «на покращення
будь-якого інструмента — СИЛА й ЗОНА — або на лампи для огляду» sets them off as a dash apposition. That is the
standard Ukrainian way to embed uninflected UI labels, and it reads cleanly. Left as written.

## Changes
- water_body: «не намальований поверх килима і не стирається гумкою» -> «не намальований поверх килима й не стирається гумкою»
  (euphony: after a vowel and before a consonant Ukrainian uses й. The rest of the page follows this rule.)

## Doubts (left as written)
- reveal_body: «візерунок повертається» is faithful to "comes back". «знову проступає» would be a little more vivid,
  but this is a matter of taste.
- hdr_water: «Вода й багнюка як у житті» could take a dash («Вода й багнюка — як у житті», 27 chars). As an
  elliptical header it is acceptable without one.
- sysreq_rec_soundcard uses «рекомендуємо» while the minimum line uses «Рекомендовано». Both are standard, so the
  difference was kept.

## Meaning audit
2026-09-28, back-translation of every listed field against AUDIT.md.

- tools_lead: «Десять інструментів і чіткий порядок.» -> «Десять інструментів, один правильний порядок.»
  ("ten tools and a clear/strict order" lost "right" and leaned towards a fixed, enforced sequence; now "ten tools, one right order")

Checked closely and kept:
- tools_body «нанеси на бруд піну та втри її обертовою щіткою»: «її» can only be «піну» (бруд is masculine), so the brush scrubs the foam in.
- water_body «Бруд тут не намальований поверх килима й не стирається гумкою» = "the dirt is not drawn over the rug and is not rubbed off with an eraser".
- bullet_nofail «без … жорстких таймерів: бонус за швидкість лише додає до оплати» = "no hard timers: the speed bonus only adds to the pay".
- hdr_paid «Заробляй на покращення» = "earn for upgrades".
