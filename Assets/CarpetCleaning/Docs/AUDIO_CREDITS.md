# Audio credits and licences

Every real recording the game plays, where it came from and what its licence asks for. The cuts, levels and the
reasons behind them are in `AUDIO.md` 11. This list is generated from `Tools/audio_recordings.py`:
`python Tools/audio_recordings.py credits` prints the block below, and `list` prints the table.

## 1. What a release must carry

Paste this block into the game's credits screen and the store page's legal notes. CC BY needs the author, the title,
a link and the licence, and it asks you to say that the sound was changed (all of them were cut and levelled).

```
Sound effects from Freesound.org, cut and levelled for this game:
"bissell_carpet_cleaner_vacuum_01.wav" by joedeshon (https://freesound.org/s/79236/), licensed under CC BY 4.0
"loud metal fan turned on and off a few times (machine starting up)" by busabx (https://freesound.org/s/726634/), licensed under CC BY 4.0
"Floor Buffer Sound" by prometheus888 (https://freesound.org/s/864976/), licensed under CC BY 4.0
"shovelling scraping water" by adda_records (https://freesound.org/s/519974/), licensed under CC BY 4.0
"26_Fregona_Agua.mp3" by byronabadia (https://freesound.org/s/444946/), licensed under CC BY 4.0
"Whipped Cream Spray Can" by kev_durr (https://freesound.org/s/321055/), licensed under CC BY 4.0
"BerberCarpetScratching.wav" by akent (https://freesound.org/s/178499/), licensed under CC BY 3.0
"Vacuum Cleaner, On Idle Off, Close, A.wav" by InspectorJ (www.jshaw.co.uk, https://freesound.org/s/430632/), licensed under CC BY 4.0
Licences: https://creativecommons.org/licenses/by/4.0/ and https://creativecommons.org/licenses/by/3.0/
Additional sounds: Mixkit (mixkit.co) and Freesound users kyles, richwise, RossJuterbock, crcavol, yehdawgo (CC0).
```

The last line is a courtesy: CC0 and the Mixkit licence ask for nothing.

**The dry vacuum's machine is InspectorJ's Dyson since 2026-09-26** (Freesound 430632, CC BY 4.0, credit line in the
block above). It replaced the Samsung take (Debsound, Freesound 100210, **CC BY-NC 4.0**, non-commercial only, `AUDIO.md`
9.6), which stays on disk as a reference only: it is not in `Tools/audio_recordings.py`, not in the bank, and must never
ship or appear in a trailer. `SteamBuild` still warns in its result line if any NC credit comes back into the bank
(machine takes and `clipCredits` both).

## 2. Every recording

| Tool | Layer | Freesound / Mixkit | Author | Licence |
|---|---|---|---|---|
| Scraper | surface | 178499 BerberCarpetScratching.wav | akent | CC BY 3.0 |
| Dry vacuum | machine (since 2026-09-26) | 430632 Vacuum Cleaner, On Idle Off, Close, A.wav | InspectorJ | CC BY 4.0 |
| Dry vacuum | surface | 154630 Dragging_On_Carpet.aif | crcavol | CC0 |
| Dry vacuum | machine until 2026-09-26, reference only (never ship) | 100210 Vacuum Cleaner-Samsung Easy 1300.wav | Debsound | **CC BY-NC 4.0** |
| Hose | machine | 580534 Pressure Washer | richwise | CC0 |
| Foam sprayer | machine | 321055 Whipped Cream Spray Can | kev_durr | CC BY 4.0 |
| Brush | motor loop | 864976 Floor Buffer Sound | prometheus888 | CC BY 4.0 |
| Brush | power on / off | 451933 switch big breaker metal click on, off.flac | kyles | CC0 |
| Brush | surface | 543471 Hair brush on carpet MSH-6.wav | RossJuterbock | CC0 |
| Squeegee | surface | 519974 shovelling scraping water | adda_records | CC BY 4.0 |
| Wet vacuum | machine | 79236 bissell_carpet_cleaner_vacuum_01.wav | joedeshon | CC BY 4.0 |
| Wet vacuum | surface | 720073 Slurping Water | yehdawgo | CC0 |
| Dryer | machine | 726634 loud metal fan turned on and off a few times | busabx | CC BY 4.0 |
| Detergent | machine | Mixkit 3215 Spray water or liquid | Mixkit | Mixkit Sound Effects Free License |
| Mop | surface | 444946 26_Fregona_Agua.mp3 | byronabadia | CC BY 4.0 |

Still synthesised, and so ours: the crumb rattle in both vacuum tubes (kept on request, `AUDIO.md` 8), the tool-switch
tick and the UI sounds (`AUDIO.md` 10).

## 3. The licences in one line each

- **CC0** (Creative Commons Zero): public domain. No credit and no conditions.
- **CC BY 3.0 / 4.0**: any use, commercial included, with a credit (author, title, link, licence) and a note that the
  sound was changed. The credit has to appear where credits are expected: the game's credits and the store page.
- **Mixkit Sound Effects Free License** (https://mixkit.co/license/#sfxFree, read 2026-09-25): free in commercial
  and non-commercial projects, video games named explicitly, no credit required. Three limits:
  - no redistributing the sound on its own, as stock or in a tool or template (inside the game build is fine; a sound
    pack is not);
  - no claiming it as your own;
  - no registering it with a rights-management service (Content ID and the like).
- **CC BY-NC**: no commercial use at all. That rules it out of a paid game. Only the Samsung take is under it, and it was swapped out on 2026-09-26.

Every licence was read on the sound's own Freesound page when it was chosen, not taken from the search filter. That
check caught one problem: Freesound 371715 ("F6_hose.mp3", tagged CC0) matches 323485 (meisterjaan, CC BY 3.0) almost
exactly (waveform correlation 0.91), so it looks like a re-upload. It was not used. Before adding a sound from Freesound,
read the licence on its page.

## 4. Where the files are

- `Audio/Sources/<id>__<author>__<title>.ogg`: the sources as downloaded (Freesound HQ previews, the Mixkit WAV). They
  are referenced by nothing, so never in a build. They are kept so every cut can be redone.
- `Audio/Recorded/*.wav` + `*.json`: the clips the bank plays. Each JSON records the source, the cut and the credit.
- `Settings/ToolSoundBank.asset`: `recording.credit` per machine take and `clipCredits` per entry.
  `ToolSoundBaker.Credits()` prints them all.
