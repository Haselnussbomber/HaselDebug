using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselDebug.Extensions;

public static unsafe class ImGuiContextMenuBuilderExtensions
{
    public static void AddCopyRowId(this ImGuiContextMenuBuilder builder, uint rowId)
    {
        builder.Add(new ImGuiContextMenuEntry()
        {
            Visible = rowId != 0,
            Label = ServiceLocator.GetService<TextService>()?.Translate("ContextMenu.CopyRowId") ?? "Copy RowId",
            ClickCallback = () => ImGui.SetClipboardText(rowId.ToString())
        });
    }

    public static void AddCopyName(this ImGuiContextMenuBuilder builder, string name)
    {
        builder.Add(new ImGuiContextMenuEntry()
        {
            Visible = !string.IsNullOrEmpty(name),
            Label = ServiceLocator.GetService<TextService>()?.Translate("ContextMenu.CopyName") ?? "Copy Name",
            ClickCallback = () => ImGui.SetClipboardText(name)
        });
    }

    public static void AddCopyAddress(this ImGuiContextMenuBuilder builder, nint address)
    {
        builder.Add(new ImGuiContextMenuEntry()
        {
            Visible = address != 0,
            Label = ServiceLocator.GetService<TextService>()?.Translate("ContextMenu.CopyAddress") ?? "Copy Address",
            ClickCallback = () => ImGui.SetClipboardText(address.ToString("X"))
        });
    }

    public static void AddViewOutfitGlamourReadyItems(this ImGuiContextMenuBuilder builder, ItemHandle item)
    {
        builder.Add(new ViewOutfitGlamourReadyItemsContextMenuEntry(item));
    }

    public static void AddRestoreItem(this ImGuiContextMenuBuilder builder, ItemHandle item)
    {
        builder.Add(new RestoreItemContextMenuEntry(item));
    }

    private readonly struct ViewOutfitGlamourReadyItemsContextMenuEntry(ItemHandle item) : IImGuiContextMenuEntry
    {
        public bool Visible
        {
            get
            {
                if (!AgentMiragePrismPrismBox.Instance()->IsAgentActive())
                    return false;

                if (!TryGetAddon<AtkUnitBase>("MiragePrismPrismBoxCrystallize", out _))
                    return false;

                if (!ServiceLocator.TryGetService<ExcelModule>(out var excelModule) || !excelModule.GetSheet<Lumina.Excel.Sheets.MirageStoreSetItemLookup>().HasRow(item))
                    return false;

                return GetInventoryItem() != null;
            }
        }

        public bool Enabled => true;
        public bool Selected => false;
        public string Label => ServiceLocator.GetService<TextService>()?.GetAddonText(15635) ?? "View Outfit Glamour-ready Items";
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
                    ImGui.SetWindowFocus();
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

            AgentMiragePrismPrismSetConvert.Instance()->Open(item, slot->GetInventoryType(), slot->GetSlot(), openerAddon->Id, true);
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
                    if (slot == null || slot->IsEmpty())
                        continue;

                    if (slot->GetItemId() == item)
                        return slot;
                }
            }

            return null;
        }
    }

    private struct RestoreItemContextMenuEntry(ItemHandle item) : IImGuiContextMenuEntry
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

                if (mirageManager->PrismBoxItemIds.IndexOf(item) == -1)
                    return false;

                return true;
            }
        }

        public bool Enabled => true;
        public bool Selected => false;
        public string Label => ServiceLocator.GetService<TextService>()?.GetAddonText(11904) ?? "Restore Item";
        public bool LoseFocusOnClick => false;
        public Action? ClickCallback => OnClick;
        public Action? HoverCallback => null;

        public void Draw(IterationArgs args)
        {
            // TODO: this sucks, lol

            if (ImGui.MenuItem(Label, false, Enabled))
            {
                ClickCallback?.Invoke();

                if (LoseFocusOnClick)
                    ImGui.SetWindowFocus();
            }
            if (ImGui.IsItemHovered())
                HoverCallback?.Invoke();
        }

        public void OnClick()
        {
            var manager = MirageManager.Instance();
            if (!manager->PrismBoxLoaded) return;

            var index = MirageManager.Instance()->PrismBoxItemIds.IndexOf(item);
            if (index == -1) return;

            MirageManager.Instance()->RestorePrismBoxItem((uint)index);
        }
    }
}
