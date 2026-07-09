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
    private readonly string _sessionPath;
    private List<ModEntry> _mods = new();
    private SortStrategyKind _selectedStrategy;
    private string _customTemplate;

    private readonly IPluginLog _log;

    public MainWindow(IPenumbraIpc ipc, ProtectionStore protection, PlanState planState, Configuration config, ReviewWindow reviewWindow, IPluginLog log, string sessionPath)
        : base("PenumbraSort###PenumbraSortMain")
    {
        _ipc = ipc;
        _protection = protection;
        _planState = planState;
        _config = config;
        _reviewWindow = reviewWindow;
        _log = log;
        _sessionPath = sessionPath;
        _selectedStrategy = config.LastStrategy;
        _customTemplate = config.CustomTemplate;
    }

    public override void OnOpen()
    {
        if (!_ipc.IsAvailable)
            return;

        Scan();
        ApplyStrategy(_selectedStrategy);
    }

    public override void Draw()
    {
        if (!_ipc.IsAvailable)
        {
            ImGui.TextColored(new System.Numerics.Vector4(1, 0.4f, 0.4f, 1),
                "Penumbra IPC is not available. Make sure Penumbra is installed and enabled.");
            return;
        }

        ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 1),
            "Best for a loose, unsorted mod list. If you already have a working folder structure, " +
            "sorting may reorganize things you don't want touched - use Protect below, or check the Current " +
            "pane against Proposed carefully before applying.");
        ImGui.Separator();

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
            DrawTree(ModTreeBuilder.Build(_mods, useProposedPath: false), showProtectCheckbox: true);

            ImGui.TableNextColumn();
            ImGui.TextUnformatted("Proposed");
            DrawTree(ModTreeBuilder.Build(_mods, useProposedPath: true), showProtectCheckbox: false);

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

        if (_selectedStrategy == SortStrategyKind.Custom)
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(260);
            if (ImGui.InputText("##customTemplate", ref _customTemplate, 256))
                ApplyStrategy(SortStrategyKind.Custom);

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Placeholders: {Creator}, {Category}, {Name}\nExample: {Category}/{Creator}/{Name}");
        }
    }

    private void ApplyStrategy(SortStrategyKind kind)
    {
        if (kind == SortStrategyKind.Custom && string.IsNullOrWhiteSpace(_customTemplate))
            return; // no template entered yet - nothing to propose

        var proposal = kind == SortStrategyKind.Custom
            ? SortStrategies.ApplyCustom(_customTemplate, _mods)
            : SortStrategies.Apply(kind, _mods);

        foreach (var mod in _mods)
            mod.ProposedPath = proposal.TryGetValue(mod.Directory, out var path) ? path : null;

        var planStateProposal = proposal.ToDictionary(kv => kv.Key, kv => (string?)kv.Value);
        _planState.Apply(planStateProposal);

        try
        {
            SessionStore.Save(_sessionPath, planStateProposal);
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "PenumbraSort: failed to save session to {Path}.", _sessionPath);
        }

        _config.LastStrategy = kind;
        if (kind == SortStrategyKind.Custom)
            _config.CustomTemplate = _customTemplate;
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

    // showProtectCheckbox: protection is one shared piece of state per mod, not a
    // per-pane concept, so the toggle is only drawn once (in the Current pane) to avoid
    // showing the same checkbox twice for every mod across both trees.
    private void DrawTree(ModTreeNode node, bool showProtectCheckbox)
    {
        foreach (var (name, child) in node.Children)
        {
            if (ImGui.TreeNode(name))
            {
                DrawTree(child, showProtectCheckbox);
                ImGui.TreePop();
            }
        }

        foreach (var mod in node.Mods)
        {
            if (showProtectCheckbox)
            {
                var isProtected = mod.Protected;
                if (ImGui.Checkbox($"##protect-{mod.Directory}", ref isProtected))
                    ToggleProtection(mod, isProtected);
                ImGui.SameLine();
            }

            ImGui.TextUnformatted(mod.Name);
        }
    }
}
