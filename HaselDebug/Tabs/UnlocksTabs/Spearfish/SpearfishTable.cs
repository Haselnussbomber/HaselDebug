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

namespace HaselDebug.Tabs.UnlocksTabs.Spearfish;

[RegisterSingleton]
public unsafe class SpearfishTable : Table<SpearfishingItem>
{
    internal readonly ExcelService _excelService;

    public SpearfishTable(
        ExcelService excelService,
        DebugRenderer debugRenderer,
        TextService textService,
        UnlocksTabUtils unlocksTabUtils,
        ImGuiContextMenuService imGuiContextMenuService,
        LanguageProvider languageProvider) : base("SpearfishTable", languageProvider)
    {
        _excelService = excelService;

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

    public override void LoadRows()
    {
        Rows = _excelService.GetSheet<SpearfishingItem>()
            .Where(row => row.Item.RowId != 0)
            .ToList();
    }

    private class RowIdColumn : ColumnNumber<SpearfishingItem>
    {
        public override string ToName(SpearfishingItem row)
            => row.RowId.ToString();

        public override int ToValue(SpearfishingItem row)
            => (int)row.RowId;
    }

    private class CaughtColumn : ColumnBool<SpearfishingItem>
    {
        public override unsafe bool ToBool(SpearfishingItem row)
            => row.IsVisible && PlayerState.Instance()->IsSpearfishCaught(row.RowId);

        public override void DrawColumn(SpearfishingItem row)
        {
            if (row.IsVisible)
                base.DrawColumn(row);
        }
    }

    private class NameColumn(
        DebugRenderer debugRenderer,
        TextService textService,
        UnlocksTabUtils unlocksTabUtils,
        ImGuiContextMenuService imGuiContextMenuService) : ColumnString<SpearfishingItem>
    {
        public override string ToName(SpearfishingItem row)
            => textService.GetItemName(row.Item.RowId);

        public override unsafe void DrawColumn(SpearfishingItem row)
        {
            var item = row.Item.Value;

            debugRenderer.DrawIcon(item.Icon);

            if (ImGui.Selectable(ToName(row)) && AgentLobby.Instance()->IsLoggedIn)
                AgentFishGuide.Instance()->OpenForItemId(row.Item.RowId, true);

            if (ImGui.IsItemHovered())
                unlocksTabUtils.DrawItemTooltip(item);

            imGuiContextMenuService.Draw($"###SpearfishItem_{row.RowId}_ItemContextMenu", builder =>
            {
                builder.AddItemFinder(item.RowId);
                builder.AddCopyItemName(item.RowId);
                builder.AddItemSearch(item.AsRef());
                builder.AddOpenOnGarlandTools("item", item.RowId);
            });
        }
    }
}
