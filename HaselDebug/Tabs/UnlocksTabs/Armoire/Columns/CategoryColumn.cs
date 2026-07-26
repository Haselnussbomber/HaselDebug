using HaselCommon.Gui.ImGuiTable;
using CabinetSheet = Lumina.Excel.Sheets.Cabinet;

namespace HaselDebug.Tabs.UnlocksTabs.Armoire.Columns;

[RegisterSingleton, AutoConstruct]
public partial class CategoryColumn : ColumnString<CabinetSheet>
{
    private const float IconSize = ArmoireTable.IconSize;

    private readonly ITextureProvider _textureProvider;

    [AutoPostConstruct]
    public void Initialize()
    {
        SetStretchWidth(1);
        Flags |= ImGuiTableColumnFlags.DefaultSort;
    }

    public override string ToName(CabinetSheet row)
        => row.Category.Value.Category.Value.Text.ToString();

    public override void DrawColumn(CabinetSheet row)
    {
        ImGui.Dummy(ImGuiHelpers.ScaledVector2(IconSize));
        ImGui.SameLine(0, 0);
        ImCursor.X -= IconSize * ImStyle.Scale;
        _textureProvider.DrawIcon(
            (uint)row.Category.Value.Icon,
            new(IconSize * ImStyle.Scale)
        );

        ImGui.SameLine(IconSize * ImStyle.Scale + ImStyle.ItemSpacing.X, 0);
        ImCursor.Y += IconSize * ImStyle.Scale / 2f - ImStyle.TextLineHeight / 2f;
        ImGui.Text(ToName(row));
    }
}
