using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class RaptureGearsetModuleTab : DebugTab
{
    private const ImGuiTableFlags GearsetEntryTableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings;

    private readonly DebugRenderer _debugRenderer;
    private readonly TextService _textService;
    private readonly ItemService _itemService;
    private readonly ExcelService _excelService;

    private int? _renamingGearsetId;
    private string _renameInput = string.Empty;
    private bool _openRenamePopup;

    public override void Draw()
    {
        var raptureGearsetModule = RaptureGearsetModule.Instance();

        _debugRenderer.DrawPointerType(raptureGearsetModule);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        DrawGearsetEntriesTable(raptureGearsetModule);
        DrawRenamePopup();
    }

    private void DrawGearsetEntriesTable(RaptureGearsetModule* module)
    {
        using var scroll = ImRaii.Child("RaptureGearsetModuleEntriesScroll"u8, new Vector2(-1), false, ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.NoSavedSettings);
        if (!scroll)
            return;

        var drawHeader = true;
        var chunkIndex = 0;

        for (var i = 0; i < module->Entries.Length;)
        {
            var gearset = module->GetGearset(i);
            if (gearset == null)
            {
                i++;
                continue;
            }

            var itemsExpanded = false;
            RaptureGearsetModule.GearsetEntry* expandedGearset = null;
            var expandedGearsetId = -1;

            using (var table = ImRaii.Table($"RaptureGearsetModuleEntriesChunk{chunkIndex}", 8, GearsetEntryTableFlags))
            {
                if (!table)
                    return;

                ImGui.TableSetupColumn("Id"u8, ImGuiTableColumnFlags.WidthFixed, 30);
                ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("ClassJob"u8, ImGuiTableColumnFlags.WidthFixed, 140);
                ImGui.TableSetupColumn("GlamourSetLink"u8, ImGuiTableColumnFlags.WidthFixed, 110);
                ImGui.TableSetupColumn("ItemLevel"u8, ImGuiTableColumnFlags.WidthFixed, 70);
                ImGui.TableSetupColumn("BannerIndex"u8, ImGuiTableColumnFlags.WidthFixed, 90);
                ImGui.TableSetupColumn("Flags"u8, ImGuiTableColumnFlags.WidthFixed, 120);
                ImGui.TableSetupColumn("Items"u8, ImGuiTableColumnFlags.WidthFixed, 360);

                if (drawHeader)
                {
                    ImGui.TableHeadersRow();
                    drawHeader = false;
                }

                for (; i < module->Entries.Length; i++)
                {
                    gearset = module->GetGearset(i);
                    if (gearset == null)
                        continue;

                    using var disabled = ImRaii.Disabled(!gearset->Flags.HasFlag(RaptureGearsetModule.GearsetFlag.Exists));

                    ImGui.TableNextRow();
                    itemsExpanded = DrawGearsetEntryRow(module, i, gearset);

                    if (itemsExpanded)
                    {
                        expandedGearset = gearset;
                        expandedGearsetId = i;
                        i++;
                        break;
                    }
                }
            }

            chunkIndex++;

            if (itemsExpanded && expandedGearset != null)
                DrawGearsetItemsTable(expandedGearsetId, expandedGearset);
        }
    }

    private bool DrawGearsetEntryRow(RaptureGearsetModule* module, int i, RaptureGearsetModule.GearsetEntry* gearset)
    {
        ClassJob classJob = default;
        var hasClassJob = gearset->ClassJob != 0 && _excelService.TryGetRow<ClassJob>(gearset->ClassJob, out classJob);

        ImGui.TableNextColumn(); // Id
        ImGui.Text(gearset->Id.ToString());

        ImGui.TableNextColumn(); // Name
        if (hasClassJob)
        {
            _debugRenderer.DrawIcon(62000 + classJob.RowId);
        }

        if (ImGui.Selectable(gearset->NameString, module->CurrentGearsetIndex == i))
        {
            module->EquipGearset(i);
        }
        DrawGearsetContextMenu(module, i, gearset);

        ImGui.TableNextColumn(); // ClassJob
        if (hasClassJob)
        {
            ImGui.Text($"{classJob.Name} ({gearset->ClassJob})");
        }
        else
        {
            ImGui.Text(gearset->ClassJob.ToString());
        }

        ImGui.TableNextColumn(); // GlamourSetLink
        ImGui.Text(gearset->GlamourSetLink.ToString());

        ImGui.TableNextColumn(); // ItemLevel
        ImGui.Text(gearset->ItemLevel.ToString());

        ImGui.TableNextColumn(); // BannerIndex
        ImGui.Text(gearset->BannerIndex.ToString());

        ImGui.TableNextColumn(); // Flags
        ImGuiUtils.DrawCopyableText(gearset->Flags.ToString());

        var itemsExpanded = false;
        ImGui.TableNextColumn(); // Items
        using (var itemsNode = ImRaii.TreeNode($"##Items{i}", ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            itemsExpanded = itemsNode;
            DrawGearsetItemIcons(gearset);
        }

        return itemsExpanded;
    }

    private void DrawGearsetItemsTable(int gearsetId, RaptureGearsetModule.GearsetEntry* gearset)
    {
        using var table = ImRaii.Table($"RaptureGearsetModuleItemsTable{gearsetId}", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings);
        if (!table)
            return;

        ImGui.TableSetupColumn("Slot"u8, ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Armoury"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Id"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(4, 1);
        ImGui.TableHeadersRow();

        foreach (var slot in Enum.GetValues<RaptureGearsetModule.GearsetItemIndex>())
        {
            var item = gearset->Items.GetPointer((int)slot);

            using var disabled = ImRaii.Disabled(item->ItemId == 0);

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Slot
            ImGui.Text(((int)slot).ToString());

            ImGui.TableNextColumn(); // Armoury
            ImGui.Text(slot.ToString());

            ImGui.TableNextColumn(); // Id
            ImGuiUtils.DrawCopyableText(item->ItemId.ToString());

            ImGui.TableNextColumn(); // Name
            if (item->ItemId != 0)
            {
                _debugRenderer.DrawIcon(_itemService.GetItemIcon(item->ItemId), ItemUtil.IsHighQuality(item->ItemId));
                _debugRenderer.DrawPointerType(item, new NodeOptions()
                {
                    AddressPath = new AddressPath([gearsetId, (nint)slot]),
                    Title = _textService.GetItemName(item->ItemId).ToString(),
                });
            }
        }

        var glassesIndex = -1;
        foreach (var glassesId in gearset->GlassesIds)
        {
            glassesIndex++;

            if (!_excelService.TryGetRow<Glasses>(glassesId, out var row))
                continue;

            using var disabled = ImRaii.Disabled(glassesId == 0);

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Slot
            ImGui.Text(glassesIndex.ToString());

            ImGui.TableNextColumn(); // Armoury
            ImGui.Text("Glasses"u8);

            ImGui.TableNextColumn(); // Id
            ImGuiUtils.DrawCopyableText(glassesId.ToString());

            ImGui.TableNextColumn(); // Name
            if (glassesId != 0)
            {
                _debugRenderer.DrawIcon((uint)row.Icon);
                _debugRenderer.DrawExdRow(typeof(Glasses), row.RowId, 0, new()
                {
                    Title = row.Name.ToString()
                });
            }
        }
    }

    private void DrawGearsetItemIcons(RaptureGearsetModule.GearsetEntry* gearset)
    {
        ImGui.SameLine(0, ImStyle.ItemInnerSpacing.X);

        foreach (var slot in Enum.GetValues<RaptureGearsetModule.GearsetItemIndex>())
        {
            var (baseId, kind) = ItemUtil.GetBaseId(gearset->GetItem(slot).ItemId);
            _debugRenderer.DrawIcon(_itemService.GetItemIcon(baseId), kind == Dalamud.Utility.ItemKind.Hq, false, canCopy: false, noTooltip: true);
            ImGui.SameLine(0, ImStyle.ItemInnerSpacing.X);
        }

        foreach (var glassesId in gearset->GlassesIds)
        {
            if (glassesId == 0 || !_excelService.TryGetRow<Glasses>(glassesId, out var row))
                continue;

            _debugRenderer.DrawIcon((uint)row.Icon, false, false, canCopy: false, noTooltip: true);
            ImGui.SameLine(0, ImStyle.ItemInnerSpacing.X);
        }
    }

    private void DrawGearsetContextMenu(RaptureGearsetModule* module, int gearsetId, RaptureGearsetModule.GearsetEntry* gearset)
    {
        ImGuiContextMenu.Draw($"##Gearset{gearsetId}ContextMenu", builder =>
        {
            builder.Add(new ImGuiContextMenuEntry()
            {
                Label = "Equip Gearset",
                ClickCallback = () => module->EquipGearset(gearsetId),
            });

            builder.Add(new ImGuiContextMenuEntry()
            {
                Label = "Update Gearset",
                ClickCallback = () => module->UpdateGearset(gearsetId),
            });

            builder.Add(new ImGuiContextMenuEntry()
            {
                Label = "Rename Gearset",
                ClickCallback = () =>
                {
                    _renamingGearsetId = gearsetId;
                    _renameInput = gearset->NameString;
                    _openRenamePopup = true;
                },
            });

            builder.AddSeparator();

            builder.Add(new ImGuiContextMenuEntry()
            {
                Label = "Delete Gearset",
                Enabled = ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift),
                ClickCallback = () => module->DeleteGearset(gearsetId),
            });
        });
    }

    private void DrawRenamePopup()
    {
        if (_openRenamePopup)
        {
            ImGui.OpenPopup("RenameGearsetPopup");
            _openRenamePopup = false;
        }

        using var popup = ImRaii.PopupModal("RenameGearsetPopup", ImGuiWindowFlags.AlwaysAutoResize);
        if (!popup)
            return;

        ImGui.SetNextItemWidth(300);
        if (ImGui.InputText("Name"u8, ref _renameInput, 47, ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EnterReturnsTrue))
            ConfirmRename();

        ImGui.Spacing();

        if (ImGui.Button("Rename", new Vector2(ImGui.GetContentRegionAvail().X / 2f - ImStyle.ItemInnerSpacing.X / 2f, 0)))
            ConfirmRename();

        ImGui.SameLine();

        if (ImGui.Button("Cancel", ImGui.GetContentRegionAvail()))
        {
            _renamingGearsetId = null;
            _renameInput = string.Empty;
            ImGui.CloseCurrentPopup();
        }
    }

    private void ConfirmRename()
    {
        if (_renamingGearsetId is not int gearsetId || string.IsNullOrWhiteSpace(_renameInput))
            return;

        using var name = new Utf8String(_renameInput);
        RaptureGearsetModule.Instance()->RenameGearset(gearsetId, &name);
        _renamingGearsetId = null;
        _renameInput = string.Empty;
        ImGui.CloseCurrentPopup();
    }
}
