using System.Collections.Generic;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Extensions;
using HaselCommon.Services;
using HaselCommon.Utils;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

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
        for (var i = 0u; i < sheet.RowCount; i++)
        {
            var row = sheet.GetRow(i);
            var rowWI = sheetWI.GetRow(i);
            if (i == 0 || row == null || rowWI == null || rowWI.WorkIndex == 0) continue;

            var isSeen = uiState->IsCutsceneSeen(i);

            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // Index
            ImGui.TextUnformatted(i.ToString());

            ImGui.TableNextColumn(); // Seen
            using (ImRaii.PushColor(ImGuiCol.Text, (uint)(isSeen ? Colors.Green : Colors.Red)))
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
                if (cutscene.Row == 0) continue;
                var tuple = (typeof(CompleteJournal), row.RowId, row.Name.ExtractText());

                if (Cutscenes.TryGetValue(cutscene.Row, out var cEntry))
                    cEntry.Add(tuple);
                else
                    Cutscenes.Add(cutscene.Row, new([tuple]));
            }
        }

        foreach (var row in ExcelService.GetSheet<Lumina.Excel.GeneratedSheets.InstanceContent>())
        {
            if (row.Cutscene.Row == 0) continue;

            var cfc = ExcelService.FindRow<ContentFinderCondition>(cfcrow => cfcrow!.ContentLinkType == 1 && cfcrow.Content == row.RowId);

            var tuple = (typeof(Lumina.Excel.GeneratedSheets.InstanceContent), row.RowId, cfc?.Name.ExtractText() ?? "?");

            if (Cutscenes.TryGetValue(row.Cutscene.Row, out var cEntry))
                cEntry.Add(tuple);
            else
                Cutscenes.Add(row.Cutscene.Row, new([tuple]));
        }

        // PartyContentCutscene, PublicContentCutscene, Warp
    }
}
