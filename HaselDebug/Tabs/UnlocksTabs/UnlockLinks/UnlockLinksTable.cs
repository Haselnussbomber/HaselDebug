using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Game.Enums;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselCommon.Services.SeStringEvaluation;
using HaselCommon.Sheets;
using HaselDebug.Services;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.UnlockLinks;

[RegisterSingleton]
public unsafe class UnlockLinksTable : Table<UnlockLinkEntry>
{
    internal readonly ExcelService _excelService;
    private readonly SeStringEvaluatorService _seStringEvaluator;
    private readonly TextService _textService;

    public bool PersonalCharaMakeCustomizeOnly = true;

    public UnlockLinksTable(
        ExcelService excelService,
        DebugRenderer debugRenderer,
        SeStringEvaluatorService seStringEvaluator,
        TextService textService,
        LanguageProvider languageProvider) : base("UnlockLinksTable", languageProvider)
    {
        _excelService = excelService;
        _seStringEvaluator = seStringEvaluator;
        _textService = textService;

        Columns = [
            new IndexColumn() {
                Label = "Index",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 60,
            },
            new UnlockedColumn() {
                Label = "Unlocked",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 75,
            },
            new UnlocksColumn(debugRenderer) {
                Label = "Unlocks",
            }
        ];

        LineHeight = 0;
    }

    public override void LoadRows()
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

                names.Add(new($"GeneralAction#{row.RowId}", (uint)row.Icon, row.Name.ExtractText()));
            }
        }

        foreach (var row in _excelService.GetSheet<Lumina.Excel.Sheets.Action>())
        {
            if (row.UnlockLink.RowId is > 0 and < 65536)
            {
                if (!dict.TryGetValue(row.UnlockLink.RowId, out var names))
                    dict.Add(row.UnlockLink.RowId, names = []);

                names.Add(new($"Action#{row.RowId}", row.Icon, row.Name.ExtractText()));
            }
        }

        foreach (var row in _excelService.GetSheet<BuddyAction>())
        {
            if (row.Reward != 0)
            {
                if (!dict.TryGetValue(row.Reward, out var names))
                    dict.Add(row.Reward, names = []);

                names.Add(new($"BuddyAction#{row.RowId}", (uint)row.Icon, row.Name.ExtractText()));
            }
        }

        foreach (var row in _excelService.GetSheet<CraftAction>())
        {
            if (row.QuestRequirement.RowId is > 0 and < 65536)
            {
                if (!dict.TryGetValue(row.QuestRequirement.RowId, out var names))
                    dict.Add(row.QuestRequirement.RowId, names = []);

                names.Add(new($"CraftAction#{row.RowId}", row.Icon, row.Name.ExtractText()));
            }
        }

        foreach (var row in _excelService.GetSheet<Emote>())
        {
            if (row.UnlockLink is > 0 and < 65536)
            {
                if (!dict.TryGetValue(row.UnlockLink, out var names))
                    dict.Add(row.UnlockLink, names = []);

                names.Add(new($"Emote#{row.RowId}", row.Icon, row.Name.ExtractText()));
            }
        }

        foreach (var row in _excelService.GetSheet<Perform>())
        {
            if (row.StopAnimation.RowId > 0)
            {
                if (!dict.TryGetValue(row.StopAnimation.RowId, out var names))
                    dict.Add(row.StopAnimation.RowId, names = []);

                names.Add(new($"Perform#{row.RowId}", 0, row.Name.ExtractText()));
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
                names.Add(new($"BannerBg#{bgRow.RowId}", (uint)bgRow.Icon, bgRow.Name.ExtractText()));
            }

            foreach (var frameRow in _excelService.FindRows<BannerFrame>(frameRow => frameRow.UnlockCondition.RowId == row.RowId))
            {
                names.Add(new($"BannerFrame#{frameRow.RowId}", (uint)frameRow.Icon, frameRow.Name.ExtractText()));
            }

            foreach (var decorationRow in _excelService.FindRows<BannerDecoration>(decorationRow => decorationRow.UnlockCondition.RowId == row.RowId))
            {
                names.Add(new($"BannerDecoration#{decorationRow.RowId}", (uint)decorationRow.Icon, decorationRow.Name.ExtractText()));
            }

            foreach (var facialRow in _excelService.FindRows<BannerFacial>(facialRow => facialRow.UnlockCondition.RowId == row.RowId))
            {
                if (facialRow.Emote.IsValid)
                    names.Add(new($"BannerFacial#{facialRow.RowId}", facialRow.Emote.Value.Icon, facialRow.Emote.Value.Name.ExtractText()));
            }

            foreach (var timelineRow in _excelService.FindRows<BannerTimeline>(timelineRow => timelineRow.UnlockCondition.RowId == row.RowId))
            {
                names.Add(new($"BannerTimeline#{timelineRow.RowId}", (uint)timelineRow.Icon, timelineRow.Name.ExtractText()));
            }
        }

        // TODO: reload table when logging out/in
        HairMakeTypeCustom hairMakeType = default;
        var hasFoundHairMakeType = isLoggedIn && _excelService.TryFindRow(t => t.HairMakeType.Tribe.RowId == tribeId && t.HairMakeType.Gender == sexId, out hairMakeType);

        foreach (var row in _excelService.GetSheet<CharaMakeCustomize>())
        {
            if (!row.IsPurchasable)
                continue;

            if (isLoggedIn &&
                PersonalCharaMakeCustomizeOnly &&
                hasFoundHairMakeType &&
                row.HintItem.RowId != 0 &&
                row.HintItem.IsValid &&
                row.HintItem.Value.ItemAction.RowId != 0 &&
                row.HintItem.Value.ItemAction.IsValid &&
                row.HintItem.Value.ItemAction.Value.Type == (uint)ItemActionType.UnlockLink &&
                row.HintItem.Value.ItemAction.Value.Data[0] == row.Data &&
                row.HintItem.Value.ItemAction.Value.Data[1] == 4659 && // LogMessage id
                !hairMakeType.HairStyles.Any(h => h.RowId == row.RowId))
            {
                continue;
            }

            if (!dict.TryGetValue(row.Data, out var names))
                dict.Add(row.Data, names = []);

            var title = string.Empty;

            if (row.HintItem.RowId != 0 && row.HintItem.IsValid)
            {
                title = _textService.GetItemName(row.HintItem.RowId);
            }
            else if (row.Hint.RowId != 0 && row.Hint.IsValid)
            {
                title = _seStringEvaluator.EvaluateFromLobby(row.Hint.RowId, new SeStringContext() { LocalParameters = [row.HintItem.RowId] }).ExtractText();
            }

            names.Add(new($"CharaMakeCustomize#{row.RowId} (FeatureID: {row.FeatureID})", row.Icon, title));
        }

        foreach (var row in _excelService.GetSheet<MJILandmark>())
        {
            if (row.Unknown0 == 0)
                continue;

            if (!dict.TryGetValue(row.Unknown0, out var names))
                dict.Add(row.Unknown0, names = []);

            names.Add(new($"MJILandmark#{row.RowId}", row.Icon, row.Name.Value.Text.ExtractText()));
        }

        foreach (var row in _excelService.GetSheet<CSBonusContentType>())
        {
            if (row.Unknown11 == 0)
                continue;

            if (!dict.TryGetValue(row.Unknown11, out var names))
                dict.Add(row.Unknown11, names = []);

            names.Add(new($"CSBonusContentType#{row.RowId}", row.ContentType.Value.Icon, row.ContentType.Value.Name.ExtractText()));
        }

        foreach (var row in _excelService.GetSheet<NotebookDivision>())
        {
            if (row.QuestUnlock.RowId is > 0 and < 65536)
            {
                if (!dict.TryGetValue(row.QuestUnlock.RowId, out var names))
                    dict.Add(row.QuestUnlock.RowId, names = []);

                names.Add(new($"NotebookDivision#{row.RowId}", 0, row.Name.ExtractText()));
            }
        }

        foreach (var row in _excelService.GetSheet<Trait>())
        {
            if (row.Quest.RowId is > 0 and < 65536)
            {
                if (!dict.TryGetValue(row.Quest.RowId, out var names))
                    dict.Add(row.Quest.RowId, names = []);

                names.Add(new($"Trait#{row.RowId}", (uint)row.Icon, row.Name.ExtractText()));
            }
        }

        foreach (var row in _excelService.GetSheet<QuestAcceptAdditionCondition>())
        {
            if (row.Requirement0.RowId is > 0 and < 65536)
            {
                if (!dict.TryGetValue(row.Requirement0.RowId, out var names))
                    dict.Add(row.Requirement0.RowId, names = []);

                names.Add(new($"QuestAcceptAdditionCondition#{row.RowId} (Requirement 0)", 0, _textService.GetQuestName(row.RowId)));
            }

            if (row.Requirement1.RowId is > 0 and < 65536)
            {
                if (!dict.TryGetValue(row.Requirement1.RowId, out var names))
                    dict.Add(row.Requirement1.RowId, names = []);

                names.Add(new($"QuestAcceptAdditionCondition#{row.RowId} (Requirement 1)", 0, _textService.GetQuestName(row.RowId)));
            }
        }

        Rows = dict
            .Select(kv => new UnlockLinkEntry(kv.Key, [.. kv.Value]))
            .ToList();
    }

    private class IndexColumn : ColumnNumber<UnlockLinkEntry>
    {
        public override string ToName(UnlockLinkEntry entry)
            => entry.Index.ToString();

        public override int ToValue(UnlockLinkEntry entry)
            => (int)entry.Index;
    }

    private class UnlockedColumn : ColumnBool<UnlockLinkEntry>
    {
        public override unsafe bool ToBool(UnlockLinkEntry entry)
            => UIState.Instance()->IsUnlockLinkUnlocked((ushort)entry.Index);
    }

    private class UnlocksColumn(DebugRenderer debugRenderer) : ColumnString<UnlockLinkEntry>
    {
        public override string ToName(UnlockLinkEntry entry)
            => string.Join(' ', entry.Unlocks.Select(unlock => unlock.SheetRow + ' ' + unlock.Text));

        public override int Compare(UnlockLinkEntry lhs, UnlockLinkEntry rhs)
        {
            static string toName(UnlockLinkEntry entry) => string.Join(' ', entry.Unlocks.Select(unlock => unlock.Text));
            return toName(lhs).CompareTo(toName(rhs));
        }

        public override unsafe void DrawColumn(UnlockLinkEntry entry)
        {
            using var innertable = ImRaii.Table($"InnerTable{entry.Index}", 2, ImGuiTableFlags.NoSavedSettings, new Vector2(-1, -1));
            if (!innertable) return;

            ImGui.TableSetupColumn("Sheet", ImGuiTableColumnFlags.WidthFixed, 320);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);

            foreach (var (SheetRow, IconId, Text) in entry.Unlocks)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn(); // Sheet
                ImGui.TextUnformatted(SheetRow);

                ImGui.TableNextColumn(); // Name
                debugRenderer.DrawIcon(IconId);
                ImGui.TextUnformatted(Text);
            }
        }
    }
}
