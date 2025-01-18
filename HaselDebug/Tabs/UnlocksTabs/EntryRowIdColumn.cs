using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Extensions;
using HaselDebug.Services;
using HaselDebug.Windows;
using ImGuiNET;
using Lumina.Excel;

namespace HaselDebug.Tabs.UnlocksTabs;

public class EntryRowIdColumn<T, TRow>(
    WindowManager windowManager,
    DebugRenderer debugRenderer,
    ImGuiContextMenuService imGuiContextMenu,
    TextService textService,
    Type rowType) : ColumnNumber<T>
    where T : IUnlockEntry
    where TRow : struct, IExcelRow<TRow>
{
    public override int ToValue(T entry)
        => (int)entry.RowId;

    public override void DrawColumn(T entry)
    {
        if (ImGui.Selectable(ToName(entry)))
        {
            windowManager.CreateOrOpen($"{rowType.Name}#{entry.RowId}", () => new ExcelRowTab(windowManager, debugRenderer, rowType, entry.RowId, $"{rowType.Name}#{entry.RowId}"));
        }

        imGuiContextMenu.Draw($"{rowType.Name}{entry.RowId}RowIdContextMenu", builder =>
        {
            builder.AddCopyRowId(textService, entry.RowId);
        });
    }

    public static EntryRowIdColumn<T, TRow> Create()
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
