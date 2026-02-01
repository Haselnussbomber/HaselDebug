using Dalamud.Interface.Textures.TextureWraps;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Game.Enums;
using HaselDebug.Services;
using HaselDebug.Sheets;
using Companion = Lumina.Excel.Sheets.Companion;
using Ornament = Lumina.Excel.Sheets.Ornament;

namespace HaselDebug.Utils;

[RegisterSingleton, AutoConstruct]
public unsafe partial class UnlocksTabUtils
{
    private readonly ExcelService _excelService;
    private readonly TextService _textService;
    private readonly ITextureProvider _textureProvider;
    private readonly UldService _uldService;
    private readonly ItemService _itemService;
    private readonly IDataManager _dataManager;
    private readonly TripleTriadNumberFont _tripleTriadNumberFont;
    private readonly ISeStringEvaluator _seStringEvaluator;
    private readonly DebugRenderer _debugRenderer;

    private readonly Dictionary<uint, Vector2> _iconSizeCache = [];
    private readonly Dictionary<ushort, uint> _facePaintIconCache = [];

    public bool DrawSelectableItem(ItemHandle item, ImGuiId id, bool drawIcon = true, bool isHq = false, float? iconSize = null, bool selected = false, ImGuiSelectableFlags flags = ImGuiSelectableFlags.None)
    {
        var itemName = _itemService.GetItemName(item).ToString();
        var isHovered = false;
        iconSize ??= ImGui.GetTextLineHeight();

        if (drawIcon)
        {
            _debugRenderer.DrawIcon(_itemService.GetItemIcon(item), isHq, drawInfo: (float)iconSize, noTooltip: true);
            isHovered |= ImGui.IsItemHovered();
        }
        var clicked = ImGui.Selectable(itemName, selected, flags);
        isHovered |= ImGui.IsItemHovered();

        if (string.IsNullOrWhiteSpace(itemName))
            return false;

        if (isHovered && !ImGui.IsKeyDown(ImGuiKey.LeftAlt))
        {
            DrawItemTooltip(item);
        }

        ImGuiContextMenu.Draw($"##{id}_ItemContextMenu{item.ItemId}_IconTooltip", builder =>
        {
            builder.AddTryOn(item);
            builder.AddItemFinder(item);
            builder.AddLinkItem(item);
            builder.AddCopyItemName(item);
            builder.AddItemSearch(item);
            builder.AddOpenOnGarlandTools("item", item.ItemId);
        });

        if (_itemService.IsUnlocked(item))
        {
            ImGui.SameLine(1, 0);

            if (_textureProvider.GetFromGame("ui/uld/RecipeNoteBook_hr1.tex").TryGetWrap(out var tex, out _))
            {
                var pos = ImGui.GetCursorScreenPos() + new Vector2((float)iconSize / 2f);
                ImGui.GetWindowDrawList().AddImage(tex.Handle, pos, pos + new Vector2((float)iconSize / 1.5f), new Vector2(0.6818182f, 0.21538462f), new Vector2(1, 0.4f));
            }
        }

        return clicked;
    }

    public void DrawTooltip(uint iconId, ReadOnlySeString title, ReadOnlySeString category = default, ReadOnlySeString description = default)
    {
        if (!_textureProvider.TryGetFromGameIcon(iconId, out var tex) || !tex.TryGetWrap(out var texture, out _))
            return;

        DrawTooltip(texture, default, title, category, description);
    }

    public void DrawTooltip(string texturePath, DrawInfo drawInfo, ReadOnlySeString title, ReadOnlySeString category = default, ReadOnlySeString description = default)
    {
        if (!_textureProvider.GetFromGame(texturePath).TryGetWrap(out var texture, out _))
            return;

        DrawTooltip(texture, drawInfo, title, category, description);
    }

    public void DrawTooltip(IDalamudTextureWrap icon, DrawInfo drawInfo, ReadOnlySeString title, ReadOnlySeString category = default, ReadOnlySeString description = default)
    {
        using var tooltip = ImRaii.Tooltip();
        if (!tooltip) return;

        using var popuptable = ImRaii.Table("PopupTable"u8, 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.NoKeepColumnsVisible);
        if (!popuptable) return;

        var itemInnerSpacing = ImGui.GetStyle().ItemInnerSpacing * ImGuiHelpers.GlobalScale;
        var drawResult = ImGuiHelpers.SeStringWrapped(title, new()
        {
            TargetDrawList = default(ImDrawListPtr),
            Font = ImGui.GetFont(),
            ScreenOffset = ImGui.GetCursorScreenPos(),
            FontSize = ImGui.GetFontSize(),
        });

        ImGui.TableSetupColumn("Icon"u8, ImGuiTableColumnFlags.WidthFixed, 40 * ImGuiHelpers.GlobalScale + itemInnerSpacing.X);
        ImGui.TableSetupColumn("Text"u8, ImGuiTableColumnFlags.WidthFixed, Math.Max(drawResult.Size.X + itemInnerSpacing.X, 300 * ImGuiHelpers.GlobalScale));

        ImGui.TableNextColumn(); // Icon
        drawInfo.DrawSize ??= ImGuiHelpers.ScaledVector2(40);
        icon.Draw(drawInfo);

        ImGui.TableNextColumn(); // Text
        using var indentSpacing = ImRaii.PushStyle(ImGuiStyleVar.IndentSpacing, itemInnerSpacing.X);
        using var indent = ImRaii.PushIndent(1);

        ImGuiHelpers.SeStringWrapped(title);

        if (!category.IsEmpty)
        {
            ImGuiUtils.PushCursorY(-3 * ImGuiHelpers.GlobalScale);
            ImGuiHelpers.SeStringWrapped(category, new() { Color = Color.Grey.ToUInt() });
        }

        if (!description.IsEmpty)
        {
            ImGuiUtils.PushCursorY(1 * ImGuiHelpers.GlobalScale);

            // separator
            var pos = ImGui.GetCursorScreenPos();
            ImGui.GetWindowDrawList().AddLine(pos, pos + new Vector2(ImGui.GetContentRegionAvail().X, 0), ImGui.GetColorU32(ImGuiCol.Separator));
            ImGuiUtils.PushCursorY(4 * ImGuiHelpers.GlobalScale);

            ImGuiHelpers.SeStringWrapped(description);
        }
    }

    public void DrawItemTooltip(RowRef rowRef)
    {
        if (rowRef.TryGetValue<Item>(out var item))
            DrawItemTooltip(item);
        else if (rowRef.TryGetValue<EventItem>(out var eventItem))
            DrawEventItemTooltip(eventItem);
    }

    public void DrawItemTooltip(ItemHandle item, string? description = null)
    {
        if (!_textureProvider.TryGetFromGameIcon(_itemService.GetItemIcon(item), out var tex) || !tex.TryGetWrap(out var icon, out _))
            return;

        using var id = ImRaii.PushId($"ItemTooltip{item.ItemId}");

        using var tooltip = ImRaii.Tooltip();
        if (!tooltip) return;

        using var popuptable = ImRaii.Table("PopupTable"u8, 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.NoKeepColumnsVisible);
        if (!popuptable) return;

        var itemInnerSpacing = ImGui.GetStyle().ItemInnerSpacing * ImGuiHelpers.GlobalScale;
        var title = _itemService.GetItemName(item).ToString();

        ImGui.TableSetupColumn("Icon"u8, ImGuiTableColumnFlags.WidthFixed, 40 * ImGuiHelpers.GlobalScale + itemInnerSpacing.X);
        ImGui.TableSetupColumn("Text"u8, ImGuiTableColumnFlags.WidthFixed, Math.Max(ImGui.CalcTextSize(title).X + itemInnerSpacing.X, 300 * ImGuiHelpers.GlobalScale));

        ImGui.TableNextColumn(); // Icon
        ImGui.Image(icon.Handle, ImGuiHelpers.ScaledVector2(40));

        var isUnlocked = _itemService.IsUnlocked(item);
        if (isUnlocked)
        {
            ImGui.SameLine(1 + ImGui.GetStyle().CellPadding.X + itemInnerSpacing.X, 0);

            if (_textureProvider.GetFromGame("ui/uld/RecipeNoteBook_hr1.tex").TryGetWrap(out var checkTex, out _))
            {
                var pos = ImGui.GetCursorScreenPos() + new Vector2(40 * ImGuiHelpers.GlobalScale / 2f);
                ImGui.GetWindowDrawList().AddImage(checkTex.Handle, pos, pos + new Vector2(40 * ImGuiHelpers.GlobalScale / 1.5f), new Vector2(0.6818182f, 0.21538462f), new Vector2(1, 0.4f));
            }
        }

        ImGui.TableNextColumn(); // Text
        using var indentSpacing = ImRaii.PushStyle(ImGuiStyleVar.IndentSpacing, itemInnerSpacing.X);
        using var indent = ImRaii.PushIndent(1);

        ImGui.Text(title);

        if (isUnlocked)
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 40 * ImGuiHelpers.GlobalScale / 2f - 3); // wtf

        var isItem = _itemService.TryGetItem(item, out var itemRow);

        var category = isItem && itemRow.ItemUICategory.IsValid ? itemRow.ItemUICategory.Value.Name.ToString() : null;
        if (!string.IsNullOrEmpty(category))
        {
            ImGuiUtils.PushCursorY(-3 * ImGuiHelpers.GlobalScale);
            using (ImRaii.PushColor(ImGuiCol.Text, Color.Grey.ToUInt()))
                ImGui.Text(category);
        }

        if (description == null)
        {
            var itemDescription = _itemService.GetItemDescription(item);
            if (!itemDescription.IsEmpty)
            {
                description = itemDescription.ToString();
            }
        }

        if (!string.IsNullOrEmpty(description))
        {
            DrawSeparator(marginTop: 1, marginBottom: 4);

            ImGui.TextWrapped(description);
        }

        if (isItem && itemRow.ItemAction.TryGetRow(out var itemAction))
        {
            switch ((ItemActionType)itemAction.Action.RowId)
            {
                case ItemActionType.Mount when _excelService.TryGetRow<Mount>(itemAction.Data[0], out var mount):
                    _textureProvider.DrawIcon(64000 + mount.Icon, new DrawInfo() { Scale = 0.5f * ImGuiHelpers.GlobalScale });
                    break;

                case ItemActionType.Companion when _excelService.TryGetRow<Companion>(itemAction.Data[0], out var companion):
                    _textureProvider.DrawIcon(64000 + companion.Icon, new DrawInfo() { Scale = 0.5f * ImGuiHelpers.GlobalScale });
                    break;

                case ItemActionType.Ornament when _excelService.TryGetRow<Ornament>(itemAction.Data[0], out var ornament):
                    _textureProvider.DrawIcon(59000 + ornament.Icon, new DrawInfo() { Scale = 0.5f * ImGuiHelpers.GlobalScale });
                    break;

                case ItemActionType.UnlockLink when itemAction.Data[1] == 5211 && _excelService.TryGetRow<Emote>(itemAction.Data[2], out var emote):
                    _textureProvider.DrawIcon((uint)emote.Icon, new DrawInfo() { Scale = 0.5f * ImGuiHelpers.GlobalScale });
                    break;

                case ItemActionType.UnlockLink when itemAction.Data[1] == 4659 && _itemService.GetHairstyleIconId(item) is { } hairStyleIconId && hairStyleIconId != 0:
                    _textureProvider.DrawIcon(hairStyleIconId, new DrawInfo() { Scale = ImGuiHelpers.GlobalScale });
                    break;

                case ItemActionType.UnlockLink when itemAction.Data[1] == 9390 && TryGetFacePaintIconId(itemAction.Data[0], out var facePaintIconId):
                    _textureProvider.DrawIcon(facePaintIconId, new DrawInfo() { Scale = ImGuiHelpers.GlobalScale });
                    break;

                case ItemActionType.TripleTriadCard:
                    if (_excelService.TryGetRow<TripleTriadCardResident>(itemAction.Data[0], out var residentRow) &&
                        _excelService.TryGetRow<TripleTriadCardObtain>(residentRow.AcquisitionType.RowId, out var obtainRow) &&
                        obtainRow.Icon != 0)
                    {
                        DrawSeparator();
                        _textureProvider.DrawIcon(obtainRow.Icon, 40 * ImGuiHelpers.GlobalScale);
                        ImGui.SameLine();
                        ImGui.TextWrapped(_seStringEvaluator.EvaluateFromAddon(obtainRow.Icon, [
                            residentRow.Acquisition.RowId,
                            residentRow.Location.RowId
                        ]).ToString());
                    }

                    DrawTripleTriadCard(item);
                    break;

                default:
                    if (itemRow.ItemUICategory.RowId == 95 && _excelService.TryGetRow<Picture>(itemRow.AdditionalData.RowId, out var picture)) // Paintings
                    {
                        _textureProvider.DrawIcon(picture.Image, new DrawInfo() { Fit = ContentFit.Cover });
                    }
                    break;
            }
        }
    }

    private void DrawTripleTriadCard(Item item)
    {
        if (item.ItemAction.IsValid)
            DrawTripleTriadCard(item.ItemAction.Value.Data[0]);
    }

    private void DrawTripleTriadCard(uint cardId)
    {
        if (!_excelService.TryGetRow<TripleTriadCard>(cardId, out var card))
            return;

        if (!_excelService.TryGetRow<TripleTriadCardResident>(cardId, out var cardResident))
            return;

        DrawSeparator(marginTop: 3);

        var isEx = cardResident.UIPriority == 5;
        var order = (uint)cardResident.Order;
        var addonRowId = isEx ? 9773u : 9772;

        var infoText = $"{_seStringEvaluator.EvaluateFromAddon(addonRowId, [order]).ToString()} - {card.Name}";
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - ImGui.GetStyle().IndentSpacing + ImGui.GetContentRegionAvail().X / 2f - ImGui.CalcTextSize(infoText).X / 2f);
        ImGui.Text(infoText);

        var cardSizeScaled = ImGuiHelpers.ScaledVector2(208, 256);
        var cardStartPosX = ImGui.GetCursorPosX() - ImGui.GetStyle().IndentSpacing + ImGui.GetContentRegionAvail().X / 2f - cardSizeScaled.X / 2f;
        var cardStartPos = new Vector2(cardStartPosX, ImGui.GetCursorPosY());

        // draw background
        ImGui.SetCursorPosX(cardStartPosX);
        _uldService.DrawPart("CardTripleTriad", 1, 0, cardSizeScaled);

        // draw card
        ImGui.SetCursorPos(cardStartPos);
        _textureProvider.DrawIcon(87000 + cardId, cardSizeScaled);

        // draw numbers
        using var font = _tripleTriadNumberFont.Push();

        var letterSize = ImGui.CalcTextSize("A");
        var scaledLetterSize = letterSize / 2f;
        var pos = cardStartPos + new Vector2(cardSizeScaled.X / 2f, cardSizeScaled.Y - letterSize.Y * 1.5f) - letterSize;

        var positionTop = pos + new Vector2(scaledLetterSize.X, -scaledLetterSize.Y);
        var positionBottom = pos + new Vector2(scaledLetterSize.X, scaledLetterSize.Y);
        var positionRight = pos + new Vector2(letterSize.X * 1.1f + scaledLetterSize.X, 0);
        var positionLeft = pos + new Vector2(-(letterSize.X * 0.1f + scaledLetterSize.X), 0);

        var textTop = $"{cardResident.Top:X}";
        var textBottom = $"{cardResident.Bottom:X}";
        var textRight = $"{cardResident.Right:X}";
        var textLeft = $"{cardResident.Left:X}";

        DrawTextShadow(positionTop, textTop);
        DrawTextShadow(positionBottom, textBottom);
        DrawTextShadow(positionRight, textRight);
        DrawTextShadow(positionLeft, textLeft);

        DrawText(positionTop, textTop);
        DrawText(positionBottom, textBottom);
        DrawText(positionRight, textRight);
        DrawText(positionLeft, textLeft);

        // draw stars
        var cardRarity = cardResident.TripleTriadCardRarity.Value!;

        var starSize = 32 * 0.75f * ImGuiHelpers.GlobalScale;
        var starRadius = starSize / 1.666f;
        var starCenter = cardStartPos + ImGuiHelpers.ScaledVector2(14) + new Vector2(starSize) / 2f;

        if (cardRarity.Stars >= 1)
        {
            DrawStar(StarPosition.Top);

            if (cardRarity.Stars >= 2)
                DrawStar(StarPosition.Left);
            if (cardRarity.Stars >= 3)
                DrawStar(StarPosition.Right);
            if (cardRarity.Stars >= 4)
                DrawStar(StarPosition.BottomLeft);
            if (cardRarity.Stars >= 5)
                DrawStar(StarPosition.BottomRight);
        }

        // draw type
        if (cardResident.TripleTriadCardType.RowId != 0)
        {
            var typeSize = 32 * ImGuiHelpers.GlobalScale;

            var partIndex = cardResident.TripleTriadCardType.RowId switch
            {
                4 => 2u,
                _ => cardResident.TripleTriadCardType.RowId + 2
            };

            ImGui.SetCursorPos(cardStartPos + new Vector2(cardSizeScaled.X - typeSize * 1.5f, typeSize / 2.5f));
            _uldService.DrawPart("CardTripleTriad", 1, partIndex, typeSize);
        }

        // functions

        void DrawStar(StarPosition pos)
        {
            var angleIncrement = 2 * MathF.PI / 5; // 5 = amount of stars
            var angle = (int)pos * angleIncrement - MathF.PI / 2;

            ImGui.SetCursorPos(starCenter + new Vector2(starRadius * MathF.Cos(angle), starRadius * MathF.Sin(angle)));
            _uldService.DrawPart("CardTripleTriad", 1, 1, starSize);
        }
    }

    private static void DrawTextShadow(Vector2 position, string text)
    {
        DrawShadow(position, ImGui.CalcTextSize(text), 8, Color.Black with { A = 0.1f });
    }

    private static void DrawText(Vector2 position, string text)
    {
        var outlineColor = Color.Black with { A = 0.5f };

        // outline
        ImGui.SetCursorPos(position + ImGuiHelpers.ScaledVector2(-1));
        using (outlineColor.Push(ImGuiCol.Text))
            ImGui.Text(text);

        ImGui.SetCursorPos(position + ImGuiHelpers.ScaledVector2(1));
        using (outlineColor.Push(ImGuiCol.Text))
            ImGui.Text(text);

        ImGui.SetCursorPos(position + ImGuiHelpers.ScaledVector2(1, -1));
        using (outlineColor.Push(ImGuiCol.Text))
            ImGui.Text(text);

        ImGui.SetCursorPos(position + ImGuiHelpers.ScaledVector2(-1, 1));
        using (outlineColor.Push(ImGuiCol.Text))
            ImGui.Text(text);

        // text
        ImGui.SetCursorPos(position);
        ImGui.Text(text);
    }

    private static void DrawShadow(Vector2 pos, Vector2 size, int layers, Vector4 shadowColor)
    {
        var drawList = ImGui.GetWindowDrawList();

        for (var i = 0; i < layers; i++)
        {
            var shadowOffset = i * 2.0f;
            var transparency = shadowColor.W * (1.0f - (float)i / layers);
            var currentShadowColor = new Vector4(shadowColor.X, shadowColor.Y, shadowColor.Z, transparency);

            drawList.AddRectFilled(
                pos - new Vector2(shadowOffset, shadowOffset),
                pos + size + new Vector2(shadowOffset, shadowOffset),
                ImGui.ColorConvertFloat4ToU32(currentShadowColor),
                50
            );
        }
    }

    private enum StarPosition
    {
        Top = 0,
        Right = 1,
        Left = 4,
        BottomLeft = 3,
        BottomRight = 2
    }

    private unsafe bool TryGetFacePaintIconId(ushort dataId, out uint iconId)
    {
        if (_facePaintIconCache.TryGetValue(dataId, out iconId))
            return true;

        var playerState = PlayerState.Instance();
        if (playerState == null || playerState->IsLoaded)
        {
            _facePaintIconCache.Add(dataId, iconId = 0);
            return false;
        }

        if (!_excelService.TryFindRow<HairMakeType>(t => t.Tribe.RowId == playerState->Tribe && t.Gender == playerState->Sex, out var hairMakeType))
        {
            _facePaintIconCache.Add(dataId, iconId = 0);
            return false;
        }

        if (!_excelService.TryFindRow<CharaMakeCustomize>(row => row.IsPurchasable && row.UnlockLink == dataId && hairMakeType.CharaMakeStruct[7].SubMenuParam.Any(id => id == row.RowId), out var charaMakeCustomize))
        {
            _facePaintIconCache.Add(dataId, iconId = 0);
            return false;
        }

        _facePaintIconCache.Add(dataId, iconId = charaMakeCustomize.Icon);
        return true;
    }

    public void DrawEventItemTooltip(EventItem item)
    {
        if (!_textureProvider.TryGetFromGameIcon((uint)item.Icon, out var tex) || !tex.TryGetWrap(out var icon, out _))
            return;

        using var id = ImRaii.PushId($"ItemTooltip{item.RowId}");

        using var tooltip = ImRaii.Tooltip();
        if (!tooltip) return;

        using var popuptable = ImRaii.Table("PopupTable"u8, 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.NoKeepColumnsVisible);
        if (!popuptable) return;

        var itemInnerSpacing = ImGui.GetStyle().ItemInnerSpacing * ImGuiHelpers.GlobalScale;
        var title = _textService.GetItemName(item.RowId).ToString();

        ImGui.TableSetupColumn("Icon"u8, ImGuiTableColumnFlags.WidthFixed, 40 * ImGuiHelpers.GlobalScale + itemInnerSpacing.X);
        ImGui.TableSetupColumn("Text"u8, ImGuiTableColumnFlags.WidthFixed, Math.Max(ImGui.CalcTextSize(title).X + itemInnerSpacing.X, 300 * ImGuiHelpers.GlobalScale));

        ImGui.TableNextColumn(); // Icon
        ImGui.Image(icon.Handle, ImGuiHelpers.ScaledVector2(40));

        ImGui.TableNextColumn(); // Text
        using var indentSpacing = ImRaii.PushStyle(ImGuiStyleVar.IndentSpacing, itemInnerSpacing.X);
        using var indent = ImRaii.PushIndent(1);

        ImGui.Text(title);

        if (item.Category.RowId != 0 && _excelService.TryGetRow<EventItemCategory>(item.Category.RowId, out var itemCategoy) && !itemCategoy.Unknown0.IsEmpty)
        {
            var text = itemCategoy.RowId switch
            {
                1 when item.Quest.IsValid && !item.Quest.Value.Name.IsEmpty => _seStringEvaluator.Evaluate(itemCategoy.Unknown0, [_textService.GetQuestName(item.Quest.RowId)]),
                _ => itemCategoy.Unknown0
            };

            if (!text.IsEmpty)
            {
                ImGuiUtils.PushCursorY(-3 * ImGuiHelpers.GlobalScale);
                using (ImRaii.PushColor(ImGuiCol.Text, Color.Grey.ToUInt()))
                    ImGui.Text(text.ToString());
            }
        }

        if (_excelService.TryGetRow<EventItemHelp>(item.RowId, out var itemHelp) && !itemHelp.Description.IsEmpty)
        {
            DrawSeparator(marginTop: 1, marginBottom: 4);

            ImGui.TextWrapped(itemHelp.Description.ToString());
        }
    }

    public void DrawQuestTooltip(Quest quest)
    {
        using var id = ImRaii.PushId($"QuestTooltip{quest.RowId}");

        using var tooltip = ImRaii.Tooltip();
        if (!tooltip) return;

        using var popuptable = ImRaii.Table("PopupTable"u8, 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.NoKeepColumnsVisible);
        if (!popuptable) return;

        var itemInnerSpacing = ImGui.GetStyle().ItemInnerSpacing * ImGuiHelpers.GlobalScale;
        var title = _textService.GetQuestName(quest.RowId);

        ImGui.TableSetupColumn("Icon"u8, ImGuiTableColumnFlags.WidthFixed, 40 * ImGuiHelpers.GlobalScale + itemInnerSpacing.X);
        ImGui.TableSetupColumn("Text"u8, ImGuiTableColumnFlags.WidthFixed, Math.Max(ImGui.CalcTextSize(title).X + itemInnerSpacing.X, 300 * ImGuiHelpers.GlobalScale));

        ImGui.TableNextColumn(); // Icon

        var eventIconType = quest.EventIconType.IsValid
            ? quest.EventIconType.Value
            : _excelService.GetSheet<EventIconType>().GetRow(1);

        var iconOffset = 1u;
        if (QuestManager.IsQuestComplete(quest.RowId))
            iconOffset = 5u;
        else if (quest.IsRepeatable)
            iconOffset = 2u;

        if (eventIconType.MapIconAvailable != 0 &&
            _textureProvider.TryGetFromGameIcon(eventIconType.MapIconAvailable + iconOffset, out var tex) &&
            tex.TryGetWrap(out var icon, out _))
        {
            ImGui.Image(icon.Handle, ImGuiHelpers.ScaledVector2(40));
        }

        ImGui.TableNextColumn(); // Text
        using var indentSpacing = ImRaii.PushStyle(ImGuiStyleVar.IndentSpacing, itemInnerSpacing.X);
        using var indent = ImRaii.PushIndent(1);

        ImGui.Text(title);

        var text = quest.JournalGenre.IsValid ? quest.JournalGenre.Value.Name.ToString() : null;
        if (!string.IsNullOrWhiteSpace(text))
        {
            ImGuiUtils.PushCursorY(-3 * ImGuiHelpers.GlobalScale);
            using (ImRaii.PushColor(ImGuiCol.Text, Color.Grey.ToUInt()))
                ImGui.Text(text);
        }

        var iconId = quest.Icon;

        var currentQuest = quest;
        while (iconId == 0 && currentQuest.PreviousQuest[0].RowId != 0)
        {
            currentQuest = currentQuest.PreviousQuest[0].Value;
            iconId = currentQuest.Icon;
        }

        if (iconId != 0 && _textureProvider.TryGetFromGameIcon(iconId, out var imageTex) && imageTex.TryGetWrap(out var image, out _))
        {
            DrawSeparator(marginTop: 1, marginBottom: 5);
            var newWidth = ImGui.GetContentRegionAvail().X;
            var ratio = newWidth / image.Width;
            var newHeight = image.Height * ratio;
            ImGui.Image(image.Handle, new Vector2(newWidth, newHeight));
        }

        var questText = _excelService.GetSheet<QuestText>($"quest/{(quest.RowId - 0x10000) / 100:000}/{quest.Id.ToString()}");
        var questSequence = QuestManager.GetQuestSequence((ushort)(quest.RowId - 0x10000));
        if (questSequence == 0xFF) questSequence = 1;
        for (var seq = questSequence == 0 ? 0 : 1; seq <= questSequence; seq++)
        {
            if (questText.TryGetFirst(kvRow => kvRow.LuaKey.ToString() == $"TEXT_{quest.Id.ToString().ToUpper()}_SEQ_{seq:00}", out var seqText) && !seqText.Text.IsEmpty)
            {
                DrawSeparator(marginTop: 1, marginBottom: 4);
                ImGuiHelpers.SeStringWrapped(_seStringEvaluator.Evaluate(seqText.Text));
            }
        }
    }

    // kinda meh, lol
    public void DrawAdventureTooltip(int index, Adventure adventure)
    {
        using var id = ImRaii.PushId($"AdventureTooltip{adventure.RowId}");

        using var tooltip = ImRaii.Tooltip();
        if (!tooltip) return;

        using var outerpopuptable = ImRaii.Table("OuterPopupTable"u8, 1, ImGuiTableFlags.NoPadOuterX | ImGuiTableFlags.NoPadInnerX | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.NoKeepColumnsVisible);
        if (!outerpopuptable) return;

        var itemInnerSpacing = ImGui.GetStyle().ItemInnerSpacing * ImGuiHelpers.GlobalScale;
        var indexStr = $"#{index:000}";
        var title = adventure.Name.ToString();

        var leftColumnWidth = ImGui.CalcTextSize(indexStr).X + itemInnerSpacing.X;
        var rightColumnWidth = Math.Max(ImGui.CalcTextSize(title).X + itemInnerSpacing.X, 300 * ImGuiHelpers.GlobalScale);

        ImGui.TableSetupColumn("Table"u8, ImGuiTableColumnFlags.WidthFixed, leftColumnWidth + rightColumnWidth + ImGui.GetStyle().CellPadding.X * 2); // ???

        ImGui.TableNextColumn(); // Table

        using var popuptable = ImRaii.Table("PopupTable"u8, 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.NoKeepColumnsVisible);
        if (!popuptable) return;

        ImGui.TableSetupColumn("Index"u8, ImGuiTableColumnFlags.WidthFixed, leftColumnWidth);
        ImGui.TableSetupColumn("Title"u8, ImGuiTableColumnFlags.WidthFixed, rightColumnWidth);

        ImGui.TableNextColumn(); // Index
        ImGui.Text(indexStr);

        ImGui.TableNextColumn(); // Title
        using var indentSpacing = ImRaii.PushStyle(ImGuiStyleVar.IndentSpacing, itemInnerSpacing.X);
        using var indent = ImRaii.PushIndent(1);
        ImGui.Text(title);

        var text = _textService.GetPlaceName(adventure.PlaceName.RowId);
        ImGuiUtils.PushCursorY(-3 * ImGuiHelpers.GlobalScale);
        using (ImRaii.PushColor(ImGuiCol.Text, Color.Grey.ToUInt()))
            ImGui.Text(text);

        indent.Dispose();
        indentSpacing.Dispose();
        popuptable.Dispose();

        var iconId = adventure.IconDiscovered;
        var iconDrawn = false;
        if (iconId != 0 && _textureProvider.TryGetFromGameIcon(iconId, out var imageTex) && imageTex.TryGetWrap(out var image, out _))
        {
            ImGuiUtils.PushCursorY(5 * ImGuiHelpers.GlobalScale);
            var newWidth = ImGui.GetContentRegionAvail().X;
            var ratio = newWidth / image.Width;
            var newHeight = image.Height * ratio;
            ImGui.Image(image.Handle, new Vector2(newWidth, newHeight));
            iconDrawn = true;
        }

        ImGuiUtils.PushCursorY((iconDrawn ? -10 : 1) * ImGuiHelpers.GlobalScale);
        using var indentSpacing2 = ImRaii.PushStyle(ImGuiStyleVar.IndentSpacing, itemInnerSpacing.X);
        using var indent2 = ImRaii.PushIndent(1);
        ImGuiHelpers.SeStringWrapped(_seStringEvaluator.Evaluate(adventure.Description));
    }

    public void DrawHowToTooltip(HowTo howTo)
    {
        using var id = ImRaii.PushId($"HowToTooltip{howTo.RowId}");

        using var tooltip = ImRaii.Tooltip();
        if (!tooltip) return;

        var itemInnerSpacing = ImGui.GetStyle().ItemInnerSpacing * ImGuiHelpers.GlobalScale;
        const float MaxImageWidth = 200f;

        using var popuptitletable = ImRaii.Table("PopupTitleTableRow"u8, 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.NoKeepColumnsVisible);
        if (!popuptitletable) return;

        ImGui.TableSetupColumn("Icon"u8, ImGuiTableColumnFlags.WidthFixed, ImGui.GetTextLineHeight() * 2);
        ImGui.TableSetupColumn("Text"u8, ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableNextRow();
        ImGui.TableNextColumn(); // Icon

        _uldService.DrawPart("HowTo", 8, 2, ImGui.GetTextLineHeight() * 2);

        ImGui.TableNextColumn(); // Text

        ImGui.Text(howTo.Name.ToString());

        var category = howTo.Category.ValueNullable?.Category.ToString() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(category))
        {
            ImGuiUtils.PushCursorY(-3 * ImGuiHelpers.GlobalScale);
            using (ImRaii.PushColor(ImGuiCol.Text, Color.Grey.ToUInt()))
                ImGui.Text(category);
        }

        DrawSeparator(marginTop: 1, marginBottom: 5);

        var playerState = PlayerState.Instance();

        var i = 0;
        foreach (var page in howTo.HowToPagePC)
        {
            if (page.RowId == 0 || !page.IsValid) continue;

            if (i > 0)
                DrawSeparator(marginTop: -1);

            var iconOffsetType = page.Value.IconType switch
            {
                1 when playerState->IsLoaded => playerState->StartTown - 1,
                2 when playerState->IsLoaded => playerState->GrandCompany - 1,
                _ => 0,
            };
            var iconOffset = new int[] { 0, 3000, 6000 }[iconOffsetType];
            var iconId = page.Value.Image + iconOffset;

            var textIndex = page.Value.TextType switch
            {
                1 when playerState->IsLoaded => playerState->StartTown - 1,
                2 when playerState->IsLoaded => playerState->GrandCompany - 1,
                _ => 0,
            };
            var text = page.Value.Text[textIndex];

            if (text.IsEmpty)
            {
                var maxWidth = Math.Max(ImGui.GetContentRegionAvail().X, 640);

                if (_textureProvider.TryGetFromGameIcon(iconId, out var texture2) && texture2.TryGetWrap(out var textureWrap2, out _))
                {
                    var size = textureWrap2.Size;

                    if (size.X > maxWidth)
                        size *= maxWidth / size.X;

                    textureWrap2.Draw(size);
                }
                else
                {
                    ImGui.Dummy(new Vector2(maxWidth, 1));
                }

                continue;
            }

            using var popuptable = ImRaii.Table($"PopupTableRow{i++}", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.NoKeepColumnsVisible);
            if (!popuptable) return;

            ImGui.TableSetupColumn("Icon"u8, ImGuiTableColumnFlags.WidthFixed, MaxImageWidth * ImGuiHelpers.GlobalScale + itemInnerSpacing.X);
            ImGui.TableSetupColumn("Text"u8, ImGuiTableColumnFlags.WidthFixed, 360 * ImGuiHelpers.GlobalScale);

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Icon

            if (_textureProvider.TryGetFromGameIcon(iconId, out var texture) && texture.TryGetWrap(out var textureWrap, out _))
            {
                var size = textureWrap.Size;

                if (size.X > MaxImageWidth)
                    size *= MaxImageWidth / size.X;

                textureWrap.Draw(size * ImGuiHelpers.GlobalScale);
            }
            else
            {
                ImGui.Dummy(new Vector2(280, 1));
            }

            ImGui.TableNextColumn(); // Text

            ImGuiHelpers.SeStringWrapped(_seStringEvaluator.Evaluate(text));
        }
    }

    private static void DrawSeparator(float marginTop = 2, float marginBottom = 5)
    {
        ImGuiUtils.PushCursorY(marginTop * ImGuiHelpers.GlobalScale);
        var pos = ImGui.GetCursorScreenPos();
        ImGui.GetWindowDrawList().AddLine(pos, pos + new Vector2(ImGui.GetContentRegionAvail().X, 0), ImGui.GetColorU32(ImGuiCol.Separator));
        ImGuiUtils.PushCursorY(marginBottom * ImGuiHelpers.GlobalScale);
    }
}
