using System.Collections.Generic;
using System.Globalization;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Graphics;
using HaselCommon.Gui;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

public unsafe partial class UnlocksTab : DebugTab, IDisposable
{
    private readonly Dictionary<uint, uint> AetherCurrentEObjCache = [];
    private readonly Dictionary<uint, uint> EObjLevelCache = [];

    public void DrawAetherCurrents()
    {
        using var tab = ImRaii.TabItem("Aether Currents");
        if (!tab) return;

        using var table = ImRaii.Table("CurrentsTabTable", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("RowId", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Completed", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Zone", ImGuiTableColumnFlags.WidthFixed, 220);
        ImGui.TableSetupColumn("Location", ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        foreach (var row in ExcelService.GetSheet<AetherCurrentCompFlgSet>())
        {
            var currentNumber = 1;
            var lastWasQuest = false;
            foreach (var aetherCurrent in row.AetherCurrents)
            {
                if (!aetherCurrent.IsValid) continue;

                var isQuest = aetherCurrent.Value.Quest.IsValid;
                if (isQuest)
                {
                    lastWasQuest = true;
                }
                else if (lastWasQuest)
                {
                    currentNumber = 1;
                    lastWasQuest = false;
                }

                ImGui.TableNextRow();
                ImGui.TableNextColumn(); // RowId
                ImGui.TextUnformatted(aetherCurrent.RowId.ToString());

                ImGui.TableNextColumn(); // Completed
                var isComplete = PlayerState.Instance()->IsAetherCurrentUnlocked(aetherCurrent.RowId);
                using (ImRaii.PushColor(ImGuiCol.Text, (uint)(isComplete ? Color.Green : Color.Red)))
                    ImGui.TextUnformatted(isComplete.ToString());

                ImGui.TableNextColumn(); // Name
                ImGui.TextUnformatted(row.Territory.Value.Map.Value.PlaceName.Value.Name.ExtractText());

                ImGui.TableNextColumn(); // Location

                var clicked = ImGui.Selectable($"###AetherCurrentSelectable_{aetherCurrent.RowId}");

                if (ImGui.IsItemHovered())
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                ImGui.SameLine(0, 0);

                if (isQuest)
                    DrawQuest(currentNumber++, aetherCurrent);
                else
                    DrawEObject(currentNumber++, aetherCurrent);

                if (!clicked)
                    continue;

                if (isQuest)
                {
                    if (!TryGetFixedQuest(aetherCurrent, out var quest))
                        continue;

                    MapService.OpenMap(quest.IssuerLocation.Value);
                }
                else
                {
                    if (!TryGetEObj(aetherCurrent, out var eobj))
                        continue;

                    if (!TryGetLevel(eobj, out var level))
                        continue;

                    MapService.OpenMap(level);
                }
            }
        }
    }

    private void DrawQuest(int index, RowRef<AetherCurrent> aetherCurrent)
    {
        if (!TryGetFixedQuest(aetherCurrent, out var quest))
            return;

        DebugRenderer.DrawIcon(quest.EventIconType.Value!.MapIconAvailable + 1, canCopy: false);
        ImGuiUtils.TextUnformattedColored(Color.Yellow, $"[#{index}] {TextService.GetQuestName(quest.RowId)}");
        ImGui.SameLine();
        ImGui.TextUnformatted($"{GetHumanReadableCoords(quest.IssuerLocation.Value)} | {TextService.GetENpcResidentName(quest.IssuerStart.RowId)}");
    }

    private void DrawEObject(int index, RowRef<AetherCurrent> aetherCurrent)
    {
        if (!TryGetEObj(aetherCurrent, out var eobj))
            return;

        if (!TryGetLevel(eobj, out var level))
            return;

        DebugRenderer.DrawIcon(60033, canCopy: false);
        ImGuiUtils.TextUnformattedColored(Color.Green, $"[#{index}] {TextService.GetEObjName(eobj.RowId)}");
        ImGui.SameLine();
        ImGui.TextUnformatted(GetHumanReadableCoords(level));
    }

    private bool TryGetFixedQuest(RowRef<AetherCurrent> aetherCurrent, out Quest quest)
    {
        var questId = aetherCurrent.Value.Quest.RowId;

        // Some AetherCurrents link to the wrong Quest.
        // See https://github.com/Haselnussbomber/HaselTweaks/issues/15

        // The Dravanian Forelands (CompFlgSet#2)
        if (aetherCurrent.RowId == 2818065 && questId == 67328) // Natural Repellent
            questId = 67326; // Stolen Munitions
        else if (aetherCurrent.RowId == 2818066 && questId == 67334) // Chocobo's Last Stand
            questId = 67333; // The Hunter Becomes the Kweh

        // The Churning Mists (CompFlgSet#4)
        else if (aetherCurrent.RowId == 2818096 && questId == 67365) // The Unceasing Gardener
            questId = 67364; // Hide Your Moogles

        // The Sea of Clouds (CompFlgSet#5)
        else if (aetherCurrent.RowId == 2818110 && questId == 67437) // Search and Rescue
            questId = 67410; // Honoring the Past

        // Thavnair (CompFlgSet#21)
        else if (aetherCurrent.RowId == 2818328 && questId == 70030) // Curing What Ails
            questId = 69793; // In Agama's Footsteps

        if (!ExcelService.TryGetRow<Quest>(questId, out quest) || quest.IssuerLocation.RowId == 0)
            return false;

        return quest.IssuerLocation.IsValid;
    }

    private bool TryGetEObj(RowRef<AetherCurrent> aetherCurrent, out EObj eobj)
    {
        if (AetherCurrentEObjCache.TryGetValue(aetherCurrent.RowId, out var eobjRowId))
        {
            if (!ExcelService.TryGetRow<EObj>(eobjRowId, out eobj))
                return false;
        }
        else
        {
            if (!ExcelService.TryFindRow<EObj>(row => row.Data == aetherCurrent.RowId, out eobj))
                return false;

            AetherCurrentEObjCache.Add(aetherCurrent.RowId, eobj.RowId);
        }

        return true;
    }

    private bool TryGetLevel(EObj eobj, out Level level)
    {
        if (EObjLevelCache.TryGetValue(eobj.RowId, out var levelRowId))
        {
            if (!ExcelService.TryGetRow<Level>(levelRowId, out level))
                return false;
        }
        else
        {
            if (!ExcelService.TryFindRow<Level>(row => row.Object.RowId == eobj.RowId, out level))
                return false;

            EObjLevelCache.Add(eobj.RowId, level.RowId);
        }

        return true;
    }

    private string GetHumanReadableCoords(Level level)
    {
        var coords = MapService.GetCoords(level);
        var x = coords.X.ToString("0.0", CultureInfo.InvariantCulture);
        var y = coords.Y.ToString("0.0", CultureInfo.InvariantCulture);
        return string.Format("X: {0}, Y: {1}", x, y);
    }
}
