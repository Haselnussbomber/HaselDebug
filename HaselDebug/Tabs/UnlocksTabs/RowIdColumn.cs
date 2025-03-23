using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Extensions;
using HaselDebug.Services;
using HaselDebug.Windows;
using ImGuiNET;
using Lumina.Excel;

namespace HaselDebug.Tabs.UnlocksTabs;

[AutoConstruct]
public partial class RowIdColumn<TRow> : ColumnNumber<TRow> where TRow : struct, IExcelRow<TRow>
{
    private readonly WindowManager _windowManager;
    private readonly TextService _textService;
    private readonly LanguageProvider _languageProvider;
    private readonly DebugRenderer _debugRenderer;
    private readonly ImGuiContextMenuService _imGuiContextMenu;
    private readonly Type _rowType;

    public override int ToValue(TRow row)
        => (int)row.RowId;

    public override void DrawColumn(TRow row)
    {
        if (ImGui.Selectable(ToName(row)))
        {
            var title = $"{_rowType.Name}#{row.RowId} ({_languageProvider.ClientLanguage})";
            _windowManager.CreateOrOpen(title, () => new ExcelRowTab(_windowManager, _textService, _languageProvider, _debugRenderer, _rowType, row.RowId, _languageProvider.ClientLanguage, title));
        }

        _imGuiContextMenu.Draw($"{_rowType.Name}{row.RowId}RowIdContextMenu", builder =>
        {
            builder.AddCopyRowId(_textService, row.RowId);
        });
    }

    public static RowIdColumn<TRow> Create()
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
