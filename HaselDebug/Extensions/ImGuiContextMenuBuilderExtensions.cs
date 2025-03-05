using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Services;
using HaselCommon.Utils;
using ImGuiNET;

namespace HaselDebug.Extensions;

public static unsafe class ImGuiContextMenuBuilderExtensions
{
    public static void AddCopyRowId(this ImGuiContextMenuBuilder builder, TextService textService, uint rowId)
    {
        builder.Add(new ImGuiContextMenuEntry()
        {
            Visible = rowId != 0,
            Label = textService.Translate("ContextMenu.CopyRowId"),
            ClickCallback = () => ImGui.SetClipboardText(rowId.ToString())
        });
    }

    public static void AddCopyName(this ImGuiContextMenuBuilder builder, TextService textService, string name)
    {
        builder.Add(new ImGuiContextMenuEntry()
        {
            Visible = !string.IsNullOrEmpty(name),
            Label = textService.Translate("ContextMenu.CopyName"),
            ClickCallback = () => ImGui.SetClipboardText(name)
        });
    }

    public static void AddCopyAddress(this ImGuiContextMenuBuilder builder, TextService textService, nint address)
    {
        builder.Add(new ImGuiContextMenuEntry()
        {
            Visible = address != 0,
            Label = textService.Translate("ContextMenu.CopyAddress"),
            ClickCallback = () => ImGui.SetClipboardText(address.ToString("X"))
        });
    }

    public static void AddViewOutfitGlamourReadyItems(this ImGuiContextMenuBuilder builder, TextService textService, ExcelService excelService, uint itemId)
    {
        builder.Add(new ViewOutfitGlamourReadyItemsContextMenuEntry(textService, excelService, itemId));
    }

    public static void AddRestoreItem(this ImGuiContextMenuBuilder builder, TextService textService, uint itemId)
    {
        builder.Add(new RestoreItemContextMenuEntry(textService, itemId));
    }

    private struct ViewOutfitGlamourReadyItemsContextMenuEntry(TextService textService, ExcelService excelService, uint itemId) : IImGuiContextMenuEntry
    {
        private readonly uint _itemId = itemId;

        public bool Visible
        {
            get
            {
                if (!AgentMiragePrismPrismBox.Instance()->IsAgentActive())
                    return false;

                if (!TryGetAddon<AtkUnitBase>("MiragePrismPrismBoxCrystallize", out _))
                    return false;

                if (!excelService.GetSheet<Lumina.Excel.Sheets.MirageStoreSetItemLookup>().HasRow(_itemId))
                    return false;

                return GetInventoryItem() != null;
            }
        }

        public bool Enabled => true;
        public string Label => textService.GetAddonText(15635);
        public bool LoseFocusOnClick => false;
        public Action? ClickCallback => OnClick;
        public Action? HoverCallback => null;

        public void Draw(IterationArgs args)
        {
            // TODO: this sucks, lol

            if (ImGui.MenuItem(Label, Enabled))
            {
                ClickCallback?.Invoke();

                if (LoseFocusOnClick)
                    ImGui.SetWindowFocus(null);
            }
            if (ImGui.IsItemHovered())
                HoverCallback?.Invoke();
        }

        public void OnClick()
        {
            var slot = GetInventoryItem();
            if (slot == null)
                return;

            if (!TryGetAddon<AtkUnitBase>("MiragePrismPrismBoxCrystallize", out var openerAddon))
                return;

            AgentMiragePrismPrismSetConvert.Instance()->Open(_itemId, slot->GetInventoryType(), slot->GetSlot(), openerAddon->Id, true);
        }

        private InventoryItem* GetInventoryItem()
        {
            for (var i = 0; i < 4; i++)
            {
                var container = InventoryManager.Instance()->GetInventoryContainer((InventoryType)i);
                if (container == null || !container->IsLoaded)
                    continue;

                for (var j = 0; j < container->GetSize(); j++)
                {
                    var slot = container->GetInventorySlot(j);
                    if (slot == null || slot->IsNotLinked())
                        continue;

                    if (slot->GetItemId() == _itemId)
                        return slot;
                }
            }

            return null;
        }
    }

    private struct RestoreItemContextMenuEntry(TextService textService, uint itemId) : IImGuiContextMenuEntry
    {
        public bool Visible
        {
            get
            {
                if (!AgentMiragePrismPrismBox.Instance()->IsAgentActive())
                    return false;

                var mirageManager = MirageManager.Instance();
                if (!mirageManager->PrismBoxLoaded)
                    return false;

                if (mirageManager->PrismBoxItemIds.IndexOf(itemId) == -1)
                    return false;

                return true;
            }
        }

        public bool Enabled => true;
        public string Label => textService.GetAddonText(11904);
        public bool LoseFocusOnClick => false;
        public Action? ClickCallback => OnClick;
        public Action? HoverCallback => null;

        public void Draw(IterationArgs args)
        {
            // TODO: this sucks, lol

            if (ImGui.MenuItem(Label, Enabled))
            {
                ClickCallback?.Invoke();

                if (LoseFocusOnClick)
                    ImGui.SetWindowFocus(null);
            }
            if (ImGui.IsItemHovered())
                HoverCallback?.Invoke();
        }

        public void OnClick()
        {
            var manager = MirageManager.Instance();
            if (!manager->PrismBoxLoaded) return;

            var index = MirageManager.Instance()->PrismBoxItemIds.IndexOf(itemId);
            if (index == -1) return;

            MirageManager.Instance()->RestorePrismBoxItem((uint)index);
        }
    }
}
