using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Gui;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Windows;
using ImGuiNET;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append)]
public class UnlocksTab : DebugTab
{
    public override unsafe bool DrawInChild => !AgentLobby.Instance()->IsLoggedIn;
    public override bool IsPinnable => false;
    public override bool CanPopOut => false;

    public UnlocksTab(IEnumerable<IUnlockTab> subTabs)
    {
        var unlockTabs = subTabs.OrderBy(t => t.Title).ToArray();
        SubTabs = unlockTabs.Cast<ISubTab<UnlocksTab>>().ToImmutableArray();
    }

    public override unsafe void Draw()
    {
        if (!AgentLobby.Instance()->IsLoggedIn || SubTabs == null){
            ImGui.TextUnformatted("Not logged in.");
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
                Service.Get<PluginWindow>().SelectTab(tab.InternalName);
            }

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(canShowProgress
                ? $"{progress.NumUnlocked} / {progress.TotalUnlocks} ({progress.NumUnlocked / (float)progress.TotalUnlocks * 100f:0.00}%)"
                : "Missing Data");
        }
    }
}
