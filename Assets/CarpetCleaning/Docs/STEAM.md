# Steam integration (2026-09-23)

User request: integrate the Steam SDK so the game can be uploaded to Steam. Scope agreed: core Steamworks (init,
relaunch through Steam, overlay), Rich Presence, a Windows build pipeline and SteamPipe upload scripts.
2026-09-30: Steam achievements + stats (`ACHIEVEMENTS.md`) and Steam Cloud for the save (section 11).

Wrapper: **Steamworks.NET 2025.164.1** (Steamworks SDK 1.64), UPM git package
`com.rlabrecque.steamworks.net` in `Packages/manifest.json`. It ships `steam_api64.dll` for Windows x64 and
compiles on Windows/Linux/macOS/Android standalone only; our scripts use the same `DISABLESTEAMWORKS` guard.

## 1. Ids and where they live

| What | Value | Where |
|---|---|---|
| App ID | 5304930 | `Scripts/Steam/SteamConfig.cs` `AppId`, `steam_appid.txt` (project root), `Tools/Steam/app_build_5304930.vdf` |
| Windows depot | 5304931 | `SteamConfig.DepotId`, `Tools/Steam/depot_build_5304931.vdf`, the `Depots` block of the app script |

The depot id is Steamworks' default (App ID + 1). Check it in Steamworks > SteamPipe > Depots, and rename the
depot script if it differs. Change every place in the table together.

`steam_appid.txt` (ASCII, no BOM, no newline) lets `SteamAPI.Init` work when the process was not started by
Steam: the Editor, and `Builds/Steam/local`. **It must never ship.** `SteamBuild.BuildRelease` deletes it, and the
depot script excludes it as well.

## 2. SteamService (`Scripts/Steam/SteamService.cs`, order -1000, on the `Steam` object)

- `Awake`:
  1. Player only: `SteamAPI.RestartAppIfNecessary(5304930)`. When the exe was started directly and there is no
     `steam_appid.txt` next to it, Steam starts the game again through the client and this copy calls
     `Application.Quit()`.
  2. `Packsize.Test`, `DllCheck.Test`.
  3. `SteamAPI.InitEx`. On failure the reason goes to `LastError` and the log, and **the game runs on without
     Steam**. This is a soft dependency, not DRM.
  4. Warning hook, `Callback<GameOverlayActivated_t>`.
- Statics: `IsReady`, `OverlayActive`, `RunningAppId`, `PersonaName`, `LastError`, `Instance`. They are reset in
  `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` because domain reload is disabled.
- `Update`: `SteamAPI.RunCallbacks()`.
- `OnDestroy`: releases an overlay pause, calls `ClearRichPresence`, disposes the callback, then `SteamAPI.Shutdown()`.
  In the Editor this runs on every Play stop, so the next Play inits again cleanly (verified).
- `initInEditor` (default on): turn it off to work in the Editor without the Steam client. The game then behaves
  exactly as when Steam is missing.
- Log lines: `[Steam] ready: app 5304930, user <name>` / `[Steam] init failed, running without Steam: <reason>` /
  `[Steam] shut down`. In a player they go to `%USERPROFILE%/AppData/LocalLow/VahovGames/Refluff/Player.log`.

## 3. Overlay pause

`SetOverlay(bool)` runs from the overlay callback, and from `CarpetDebugApi.SteamSimulateOverlay`. When the
overlay opens:

- `CarpetSimulation.Paused = true`. The floor sim follows `carpet.Paused`.
- `ToolController.Suspended = true`. This takes the same branch as `InputEnabled == false`: the stroke ends, the
  hotkeys 1-8/R and the wheel are ignored, and `ToolAudio` spins down because `IsStroking` goes false.
- `CarpetHandover` ignores H/N. The flight animation still finishes.

The Input System keeps receiving input under the overlay, which is why the tools need this gate.

`Suspended` is a separate flag because the hand-over owns `InputEnabled`. When the overlay closes, only a pause
this class set is lifted, so a pause set by anything else survives.

The real overlay only draws in a process that Steam launched: it is injected before the D3D12 device exists. In
the Editor, or when the exe is run directly, Shift+Tab usually shows nothing. Test the real overlay on a build
installed through Steam.

## 4. Rich Presence (`Scripts/Steam/SteamRichPresence.cs`, order 220)

The component polls the game state every frame. It pushes to Steam only when the state changed, and at most once
per `minInterval` (2 s, real time).

| Game state | `steam_display` | `clean` |
|---|---|---|
| hand-over `Idle`, not won | `#Status_Cleaning` | `"72%"` (floor of `Cleanliness*100`) |
| `Idle` and `sim.IsWon` | `#Status_Spotless` | "" |
| `FlyOut` / `Away` | `#Status_BetweenCarpets` | "" |
| `FlyIn` | `#Status_Cleaning` | new carpet's % |

The localisation lives in `Tools/Steam/rich_presence_<steam api language>.vdf`. English and Russian were **uploaded
2026-09-27** (App Admin > Community > Rich Presence, both answered "Saved") and published. The other 29 languages were
written the same day with the game's localization (`LOCALIZATION.md`) and still have to be uploaded + published; each
file names its language (`"Language" "koreana"`, `"latam"`, `"schinese"`...) and keeps `%clean%`. `python
Tools/loc/loc_tool.py check` validates all 31. Change a vdf -> upload it again -> Publish.

Until then, Steam discards the token: reading our own presence back gives `clean=72%` but an empty
`steam_display` (verified 2026-09-23), and friends see only "Playing CarpetCleaning". After uploading, check it
on https://steamcommunity.com/dev/testrichpresence while the game runs.

## 5. Builds (`Editor/SteamBuild.cs`, menu CarpetCleaning > Steam)

| Menu item / method | Output | Notes |
|---|---|---|
| Build Windows (release, for upload) / `SteamBuild.BuildRelease()` | `Builds/Steam/content/windows/` | This is the depot content root. No `steam_appid.txt`. Unity's `*_BurstDebugInformation_DoNotShip` and `*_BackUpThisFolder_ButDontShipItWithYourGame` folders are moved to `Builds/Steam/symbols/<bundleVersion>/` and kept for crash dumps. |
| Build Windows (local test, with steam_appid.txt) / `SteamBuild.BuildLocalTest()` | `Builds/Steam/local/` | The same player plus `steam_appid.txt`, so the exe runs directly. Never upload it. |

- Both builds:
  - set the exe icon and the full-screen defaults first (`EnsurePlayerSettings`, section 5.1), and fail if an icon file is
    missing or imports at another size than its slot;
  - wipe their output folder first (the method refuses any path outside `Builds/Steam`);
  - build every enabled scene in Build Settings for StandaloneWindows64 (Mono backend, `BuildOptions.None`);
  - write `Refluff.exe` + `Refluff_Data/` (`SteamBuild.ExeName`, since 2026-09-25; independent of `productName`, see section 10);
  - fail if `Refluff_Data/Plugins/x86_64/steam_api64.dll` is missing;
  - write `CREDITS.txt` (from `UI/Credits.txt`) and `THIRD_PARTY_NOTICES.txt` (from `Tools/Steam/THIRD_PARTY_NOTICES.txt`,
    `python Tools/Steam/make_notices.py`) next to the exe, and fail if either source is missing (since 2026-09-27);
  - warn on the result line about a managed dll that is not engine, not ours and not in `SteamBuild.KnownThirdParty`
    (a new package brought a library without a licence notice; `PAUSE_MENU.md` 3).
- They return `OK ...` / `FAIL: ...`.
- From the CLI: `unity command menu --path "CarpetCleaning/Steam/Build Windows (release, for upload)" --timeout 1800`.
  It blocks until the build is done, and the result is the `[SteamBuild] OK ...` / `FAIL` line in the console.
  Do not schedule it through `EditorApplication.delayCall`: that never fires while the Editor is unfocused (tried).
- The old `Builds/CarpetCleaning.exe` (2026-09-19) is unrelated and untouched.

### 5.1 Exe icon and screen mode (2026-09-27, v0.9.1)

v0.9.0 shipped with Unity's default exe / taskbar icon and, on this machine, opened in a 1600x900 window. Both are fixed
in v0.9.1, and `SteamBuild.EnsurePlayerSettings` sets both before every build.

- **Icon.** `Assets/CarpetCleaning/AppIcon/refluff_icon_<n>.png` holds one file per Standalone icon slot (1024, 512, 256,
  128, 64, 48, 32, 16). The 1024 file is also the Default Icon. `python Tools/steam_art/export_app_icon.py` cuts them from
  the Steam icon art:
  - 1024 = the mark;
  - 512 / 256 / 128 = the mark resized (512 and 256 are identical to `SteamStore/icons/shortcut_icon_*.png`);
  - 64 / 48 / 32 = the hand-made small tiles;
  - 16 = the frame from the `.ico`.

  So the exe, its window and its taskbar button show the same art as the Steam shortcut icon.
  - The importer is forced to uncompressed, no mips and **no non-power-of-two scaling**. The default NPOT scaling had
    imported the 48 px file at 64 px, and the exe got a blurred 48 frame. That is the frame the taskbar uses at 150 %
    display scaling.
  - The exe holds one icon group (id 103) with 9 frames: 16, 24, 32, 48, 64, 96, 128, 192 and 256. Unity scales the 24,
    96 and 192 frames itself.
  - To check a build: `python Tools/Steam/check_exe_icon.py [exe] [--out dir]` compares every frame that has an art file
    with that file. v0.9.1: all 6 exact. v0.9.0: Unity's icon, every frame differs.
- **Screen mode.** The player starts as a borderless window over the whole monitor (`FullScreenMode.FullScreenWindow`)
  at the desktop resolution (`defaultIsNativeResolution`). Alt+Enter switches to a window. v0.9.0 already had these Player
  Settings.
  - Unity remembers the mode the player was last closed in and restores it on the next start. It lives in
    `HKCU\Software\VahovGames\Refluff`, in the `Screenmanager *` values:
    - `Fullscreen mode`: 0 exclusive, 1 borderless window, 3 window;
    - `Resolution Width` / `Height`, `Use Native` and the others.
  - The v0.9.0 and v0.9.1 smoke tests ran in a 1600x900 window and left `Fullscreen mode = 3` there. From then on every
    start on this machine opened a 1600x900 window, the Steam install of v0.9.0 included. That was the "does not open full
    screen" report.
    - Fix: those values were deleted, with a `reg export` backup kept in the session scratchpad.
    - v0.9.1 then started borderless over the whole 3440x1440 monitor, and quitting it wrote mode 1 back.
  - **Smoke-test rule.** Start the player without screen arguments; it then covers the monitor for a few seconds, so warn
    the person first. Otherwise delete the `Screenmanager *` values afterwards:
    `$k='HKCU:\Software\VahovGames\Refluff'; (Get-Item $k).GetValueNames() | ? { $_ -like 'Screenmanager *' } | % { Remove-ItemProperty $k -Name $_ }`.
    Unity writes these values on a clean quit (`WM_CLOSE`), not when the process is killed.

## 6. Testing (CarpetDebugApi)

- `SteamJson()` returns:
  - `ready`, `appId` (running) vs `configAppId`, `persona`, `error`;
  - `overlay`, `simPaused`, `toolsSuspended`, `simTime`;
  - `rp{token, clean, sentToken, sentClean, pushes}`. The mapping is evaluated even without Steam.
- `SteamSimulateOverlay(bool)` drives the overlay path without Steam.
- Frames only advance while CLI commands tick the Editor, so "does the sim stop" is measured by reading `simTime`
  before and after a few `editor_status` calls.

## 7. Uploading (SteamPipe, `Tools/Steam/`)

Files:

- `app_build_5304930.vdf`:
  - `ContentRoot ..\..\Builds\Steam\content\`, `BuildOutput output\`;
  - `SetLive ""`: a script never makes a build live.
- `depot_build_5304931.vdf`: `windows\*` goes to the install root, recursive. It excludes `*.pdb`,
  `steam_appid.txt` and the DoNotShip folders.
- `upload.ps1` + `upload.bat`. The bat runs the ps1 with `-ExecutionPolicy Bypass`, because Windows blocks
  unsigned scripts by default.
  - It downloads steamcmd into `Tools/Steam/steamcmd/` on the first run.
  - It refuses to run without a release build, or when `steam_appid.txt` is in the content.
  - It writes a temporary copy of the app script with `Desc`/`Preview` filled in, runs
    `steamcmd +login <account> +run_app_build <that copy> +quit`, then deletes the copy.
- steamcmd asks for the password and the Steam Guard code itself. Nothing is stored.

Steps:

1. In Unity: CarpetCleaning > Steam > Build Windows (release, for upload).
2. Dry run: `Tools\Steam\upload.bat -Account <builder_login> -Preview`. It builds the manifests and logs in
   `Tools/Steam/output/` and uploads nothing.
3. Upload: `Tools\Steam\upload.bat -Account <builder_login> -Desc "v0.1.0 first test"`.
4. In Steamworks > SteamPipe > Builds, set the new build live on a branch. Use a password-protected beta branch
   for testers, or `default` before release.
5. Install through the Steam client and check: the game starts, the overlay opens with Shift+Tab and pauses the
   game, and Rich Presence shows (after section 4's upload).

### 7.1 Web upload instead of steamcmd (used for v0.9.0, 2026-09-27)

Steamworks > SteamPipe > **Web Uploads** (`https://partner.steamgames.com/apps/depotuploads/5304930`) takes the depot as a
ZIP of up to 2048 MB, so no steamcmd login is needed.

1. Build the release (section 5).
2. Zip the **contents** of `Builds/Steam/content/windows` (files at the zip root), leaving out what the depot script
   excludes (`*.pdb`, `steam_appid.txt`, `*_DoNotShip`, `*_ButDontShipItWithYourGame`). v0.9.0: 210 files, 176.6 MB.
3. Upload with **Standard** (a full replace; "Merge with Baseline" keeps files the ZIP does not contain).
   - The page sends the file in 2 MB chunks to `partnerupload.steampowered.com`, all at once. Progress shows nothing for
     minutes, then jumps to 100 %: 177 MB took about 4 minutes.
   - Then it shows "Waiting for depot build ..." and "Building depot done (ManifestID ...)".
4. "Commit uploaded depots as build" with a note. Leave the branch at None, then set it live from **Builds**:
   Preview Change > Set Build Live Now. The last step raises a native confirm() in the page, which the person clicks.
5. Publish (App Admin > Publish: View Diffs > Prepare for Publishing > Publish to Steam, confirmation code `STEAMWORKS`).

Browser-automation notes: the file tool takes at most 10 MB per call, so the ZIP went in as 9 MB parts and was rebuilt in
the page with `new File(parts)`. Its SHA-256 was checked against the local file, then set on the `depotfile` input
through a `DataTransfer`. Clicks by coordinate do nothing while the tab is in the background; call the page's own
handlers instead.

v0.9.0 on Steam: BuildID **25558788**, manifest 7836903329287943867, 573.0 MB on disk, 165.6 MB download, live on `default`.

v0.9.1 (the 31 languages, 2026-09-27) went up twice:

1. The first ZIP, without the exe icon, was web-uploaded by the person. It is the baseline manifest 983883189799426378;
   the Steam install ran it on this machine.
2. It was rebuilt the same evening with the exe icon (section 5.1) and uploaded with steamcmd (section 7.2):
   - BuildID **25562494**, manifest 8316280809583961206.
   - Against the baseline only `Refluff.exe` and `Refluff_Data/boot.config` changed, so 2 chunks went up in 7 s.
   - It is NOT set live on any branch; the person sets it live and publishes it.

The ZIP `Builds/Steam/upload/Refluff_0.9.1.zip` was rebuilt from the same content (210 files, 174.6 MB,
SHA-256 `a5aea566ad05fc06fd0866d8a4f0b3110ae8d3531791be62f3f4d644870b6227`).

### 7.2 steamcmd from an agent's session (used for v0.9.1, 2026-09-27)

- **Install.** steamcmd lives in `Tools/Steam/steamcmd/`. The first run updates itself and exits with code 7; the next run
  exits with 0.
- **Login.** An agent's shell has no console input, so it cannot answer steamcmd's password prompt. The agent opens a
  console window for the person instead:
  `Start-Process powershell.exe -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-File','Tools\Steam\upload_window.ps1','-Desc','<text>'`.
  - The window asks for the login and runs `upload.ps1`. steamcmd then asks for the password and the Steam Guard code
    itself.
  - On "Invalid Password" (exit code 5) the window offers another try.
  - At the end it writes `Tools/Steam/output/upload_status.txt`. The agent waits for that file, then reads the BuildID from
    `Tools/Steam/output/app_build_5304930.log`.
- **Cached login.** After a successful login steamcmd keeps it: `steamcmd/config/config.vdf` gets a `ConnectCache` entry.
  The next `upload.bat -Account <login>` needs no password until Steam expires the token. That file is a credential: never
  copy the steamcmd folder off this machine, and never ship it.
- **Incremental upload.** The depot build compares against the depot's last manifest and uploads only the changed chunks.
- **Failed logins.** Steam counts them ("BeginAuthSessionViaCredentials failed, 1/5" in `steamcmd/logs/connection_log.txt`).
  Stop after a couple of wrong passwords rather than hitting the rate limit.

### 7.3 Balance build uploaded 2026-10-19

- Rebuilt the Windows release with the minimum 50% dust / 20% crust gameplay coverage change; bundleVersion stays
  `0.9.1`. `SteamBuild` reported OK: 210 files, 556.6 MB, 42 seconds. Required player/Steam/legal files, absence of
  `steam_appid.txt` and debug folders in the content root, and all six exact exe icon frames passed file checks.
- Uploaded through `upload_window.ps1` / SteamCMD after the user requested upload: AppID `5304930`, depot `5304931`,
  **BuildID 26044364**, manifest **5526638028200978727**. SteamCMD exited 0; both app/depot logs report success.
  Relative to manifest `8316280809583961206`: 8 files changed, 5.20 MB changed, 13 missing chunks uploaded.
- Description: `Refluff 0.9.1 2026-10-19: minimum dust 50pct and crust 20pct`. Upload used `SetLive ""`; the agent did
  not assign a branch or publish. No Play-mode or player smoke test was performed; the user handles gameplay checks.

### 7.4 Cartoon rug / Editor-only Reset build uploaded 2026-10-19

- Rebuilt the Windows release after adding five generated cartoon rug designs and hiding the HUD Reset button in
  players. Version stays `0.9.1`. `SteamBuild` reported OK: **211 files, 586.6 MB**, 30 seconds; exact content size
  **615,073,531 bytes**. Build console has no errors or compilation failure; existing CarpetFoam shader warnings
  about potentially uninitialized `FieldAt` / `FAt` remain.
- All five cartoon resource names occur in the built `Refluff_Data/resources.assets`. Required player/Steam/legal
  files and exe icon checks passed; no `steam_appid.txt`, PDB or debug/backup directories in the upload content.
  No Play-mode or player smoke test was run; the user handles gameplay checks.
- Uploaded through the interactive `upload_window.ps1` / SteamCMD workflow: AppID `5304930`, depot `5304931`,
  **BuildID 26046043**, manifest **8343041185643269779**. Fresh `upload_status.txt` is `exit=0`; app/depot logs
  confirm success at **13:31:40 Moscow** on 2026-10-19.
- Description: `Refluff 0.9.1 2026-10-19: 5 cartoon rugs and Editor-only Reset`. Relative to baseline manifest
  `5526638028200978727`: one 30.00 MB resource-data file added, five files changed (2.31 MB), 36 missing chunks
  uploaded. `SetLive` stayed empty: upload only, with no branch assignment or Steamworks publication.

## 8. Steamworks partner-site checklist (manual, not scriptable)

- [x] SteamPipe > Depots: depot 5304931 exists (OS Windows, all languages).
- [x] Application > General > Supported Operating Systems: **Windows, 64-bit only** (set and published 2026-09-27; it was
  empty, which turned "Platform Support Matches" red after the first Publish).
- [ ] Installation > General: launch option executable `Refluff.exe`, OS Windows, 64-bit only. It must match `SteamBuild.ExeName` and the check in `upload.ps1`; change all three together.
- [ ] Your Steam account owns the app. Partners get the "Developer Comp" package; without it `SteamAPI.InitEx`
  fails with the real App ID. The Editor inits fine on this machine today.
- [x] Community > Rich Presence: `rich_presence_english.vdf` and `rich_presence_russian.vdf` uploaded 2026-09-27.
- [x] Publish (Steamworks settings changes do nothing until published): 2026-09-27, twice (icons + Rich Presence; then the OS list).
- [ ] Stats & Achievements: 2 stats + 10 achievements with icons + the localization file (`ACHIEVEMENTS.md` 5), then Publish.
- [ ] Steam Cloud: quota + one Auto-Cloud row (section 11), then Publish.
- [ ] Recommended: a separate builder Steam account with only the "Edit App Metadata" and "Publish App Changes To
  Steam" permissions for this app, used only by `upload.bat`.

## 9. Pitfalls found while integrating

- **Define-symbol loop.** Steamworks.NET's `RedistInstall` adds `STEAMWORKS_NET` to the global Standalone defines.
  The active build profile (`Assets/Settings/Build Profiles/Windows.asset`, empty defines) removed it again on every
  Play stop ("Player settings modified"). The package then re-added it on the next Editor tick, which usually fell
  inside Play mode: a recompile in Play wiped the sim RTs and orphaned SteamService.
  - Fix: `ProjectSettings/SteamworksNETSettings.json` `{"canManageDefineSymbols": false}`, and the define cleared.
    Nothing reads `STEAMWORKS_NET`; our guards use the platform defines.
  - Keep that JSON. If `STEAMWORKS_NET` ever reappears in Player Settings, remove it.
- The package writes `steam_appid.txt` = 480 only when the file is missing. Ours holds 5304930; do not delete it.
- 6000.7 marks `FindFirstObjectByType` obsolete: new code uses `FindAnyObjectByType`. The UAL0010/UAL0013
  "AutoStaticsCleanup" analyzer warnings are left alone, as in the rest of the project; statics are reset by hand.
- As always: after a recompile in Play mode, stop and start Play.
- **A windowed smoke test sticks.** The player keeps its last screen mode in the registry. After one run in a window,
  every later start on the same machine opens in a window, Steam installs included. See section 5.1 for the rule and the
  clean-up.

## 10. Before a public release (not done in this pass)

- Names: resolved 2026-09-25. `companyName` = `VahovGames`, `productName` = `Refluff` (window title), exe `Refluff.exe`
  (`SteamBuild.ExeName`). Company + product name the Player.log / save folder `LocalLow/VahovGames/Refluff`. The editor
  save was copied across; the original under `LocalLow/DefaultCompany/CarpetCleaning` is kept as a backup.
  Renaming either again after players have saves would strand them.
- `MCPForUnity.Runtime.dll` + `Newtonsoft.Json.dll` (coplay unity-mcp package) end up in the player. Consider
  excluding the package from player builds.
- Build size is 548 MB (2026-09-25; 484 MB on 2026-09-23, when 352 MB of it was textures): each of the 7 Tripo tool models
  (`Models/Tools/Tool_*.glb`) carries about 48 MB. Reimport them with compressed, downsized textures before the
  first public build, so every player's download is smaller.
- Carpet pictures: resolved 2026-09-23. The five rug photos are museum public domain / CC0 and need no
  attribution; `cat_meme.jpg` (CC BY-SA) was removed (`HANDOVER.md` 5).
- The audio is real recordings since 2026-09-25 (`AUDIO.md` 11), all CC0 / CC BY / Mixkit except the dry vacuum's machine.
  - CC BY needs credits: the game's credits screen (none yet) and the store page must carry the block in `AUDIO_CREDITS.md`
    (`python Tools/audio_recordings.py credits`).
  - The dry vacuum's machine is a Freesound recording under **CC BY-NC 4.0** (Debsound, https://freesound.org/s/100210/):
    non-commercial use only. A paid release must swap in the ready CC BY cut (`dryvacuum_dyson.json`, `AUDIO.md` 11.4), use your
    own recording, or get the author's written permission.
  - While an NC credit is in the bank (machine takes and every recorded clip slot), `SteamBuild` logs a warning and adds it to its
    result line (`AUDIO.md` 9.6).
  - The clips were cut from Freesound's previews (~192 kbps OGG); rebuild them from the originals before release (`AUDIO.md` 11.2).
- Store page, age rating, the release checklist in Steamworks, and a price are all manual.

### Save file names
- **Names** are set (`VahovGames` / `Refluff`, 2026-09-25). Changing `companyName` or `productName` again after players have
  saves would strand them locally and in Steam Cloud (section 11), so the names stay as they are.

## 11. Steam Cloud (Auto-Cloud, 2026-09-30)

The whole progress is one small JSON: `carpet_save.json` in `persistentDataPath`, i.e.
`%USERPROFILE%/AppData/LocalLow/VahovGames/Refluff/` (about 0.5-1 KB: money, counters, upgrade levels, lamps, and since
2026-09-30 the local achievement flags and the used-tools mask, `ACHIEVEMENTS.md` 3). Steam **Auto-Cloud** syncs that file
without any game code: the client downloads it before the game starts and uploads it after the game exits. The Editor's
`carpet_save_editor.json` never goes to the cloud.

### 11.1 Steamworks setup (manual, then Publish)

App Admin > Application > **Steam Cloud** (`https://partner.steamgames.com/apps/cloud/5304930`):

1. **Byte quota per user** `1048576` (1 MB) and **Number of files allowed per user** `10`, Save. The Auto-Cloud block
   unlocks after this first save.
2. **Auto-Cloud Root Paths**, one row:

   | Root | Subdirectory | Pattern | OS | Recursive |
   |---|---|---|---|---|
   | `WinAppDataLocalLow` | `VahovGames/Refluff` | `carpet_save.json` | [All OSes] | off |

   - The pattern is the exact file name on purpose: the same folder holds `Player.log`, `Player-prev.log`, Unity's cache
     folder, the Editor save, and `SaveStore`'s transient `carpet_save.json.tmp` / `.bak`. None of them may sync.
   - No root overrides: the game ships for Windows only, and under Proton / Steam Deck it writes into the same
     `AppData/LocalLow` inside the prefix, which Steam syncs with this root. A native Linux / macOS build would need overrides
     (`LinuxHome` + `.config/unity3d/VahovGames/Refluff`, `MacAppSupport` + `VahovGames/Refluff`, "Replace Path" ticked).
3. App Admin > **Publish**. After publishing, the Steam client shows the cloud icon for the game (may take a few minutes or a
   Steam restart). The store page's "Steam Cloud" feature follows from the published config.

"Enable cloud support for developers only" hides the cloud from customers and switches Auto-Cloud off. **Steam ticks it
by itself on the first quota Save**; it must be unticked again (done 2026-09-30).

State 2026-09-30: quota `1048576` / `10` files, the one Auto-Cloud row above (preview
`%USERPROFILE%/AppData/LocalLow/VahovGames/Refluff`, Cross-Platform "No", as expected for a Windows-only root), developers-only
unticked, Dynamic Cloud Sync off. Saved in Steamworks and checked after a reload; takes effect with the next Publish.

### 11.2 How it behaves

- Steam writes a `steam_autocloud.vdf` into the folder; the game ignores it.
- A save written during the session is uploaded when the game exits. If the local copy and the cloud copy both changed since
  the last sync (two PCs played offline), the Steam client, not the game, shows its Cloud Conflict dialog and the player picks one.
- The save is per PC / per Windows user, not per Steam account (no `{64BitSteamID}` sub-folder). That matches how the
  game saved before the cloud existed.
- Settings stay local on purpose: language, volume and the screen mode are PlayerPrefs in
  `HKCU\Software\VahovGames\Refluff` (Valve's Steam Deck guidance: keep machine-specific settings out of the cloud).
- `SaveStore` writes atomically (`.tmp` + `File.Replace`), so the client never sees a half-written file.

### 11.3 Checking it

- In Play / a player: `CarpetDebugApi.SteamJson()` has `cloud{account, app, quotaTotal, quotaAvail, files}`:
  `account` / `app` are `IsCloudEnabledForAccount` / `IsCloudEnabledForApp` (the player can switch the cloud off globally or
  per game), `quotaTotal` shows that the published quota reached the client, `files` counts what Steam Cloud tracks.
- End to end (after Publish): open `steam://open/console`, type `testappcloudpaths 5304930` and `set_spew_level 4 4`, start the
  game through Steam, hand over a carpet, quit, and read `%Steam%\logs\cloud_log.txt` for the upload of `carpet_save.json`.
  Afterwards `testappcloudpaths 0` and `set_spew_level 0 0`. The Steam client's game Properties > General also shows the
  cloud status and "Last synced".
