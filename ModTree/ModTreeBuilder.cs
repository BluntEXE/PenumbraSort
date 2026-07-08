using System;
using System.Collections.Generic;

namespace PenumbraSort.ModTree;

public sealed class ModTreeNode
{
    public string Name { get; }
    public Dictionary<string, ModTreeNode> Children { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<ModEntry> Mods { get; } = new();

    public ModTreeNode(string name) => Name = name;
}

public static class ModTreeBuilder
{
    public static ModTreeNode Build(IEnumerable<ModEntry> mods, bool useProposedPath = false)
    {
        var root = new ModTreeNode(string.Empty);

        foreach (var mod in mods)
        {
            var path = useProposedPath ? mod.ProposedPath ?? mod.CurrentPath : mod.CurrentPath;
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

            var node = root;
            // The last segment is the mod's own display slot, not a folder;
            // only the segments before it form the tree structure.
            for (var i = 0; i < segments.Length - 1; i++)
            {
                var segment = segments[i];
                if (!node.Children.TryGetValue(segment, out var child))
                {
                    child = new ModTreeNode(segment);
                    node.Children[segment] = child;
                }
                node = child;
            }

            node.Mods.Add(mod);
        }

        return root;
    }
}
