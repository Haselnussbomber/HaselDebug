using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Services;
using HaselCommon.Utils;
using HaselDebug.Abstracts;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;

namespace HaselDebug.Tabs;

public unsafe class AgentsTab(
    TextService TextService,
    DebugRenderer DebugRenderer,
    ImGuiContextMenuService ImGuiContextMenu,
    PinnedInstancesService PinnedInstances) : DebugTab
{
    private ImmutableSortedDictionary<AgentId, (Pointer<AgentInterface> Address, Type Type)>? Agents;
    private AgentId SelectedAgentId = AgentId.Lobby;
    private string AgentNameSearchTerm = string.Empty;

    public override bool DrawInChild => false;
    public override void Draw()
    {
        Agents ??= typeof(AgentAttribute).Assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<AgentAttribute>() != null)
            .ToImmutableSortedDictionary(
                type => type.GetCustomAttribute<AgentAttribute>()!.Id,
                type => ((Pointer<AgentInterface>)AgentModule.Instance()->GetAgentByInternalId(type.GetCustomAttribute<AgentAttribute>()!.Id), type));

        DrawAgentsList();

        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);

        DrawAgent(SelectedAgentId);
    }

    private void DrawAgentsList()
    {
        using var sidebarchild = ImRaii.Child("AgentsListChild", new Vector2(300, -1), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
        if (!sidebarchild) return;

        ImGui.SetNextItemWidth(-1);
        var hasSearchTermChanged = ImGui.InputTextWithHint("##TextSearch", TextService.Translate("SearchBar.Hint"), ref AgentNameSearchTerm, 256, ImGuiInputTextFlags.AutoSelectAll);
        var hasSearchTerm = !string.IsNullOrWhiteSpace(AgentNameSearchTerm);
        var hasSearchTermAutoSelected = false;

        using var table = ImRaii.Table("AgentsTable", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings, new Vector2(300, -1));
        if (!table) return;

        ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Name");
        ImGui.TableSetupColumn("Active", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupScrollFreeze(3, 1);
        ImGui.TableHeadersRow();

        var agentModule = AgentModule.Instance();

        for (var i = 0; i < agentModule->Agents.Length; i++)
        {
            var agent = agentModule->Agents[i];
            var agentId = (AgentId)i;
            var agentName = Enum.GetName(agentId) ?? string.Empty;

            if (hasSearchTerm && !agentName.Contains(AgentNameSearchTerm, StringComparison.InvariantCultureIgnoreCase))
                continue;

            if (hasSearchTerm && !hasSearchTermAutoSelected)
            {
                SelectedAgentId = agentId;
                hasSearchTermAutoSelected = true;
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Id
            ImGui.TextUnformatted(i.ToString());

            ImGui.TableNextColumn(); // Name
            if (ImGui.Selectable(agentName + $"##Agent{i}", SelectedAgentId == agentId, ImGuiSelectableFlags.SpanAllColumns))
            {
                SelectedAgentId = agentId;
            }
            using (var contextMenu = ImRaii.ContextPopupItem($"##AgentContext{agentId}"))
            {
                if (contextMenu)
                {
                    if (!string.IsNullOrEmpty(agentName) && ImGui.MenuItem("Copy AgentId"))
                    {
                        ImGui.SetClipboardText(agentName);
                    }

                    if (ImGui.MenuItem("Copy Address"))
                    {
                        ImGui.SetClipboardText($"0x{(nint)agent.Value:X}");
                    }
                }
            }

            ImGui.TableNextColumn(); // Active
            ImGui.TextUnformatted(agent.Value->IsAgentActive().ToString());
        }
    }

    private void DrawAgent(AgentId agentId)
    {
        using var hostchild = ImRaii.Child("AgentChild", new Vector2(-1), true, ImGuiWindowFlags.NoSavedSettings);

        var agent = AgentModule.Instance()->GetAgentByInternalId(agentId);
        var agentType = Agents!.TryGetValue(agentId, out var value) ? value.Type : typeof(AgentInterface);

        DebugRenderer.DrawPointerType(agent, agentType, new NodeOptions()
        {
            DefaultOpen = true,
            DrawContextMenu = (nodeOptions) =>
            {
                if (agentType.Name == "AgentInterface") return;

                ImGuiContextMenu.Draw($"ContextMenu{nodeOptions.AddressPath}", builder =>
                {
                    var isPinned = PinnedInstances.Contains(agentType);

                    builder.Add(new ImGuiContextMenuEntry()
                    {
                        Visible = !isPinned,
                        Label = TextService.Translate("PinnedInstances.Pin"),
                        ClickCallback = () => PinnedInstances.Add((nint)agent, agentType)
                    });

                    builder.Add(new ImGuiContextMenuEntry()
                    {
                        Visible = isPinned,
                        Label = TextService.Translate("PinnedInstances.Unpin"),
                        ClickCallback = () => PinnedInstances.Remove(agentType)
                    });
                });
            }
        });
    }
}
