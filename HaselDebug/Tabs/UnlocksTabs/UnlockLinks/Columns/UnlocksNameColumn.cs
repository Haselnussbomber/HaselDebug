using System.Linq;
using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;

namespace HaselDebug.Tabs.UnlocksTabs.UnlockLinks.Columns;

[RegisterTransient, AutoConstruct]
public partial class UnlocksNameColumn : ColumnString<UnlockLinkEntry>
{
    private readonly DebugRenderer _debugRenderer;
    private readonly UnlocksTabUtils _unlocksTabUtils;

    public override string ToName(UnlockLinkEntry entry)
        => string.Join(' ', entry.Unlocks.Select(unlock => unlock.Label));

    public override unsafe void DrawColumn(UnlockLinkEntry entry)
    {
        foreach (var unlock in entry.Unlocks)
        {
            switch (unlock.RowType.Name)
            {
                case "Item":
                    _unlocksTabUtils.DrawSelectableItem(unlock.RowId, $"Unlock{entry.Index}Item{unlock.RowId}");
                    break;

                default:
                    ImGui.BeginGroup();
                    _debugRenderer.DrawIcon(unlock.IconId, noTooltip: true);
                    ImGuiUtilsEx.DrawCopyableText(unlock.Label, noTooltip: true);
                    ImGui.EndGroup();

                    if (ImGui.IsItemHovered())
                    {
                        _unlocksTabUtils.DrawTooltip(
                            unlock.IconId,
                            unlock.Label,
                            !string.IsNullOrEmpty(unlock.Category)
                                ? unlock.Category
                                : unlock.RowType.Name);
                    }

                    break;
            }
        }
    }
}
