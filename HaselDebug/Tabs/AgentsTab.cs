using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Graphics;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Extensions;
using HaselDebug.Services;
using HaselDebug.Utils;
using HaselDebug.Windows;
using ImGuiNET;

namespace HaselDebug.Tabs;

public unsafe class AgentsTab(
    TextService TextService,
    DebugRenderer DebugRenderer,
    ImGuiContextMenuService ImGuiContextMenu,
    PinnedInstancesService PinnedInstances,
    WindowManager WindowManager) : DebugTab
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
            var (agentName, isAgentNameAddonName) = GetAgentName(agentId);

            if (hasSearchTerm && !agentName.Contains(AgentNameSearchTerm, StringComparison.InvariantCultureIgnoreCase))
                continue;

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Id
            ImGui.TextUnformatted(i.ToString());

            ImGui.TableNextColumn(); // Name

            using (Color.Yellow.Push(ImGuiCol.Text, isAgentNameAddonName))
            {
                if (ImGui.Selectable(agentName + $"###AgentSelectable{i}", SelectedAgentId == agentId, ImGuiSelectableFlags.SpanAllColumns))
                {
                    SelectedAgentId = agentId;
                }
            }
            ImGuiContextMenu.Draw($"ContextMenuAgent{i}", builder =>
            {
                if (!DebugRenderer.AgentTypes.TryGetValue(agentId, out var agentType))
                    agentType = typeof(AgentInterface);

                var pinnedInstances = Service.Get<PinnedInstancesService>();
                var isPinned = pinnedInstances.Contains(agentType);

                builder.AddCopyName(TextService, agentId.ToString());
                builder.AddCopyAddress(TextService, (nint)agent.Value);

                builder.AddSeparator();

                builder.Add(new ImGuiContextMenuEntry()
                {
                    Visible = !WindowManager.Contains(agentType.Name),
                    Label = TextService.Translate("ContextMenu.TabPopout"),
                    ClickCallback = () => WindowManager.Open(new PointerTypeWindow(WindowManager, DebugRenderer, (nint)agent.Value, agentType))
                });

                builder.Add(new ImGuiContextMenuEntry()
                {
                    Visible = !isPinned,
                    Label = TextService.Translate("ContextMenu.PinnedInstances.Pin"),
                    ClickCallback = () => pinnedInstances.Add((nint)agent.Value, agentType)
                });

                builder.Add(new ImGuiContextMenuEntry()
                {
                    Visible = isPinned,
                    Label = TextService.Translate("ContextMenu.PinnedInstances.Unpin"),
                    ClickCallback = () => pinnedInstances.Remove(agentType)
                });
            });

            ImGui.TableNextColumn(); // Active
            ImGui.TextUnformatted(agent.Value->IsAgentActive().ToString());
        }
    }

    private (string, bool) GetAgentName(AgentId agentId)
    {
        var name = Enum.GetName(agentId);
        if (!string.IsNullOrEmpty(name))
            return (name, false);

        if (TryGetAddon<AtkUnitBase>(agentId, out var addon) && !string.IsNullOrEmpty(addon->NameString))
            return (addon->NameString, true);

        return (string.Empty, false);
    }

    private void DrawAgent(AgentId agentId)
    {
        using var hostchild = ImRaii.Child("AgentChild", new Vector2(-1), true, ImGuiWindowFlags.NoSavedSettings);

        var agent = AgentModule.Instance()->GetAgentByInternalId(agentId);
        var agentType = Agents!.TryGetValue(agentId, out var value) ? value.Type : typeof(AgentInterface);

        DebugRenderer.DrawPointerType(agent, agentType, new NodeOptions()
        {
            DefaultOpen = true,
            DrawContextMenu = (nodeOptions, builder) =>
            {
                if (agentType.Name == "AgentInterface") return;

                var isPinned = PinnedInstances.Contains(agentType);

                builder.Add(new ImGuiContextMenuEntry()
                {
                    Visible = !WindowManager.Contains(agentType.Name),
                    Label = TextService.Translate("ContextMenu.TabPopout"),
                    ClickCallback = () => WindowManager.Open(new PointerTypeWindow(WindowManager, DebugRenderer, (nint)agent, agentType))
                });

                builder.Add(new ImGuiContextMenuEntry()
                {
                    Visible = !isPinned,
                    Label = TextService.Translate("ContextMenu.PinnedInstances.Pin"),
                    ClickCallback = () => PinnedInstances.Add((nint)agent, agentType)
                });

                builder.Add(new ImGuiContextMenuEntry()
                {
                    Visible = isPinned,
                    Label = TextService.Translate("ContextMenu.PinnedInstances.Unpin"),
                    ClickCallback = () => PinnedInstances.Remove(agentType)
                });
            }
        });
    }
}
