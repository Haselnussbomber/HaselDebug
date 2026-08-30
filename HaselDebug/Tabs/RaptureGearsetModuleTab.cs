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
    private readonly ITextureProvider _textureProvider;

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
        using var scroll = ImRaii.Child("RaptureGearsetModuleEntriesScroll"u8, new Vector2(0, ImGui.GetContentRegionAvail().Y), false, ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.NoSavedSettings);
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

            using (var table = ImRaii.Table($"RaptureGearsetModuleEntriesChunk{chunkIndex}", 9, GearsetEntryTableFlags))
            {
                if (!table)
                    return;

                SetupGearsetEntryColumns();

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

    private static void SetupGearsetEntryColumns()
    {
        ImGui.TableSetupColumn("Id"u8, ImGuiTableColumnFlags.WidthFixed, 30);
        ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthFixed, 140);
        ImGui.TableSetupColumn("ClassJob"u8, ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableSetupColumn("GlamourSetLink"u8, ImGuiTableColumnFlags.WidthFixed, 110);
        ImGui.TableSetupColumn("ItemLevel"u8, ImGuiTableColumnFlags.WidthFixed, 70);
        ImGui.TableSetupColumn("BannerIndex"u8, ImGuiTableColumnFlags.WidthFixed, 90);
        ImGui.TableSetupColumn("Flags"u8, ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableSetupColumn("Items"u8, ImGuiTableColumnFlags.WidthFixed, 300);
        ImGui.TableSetupColumn("GlassesIds"u8, ImGuiTableColumnFlags.WidthFixed, 90);
    }

    private bool DrawGearsetEntryRow(RaptureGearsetModule* module, int i, RaptureGearsetModule.GearsetEntry* gearset)
    {
        ImGui.TableNextColumn(); // Id
        ImGui.Text(gearset->Id.ToString());

        ImGui.TableNextColumn(); // Name
        ImGui.Text(gearset->NameString);
        DrawGearsetContextMenu(module, i, gearset);

        ImGui.TableNextColumn(); // ClassJob
        if (_excelService.TryGetRow<ClassJob>(gearset->ClassJob, out var classJob))
        {
            ImGui.Text($"{gearset->ClassJob} ({classJob.Name})");
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
        TextWithTooltip(gearset->Flags.ToString(), $"##FlagsTooltip{i}");

        var itemsExpanded = false;
        ImGui.TableNextColumn(); // Items
        using (var itemsNode = ImRaii.TreeNode($"##Items{i}", ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            itemsExpanded = itemsNode;
            DrawGearsetItemIcons(gearset);
        }

        ImGui.TableNextColumn(); // GlassesIds
        ImGui.Text($"{gearset->GlassesIds[0]}, {gearset->GlassesIds[1]}");

        return itemsExpanded;
    }

    private void DrawGearsetItemsTable(int gearsetId, RaptureGearsetModule.GearsetEntry* gearset)
    {
        using var table = ImRaii.Table($"RaptureGearsetModuleItemsTable{gearsetId}", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings);
        if (!table)
            return;

        ImGui.TableSetupColumn("Slot"u8, ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Armoury"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("ItemId"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(4, 1);
        ImGui.TableHeadersRow();

        foreach (var slot in Enum.GetValues<RaptureGearsetModule.GearsetItemIndex>())
        {
            var item = gearset->Items[(int)slot];

            using var disabled = ImRaii.Disabled(item.ItemId == 0);

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Slot
            ImGui.Text(((int)slot).ToString());

            ImGui.TableNextColumn(); // Armoury
            ImGui.Text(slot.ToString());

            ImGui.TableNextColumn(); // ItemId
            ImGui.Text(item.ItemId.ToString());

            ImGui.TableNextColumn(); // Name
            if (item.ItemId != 0)
            {
                _debugRenderer.DrawIcon(_itemService.GetItemIcon(item.ItemId), ItemUtil.IsHighQuality(item.ItemId));

                fixed (RaptureGearsetModule.GearsetItem* itemsPtr = gearset->Items)
                {
                    _debugRenderer.DrawPointerType(itemsPtr + (int)slot, new NodeOptions()
                    {
                        AddressPath = new AddressPath([gearsetId, (nint)slot]),
                        Title = _textService.GetItemName(item.ItemId).ToString(),
                    });
                }
            }
        }
    }

    private void DrawGearsetItemIcons(RaptureGearsetModule.GearsetEntry* gearset)
    {
        ImGui.SameLine(0, ImStyle.ItemInnerSpacing.X);

        foreach (var slot in Enum.GetValues<RaptureGearsetModule.GearsetItemIndex>())
        {
            DrawGearsetItemIcon(gearset->Items[(int)slot]);
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

    private static void TextWithTooltip(string text, string id)
    {
        var maxWidth = ImGui.GetContentRegionAvail().X;
        var pos = ImGui.GetCursorScreenPos();
        var size = new Vector2(maxWidth, ImGui.GetTextLineHeightWithSpacing());
        var clipRect = new Vector4(pos.X, pos.Y, pos.X + size.X, pos.Y + size.Y);

        ImGui.GetWindowDrawList().AddTextClippedEx(pos, pos + size, text, null, Vector2.Zero, clipRect);

        ImGui.SetCursorScreenPos(pos);
        ImGui.InvisibleButton(id, size);

        if (ImGui.IsItemHovered() && ImGui.CalcTextSize(text).X > maxWidth)
        {
            using var tooltip = ImRaii.Tooltip();
            ImGui.Text(text);
        }
    }

    private void DrawGearsetItemIcon(RaptureGearsetModule.GearsetItem item)
    {
        var iconId = item.ItemId != 0 ? _itemService.GetItemIcon(item.ItemId) : 0u;
        if (iconId != 0 && _textureProvider.TryGetFromGameIcon(iconId, out var tex) && tex.TryGetWrap(out var texture, out _))
        {
            ImGui.Image(texture.Handle, new Vector2(ImStyle.TextLineHeight));
        }
        else
        {
            ImGui.Dummy(new Vector2(ImStyle.TextLineHeight));
        }
    }
}
