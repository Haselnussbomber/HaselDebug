using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Graphics;
using HaselCommon.Services;
using HaselCommon.Services.SeStringEvaluation;
using HaselCommon.Sheets;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using ImGuiNET;
using Lumina.Excel.Sheets;
using UnlockEntry = (string SheetRow, uint IconId, Lumina.Text.ReadOnly.ReadOnlySeString Text);

namespace HaselDebug.Tabs;

public unsafe class UnlocksTabUnlockLinks : DebugTab, ISubTab<UnlocksTab>, IDisposable
{
    private readonly DebugRenderer _debugRenderer;
    private readonly ExcelService _excelService;
    private readonly LanguageProvider _languageProvider;
    private readonly TextService _textService;
    private readonly SeStringEvaluatorService _seStringEvaluatorService;

    private (uint Index, HashSet<UnlockEntry> Unlocks)[] UnlockLinks = [];
    private bool _personalCharaMakeCustomizeOnly = true;

    public override string Title => "Unlock Links";

    public UnlocksTabUnlockLinks(
        DebugRenderer debugRenderer,
        ExcelService excelService,
        LanguageProvider languageProvider,
        TextService textService,
        SeStringEvaluatorService seStringEvaluatorService) : base()
    {
        _debugRenderer = debugRenderer;
        _excelService = excelService;
        _languageProvider = languageProvider;
        _textService = textService;
        _seStringEvaluatorService = seStringEvaluatorService;

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
        var uiState = UIState.Instance();

        if (uiState->PlayerState.IsLoaded == 1 && ImGui.Checkbox("Only show CharaMakeCustomize rows usable on current character", ref _personalCharaMakeCustomizeOnly))
            Update();

        using var table = ImRaii.Table("UnlockLinksTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Unlocked", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Unlocks", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(3, 1);
        ImGui.TableHeadersRow();

        foreach (var (id, entries) in UnlockLinks)
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // Id
            ImGui.TextUnformatted(id.ToString());

            ImGui.TableNextColumn(); // Unlocked
            var isUnlocked = UIState.Instance()->IsUnlockLinkUnlocked(id);
            using (ImRaii.PushColor(ImGuiCol.Text, (uint)(isUnlocked ? Color.Green : Color.Red)))
                ImGui.TextUnformatted(isUnlocked.ToString());

            ImGui.TableNextColumn(); // Unlocks

            using var innertable = ImRaii.Table($"InnerTable{id}", 2, ImGuiTableFlags.NoSavedSettings, new Vector2(-1, -1));
            if (!innertable) return;

            ImGui.TableSetupColumn("Sheet", ImGuiTableColumnFlags.WidthFixed, 320);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);

            foreach (var (SheetRow, IconId, Text) in entries)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn(); // Sheet
                ImGui.TextUnformatted(SheetRow);

                ImGui.TableNextColumn(); // Name
                _debugRenderer.DrawIcon(IconId);
                ImGui.TextUnformatted(Text.ExtractText());
            }
        }
    }

    private void Update()
    {
        var dict = new Dictionary<uint, HashSet<UnlockEntry>>();

        var playerState = PlayerState.Instance();

        var isLoggedIn = playerState->IsLoaded == 1;
        var tribeId = isLoggedIn ? playerState->Tribe : 1;
        var sexId = isLoggedIn ? playerState->Sex : 1;

        foreach (var row in _excelService.GetSheet<GeneralAction>())
        {
            if (row.UnlockLink > 0)
            {
                if (!dict.TryGetValue(row.UnlockLink, out var names))
                    dict.Add(row.UnlockLink, names = []);

                names.Add(($"GeneralAction#{row.RowId}", (uint)row.Icon, row.Name));
            }
        }

        foreach (var row in _excelService.GetSheet<Lumina.Excel.Sheets.Action>())
        {
            if (row.UnlockLink.RowId is > 0 and < 65536)
            {
                if (!dict.TryGetValue(row.UnlockLink.RowId, out var names))
                    dict.Add(row.UnlockLink.RowId, names = []);

                names.Add(($"Action#{row.RowId}", row.Icon, row.Name));
            }
        }

        foreach (var row in _excelService.GetSheet<BuddyAction>())
        {
            if (row.Reward != 0)
            {
                if (!dict.TryGetValue(row.Reward, out var names))
                    dict.Add(row.Reward, names = []);

                names.Add(($"BuddyAction#{row.RowId}", (uint)row.Icon, row.Name));
            }
        }

        foreach (var row in _excelService.GetSheet<CraftAction>())
        {
            if (row.QuestRequirement.RowId is > 0 and < 65536)
            {
                if (!dict.TryGetValue(row.QuestRequirement.RowId, out var names))
                    dict.Add(row.QuestRequirement.RowId, names = []);

                names.Add(($"CraftAction#{row.RowId}", row.Icon, row.Name));
            }
        }

        foreach (var row in _excelService.GetSheet<Emote>())
        {
            if (row.UnlockLink is > 0 and < 65536)
            {
                if (!dict.TryGetValue(row.UnlockLink, out var names))
                    dict.Add(row.UnlockLink, names = []);

                names.Add(($"Emote#{row.RowId}", row.Icon, row.Name));
            }
        }

        foreach (var row in _excelService.GetSheet<Perform>())
        {
            if (row.StopAnimation.RowId > 0)
            {
                if (!dict.TryGetValue(row.StopAnimation.RowId, out var names))
                    dict.Add(row.StopAnimation.RowId, names = []);

                names.Add(($"Perform#{row.RowId}", 0, row.Name));
            }
        }

        // Skipping DescriptionPage which is too complex

        foreach (var row in _excelService.GetSheet<BannerCondition>())
        {
            if (row.UnlockType1 != 2)
                continue;

            if (!dict.TryGetValue(row.UnlockCriteria1[0].RowId, out var names))
                dict.Add(row.UnlockCriteria1[0].RowId, names = []);

            foreach (var bgRow in _excelService.FindRows<BannerBg>(bgRow => bgRow.UnlockCondition.RowId == row.RowId))
            {
                names.Add(($"BannerBg#{bgRow.RowId}", (uint)bgRow.Icon, bgRow.Name));
            }

            foreach (var frameRow in _excelService.FindRows<BannerFrame>(frameRow => frameRow.UnlockCondition.RowId == row.RowId))
            {
                names.Add(($"BannerFrame#{frameRow.RowId}", (uint)frameRow.Icon, frameRow.Name));
            }

            foreach (var decorationRow in _excelService.FindRows<BannerDecoration>(decorationRow => decorationRow.UnlockCondition.RowId == row.RowId))
            {
                names.Add(($"BannerDecoration#{decorationRow.RowId}", (uint)decorationRow.Icon, decorationRow.Name));
            }

            foreach (var facialRow in _excelService.FindRows<BannerFacial>(facialRow => facialRow.UnlockCondition.RowId == row.RowId))
            {
                if (facialRow.Emote.IsValid)
                    names.Add(($"BannerFacial#{facialRow.RowId}", facialRow.Emote.Value.Icon, facialRow.Emote.Value.Name));
            }

            foreach (var timelineRow in _excelService.FindRows<BannerTimeline>(timelineRow => timelineRow.UnlockCondition.RowId == row.RowId))
            {
                names.Add(($"BannerTimeline#{timelineRow.RowId}", (uint)timelineRow.Icon, timelineRow.Name));
            }
        }

        HairMakeTypeCustom hairMakeType = default;
        var hasFoundHairMakeType = isLoggedIn && _excelService.TryFindRow(t => t.HairMakeType.Tribe.RowId == tribeId && t.HairMakeType.Gender == sexId, out hairMakeType);

        foreach (var row in _excelService.GetSheet<CharaMakeCustomize>())
        {
            if (!row.IsPurchasable)
                continue;

            if (isLoggedIn && _personalCharaMakeCustomizeOnly && hasFoundHairMakeType && !hairMakeType.HairStyles.Any(h => h.RowId == row.RowId))
                continue;

            if (!dict.TryGetValue(row.Data, out var names))
                dict.Add(row.Data, names = []);

            var title = string.Empty;

            if (row.HintItem.RowId != 0 && row.HintItem.IsValid)
            {
                title = _textService.GetItemName(row.HintItem.RowId);
            }
            else if (row.Hint.RowId != 0 && row.Hint.IsValid)
            {
                title = _seStringEvaluatorService.EvaluateFromLobby(row.Hint.RowId, new SeStringContext() { LocalParameters = [row.HintItem.RowId] }).ExtractText();
            }

            names.Add(($"CharaMakeCustomize#{row.RowId} (FeatureID: {row.FeatureID})", row.Icon, title));
        }

        foreach (var row in _excelService.GetSheet<MJILandmark>())
        {
            if (row.Unknown0 == 0)
                continue;

            if (!dict.TryGetValue(row.Unknown0, out var names))
                dict.Add(row.Unknown0, names = []);

            names.Add(($"MJILandmark#{row.RowId}", row.Icon, row.Name.Value.Text));
        }

        foreach (var row in _excelService.GetSheet<CSBonusContentType>())
        {
            if (row.Unknown11 == 0)
                continue;

            if (!dict.TryGetValue(row.Unknown11, out var names))
                dict.Add(row.Unknown11, names = []);

            names.Add(($"CSBonusContentType#{row.RowId}", row.ContentType.Value.Icon, row.ContentType.Value.Name));
        }

        foreach (var row in _excelService.GetSheet<NotebookDivision>())
        {
            if (row.QuestUnlock.RowId is > 0 and < 65536)
            {
                if (!dict.TryGetValue(row.QuestUnlock.RowId, out var names))
                    dict.Add(row.QuestUnlock.RowId, names = []);

                names.Add(($"NotebookDivision#{row.RowId}", 0, row.Name));
            }
        }

        foreach (var row in _excelService.GetSheet<Trait>())
        {
            if (row.Quest.RowId is > 0 and < 65536)
            {
                if (!dict.TryGetValue(row.Quest.RowId, out var names))
                    dict.Add(row.Quest.RowId, names = []);

                names.Add(($"Trait#{row.RowId}", (uint)row.Icon, row.Name));
            }
        }

        foreach (var row in _excelService.GetSheet<QuestAcceptAdditionCondition>())
        {
            if (row.Requirement0.RowId is > 0 and < 65536)
            {
                if (!dict.TryGetValue(row.Requirement0.RowId, out var names))
                    dict.Add(row.Requirement0.RowId, names = []);

                names.Add(($"QuestAcceptAdditionCondition#{row.RowId} (Requirement 0)", 0, _textService.GetQuestName(row.RowId)));
            }

            if (row.Requirement1.RowId is > 0 and < 65536)
            {
                if (!dict.TryGetValue(row.Requirement1.RowId, out var names))
                    dict.Add(row.Requirement1.RowId, names = []);

                names.Add(($"QuestAcceptAdditionCondition#{row.RowId} (Requirement 1)", 0, _textService.GetQuestName(row.RowId)));
            }
        }

        UnlockLinks = dict
            .OrderBy(kv => kv.Key)
            .Select(kv => (Id: kv.Key, Unlocks: kv.Value))
            .ToArray();
    }
}
