using System.Collections.Generic;
using PenumbraSort.ModTree;
using Xunit;

namespace PenumbraSort.Tests;

public class ModTreeBuilderTests
{
    [Fact]
    public void Build_GroupsModsByPathSegments()
    {
        var mods = new List<ModEntry>
        {
            new() { Directory = "d1", Name = "Mod1", CurrentPath = "Creator A/Hair/Mod1" },
            new() { Directory = "d2", Name = "Mod2", CurrentPath = "Creator A/Hair/Mod2" },
            new() { Directory = "d3", Name = "Mod3", CurrentPath = "Creator B/Mod3" },
        };

        var root = ModTreeBuilder.Build(mods);

        var creatorA = root.Children["Creator A"];
        var hair = creatorA.Children["Hair"];
        Assert.Equal(2, hair.Mods.Count);
        Assert.Contains(hair.Mods, m => m.Directory == "d1");
        Assert.Contains(hair.Mods, m => m.Directory == "d2");

        var creatorB = root.Children["Creator B"];
        Assert.Single(creatorB.Mods);
        Assert.Equal("d3", creatorB.Mods[0].Directory);
    }

    [Fact]
    public void Build_UsesProposedPathWhenRequested()
    {
        var mods = new List<ModEntry>
        {
            new() { Directory = "d1", Name = "Mod1", CurrentPath = "Old/Mod1", ProposedPath = "New/Mod1" },
        };

        var root = ModTreeBuilder.Build(mods, useProposedPath: true);

        Assert.True(root.Children.ContainsKey("New"));
        Assert.False(root.Children.ContainsKey("Old"));
    }

    [Fact]
    public void Build_RootLevelModHasNoFolderSegments()
    {
        var mods = new List<ModEntry>
        {
            new() { Directory = "d1", Name = "Mod1", CurrentPath = "Mod1" },
        };

        var root = ModTreeBuilder.Build(mods);

        Assert.Single(root.Mods);
        Assert.Empty(root.Children);
    }
}
