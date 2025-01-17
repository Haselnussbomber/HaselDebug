using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Services;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.SightseeingLog;

[RegisterSingleton]
public unsafe class SightseeingLogTable : Table<AdventureEntry>
{
    internal readonly ExcelService _excelService;

    public SightseeingLogTable(
        ExcelService excelService,
        DebugRenderer debugRenderer,
        MapService mapService,
        LanguageProvider languageProvider) : base("SightseeingLogTable", languageProvider)
    {
        _excelService = excelService;

        Columns = [
            EntryRowIdColumn<AdventureEntry, Adventure>.Create(),
            new IndexColumn() {
                Label = "Index",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 60,
            },
            new CompletedColumn() {
                Label = "Completed",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 75,
            },
            new NameColumn(debugRenderer, mapService) {
                Label = "Name",
            }
        ];
    }

    public override void LoadRows()
    {
        Rows = _excelService.GetSheet<Adventure>()
            .Select((row, index) => new AdventureEntry(index, row))
            .ToList();
    }

    private class IndexColumn : ColumnNumber<AdventureEntry>
    {
        public override string ToName(AdventureEntry entry)
            => entry.Index.ToString();

        public override int ToValue(AdventureEntry entry)
            => entry.Index;
    }

    private class CompletedColumn : ColumnBool<AdventureEntry>
    {
        public override unsafe bool ToBool(AdventureEntry entry)
            => PlayerState.Instance()->IsAdventureComplete((uint)entry.Index);
    }

    private class NameColumn(DebugRenderer debugRenderer, MapService mapService) : ColumnString<AdventureEntry>
    {
        public override string ToName(AdventureEntry entry)
            => entry.Row.Name.ExtractText();

        public override unsafe void DrawColumn(AdventureEntry entry)
        {
            debugRenderer.DrawIcon((uint)entry.Row.IconList);

            var isLoggedIn = AgentLobby.Instance()->IsLoggedIn;
            if (AgentLobby.Instance()->IsLoggedIn)
            {
                var clicked = ImGui.Selectable(ToName(entry));

                if (ImGui.IsItemHovered())
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                if (clicked)
                    mapService.OpenMap(entry.Row.Level.Value);
            }
            else
            {
                ImGui.TextUnformatted(ToName(entry));
            }
        }
    }
}
