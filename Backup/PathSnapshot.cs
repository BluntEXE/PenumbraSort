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
