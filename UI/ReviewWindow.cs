using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
        var includedCount = _pendingMods.Count - _skipped.Count;
        ImGui.TextUnformatted($"{_pendingMods.Count} changes ({includedCount} included, {_skipped.Count} skipped)");

        if (ImGui.Button("Select All"))
            _skipped.Clear();
        ImGui.SameLine();
        if (ImGui.Button("Skip All"))
        {
            _skipped.Clear();
            foreach (var mod in _pendingMods)
                _skipped.Add(mod.Directory);
        }

        ImGui.SameLine();
        if (ImGui.Button("Apply"))
            Apply();

        ImGui.SameLine();
        if (ImGui.Button("Restore Last Apply") && _snapshot.HasSnapshot)
            _snapshot.Restore(_ipc);

        ImGui.Separator();

        if (ImGui.BeginChild("psort-review-scroll", new Vector2(0, -1)))
        {
            foreach (var group in GroupByTopLevelFolder(_pendingMods))
            {
                if (ImGui.CollapsingHeader($"{group.Key} ({group.Value.Count})###group-{group.Key}", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    foreach (var mod in group.Value)
                        DrawRow(mod);
                }
            }

            if (_lastApplyResults is not null)
            {
                ImGui.Separator();
                foreach (var (directory, modName, result) in _lastApplyResults.Where(r => r.Result != PenumbraApiEc.Success))
                    ImGui.TextColored(new Vector4(1, 0.4f, 0.4f, 1), $"{modName} ({directory}): {result}");
            }
        }

        ImGui.EndChild();
    }

    private void DrawRow(ModEntry mod)
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

    /// <summary>
    /// Groups by the first path segment of each mod's proposed path - strategy-agnostic
    /// (works whether the proposal came from ByType, ByCreator, Alphabetical, etc.) since it's
    /// just the top-level folder the mod will actually land in.
    /// </summary>
    private static IEnumerable<KeyValuePair<string, List<ModEntry>>> GroupByTopLevelFolder(IEnumerable<ModEntry> mods)
    {
        var groups = new Dictionary<string, List<ModEntry>>(StringComparer.OrdinalIgnoreCase);
        foreach (var mod in mods)
        {
            var topLevel = mod.ProposedPath!.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? mod.ProposedPath!;
            if (!groups.TryGetValue(topLevel, out var list))
            {
                list = new List<ModEntry>();
                groups[topLevel] = list;
            }

            list.Add(mod);
        }

        return groups.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase);
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
