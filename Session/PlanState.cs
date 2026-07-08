using System;
using System.Collections.Generic;

namespace PenumbraSort.Session;

public sealed class PlanState
{
    private readonly Stack<IReadOnlyDictionary<string, string?>> _undoStack = new();
    private readonly Stack<IReadOnlyDictionary<string, string?>> _redoStack = new();

    public IReadOnlyDictionary<string, string?> Current { get; private set; } =
        new Dictionary<string, string?>();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public void Apply(IReadOnlyDictionary<string, string?> newProposal)
    {
        ArgumentNullException.ThrowIfNull(newProposal);

        _undoStack.Push(Current);
        _redoStack.Clear();
        Current = new Dictionary<string, string?>(newProposal);
    }

    public void Undo()
    {
        if (!CanUndo)
            return;

        _redoStack.Push(Current);
        Current = _undoStack.Pop();
    }

    public void Redo()
    {
        if (!CanRedo)
            return;

        _undoStack.Push(Current);
        Current = _redoStack.Pop();
    }
}
