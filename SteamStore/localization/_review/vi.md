# vi (Vietnamese) review

Verdict: OK after fixes

Faithful, lively Vietnamese copy. It addresses the player as bạn, as the game does (Cảm ơn bạn đã chơi).
Glossary terms are used consistently: Dao cạo, Bàn chải (bàn chải xoay), Cây gạt nước, Máy hút nước, Chất tẩy rửa,
Cây lau nhà, Giao thảm, SỨC MẠNH / PHẠM VI in capitals, and thưởng tốc độ / không tì vết / khô cong / combo. The
short description (285 characters) keeps the verb rhythm (Cạo bỏ... xịt ướt... phủ bọt, chà kỹ, gạt sạch... sấy khô...)
and reads like native copy. The four headers are natural and within 28 characters: Ngắm hoa văn dần trở lại,
Nước thật, bùn thật, Mỗi đơn, một tấm thảm mới, Nhận tiền, nâng cấp đồ nghề.

Diacritics: every field is precomposed Unicode (NFC). No wrong or missing tone marks were found. The old-style tone
placement (hóa, họa) is the same everywhere.

Word choice:
- đơn, short for đơn hàng, is the everyday word for a customer order in today's service and gig apps (nhận đơn, chốt đơn).
  Mỗi đơn, một tấm thảm mới is short and natural. The same word in rugs_body keeps the page consistent.
- đồ nghề (a tradesman's tools) is the natural word for "kit" and echoes the game's shop title Nâng tầm đồ nghề.
- phiếu lương for "pay slip", đã mắt for "satisfying to watch" and Đeo tai nghe vào nào for "Headphones on" are all idiomatic.

## Changes
- water_body: Vết bẩn không phải một lớp hình vẽ chà là mất -> Vết bẩn không phải một lớp hình vẽ chà qua là mất
  (fluency: "chà" + "là" side by side also spells chà là, the date fruit, a small snag that makes the reader stop.
  chà qua là mất, "rub over it and it is gone", keeps the meaning and removes the snag.)
- headphones_body: nước xịt xè xè -> nước xịt xì xì
  (xè xè is not an established Vietnamese sound word for water. xì xì is the standard hiss, the English "hisses".)

## Doubts (left as written)
- sysreq_min_notes has no closing full stop, while the sound lines have one. This matches the standard line Steam prints
  itself, which has no full stop in any language, so it was left as written. The Indonesian and Malay files do the same.
- water_body: cả một hệ mô phỏng chất lỏng. hệ thống mô phỏng would be the more usual compound, but hệ alone is
  acceptable and clear.
- sysreq_rec_soundcard: Rất nên dùng tai nghe is colloquial. Đặc biệt khuyên dùng tai nghe would match the minimum
  line (Khuyên dùng tai nghe) more closely, but the current text is understandable and common, so changing it would be
  a matter of taste.
- alt_clip5_paid: hai nâng cấp được mua is a slightly English-like passive. It is alt text only and still correct.

## Meaning audit
no meaning errors

Back-translations checked against the audit facts (literal):
- tools_lead: "Ten pieces of kit, one standard sequence."
- tools_body: "Pry apart the dry dirt crust with the scraper, cover the stains with foam, scrub so the foam soaks deep with the rotary brush, then use the squeegee to push the dirty water clean off the rug." (the FOAM is scrubbed in)
- water_body: "... The dirt is not a layer of picture that is gone once you rub over it: under every carpet fibre is a whole fluid simulation."
- rugs_body: "... Each order randomly mixes dust, grime, dry crust and spill marks over the pattern of a real carpet in a museum." (the carpet is in a museum; the player does not visit one)
- headphones_body: "All machine sounds are recorded from real machines: ..." (machines only)
- bullet_nofail: "No enemies, no losing, no running out of time: the speed bonus only adds, never deducts." ("trừ" = deduct [from the pay]; does not say the bonus never shrinks)
