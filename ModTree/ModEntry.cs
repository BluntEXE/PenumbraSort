using System.Collections.Generic;

namespace PenumbraSort.ModTree;

public sealed class ModEntry
{
    public required string Directory { get; init; }
    public required string Name { get; init; }
    public required string CurrentPath { get; set; }
    public string? ProposedPath { get; set; }
    public bool Protected { get; set; }
    public IReadOnlyDictionary<string, object?> ChangedItems { get; set; } =
        new Dictionary<string, object?>();
}
