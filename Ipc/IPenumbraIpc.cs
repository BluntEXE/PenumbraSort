using System;
using System.Collections.Generic;
using Penumbra.Api.Enums;

namespace PenumbraSort.Ipc;

public interface IPenumbraIpc
{
    IReadOnlyDictionary<string, string> GetModList();

    (PenumbraApiEc Code, string FullPath) GetModPath(string modDirectory, string modName);

    PenumbraApiEc SetModPath(string modDirectory, string newPath, string modName);

    IReadOnlyDictionary<string, object?> GetChangedItems(string modDirectory, string modName);

    event Action<string>? ModAdded;
    event Action<string>? ModDeleted;
    event Action<string, string>? ModMoved;

    bool IsAvailable { get; }
}
