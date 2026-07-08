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
}
