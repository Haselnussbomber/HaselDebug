using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Game.Enums;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselCommon.Sheets;
using HaselDebug.Tabs.UnlocksTabs.UnlockLinks.Columns;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.UnlockLinks;

[RegisterSingleton, AutoConstruct]
public unsafe partial class UnlockLinksTable : Table<UnlockLinkEntry>, IDisposable
{
    internal readonly ExcelService _excelService;
    private readonly SeStringEvaluator _seStringEvaluator;
    private readonly TextService _textService;
    private readonly IClientState _clientState;

    private readonly IndexColumn _indexColumn;
    private readonly UnlockedColumn _unlockedColumn;
    private readonly UnlocksColumn _unlocksColumn;
    private readonly UnlocksNameColumn _unlocksNameColumn;

    [AutoPostConstruct]
    public void Initialize()
    {
        Columns = [
            _indexColumn,
            _unlockedColumn,
            _unlocksColumn,
            _unlocksNameColumn,
        ];

        LineHeight = 0;

        _clientState.Login += OnLogin;
        _clientState.Logout += OnLogout;
    }

    public override void Dispose()
    {
        _clientState.Logout -= OnLogout;
        _clientState.Login -= OnLogin;
        base.Dispose();
    }

    private void OnLogin()
    {
        LoadRows();
    }

    private void OnLogout(int type, int code)
    {
        LoadRows();
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

                names.Add(new UnlockEntry()
                {
                    RowType = typeof(GeneralAction),
                    RowId = row.RowId,
                    IconId = (uint)row.Icon,
                    Label = row.Name.ExtractText(),
                    Category = "General Action"
                });
            }
        }

        foreach (var row in _excelService.GetSheet<Lumina.Excel.Sheets.Action>())
        {
            if (row.UnlockLink.RowId is > 0 and < 65536)
            {
                if (!dict.TryGetValue(row.UnlockLink.RowId, out var names))
                    dict.Add(row.UnlockLink.RowId, names = []);

                names.Add(new UnlockEntry()
                {
                    RowType = typeof(Lumina.Excel.Sheets.Action),
                    RowId = row.RowId,
                    IconId = row.Icon,
                    Label = row.Name.ExtractText(),
                    Category = _excelService.TryFindRow<AozAction>(aozRow => aozRow.Action.RowId == row.RowId, out var aozAction) ? $"Blue Mage Action {aozAction.RowId}" : "Action"
                });
            }
        }

        foreach (var row in _excelService.GetSheet<BuddyAction>())
        {
            if (row.UnlockLink != 0)
            {
                if (!dict.TryGetValue(row.UnlockLink, out var names))
                    dict.Add(row.UnlockLink, names = []);

                names.Add(new UnlockEntry()
                {
                    RowType = typeof(BuddyAction),
                    RowId = row.RowId,
                    IconId = (uint)row.Icon,
                    Label = row.Name.ExtractText(),
                    Category = "Pet Action"
                });
            }
        }

        foreach (var row in _excelService.GetSheet<CraftAction>())
        {
            if (row.QuestRequirement.RowId is > 0 and < 65536)
            {
                if (!dict.TryGetValue(row.QuestRequirement.RowId, out var names))
                    dict.Add(row.QuestRequirement.RowId, names = []);

                names.Add(new UnlockEntry()
                {
                    RowType = typeof(CraftAction),
                    RowId = row.RowId,
                    IconId = row.Icon,
                    Label = row.Name.ExtractText(),
                    Category = "Crafting Action"
                });
            }
        }

        foreach (var row in _excelService.GetSheet<Emote>())
        {
            if (row.UnlockLink is > 0 and < 65536)
            {
                if (!dict.TryGetValue(row.UnlockLink, out var names))
                    dict.Add(row.UnlockLink, names = []);

                names.Add(new UnlockEntry()
                {
                    RowType = typeof(Emote),
                    RowId = row.RowId,
                    IconId = row.Icon,
                    Label = row.Name.ExtractText()
                });
            }
        }

        foreach (var row in _excelService.GetSheet<Perform>())
        {
            if (row.UnlockLink > 0)
            {
                if (!dict.TryGetValue((uint)row.UnlockLink, out var names))
                    dict.Add((uint)row.UnlockLink, names = []);

                names.Add(new UnlockEntry()
                {
                    RowType = typeof(Perform),
                    RowId = row.RowId,
                    Label = row.Name.ExtractText()
                });
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
                names.Add(new UnlockEntry()
                {
                    RowType = typeof(BannerBg),
                    RowId = bgRow.RowId,
                    IconId = (uint)bgRow.Icon,
                    Label = bgRow.Name.ExtractText(),
                    Category = _textService.GetAddonText(14687)
                });
            }

            foreach (var frameRow in _excelService.FindRows<BannerFrame>(frameRow => frameRow.UnlockCondition.RowId == row.RowId))
            {
                names.Add(new UnlockEntry()
                {
                    RowType = typeof(BannerFrame),
                    RowId = frameRow.RowId,
                    IconId = (uint)frameRow.Icon,
                    Label = frameRow.Name.ExtractText(),
                    Category = _textService.GetAddonText(14688)
                });
            }

            foreach (var decorationRow in _excelService.FindRows<BannerDecoration>(decorationRow => decorationRow.UnlockCondition.RowId == row.RowId))
            {
                names.Add(new UnlockEntry()
                {
                    RowType = typeof(BannerDecoration),
                    RowId = decorationRow.RowId,
                    IconId = (uint)decorationRow.Icon,
                    Label = decorationRow.Name.ExtractText(),
                    Category = _textService.GetAddonText(14689)
                });
            }

            foreach (var facialRow in _excelService.FindRows<BannerFacial>(facialRow => facialRow.UnlockCondition.RowId == row.RowId))
            {
                if (facialRow.Emote.IsValid)
                {
                    names.Add(new UnlockEntry()
                    {
                        RowType = typeof(BannerFacial),
                        RowId = facialRow.RowId,
                        IconId = facialRow.Emote.Value.Icon,
                        Label = facialRow.Emote.Value.Name.ExtractText(),
                        Category = _textService.GetAddonText(14691)
                    });
                }
            }

            foreach (var timelineRow in _excelService.FindRows<BannerTimeline>(timelineRow => timelineRow.UnlockCondition.RowId == row.RowId))
            {
                names.Add(new UnlockEntry()
                {
                    RowType = typeof(BannerTimeline),
                    RowId = timelineRow.RowId,
                    IconId = (uint)timelineRow.Icon,
                    Label = timelineRow.Name.ExtractText(),
                    Category = _textService.GetAddonText(14690)
                });
            }
        }

        CustomHairMakeType hairMakeType = default;
        var hasFoundHairMakeType = isLoggedIn && _excelService.TryFindRow(t => t.Tribe.RowId == tribeId && t.Gender == sexId, out hairMakeType);

        foreach (var row in _excelService.GetSheet<CharaMakeCustomize>())
        {
            if (!row.IsPurchasable)
                continue;

            var description = string.Empty;

            if (isLoggedIn && hasFoundHairMakeType)
            {
                if (row.HintItem.RowId != 0 &&
                    row.HintItem.IsValid &&
                    row.HintItem.Value.ItemAction.RowId != 0 &&
                    row.HintItem.Value.ItemAction.IsValid &&
                    row.HintItem.Value.ItemAction.Value.Type == (uint)ItemActionType.UnlockLink &&
                    row.HintItem.Value.ItemAction.Value.Data[0] == row.UnlockLink)
                {
                    // Hairstyles
                    if (row.HintItem.Value.ItemAction.Value.Data[1] == 4659 && // LogMessage id
                        !hairMakeType.CharaMakeStruct[0].SubMenuParam.Any(id => id == row.RowId))
                    {
                        continue;
                    }
                    else
                    {
                        description = _textService.GetLobbyText(234);
                    }

                    // Face Paint
                    if (row.HintItem.Value.ItemAction.Value.Data[1] == 9390 && // LogMessage id
                        !hairMakeType.CharaMakeStruct[7].SubMenuParam.Any(id => id == row.RowId))
                    {
                        continue;
                    }
                    else
                    {
                        description = _textService.GetLobbyText(249);
                    }
                }

                // Hairstyle available in preparation for the Ceremony of Eternal Bonding.
                if (row.Hint.RowId == 641)
                {
                    if (!hairMakeType.CharaMakeStruct[0].SubMenuParam.Any(id => id == row.RowId))
                    {
                        continue;
                    }
                    else
                    {
                        description = _textService.GetLobbyText(234);
                    }
                }
            }

            if (!dict.TryGetValue(row.UnlockLink, out var names))
                dict.Add(row.UnlockLink, names = []);

            var title = string.Empty;

            if (row.HintItem.RowId != 0 && row.HintItem.IsValid)
            {
                title = _textService.GetItemName(row.HintItem.RowId);
            }
            else if (row.Hint.RowId != 0 && row.Hint.IsValid)
            {
                title = _seStringEvaluator.EvaluateFromLobby(row.Hint.RowId, [row.HintItem.RowId]).ExtractText();
            }

            names.Add(new UnlockEntry()
            {
                RowType = typeof(CharaMakeCustomize),
                RowId = row.RowId,
                ExtraSheetText = $" (FeatureID: {row.FeatureID})",
                IconId = row.Icon,
                Label = title,
                Category = description
            });
        }

        foreach (var row in _excelService.GetSheet<MJILandmark>())
        {
            if (row.UnlockLink == 0)
                continue;

            if (!dict.TryGetValue(row.UnlockLink, out var names))
                dict.Add(row.UnlockLink, names = []);

            names.Add(new UnlockEntry()
            {
                RowType = typeof(MJILandmark),
                RowId = row.RowId,
                IconId = row.Icon,
                Label = row.Name.Value.Text.ExtractText(),
                Category = _textService.GetAddonText(14269)
            });
        }

        foreach (var row in _excelService.GetSheet<CSBonusContentType>())
        {
            if (row.UnlockLink == 0)
                continue;

            if (!dict.TryGetValue(row.UnlockLink, out var names))
                dict.Add(row.UnlockLink, names = []);

            names.Add(new UnlockEntry()
            {
                RowType = typeof(CSBonusContentType),
                RowId = row.RowId,
                IconId = row.ContentType.Value.Icon,
                Label = row.ContentType.Value.Name.ExtractText()
            });
        }

        foreach (var row in _excelService.GetSheet<NotebookDivision>())
        {
            if (row.QuestUnlock.RowId is > 0 and < 65536)
            {
                if (!dict.TryGetValue(row.QuestUnlock.RowId, out var names))
                    dict.Add(row.QuestUnlock.RowId, names = []);

                names.Add(new UnlockEntry()
                {
                    RowType = typeof(NotebookDivision),
                    RowId = row.RowId,
                    Label = row.Name.ExtractText()
                });
            }
        }

        foreach (var row in _excelService.GetSheet<Trait>())
        {
            if (row.Quest.RowId is > 0 and < 65536)
            {
                if (!dict.TryGetValue(row.Quest.RowId, out var names))
                    dict.Add(row.Quest.RowId, names = []);

                names.Add(new UnlockEntry()
                {
                    RowType = typeof(Trait),
                    RowId = row.RowId,
                    IconId = (uint)row.Icon,
                    Label = row.Name.ExtractText(),
                    Category = _textService.GetAddonText(102478)
                });
            }
        }

        foreach (var row in _excelService.GetSheet<QuestAcceptAdditionCondition>())
        {
            if (row.Requirement0.RowId is > 0 and < 65536)
            {
                if (!dict.TryGetValue(row.Requirement0.RowId, out var names))
                    dict.Add(row.Requirement0.RowId, names = []);

                names.Add(new UnlockEntry()
                {
                    RowType = typeof(QuestAcceptAdditionCondition),
                    RowId = row.RowId,
                    ExtraSheetText = " (Requirement 0)",
                    Label = _textService.GetQuestName(row.RowId)
                });
            }

            if (row.Requirement1.RowId is > 0 and < 65536)
            {
                if (!dict.TryGetValue(row.Requirement1.RowId, out var names))
                    dict.Add(row.Requirement1.RowId, names = []);

                names.Add(new UnlockEntry()
                {
                    RowType = typeof(QuestAcceptAdditionCondition),
                    RowId = row.RowId,
                    ExtraSheetText = " (Requirement 1)",
                    Label = _textService.GetQuestName(row.RowId)
                });
            }
        }

        foreach (var row in _excelService.GetSheet<Item>())
        {
            if (row.ItemAction.RowId == 0 || !row.ItemAction.IsValid || row.ItemAction.Value.Type != (uint)ItemActionType.UnlockLink)
                continue;

            if (!dict.TryGetValue(row.ItemAction.Value.Data[0], out var names))
                dict.Add(row.ItemAction.Value.Data[0], names = []);

            names.Add(new UnlockEntry()
            {
                RowType = typeof(Item),
                RowId = row.RowId,
                IconId = row.Icon,
                Label = _textService.GetItemName(row.RowId)
            });
        }

        Rows = dict
            .Select(kv => new UnlockLinkEntry(kv.Key, [.. kv.Value]))
            .ToList();
    }
}
