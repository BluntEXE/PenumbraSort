using System;
using System.Collections.Generic;
using System.Linq;

namespace PenumbraSort.Sorting;

public enum ChangedItemCategory
{
    Hair,
    Face,
    /// <summary>Character customization only (skin color, body type, tattoos) - NOT equipment.</summary>
    Body,
    Head,
    /// <summary>Chest/torso equipment (shirts, jackets, dresses).</summary>
    Top,
    /// <summary>Leg equipment (pants, skirts).</summary>
    Bottom,
    /// <summary>Foot equipment.</summary>
    Shoes,
    /// <summary>Hand equipment (gloves, gauntlets).</summary>
    Hands,
    Weapon,
    Accessory,
    Other,
    /// <summary>No single category has a majority, but every item is a clothing slot
    /// (Head/Top/Bottom/Shoes/Hands) - a coherent multi-piece outfit, not a random grab-bag.</summary>
    Outfit,
    /// <summary>No single category has a majority and the items aren't purely clothing -
    /// a genuinely incoherent mix (e.g. hair + weapon + accessory together).</summary>
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
        ("head", ChangedItemCategory.Head),
        ("chest", ChangedItemCategory.Top),
        ("torso", ChangedItemCategory.Top),
        ("hands", ChangedItemCategory.Hands),
        ("legs", ChangedItemCategory.Bottom),
        ("feet", ChangedItemCategory.Shoes),
        ("weapon", ChangedItemCategory.Weapon),
        ("mainhand", ChangedItemCategory.Weapon),
        ("offhand", ChangedItemCategory.Weapon),
        ("earring", ChangedItemCategory.Accessory),
        ("necklace", ChangedItemCategory.Accessory),
        ("bracelet", ChangedItemCategory.Accessory),
        ("ring", ChangedItemCategory.Accessory),
    ];

    private static readonly HashSet<ChangedItemCategory> ClothingCategories =
    [
        ChangedItemCategory.Head,
        ChangedItemCategory.Top,
        ChangedItemCategory.Bottom,
        ChangedItemCategory.Shoes,
        ChangedItemCategory.Hands,
    ];

    /// <summary>
    /// Classifies a mod's changed items by the dominant category. If the dominant category
    /// does not hold a strict majority (&gt;50%) of the changed items: if every item present
    /// is a clothing slot (Head/Top/Bottom/Shoes/Hands), the mod is a coherent multi-piece
    /// outfit and classified as Outfit; otherwise it's a genuinely incoherent mix and
    /// classified as Mixed. Neither is guessed as a single wrong category.
    ///
    /// Per item, the real Penumbra-reported equipment/customization slot
    /// (<see cref="PenumbraSlotResolver"/>) is used when available; if Penumbra can't map an
    /// item to a real slot (its own Unknown placeholder, or a non-equipment changed-item type),
    /// classification falls back to keyword-matching the item's display name instead of
    /// trusting a bogus category.
    /// </summary>
    public static ChangedItemCategory Classify(IReadOnlyDictionary<string, object?> changedItems)
    {
        if (changedItems.Count == 0)
            return ChangedItemCategory.Uncategorized;

        var counts = new Dictionary<ChangedItemCategory, int>();
        foreach (var (itemName, value) in changedItems)
        {
            var category = PenumbraSlotResolver.TryResolve(value) ?? ClassifyByName(itemName);
            counts[category] = counts.GetValueOrDefault(category) + 1;
        }

        var total = changedItems.Count;
        var dominant = counts.OrderByDescending(kv => kv.Value).First();

        if (dominant.Value * 2 > total)
            return dominant.Key;

        return counts.Keys.All(ClothingCategories.Contains) ? ChangedItemCategory.Outfit : ChangedItemCategory.Mixed;
    }

    private static ChangedItemCategory ClassifyByName(string itemName)
    {
        foreach (var (keyword, candidate) in Keywords)
        {
            if (itemName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return candidate;
        }

        return ChangedItemCategory.Other;
    }
}
