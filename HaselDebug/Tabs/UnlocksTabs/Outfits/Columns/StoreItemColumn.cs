using Dalamud.Utility;
using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.Outfits.Columns;

[RegisterSingleton, AutoConstruct]
public partial class StoreItemColumn : Column<MirageStoreSetItem>
{
    private readonly ITextureProvider _textureProvider;
    private readonly TextService _textService;

    public OutfitsTable Table;

    [AutoPostConstruct]
    public void Initialize()
    {
        SetFixedWidth(80);
    }

    public override int Compare(MirageStoreSetItem lhs, MirageStoreSetItem rhs)
    {
        return Table.IsStoreSet(lhs).CompareTo(Table.IsStoreSet(rhs));
    }

    public override void DrawColumn(MirageStoreSetItem row)
    {
        if (Table.IsStoreSet(row))
        {
            _textureProvider.DrawIcon(61831, OutfitsTable.IconSize);

            var url = Table.GetStoreUrl(row.Items.First(item => item.RowId != 0 && item.IsValid).RowId);
            var hasUrl = !string.IsNullOrEmpty(url);

            if (ImGui.IsItemHovered())
            {
                using var tooltip = ImRaii.Tooltip();
                ImGui.Text(_textService.Translate("HaselDebug.Tabs.UnlocksTabs.Outfits.Columns.StoreItemColumn.Tooltip"));

                if (hasUrl)
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            if (hasUrl && ImGui.IsItemClicked())
            {
                Util.OpenLink(url);
            }
        }
    }
}
