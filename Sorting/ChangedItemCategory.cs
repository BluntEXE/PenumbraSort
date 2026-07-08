using System;
using System.Collections.Generic;
using System.Linq;

namespace PenumbraSort.Sorting;

public enum ChangedItemCategory
{
    Hair,
    Face,
    Body,
    Top,
    Bottom,
    Weapon,
    Accessory,
    Other,
    Mixed,
    Uncategorized,
}

public static class ChangedItemClassifier
{
    private static readonly (string Keyword, ChangedItemCategory Category)[] Keywords =
    [
        ("hair", ChangedItemCategory.Hair),
        ("face", ChangedItemCategory.Face),
        ("ears", ChangedItemCategory.Face),
        ("tail", ChangedItemCategory.Body),
        ("body", ChangedItemCategory.Body),
        ("head", ChangedItemCategory.Top),
        ("hands", ChangedItemCategory.Top),
        ("legs", ChangedItemCategory.Bottom),
        ("feet", ChangedItemCategory.Bottom),
        ("weapon", ChangedItemCategory.Weapon),
        ("mainhand", ChangedItemCategory.Weapon),
        ("offhand", ChangedItemCategory.Weapon),
        ("earring", ChangedItemCategory.Accessory),
        ("necklace", ChangedItemCategory.Accessory),
        ("bracelet", ChangedItemCategory.Accessory),
        ("ring", ChangedItemCategory.Accessory),
    ];

    /// <summary>
    /// Classifies a mod's changed items by the dominant category. If the dominant
    /// category does not hold a strict majority (&gt;50%) of the changed items,
    /// the mod is classified as Mixed rather than guessed.
    /// </summary>
    public static ChangedItemCategory Classify(IReadOnlyDictionary<string, object?> changedItems)
    {
        if (changedItems.Count == 0)
            return ChangedItemCategory.Uncategorized;

        var counts = new Dictionary<ChangedItemCategory, int>();
        foreach (var itemName in changedItems.Keys)
        {
            var category = ChangedItemCategory.Other;
            foreach (var (keyword, candidate) in Keywords)
            {
                if (itemName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    category = candidate;
                    break;
                }
            }

            counts[category] = counts.GetValueOrDefault(category) + 1;
        }

        var total = changedItems.Count;
        var dominant = counts.OrderByDescending(kv => kv.Value).First();

        return dominant.Value * 2 > total ? dominant.Key : ChangedItemCategory.Mixed;
    }
}
