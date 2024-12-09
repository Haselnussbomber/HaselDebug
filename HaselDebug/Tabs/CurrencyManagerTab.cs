using Dalamud.Game;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;

namespace HaselDebug.Tabs;

public unsafe class CurrencyManagerTab(DebugRenderer DebugRenderer, TextService TextService) : DebugTab
{
    public override string Title => "CurrencyManager";

    public override void Draw()
    {
        var currencyManager = CurrencyManager.Instance();
        DebugRenderer.DrawPointerType(currencyManager, typeof(CurrencyManager), new NodeOptions());

        using (var node = ImRaii.TreeNode(nameof(CurrencyManager.SpecialItemBucket), ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            if (node)
            {
                using var table = ImRaii.Table(nameof(CurrencyManager.SpecialItemBucket) + "Table", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg);
                if (table)
                {
                    ImGui.TableSetupColumn("ItemId", ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("SpecialId", ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("Count", ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("Remaining", ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableHeadersRow();

                    foreach (var (itemId, item) in currencyManager->SpecialItemBucket)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        DebugRenderer.DrawCopyableText(itemId.ToString());
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(item.SpecialId.ToString());
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{item.Count} / {item.MaxCount}");
                        ImGui.TableNextColumn();
                        if (currencyManager->IsItemLimited(itemId))
                            ImGui.TextUnformatted(currencyManager->GetItemCountRemaining(itemId).ToString());
                        ImGui.TableNextColumn();
                        DebugRenderer.DrawCopyableText(TextService.GetItemName(itemId, ImGui.IsKeyDown(ImGuiKey.LeftShift) ? ClientLanguage.English : null));
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
                    ImGui.TableSetupColumn("ItemId", ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("Count", ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("Remaining", ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("IsUnlimited", ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableHeadersRow();

                    foreach (var (itemId, item) in currencyManager->ItemBucket)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        DebugRenderer.DrawCopyableText(itemId.ToString());
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{item.Count} / {item.MaxCount}");
                        ImGui.TableNextColumn();
                        if (currencyManager->IsItemLimited(itemId))
                            ImGui.TextUnformatted(currencyManager->GetItemCountRemaining(itemId).ToString());
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(item.IsUnlimited.ToString());
                        ImGui.TableNextColumn();
                        DebugRenderer.DrawCopyableText(TextService.GetItemName(itemId, ImGui.IsKeyDown(ImGuiKey.LeftShift) ? ClientLanguage.English : null));
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
                    ImGui.TableSetupColumn("ItemId", ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("Count", ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("Remaining", ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("IsUnlimited", ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableHeadersRow();

                    foreach (var (itemId, item) in currencyManager->ContentItemBucket)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        DebugRenderer.DrawCopyableText(itemId.ToString());
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"{item.Count} / {item.MaxCount}");
                        ImGui.TableNextColumn();
                        if (currencyManager->IsItemLimited(itemId))
                            ImGui.TextUnformatted(currencyManager->GetItemCountRemaining(itemId).ToString());
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(item.IsUnlimited.ToString());
                        ImGui.TableNextColumn();
                        DebugRenderer.DrawCopyableText(TextService.GetItemName(itemId, ImGui.IsKeyDown(ImGuiKey.LeftShift) ? ClientLanguage.English : null));
                    }
                }
            }
        }
    }
}
