# PenumbraSort

A [Dalamud](https://github.com/goatcorp/Dalamud) plugin for FFXIV that reorganizes your [Penumbra](https://github.com/xivdev/Penumbra) mod folders **live, in-game**, via Penumbra's IPC — no need to close the game or hand-edit Penumbra's config, unlike similar tools that only work offline.

## Features

- **Live scan** of your full Penumbra mod list, with side-by-side Current vs Proposed folder trees
- **Sort strategies**: By Creator, By Type, Type-then-Creator, Creator-then-Type, Alphabetical, Preserve & Clean (no-op baseline), or a fully custom folder template (`{Creator}`, `{Category}`, `{Name}` placeholders)
- **Type classification** reads the actual equipment/customization data a mod changes (via reflection over Penumbra's `GetChangedItems`) rather than guessing from the mod's display name — real mod names are often just creative branding with no structural info in them
- **Protection** — mark specific mods so no sort strategy ever touches them
- **Review before apply** — grouped-by-destination diff view with per-mod skip toggles and a pinned summary; nothing moves until you confirm
- **Multi-level undo** — every Apply is snapshotted onto a stack, so Restore steps back through your last several Applies, not just the most recent one
- **Auto-resume on open** — the window scans and re-applies your last-used strategy automatically, so you don't repeat the same two clicks every session

## Commands

- `/pensort` — opens the PenumbraSort window

## Known limitation

Penumbra's public IPC has no folder-deletion or merge endpoint (confirmed by decompiling `Penumbra.dll` — only mod-level operations like `InstallMod`/`DeleteMod`/`SetModPath` exist). Penumbra's own UI can delete/merge folders because it has direct in-process access to its internal file system; a plugin cannot reach that. **Folders left empty after a sort must be deleted manually** in Penumbra's own mod list — this is not a bug, it's a hard API boundary.

## Installation

Not yet submitted to the official Dalamud plugin repository (DalamudPluginsD17). Install via a custom plugin repo instead:

1. In-game, open the Dalamud settings (`/xlsettings`) → **Experimental** tab
2. Under **Custom Plugin Repositories**, paste:
   ```
   https://raw.githubusercontent.com/BluntEXE/PenumbraSort/main/repo.json
   ```
3. Save, then find **PenumbraSort** in the plugin installer (`/xlplugins`) under All Plugins and install it

## Status

Actively developed, 93 unit tests, used daily by the author.

## Building

```
dotnet build -c Release
```

Produces `bin/Release/PenumbraSort/PenumbraSort.zip` via DalamudPackager.

## License

[AGPL-3.0-or-later](LICENSE)
