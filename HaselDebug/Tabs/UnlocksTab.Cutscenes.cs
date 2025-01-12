using System.Collections.Generic;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Graphics;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

[RegisterSingleton<ISubTab<UnlocksTab>>(Duplicate = DuplicateStrategy.Append)]
public unsafe class UnlocksTabCutscenes : DebugTab, ISubTab<UnlocksTab>, IDisposable
{
    private readonly ExcelService _excelService;
    private readonly LanguageProvider _languageProvider;

    private readonly Dictionary<uint, HashSet<(Type SheetType, uint RowId, string Label)>> Cutscenes = [];

    public override string Title => "Cutscenes";
    public override bool DrawInChild => false;

    public UnlocksTabCutscenes(ExcelService excelService, LanguageProvider languageProvider) : base()
    {
        _excelService = excelService;
        _languageProvider = languageProvider;

        _languageProvider.LanguageChanged += OnLanguageChanged;
        Update();
    }

    public void Dispose()
    {
        _languageProvider.LanguageChanged -= OnLanguageChanged;
        GC.SuppressFinalize(this);
    }

    private void OnLanguageChanged(string langCode)
    {
        Update();
    }

    public override void Draw()
    {
        using var table = ImRaii.Table("CutsceneTable", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Seen", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Path", ImGuiTableColumnFlags.WidthFixed, 300);
        ImGui.TableSetupColumn("Used in");
        ImGui.TableSetupScrollFreeze(4, 1);
        ImGui.TableHeadersRow();

        var uiState = UIState.Instance();
        var sheet = _excelService.GetSheet<Cutscene>()!;
        var sheetWI = _excelService.GetSheet<CutsceneWorkIndex>()!;
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

    public void Update()
    {
        foreach (var row in _excelService.GetSheet<CompleteJournal>())
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

        foreach (var row in _excelService.GetSheet<Lumina.Excel.Sheets.InstanceContent>())
        {
            if (row.Cutscene.RowId == 0)
                continue;

            if (!_excelService.TryFindRow<ContentFinderCondition>(cfcrow => cfcrow!.ContentLinkType == 1 && cfcrow.Content.RowId == row.RowId, out var cfc))
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
