# Creator outreach — preparation and send record

## Current status after YouTube wave — 19 October 2026

**26 submissions sent in total: 24 Gmail + 2 official developer forms.** The
original campaign remains 18 sent (17 Gmail + CGDannyB form), 2 excluded.
After a new explicit human instruction, seven additional individual emails were
sent from vahovgames@gmail.com to Grillmastah, CozyGameNight, Kanzalone,
Z1 Gaming, Kimchica, Sarah Sunstone and Сестра Винчестер; Her Cozy Gaming's
official form accepted the eighth submission. Each received a distinct own key
and public media link, for voluntary unpaid coverage without a posting obligation.

Final Gmail Sent search for the seven exact new addresses shows 1–7 of 7.
Six individual Message sent confirmations were captured; Grillmastah's first
toast was transient, but a new Sent result after an empty pre-send search and
the final seven-address search confirm it. Do not resend. The Her form displayed
“Submission Success! I will be in touch shortly.” Its Email field was masked in
DOM; the operator supplied the exact studio address and observed server acceptance.
These confirm submission, not delivery, reading, activation or publication.

Original **31-key accounting is unchanged**: 18 transmitted, 12 available,
1 reserved/not transmitted for excluded 2BRO. The separate new 18-key pool is
8 SENT and 10 AVAILABLE. Combined: **49 supplied, 26 transmitted, 22 available,
1 reserved/not transmitted**. Access before release was directly confirmed by
the user (USER_CONFIRMED_PRERELEASE_ACCESS), not by agent activation or a new
Steamworks inspection. Existing assignments and SENT/EXCLUDED history were preserved.

Current [YouTube campaign status](../../../SteamStore/outreach/2026-10-19-youtube/START_HERE_RU.md),
[sent summary](../../../SteamStore/outreach/2026-10-19-youtube/PRIVATE_DO_NOT_UPLOAD/SENT_SUMMARY_RU.md),
[provider ledger](../../../SteamStore/outreach/2026-10-19-youtube/PRIVATE_DO_NOT_UPLOAD/SEND_RESULTS_2026-10-19.json)
and [current send QA](../../../SteamStore/outreach/2026-10-19-youtube/PRIVATE_DO_NOT_UPLOAD/SEND_QA_REPORT.json)
are authoritative for the new eight. Exact sent letters and keys stay private.
The previous preparation QA is historical; the old generator refuses to reset
assigned state. One manual follow-up may be considered from 26 October only
if unanswered; no automation or monitor exists. The other 12 new candidates
remain unsent and require their documented checks or a later selection.

## Original campaign — 19 October 2026 (Europe/Moscow)

The original list of 20 short-form creators is processed: **18 individual
submissions sent, 2 excluded**. Sender and form reply address:
**vahovgames@gmail.com**, signature Vahov Games. Both waves were explicitly
authorized by the user; those completed authorizations do not schedule further sends.

| Wave | Date | Gmail | Private developer form | Total sent |
|---|---|---|---|---|
| First eight | 2026-10-19 | 7 | 1 (CGDannyB) | 8 |
| Remaining unpaid editorial submissions | 2026-10-19 | 10 | 0 | 10 |
| Total | | 17 | 1 | 18 |

Excluded **ImJustJarrod** (published paid rate) and **2BRO.** (official channel asks
publishers not to email Steam keys). The remaining-wave letters request unpaid
editorial consideration; no paid placement or publication is agreed.

**A different key is inside each initial private letter.** Of 31 user-supplied
keys (8 + 23), 18 were sent, 12 remain unassigned and 1 is reserved but not
transmitted for excluded 2BRO. Thus 13 keys have not been transmitted; only 12
are currently unassigned. No key activation or recipient redemption was checked.
Key values stay in `PRIVATE_DO_NOT_UPLOAD`, never in project docs/public materials.

The user supplied the uploaded [public media folder](https://drive.google.com/drive/folders/1GcksmaH-uE01V4FYM04V2Q2mFInWnnBr?usp=sharing).
Drive's share dialog was checked as Anyone with the link / Viewer. Every sent
letter contains this folder link and an individual Steam UTM link. Media and
original trailer masters were not changed during sending.

Gmail confirmed Message sent for all 17 emails; CGDannyB's form confirmed
Message Sent!. These confirm submission, not delivery, reading or publication.
The private tracker records Moscow UTC+03:00 timestamps. Old personal-account
drafts are superseded; **do not resend SENT or EXCLUDED rows**. No further
eligible contact from this list remains pending. One manual follow-up may be
considered from **26 October 2026**, only if there is no reply; nothing is scheduled.

### Evidence and continuation

- [Tracker and current recipient states](../../../SteamStore/outreach/2026-10-18/PRIVATE_DO_NOT_UPLOAD/TRACKER.md);
  `TRACKER.json` is the editable record for `sent_at`, replies and next actions.
- [First-wave result](../../../SteamStore/outreach/2026-10-18/PRIVATE_DO_NOT_UPLOAD/FIRST_WAVE_SENT.md)
  and [second-wave result](../../../SteamStore/outreach/2026-10-18/PRIVATE_DO_NOT_UPLOAD/SECOND_WAVE_SENT.md).
- Private provider confirmations: `SEND_RESULTS_2026-10-19.json` and
  `SECOND_WAVE_SEND_RESULTS_2026-10-19.json`; exact second-wave letters with keys:
  `SECOND_WAVE_SENT_LETTERS/`; allocation counts: `KEY_POOL_STATUS.json`.
- [Final Gmail Sent screenshot](../../../SteamStore/outreach/2026-10-18/PRIVATE_DO_NOT_UPLOAD/second_wave_sent_2026-10-19.jpg)
  shows 17 emails. CGDannyB's success screenshot is `cgdannyb_sent_2026-10-19.jpg`
  in the same private folder. Preparation/send history is in `SESSION_LOG.md`.

## Output

`SteamStore/outreach/2026-10-18/`, outside Assets:

- `START_HERE_RU.md`: current totals and links to wave results and tracker.
- `PUBLIC_REFLUFF_CREATOR_KIT/`: English facts, eight MP4s, four store screenshots,
  three logos, audio attribution and a SHA256 manifest.
- `REFLUFF_PUBLIC_CREATOR_KIT.zip`: exactly the public folder, no contact list,
  drafts or keys. Upload this or the public folder alone, never its parent.
- `PRIVATE_DO_NOT_UPLOAD/`: 20 personal texts (18 English, 2 Japanese), 17
  local email copies with X-Unsent:1, private-form text, follow-up templates,
  contact-source report, config, editable tracker, exact sent copies and QA/status
  reports. Local .eml files are copies, not unsent Gmail work; consult SENT first.
  Obsolete DM/form routes are marked do-not-send in `superseded_contact_routes/`.

Public facts were checked against https://store.steampowered.com/app/5304930/
on 2026-10-18: Windows/Steam, planned **November 2026**, 31 interface languages.
Do not copy the old trailer's October 2026 release card into new outreach.
No exact day, launch price or embargo is invented. A key's ability to unlock
the pre-release build must be confirmed before it is actually distributed.

## Media

`Tools/steam_art/outreach/prepare_media.py` uses the existing Python 3.12 media
environment (imageio-ffmpeg, Pillow, numpy, scipy) and existing licensed museum-rug
takes; no new Unity capture or project change is needed. The running Editor's
Pipeline server was unreachable at inspection, so no live commands were sent.

- New 20 s, 1080x1920/60 clean montage: `tt2_vacuum`, `tt3_hose`, `tt4_brush`,
  `tt5_squeegee`, `tt6_dryer`, `tt7_reveal`. No captions, logo, speech or music.
  These are cinematic macro takes with 48 shells and staged lighting, explicitly
  described as such in the public kit, not claimed to be the normal game camera.
- Five 1920x1080/60 individual fixed-camera cutaways: dry vacuum 3.5 s, foam
  3.5 s, brush 3.5 s, squeegee 4 s, wet-vac reveal 4.5 s. Source 3360x1800 frames
  are center-cropped slightly to 16:9, at original 1x playback speed.
- Cutaway/montage sound comes from `foley.render_take` using the captured
  `audio.jsonl`, with short edge fades and loudness normalization. No raw sound
  library or standalone third-party recordings are distributed.
- Outreach copy of the 60 s trailer: only final shot #21 is rendered again with
  `November 2026`, replacing frames 3280..3599. Existing mix retained, image
  re-encoded for the new combined MP4. Original master and EDL unchanged.
- Existing 25.7 s captioned, no-voice vertical is copied unchanged.
- Four selected current ACES store screenshots and three transparent logo assets.
- Full appropriate audio attribution copied from `AUDIO_CREDITS.md` to the kit.

The newer human scraper film uses a temporary test picture, so it is not part of
this public museum-rug pack. Media provenance and visual review images are in
`SteamStore/_work/outreach_20261018/`.

## Draft and key workflow

Build/rebuild from project root:

```powershell
python Tools/steam_art/outreach/prepare_media.py
python Tools/steam_art/outreach/build_bundle.py
python Tools/steam_art/outreach/verify_bundle.py
```

After upload and the user's key list:

```powershell
python Tools/steam_art/outreach/build_bundle.py --media-url "https://public-folder-url" --keys-file "C:/private/keys.txt"
python Tools/steam_art/outreach/verify_bundle.py
```

Optional `--sender-email` supplies From in .eml; otherwise the user chooses the
sender in their email client. Signature defaults to Vahov Games. One key per input
line, no heading; duplicates are rejected before drafts change. The input file
is rejected if it is inside the public folder. Keys are assigned in the tracker
order, first the initial eight. For a selected later wave, use
`--recipients creator_id,creator_id` to limit allocation. Check exclusions and
existing SENT rows before preparing another wave; never reassign a sent key.

Assignments are persisted privately in `key_assignments.json`, keyed by stable
creator id, and never reassigned on rerun or input reordering. Only counts and
fingerprints appear in reports; actual key values appear only in private
assignments and letters. `sent_at`, `response`, `next_action` and extra tracker
fields survive rebuilds. No key activation, network send or upload code exists.

First wave: IndieGameJoe (email for a private key), CGDannyB (private developer
form), Becsters, steamexplays, Cozy Tea Games, The Cozy Gaming Club, JustKaalin,
CozyReni. Later contacts retain research limitations. Initial HOLD cases included
Jestr's AI policy, 2BRO's missing gated business address and JehNotJay's unverified
channels; see the dated send result below for their resolved state. A prepared
message is not proof of a current open contact route.

Initial texts include an individual Steam UTM link and the shared media-folder
link; no tracking outcome is promised. Each first message contains exactly one
own key/slot. Follow-ups omit keys, reference the first message and are prepared
for one manual nudge after seven days, not scheduled automatically.

## Verification

`verify_bundle.py` fully decodes eight videos, verifies exact dimensions/frame
counts, H.264/yuv420p, 60 fps, audio, no black intervals and no clipping. It checks
20 private draft texts, the configured single-recipient .eml copies, public archive CRC
and public file SHA256 values. Temporary fixture checks prove 20 unique
assignments, stable reruns after input reordering, duplicate rejection without
assignment mutation and exactly one recipient's own key in each letter. Fixture
keys never enter campaign files. Real keys and public link are stored privately
when the user supplies them. Review the generated `QA_REPORT.json` for actual results.

## Send result — 19 October 2026

The user authorized sending the remaining unpaid editorial submissions and
supplied 23 new keys. Sent 10 additional individual messages from
`vahovgames@gmail.com`: Cozy K, Payton'sCorner, Papa Pupp, Eeowna, ChromaGlitch,
Eggmilk, Manechiku/unauyo (Japanese), Jestr (team business address), JehNotJay
and JubileeWhispers. Each contains exactly its own key and the shared media
link, explicitly requests unpaid editorial consideration and imposes no posting
obligation. Jestr and JubileeWhispers letters disclose the Tripo tool models.

Verified new contacts on their own pages: Eggmilk's Carrd, Manechiku's WordPress
About, Jestr's website linked from u/addit02, JehNotJay's YouTube linked from
their Reddit/Linktree, and JubileeWhispers' YouTube About after the user completed
CAPTCHA. No keys were posted to public comments or sent through social DMs.

Excluded ImJustJarrod (published paid rate) and 2BRO (official YouTube description
asks publishers not to email Steam keys). Total: **18 provider-confirmed sends**
(17 Gmail + 1 CGDannyB form), 2 excluded. Gmail Sent search shows 17 messages.
This does not prove delivery, reading, key activation or publication.

Private evidence: `SECOND_WAVE_SEND_RESULTS_2026-10-19.json`, exact sent copies in
`SECOND_WAVE_SENT_LETTERS/`, `SECOND_WAVE_SENT.md`, updated tracker and screenshot
`second_wave_sent_2026-10-19.jpg`. No key values in docs/public files. Of the 23 new
keys, 10 were sent, 12 remain unassigned, and 1 remains reserved but not transmitted
for excluded 2BRO; no previous assignments were rotated. Follow-up remains manual,
no earlier than 26 October if there is no reply. No scheduling or monitoring set up.

## YouTube preparation before sending — 19 October 2026 (historical)

A separate, unsent campaign is prepared in
[2026-10-19-youtube](../../../SteamStore/outreach/2026-10-19-youtube/START_HERE_RU.md).
The [Russian report](../../../reports/YouTube%20авторы%20для%20Refluff.md) lists
20 new YouTube candidates with linked fit evidence and contact/readiness limits.
Seven verified email contacts form the proposed initial wave: Grillmastah,
CozyGameNight, Kanzalone, Z1 Gaming, Kimchica, Sarah Sunstone and Сестра Винчестер.
Grillmastah's address is VERIFIED_USER_ABOUT: the user copied the revealed
official About email after CAPTCHA; the agent did not directly observe its value.
Her Cozy Gaming's official form is separate. Nema Lass remains on contact
hold. СТОДНЕВКА's official Telegram “Предложить игру” is not an
email. Miss Bubbles and FazyGaming remain reserve; CozyAriGames is POLICY_REVIEW
with Tripo tool-model disclosure, not an invented blanket refusal of AI assets.

The user's format is a complimentary key and optional unpaid editorial review,
with no posting obligation or positive-review condition. Seven local email texts
and one form text contain unique
recipient placeholders, not real keys. No new send, service draft, form submission,
Telegram message, key allocation or activation was performed.

The original **31-key pool is unchanged**: 18 transmitted, 12 available,
1 reserved/not transmitted for excluded 2BRO. A separate private intake holds
18 additional user-supplied keys, all valid, unique and AVAILABLE, with zero
overlap with the old 31. Combined accounting is **49 supplied, 18 transmitted,
30 available, 1 reserved/not transmitted**. The new pool does not supersede or
rotate old assignments; original SENT/EXCLUDED history remains authoritative.
Only private files hold values. New preparation/QA:
[CANDIDATES.json](../../../SteamStore/outreach/2026-10-19-youtube/PRIVATE_DO_NOT_UPLOAD/CANDIDATES.json),
[QA_REPORT.json](../../../SteamStore/outreach/2026-10-19-youtube/PRIVATE_DO_NOT_UPLOAD/QA_REPORT.json).
The existing public media folder is reused; release copy says November 2026,
without inventing a day, price or “AI-free” claim. Nothing is scheduled.
Before a real send, confirm that the supplied key type unlocks the intended
pre-release build, then assign a distinct key to each recipient and verify the
completed letters. Key access was not tested and the agent activated no key.
