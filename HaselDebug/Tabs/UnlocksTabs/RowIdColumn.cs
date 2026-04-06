using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Extensions;
using HaselDebug.Utils;

namespace HaselDebug.Tabs.UnlocksTabs;

[AutoConstruct]
public partial class RowIdColumn<TRow> : ColumnNumber<TRow> where TRow : struct, IExcelRow<TRow>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly LanguageProvider _languageProvider;
    private readonly Type _rowType;

    public override int ToValue(TRow row)
        => (int)row.RowId;

    public override void DrawColumn(TRow row)
    {
        if (ImGui.Selectable(ToName(row)))
        {
            new ExcelRowIdentifier(_rowType, row.RowId, _languageProvider.ClientLanguage)
                .OpenWindow(_serviceProvider);
        }

        ImGuiContextMenu.Draw($"{_rowType.Name}{row.RowId}RowIdContextMenu", builder =>
        {
            builder.AddCopyRowId(row.RowId);
        });
    }

    public static RowIdColumn<TRow> Create(IServiceProvider serviceProvider)
    {
        return new(
            serviceProvider.GetRequiredService<IServiceProvider>(),
            serviceProvider.GetRequiredService<LanguageProvider>(),
            typeof(TRow)
        )
        {
            LabelKey = "RowIdColumn.Label",
            Flags = ImGuiTableColumnFlags.WidthFixed,
            Width = 60
        };
    }
}
