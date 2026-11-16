# ja (Japanese) review

Verdict: OK after fixes

The translation is faithful and reads like native Japanese Steam copy: plain friendly register with しよう invitations
that matches the game (道具を強化しよう, ピカピカに仕上がった！), glossary terms used throughout (スクレーパー, 湿式掃除機,
スクイージー, 引き渡す, パワー / 範囲, 点検ライト, pay-slip lines in 「」). The short description has a good rhythm and
stays within a natural CJK length. All four headers are natural and within 13 characters
(模様がよみがえる瞬間 10, リアルな水、リアルな泥 11, 毎回ちがうカーペット 10, 稼いで道具を強化しよう 11).

## Changes
- bullet_nofail: 敵なし、ゲームオーバーなし、時間切れなし：スピードボーナスは増えるだけで減ることはない -> 敵なし、ゲームオーバーなし、時間切れなし：スピードボーナスは上乗せされるだけで、減額はなし
  (accuracy: the old text said the bonus itself only ever grows and never shrinks, which is wrong; the source means the
  bonus is only ever added to the pay and nothing is ever deducted)
- water_body: 繊維の一本一本の下で、流体シミュレーションが動いている。 -> 繊維の一本一本に至るまで、流体シミュレーションが動いている。
  (fluency: 「一本一本の下で」 is a spatial calque of "under every fibre" and reads oddly; 「に至るまで」 is the natural
  way to say the simulation goes down to every single fibre)

## Doubts (left as written)
- water_body: 汚れは表面に描かれた絵を消していくだけの演出ではない is a loose topic-comment construction (the dirt is not
  the effect; erasing it would be), but it reads acceptably in marketing copy and carries the "not a mask being erased" idea
  clearly, so it was kept.
- bullet_tools: 床用の洗剤とモップ uses the glossary name 洗剤 for the detergent sprayer; in context it is clear enough.

## Meaning audit
2026-09-28, back-translation of every listed field against AUDIT.md.

- water_body: 繊維の一本一本に至るまで、流体シミュレーションが動いている。 -> 毛足の奥まで、流体シミュレーションが行き渡っている。
  (over-claim: "a fluid simulation runs right down to each individual fibre" says every fibre is simulated on its own;
  the source only says a fluid simulation runs under the fibres. Now "a fluid simulation reaches deep into the pile".)

Checked closely and kept:
- tools_body 汚れに泡をのせて、回転するブラシでこすり込んだら: the omitted object of こすり込む is the を-object 泡
  (汚れ is only the に-target), so the brush rubs the foam in, not the grime.
- tools_lead 10種類の道具を、正しい順番で。 = "ten kinds of tools, in the right order" (an invitation, not a forced order).
- water_body 汚れは表面に描かれた絵を消していくだけの演出ではない = "the dirt is not just an effect of erasing a picture drawn
  on the surface" (the not-a-picture idea; it does not say nothing is removed).
- headphones_body マシンの音はすべて実機の録音 = "the machines' sounds are all real-machine recordings" (machines only).
- bullet_nofail スピードボーナスは上乗せされるだけで、減額はなし = "the speed bonus is only added on top, nothing is deducted".
