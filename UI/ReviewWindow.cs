using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using PenumbraSort.Backup;
using PenumbraSort.Ipc;
using PenumbraSort.ModTree;
using Penumbra.Api.Enums;

namespace PenumbraSort.UI;

public sealed class ReviewWindow : Window
{
    private readonly IPenumbraIpc _ipc;
    private readonly PathSnapshot _snapshot = new();
    private List<ModEntry> _pendingMods = new();
    private readonly HashSet<string> _skipped = new();
    private List<(string Directory, string ModName, PenumbraApiEc Result)>? _lastApplyResults;

    public ReviewWindow(IPenumbraIpc ipc) : base("Review Changes###PenumbraSortReview")
    {
        _ipc = ipc;
    }

    public void SetPending(IEnumerable<ModEntry> mods)
    {
        _pendingMods = mods.Where(m => m.ProposedPath is not null && m.ProposedPath != m.CurrentPath).ToList();
        _skipped.Clear();
        _lastApplyResults = null;
    }

    public override void Draw()
    {
        foreach (var mod in _pendingMods)
        {
            var skip = _skipped.Contains(mod.Directory);
            if (ImGui.Checkbox($"##skip-{mod.Directory}", ref skip))
            {
                if (skip) _skipped.Add(mod.Directory);
                else _skipped.Remove(mod.Directory);
            }
            ImGui.SameLine();
            ImGui.TextUnformatted($"{mod.Name}: {mod.CurrentPath} -> {mod.ProposedPath}");
        }

        if (ImGui.Button("Apply"))
            Apply();

        ImGui.SameLine();
        if (ImGui.Button("Restore Last Apply") && _snapshot.HasSnapshot)
            _snapshot.Restore(_ipc);

        if (_lastApplyResults is not null)
        {
            ImGui.Separator();
            foreach (var (directory, modName, result) in _lastApplyResults.Where(r => r.Result != PenumbraApiEc.Success))
                ImGui.TextColored(new System.Numerics.Vector4(1, 0.4f, 0.4f, 1), $"{modName} ({directory}): {result}");
        }
    }

    private void Apply()
    {
        var toApply = _pendingMods.Where(m => !_skipped.Contains(m.Directory)).ToList();
        _snapshot.Capture(_ipc, toApply);

        var results = new List<(string, string, PenumbraApiEc)>();
        foreach (var mod in toApply)
        {
            var result = _ipc.SetModPath(mod.Directory, mod.ProposedPath!, mod.Name);
            results.Add((mod.Directory, mod.Name, result));
        }

        _lastApplyResults = results;
    }
}
