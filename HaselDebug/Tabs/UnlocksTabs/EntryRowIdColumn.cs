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
    TextService textService,
    LanguageProvider languageProvider,
    DebugRenderer debugRenderer,
    ImGuiContextMenuService imGuiContextMenu,
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
            windowManager.CreateOrOpen($"{rowType.Name}#{entry.RowId}", () => new ExcelRowTab(windowManager, textService, languageProvider, debugRenderer, rowType, entry.RowId, $"{rowType.Name}#{entry.RowId}"));
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
            Service.Get<TextService>(),
            Service.Get<LanguageProvider>(),
            Service.Get<DebugRenderer>(),
            Service.Get<ImGuiContextMenuService>(),
            typeof(TRow)
        )
        {
            Label = "RowId",
            Flags = ImGuiTableColumnFlags.WidthFixed,
            Width = 60
        };
    }
}
