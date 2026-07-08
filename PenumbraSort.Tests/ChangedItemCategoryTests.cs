using System.Collections.Generic;
using PenumbraSort.Sorting;
using Xunit;

namespace PenumbraSort.Tests;

public class ChangedItemCategoryTests
{
    [Fact]
    public void Classify_EmptyChangedItems_ReturnsUncategorized()
    {
        var result = ChangedItemClassifier.Classify(new Dictionary<string, object?>());
        Assert.Equal(ChangedItemCategory.Uncategorized, result);
    }

    [Fact]
    public void Classify_AllSameCategory_ReturnsThatCategory()
    {
        var items = new Dictionary<string, object?>
        {
            ["Hair (Female)"] = null,
            ["Hair (Male)"] = null,
        };

        var result = ChangedItemClassifier.Classify(items);
        Assert.Equal(ChangedItemCategory.Hair, result);
    }

    [Fact]
    public void Classify_StrictMajority_ReturnsDominantCategory()
    {
        var items = new Dictionary<string, object?>
        {
            ["Hair A"] = null,
            ["Hair B"] = null,
            ["Hair C"] = null,
            ["Legs"] = null,
        };

        var result = ChangedItemClassifier.Classify(items);
        Assert.Equal(ChangedItemCategory.Hair, result);
    }

    [Fact]
    public void Classify_NoMajority_ReturnsMixed()
    {
        var items = new Dictionary<string, object?>
        {
            ["Hair A"] = null,
            ["Hair B"] = null,
            ["Legs"] = null,
            ["Legs 2"] = null,
        };

        var result = ChangedItemClassifier.Classify(items);
        Assert.Equal(ChangedItemCategory.Mixed, result);
    }

    [Fact]
    public void Classify_OneVersusOne_ReturnsMixed()
    {
        var items = new Dictionary<string, object?>
        {
            ["Hair"] = null,
            ["Legs"] = null,
        };

        var result = ChangedItemClassifier.Classify(items);
        Assert.Equal(ChangedItemCategory.Mixed, result);
    }

    [Fact]
    public void Classify_Face_ReturnsFace()
    {
        var items = new Dictionary<string, object?>
        {
            ["Face"] = null,
            ["Ears"] = null,
        };

        var result = ChangedItemClassifier.Classify(items);
        Assert.Equal(ChangedItemCategory.Face, result);
    }

    [Fact]
    public void Classify_Body_ReturnsBody()
    {
        var items = new Dictionary<string, object?>
        {
            ["Body"] = null,
            ["Tail"] = null,
        };

        var result = ChangedItemClassifier.Classify(items);
        Assert.Equal(ChangedItemCategory.Body, result);
    }

    [Fact]
    public void Classify_Top_ReturnsTop()
    {
        var items = new Dictionary<string, object?>
        {
            ["Head"] = null,
            ["Hands"] = null,
        };

        var result = ChangedItemClassifier.Classify(items);
        Assert.Equal(ChangedItemCategory.Top, result);
    }

    [Fact]
    public void Classify_Weapon_ReturnsWeapon()
    {
        var items = new Dictionary<string, object?>
        {
            ["Weapon"] = null,
            ["Mainhand"] = null,
            ["Offhand"] = null,
        };

        var result = ChangedItemClassifier.Classify(items);
        Assert.Equal(ChangedItemCategory.Weapon, result);
    }

    [Fact]
    public void Classify_Accessory_ReturnsAccessory()
    {
        var items = new Dictionary<string, object?>
        {
            ["Earring"] = null,
            ["Necklace"] = null,
            ["Bracelet"] = null,
            ["Ring"] = null,
        };

        var result = ChangedItemClassifier.Classify(items);
        Assert.Equal(ChangedItemCategory.Accessory, result);
    }

    [Fact]
    public void Classify_UnmatchedKeyword_ReturnsOther()
    {
        var items = new Dictionary<string, object?>
        {
            ["Glasses"] = null,
        };

        var result = ChangedItemClassifier.Classify(items);
        Assert.Equal(ChangedItemCategory.Other, result);
    }

    [Fact]
    public void Classify_IsCaseInsensitive()
    {
        var items = new Dictionary<string, object?>
        {
            ["HAIR"] = null,
            ["hAiR"] = null,
        };

        var result = ChangedItemClassifier.Classify(items);
        Assert.Equal(ChangedItemCategory.Hair, result);
    }
}
