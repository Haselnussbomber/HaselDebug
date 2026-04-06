using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Extensions;

namespace HaselDebug.Tabs.UnlocksTabs.UnlockLinks.Columns;

[RegisterTransient, AutoConstruct]
public partial class UnlocksColumn : ColumnString<UnlockLinkEntry>
{
    private readonly IServiceProvider _serviceProvider;

    [AutoPostConstruct]
    public void Initialize()
    {
        SetFixedWidth(320);
    }

    public override string ToName(UnlockLinkEntry entry)
        => string.Join(' ', entry.Unlocks.Select(unlock => unlock.ExcelRowIdentifier?.ToString() ?? string.Empty));

    public override void DrawColumn(UnlockLinkEntry entry)
    {
        foreach (var unlock in entry.Unlocks)
        {
            if (unlock.ExcelRowIdentifier == null)
            {
                ImGui.Text("");
                continue;
            }

            if (ImGui.Selectable($"{unlock.ExcelRowIdentifier}{unlock.ExtraSheetText}"))
            {
                unlock.ExcelRowIdentifier.OpenWindow(_serviceProvider);
            }

            ImGuiContextMenu.Draw($"Entry{entry.Index}_{unlock.ExcelRowIdentifier.GetKey()}_RowIdContextMenu", builder =>
            {
                builder.AddCopyRowId(unlock.ExcelRowIdentifier.RowId);
            });
        }
    }
}
