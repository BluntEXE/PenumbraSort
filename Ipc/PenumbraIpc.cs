using System;
using System.Collections.Generic;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc.Exceptions;
using Penumbra.Api.Enums;
using Penumbra.Api.IpcSubscribers;

namespace PenumbraSort.Ipc;

public sealed class PenumbraIpc : IPenumbraIpc, IDisposable
{
    private readonly GetModList _getModList;
    private readonly GetModPath _getModPath;
    private readonly SetModPath _setModPath;
    private readonly GetChangedItems _getChangedItems;
    private readonly IDisposable _modAddedSubscriber;
    private readonly IDisposable _modDeletedSubscriber;
    private readonly IDisposable _modMovedSubscriber;
    private bool _disposed;

    public event Action<string>? ModAdded;
    public event Action<string>? ModDeleted;
    public event Action<string, string>? ModMoved;

    public PenumbraIpc(IDalamudPluginInterface pluginInterface)
    {
        _getModList = new GetModList(pluginInterface);
        _getModPath = new GetModPath(pluginInterface);
        _setModPath = new SetModPath(pluginInterface);
        _getChangedItems = new GetChangedItems(pluginInterface);

        _modAddedSubscriber = Penumbra.Api.IpcSubscribers.ModAdded.Subscriber(
            pluginInterface, directory => ModAdded?.Invoke(directory));
        _modDeletedSubscriber = Penumbra.Api.IpcSubscribers.ModDeleted.Subscriber(
            pluginInterface, directory => ModDeleted?.Invoke(directory));
        _modMovedSubscriber = Penumbra.Api.IpcSubscribers.ModMoved.Subscriber(
            pluginInterface, (oldDir, newDir) => ModMoved?.Invoke(oldDir, newDir));
    }

    public bool IsAvailable
    {
        get
        {
            try
            {
                _getModList.Invoke();
                return true;
            }
            catch (IpcNotReadyError)
            {
                return false;
            }
        }
    }

    public IReadOnlyDictionary<string, string> GetModList()
        => _getModList.Invoke();

    public (PenumbraApiEc Code, string FullPath) GetModPath(string modDirectory, string modName)
    {
        var (code, fullPath, _, _) = _getModPath.Invoke(modDirectory, modName);
        return (code, fullPath);
    }

    public PenumbraApiEc SetModPath(string modDirectory, string newPath, string modName)
        => _setModPath.Invoke(modDirectory, newPath, modName);

    public IReadOnlyDictionary<string, object?> GetChangedItems(string modDirectory, string modName)
        => _getChangedItems.Invoke(modDirectory, modName);

    public void Dispose()
    {
        if (_disposed)
            return;

        _modAddedSubscriber.Dispose();
        _modDeletedSubscriber.Dispose();
        _modMovedSubscriber.Dispose();
        _disposed = true;
    }
}
