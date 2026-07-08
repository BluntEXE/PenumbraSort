using System;
using System.Collections.Generic;
using PenumbraSort.ModTree;

namespace PenumbraSort.Protection;

public sealed class ProtectionStore
{
    private readonly HashSet<string> _protected = new(StringComparer.OrdinalIgnoreCase);

    public bool IsProtected(string modDirectory) => _protected.Contains(modDirectory);

    public void Protect(string modDirectory) => _protected.Add(modDirectory);

    public void Unprotect(string modDirectory) => _protected.Remove(modDirectory);

    public void ProtectFolder(ModTreeNode folder)
    {
        foreach (var mod in EnumerateMods(folder))
            Protect(mod.Directory);
    }

    public void UnprotectFolder(ModTreeNode folder)
    {
        foreach (var mod in EnumerateMods(folder))
            Unprotect(mod.Directory);
    }

    public IReadOnlySet<string> Snapshot() => _protected;

    public void LoadFrom(IEnumerable<string> directories)
    {
        _protected.Clear();
        foreach (var directory in directories)
            _protected.Add(directory);
    }

    private static IEnumerable<ModEntry> EnumerateMods(ModTreeNode node)
    {
        foreach (var mod in node.Mods)
            yield return mod;

        foreach (var child in node.Children.Values)
            foreach (var mod in EnumerateMods(child))
                yield return mod;
    }
}
