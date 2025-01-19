using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Extensions;
using HaselDebug.Services;
using HaselDebug.Windows;
using ImGuiNET;
using Lumina.Excel;

namespace HaselDebug.Tabs.UnlocksTabs;

public class RowIdColumn<TRow>(
    WindowManager windowManager,
    DebugRenderer debugRenderer,
    ImGuiContextMenuService imGuiContextMenu,
    TextService textService,
    Type rowType) : ColumnNumber<TRow> where TRow : struct, IExcelRow<TRow>
{
    public override int ToValue(TRow row)
        => (int)row.RowId;

    public override void DrawColumn(TRow row)
    {
        if (ImGui.Selectable(ToName(row)))
        {
            windowManager.CreateOrOpen($"{rowType.Name}#{row.RowId}", () => new ExcelRowTab(windowManager, debugRenderer, rowType, row.RowId, $"{rowType.Name}#{row.RowId}"));
        }

        imGuiContextMenu.Draw($"{rowType.Name}{row.RowId}RowIdContextMenu", builder =>
        {
            builder.AddCopyRowId(textService, row.RowId);
        });
    }

    public static RowIdColumn<TRow> Create()
    {
        return new(
            Service.Get<WindowManager>(),
            Service.Get<DebugRenderer>(),
            Service.Get<ImGuiContextMenuService>(),
            Service.Get<TextService>(),
            typeof(TRow)
        )
        {
            Label = "RowId",
            Flags = ImGuiTableColumnFlags.WidthFixed,
            Width = 60
        };
    }
}