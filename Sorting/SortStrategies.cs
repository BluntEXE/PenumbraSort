using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PenumbraSort.ModTree;

namespace PenumbraSort.Sorting;

public enum SortStrategyKind
{
    Manual,
    ByCreator,
    ByType,
    TypeThenCreator,
    CreatorThenType,
    PreserveAndClean,
    Alphabetical,
    Custom,
}

public static partial class SortStrategies
{
    [GeneratedRegex(@"^\[(?<creator>[^\]]+)\]")]
    private static partial Regex CreatorPrefixRegex();

    private const string UnknownCreator = "Unknown Creator";

    /// <summary>
    /// Applies a sort strategy to a list of mods and returns a proposal:
    /// modDirectory -> proposed path. Protected mods and mods with no proposed
    /// change under the given strategy are omitted from the result.
    /// </summary>
    public static Dictionary<string, string> Apply(SortStrategyKind kind, IEnumerable<ModEntry> mods)
    {
        var proposal = new Dictionary<string, string>();

        foreach (var mod in mods)
        {
            if (mod.Protected)
                continue;

            var path = kind switch
            {
                SortStrategyKind.Manual => null,
                SortStrategyKind.ByCreator => $"{Creator(mod)}/{mod.Name}",
                SortStrategyKind.ByType => $"{Category(mod)}/{mod.Name}",
                SortStrategyKind.TypeThenCreator => $"{Category(mod)}/{Creator(mod)}/{mod.Name}",
                SortStrategyKind.CreatorThenType => $"{Creator(mod)}/{Category(mod)}/{mod.Name}",
                SortStrategyKind.Alphabetical => $"{FirstLetterBucket(mod.Name)}/{mod.Name}",
                SortStrategyKind.PreserveAndClean => mod.CurrentPath,
                SortStrategyKind.Custom => null, // handled by ApplyCustom
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
            };

            if (path is not null && path != mod.CurrentPath)
                proposal[mod.Directory] = path;
        }

        return proposal;
    }

    /// <summary>
    /// Applies a custom folder template. Supported placeholders: {Creator}, {Category}, {Name}.
    /// </summary>
    public static Dictionary<string, string> ApplyCustom(string template, IEnumerable<ModEntry> mods)
    {
        var proposal = new Dictionary<string, string>();

        foreach (var mod in mods)
        {
            if (mod.Protected)
                continue;

            var path = template
                .Replace("{Creator}", Creator(mod))
                .Replace("{Category}", Category(mod))
                .Replace("{Name}", mod.Name);

            if (path != mod.CurrentPath)
                proposal[mod.Directory] = path;
        }

        return proposal;
    }

    /// <summary>
    /// Returns mods currently sitting at the folder root (no folder segments) — a
    /// triage view, not a reorganization strategy.
    /// </summary>
    public static IEnumerable<ModEntry> Unsorted(IEnumerable<ModEntry> mods)
        => mods.Where(m => !m.CurrentPath.Contains('/'));

    private static string Creator(ModEntry mod)
    {
        var match = CreatorPrefixRegex().Match(mod.Name);
        return match.Success ? match.Groups["creator"].Value : UnknownCreator;
    }

    private static string Category(ModEntry mod)
    {
        var category = ChangedItemClassifier.Classify(mod.ChangedItems);
        return category == ChangedItemCategory.Uncategorized ? "Uncategorized" : category.ToString();
    }

    private static string FirstLetterBucket(string name)
    {
        var trimmed = name.TrimStart();
        return trimmed.Length == 0 ? "#" : char.ToUpperInvariant(trimmed[0]).ToString();
    }
}
