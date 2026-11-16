# Between the carpet fibers — 2026-10-23

**Current master: English narration + logo/CTA revision 3**, `SteamStore/tiktok/between_fibers/revision3/refluff_between_fibers_en_v3.mp4`.
The owner requested English narration as a standing preference, the actual Refluff logo, Wishlist on Steam and
Coming this November. The traditional red rug and real two-second dry-vac action are retained. Earlier masters
and raw takes remain available; see revision notes below.

Requested story: camera between fibers, curious Russian voice “Хм… где это я?”, surprised realization
“Да я же между ворсинок ковра в своей игре!”, pullback, a clean rug and a two-second dry-vac pass.
Publication on TikTok, Instagram and YouTube requires the owner's approval of the finished video and exact details.

## Master and timeline

`SteamStore/tiktok/between_fibers/refluff_between_fibers_v1.mp4`: 12.000 s, 1080×1920, 60 fps / 720 frames,
H.264 High / yuv420p / BT.709, AAC stereo 48 kHz, 12,601,540 bytes.
SHA-256 `820cf109104834b0d87fdfde07fa4dd93dea08cfe5d1e1dd1f1d0991b7a774c4`.

- 0–2.6 s: static between-fibers view, near clip 0.3 mm. First phrase starts at 0.3 s.
- 2.6–6.2 s: pullback to medium macro; surprised phrase starts at 2.6 s.
- 6.2–6.4 s: dry-vac pointer approaches; 6.4–8.4 s: exactly 120 active frames, UV (.64,.45) → (.36,.45).
- 8.4–9.5 s: dry pile springs back; 9.5–11.35 s: pullback to full rug; end hold to 12 s.
- Russian subtitles end at 5.75 s. Refluff end title appears at 10 s.

The current user-supplied picture is retained; `Reset(394,0)` initializes the spotless rug. Dry-vac tool 1 has
no 3D model in the current game: the actual visual is the suction ring (FIBER_SET.md §6). At the end of the
stroke it leaves the left edge; central suction, the laid trail and recovery remain visible.

## Capture and rebuild

Recipes: `Tools/steam_art/tiktok/between_fibers/stage_parent.cs`, `take_parent.cs`, `restore_parent.cs`.
Run through `unity command eval_file`, without runtime code changes. The take script guards against overwriting
the original: choose a new name/folder before another recording.

The free-camera macro uses a temporary **384-shell mesh** to reduce visible spacing between the game's 48
layers at millimeter distances. Pile height, strand distribution, picture and simulation parameters are unchanged.
This is extra capture rendering detail. The original mesh/camera/light state is saved in AppDomain and restored;
no scene/material/settings assets or release build are changed. Editor was restored and stopped afterwards.

Raw: `SteamStore/_work/clips/between_fibers_20261023_v1/`: `f0001.png`–`f0720.png`, `seq.json`, `audio.jsonl`,
`audio_clips.json` and dumped clips. StoreGifApi runs actual EditorApplication.Step at 60 fps. Camera pose is hooked
to beginContextRendering and keyed to the take's step counter. SeqEnd: 720 frames/steps, pending PNGs 0, errors 0.

Voice: Syntx / Eleven v4, **Agassi Roux - Friendly and Engaging**, `[curious]` / `[surprised]` tags, cost 0.8
existing account tokens. Original 5.2 s MP3: `voice/syntx_ru_original.mp3`; trims/gains: `voice/timing.json`.
Speaking speed/pitch unchanged; only the silence between phrases was extended. Local multilingual base Whisper
supports timing but misrecognizes parts of phrase 2; it is not proof of perfect pronunciation. Owner listening
review remains part of approval.

`compose.py --take SteamStore/_work/clips/between_fibers_20261023_v1` reconstructs authentic foley with
`trailer/foley.py`, mixes narration, creates PIL subtitles and exports `assembled_v1.mp4`. Python:
`C:/Users/denis/AppData/Local/Python/bin/python.exe`; scipy is scoped to the recipe's `.deps`.
Social Studio `render --manifest SteamStore/tiktok/between_fibers/render_social_studio_v1.json` creates the master.
`compose.py --qa <final>` writes contact sheets, full-decode, stream and loudness reports in `qa/`.

Sound: InspectorJ Dyson recording (CC BY 4.0) + crcavol carpet drag (CC0); no music. All captions include exact
InspectorJ title, creator, source, license URL and adaptation note. The CC BY-NC Samsung recording is absent.
One logged tool-switch one-shot has volume 0, as in the game.

## Verification and publication state

Final full A/V decode passed; 720 frames, 12.000 s video/audio edits, 60/1 fps, SAR 1:1. AAC measures
−15.90 LUFS / −1.50 dBTP, all samples finite, no clipping. Report/contact sheet:
`qa/refluff_between_fibers_v1_report.json`, `qa/refluff_between_fibers_v1_contact_sheet.jpg`.
Source/finished frames inspected for intro, pullback, suction, trail, recovery, subtitle legibility and ending.
Independent frame review found no blocker for presentation; final quality awaits owner review.

Local Social Studio game `refluff`, one Clip and three **draft** posts registered in the real runtime database.
IDs/hash: `social_studio_records.json`. TikTok/YouTube @vahovgames, Instagram @vahov_games.
Exact captions: `caption_tiktok.txt`, `caption_instagram.txt`, `caption_youtube.txt`; review copy `post_details_ru.txt`.
Proposed visibility: public, immediately after approval, Europe/Moscow. No approval, upload, publication or schedule
was performed. Changed video/text/target requires new approval.

## Revision 2 — traditional red rug, 2026-10-23

Re-shot the exact 12 s timeline with bundled `Textures/rug_transylvanian_medallion.jpg` (museum CC0,
HANDOVER.md §5), retaining the original Syntx read, captions, camera keys, 384 capture shells and real two-second
vacuum action. The aligned voice WAV is byte-identical to v1. No hue/color replacement in editing.

`select_red_v2.cs` snapshots the runtime material's picture and fit, assigns the red texture with `PictureFit`,
then `take_red_v2.cs` records to a fresh `between_fibers_20261023_v2_red` folder. After capture,
`restore_red_v2.cs` restores the original texture/fit; `restore_parent.cs` restores mesh/camera/light, then Play stops.
The active picture override folder, catalogue, scene/material/settings assets are unchanged.

Raw SeqEnd: 720 frames/720 steps, 12.00 s, pending PNGs 0, errors 0. Source frames, suction, laid trail, recovery,
full-rug ending and final subtitle/title contact sheets inspected. `compose_red.py` and
`revision2/render_social_studio_red_v2.json` reproduce the edit without overwriting v1.

Final: 29,910,020 bytes, SHA256 `330973c31bdd1c79d6f828473bb8b16547438e8eb40eb74cf5c9f82ba17bf601`.
1080×1920/60, 12.000 s, 720 frames, full A/V decode passed; AAC −15.90 LUFS/−1.50 dBTP, finite, no clipping.
QA is under `revision2/qa`. Existing Social Studio Clip now points to v2; all three original post records remain
draft, with null approvedHash and URL. Text/account/public/immediate-after-approval details remain as previously
presented. Nothing approved, uploaded, scheduled or published.

## Revision 3 — English voice and branded packshot, 2026-10-23

Standing user preference: **all narration must be English**, even when the chat/story brief is Russian.
Saved in TIKTOK.md and the shared SocialStudio.md guide for future videos. The owner explicitly requested the
November release claim for this packshot; this supersedes earlier video text, without editing Steamworks settings
or asserting that the old release checklist has been independently rechecked.

Generated a new 6.240 s English read in Syntx Eleven v4, **A Carlisle - Charismatic Storyteller**, English catalogue
filter, 0.8 existing account tokens. Source and metadata are in `revision3/voice/`:
“Hmm… where am I? Wait… I'm between the carpet fibers… in my own game!”
Both source and assembled-video English ASR recovered every word. Speaking speed/pitch remain unchanged;
removed only 0.26 s of near-silence after “Wait” to finish narration at 6.34 s, before suction starts at 6.4 s.
Exact source cuts, removed gap and English caption times: `voice/timing_en_config.json` and `voice/timing.json`.

Reuses the untouched red-rug 0–12 s take. The actual last frame holds from 12–14 s; gameplay is not retimed.
The last three seconds (11–14) carry approved transparent `SteamStore/logo/refluff_logo.png`, **Wishlist on Steam**
and **Coming this November**, with a short fade and local dark gradient over the upper tile area. Elements fit
outside the right-button/bottom-caption zones. No new Unity capture, scene or saved gameplay changes.

Build: `compose_en_v3.py --take SteamStore/_work/clips/between_fibers_20261023_v2_red`, then SocialStudio
`render --manifest SteamStore/tiktok/between_fibers/revision3/render_social_studio_en_v3.json`.
QA: the same Python script with `--qa <final>`; reports/contact sheets in `revision3/qa/`.

Final master: 14.000 s, 840 frames / 60 fps, 1080×1920 H.264/AAC, 30,730,142 bytes.
SHA256 `0ca66e84f524d5102f93fbdb13b41484bc2ddc1614b31e1cb18f030b0b76b692`.
Full decode passed; final AAC −15.70 LUFS / −1.47 dBTP, finite, no clipping. English caption states, preserved
two-second suction, recovery, true logo, CTA/release text and final hold inspected in source/final contact sheets.

Existing Clip points to revision 3. Same three draft posts now have English titles/captions with the November
claim, Steam link and full InspectorJ attribution (`revision3/caption_*.txt`, `post_details_en.txt`). Accounts,
public visibility and immediate-after-approval timing remain unchanged. All approved hashes/remote URLs remain
null; no upload, approval, publication or scheduling occurred. New video and texts still need owner approval.
