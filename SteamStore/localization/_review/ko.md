# ko (Korean) review

Verdict: OK after fixes

Faithful, warm 해요체 copy that matches the game's register (-세요 imperatives, 빛나요 / 깨끗해요). Glossary terms are
used consistently (스크레이퍼, 브러시, 스퀴지, 습식 청소기, 전달하기 -> 전달하면, 파워 / 범위, 점검 램프, pay-slip lines in
Korean single quotes). The short description has the intended verb rhythm and reads like native Steam copy. All four
headers are natural and within 13 characters (무늬가 되살아나는 순간, 진짜 물, 진짜 진흙, 의뢰마다 새로운 카펫,
돈 벌고 도구 강화). System requirements follow Steam's Korean wording (64비트 프로세서와 운영 체제가 필요합니다).

## Changes
- bullet_nofail: 적도, 실패도, 시간 초과도 없어요. 속도 보너스는 더해지기만 할 뿐 깎이지 않아요 -> 적도, 실패도, 시간 초과도 없어요. 속도 보너스는 추가 보상일 뿐, 늦는다고 돈이 깎이지는 않아요
  (accuracy: 깎이지 않아요 could be read as "the bonus never shrinks", which is false: the speed bonus falls to 0 as time
  passes; the point is that being slow never costs money)
- tools_body: 스크레이퍼로 말라붙은 딱지를 깨고 -> 스크레이퍼로 딱딱하게 굳은 진흙을 깨고
  (fluency: 딱지 first means a scab, a dictionary calque of "crust"; the game's crust is hardened dried mud)
- alt_clip3_tools: 빨간 카펫의 말라붙은 진흙 딱지를 깨뜨리자 -> 빨간 카펫의 딱딱하게 굳은 진흙을 깨뜨리자
  (same reason as tools_body, kept consistent)
- water_body: 섬유 한 올 한 올 아래에서 유체 시뮬레이션이 돌아가고 있어요. -> 섬유 한 올 한 올까지 유체 시뮬레이션이 돌아가고 있어요.
  (fluency: 아래에서 is a spatial calque of "under every fibre"; 까지 = down to every single fibre)
- rugs_body: 엎지른 얼룩 -> 엎지른 자국
  (collocation: one spills a drink, not a stain; 엎지른 자국 = spill marks, and the game's spills are tea / wine / oil /
  grass, so no specific drink was named)
- headphones_body: 모든 기계 소리는 실제 기계를 녹음했어요. -> 모든 기계 소리는 실제 기계에서 녹음했어요.
  (grammar: "the sounds recorded a real machine"; 에서 makes it "recorded from real machines")

## Doubts (left as written)
- rugs_body: 그 아래 무늬는 박물관에 소장된 실제 카펫이에요 equates the pattern with the carpet; loose but common in
  spoken-style copy and clear, so kept.
- alt_clip5_paid: 합계 $120이 — the particle depends on how a screen reader voices $120 (달러 -> 가); alt text only and
  the brief says to keep $120 as written, so kept.
- headphones_h2: 헤드폰을 준비하세요 is fine; 헤드폰을 쓰고 들어 보세요 would be a more direct invitation but is a taste call.

## Meaning audit
2026-09-28, back-translation of every listed field against AUDIT.md.

- water_body: 섬유 한 올 한 올까지 유체 시뮬레이션이 돌아가고 있어요. -> 털 속 깊은 곳까지 유체 시뮬레이션이 돌아가고 있어요.
  (over-claim: "a fluid simulation runs right down to every single strand" says each fibre is simulated on its own; the
  source only says a fluid simulation runs under the fibres. Now "a fluid simulation runs deep into the pile", with 털 as
  in the rest of the page.)

Checked closely and kept:
- tools_lead 10가지 도구, 순서가 핵심이에요 = "ten tools, the order is the key" (there is an order that matters; not forced).
- tools_body 찌든 때에 거품을 올린 뒤 회전하는 브러시로 문질러 주고 = "put foam on the grime, then scrub with the rotating
  brush" (scrub, not "into the fibres").
- water_body 겉에 씌운 그림을 지워 나가는 방식이 아니에요 = "it is not a matter of erasing a picture laid over the surface".
- bullet_nofail 속도 보너스는 추가 보상일 뿐, 늦는다고 돈이 깎이지는 않아요 = "the speed bonus is only an extra reward; being
  slow does not cut your money".
- headphones_body 모든 기계 소리는 실제 기계에서 녹음했어요 = "all machine sounds were recorded from real machines" (machines only).
