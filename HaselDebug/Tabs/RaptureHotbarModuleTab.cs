using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class RaptureHotbarModuleTab : DebugTab
{
    private readonly DebugRenderer _debugRenderer;
    private readonly ExcelService _excelService;
    private readonly TextService _textService;
    private readonly ISeStringEvaluator _seStringEvaluatorService;
    private readonly ITextureProvider _textureProvider;

    public override void Draw()
    {
        var raptureHotbarModule = RaptureHotbarModule.Instance();

        _debugRenderer.DrawPointerType(raptureHotbarModule, typeof(RaptureHotbarModule), new NodeOptions());

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        for (var i = 0; i < raptureHotbarModule->Hotbars.Length; i++)
        {
            var hotbar = raptureHotbarModule->Hotbars[i];

            using var titleColor = ImRaii.PushColor(ImGuiCol.Text, 0xFF00FFFF);
            using var node = ImRaii.TreeNode($"##Hotbar{i}", ImGuiTreeNodeFlags.SpanAvailWidth);

            ImGui.SameLine(ImGui.GetStyle().FramePadding.X * 3f + ImGui.GetFontSize(), 0);
            ImGui.Text($"Hotbar {i}");
            ImGui.SameLine(0, ImGui.GetStyle().FramePadding.X * 3);

            for (var j = 0; j < hotbar.Slots.Length; j++)
            {
                var slot = hotbar.Slots[j];
                DrawHotbarSlotIcon(slot);
                ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
            }
            ImGui.NewLine();

            if (!node)
                continue;
            titleColor?.Dispose();

            using var table = ImRaii.Table("RaptureHotbarModuleTable"u8, 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings);
            if (!table) return;

            ImGui.TableSetupColumn("Slot"u8, ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableSetupColumn("CommandType"u8, ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("CommandId"u8, ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Execute"u8, ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupScrollFreeze(5, 1);
            ImGui.TableHeadersRow();

            for (var j = 0; j < hotbar.Slots.Length; j++)
            {
                var slot = raptureHotbarModule->GetSlotById((uint)i, (uint)j);

                ImGui.TableNextRow();
                ImGui.TableNextColumn(); // Slot
                ImGui.Text(j.ToString());

                ImGui.TableNextColumn(); // CommandType
                ImGui.Text(slot->CommandType.ToString());

                ImGui.TableNextColumn(); // CommandId
                ImGui.Text(slot->CommandId.ToString());

                ImGui.TableNextColumn(); // Name
                if (!slot->IsEmpty)
                {
                    _debugRenderer.DrawIcon(slot->IconId);
                    _debugRenderer.DrawPointerType(slot, typeof(RaptureHotbarModule.HotbarSlot), new NodeOptions()
                    {
                        Title = slot->CommandType switch
                        {
                            RaptureHotbarModule.HotbarSlotType.Action => _textService.GetActionName(slot->CommandId),
                            RaptureHotbarModule.HotbarSlotType.Item => _textService.GetItemName(slot->CommandId).ToString(),
                            RaptureHotbarModule.HotbarSlotType.EventItem => _textService.GetItemName(slot->CommandId).ToString(),
                            RaptureHotbarModule.HotbarSlotType.Emote => _textService.GetEmoteName(slot->CommandId),
                            RaptureHotbarModule.HotbarSlotType.Macro => GetMacroName(slot->CommandId),
                            RaptureHotbarModule.HotbarSlotType.Marker => _excelService.TryGetRow<Marker>(slot->CommandId, out var marker) ? marker.Name.ToString() : $"Marker#{slot->CommandId}",
                            RaptureHotbarModule.HotbarSlotType.CraftAction => _textService.GetCraftActionName(slot->CommandId),
                            RaptureHotbarModule.HotbarSlotType.GeneralAction => _textService.GetGeneralActionName(slot->CommandId),
                            RaptureHotbarModule.HotbarSlotType.BuddyAction => _textService.GetBuddyActionName(slot->CommandId),
                            RaptureHotbarModule.HotbarSlotType.MainCommand => _textService.GetMainCommandName(slot->CommandId),
                            RaptureHotbarModule.HotbarSlotType.Companion => _textService.GetCompanionName(slot->CommandId),
                            RaptureHotbarModule.HotbarSlotType.GearSet => GetGearsetName((int)slot->CommandId),
                            RaptureHotbarModule.HotbarSlotType.PetAction => _textService.GetPetActionName(slot->CommandId),
                            RaptureHotbarModule.HotbarSlotType.Mount => _textService.GetMountName(slot->CommandId),
                            RaptureHotbarModule.HotbarSlotType.FieldMarker => _excelService.TryGetRow<FieldMarker>(slot->CommandId, out var fieldMarker) ? fieldMarker.Name.ToString() : $"FieldMarker#{slot->CommandId}",
                            RaptureHotbarModule.HotbarSlotType.Recipe => GetRecipeName(slot),
                            RaptureHotbarModule.HotbarSlotType.ChocoboRaceAbility => _excelService.TryGetRow<ChocoboRaceAbility>(slot->CommandId, out var chocoboRaceAbility) ? chocoboRaceAbility.Name.ToString() : $"ChocoboRaceAbility#{slot->CommandId}",
                            RaptureHotbarModule.HotbarSlotType.ChocoboRaceItem => _excelService.TryGetRow<ChocoboRaceItem>(slot->CommandId, out var chocoboRaceItem) ? chocoboRaceItem.Name.ToString() : $"ChocoboRaceItem#{slot->CommandId}",
                            RaptureHotbarModule.HotbarSlotType.ExtraCommand => _excelService.TryGetRow<ExtraCommand>(slot->CommandId, out var extraCommand) ? extraCommand.Name.ToString() : $"ExtraCommand#{slot->CommandId}",
                            RaptureHotbarModule.HotbarSlotType.PvPQuickChat => _excelService.TryGetRow<QuickChat>(slot->CommandId, out var quickChat) ? quickChat.NameAction.ToString() : $"QuickChat#{slot->CommandId}",
                            RaptureHotbarModule.HotbarSlotType.PvPCombo => _excelService.TryGetRow<ActionComboRoute>(slot->CommandId, out var actionComboRoute) ? actionComboRoute.Name.ToString() : $"ActionComboRoute#{slot->CommandId}",
                            RaptureHotbarModule.HotbarSlotType.BgcArmyAction => _excelService.TryGetRow<BgcArmyAction>(slot->CommandId, out var bgcArmyAction) ? bgcArmyAction.Name.ToString() : $"BgcArmyAction#{slot->CommandId}",
                            RaptureHotbarModule.HotbarSlotType.PerformanceInstrument => _excelService.TryGetRow<Perform>(slot->CommandId, out var perform) ? perform.Instrument.ToString() : $"Perform#{slot->CommandId}",
                            RaptureHotbarModule.HotbarSlotType.McGuffin => _excelService.TryGetRow<McGuffinUIData>(_excelService.TryGetRow<McGuffin>(slot->CommandId, out var mcGuffin) ? mcGuffin.UIData.RowId : 0, out var mcGuffinUIData) ? mcGuffinUIData.Name.ToString() : $"McGuffin#{slot->CommandId}",
                            RaptureHotbarModule.HotbarSlotType.Ornament => _textService.GetOrnamentName(slot->CommandId),
                            // LostFindsItem
                            RaptureHotbarModule.HotbarSlotType.Glasses => _textService.GetGlassesName(slot->CommandId),
                            _ => string.Empty
                        }
                    });
                }

                ImGui.TableNextColumn(); // Execute
                if (!slot->IsEmpty && ImGui.SmallButton($"Execute##H{i}S{j}Execute"))
                {
                    raptureHotbarModule->ExecuteSlot(slot);
                }
            }
        }
    }

    private string GetGearsetName(int gearsetId)
    {
        var gearset = RaptureGearsetModule.Instance()->GetGearset(gearsetId);
        return gearset == null || !gearset->Flags.HasFlag(RaptureGearsetModule.GearsetFlag.Exists) ? string.Empty : gearset->NameString;
    }

    private string GetMacroName(uint macroId)
    {
        var set = macroId >= 256 ? 1u : 0;
        var idx = macroId >= 256 ? macroId - 256 : macroId;

        var macro = RaptureMacroModule.Instance()->GetMacro(set, idx);
        return macro == null || !macro->IsNotEmpty() ? string.Empty : macro->Name.ToString();
    }

    private string GetRecipeName(RaptureHotbarModule.HotbarSlot* slot)
    {
        if (slot->RecipeValid == 0)
            return _textService.GetAddonText(1449); // Deleted Recipes

        return _seStringEvaluatorService.EvaluateFromAddon(1442, [slot->RecipeItemId, slot->RecipeCraftType + 8]).ToString();
    }

    private void DrawHotbarSlotIcon(RaptureHotbarModule.HotbarSlot slot)
    {
        if (_textureProvider.TryGetFromGameIcon(slot.IconId, out var tex) && tex.TryGetWrap(out var texture, out _))
        {
            ImGui.Image(texture.Handle, new Vector2(ImGui.GetTextLineHeight()));
        }
        else
        {
            ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight()));
        }
    }
}
