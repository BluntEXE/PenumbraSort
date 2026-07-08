using System.Collections.Generic;
using PenumbraSort.Session;
using Xunit;

namespace PenumbraSort.Tests;

public class PlanStateTests
{
    [Fact]
    public void Apply_SetsCurrentProposal()
    {
        var state = new PlanState();
        state.Apply(new Dictionary<string, string?> { ["d1"] = "New/Path" });

        Assert.Equal("New/Path", state.Current["d1"]);
    }

    [Fact]
    public void Undo_RevertsToPreviousProposal()
    {
        var state = new PlanState();
        state.Apply(new Dictionary<string, string?> { ["d1"] = "First" });
        state.Apply(new Dictionary<string, string?> { ["d1"] = "Second" });

        state.Undo();

        Assert.Equal("First", state.Current["d1"]);
    }

    [Fact]
    public void Redo_ReappliesUndoneProposal()
    {
        var state = new PlanState();
        state.Apply(new Dictionary<string, string?> { ["d1"] = "First" });
        state.Apply(new Dictionary<string, string?> { ["d1"] = "Second" });
        state.Undo();

        state.Redo();

        Assert.Equal("Second", state.Current["d1"]);
    }

    [Fact]
    public void Apply_AfterUndo_ClearsRedoStack()
    {
        var state = new PlanState();
        state.Apply(new Dictionary<string, string?> { ["d1"] = "First" });
        state.Apply(new Dictionary<string, string?> { ["d1"] = "Second" });
        state.Undo();

        state.Apply(new Dictionary<string, string?> { ["d1"] = "Third" });

        Assert.False(state.CanRedo);
    }

    [Fact]
    public void Undo_WhenNothingToUndo_IsNoOp()
    {
        var state = new PlanState();
        state.Undo();

        Assert.False(state.CanUndo);
        Assert.Empty(state.Current);
    }

    [Fact]
    public void Redo_WhenNothingToRedo_IsNoOp()
    {
        var state = new PlanState();
        state.Apply(new Dictionary<string, string?> { ["d1"] = "First" });

        state.Redo();

        Assert.False(state.CanRedo);
        Assert.Equal("First", state.Current["d1"]);
    }

    [Fact]
    public void UndoRedo_MultipleCycles_TrackStateCorrectly()
    {
        var state = new PlanState();
        state.Apply(new Dictionary<string, string?> { ["d1"] = "First" });
        state.Apply(new Dictionary<string, string?> { ["d1"] = "Second" });
        state.Apply(new Dictionary<string, string?> { ["d1"] = "Third" });

        state.Undo();
        Assert.Equal("Second", state.Current["d1"]);
        state.Undo();
        Assert.Equal("First", state.Current["d1"]);
        Assert.True(state.CanUndo);
        Assert.False(state.CanUndo && state.Current.Count == 0);

        state.Redo();
        Assert.Equal("Second", state.Current["d1"]);
        state.Redo();
        Assert.Equal("Third", state.Current["d1"]);
        Assert.False(state.CanRedo);
        Assert.True(state.CanUndo);
    }

    [Fact]
    public void Apply_AfterDeepUndo_FullyClearsRedoStack()
    {
        var state = new PlanState();
        state.Apply(new Dictionary<string, string?> { ["d1"] = "First" });
        state.Apply(new Dictionary<string, string?> { ["d1"] = "Second" });
        state.Apply(new Dictionary<string, string?> { ["d1"] = "Third" });

        state.Undo();
        state.Undo();
        Assert.True(state.CanRedo);

        state.Apply(new Dictionary<string, string?> { ["d1"] = "Fourth" });

        Assert.False(state.CanRedo);
        state.Redo();
        Assert.Equal("Fourth", state.Current["d1"]);
    }

    [Fact]
    public void Undo_ToInitialEmptyState_ThenCanUndoIsFalse()
    {
        var state = new PlanState();
        state.Apply(new Dictionary<string, string?> { ["d1"] = "First" });

        state.Undo();

        Assert.False(state.CanUndo);
        Assert.Empty(state.Current);
    }
}
