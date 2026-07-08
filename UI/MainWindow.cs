using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using PenumbraSort.Ipc;
using PenumbraSort.ModTree;
using PenumbraSort.Protection;
using PenumbraSort.Session;
using PenumbraSort.Sorting;

namespace PenumbraSort.UI;

public sealed class MainWindow : Window
{
    private readonly IPenumbraIpc _ipc;
    private readonly ProtectionStore _protection;
    private readonly PlanState _planState;
    private readonly Configuration _config;
    private readonly ReviewWindow _reviewWindow;
    private List<ModEntry> _mods = new();
    private SortStrategyKind _selectedStrategy;

    private readonly IPluginLog _log;

    public MainWindow(IPenumbraIpc ipc, ProtectionStore protection, PlanState planState, Configuration config, ReviewWindow reviewWindow, IPluginLog log)
        : base("PenumbraSort###PenumbraSortMain")
    {
        _ipc = ipc;
        _protection = protection;
        _planState = planState;
        _config = config;
        _reviewWindow = reviewWindow;
        _log = log;
        _selectedStrategy = config.LastStrategy;
    }

    public override void Draw()
    {
        if (!_ipc.IsAvailable)
        {
            ImGui.TextColored(new System.Numerics.Vector4(1, 0.4f, 0.4f, 1),
                "Penumbra IPC is not available. Make sure Penumbra is installed and enabled.");
            return;
        }

        if (ImGui.Button("Scan My Mods"))
            Scan();

        ImGui.SameLine();
        DrawStrategyDropdown();

        ImGui.SameLine();
        if (ImGui.Button("Review Changes"))
        {
            _reviewWindow.SetPending(_mods);
            _reviewWindow.IsOpen = true;
        }

        ImGui.Separator();

        if (ImGui.BeginTable("psort-panes", 2, ImGuiTableFlags.BordersInnerV))
        {
            ImGui.TableNextColumn();
            ImGui.TextUnformatted("Current");
            DrawTree(ModTreeBuilder.Build(_mods, useProposedPath: false));

            ImGui.TableNextColumn();
            ImGui.TextUnformatted("Proposed");
            DrawTree(ModTreeBuilder.Build(_mods, useProposedPath: true));

            ImGui.EndTable();
        }
    }

    private void Scan()
    {
        var modList = _ipc.GetModList();
        _mods = modList.Select(kvp =>
        {
            var (_, fullPath) = _ipc.GetModPath(kvp.Key, kvp.Value);
            var changedItems = _ipc.GetChangedItems(kvp.Key, kvp.Value);
            var category = PenumbraSort.Sorting.ChangedItemClassifier.Classify(changedItems);
            _log.Information(
                "PenumbraSort scan: mod={ModName} category={Category} changedItems=[{Items}]",
                kvp.Value, category, string.Join(", ", changedItems.Keys));
            return new ModEntry
            {
                Directory = kvp.Key,
                Name = kvp.Value,
                CurrentPath = fullPath,
                Protected = _protection.IsProtected(kvp.Key),
                ChangedItems = changedItems,
            };
        }).ToList();
    }

    private void DrawStrategyDropdown()
    {
        ImGui.SetNextItemWidth(200);
        if (ImGui.BeginCombo("Sort Strategy", _selectedStrategy.ToString()))
        {
            foreach (var kind in Enum.GetValues<SortStrategyKind>())
            {
                if (ImGui.Selectable(kind.ToString(), kind == _selectedStrategy))
                {
                    _selectedStrategy = kind;
                    ApplyStrategy(kind);
                }
            }
            ImGui.EndCombo();
        }
    }

    private void ApplyStrategy(SortStrategyKind kind)
    {
        if (kind == SortStrategyKind.Custom)
            return; // Custom requires a template string from a future text-input UI; skip for now rather than crash on Apply's guard exception.

        var proposal = SortStrategies.Apply(kind, _mods);
        foreach (var mod in _mods)
            mod.ProposedPath = proposal.TryGetValue(mod.Directory, out var path) ? path : null;

        _planState.Apply(proposal.ToDictionary(kv => kv.Key, kv => (string?)kv.Value));

        _config.LastStrategy = kind;
        _config.Save();
    }

    private void ToggleProtection(ModEntry mod, bool isProtected)
    {
        if (isProtected)
            _protection.Protect(mod.Directory);
        else
            _protection.Unprotect(mod.Directory);

        _config.ProtectedModDirectories = _protection.Snapshot().ToHashSet(StringComparer.OrdinalIgnoreCase);
        _config.Save();
    }

    private void DrawTree(ModTreeNode node)
    {
        foreach (var (name, child) in node.Children)
        {
            if (ImGui.TreeNode(name))
            {
                DrawTree(child);
                ImGui.TreePop();
            }
        }

        foreach (var mod in node.Mods)
        {
            var isProtected = mod.Protected;
            if (ImGui.Checkbox($"##protect-{mod.Directory}", ref isProtected))
                ToggleProtection(mod, isProtected);
            ImGui.SameLine();
            ImGui.TextUnformatted(mod.Name);
        }
    }
}
