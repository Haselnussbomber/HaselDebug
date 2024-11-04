using System.Collections.Generic;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Graphics;
using HaselCommon.Services;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

public unsafe partial class UnlocksTab
{
    private readonly Dictionary<uint, HashSet<(Type SheetType, uint RowId, string Label)>> Cutscenes = [];

    public void DrawCutscenes()
    {
        using var tab = ImRaii.TabItem("Cutscenes");
        if (!tab) return;

        using var table = ImRaii.Table("CutsceneTable", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Seen", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Path", ImGuiTableColumnFlags.WidthFixed, 300);
        ImGui.TableSetupColumn("Used in");
        ImGui.TableSetupScrollFreeze(4, 1);
        ImGui.TableHeadersRow();

        var uiState = UIState.Instance();
        var sheet = ExcelService.GetSheet<Cutscene>()!;
        var sheetWI = ExcelService.GetSheet<CutsceneWorkIndex>()!;
        for (var i = 0u; i < sheet.Count; i++)
        {
            var row = sheet.GetRow(i);
            var rowWI = sheetWI.GetRow(i);
            if (i == 0 || rowWI.WorkIndex == 0) continue;

            var isSeen = uiState->IsCutsceneSeen(i);

            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // Index
            ImGui.TextUnformatted(i.ToString());

            ImGui.TableNextColumn(); // Seen
            using (ImRaii.PushColor(ImGuiCol.Text, (uint)(isSeen ? Color.Green : Color.Red)))
                ImGui.TextUnformatted(isSeen.ToString());

            ImGui.TableNextColumn(); // Path
            //DebugUtils.DrawCopyableText(row.Path.ExtractText());
            ImGui.TextUnformatted(row.Path.ExtractText());

            ImGui.TableNextColumn(); // Used in
            if (Cutscenes.TryGetValue(row.RowId, out var c))
            {
                foreach (var (SheetType, RowId, Label) in c)
                    ImGui.TextUnformatted($"{SheetType.Name}#{RowId} ({Label})");
            }
        }
    }

    private void UpdateCutscenes()
    {
        foreach (var row in ExcelService.GetSheet<CompleteJournal>())
        {
            foreach (var cutscene in row.Cutscene)
            {
                if (cutscene.RowId == 0) continue;
                var tuple = (typeof(CompleteJournal), row.RowId, row.Name.ExtractText());

                if (Cutscenes.TryGetValue(cutscene.RowId, out var cEntry))
                    cEntry.Add(tuple);
                else
                    Cutscenes.Add(cutscene.RowId, new([tuple]));
            }
        }

        foreach (var row in ExcelService.GetSheet<Lumina.Excel.Sheets.InstanceContent>())
        {
            if (row.Cutscene.RowId == 0)
                continue;

            if (!ExcelService.TryFindRow<ContentFinderCondition>(cfcrow => cfcrow!.ContentLinkType == 1 && cfcrow.Content.RowId == row.RowId, out var cfc))
                continue;

            var tuple = (typeof(Lumina.Excel.Sheets.InstanceContent), row.RowId, cfc.Name.ExtractText() ?? "?");

            if (Cutscenes.TryGetValue(row.Cutscene.RowId, out var cEntry))
                cEntry.Add(tuple);
            else
                Cutscenes.Add(row.Cutscene.RowId, new([tuple]));
        }

        // PartyContentCutscene, PublicContentCutscene, Warp
    }
}
