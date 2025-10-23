using System.Globalization;
using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.Recipes.Columns;

[RegisterTransient, AutoConstruct]
public partial class PatchColumn : ColumnNumber<Recipe>
{
    [AutoPostConstruct]
    private void Initialize()
    {
        LabelKey = "PatchColumn.Label";
        SetFixedWidth(50);
    }

    public override int ToValue(Recipe row)
    {
        return row.PatchNumber;
    }

    public override string ToName(Recipe row)
    {
        if (row.PatchNumber == 0)
            return string.Empty;

        return (row.PatchNumber / 100.0).ToString("F2", CultureInfo.InvariantCulture);
    }
}
