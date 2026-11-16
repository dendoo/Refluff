# bg (Bulgarian) review

Verdict: OK after fixes

Faithful, idiomatic Bulgarian. The structure mirrors the Russian page, but the grammar is Bulgarian:
- Articles are right throughout: full article on subjects (бонусът, фишът, магазинът, звукът), short article on
  objects and after prepositions (косъма, ракела, праха).
- Count forms are correct (десет инструмента, 31 езика).
- There is no Russian-style comma before и between main clauses.
- The comma exception after an adverb is applied correctly (точно когато).
- There are no Russianisms or false friends: в мига, в който; трохичката потраква; моторите набират обороти; и към
  следващия.

It uses informal «ти» as the game does. The glossary terms are exact: стъргалка, четка, ракел, водосмукачка,
прахосмукачка, препарат, моп, Предай на клиента, надгради инструментите, and the pay-slip lines in „“ (бонус за
скорост, нито петънце, нито капка, комбо). The short description keeps the verb rhythm (изстържи, намокри, напени,
изчеткай, избутай, изсуши) and reads like native store copy. The system-requirement lines match Bulgarian Steam
(Изисква 64-битов процесор и операционна система).

Stat names: Bulgarian nouns have no case, so СИЛА and ПЛОЩ could stand in the sentence directly. The dash apposition
«подобрения на всеки инструмент — СИЛА и ПЛОЩ —» is equally natural and keeps the labels exact. Left as written.

## Changes
- hdr_reveal: Виж как шарката оживява -> Виж, как шарката оживява
  (punctuation: Bulgarian sets off every subordinate clause with a comma, and the rest of the page follows that norm)
- rugs_body: петна от разлято -> петна от разлети течности
  (fluency: a bare neuter participle with no article or noun reads as a calque of Russian «следы пролитого»)
- headphones_h2: Сложи слушалките -> Сложи си слушалките
  (idiom: "put your headphones on" needs the possessive dative си. Without it, it reads as "put the headphones
  (down somewhere)".)

## Doubts (left as written)
- hdr_reveal: headline practice (news, marketing) often drops the comma in «Виж как ...». Here the norm was
  followed. If the owner prefers headline style, the comma can go.
- reveal_body: «шарката се връща» is faithful to "comes back". «изплува отново» would be more vivid, but this is a
  matter of taste.
- water_body: «попива и се просмуква из косъма» is close to "soaks in and wicks through". «се разстила» would stress
  the sideways spread more, but the meaning holds.
- headphones_body: «пяната пращи» (crackles) for "foam fizzes" is an acceptable sound image. «пука» or «шушне» are
  alternatives.

## Meaning audit
2026-09-28, back-translation of every listed field against AUDIT.md: no meaning errors.

Unsure, checked and kept:
- tools_body «покрий мръсотията с пяна и я изтъркай с въртящата се четка» = "cover the grime with foam and scrub it with the
  rotating brush". «я» most naturally points to «мръсотията» (the direct object), so the brush scrubs the grime rather than
  "scrubs the foam in". That is still true in the game and «изтъркай» (scrub / scour) does not say "into the fibres";
  «втрий» would have made the forbidden "rub the grime in" reading possible, so the current verb is the safer one.
- water_body «Мръсотията тук не е нарисувана върху килима и не се изтрива с гумичка» = "the dirt is not painted on the
  rug and is not rubbed off with an eraser" (the not-a-picture idea, no face mask, does not say nothing is removed).
- bullet_nofail «без изтичащо време: бонусът за скорост само добавя към заплащането» = "no time running out: the speed
  bonus only adds to the pay".
