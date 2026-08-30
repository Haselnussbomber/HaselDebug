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
    private readonly DebugRenderer _debugRenderer;
    private readonly TextService _textService;
    private readonly ItemService _itemService;
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

        for (var i = 0; i < raptureGearsetModule->Entries.Length; i++)
        {
            var gearset = raptureGearsetModule->GetGearset(i);
            if (gearset == null || !gearset->Flags.HasFlag(RaptureGearsetModule.GearsetFlag.Exists))
                continue;

            using var titleColor = ImRaii.PushColor(ImGuiCol.Text, 0xFF00FFFF);
            using var node = ImRaii.TreeNode($"##Gearset{i}", ImGuiTreeNodeFlags.SpanAvailWidth);

            DrawGearsetContextMenu(raptureGearsetModule, i, gearset);

            ImGui.SameLine(ImStyle.FramePadding.X * 3f + ImGui.GetFontSize(), 0);
            ImGui.Text($"Gearset {i} - ");
            ImGui.SameLine(0, ImStyle.FramePadding.X);
            ImGui.Text(gearset->NameString);
            ImGui.SameLine(0, ImStyle.FramePadding.X * 3);

            foreach (var slot in Enum.GetValues<RaptureGearsetModule.GearsetItemIndex>())
            {
                DrawGearsetItemIcon(gearset->Items[(int)slot]);
                ImGui.SameLine(0, ImStyle.ItemInnerSpacing.X);
            }
            ImGui.NewLine();

            if (!node)
                continue;
            titleColor?.Dispose();

            using var table = ImRaii.Table($"RaptureGearsetModuleTable{i}", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings);
            if (!table) return;

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
                            AddressPath = new AddressPath([i, (nint)slot]),
                            Title = _textService.GetItemName(item.ItemId).ToString(),
                        });
                    }
                }
            }
        }

        DrawRenamePopup();
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
