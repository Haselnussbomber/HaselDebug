using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class UnlocksTab : DebugTab
{
    private readonly IServiceProvider _serviceProvider;

    public override unsafe bool DrawInChild => !AgentLobby.Instance()->IsLoggedIn;
    public override bool IsPinnable => false;
    public override bool CanPopOut => false;

    [AutoPostConstruct]
    private void Initialize(IEnumerable<IUnlockTab> subTabs)
    {
        SubTabs = subTabs
            .OrderBy(t => t.Title).ToArray()
            .Cast<IDebugTab>().ToImmutableArray();
    }

    public override unsafe void Draw()
    {
        if (!AgentLobby.Instance()->IsLoggedIn || SubTabs == null)
        {
            ImGui.Text("Not logged in.");
            return;
        }

        using var table = ImRaii.Table("UnlocksTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg, new Vector2(-1));
        if (!table) return;

        ImGui.TableSetupColumn("Tab", ImGuiTableColumnFlags.WidthFixed, 200);
        ImGui.TableSetupColumn("Progress", ImGuiTableColumnFlags.WidthFixed, 200);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        foreach (IUnlockTab tab in SubTabs)
        {
            ImGui.TableNextRow();
            var progress = tab.GetUnlockProgress();
            var canShowProgress = !progress.NeedsExtraData || (progress.NeedsExtraData && progress.HasExtraData);

            ImGui.TableNextColumn();
            if (ImGui.Selectable(tab.Title, false, ImGuiSelectableFlags.SpanAllColumns))
            {
                _serviceProvider.GetRequiredService<PluginWindow>().SelectTab(tab.InternalName);
            }

            ImGui.TableNextColumn();
            ImGui.Text(canShowProgress
                ? $"{progress.NumUnlocked} / {progress.TotalUnlocks} ({progress.NumUnlocked / (float)progress.TotalUnlocks * 100f:0.00}%)"
                : "Missing Data");
        }
    }
}
