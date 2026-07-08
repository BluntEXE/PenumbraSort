using System.Collections.Generic;
using PenumbraSort.ModTree;
using PenumbraSort.Protection;
using Xunit;

namespace PenumbraSort.Tests;

public class ProtectionStoreTests
{
    [Fact]
    public void Protect_MarksDirectoryAsProtected()
    {
        var store = new ProtectionStore();
        store.Protect("d1");

        Assert.True(store.IsProtected("d1"));
        Assert.False(store.IsProtected("d2"));
    }

    [Fact]
    public void Unprotect_RemovesProtection()
    {
        var store = new ProtectionStore();
        store.Protect("d1");
        store.Unprotect("d1");

        Assert.False(store.IsProtected("d1"));
    }

    [Fact]
    public void ProtectFolder_ProtectsEveryModUnderNode()
    {
        var store = new ProtectionStore();
        var root = ModTreeBuilder.Build(new List<ModEntry>
        {
            new() { Directory = "d1", Name = "Mod1", CurrentPath = "Folder/Sub/Mod1" },
            new() { Directory = "d2", Name = "Mod2", CurrentPath = "Folder/Mod2" },
        });

        store.ProtectFolder(root.Children["Folder"]);

        Assert.True(store.IsProtected("d1"));
        Assert.True(store.IsProtected("d2"));
    }

    [Fact]
    public void UnprotectFolder_RemovesProtectionForEveryModUnderNode()
    {
        var store = new ProtectionStore();
        var root = ModTreeBuilder.Build(new List<ModEntry>
        {
            new() { Directory = "d1", Name = "Mod1", CurrentPath = "Folder/Sub/Mod1" },
            new() { Directory = "d2", Name = "Mod2", CurrentPath = "Folder/Mod2" },
        });
        store.ProtectFolder(root.Children["Folder"]);

        store.UnprotectFolder(root.Children["Folder"]);

        Assert.False(store.IsProtected("d1"));
        Assert.False(store.IsProtected("d2"));
    }

    [Fact]
    public void LoadFrom_ReplacesExistingSet()
    {
        var store = new ProtectionStore();
        store.Protect("stale");

        store.LoadFrom(new[] { "d1", "d2" });

        Assert.False(store.IsProtected("stale"));
        Assert.True(store.IsProtected("d1"));
        Assert.True(store.IsProtected("d2"));
    }

    [Fact]
    public void LoadFrom_EmptyEnumerable_ClearsStore()
    {
        var store = new ProtectionStore();
        store.Protect("stale");

        store.LoadFrom(System.Array.Empty<string>());

        Assert.False(store.IsProtected("stale"));
    }

    [Fact]
    public void IsProtected_IsCaseInsensitive()
    {
        var store = new ProtectionStore();
        store.Protect("D1");

        Assert.True(store.IsProtected("d1"));
        Assert.True(store.IsProtected("d1".ToUpperInvariant()));
    }

    [Fact]
    public void Snapshot_ReflectsCurrentProtectedSet()
    {
        var store = new ProtectionStore();
        store.Protect("d1");
        store.Protect("d2");

        var snapshot = store.Snapshot();

        Assert.Equal(2, snapshot.Count);
        Assert.Contains("d1", snapshot);
        Assert.Contains("d2", snapshot);
    }

    [Fact]
    public void ProtectFolder_OnEmptyNode_ProtectsNothing()
    {
        var store = new ProtectionStore();
        var emptyNode = new ModTreeNode("Empty");

        store.ProtectFolder(emptyNode);

        Assert.Empty(store.Snapshot());
    }
}
