# Pause menu, credits and legal files (2026-09-27)

Added for the first Steam build (`RELEASE_CHECKLIST.md` 2 and 4). Two release blockers were open: the CC BY sounds need
their credits **inside the game** (`AUDIO_CREDITS.md` 1), and a fullscreen game with no way out but Alt+F4 does not read
as a finished product. The game had no menu at all before this.

## 1. What the player gets

- **Esc**, or the **Menu** button (third in the top-right row, after Upgrades and Reset), opens a modal card:
  - **Resume** (Esc): the emerald CTA.
  - **Volume**: master volume, 0-100 %, `AudioListener.volume`, saved in `PlayerPrefs` `refluff.masterVolume` and applied
    in `PauseMenu.Awake` on every launch. The M key still mutes the tool sounds on its own (`ToolAudio.muteKey`).
  - **Language** (2026-09-27): a drop-down of the 31 languages, each in its own name and script; a pick re-texts the whole
    HUD at once and is saved (`PlayerPrefs` `refluff.language`). Esc closes the open list first. `LanguagePicker`,
    `LOCALIZATION.md`.
  - **Credits**: a second page, 640 px wide, scrolling. Back / Esc returns to the main page.
  - **Quit to desktop**: `PlayerPrefs.Save()`, then `Application.Quit()` (ignored in the Editor, logged as `[PauseMenu] quit to desktop`).
  - A note under the buttons: money and upgrades are saved as you play (true: `Economy` persists on every change, `ECONOMY.md` 3).
- While the menu is open the simulation pauses (`CarpetSimulation.UiPaused`) and the tools stop (`ToolController.UiModal`),
  exactly as with the shop. Clicking the dimmed scrim resumes.

## 2. Code

| File | Role |
|---|---|
| `Scripts/UI/PauseMenu.cs` | component on `CarpetUI`, `[DefaultExecutionOrder(228)]` |
| `Scripts/UI/LanguagePicker.cs` | the Language drop-down, owned by `PauseMenu` (`LOCALIZATION.md` 3) |
| `UI/CarpetHud.uxml` | `#menu-button` in `.top-right__row`; the `#menu` layer after `#shop`, before `#fx` |
| `UI/CarpetHud.uss` | section "pause menu": `.menu*`, `.credits*`, the slider and scroller restyled for the white card |
| `UI/Credits.txt` | the credits text (TextAsset `PauseMenu.credits`) |
| `Editor/CarpetSceneBuilder.cs` | `BuildUI` adds `PauseMenu` and wires `sim`, `tools`, `economyHud`, `uiAudio`, `credits` (`CreditsPath`) |
| `Scripts/UI/EconomyHud.cs` | ignores U / Space / Esc while the menu is open |

Shop and menu never stack:
- `PauseMenu` runs at 228, `EconomyHud` at 230. The Esc that closes the shop is seen by the menu first while `ShopOpen` is
  still true, so it does not open the menu in the same frame.
- `SetOpen(true)` refuses while the shop is open; U is ignored while the menu is open; each one's scrim covers the
  other's button.
- Both ignore Esc while the Steam overlay is up (`ToolController.Suspended`).

`UI/Credits.txt` format: one Label per line; a line starting with `# ` is a heading (teal caption, uppercased); blank
lines are skipped (the headings carry the spacing). USS has no `:first-child`, so the code tags the first heading
`credits__head--first`. Headings and the prose lines are translated (their uppercase / exact text is a key of
`Resources/Loc`); attribution lines have no key and stay English, left to right and in Inter in every language
(`LOCALIZATION.md` 2). Editing a translated line changes its key: update `Tools/loc/strings.json` and the 30 files, or
it shows in English.

## 3. Files next to the exe (SteamBuild)

`SteamBuild.Build` (release and local test) now writes two files into the build root and fails if either source is
missing:

- `CREDITS.txt`: `UI/Credits.txt` with CRLF line ends.
- `THIRD_PARTY_NOTICES.txt`: `Tools/Steam/THIRD_PARTY_NOTICES.txt`, written by `python Tools/Steam/make_notices.py` from
  the package cache. It holds the full licence of every third-party library in `Refluff_Data/Managed`:
  - Steamworks.NET (MIT);
  - Unity glTFast (Apache 2.0, full text);
  - Newtonsoft.Json (MIT, through Unity's package notices);
  - MCP for Unity runtime (MIT, "Copyright (c) 2025 CoplayDev"; the package ships no licence file, so the text was read
    from its GitHub repo on 2026-09-27).

`SteamBuild` also lists the managed dlls after every build. One that is neither engine (`UnityEngine*`, `Unity.*`,
`System*`, `Mono.*`, `mscorlib`, `netstandard`), nor ours (`Assembly-CSharp`), nor in `KnownThirdParty` is named in a
WARNING on the result line: a new package brought a library whose notice is missing. Add it to `make_notices.py` and to
`KnownThirdParty`.

## 4. Update the credits when...

- a recording is added or swapped: `python Tools/audio_recordings.py credits` prints the block (keep the InspectorJ line
  with `www.jshaw.co.uk`, as `AUDIO_CREDITS.md` 1 has it), and it also goes into the store page's Legal Lines;
- a rug picture is added: `HANDOVER.md` 5 has the museum and object number;
- a package adds a runtime assembly: see section 3.

## 5. Verified 2026-09-27 (Play, scripted `EditorApplication.Step` frames)

- Before opening, the sim advanced 0.20 s in 10 frames. With the menu open it advanced 0.00 s in 20 frames, with
  `simPaused` and `toolsModal` both true. After closing it advanced 0.20 s in 10 frames again.
- With the shop open, `MenuOpen(true)` left the menu closed.
- Credits page: 28 lines built; the panel resolves to 640 px wide.
- `MenuVolume(0.35)` set `AudioListener.volume` to 0.35 and PlayerPrefs to 0.35; set back to 1.
- The Quit path ran once (`quits` 1), and Play mode stayed up.
- Screenshots: `Assets/Screenshots/menu_main.png`, `menu_credits.png` (`EconomyDebugApi.CaptureHud`, 2560x1440).
- Player build 0.9.0, windowed smoke run of 45 s: `[Steam] ready: app 5304930`, no exceptions, 788 MB working set. A
  `PrintWindow` frame shows the carpet, the HUD and the Menu button at 222 fps.

Debug API: `CarpetDebugApi.MenuJson()`, `MenuOpen(open, credits = false)`, `MenuVolume(v)`.

Not tested: a real Esc key press. Keyboard input cannot be faked from an eval (`unity-editor-ticking`); the Esc branch is the same
`SetOpen` / `ShowCredits` calls the tests drive.

Trap: re-importing `CarpetHud.uss` / `.uxml` during Play rebuilds the UIDocument tree, and every HUD component keeps
references to the dead elements (`CaptureHud` then says "HUD not built"). Restart Play after editing the HUD files.
