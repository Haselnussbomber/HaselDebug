using System.Linq;
using Dalamud.Plugin.Services;
using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Tabs.UnlocksTabs.UnlockLinks.Columns;

[RegisterTransient, AutoConstruct]
public partial class UnlocksNameColumn : ColumnString<UnlockLinkEntry>
{
    private readonly DebugRenderer _debugRenderer;
    private readonly ITextureProvider _textureProvider;
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

                    if (unlock.IconId != 0)
                    {
                        _debugRenderer.DrawIcon(unlock.IconId, noTooltip: true);
                    }
                    else if (!string.IsNullOrEmpty(unlock.TexturePath))
                    {
                        _debugRenderer.DrawTexture(unlock.TexturePath, drawInfo: unlock.DrawInfo, noTooltip: true);
                    }

                    ImGuiUtilsEx.DrawCopyableText(unlock.Label, noTooltip: true);

                    ImGui.EndGroup();

                    if (ImGui.IsItemHovered())
                    {
                        if (unlock.IconId != 0)
                        {
                            _unlocksTabUtils.DrawTooltip(
                                unlock.IconId,
                                unlock.Label,
                                !string.IsNullOrEmpty(unlock.Category)
                                    ? unlock.Category
                                    : unlock.RowType.Name);
                        }
                        else if (!string.IsNullOrEmpty(unlock.TexturePath))
                        {
                            _unlocksTabUtils.DrawTooltip(
                                unlock.TexturePath,
                                unlock.DrawInfo,
                                unlock.Label,
                                !string.IsNullOrEmpty(unlock.Category)
                                    ? unlock.Category
                                    : unlock.RowType.Name);
                        }
                    }

                    break;
            }
        }
    }
}
