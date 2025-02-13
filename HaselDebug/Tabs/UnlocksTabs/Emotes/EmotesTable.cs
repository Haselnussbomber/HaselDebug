using System.Linq;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Services;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Emotes;

[RegisterSingleton]
public unsafe class EmotesTable : Table<Emote>
{
    internal readonly ExcelService _excelService;

    public EmotesTable(
        ExcelService excelService,
        DebugRenderer debugRenderer,
        LanguageProvider languageProvider) : base(languageProvider)
    {
        _excelService = excelService;

        Columns = [
            RowIdColumn<Emote>.Create(),
            new CanUseColumn() {
                Label = "Can Use",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 75,
            },
            new ItemColumn(debugRenderer) {
                Label = "Name",
            }
        ];
    }

    public override void LoadRows()
    {
        Rows = _excelService.GetSheet<Emote>()
            .Where(row => row.RowId != 0 && !row.Name.IsEmpty)
            .ToList();
    }

    private class CanUseColumn : ColumnBool<Emote>
    {
        public override unsafe bool ToBool(Emote row)
            => AgentEmote.Instance()->CanUseEmote((ushort)row.RowId);
    }

    private class ItemColumn(DebugRenderer debugRenderer) : ColumnString<Emote>
    {
        public override string ToName(Emote row)
            => row.Name.ExtractText();

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
                ImGui.TextUnformatted(ToName(row));
            }
        }
    }
}
