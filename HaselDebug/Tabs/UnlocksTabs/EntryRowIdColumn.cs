using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Extensions;
using HaselDebug.Utils;

namespace HaselDebug.Tabs.UnlocksTabs;

[AutoConstruct]
public partial class EntryRowIdColumn<T, TRow> : ColumnNumber<T>
    where T : IUnlockEntry
    where TRow : struct, IExcelRow<TRow>
{

    private readonly IServiceProvider _serviceProvider;
    private readonly LanguageProvider _languageProvider;
    private readonly Type _rowType;

    public override int ToValue(T entry)
        => (int)entry.RowId;

    public override void DrawColumn(T entry)
    {
        if (ImGui.Selectable(ToName(entry)))
        {
            new ExcelRowIdentifier(_rowType, entry.RowId, _languageProvider.ClientLanguage)
                .OpenWindow(_serviceProvider);
        }

        ImGuiContextMenu.Draw($"{_rowType.Name}{entry.RowId}RowIdContextMenu", builder =>
        {
            builder.AddCopyRowId(entry.RowId);
        });
    }

    public static EntryRowIdColumn<T, TRow> Create(IServiceProvider serviceProvider)
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
