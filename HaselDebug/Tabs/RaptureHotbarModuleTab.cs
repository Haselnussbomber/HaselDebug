using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselCommon.Services;
using HaselCommon.Services.SeStringEvaluation;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append)]
public unsafe class RaptureHotbarModuleTab(
    DebugRenderer DebugRenderer,
    ExcelService ExcelService,
    TextService TextService,
    SeStringEvaluatorService SeStringEvaluatorService,
    ITextureProvider TextureProvider) : DebugTab
{
    public override void Draw()
    {
        var raptureHotbarModule = RaptureHotbarModule.Instance();

        DebugRenderer.DrawPointerType(raptureHotbarModule, typeof(RaptureHotbarModule), new NodeOptions());

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        for (var i = 0; i < raptureHotbarModule->Hotbars.Length; i++)
        {
            var hotbar = raptureHotbarModule->Hotbars[i];

            using var titleColor = ImRaii.PushColor(ImGuiCol.Text, 0xFF00FFFF);
            using var node = ImRaii.TreeNode($"##Hotbar{i}", ImGuiTreeNodeFlags.SpanAvailWidth);

            ImGui.SameLine(ImGui.GetStyle().FramePadding.X * 3f + ImGui.GetFontSize(), 0);
            ImGui.TextUnformatted($"Hotbar {i}");
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

            using var table = ImRaii.Table("RaptureHotbarModuleTable", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings);
            if (!table) return;

            ImGui.TableSetupColumn("Slot", ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableSetupColumn("CommandType", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("CommandId", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Execute", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupScrollFreeze(5, 1);
            ImGui.TableHeadersRow();

            for (var j = 0; j < hotbar.Slots.Length; j++)
            {
                var slot = raptureHotbarModule->GetSlotById((uint)i, (uint)j);

                ImGui.TableNextRow();
                ImGui.TableNextColumn(); // Slot
                ImGui.TextUnformatted(j.ToString());

                ImGui.TableNextColumn(); // CommandType
                ImGui.TextUnformatted(slot->CommandType.ToString());

                ImGui.TableNextColumn(); // CommandId
                ImGui.TextUnformatted(slot->CommandId.ToString());

                ImGui.TableNextColumn(); // Name
                if (!slot->IsEmpty)
                {
                    DebugRenderer.DrawIcon(slot->IconId);
                    DebugRenderer.DrawPointerType(slot, typeof(RaptureHotbarModule.HotbarSlot), new NodeOptions()
                    {
                        Title = slot->CommandType switch
                        {
                            RaptureHotbarModule.HotbarSlotType.Action => TextService.GetActionName(slot->CommandId),
                            RaptureHotbarModule.HotbarSlotType.Item => TextService.GetItemName(slot->CommandId),
                            RaptureHotbarModule.HotbarSlotType.EventItem => TextService.GetItemName(slot->CommandId),
                            RaptureHotbarModule.HotbarSlotType.Emote => TextService.GetEmoteName(slot->CommandId),
                            RaptureHotbarModule.HotbarSlotType.Macro => GetMacroName(slot->CommandId),
                            RaptureHotbarModule.HotbarSlotType.Marker => ExcelService.TryGetRow<Marker>(slot->CommandId, out var marker) ? marker.Name.ExtractText() : $"Marker#{slot->CommandId}",
                            RaptureHotbarModule.HotbarSlotType.CraftAction => TextService.GetCraftActionName(slot->CommandId),
                            RaptureHotbarModule.HotbarSlotType.GeneralAction => TextService.GetGeneralActionName(slot->CommandId),
                            RaptureHotbarModule.HotbarSlotType.BuddyAction => TextService.GetBuddyActionName(slot->CommandId),
                            RaptureHotbarModule.HotbarSlotType.MainCommand => TextService.GetMainCommandName(slot->CommandId),
                            RaptureHotbarModule.HotbarSlotType.Companion => TextService.GetCompanionName(slot->CommandId),
                            RaptureHotbarModule.HotbarSlotType.GearSet => GetGearsetName((int)slot->CommandId),
                            RaptureHotbarModule.HotbarSlotType.PetAction => TextService.GetPetActionName(slot->CommandId),
                            RaptureHotbarModule.HotbarSlotType.Mount => TextService.GetMountName(slot->CommandId),
                            RaptureHotbarModule.HotbarSlotType.FieldMarker => ExcelService.TryGetRow<FieldMarker>(slot->CommandId, out var fieldMarker) ? fieldMarker.Name.ExtractText() : $"FieldMarker#{slot->CommandId}",
                            RaptureHotbarModule.HotbarSlotType.Recipe => GetRecipeName(slot),
                            RaptureHotbarModule.HotbarSlotType.ChocoboRaceAbility => ExcelService.TryGetRow<ChocoboRaceAbility>(slot->CommandId, out var chocoboRaceAbility) ? chocoboRaceAbility.Name.ExtractText() : $"ChocoboRaceAbility#{slot->CommandId}",
                            RaptureHotbarModule.HotbarSlotType.ChocoboRaceItem => ExcelService.TryGetRow<ChocoboRaceItem>(slot->CommandId, out var chocoboRaceItem) ? chocoboRaceItem.Name.ExtractText() : $"ChocoboRaceItem#{slot->CommandId}",
                            RaptureHotbarModule.HotbarSlotType.ExtraCommand => ExcelService.TryGetRow<ExtraCommand>(slot->CommandId, out var extraCommand) ? extraCommand.Name.ExtractText() : $"ExtraCommand#{slot->CommandId}",
                            RaptureHotbarModule.HotbarSlotType.PvPQuickChat => ExcelService.TryGetRow<QuickChat>(slot->CommandId, out var quickChat) ? quickChat.NameAction.ExtractText() : $"QuickChat#{slot->CommandId}",
                            RaptureHotbarModule.HotbarSlotType.PvPCombo => ExcelService.TryGetRow<ActionComboRoute>(slot->CommandId, out var actionComboRoute) ? actionComboRoute.Name.ExtractText() : $"ActionComboRoute#{slot->CommandId}",
                            RaptureHotbarModule.HotbarSlotType.BgcArmyAction => ExcelService.TryGetRow<BgcArmyAction>(slot->CommandId, out var bgcArmyAction) ? bgcArmyAction.Unknown0.ExtractText() : $"BgcArmyAction#{slot->CommandId}",
                            RaptureHotbarModule.HotbarSlotType.PerformanceInstrument => ExcelService.TryGetRow<Perform>(slot->CommandId, out var perform) ? perform.Instrument.ExtractText() : $"Perform#{slot->CommandId}",
                            RaptureHotbarModule.HotbarSlotType.McGuffin => ExcelService.TryGetRow<McGuffinUIData>(ExcelService.TryGetRow<McGuffin>(slot->CommandId, out var mcGuffin) ? mcGuffin.UIData.RowId : 0, out var mcGuffinUIData) ? mcGuffinUIData.Name.ExtractText() : $"McGuffin#{slot->CommandId}",
                            RaptureHotbarModule.HotbarSlotType.Ornament => TextService.GetOrnamentName(slot->CommandId),
                            // LostFindsItem
                            RaptureHotbarModule.HotbarSlotType.Glasses => TextService.GetGlassesName(slot->CommandId),
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
            return TextService.GetAddonText(1449); // Deleted Recipes

        return SeStringEvaluatorService.EvaluateFromAddon(1442, new SeStringContext()
        {
            LocalParameters = [slot->RecipeItemId, slot->RecipeCraftType + 8],
            StripSoftHypen = true
        }).ExtractText();
    }

    private void DrawHotbarSlotIcon(RaptureHotbarModule.HotbarSlot slot)
    {
        if (TextureProvider.TryGetFromGameIcon(slot.IconId, out var tex) && tex.TryGetWrap(out var texture, out _))
        {
            ImGui.Image(texture.ImGuiHandle, new Vector2(ImGui.GetTextLineHeight()));
        }
        else
        {
            ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight()));
        }
    }
}
