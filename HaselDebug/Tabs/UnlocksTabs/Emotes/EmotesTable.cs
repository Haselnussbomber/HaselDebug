using System.Linq;
using Dalamud.Interface.Utility.Raii;
using System.Xml.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
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
        LanguageProvider languageProvider) : base("EmotesTable", languageProvider)
    {
        _excelService = excelService;

        Columns = [
            new RowIdColumn() {
                Label = "RowId",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 60,
            },
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

    public bool HideSpoilers = true;

    public override void LoadRows()
    {
        Rows = _excelService.GetSheet<Emote>()
            .Where(row => row.RowId != 0 && !row.Name.IsEmpty)
            .ToList();
    }

    private class RowIdColumn : ColumnNumber<Emote>
    {
        public override string ToName(Emote row)
            => row.RowId.ToString();

        public override int ToValue(Emote row)
            => (int)row.RowId;
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

            using var disabled = ImRaii.Disabled(!AgentEmote.Instance()->CanUseEmote((ushort)row.RowId));
            var clicked = ImGui.Selectable(ToName(row));

            if (ImGui.IsItemHovered())
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

            if (clicked)
                AgentEmote.Instance()->ExecuteEmote((ushort)row.RowId, addToHistory: false);
        }
    }
}
