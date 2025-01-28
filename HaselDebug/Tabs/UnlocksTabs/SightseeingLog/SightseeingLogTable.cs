using System.Linq;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Services;
using HaselDebug.Utils;
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
        TextService textService,
        IClientState clientState,
        UnlocksTabUtils unlocksTabUtils,
        ITextureProvider textureProvider,
        LanguageProvider languageProvider) : base("SightseeingLogTable", languageProvider)
    {
        _excelService = excelService;

        Columns = [
            EntryRowIdColumn<AdventureEntry, Adventure>.Create(),
            new IndexColumn() {
                Label = "Index",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 50,
            },
            new CompletedColumn() {
                Label = "Completed",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 75,
            },
            new ZoneColumn(textService, clientState, mapService) {
                Label = "Zone",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 280,
            },
            new NameColumn(debugRenderer, mapService, unlocksTabUtils, textureProvider) {
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

    private class ZoneColumn(TextService textService, IClientState clientState, MapService mapService) : ColumnString<AdventureEntry>
    {
        public override string ToName(AdventureEntry entry)
            => textService.GetPlaceName(entry.Row.PlaceName.RowId);

        public override void DrawColumn(AdventureEntry entry)
        {
            base.DrawColumn(entry);

            var level = entry.Row.Level.Value;
            if (clientState.TerritoryType == level.Territory.RowId)
            {
                var distance = mapService.GetDistanceFromPlayer(level);
                if (distance is > 1f and < float.MaxValue)
                {
                    var direction = distance > 1 ? " " + mapService.GetCompassDirection(level) : string.Empty;
                    ImGui.SameLine(0, 0);
                    ImGui.TextUnformatted($" ({distance:0}y{direction})");
                }
            }
        }
    }

    private class NameColumn(DebugRenderer debugRenderer, MapService mapService, UnlocksTabUtils unlocksTabUtils, ITextureProvider textureProvider) : ColumnString<AdventureEntry>
    {
        public override string ToName(AdventureEntry entry)
            => entry.Row.Name.ExtractText();

        public override unsafe void DrawColumn(AdventureEntry entry)
        {
            debugRenderer.DrawIcon((uint)entry.Row.IconList);

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

            if (textureProvider.TryGetFromGameIcon(entry.Row.IconDiscovered, out var imageTex) && imageTex.TryGetWrap(out var image, out _))
            {
                // cool, image preloaded! now the tooltips don't flicker...
            }

            if (ImGui.IsItemHovered())
            {
                unlocksTabUtils.DrawAdventureTooltip(entry.Index, entry.Row);
            }
        }
    }
}
