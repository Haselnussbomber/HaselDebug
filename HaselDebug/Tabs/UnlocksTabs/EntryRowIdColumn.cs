using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Extensions;
using HaselDebug.Windows;
using ImGuiNET;
using Lumina.Excel;

namespace HaselDebug.Tabs.UnlocksTabs;

[AutoConstruct]
public partial class EntryRowIdColumn<T, TRow> : ColumnNumber<T>
    where T : IUnlockEntry
    where TRow : struct, IExcelRow<TRow>
{

    private readonly IServiceProvider _serviceProvider;
    private readonly WindowManager _windowManager;
    private readonly TextService _textService;
    private readonly LanguageProvider _languageProvider;
    private readonly ImGuiContextMenuService _imGuiContextMenu;
    private readonly Type _rowType;

    public override int ToValue(T entry)
        => (int)entry.RowId;

    public override void DrawColumn(T entry)
    {
        if (ImGui.Selectable(ToName(entry)))
        {
            var title = $"{_rowType.Name}#{entry.RowId} ({_languageProvider.ClientLanguage})";
            _windowManager.CreateOrOpen(title, () => new ExcelRowTab(_serviceProvider, _rowType, entry.RowId, _languageProvider.ClientLanguage, title));
        }

        _imGuiContextMenu.Draw($"{_rowType.Name}{entry.RowId}RowIdContextMenu", builder =>
        {
            builder.AddCopyRowId(_textService, entry.RowId);
        });
    }

    public static EntryRowIdColumn<T, TRow> Create()
    {
        return new(
            Service.Get<IServiceProvider>(),
            Service.Get<WindowManager>(),
            Service.Get<TextService>(),
            Service.Get<LanguageProvider>(),
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
