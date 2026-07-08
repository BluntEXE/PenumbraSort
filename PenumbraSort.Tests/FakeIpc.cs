using System;
using System.Collections.Generic;
using PenumbraSort.Ipc;
using Penumbra.Api.Enums;

namespace PenumbraSort.Tests;

public sealed class FakeIpc : IPenumbraIpc
{
    private readonly Dictionary<string, string> _modNames = new();
    private readonly Dictionary<string, string> _modPaths = new();
    private readonly Dictionary<string, Dictionary<string, object?>> _changedItems = new();

    public event Action<string>? ModAdded;
    public event Action<string>? ModDeleted;
    public event Action<string, string>? ModMoved;

    public bool IsAvailable { get; set; } = true;

    public void AddMod(string directory, string name, string path, Dictionary<string, object?>? changedItems = null)
    {
        _modNames[directory] = name;
        _modPaths[directory] = path;
        _changedItems[directory] = changedItems ?? new Dictionary<string, object?>();
    }

    public void RemoveMod(string directory)
    {
        _modNames.Remove(directory);
        _modPaths.Remove(directory);
        _changedItems.Remove(directory);
    }

    public IReadOnlyDictionary<string, string> GetModList() => _modNames;

    public (PenumbraApiEc Code, string FullPath) GetModPath(string modDirectory, string modName)
        => _modPaths.TryGetValue(modDirectory, out var path)
            ? (PenumbraApiEc.Success, path)
            : (PenumbraApiEc.ModMissing, string.Empty);

    public PenumbraApiEc SetModPath(string modDirectory, string newPath, string modName)
    {
        if (!_modNames.ContainsKey(modDirectory))
            return PenumbraApiEc.ModMissing;

        var old = _modPaths[modDirectory];
        _modPaths[modDirectory] = newPath;
        ModMoved?.Invoke(old, newPath);
        return PenumbraApiEc.Success;
    }

    public IReadOnlyDictionary<string, object?> GetChangedItems(string modDirectory, string modName)
        => _changedItems.TryGetValue(modDirectory, out var items) ? items : new Dictionary<string, object?>();
}
