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

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;

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

        // TODO: SessionStore (PenumbraSort.Session.SessionStore) save/load of the in-progress
        // proposal is intentionally NOT wired in here yet. Not required for a working /psort
        // command. Wiring must wrap SessionStore.Load in try/catch: it deliberately throws on
        // corrupt JSON or a missing target directory (see Session/SessionStore.cs).
        _mainWindow = new MainWindow(_ipc, _protection, _planState, _config);
        _reviewWindow = new ReviewWindow(_ipc);

        _windowSystem.AddWindow(_mainWindow);
        _windowSystem.AddWindow(_reviewWindow);

        PluginInterface.UiBuilder.Draw += _windowSystem.Draw;
        CommandManager.AddHandler("/psort", new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the PenumbraSort window.",
        });
    }

    private void OnCommand(string command, string args)
        => _mainWindow.IsOpen = true;

    public void Dispose()
    {
        CommandManager.RemoveHandler("/psort");
        PluginInterface.UiBuilder.Draw -= _windowSystem.Draw;
        _windowSystem.RemoveAllWindows();
        _ipc.Dispose();
    }
}
