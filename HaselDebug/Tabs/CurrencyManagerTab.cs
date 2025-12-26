using System.Text;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.STD;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using static FFXIVClientStructs.FFXIV.Client.Game.CurrencyManager;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class CurrencyManagerTab : DebugTab
{
    private readonly DebugRenderer _debugRenderer;
    private readonly TextService _textService;
    private readonly ItemService _itemService;
    private readonly UnlocksTabUtils _unlocksTabUtils;

    public override string Title => "CurrencyManager";

    public override void Draw()
    {
        var currencyManager = CurrencyManager.Instance();
        _debugRenderer.DrawPointerType(currencyManager, typeof(CurrencyManager), new NodeOptions());

        using (var node = ImRaii.TreeNode(nameof(CurrencyManager.SpecialItemBucket), ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            if (node)
            {
                DrawCopyButton("Copy SpecialItemBucket"u8, ref currencyManager->SpecialItemBucket);

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
                        ImGuiUtils.DrawCopyableText(itemId.ToString());
                        ImGui.TableNextColumn();
                        ImGui.Text(item.SpecialId.ToString());
                        ImGui.TableNextColumn();
                        ImGui.Text($"{item.Count} / {item.MaxCount}");
                        ImGui.TableNextColumn();
                        if (currencyManager->IsItemLimited(itemId))
                            ImGui.Text(currencyManager->GetItemCountRemaining(itemId).ToString());
                        ImGui.TableNextColumn();
                        _unlocksTabUtils.DrawSelectableItem(itemId, $"SpecialItemBucketCurrency{itemId}");
                    }
                }
            }
        }

        using (var node = ImRaii.TreeNode(nameof(CurrencyManager.ItemBucket), ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            if (node)
            {
                DrawCopyButton("Copy ItemBucket"u8, ref currencyManager->ItemBucket);

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
                        ImGuiUtils.DrawCopyableText(itemId.ToString());
                        ImGui.TableNextColumn();
                        ImGui.Text($"{item.Count} / {item.MaxCount}");
                        ImGui.TableNextColumn();
                        if (!item.IsUnlimited)
                            ImGui.Text(currencyManager->GetItemCountRemaining(itemId).ToString());
                        ImGui.TableNextColumn();
                        ImGui.Text(item.IsUnlimited.ToString());
                        ImGui.TableNextColumn();
                        _unlocksTabUtils.DrawSelectableItem(itemId, $"ItemBucketCurrency{itemId}");
                    }
                }
            }
        }

        using (var node = ImRaii.TreeNode(nameof(CurrencyManager.ContentItemBucket), ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            if (node)
            {
                DrawCopyButton("Copy ContentItemBucket"u8, ref currencyManager->ContentItemBucket);

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
                        ImGuiUtils.DrawCopyableText(itemId.ToString());
                        ImGui.TableNextColumn();
                        ImGui.Text($"{item.Count} / {item.MaxCount}");
                        ImGui.TableNextColumn();
                        if (!item.IsUnlimited)
                            ImGui.Text(currencyManager->GetItemCountRemaining(itemId).ToString());
                        ImGui.TableNextColumn();
                        ImGui.Text(item.IsUnlimited.ToString());
                        ImGui.TableNextColumn();
                        _unlocksTabUtils.DrawSelectableItem(itemId, $"ContentItemBucketCurrency{itemId}");
                    }
                }
            }
        }
    }

    private void DrawCopyButton(ReadOnlySpan<byte> label, ref StdMap<uint, SpecialCurrencyItem> map)
    {
        if (!ImGui.Button(label))
            return;

        var longestName = 0;
        foreach (var (itemId, _) in map)
        {
            var nameLength = _itemService.GetItemName(itemId, false, ClientLanguage.English).ToString().Length;
            if (longestName < nameLength)
                longestName = nameLength;
        }

        var sb = new StringBuilder(@$"
    /// <remarks>
    /// This bucket is known to contain the following items:<br/>
    /// <code>
    /// |-----------|--------|-{new string('-', longestName)}-|<br/>
    /// | SpecialId | ItemId | {"Item Name".PadRight(longestName)} |<br/>
    /// |-----------|--------|-{new string('-', longestName)}-|<br/>
");

        foreach (var (itemId, item) in map.OrderBy(kv => kv.Value.SpecialId))
        {
            var name = _itemService.GetItemName(itemId, false, ClientLanguage.English).ToString();
            sb.AppendLine($"    /// | {item.SpecialId,-9} | {itemId,-6} | {name.PadRight(longestName)} |<br/>");
        }

        sb.Append(@$"    /// |-----------|--------|-{new string('-', longestName)}-|
    /// </code>
    /// </remarks>");

        ImGui.SetClipboardText(sb.ToString());
    }

    private void DrawCopyButton(ReadOnlySpan<byte> label, ref StdMap<uint, CurrencyItem> map)
    {
        if (!ImGui.Button(label))
            return;

        var longestName = 0;
        foreach (var (itemId, _) in map)
        {
            var nameLength = _itemService.GetItemName(itemId, false, ClientLanguage.English).ToString().Length;
            if (longestName < nameLength)
                longestName = nameLength;
        }

        var sb = new StringBuilder(@$"
    /// <remarks>
    /// This bucket is known to contain the following items:<br/>
    /// <code>
    /// |--------|-{new string('-', longestName)}-|<br/>
    /// | ItemId | {"Item Name".PadRight(longestName)} |<br/>
    /// |--------|-{new string('-', longestName)}-|<br/>
");

        foreach (var (itemId, _) in map.OrderBy(kv => kv.Key))
        {
            var name = _itemService.GetItemName(itemId, false, ClientLanguage.English).ToString();
            sb.AppendLine($"    /// | {itemId,-6} | {name.PadRight(longestName)} |<br/>");
        }

        sb.Append(@$"    /// |--------|-{new string('-', longestName)}-|
    /// </code>
    /// </remarks>");

        ImGui.SetClipboardText(sb.ToString());
    }

    private void DrawCopyButton(ReadOnlySpan<byte> label, ref StdMap<uint, ContentCurrencyItem> map)
    {
        if (!ImGui.Button(label))
            return;

        var longestName = 0;
        foreach (var (itemId, _) in map)
        {
            var name = itemId switch
            {
                33138 => "(YoRHa Questline Progress)",
                _ => _itemService.GetItemName(itemId, false, ClientLanguage.English).ToString()
            };
            var nameLength = name.Length;
            if (longestName < nameLength)
                longestName = nameLength;
        }

        var sb = new StringBuilder(@$"
    /// <remarks>
    /// This bucket is known to contain the following items:<br/>
    /// <code>
    /// |--------|-{new string('-', longestName)}-|<br/>
    /// | ItemId | {"Item Name".PadRight(longestName)} |<br/>
    /// |--------|-{new string('-', longestName)}-|<br/>
");

        foreach (var (itemId, _) in map.OrderBy(kv => kv.Key))
        {
            var name = itemId switch
            {
                33138 => "(YoRHa Questline Progress)",
                _ => _itemService.GetItemName(itemId, false, ClientLanguage.English).ToString()
            };
            sb.AppendLine($"    /// | {itemId,-6} | {name.PadRight(longestName)} |<br/>");
        }

        sb.Append(@$"    /// |--------|-{new string('-', longestName)}-|
    /// </code>
    /// </remarks>");

        ImGui.SetClipboardText(sb.ToString());
    }
}
