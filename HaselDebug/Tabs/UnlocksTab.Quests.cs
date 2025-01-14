using System.Linq;
using System.Numerics;
using Dalamud.Game.Text;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Extensions.Sheets;
using HaselCommon.Extensions.Strings;
using HaselCommon.Graphics;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Extensions;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using HaselDebug.Windows.ItemTooltips;
using ImGuiNET;
using Lumina.Excel.Sheets;
using InstanceContent = Lumina.Excel.Sheets.InstanceContent;

namespace HaselDebug.Tabs;

[RegisterSingleton<ISubTab<UnlocksTab>>(Duplicate = DuplicateStrategy.Append)]
public unsafe class UnlocksTabQuests : DebugTab, ISubTab<UnlocksTab>, IDisposable
{
    private readonly DebugRenderer _debugRenderer;
    private readonly ExcelService _excelService;
    private readonly ImGuiContextMenuService _imGuiContextMenu;
    private readonly MapService _mapService;
    private readonly ITextureProvider _textureProvider;
    private readonly TextService _textService;
    private readonly LanguageProvider _languageProvider;
    private readonly UnlocksTabUtils _unlocksTabUtils;
    private readonly TextureService _textureService;
    private readonly TripleTriadNumberFontManager _tripleTriadNumberFontManager;
    private Quest[] _quests;
    private TripleTriadCardTooltip? _tripleTriadCardTooltip;

    public UnlocksTabQuests(
        DebugRenderer DebugRenderer,
        ExcelService ExcelService,
        ImGuiContextMenuService ImGuiContextMenu,
        MapService MapService,
        ITextureProvider TextureProvider,
        TextService TextService,
        LanguageProvider LanguageProvider,
        UnlocksTabUtils UnlocksTabUtils,
        TextureService TextureService,
        TripleTriadNumberFontManager tripleTriadNumberFontManager)
    {
        _debugRenderer = DebugRenderer;
        _excelService = ExcelService;
        _imGuiContextMenu = ImGuiContextMenu;
        _mapService = MapService;
        _textureProvider = TextureProvider;
        _textService = TextService;
        _languageProvider = LanguageProvider;
        _unlocksTabUtils = UnlocksTabUtils;
        _textureService = TextureService;
        _tripleTriadNumberFontManager = tripleTriadNumberFontManager;

        _languageProvider.LanguageChanged += OnLanguageChanged;

        _quests = _excelService.GetSheet<Quest>().Skip(1).ToArray();
    }

    public void Dispose()
    {
        _languageProvider.LanguageChanged -= OnLanguageChanged;
        _tripleTriadCardTooltip?.Dispose();
        _tripleTriadCardTooltip = null;
        GC.SuppressFinalize(this);
    }

    private void OnLanguageChanged(string langCode)
    {
        _quests = _excelService.GetSheet<Quest>().Skip(1).ToArray();
    }

    public override string Title => "Quests";
    public override bool DrawInChild => false;

    public override void Draw()
    {
        using var table = ImRaii.Table("QuestsTable7", 10, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("RowId", ImGuiTableColumnFlags.WidthFixed, 40 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("QuestId", ImGuiTableColumnFlags.WidthFixed, 40 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Completed", ImGuiTableColumnFlags.WidthFixed, 60 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Name");
        ImGui.TableSetupColumn("Currency Reward", ImGuiTableColumnFlags.WidthFixed, 70 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Emote Reward", ImGuiTableColumnFlags.WidthFixed, 40 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Instance Unlock", ImGuiTableColumnFlags.WidthFixed, 40 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Rewards");
        ImGui.TableSetupColumn("Optional Rewards");
        ImGui.TableSetupColumn("Gil Reward", ImGuiTableColumnFlags.WidthFixed, 90 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        var clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
        var count = _quests.Length;
        clipper.Begin(count, ImGui.GetTextLineHeightWithSpacing());

        while (clipper.Step())
        {
            for (var rowIndex = clipper.DisplayStart; rowIndex < clipper.DisplayEnd; rowIndex++)
            {
                if (rowIndex >= count)
                    return;

                DrawRow(_quests[rowIndex]);
            }
        }

        clipper.End();
        clipper.Destroy();
    }

    private void DrawRow(Quest row)
    {
        var isLoggedIn = AgentLobby.Instance()->IsLoggedIn;

        ImGui.TableNextRow();

        ImGui.TableNextColumn(); // RowId
        _debugRenderer.DrawCopyableText(row.RowId.ToString());

        ImGui.TableNextColumn(); // Questid
        _debugRenderer.DrawCopyableText((row.RowId - 0x10000).ToString());

        ImGui.TableNextColumn(); // Completed
        var isCompleted = isLoggedIn && UIState.Instance()->IsUnlockLinkUnlockedOrQuestCompleted((ushort)row.RowId + 0x10000u);
        using (ImRaii.PushColor(ImGuiCol.Text, (uint)(isCompleted ? Color.Green : Color.Red)))
            ImGui.TextUnformatted(isCompleted.ToString());

        ImGui.TableNextColumn(); // Name
        _debugRenderer.DrawIcon(row.Icon);

        if (ImGui.Selectable(row.Name.ExtractText()) && isLoggedIn && row.IssuerLocation.IsValid)
        {
            _mapService.OpenMap(row.IssuerLocation.Value);
        }

        _imGuiContextMenu.Draw($"Quest{row.RowId}ContextMenu", builder =>
        {
            builder.AddCopyName(_textService, row.Name.ExtractText());

            builder.Add(new ImGuiContextMenuEntry()
            {
                Visible = isLoggedIn && row.IssuerLocation.IsValid,
                Label = _textService.GetAddonText(8506), // "Open Map"
                LoseFocusOnClick = true,
                ClickCallback = () =>
                {
                    _mapService.OpenMap(row.IssuerLocation.Value);
                }
            });

            builder.Add(new ImGuiContextMenuEntry()
            {
                Visible = isLoggedIn && row.IssuerLocation.IsValid,
                Label = _textService.Translate("ContextMenu.OpenInArchive"),
                LoseFocusOnClick = true,
                ClickCallback = () =>
                {
                    AgentQuestJournal.Instance()->OpenForQuest(row.RowId, 1);
                }
            });

            builder.AddOpenOnGarlandTools("quest", row.RowId);
        });

        ImGui.TableNextColumn(); // Currency Reward

        if (row.CurrencyReward.RowId > 0 && row.CurrencyRewardCount != 0 && row.CurrencyReward.Value.ItemUICategory.RowId != 0)
        {
            using var id = ImRaii.PushId($"Quest{row.Id}CurrencyReward");
            ImGui.TextUnformatted($"{row.CurrencyRewardCount}x");
            ImGui.SameLine(0, 3);
            DrawRewardItem(row.CurrencyReward.Value);
        }

        ImGui.TableNextColumn(); // Emote Reward

        if (row.EmoteReward.RowId > 0)
        {
            using var id = ImRaii.PushId($"Quest{row.Id}Emote");
            DrawRewardEmote(row.EmoteReward.Value);
        }

        ImGui.TableNextColumn(); // Instance Unlock

        if (row.InstanceContentUnlock.RowId > 0)
        {
            using var id = ImRaii.PushId($"Quest{row.Id}InstanceContent");
            DrawRewardInstanceContentUnlock(row.InstanceContentUnlock.Value);
        }

        ImGui.TableNextColumn(); // Rewards

        var hasDrawnReward = false;
        var rewardIndex = 0;
        foreach (var reward in row.Reward)
        {
            if (reward.RowId == 0)
                continue;

            if (reward.TryGetValue<Item>(out var item) && item.ItemUICategory.RowId > 0)
            {
                if (hasDrawnReward)
                    ImGui.SameLine();

                using var id = ImRaii.PushId($"Quest{row.Id}Reward{rewardIndex++}");
                DrawRewardItem(item);

                hasDrawnReward = true;
            }
            else if (reward.TryGetValueSubrow<QuestClassJobReward>(out var questClassJobReward))
            {
                foreach (var subrow in questClassJobReward)
                {
                    for (var i = 0; i < subrow.RequiredItem.Count; i++)
                    {
                        var amount = subrow.RewardAmount[i];
                        if (amount == 0)
                            continue;

                        var rewardItem = subrow.RewardItem[i];
                        if (rewardItem.RowId == 0 || !rewardItem.IsValid || rewardItem.Value.ItemUICategory.RowId == 0)
                            continue;

                        if (hasDrawnReward)
                            ImGui.SameLine();

                        using var id = ImRaii.PushId($"Quest{row.Id}QuestClassJobReward{rewardIndex++}");

                        if (amount > 1)
                        {
                            ImGui.TextUnformatted($"{amount}x");
                            ImGui.SameLine(0, 3);
                        }

                        DrawRewardItem(rewardItem.Value);

                        hasDrawnReward |= true;
                    }
                }
            }
        }

        ImGui.TableNextColumn(); // Optional Rewards

        if (row.OptionalItemReward.Any(reward => reward.RowId != 0))
        {
            hasDrawnReward = false;

            for (var i = 0; i < row.OptionalItemReward.Count; i++)
            {
                var itemRef = row.OptionalItemReward[i];
                if (itemRef.RowId == 0)
                    continue;

                if (itemRef.Value.ItemUICategory.RowId == 0)
                    continue;

                var amount = row.OptionalItemCountReward[i];
                if (amount == 0)
                    continue;

                if (hasDrawnReward)
                    ImGui.SameLine();

                using var id = ImRaii.PushId($"Quest{row.Id}OptionalReward{rewardIndex++}");

                if (amount > 1)
                {
                    ImGui.TextUnformatted($"{amount}x");
                    ImGui.SameLine(0, 3);
                }

                DrawRewardItem(itemRef.Value); // TODO: HQ (row.OptionalItemIsHQReward)

                hasDrawnReward |= true;
            }
        }

        ImGui.TableNextColumn(); // Gil Reward

        if (row.GilReward > 0)
        {
            _debugRenderer.DrawIcon(65002, sameLine: false);
            ImGui.SameLine(0, 3);
            ImGui.TextUnformatted($"{row.GilReward} {SeIconChar.Gil.ToIconChar()}");
        }
    }

    private void DrawRewardEmote(Emote emote)
    {
        DrawIconWithTooltip(
            new GameIconLookup(emote.Icon),
            _textService.GetEmoteName(emote.RowId),
            emote.TextCommand.IsValid && !emote.TextCommand.Value.Command.IsEmpty
                ? emote.TextCommand.Value.Command.ExtractText().StripSoftHypen()
                : null);

        _imGuiContextMenu.Draw($"EmoteReward{emote.RowId}ContextMenu", builder =>
        {
            builder.AddCopyName(_textService, emote.Name.ExtractText());
            builder.AddOpenOnGarlandTools("emote", emote.RowId);
        });
    }

    private void DrawRewardInstanceContentUnlock(InstanceContent instanceContent)
    {
        if (!_excelService.TryGetRow<ContentFinderCondition>(instanceContent.Order, out var cfc))
        {
            ImGui.Dummy(new(ImGui.GetTextLineHeight()));
            return;
        }

        DrawIconWithTooltip(
            cfc.ContentType.Value.Icon,
            cfc.Name.ExtractText(),
            cfc.ContentUICategory.IsValid && !cfc.ContentUICategory.Value.Name.IsEmpty
                ? cfc.ContentUICategory.Value.Name.ExtractText()
                : null);

        _imGuiContextMenu.Draw($"InstanceUnlockReward{instanceContent.RowId}ContextMenu", builder =>
        {
            builder.AddCopyName(_textService, cfc.Name.ExtractText());
            builder.AddOpenOnGarlandTools("instance", instanceContent.RowId);
        });
    }

    private void DrawIconWithTooltip(GameIconLookup icon, string title, string? category = null, string? description = null)
    {
        if (!_textureProvider.TryGetFromGameIcon(icon, out var tex) || !tex.TryGetWrap(out var texture, out _))
        {
            ImGui.Dummy(new(ImGui.GetTextLineHeight()));
            return;
        }

        ImGui.Image(texture.ImGuiHandle, new Vector2(ImGui.GetTextLineHeight()));

        if (ImGui.IsItemHovered())
        {
            _unlocksTabUtils.DrawTooltip(texture, title, category, description);
        }
    }

    private void DrawRewardItem(Item item)
    {
        if (!_textureProvider.TryGetFromGameIcon((uint)item.Icon, out var tex) || !tex.TryGetWrap(out var texture, out _))
        {
            ImGui.Dummy(new(ImGui.GetTextLineHeight()));
            return;
        }

        ImGui.Image(texture.ImGuiHandle, new Vector2(ImGui.GetTextLineHeight()));

        _imGuiContextMenu.Draw($"Item{item.RowId}ContextMenu", builder =>
        {
            builder.AddTryOn(item.AsRef());
            builder.AddItemFinder(item.RowId);
            builder.AddCopyItemName(item.RowId);
            builder.AddItemSearch(item.AsRef());
            builder.AddOpenOnGarlandTools("item", item.RowId);
        });

        if (ImGui.IsItemHovered())
        {
            _unlocksTabUtils.DrawItemTooltip(item);
        }
    }
}
