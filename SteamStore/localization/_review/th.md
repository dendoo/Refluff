# th (Thai) review

Verdict: OK after fixes

Faithful, natural Thai store copy. The game addresses the player with imperatives and no pronoun, and the page does the
same. It uses คุณ once, where Thai needs a subject. Glossary terms are used exactly: ที่ขูด, แปรง (แปรงหมุน for the
rotary brush), ไม้รีดน้ำ, เครื่องดูดน้ำ, น้ำยาถูพื้น, ไม้ถูพื้น, ส่งมอบ, and โบนัสความเร็ว / ไร้คราบ / แห้งสนิท / คอมโบ.
พลัง / พื้นที่ are in curly quotes, which is right because Thai has no capitals and พื้นที่ is also an everyday word.
The short description keeps the verb rhythm the source asks for (ขูด... ฉีดน้ำ... พ่นโฟม... ขัด... รีด... เป่า...) and
reads like native copy.

Spelling is clean (แห้งกรัง, กระเด็น, ระยิบระยับ, อัปเกรด and อินเทอร์เฟซ follow the Royal Institute spelling).
No text ends in a full stop, and a space separates each phrase and each list item, with the space before และ in lists.
ไม้ยมก is written the way web and marketing copy writes it (ค่อยๆ + space), the same way on the whole page.
All four headers are natural and within 24 characters: ดูลวดลายค่อยๆ กลับมา, น้ำสมจริง โคลนสมจริง, งานใหม่ พรมผืนใหม่,
รับเงิน อัปเกรดอุปกรณ์.

Register: ฟิน (in reveal_body only) is right for this page. It is the everyday Thai word for "satisfying" in ASMR and
cleaning content, and it is not slangy enough to sound cheap. It sits in body text, not in a header or the short
description. สมจริง is the standard gaming word for "realistic" (กราฟิกสมจริง). In the header it reads better than
น้ำจริง โคลนจริง, and it keeps the English meaning: a realistic simulation.

The system requirements follow Steam's Thai wording: 64 บิต, เวอร์ชัน 1909 ขึ้นไป, หรือสูงกว่า, and
ต้องใช้หน่วยประมวลผลและระบบปฏิบัติการแบบ 64 บิต.

## Changes
- paid_body: ...ความชื้นและคราบสกปรกที่ตาคุณมองข้ามไป -> ...ความชื้นและคราบสกปรกที่คุณอาจมองข้ามไป
  (fluency: ตาคุณมองข้าม, "your eyes overlook", is an English calque. In Thai a person overlooks something, not their
  eyes. ที่คุณอาจมองข้ามไป, "that you may miss", is the natural phrasing and keeps the meaning.)

## Doubts (left as written)
- headphones_h2: ใส่หูฟังแล้วตั้งใจฟัง adds "and listen closely" and sounds a little like a teacher's instruction.
  It is still natural and inviting, so changing it would be a matter of taste.
- water_body: ใต้เส้นใยทุกเส้น ("under every fibre") keeps the English spatial image. เบื้องหลังเส้นใยทุกเส้น would be a
  little more idiomatic, but the sentence is clear.
- rugs_body: แต่ละงานจะสุ่มส่วนผสม... makes "each job" the thing that randomises the mix. That is loose logically but
  normal in Thai copy.

## Meaning audit
2026-09-28, back-translation of every listed field against AUDIT.md.

- bullet_nofail: ...โบนัสความเร็วมีแต่ได้เพิ่ม ไม่มีหัก -> ...โบนัสความเร็วมีแต่ได้เงินเพิ่ม ไม่มีหักเงิน
  (ambiguity: "the speed bonus: only gets more, never cut" could be read as "the bonus only goes up and is never reduced",
  the exact error this audit hunts; with เงิน it reads "the speed bonus only gets you extra money, no money is deducted")

Checked closely and kept:
- tools_body ขัดให้โฟมซึมลึกด้วยแปรงหมุน = "scrub with the rotary brush so the foam soaks in deep" (foam, not grime);
  ไม้รีดน้ำดันโฟมและน้ำสกปรกออกนอกพรม = "the squeegee pushes the foam and dirty water off the rug".
- water_body คราบสกปรกไม่ใช่แค่ภาพที่ถูแล้วลบหายไป เพราะใต้เส้นใยทุกเส้นมีระบบจำลองของเหลวทำงานอยู่จริง = "the dirt is not
  just a picture that you rub and it disappears, because under every fibre a fluid simulation is really at work"
  (same level of claim as the English).
- rugs_body ...แบบไม่ซ้ำกัน บนลวดลายของพรมจริงจากพิพิธภัณฑ์ = "...a mix that is never the same, over the pattern of a real
  rug from a museum" ("never the same" is the natural Thai for "its own mix"; no museum visit).
- hdr_water น้ำสมจริง โคลนสมจริง = "realistic water, realistic mud" (the simulation is real; kept).
