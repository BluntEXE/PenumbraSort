using Dalamud.IoC;
using Dalamud.Plugin;

namespace PenumbraSort;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "PenumbraSort";

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    public Plugin()
    {
    }

    public void Dispose()
    {
    }
}
