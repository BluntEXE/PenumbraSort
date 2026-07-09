using System.Collections.Generic;
using System.Linq;
using PenumbraSort.Backup;
using PenumbraSort.ModTree;
using Penumbra.Api.Enums;
using Xunit;

namespace PenumbraSort.Tests;

public class PathSnapshotTests
{
    [Fact]
    public void CaptureThenRestore_RevertsModsToPreviousPaths()
    {
        var ipc = new FakeIpc();
        ipc.AddMod("d1", "Mod1", "Old/Mod1");
        var mods = new List<ModEntry> { new() { Directory = "d1", Name = "Mod1", CurrentPath = "Old/Mod1" } };

        var snapshot = new PathSnapshot();
        snapshot.Capture(ipc, mods);

        ipc.SetModPath("d1", "New/Mod1", "Mod1");
        Assert.Equal("New/Mod1", ipc.GetModPath("d1", "Mod1").FullPath);

        var results = snapshot.Restore(ipc);

        Assert.Equal("Old/Mod1", ipc.GetModPath("d1", "Mod1").FullPath);
        Assert.Single(results);
        Assert.Equal(PenumbraApiEc.Success, results[0].Result);
    }

    [Fact]
    public void Capture_SkipsModsThatFailGetModPath()
    {
        var ipc = new FakeIpc(); // no mods registered
        var mods = new List<ModEntry> { new() { Directory = "missing", Name = "Ghost", CurrentPath = "X/Ghost" } };

        var snapshot = new PathSnapshot();
        snapshot.Capture(ipc, mods);

        Assert.False(snapshot.HasSnapshot);
    }

    [Fact]
    public void HasSnapshot_IsTrueAfterSuccessfulCapture()
    {
        var ipc = new FakeIpc();
        ipc.AddMod("d1", "Mod1", "Old/Mod1");
        var mods = new List<ModEntry> { new() { Directory = "d1", Name = "Mod1", CurrentPath = "Old/Mod1" } };

        var snapshot = new PathSnapshot();
        Assert.False(snapshot.HasSnapshot);

        snapshot.Capture(ipc, mods);

        Assert.True(snapshot.HasSnapshot);
    }

    [Fact]
    public void Restore_ContinuesPastFailedModAndReportsFailureResult()
    {
        var ipc = new FakeIpc();
        ipc.AddMod("d1", "Mod1", "Old/Mod1");
        ipc.AddMod("d2", "Mod2", "Old/Mod2");
        var mods = new List<ModEntry>
        {
            new() { Directory = "d1", Name = "Mod1", CurrentPath = "Old/Mod1" },
            new() { Directory = "d2", Name = "Mod2", CurrentPath = "Old/Mod2" },
        };

        var snapshot = new PathSnapshot();
        snapshot.Capture(ipc, mods);

        ipc.SetModPath("d1", "New/Mod1", "Mod1");
        ipc.SetModPath("d2", "New/Mod2", "Mod2");
        ipc.RemoveMod("d1"); // mod deleted between capture and restore

        var results = snapshot.Restore(ipc);

        Assert.Equal(2, results.Count);
        var d1Result = results.Single(r => r.Directory == "d1");
        var d2Result = results.Single(r => r.Directory == "d2");
        Assert.Equal(PenumbraApiEc.ModMissing, d1Result.Result);
        Assert.Equal(PenumbraApiEc.Success, d2Result.Result);
        Assert.Equal("Old/Mod2", ipc.GetModPath("d2", "Mod2").FullPath);
    }

    [Fact]
    public void Capture_CalledTwice_AccumulatesLayersInsteadOfOverwriting()
    {
        var ipc = new FakeIpc();
        ipc.AddMod("d1", "Mod1", "Old/Mod1");
        ipc.AddMod("d2", "Mod2", "Old/Mod2");

        var snapshot = new PathSnapshot();
        snapshot.Capture(ipc, new List<ModEntry>
        {
            new() { Directory = "d1", Name = "Mod1", CurrentPath = "Old/Mod1" },
            new() { Directory = "d2", Name = "Mod2", CurrentPath = "Old/Mod2" },
        });

        // Second capture only includes d1, but should be a NEW layer stacked on top,
        // not a replacement - the d2 layer must still be reachable by a later Restore.
        snapshot.Capture(ipc, new List<ModEntry>
        {
            new() { Directory = "d1", Name = "Mod1", CurrentPath = "Old/Mod1" },
        });

        Assert.Equal(2, snapshot.LayerCount);

        var firstRestore = snapshot.Restore(ipc);
        Assert.Single(firstRestore);
        Assert.Equal("d1", firstRestore[0].Directory);
        Assert.Equal(1, snapshot.LayerCount);

        var secondRestore = snapshot.Restore(ipc);
        Assert.Equal(2, secondRestore.Count);
        Assert.Contains(secondRestore, r => r.Directory == "d1");
        Assert.Contains(secondRestore, r => r.Directory == "d2");
        Assert.False(snapshot.HasSnapshot);
    }

    [Fact]
    public void Restore_StepsBackOneApplyAtATime_InsteadOfReplayingSameLayer()
    {
        // This is the fix for the real incident: multiple sequential Applies used to
        // strand every state but the very last one, because Restore replayed the same
        // overwritable snapshot instead of consuming a stack of them.
        var ipc = new FakeIpc();
        ipc.AddMod("d1", "Mod1", "Old/Mod1");
        var mods = new List<ModEntry> { new() { Directory = "d1", Name = "Mod1", CurrentPath = "Old/Mod1" } };

        var snapshot = new PathSnapshot();

        // Apply #1: Old -> New
        snapshot.Capture(ipc, mods);
        ipc.SetModPath("d1", "New/Mod1", "Mod1");

        // Apply #2: New -> Another
        snapshot.Capture(ipc, mods);
        ipc.SetModPath("d1", "Another/Mod1", "Mod1");

        Assert.Equal(2, snapshot.LayerCount);

        // First restore undoes Apply #2, landing back on New/Mod1.
        var firstRestore = snapshot.Restore(ipc);
        Assert.Single(firstRestore);
        Assert.Equal("New/Mod1", ipc.GetModPath("d1", "Mod1").FullPath);
        Assert.True(snapshot.HasSnapshot);

        // Second restore undoes Apply #1, landing back on the original path.
        var secondRestore = snapshot.Restore(ipc);
        Assert.Single(secondRestore);
        Assert.Equal("Old/Mod1", ipc.GetModPath("d1", "Mod1").FullPath);
        Assert.False(snapshot.HasSnapshot);

        // A third restore has nothing left to consume - a no-op, not a replay.
        var thirdRestore = snapshot.Restore(ipc);
        Assert.Empty(thirdRestore);
        Assert.Equal("Old/Mod1", ipc.GetModPath("d1", "Mod1").FullPath);
    }
}
