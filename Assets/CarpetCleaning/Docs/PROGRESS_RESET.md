# Editor progress reset (2026-11-16)

Reset the Editor's saved progression from any of these entry points:

- **Tools > CarpetCleaning > Reset Progress...**
- **CarpetCleaning > Economy > Reset Progress...** (the same command)
- **CarpetCleaning > Economy > Test Money Window... > Reset all progress...**

Each entry point opens a confirmation dialog showing the save path. Cancel makes no changes.
The existing **Wallet to $0 (keeps upgrades)** button still resets only money.

## Scope

The command replaces `carpet_save_editor.json` in `Application.persistentDataPath` with a normalized fresh `SaveData`:
money, tool POWER/AREA levels, both inspection highlights, lifetime earnings, delivery/perfection/combo counters,
best time, local achievement flags and tool-use history are reset. The player save `carpet_save.json`, Steam
achievements/statistics, language and volume preferences are kept. The current carpet and its job clock continue.

Steam achievements normally do not sync during Editor Play (`SteamAchievements.steamInEditor` defaults to false).
If that option is enabled, a later Steam sync can restore achievements already unlocked on the account. This command
does not revoke Steam achievements or reset Steam statistics.

## Implementation

`Editor/EconomyDebugWindow.cs` owns the menu entries, button, confirmation and public `ResetProgress()` CLI hook.
The CLI hook performs the reset directly, without the interactive confirmation.

- **Edit mode, or no initialized live Economy:** create and normalize fresh save data, then write it through
  `SaveStore.Write`. The next Play loads the reset save.
- **Play mode with an initialized Economy:** call `Economy.DebugResetSave()`. It writes the fresh save before replacing
  live data, clears `LastPayout`, reapplies the starting tool levels and notifies the wallet/shop and highlight owners.
  The editor command then clears `ToolTuning.Neutral` so the actual level-0 tuning is used after scripted tests too.

`SaveStore.Write` commits through a temporary file and atomic replacement (or a move for the first save).
If the write fails, the command reports **progress NOT reset** and keeps the old save and live progress. The live
`DebugResetSave()` method returns `false` in that case; it no longer deletes the old file before attempting the reset.

Verification results belong in `SESSION_LOG.md`. When testing this command, snapshot and restore the Editor save;
the user plays between development sessions.
