using System.Reflection;
using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Extensions;
using HaselDebug.Services;
using HaselDebug.Utils;
using HaselDebug.Windows;

namespace HaselDebug.Tabs.Excel;

[AutoConstruct]
public partial class ExcelSheetColumn<T> : ColumnString<T> where T : struct
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ExcelTab _excelTab;
    private readonly ExcelTable<T> _excelTable;
    private readonly DebugRenderer _debugRenderer;
    private readonly WindowManager _windowManager;
    private readonly PropertyInfo _propertyInfo;

    public Type RowType => typeof(T);
    public Type ColumnType => _propertyInfo.PropertyType;

    [AutoConstructIgnore]
    public string ColumnTypeName { get; private set; }

    [AutoConstructIgnore]
    public bool IsStringType { get; private set; }

    [AutoConstructIgnore]
    public bool IsNumericType { get; private set; }

    [AutoConstructIgnore]
    public bool IsStructType { get; private set; }

    [AutoConstructIgnore]
    public bool IsIconColumn { get; private set; }

    [AutoPostConstruct]
    private void Initialize()
    {
        AutoLabel = false;
        Label = _propertyInfo.Name;
        ColumnTypeName = ColumnType.ReadableTypeName();

        IsStringType = ColumnType == typeof(ReadOnlySeString);
        IsNumericType = ColumnType.IsNumericType();
        IsStructType = !IsStringType && !ColumnType.IsNumericType() && ColumnType.IsStruct();
        IsIconColumn = Label.Contains("Icon") || Label.Contains("Image");

        Flags = ImGuiTableColumnFlags.WidthFixed;
        Width = true switch
        {
            _ when Label is "RowId" or "SubrowId" => 75,
            _ when IsNumericType => 100,
            _ when IsStringType || IsStructType => 200,
            _ => 100
        };
    }

    public override string ToName(T row)
    {
        var value = _propertyInfo.GetValue(row);
        if (value == null)
            return string.Empty;

        if (ColumnType == typeof(ReadOnlySeString))
            return ((ReadOnlySeString)value).ToString("m");

        if (ColumnType == typeof(RowRef))
            return ((uint)ColumnType.GetProperty("RowId")?.GetValue(value)!).ToString();

        if (ColumnType.IsGenericType && ColumnType.GetGenericTypeDefinition() == typeof(RowRef<>))
        {
            var rowRefType = ColumnType.GenericTypeArguments[0];
            var rowRefRowId = (uint)ColumnType.GetProperty("RowId")?.GetValue(value)!;
            return $"{rowRefType.Name}#{rowRefRowId}";
        }

        if (ColumnType.IsGenericType && ColumnType.GetGenericTypeDefinition() == typeof(SubrowRef<>))
        {
            var rowRefType = ColumnType.GenericTypeArguments[0];
            var rowRefRowId = (uint)ColumnType.GetProperty("RowId")?.GetValue(value)!;
            return $"{rowRefType.Name}#{rowRefRowId}";
        }

        return value.ToString() ?? string.Empty;
    }

    public int ToValue(T row)
    {
        return Convert.ToInt32(_propertyInfo.GetValue(row));
    }

    public override int Compare(T lhs, T rhs)
    {
        if (IsStringType)
            return string.Compare(ToName(lhs), ToName(rhs), StringComparison.InvariantCulture);

        if (IsNumericType)
            return ToValue(lhs).CompareTo(ToValue(rhs));

        return 0;
    }

    public override void DrawColumn(T row)
    {
        var value = _propertyInfo.GetValue(row);
        var rowId = (uint)RowType.GetProperty("RowId", BindingFlags.Public | BindingFlags.Instance)!.GetValue(row)!;

        if (value == null)
        {
            ImGui.Text("null"u8);
            return;
        }

        if (!_excelTable.IsSubrowType && Label is "RowId" or "SubrowId")
        {
            if (ImGui.Selectable(value.ToString()))
            {
                OpenSheet(RowType.Name, rowId);
            }
            ImGuiContextMenu.Draw($"{RowType.Name}{rowId}RowIdContextMenu", builder =>
            {
                builder.AddCopyRowId(rowId);
            });
            return;
        }

        if (ColumnType == typeof(ReadOnlySeString))
        {
            _debugRenderer.DrawSeString(((ReadOnlySeString)value).AsSpan(), new NodeOptions()
            {
                RenderSeString = false,
                Title = $"{RowType.Name}#{rowId} ({_excelTab.SelectedLanguage})",
                Language = _excelTab.SelectedLanguage,
                // AddressPath = nodeOptions.AddressPath.With(propName.GetHashCode()),
            });
            return;
        }

        if (ColumnType == typeof(RowRef))
        {
            var columnRowId = (uint)ColumnType.GetProperty("RowId")?.GetValue(value)!;
            ImGui.Text(columnRowId.ToString());
            return;
        }

        if (ColumnType.IsGenericType && ColumnType.GetGenericTypeDefinition() == typeof(RowRef<>))
        {
            var rowRefType = ColumnType.GenericTypeArguments[0];
            var rowRefRowId = (uint)ColumnType.GetProperty("RowId")?.GetValue(value)!;
            var rowRefIsValid = (bool)ColumnType.GetProperty("IsValid")?.GetValue(value)!;
            var text = $"{rowRefType.Name}#{rowRefRowId}";

            if (rowRefIsValid)
            {
                using var color = DebugRenderer.ColorTreeNode.Push(ImGuiCol.Text);

                if (ImGui.Selectable(text))
                    OpenSheet(rowRefType.Name, rowRefRowId);
            }
            else
            {
                using var disabled = ImRaii.Disabled();
                ImGui.Text(text);
            }

            return;
        }

        if (ColumnType.IsGenericType && ColumnType.GetGenericTypeDefinition() == typeof(SubrowRef<>))
        {
            var rowRefType = ColumnType.GenericTypeArguments[0];
            var rowRefRowId = (uint)ColumnType.GetProperty("RowId")?.GetValue(value)!;
            var rowRefIsValid = (bool)ColumnType.GetProperty("IsValid")?.GetValue(value)!;
            var text = $"{rowRefType.Name}#{rowRefRowId}";

            if (rowRefIsValid)
            {
                using var color = DebugRenderer.ColorTreeNode.Push(ImGuiCol.Text);

                if (ImGui.Selectable(text))
                    OpenSheet(rowRefType.Name, rowRefRowId);
            }
            else
            {
                using var disabled = ImRaii.Disabled();
                ImGui.Text(text);
            }

            return;
        }

        if (ColumnType.IsGenericType && ColumnType.GetGenericTypeDefinition() == typeof(Collection<>))
        {
            var count = (int)ColumnType.GetProperty("Count")?.GetValue(value)!;
            using (Color.Grey.Push(ImGuiCol.Text))
                ImGui.Text($"{count} value{(count != 1 ? "s" : "")}"); // TODO: click to open
            return;
        }

        if (IsNumericType)
        {
            _debugRenderer.DrawNumeric(value, ColumnType, new NodeOptions() { IsIconIdField = IsIconColumn, HexOnShift = true });
            return;
        }

        ImGui.Text(value.ToString()); // TODO: invariant culture
    }

    private void OpenSheet(string sheetName, uint rowId)
    {
        if (!_excelTab.TryGetSheetType(sheetName, out var sheetType))
            return;

        var title = $"{sheetName}#{rowId} ({_excelTab.SelectedLanguage})";
        _windowManager.CreateOrOpen(title, () => ActivatorUtilities.CreateInstance<ExcelRowTab>(_serviceProvider, sheetType, rowId, _excelTab.SelectedLanguage, title));
    }
}
