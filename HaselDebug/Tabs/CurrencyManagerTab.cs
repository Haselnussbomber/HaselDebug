using System.Text;
using Dalamud.Game;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

public unsafe class CurrencyManagerTab(DebugRenderer DebugRenderer, ExcelService ExcelService, TextService TextService) : DebugTab
{
    public override string Title => "CurrencyManager";

    public override void Draw()
    {
        var currencyManager = CurrencyManager.Instance();
        DebugRenderer.DrawPointerType(currencyManager, typeof(CurrencyManager), new NodeOptions());

        ImGui.TextUnformatted(nameof(CurrencyManager.SpecialItemBucket));
        using (var table = ImRaii.Table(nameof(CurrencyManager.SpecialItemBucket) + "Table", 5))
        {
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
                    ImGui.TextUnformatted($"{itemId}");
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{item.SpecialId}");
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{item.Count} / {item.MaxCount}");
                    ImGui.TableNextColumn();
                    if (currencyManager->IsItemLimited(itemId))
                        ImGui.TextUnformatted($"{currencyManager->GetItemCountRemaining(itemId)}");
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(TextService.GetItemName(itemId));
                }
            }
        }

        ImGui.TextUnformatted(nameof(CurrencyManager.ItemBucket));
        using (var table = ImRaii.Table(nameof(CurrencyManager.ItemBucket) + "Table", 5))
        {
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
                    ImGui.TextUnformatted($"{itemId}");
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{item.Count} / {item.MaxCount}");
                    ImGui.TableNextColumn();
                    if (currencyManager->IsItemLimited(itemId))
                        ImGui.TextUnformatted($"{currencyManager->GetItemCountRemaining(itemId)}");
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{item.IsUnlimited}");
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(TextService.GetItemName(itemId));
                }
            }
        }

        ImGui.TextUnformatted(nameof(CurrencyManager.ContentItemBucket));
        using (var table = ImRaii.Table(nameof(CurrencyManager.ContentItemBucket) + "Table", 5))
        {
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
                    ImGui.TextUnformatted($"{itemId}");
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{item.Count} / {item.MaxCount}");
                    ImGui.TableNextColumn();
                    if (currencyManager->IsItemLimited(itemId))
                        ImGui.TextUnformatted($"{currencyManager->GetItemCountRemaining(itemId)}");
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"{item.IsUnlimited}");
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(TextService.GetItemName(itemId));
                }
            }
        }

        var sb = new StringBuilder();
        foreach (var (itemId, item) in currencyManager->ContentItemBucket)
        {
            sb.AppendLine($"| {itemId} | {ExcelService.GetSheet<Item>(ClientLanguage.English).GetRow(itemId).Name.ToDalamudString().ToString()} |<br/>");
        }
        DebugRenderer.DrawCopyableText(sb.ToString());
    }
}
