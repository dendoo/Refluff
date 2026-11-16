# Achievements review: ja, ko, zh-Hans, zh-Hant (editor pass, 2026-09-30)

I checked all 40 entries against source_en.json (meaning, conditions and notes), the game's terms in Loc/<code>.json, and the store copy's tone and quote style.

## Changes
- ja ACH_EXPRESS desc: ひどい汚れのカーペットで → ひどく汚れたカーペットで、. The plain adjectival form still echoes the game's Heavy soil (ひどい汚れ), is less stiff, and clearly covers Filthy (汚れまみれ) carpets too.
- zh-Hans ACH_EXPRESS desc: 重度脏污的地毯 → 重度以上脏污的地毯. On its own, 重度脏污 reads as the Heavy soil class only; 以上 counts that class itself, so Filthy (极度脏污) is included, as in the English.
- zh-Hant ACH_EXPRESS desc: 重度髒污的地毯 → 重度以上髒污的地毯 (same reason; 以上 includes the class itself in Taiwan usage too).

## Checked, kept as written
- All terms match the game: ja 引き渡す / ピカピカ / カラカラ / 汚れまみれ / スピードボーナス / パワー / 範囲 / MAX / 強化 / 水分ハイライト / 汚れハイライト; ko 전달하기 / 반짝반짝 / 뽀송뽀송 / 극심한 오염 / 속도 보너스 / 파워 / 범위 / MAX / 습기·오염 하이라이트; zh-Hans 交付 / 一尘不染 / 完全干透 / 极度脏污 / 速度奖励 / 功率 / 范围 / 满级 / 水分高亮 / 污渍高亮; zh-Hant 交付 / 一塵不染 / 完全乾透 / 極度髒污 / 速度獎勵 / 功率 / 範圍 / 滿級 / 水分偵測 / 污漬偵測.
- 100% follows each game's '{0}% clean' spacing: ja/ko 清潔度 100% / 청결도 100%; zh 清洁度 100% (with a space). ACH_COMBO reuses the exact '100% clean + 100% dry' string in all 4 languages.
- Conditions are exact in all 4: MAXED_OUT = POWER and AREA of the same tool (ひとつの道具…両方 / 도구 하나의…모두 / 同一个工具…都); BONE_DRY = soak first, then 100% dry; SPOTLESS / COMBO = a dirty carpet.
- ja register: dictionary form (〜する。) is kept. It is the usual Japanese achievement style, and plain like the game's しよう hints.
- ko register: polite -세요 imperatives, as in the game's hints. Titles 폭신폭신 첫걸음, 꼬질꼬질 대변신, 만렙 도구 and 깐깐한 검사관 read naturally.
- zh titles: 化脏污为蓬松 / 化髒污為蓬鬆 (the 化X为Y pattern) works. Using 宛如新品 rather than 焕然一新 correctly avoids the game's 'Sparkling clean!' line.
- zh-Hant uses Taiwan wording (老主顧, 品管達人, 弄濕, 地板工具) and is consistent with the game's zh-Hant table.
- Format: UTF-8 without BOM, LF line endings, source order, no trailing spaces. Titles are 3-11 characters and descriptions 8-33.
