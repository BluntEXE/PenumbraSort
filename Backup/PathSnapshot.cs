using System.Collections.Generic;
using PenumbraSort.Ipc;
using PenumbraSort.ModTree;
using Penumbra.Api.Enums;

namespace PenumbraSort.Backup;

public sealed class PathSnapshot
{
    private readonly Dictionary<string, (string ModName, string PreviousPath)> _entries = new();

    public bool HasSnapshot => _entries.Count > 0;

    public void Capture(IPenumbraIpc ipc, IEnumerable<ModEntry> mods)
    {
        _entries.Clear();

        foreach (var mod in mods)
        {
            var (code, fullPath) = ipc.GetModPath(mod.Directory, mod.Name);
            if (code == PenumbraApiEc.Success)
                _entries[mod.Directory] = (mod.Name, fullPath);
        }
    }

    /// <summary>
    /// Replays SetModPath for every captured entry to restore pre-Apply paths.
    /// Intentionally does NOT clear the snapshot afterward: the captured paths remain
    /// available for inspection or a repeat Restore call. Do not add an _entries.Clear()
    /// here without checking callers that rely on this replay-ability.
    /// Iterates in dictionary order; assumes Penumbra tolerates transient duplicate paths
    /// during a multi-mod restore, or that restore order doesn't matter (unverified).
    /// </summary>
    public IReadOnlyList<(string Directory, string ModName, PenumbraApiEc Result)> Restore(IPenumbraIpc ipc)
    {
        var results = new List<(string, string, PenumbraApiEc)>();

        foreach (var (directory, (modName, previousPath)) in _entries)
        {
            var result = ipc.SetModPath(directory, previousPath, modName);
            results.Add((directory, modName, result));
        }

        return results;
    }
}
