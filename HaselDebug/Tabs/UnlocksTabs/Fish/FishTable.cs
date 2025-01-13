using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Extensions.Sheets;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Fish;

[RegisterSingleton]
public unsafe class FishTable : Table<FishParameter>
{
    internal readonly ExcelService _excelService;
    private readonly TextService _textService;

    public FishTable(
        ExcelService excelService,
        DebugRenderer debugRenderer,
        TextService textService,
        UnlocksTabUtils unlocksTabUtils,
        ImGuiContextMenuService imGuiContextMenuService,
        LanguageProvider languageProvider) : base("FishTable", languageProvider)
    {
        _excelService = excelService;
        _textService = textService;

        Columns = [
            new RowIdColumn() {
                Label = "RowId",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 60,
            },
            new CaughtColumn() {
                Label = "Caught",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 75,
            },
            new NameColumn(debugRenderer, textService, unlocksTabUtils, imGuiContextMenuService) {
                Label = "Name",
            }
        ];
    }

    public bool HideSpoilers = true;

    public override void LoadRows()
    {
        Rows = _excelService.GetSheet<FishParameter>()
            .Where(row => row.RowId != 0 && !string.IsNullOrEmpty(_textService.GetItemName(row.Item.RowId)))
            .ToList();
    }

    private class RowIdColumn : ColumnNumber<FishParameter>
    {
        public override string ToName(FishParameter row)
            => row.RowId.ToString();

        public override int ToValue(FishParameter row)
            => (int)row.RowId;
    }

    private class CaughtColumn : ColumnBool<FishParameter>
    {
        public override unsafe bool ToBool(FishParameter row)
            => row.IsInLog && PlayerState.Instance()->IsFishCaught(row.RowId);

        public override unsafe void DrawColumn(FishParameter row)
        {
            if (row.IsInLog)
                base.DrawColumn(row);
        }
    }

    private class NameColumn(
        DebugRenderer debugRenderer,
        TextService textService,
        UnlocksTabUtils unlocksTabUtils,
        ImGuiContextMenuService imGuiContextMenuService) : ColumnString<FishParameter>
    {
        public override string ToName(FishParameter row)
            => textService.GetItemName(row.Item.RowId);

        public override unsafe void DrawColumn(FishParameter row)
        {
            var isItem = row.Item.TryGetValue(out Item item);
            var isEventItem = row.Item.TryGetValue(out EventItem eventItem);
            debugRenderer.DrawIcon(isItem ? item.Icon : isEventItem ? eventItem.Icon : 0u);

            if (ImGui.Selectable(ToName(row)))
                AgentFishGuide.Instance()->OpenForItemId(row.Item.RowId, false);

            if (ImGui.IsItemHovered())
                unlocksTabUtils.DrawItemTooltip(row.Item);

            imGuiContextMenuService.Draw($"###FishItem_{row.RowId}_ItemContextMenu", builder =>
            {
                if (isItem)
                {
                    builder.AddItemFinder(item.RowId);
                    builder.AddCopyItemName(item.RowId);
                    builder.AddItemSearch(item.AsRef());
                    builder.AddOpenOnGarlandTools("item", item.RowId);
                }
                else if (isEventItem)
                {
                    builder.AddCopyItemName(eventItem.RowId);
                }
            });
        }
    }
}
