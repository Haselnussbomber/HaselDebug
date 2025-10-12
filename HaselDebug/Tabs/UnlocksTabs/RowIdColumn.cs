using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Extensions;
using HaselDebug.Windows;
using Lumina.Excel;
using Microsoft.Extensions.DependencyInjection;

namespace HaselDebug.Tabs.UnlocksTabs;

[AutoConstruct]
public partial class RowIdColumn<TRow> : ColumnNumber<TRow> where TRow : struct, IExcelRow<TRow>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly WindowManager _windowManager;
    private readonly TextService _textService;
    private readonly LanguageProvider _languageProvider;
    private readonly ImGuiContextMenuService _imGuiContextMenu;
    private readonly Type _rowType;

    public override int ToValue(TRow row)
        => (int)row.RowId;

    public override void DrawColumn(TRow row)
    {
        if (ImGui.Selectable(ToName(row)))
        {
            var title = $"{_rowType.Name}#{row.RowId} ({_languageProvider.ClientLanguage})";
            _windowManager.CreateOrOpen(title, () => ActivatorUtilities.CreateInstance<ExcelRowTab>(_serviceProvider, _rowType, row.RowId, _languageProvider.ClientLanguage, title));
        }

        _imGuiContextMenu.Draw($"{_rowType.Name}{row.RowId}RowIdContextMenu", builder =>
        {
            builder.AddCopyRowId(_textService, row.RowId);
        });
    }

    public static RowIdColumn<TRow> Create(IServiceProvider serviceProvider)
    {
        return new(
            serviceProvider.GetRequiredService<IServiceProvider>(),
            serviceProvider.GetRequiredService<WindowManager>(),
            serviceProvider.GetRequiredService<TextService>(),
            serviceProvider.GetRequiredService<LanguageProvider>(),
            serviceProvider.GetRequiredService<ImGuiContextMenuService>(),
            typeof(TRow)
        )
        {
            LabelKey = "RowIdColumn.Label",
            Flags = ImGuiTableColumnFlags.WidthFixed,
            Width = 60
        };
    }
}
