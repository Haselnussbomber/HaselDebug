using System.Text;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Graphics;
using HaselCommon.Gui;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Sheets;
using ImGuiNET;

namespace HaselDebug.Tabs.UnlocksTabs.OutfitsTableColumns;

[RegisterSingleton]
public class ItemsColumn(
    TextService textService,
    TextureService textureService,
    ITextureProvider textureProvider,
    ImGuiContextMenuService imGuiContextMenuService,
    PrismBoxProvider prismBoxProvider) : ColumnString<CustomMirageStoreSetItem>
{
    private const float IconSize = OutfitsTable.IconSize;
    private readonly StringBuilder _stringBuilder = new();

    public override string ToName(CustomMirageStoreSetItem row)
    {
        _stringBuilder.Clear();

        for (var i = 1; i < row.Items.Count; i++)
            _stringBuilder.AppendLine(textService.GetItemName(row.Items[i].RowId));

        return _stringBuilder.ToString();
    }

    public override void DrawColumn(CustomMirageStoreSetItem row)
    {
        var isSetCollected = prismBoxProvider.ItemIds.Contains(row.RowId);

        for (var i = 1; i < row.Items.Count; i++)
        {
            var item = row.Items[i];
            if (item.RowId == 0)
                continue;

            var isItemCollected = prismBoxProvider.ItemIds.Contains(item.RowId) || prismBoxProvider.ItemIds.Contains(item.RowId + 1_000_000);

            ImGui.Dummy(ImGuiHelpers.ScaledVector2(IconSize));
            ImGui.SameLine(0, 0);
            ImGuiUtils.PushCursorX(-IconSize * ImGuiHelpers.GlobalScale);
            textureService.DrawIcon(
                item.Value.Icon,
                false,
                new(IconSize * ImGuiHelpers.GlobalScale)
                {
                    TintColor = isSetCollected || isItemCollected
                        ? Color.White
                        : ImGui.IsItemHovered() || ImGui.IsPopupOpen($"###SetItem_{row.RowId}_{item.RowId}_ItemContextMenu")
                            ? Color.White : Color.Grey3
                }
            );

            if (ImGui.IsItemClicked())
                AgentTryon.TryOn(0, item.RowId);

            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                using var tooltip = ImRaii.Tooltip();
                if (textureProvider.TryGetFromGameIcon(new(item.Value.Icon), out var texture) && texture.TryGetWrap(out var textureWrap, out _))
                {
                    ImGui.Image(textureWrap.ImGuiHandle, textureWrap.Size);
                    ImGui.SameLine();
                    ImGuiUtils.PushCursorY(textureWrap.Height / 2f - ImGui.GetTextLineHeight() / 2f);
                }
                ImGui.TextUnformatted(textService.GetItemName(item.RowId));
            }

            imGuiContextMenuService.Draw($"###SetItem_{row.RowId}_{item.RowId}_ItemContextMenu", builder =>
            {
                builder.AddTryOn(item);
                builder.AddItemFinder(item.RowId);
                builder.AddCopyItemName(item.RowId);
                builder.AddItemSearch(item);
                builder.AddSearchCraftingMethod(item);
                builder.AddOpenOnGarlandTools("item", item.RowId);
            });

            if (isItemCollected)
                OutfitsTable.DrawCollectedCheckmark(textureProvider);

            ImGui.SameLine();
        }

        ImGui.NewLine();
    }
}
