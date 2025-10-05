using Dalamud.Game;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class CurrencyManagerTab : DebugTab
{
    private readonly DebugRenderer _debugRenderer;
    private readonly TextService _textService;
    private readonly ItemService _itemService;

    public override string Title => "CurrencyManager";

    public override void Draw()
    {
        var currencyManager = CurrencyManager.Instance();
        _debugRenderer.DrawPointerType(currencyManager, typeof(CurrencyManager), new NodeOptions());

        using (var node = ImRaii.TreeNode(nameof(CurrencyManager.SpecialItemBucket), ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            if (node)
            {
                using var table = ImRaii.Table(nameof(CurrencyManager.SpecialItemBucket) + "Table", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg);
                if (table)
                {
                    ImGui.TableSetupColumn("ItemId"u8, ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("SpecialId"u8, ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("Count"u8, ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("Remaining"u8, ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableHeadersRow();

                    foreach (var (itemId, item) in currencyManager->SpecialItemBucket)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGuiUtilsEx.DrawCopyableText(itemId.ToString());
                        ImGui.TableNextColumn();
                        ImGui.Text(item.SpecialId.ToString());
                        ImGui.TableNextColumn();
                        ImGui.Text($"{item.Count} / {item.MaxCount}");
                        ImGui.TableNextColumn();
                        if (currencyManager->IsItemLimited(itemId))
                            ImGui.Text(currencyManager->GetItemCountRemaining(itemId).ToString());
                        ImGui.TableNextColumn();
                        _debugRenderer.DrawIcon(_itemService.GetIconId(itemId));
                        ImGuiUtilsEx.DrawCopyableText(_textService.GetItemName(itemId, ImGui.IsKeyDown(ImGuiKey.LeftShift) ? ClientLanguage.English : null).ToString());
                    }
                }
            }
        }

        using (var node = ImRaii.TreeNode(nameof(CurrencyManager.ItemBucket), ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            if (node)
            {
                using var table = ImRaii.Table(nameof(CurrencyManager.ItemBucket) + "Table", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg);
                if (table)
                {
                    ImGui.TableSetupColumn("ItemId"u8, ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("Count"u8, ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("Remaining"u8, ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("IsUnlimited"u8, ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableHeadersRow();

                    foreach (var (itemId, item) in currencyManager->ItemBucket)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGuiUtilsEx.DrawCopyableText(itemId.ToString());
                        ImGui.TableNextColumn();
                        ImGui.Text($"{item.Count} / {item.MaxCount}");
                        ImGui.TableNextColumn();
                        if (currencyManager->IsItemLimited(itemId))
                            ImGui.Text(currencyManager->GetItemCountRemaining(itemId).ToString());
                        ImGui.TableNextColumn();
                        ImGui.Text(item.IsUnlimited.ToString());
                        ImGui.TableNextColumn();
                        _debugRenderer.DrawIcon(_itemService.GetIconId(itemId));
                        ImGuiUtilsEx.DrawCopyableText(_textService.GetItemName(itemId, ImGui.IsKeyDown(ImGuiKey.LeftShift) ? ClientLanguage.English : null).ToString());
                    }
                }
            }
        }

        using (var node = ImRaii.TreeNode(nameof(CurrencyManager.ContentItemBucket), ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            if (node)
            {
                using var table = ImRaii.Table(nameof(CurrencyManager.ContentItemBucket) + "Table", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg);
                if (table)
                {
                    ImGui.TableSetupColumn("ItemId"u8, ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("Count"u8, ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("Remaining"u8, ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("IsUnlimited"u8, ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableHeadersRow();

                    foreach (var (itemId, item) in currencyManager->ContentItemBucket)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGuiUtilsEx.DrawCopyableText(itemId.ToString());
                        ImGui.TableNextColumn();
                        ImGui.Text($"{item.Count} / {item.MaxCount}");
                        ImGui.TableNextColumn();
                        if (currencyManager->IsItemLimited(itemId))
                            ImGui.Text(currencyManager->GetItemCountRemaining(itemId).ToString());
                        ImGui.TableNextColumn();
                        ImGui.Text(item.IsUnlimited.ToString());
                        ImGui.TableNextColumn();
                        _debugRenderer.DrawIcon(_itemService.GetIconId(itemId));
                        ImGuiUtilsEx.DrawCopyableText(_textService.GetItemName(itemId, ImGui.IsKeyDown(ImGuiKey.LeftShift) ? ClientLanguage.English : null).ToString());
                    }
                }
            }
        }
    }
}
