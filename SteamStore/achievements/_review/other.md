# Achievement translations: editor review (ar, tr, hu, th, vi, id, ms)

Reviewed against source_en.json (notes, terms) and the game UI tables in Assets/CarpetCleaning/Localization/Resources/Loc/<code>.json.
Every game term, address form and condition was checked (MAXED_OUT = same tool, BONE_DRY = soak then dry, EXPRESS = full speed bonus on a heavily soiled carpet).
`python Tools/Steam/achievements.py check`: 0 errors, 0 warnings after the edits.

## Changes (one line each)
- ar ACH_BONE_DRY desc: بلّل سجادة جيدًا -> اغمر سجادة بالماء. "Soak" now uses the game's own "Wet it down" hint (اغمر السجادة بالماء) and is stronger than "wet well".
- ar ACH_EXPRESS desc: مع سجادة -> عن سجادة. The bonus is earned *for* the carpet (عن). مع ("together with") read oddly.
- tr ACH_MAXED_OUT desc: hem GÜÇ hem ALAN -> hem GÜÇ hem de ALAN. The natural "hem ... hem de" pairing.
- hu ACH_BONE_DRY desc: Áztass el -> Áztass be. This is the game's verb (hint "Áztasd be a szőnyeget"). "eláztat" suggests an accidental drenching.
- hu ACH_EXPRESS desc: szőnyegen -> szőnyegnél. "egy nagyon koszos szőnyegnél" = with/for that job. "szőnyegen" read as "on top of the carpet".
- th ACH_MAXED_OUT desc: ชิ้นเดียว -> ชิ้นเดียวกัน. The text now clearly says both tracks belong to the SAME tool.
- vi ACH_FIRST_CARPET desc: "của bạn" -> "cho khách". "của bạn" was translationese. The new text is parallel with the other descriptions ("giao cho khách").
- vi ACH_SPOTLESS / ACH_FILTHY desc: "Làm một tấm thảm (cực) bẩn sạch 100%" -> "Giặt sạch 100% một tấm thảm (cực) bẩn". "làm + NP + adj" was awkward and put bẩn/sạch side by side. "giặt sạch" is the trade collocation (giặt thảm).
- vi ACH_COMBO desc -> "Giặt sạch 100% và sấy khô 100% một tấm thảm bẩn rồi giao cho khách". Same pattern as SPOTLESS, with the game's "sấy khô" verb.
- vi ACH_BONE_DRY desc: removed the comma before "rồi", as in the other descriptions.
- vi ACH_EXPRESS desc: "với một tấm thảm" -> "khi giao một tấm thảm". The bonus is paid at handover, and "với" was vague.
- vi ACH_MAXED_OUT desc: "của một dụng cụ" -> "của cùng một dụng cụ". This makes the same-tool condition explicit.
- id ACH_FULL_KIT desc: "semua 10 alat" -> "seluruh 10 alat". "seluruh + number" is the idiomatic Indonesian form (as in "seluruh 34 provinsi") and keeps the digit.
- ms ACH_BONE_DRY desc: "serahkan 100% kering" -> "serahkan dalam keadaan 100% kering". This matches SPOTLESS / COMBO / FILTHY. The text is 75 characters, under the 90-character limit.

## Kept on purpose
- All titles. The game terms are exact: ar نظافة تامة / جفاف تام, tr Tertemiz / Kupkuru, hu Makulátlan / Csontszáraz, th ไร้คราบ / แห้งสนิท, vi Không tì vết / Khô cong, id Tanpa Noda / Kering Kerontang, ms Tanpa Cela / Kering Kontang. The playful titles read naturally.
- tr "Takım Taklavat" is kept: its dictionary sense is "with all one's belongings", which fits "every tool in the van".
- ms "Gunakan kesemua 10 alat" is kept. "kesemua + number" is natural Malaysian Malay.
