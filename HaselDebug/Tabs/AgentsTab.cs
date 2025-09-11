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
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using HaselDebug.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class AgentsTab : DebugTab
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TextService _textService;
    private readonly LanguageProvider _languageProvider;
    private readonly DebugRenderer _debugRenderer;
    private readonly ImGuiContextMenuService _imGuiContextMenu;
    private readonly PinnedInstancesService _pinnedInstances;
    private readonly WindowManager _windowManager;

    private ImmutableSortedDictionary<AgentId, (Pointer<AgentInterface> Address, Type Type)>? _agents;
    private AgentId _selectedAgentId = AgentId.Lobby;
    private string _agentNameSearchTerm = string.Empty;

    public override bool DrawInChild => false;

    public override void Draw()
    {
        _agents ??= typeof(AgentAttribute).Assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<AgentAttribute>() != null)
            .ToImmutableSortedDictionary(
                type => type.GetCustomAttribute<AgentAttribute>()!.Id,
                type => ((Pointer<AgentInterface>)AgentModule.Instance()->GetAgentByInternalId(type.GetCustomAttribute<AgentAttribute>()!.Id), type));

        DrawAgentsList();

        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);

        DrawAgent(_selectedAgentId);
    }

    private void DrawAgentsList()
    {
        using var sidebarchild = ImRaii.Child("AgentsListChild", new Vector2(300, -1), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
        if (!sidebarchild) return;

        ImGui.SetNextItemWidth(-1);
        var hasSearchTermChanged = ImGui.InputTextWithHint("##TextSearch", _textService.Translate("SearchBar.Hint"), ref _agentNameSearchTerm, 256, ImGuiInputTextFlags.AutoSelectAll);
        var hasSearchTerm = !string.IsNullOrWhiteSpace(_agentNameSearchTerm);

        using var table = ImRaii.Table("AgentsTable"u8, 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings, new Vector2(300, -1));
        if (!table) return;

        ImGui.TableSetupColumn("Id"u8, ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Name");
        ImGui.TableSetupColumn("Active"u8, ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupScrollFreeze(3, 1);
        ImGui.TableHeadersRow();

        var agentModule = AgentModule.Instance();

        for (var i = 0; i < agentModule->Agents.Length; i++)
        {
            var agent = agentModule->Agents[i];
            var agentId = (AgentId)i;
            var (agentName, isAgentNameAddonName) = GetAgentName(agentId);
            var isActive = agent.Value->IsAgentActive();

            if (hasSearchTerm && !agentName.Contains(_agentNameSearchTerm, StringComparison.InvariantCultureIgnoreCase))
                continue;

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Id
            using (Color.Green.Push(ImGuiCol.Text, isActive))
                ImGui.Text(i.ToString());

            ImGui.TableNextColumn(); // Name

            using (Color.Green.Push(ImGuiCol.Text, isActive))
            using (Color.Yellow.Push(ImGuiCol.Text, isAgentNameAddonName))
            {
                if (ImGui.Selectable(agentName + $"###AgentSelectable{i}", _selectedAgentId == agentId, ImGuiSelectableFlags.SpanAllColumns))
                {
                    _selectedAgentId = agentId;
                }
            }
            _imGuiContextMenu.Draw($"ContextMenuAgent{i}", builder =>
            {
                if (!_debugRenderer.AgentTypes.TryGetValue(agentId, out var agentType))
                    agentType = typeof(AgentInterface);

                var isPinned = _pinnedInstances.Contains(agentType);

                builder.AddCopyName(_textService, agentId.ToString());
                builder.AddCopyAddress(_textService, (nint)agent.Value);

                builder.AddSeparator();

                builder.Add(new ImGuiContextMenuEntry()
                {
                    Visible = !_windowManager.Contains(win => win.WindowName == agentType.Name),
                    Label = _textService.Translate("ContextMenu.TabPopout"),
                    ClickCallback = () => _windowManager.Open(ActivatorUtilities.CreateInstance<PointerTypeWindow>(_serviceProvider, (nint)agent.Value, agentType, string.Empty))
                });

                builder.Add(new ImGuiContextMenuEntry()
                {
                    Visible = !isPinned,
                    Label = _textService.Translate("ContextMenu.PinnedInstances.Pin"),
                    ClickCallback = () => _pinnedInstances.Add((nint)agent.Value, agentType)
                });

                builder.Add(new ImGuiContextMenuEntry()
                {
                    Visible = isPinned,
                    Label = _textService.Translate("ContextMenu.PinnedInstances.Unpin"),
                    ClickCallback = () => _pinnedInstances.Remove(agentType)
                });

                builder.Add(new ImGuiContextMenuEntry()
                {
                    Visible = agent.Value->IsActivatable() && !agent.Value->IsAddonShown(),
                    Label = _textService.Translate("ContextMenu.Agent.Show"),
                    ClickCallback = () => agent.Value->Show()
                });

                builder.Add(new ImGuiContextMenuEntry()
                {
                    Visible = agent.Value->IsActivatable() && agent.Value->IsAddonShown(),
                    Label = _textService.Translate("ContextMenu.Agent.Hide"),
                    ClickCallback = () => agent.Value->Hide()
                });
            });

            ImGui.TableNextColumn(); // Active
            using (Color.Green.Push(ImGuiCol.Text, isActive))
                ImGui.Text(isActive.ToString());
        }
    }

    private (string, bool) GetAgentName(AgentId agentId)
    {
        var name = Enum.GetName(agentId);
        if (!string.IsNullOrEmpty(name) && !name.StartsWith("Unk"))
            return (name, false);

        if (TryGetAddon<AtkUnitBase>(agentId, out var addon) && !string.IsNullOrEmpty(addon->NameString))
            return (addon->NameString, true);

        return (string.Empty, false);
    }

    private void DrawAgent(AgentId agentId)
    {
        using var hostchild = ImRaii.Child("AgentChild", new Vector2(-1), true, ImGuiWindowFlags.NoSavedSettings);

        var agent = AgentModule.Instance()->GetAgentByInternalId(agentId);
        var agentType = _agents!.TryGetValue(agentId, out var value) ? value.Type : typeof(AgentInterface);

        _debugRenderer.DrawPointerType(agent, agentType, new NodeOptions()
        {
            DefaultOpen = true,
            DrawContextMenu = (nodeOptions, builder) =>
            {
                if (agentType.Name == "AgentInterface") return;

                var isPinned = _pinnedInstances.Contains(agentType);

                builder.Add(new ImGuiContextMenuEntry()
                {
                    Visible = !_windowManager.Contains(win => win.WindowName == agentType.Name),
                    Label = _textService.Translate("ContextMenu.TabPopout"),
                    ClickCallback = () => _windowManager.Open(ActivatorUtilities.CreateInstance<PointerTypeWindow>(_serviceProvider, (nint)agent, agentType, string.Empty))
                });

                builder.Add(new ImGuiContextMenuEntry()
                {
                    Visible = !isPinned,
                    Label = _textService.Translate("ContextMenu.PinnedInstances.Pin"),
                    ClickCallback = () => _pinnedInstances.Add((nint)agent, agentType)
                });

                builder.Add(new ImGuiContextMenuEntry()
                {
                    Visible = isPinned,
                    Label = _textService.Translate("ContextMenu.PinnedInstances.Unpin"),
                    ClickCallback = () => _pinnedInstances.Remove(agentType)
                });
            }
        });
    }
}
