using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Services;

namespace HaselDebug.Tabs.UnlocksTabs.Emotes.Columns;

[RegisterTransient]
public class ItemColumn(DebugRenderer debugRenderer) : ColumnString<Emote>
{
    public override string ToName(Emote row)
        => row.Name.ToString();

    public override unsafe void DrawColumn(Emote row)
    {
        debugRenderer.DrawIcon(row.Icon);

        if (AgentLobby.Instance()->IsLoggedIn)
        {
            using var disabled = ImRaii.Disabled(!AgentEmote.Instance()->CanUseEmote((ushort)row.RowId));
            var clicked = ImGui.Selectable(ToName(row));

            if (ImGui.IsItemHovered())
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

            if (clicked)
                AgentEmote.Instance()->ExecuteEmote((ushort)row.RowId, addToHistory: false);
        }
        else
        {
            ImGui.Text(ToName(row));
        }
    }
}
