using System.Reflection;
using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.Excel;

[AutoConstruct]
public partial class ExcelTable<T> : Table<T> where T : struct
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ExcelTab _excelTab;
    private readonly ExcelService _excelService;

    public List<ExcelSheetColumn<T>> AvailableColumns { get; private set; } = [];
    public bool IsSubrowType { get; private set; }

    [AutoPostConstruct]
    public void Initialize()
    {
        Flags |= ImGuiTableFlags.Borders;
        Flags |= ImGuiTableFlags.ScrollX;
        Flags |= ImGuiTableFlags.Resizable;
        Flags |= ImGuiTableFlags.Reorderable;
        Flags |= ImGuiTableFlags.NoSavedSettings;
        Flags &= ~ImGuiTableFlags.NoBordersInBodyUntilResize;

        IsSubrowType = typeof(T).IsAssignableTo(typeof(IExcelSubrow<T>));

        if (IsSubrowType) ScrollFreezeCols = 2;

        foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.Name is "ExcelPage" or "RowOffset")
                continue;

            var column = ActivatorUtilities.CreateInstance<ExcelSheetColumn<T>>(_serviceProvider, _excelTab, this, property);

            AvailableColumns.Add(column);

            if (Columns.Count < ExcelTab.MaxColumns)
            {
                Columns.Add(column);
            }
        }
    }

    public override void LoadRows()
    {
        if (IsSubrowType)
        {
            // Rows = [.. _excelService.GetSubrowSheet<T>(_excelTab.SelectedLanguage).SelectMany(row => row)];

            var getSheetMethodInfo = typeof(ExcelService)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(mi => mi.Name == "GetSubrowSheet" && mi.IsGenericMethod && mi.GetParameters().Length == 1)
                .First();
            var getSheetTyped = getSheetMethodInfo.MakeGenericMethod(typeof(T));
            var collection = (System.Collections.IEnumerable)getSheetTyped.Invoke(_excelService, [_excelTab.SelectedLanguage])!;
            Rows = [.. collection.Cast<IReadOnlyList<T>>().SelectMany(row => row)];
        }
        else
        {
            // Rows = [.. _excelService.GetSheet<T>(_excelTab.SelectedLanguage)];

            var getSheetMethodInfo = typeof(ExcelService)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(mi => mi.Name == "GetSheet" && mi.IsGenericMethod && mi.GetParameters().Length == 1)
                .First();
            var getSheetTyped = getSheetMethodInfo.MakeGenericMethod(typeof(T));
            var collection = (IReadOnlyCollection<T>)getSheetTyped.Invoke(_excelService, [_excelTab.SelectedLanguage])!;
            Rows = [.. collection];
        }
    }
}
