using HaselDebug.Windows;

namespace HaselDebug.Utils;

public record ExcelRowIdentifier
{
    public Type SheetType { get; init; }
    public uint RowId { get; init; }
    public ushort? SubrowId { get; init; }
    public ClientLanguage? Language { get; init; }

    public bool IsSubrowSheet => SheetType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IExcelSubrow<>));

    public ExcelRowIdentifier(Type sheetType, uint rowId, ClientLanguage? language = null)
    {
        SheetType = sheetType;
        RowId = rowId;
        Language = language;
    }

    public ExcelRowIdentifier(Type sheetType, uint rowId, ushort? subrowId, ClientLanguage? language = null) : this(sheetType, rowId, language)
    {
        SubrowId = subrowId;
    }

    public void OpenWindow(IServiceProvider serviceProvider)
    {
        serviceProvider
            .GetRequiredService<WindowManager>()
            .CreateOrOpen(ToString(), () => ActivatorUtilities.CreateInstance<ExcelRowWindow>(serviceProvider, this, ToString()));
    }

    public override string ToString()
    {
        var label = $"{SheetType.Name}#{RowId}";

        if (SubrowId != null)
            label += $".{SubrowId}";

        return label;
    }

    public string GetKey()
    {
        var label = $"{SheetType.Name}_{RowId}";

        if (SubrowId != null)
            label += $".{SubrowId}";

        return label;
    }
}
