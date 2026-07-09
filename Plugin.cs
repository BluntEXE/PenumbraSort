using System;
using System.IO;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using PenumbraSort.Ipc;
using PenumbraSort.Protection;
using PenumbraSort.Session;
using PenumbraSort.UI;

namespace PenumbraSort;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "PenumbraSort";

    private const string SessionFileName = "session.json";

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private readonly Configuration _config;
    private readonly PenumbraIpc _ipc;
    private readonly ProtectionStore _protection = new();
    private readonly PlanState _planState = new();
    private readonly WindowSystem _windowSystem = new("PenumbraSort");
    private readonly MainWindow _mainWindow;
    private readonly ReviewWindow _reviewWindow;

    public Plugin()
    {
        _config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        _config.Initialize(PluginInterface);
        _protection.LoadFrom(_config.ProtectedModDirectories);

        _ipc = new PenumbraIpc(PluginInterface);

        var sessionPath = Path.Combine(PluginInterface.GetPluginConfigDirectory(), SessionFileName);
        try
        {
            var savedProposal = SessionStore.Load(sessionPath);
            if (savedProposal.Count > 0)
                _planState.Apply(savedProposal);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "PenumbraSort: failed to load saved session from {Path}, starting fresh.", sessionPath);
        }

        _reviewWindow = new ReviewWindow(_ipc);
        _mainWindow = new MainWindow(_ipc, _protection, _planState, _config, _reviewWindow, Log, sessionPath);

        _windowSystem.AddWindow(_mainWindow);
        _windowSystem.AddWindow(_reviewWindow);

        PluginInterface.UiBuilder.Draw += _windowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
        var registered = CommandManager.AddHandler("/pensort", new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the PenumbraSort window.",
        });
        Log.Information("PenumbraSort constructed. /pensort AddHandler returned {Registered}. IPC available: {IpcAvailable}", registered, _ipc.IsAvailable);
    }

    private void OnCommand(string command, string args)
    {
        Log.Information("PenumbraSort OnCommand fired: command={Command} args={Args}", command, args);
        _mainWindow.IsOpen = true;
        Log.Information("PenumbraSort MainWindow.IsOpen set to {IsOpen}", _mainWindow.IsOpen);
    }

    private void ToggleMainUi() => _mainWindow.Toggle();

    public void Dispose()
    {
        CommandManager.RemoveHandler("/pensort");
        PluginInterface.UiBuilder.Draw -= _windowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        _windowSystem.RemoveAllWindows();
        _ipc.Dispose();
    }
}
