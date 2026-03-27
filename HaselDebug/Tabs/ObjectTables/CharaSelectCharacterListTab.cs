using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;

namespace HaselDebug.Tabs.ObjectTables;

[RegisterSingleton<IObjectTableTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class CharaSelectCharacterListTab : DebugTab, IObjectTableTab
{
    private readonly DebugRenderer _debugRenderer;

    public override bool DrawInChild => false;

    public override void Draw()
    {
        var list = CharaSelectCharacterList.Instance();
        var clientObjectManager = ClientObjectManager.Instance();
        var agentLobby = AgentLobby.Instance();

        using var table = ImRaii.Table("CharaSelectCharacterTable"u8, 4, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable);
        if (!table) return;

        ImGui.TableSetupColumn("ClientObjectIndex"u8, ImGuiTableColumnFlags.WidthFixed, 30);
        ImGui.TableSetupColumn("ContentId"u8, ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthFixed, 180);
        ImGui.TableSetupColumn("GameObject"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        foreach (var item in list->CharacterMapping)
        {
            if (item.ClientObjectIndex == -1)
                continue;

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // ClientObjectIndex
            ImGui.Text(item.ClientObjectIndex.ToString());

            ImGui.TableNextColumn(); // ContentId
            ImGui.Text(item.ContentId.ToString("X"));

            ImGui.TableNextColumn(); // Name
            if (agentLobby->LobbyData.CharaSelectEntries.TryGetFirst(p => p.Value->ContentId == item.ContentId, out var entry))
                ImGui.Text(entry.Value->NameString);

            ImGui.TableNextColumn(); // GameObject
            var obj = clientObjectManager->GetObjectByIndex((ushort)item.ClientObjectIndex);
            if (obj != null)
                _debugRenderer.DrawPointerType(obj);
        }
    }
}
