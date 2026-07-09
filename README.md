# PenumbraSort

A [Dalamud](https://github.com/goatcorp/Dalamud) plugin for FFXIV that reorganizes your [Penumbra](https://github.com/xivdev/Penumbra) mod folders **live, in-game**, via Penumbra's IPC — no need to close the game or hand-edit Penumbra's config, unlike similar tools that only work offline.

## Is this for you?

**Yes, if:** your mod list is loose/unsorted — mods dumped flat with no real folder structure, and you don't already have a system.

**Probably not, if:** you've already built a working folder structure you're happy with. PenumbraSort's automatic categorization is based on real equipment-slot/customization data, which is a different (and coarser) taxonomy than most hand-built schemes — running a bulk sort on an already-organized library is more likely to scramble it than improve it. Penumbra's own drag-and-drop is simpler for touch-ups to an existing structure.

This tool gets you **most of the way** from chaos to something reasonable, automatically, in one pass. It does not (and can't) reproduce a bespoke, hand-tuned taxonomy — see Limitations below.

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

## Limitations (read before running a sort)

- **No folder deletion.** Penumbra's public IPC has no folder-deletion or merge endpoint (confirmed by decompiling `Penumbra.dll` — only mod-level operations like `InstallMod`/`DeleteMod`/`SetModPath` exist). Penumbra's own UI can delete/merge folders because it has direct in-process access to its internal file system; a plugin cannot reach that. **Folders left empty after a sort must be deleted manually** in Penumbra's own mod list — this is not a bug, it's a hard API boundary.
- **Non-appearance mods aren't classifiable.** Category detection reads Penumbra's `GetChangedItems` — equipment slots and body/face customization only. Idle/dance/emote animations, skeleton replacements, UI mods, and tool/utility mods (e.g. teleport plugins bundled as a "mod") change nothing Penumbra reports there, so they can't be auto-sorted and will land in `Uncategorized`/`Other`. Plan on filing those manually regardless of strategy.
- **No per-character awareness.** Penumbra doesn't expose "this mod belongs to character X" as data — that's a Collections concept, separate from the mod list. If you organize by character, PenumbraSort can't detect or preserve that split automatically.
- **Coarse category taxonomy.** The built-in categories (Hair, Face, Top, Bottom, Shoes, Weapon, Accessory, Outfit, Mixed, Uncategorized) are flat and equipment-slot-driven. Multi-piece outfits with no majority slot land in `Outfit` or `Mixed` rather than a specific slot — expect some manual cleanup after any bulk sort, especially on a large or eclectic library.
- **Always review before Apply.** Nothing moves until you confirm in the Review screen. Use Protect on anything you don't want a sort strategy to ever touch.

## Installation

Not yet submitted to the official Dalamud plugin repository (DalamudPluginsD17). Install via a custom plugin repo instead:

1. In-game, open the Dalamud settings (`/xlsettings`) → **Experimental** tab
2. Under **Custom Plugin Repositories**, paste:
   ```
   https://raw.githubusercontent.com/BluntEXE/PenumbraSort/master/repo.json
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
