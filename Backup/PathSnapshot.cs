using System.Collections.Generic;
using PenumbraSort.Ipc;
using PenumbraSort.ModTree;
using Penumbra.Api.Enums;

namespace PenumbraSort.Backup;

public sealed class PathSnapshot
{
    private readonly Stack<Dictionary<string, (string ModName, string PreviousPath)>> _layers = new();

    public bool HasSnapshot => _layers.Count > 0;

    /// <summary>Number of Applies that can still be stepped back through via Restore.</summary>
    public int LayerCount => _layers.Count;

    /// <summary>
    /// Pushes a new undo layer for the given mods' current paths. Each Apply should call
    /// this before moving anything, so repeated Applies (e.g. during testing) accumulate
    /// independent layers instead of one overwriting the last - see the real incident where
    /// a single overwritable snapshot couldn't recover past the most recent Apply
    /// (memory/project_penumbrasort.md).
    /// </summary>
    public void Capture(IPenumbraIpc ipc, IEnumerable<ModEntry> mods)
    {
        var layer = new Dictionary<string, (string ModName, string PreviousPath)>();

        foreach (var mod in mods)
        {
            var (code, fullPath) = ipc.GetModPath(mod.Directory, mod.Name);
            if (code == PenumbraApiEc.Success)
                layer[mod.Directory] = (mod.Name, fullPath);
        }

        if (layer.Count > 0)
            _layers.Push(layer);
    }

    /// <summary>
    /// Restores the most recently captured layer, then discards it. Calling Restore again
    /// steps one Apply further back in history rather than replaying the same layer - this
    /// is what lets a sequence of Applies be undone one at a time instead of stranding
    /// everything but the very last one.
    /// Iterates in dictionary order; assumes Penumbra tolerates transient duplicate paths
    /// during a multi-mod restore, or that restore order doesn't matter (unverified).
    /// </summary>
    public IReadOnlyList<(string Directory, string ModName, PenumbraApiEc Result)> Restore(IPenumbraIpc ipc)
    {
        var results = new List<(string, string, PenumbraApiEc)>();

        if (_layers.Count == 0)
            return results;

        var layer = _layers.Pop();

        foreach (var (directory, (modName, previousPath)) in layer)
        {
            var result = ipc.SetModPath(directory, previousPath, modName);
            results.Add((directory, modName, result));
        }

        return results;
    }
}
