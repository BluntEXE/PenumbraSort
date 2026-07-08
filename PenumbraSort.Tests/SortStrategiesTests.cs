using System.Collections.Generic;
using System.Linq;
using PenumbraSort.ModTree;
using PenumbraSort.Sorting;
using Xunit;

namespace PenumbraSort.Tests;

public class SortStrategiesTests
{
    private static ModEntry Mod(string dir, string name, string currentPath, Dictionary<string, object?>? items = null)
        => new()
        {
            Directory = dir,
            Name = name,
            CurrentPath = currentPath,
            ChangedItems = items ?? new Dictionary<string, object?>(),
        };

    [Fact]
    public void Manual_ProposesNoChanges()
    {
        var mods = new List<ModEntry> { Mod("d1", "Mod1", "Old/Mod1") };

        var proposal = SortStrategies.Apply(SortStrategyKind.Manual, mods);

        Assert.False(proposal.ContainsKey("d1"));
    }

    [Fact]
    public void ByCreator_UsesBracketPrefixInName()
    {
        var mods = new List<ModEntry> { Mod("d1", "[Foo Studio] Cool Hair", "Root/Cool Hair") };

        var proposal = SortStrategies.Apply(SortStrategyKind.ByCreator, mods);

        Assert.Equal("Foo Studio/[Foo Studio] Cool Hair", proposal["d1"]);
    }

    [Fact]
    public void ByCreator_FallsBackToUnknownCreator()
    {
        var mods = new List<ModEntry> { Mod("d1", "Cool Hair", "Root/Cool Hair") };

        var proposal = SortStrategies.Apply(SortStrategyKind.ByCreator, mods);

        Assert.Equal("Unknown Creator/Cool Hair", proposal["d1"]);
    }

    [Fact]
    public void ByType_UsesChangedItemCategory()
    {
        var mods = new List<ModEntry>
        {
            Mod("d1", "Cool Hair", "Root/Cool Hair", new Dictionary<string, object?> { ["Hair A"] = null }),
        };

        var proposal = SortStrategies.Apply(SortStrategyKind.ByType, mods);

        Assert.Equal("Hair/Cool Hair", proposal["d1"]);
    }

    [Fact]
    public void TypeThenCreator_CombinesBothDimensions()
    {
        var mods = new List<ModEntry>
        {
            Mod("d1", "[Foo Studio] Cool Hair", "Root/Cool Hair", new Dictionary<string, object?> { ["Hair A"] = null }),
        };

        var proposal = SortStrategies.Apply(SortStrategyKind.TypeThenCreator, mods);

        Assert.Equal("Hair/Foo Studio/[Foo Studio] Cool Hair", proposal["d1"]);
    }

    [Fact]
    public void Alphabetical_BucketsByFirstLetter()
    {
        var mods = new List<ModEntry> { Mod("d1", "zebra mod", "Root/zebra mod") };

        var proposal = SortStrategies.Apply(SortStrategyKind.Alphabetical, mods);

        Assert.Equal("Z/zebra mod", proposal["d1"]);
    }

    [Fact]
    public void ProtectedMods_AreExcludedFromProposal()
    {
        var mods = new List<ModEntry>
        {
            new() { Directory = "d1", Name = "Mod1", CurrentPath = "Old/Mod1", Protected = true },
        };

        var proposal = SortStrategies.Apply(SortStrategyKind.ByCreator, mods);

        Assert.False(proposal.ContainsKey("d1"));
    }

    [Fact]
    public void Unsorted_ReturnsOnlyRootLevelMods()
    {
        var mods = new List<ModEntry>
        {
            Mod("d1", "Mod1", "Mod1"),
            Mod("d2", "Mod2", "Folder/Mod2"),
        };

        var unsorted = SortStrategies.Unsorted(mods).ToList();

        Assert.Single(unsorted);
        Assert.Equal("d1", unsorted[0].Directory);
    }

    [Fact]
    public void CreatorThenType_CombinesBothDimensionsInOppositeOrder()
    {
        var mods = new List<ModEntry>
        {
            Mod("d1", "[Foo Studio] Cool Hair", "Root/Cool Hair", new Dictionary<string, object?> { ["Hair A"] = null }),
        };

        var proposal = SortStrategies.Apply(SortStrategyKind.CreatorThenType, mods);

        Assert.Equal("Foo Studio/Hair/[Foo Studio] Cool Hair", proposal["d1"]);
    }

    [Fact]
    public void PreserveAndClean_NoOpWhenPathAlreadyEqualsCurrentPath()
    {
        var mods = new List<ModEntry> { Mod("d1", "Mod1", "Some/Path/Mod1") };

        var proposal = SortStrategies.Apply(SortStrategyKind.PreserveAndClean, mods);

        Assert.False(proposal.ContainsKey("d1"));
    }

    [Fact]
    public void ApplyCustom_SubstitutesPlaceholders()
    {
        var mods = new List<ModEntry>
        {
            Mod("d1", "[Foo Studio] Cool Hair", "Root/Cool Hair", new Dictionary<string, object?> { ["Hair A"] = null }),
        };

        var proposal = SortStrategies.ApplyCustom("{Category}/{Creator}/{Name}", mods);

        Assert.Equal("Hair/Foo Studio/[Foo Studio] Cool Hair", proposal["d1"]);
    }

    [Fact]
    public void ApplyCustom_ExcludesProtectedMods()
    {
        var mods = new List<ModEntry>
        {
            new() { Directory = "d1", Name = "Mod1", CurrentPath = "Old/Mod1", Protected = true },
        };

        var proposal = SortStrategies.ApplyCustom("{Name}", mods);

        Assert.False(proposal.ContainsKey("d1"));
    }

    [Fact]
    public void Unsorted_ReturnsEmpty_WhenAllModsAreInFolders()
    {
        var mods = new List<ModEntry> { Mod("d1", "Mod1", "Folder/Mod1") };

        var unsorted = SortStrategies.Unsorted(mods).ToList();

        Assert.Empty(unsorted);
    }
}
