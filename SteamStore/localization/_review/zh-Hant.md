# zh-Hant (Traditional Chinese, Taiwan) review

Verdict: OK after fixes

This is genuinely written for Taiwan, not a character conversion of the zh-Hans text: the wording differs from zh-Hans
throughout, and the vocabulary is Taiwan usage (紓壓 / 超紓壓, 接案, 酬勞明細, 頑垢, 磁磚, 滑鼠與鍵盤, 介面, 支援, 顯示卡,
位元, 相容, 音效裝置, 即時, 解析度, 授權條款, 鏟雪車), with 「」 quotes and 、 in verb lists. No mainland terms found
(no 支持 / 实时 / 鼠标 / 界面 / 屏幕 / 软件 / 视频). Glossary terms are exact (刮刀, 刷子, 刮水器, 濕式吸塵器, 交付地毯, 功率 /
範圍, 檢查燈, 速度獎勵 / 一塵不染 / 完全乾透 / 完美組合, 地板). Headers are natural: 看花紋重新浮現 (7), 水是真的，泥也是真的
(10, a nice colloquial Taiwan rhythm), 每次接案都是新地毯 (9), 賺錢，升級裝備 (7).

## Changes
- short_description: 是一款建立在真實流體模擬上的地毯清潔ASMR遊戲 -> 是一款以真實流體模擬打造的地毯清潔ASMR遊戲
  (fluency: 建立在……上的 is a literal rendering of "built on"; 以……打造 is how Taiwan store copy says it, and this is the
  most visible sentence of the page)
- bullet_nofail: 沒有敵人、不會失敗，也沒有倒數計時：速度獎勵只加不減 -> 沒有敵人、不會失敗，也沒有倒數計時：速度獎勵只會加錢，不會扣錢
  (accuracy: 只加不減 reads first as "the bonus only ever goes up", which is false, since the speed bonus falls to 0 as
  time passes; the source means it only adds money and being slow never costs any)
- water_body: 每一根纖維底下，都有流體模擬在即時運算 -> 每一根纖維背後，都有流體模擬在即時運算
  (fluency: 底下 is a spatial calque of "under every fibre"; 背後 is the idiomatic "behind every fibre")

## Doubts (left as written)
- paid_body: 肉眼看不到 ("cannot see") is slightly stronger than "your eyes miss" (肉眼容易忽略); acceptable marketing licence.
- sysreq_min_graphics: 計算著色器 is Microsoft's Taiwan term for compute shader; many Taiwan gamers would also accept
  "Compute Shader" in English. Kept.

## Meaning audit
2026-09-28, back-translation of every listed field against AUDIT.md: no meaning errors.

Checked closely and kept:
- tools_body 用旋轉的刷子把泡沫刷進絨毛，最後用刮水器像鏟雪車一樣，把泥水推出地毯 = "the rotating brush scrubs the foam
  into the pile, then the squeegee pushes the slurry off the rug like a snowplough" (foam, not grime; pushes, not sucks).
- water_body 這裡的髒污不是一層擦一擦就會消失的貼圖，每一根纖維背後，都有流體模擬在即時運算 = "the dirt here is not a
  texture layer that vanishes when you wipe it; behind every fibre a fluid simulation computes in real time" (same level
  of claim as "runs under every fibre").
- bullet_nofail 也沒有倒數計時：速度獎勵只會加錢，不會扣錢 = "no countdown: the speed bonus only adds money, never deducts".
- alt_clip5_paid 蓋上「完美！」印章 = the combo stamp (完美！ is the game's COMBO! stamp, glossary).
