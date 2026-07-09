using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using Dalamud.Plugin;
using PenumbraSort.Sorting;

namespace PenumbraSort;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public HashSet<string> ProtectedModDirectories { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public SortStrategyKind LastStrategy { get; set; } = SortStrategyKind.Manual;

    public string CustomTemplate { get; set; } = "{Category}/{Creator}/{Name}";

    [NonSerialized]
    private IDalamudPluginInterface? _pluginInterface;

    public void Initialize(IDalamudPluginInterface pluginInterface)
        => _pluginInterface = pluginInterface;

    public void Save()
        => _pluginInterface?.SavePluginConfig(this);
}
