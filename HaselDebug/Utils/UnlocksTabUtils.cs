using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Extensions.Strings;
using HaselCommon.Game.Enums;
using HaselCommon.Graphics;
using HaselCommon.Gui;
using HaselCommon.Services;
using HaselCommon.Sheets;
using HaselDebug.Services;
using HaselDebug.Sheets;
using ImGuiNET;
using Lumina.Data.Files;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Extensions;
using Companion = Lumina.Excel.Sheets.Companion;
using Ornament = Lumina.Excel.Sheets.Ornament;

namespace HaselDebug.Utils;

[RegisterSingleton]
public unsafe class UnlocksTabUtils(
    ExcelService ExcelService,
    TextService TextService,
    TextureService TextureService,
    ItemService ItemService,
    ImGuiContextMenuService ImGuiContextMenuService,
    IDataManager DataManager,
    ITextureProvider TextureProvider,
    TripleTriadNumberFont TripleTriadNumberFont,
    SeStringEvaluator SeStringEvaluator)
{
    private readonly Dictionary<uint, Vector2> _iconSizeCache = [];
    private readonly Dictionary<ushort, uint> _facePaintIconCache = [];

    public bool DrawSelectableItem(uint itemId, ImGuiId id, bool drawIcon = true, bool isHq = false, float? iconSize = null)
    {
        if (ExcelService.TryGetRow<Item>(itemId, out var item))
            return DrawSelectableItem(item, id, drawIcon, isHq, iconSize);
        return false;
    }

    public bool DrawSelectableItem(Item item, ImGuiId id, bool drawIcon = true, bool isHq = false, float? iconSize = null)
    {
        var itemName = TextService.GetItemName(item.RowId);
        var isHovered = false;
        iconSize ??= ImGui.GetTextLineHeight();

        if (drawIcon)
        {
            TextureService.DrawIcon(item.Icon, isHq, (float)iconSize);
            isHovered |= ImGui.IsItemHovered();
            ImGui.SameLine();
        }
        var clicked = ImGui.Selectable(itemName);
        isHovered |= ImGui.IsItemHovered();

        if (isHovered && !ImGui.IsKeyDown(ImGuiKey.LeftAlt))
        {
            DrawItemTooltip(item);
        }

        ImGuiContextMenuService.Draw($"##{id}_ItemContextMenu{item.RowId}_IconTooltip", builder =>
        {
            builder.AddTryOn(item);
            builder.AddItemFinder(item.RowId);
            builder.AddCopyItemName(item.RowId);
            builder.AddItemSearch(item);
            builder.AddOpenOnGarlandTools("item", item.RowId);
        });

        if (ItemService.IsUnlockable(item) && ItemService.IsUnlocked(item))
        {
            ImGui.SameLine(1, 0);

            if (TextureProvider.GetFromGame("ui/uld/RecipeNoteBook_hr1.tex").TryGetWrap(out var tex, out _))
            {
                var pos = ImGui.GetCursorScreenPos() + new Vector2((float)iconSize / 2f);
                ImGui.GetWindowDrawList().AddImage(tex.ImGuiHandle, pos, pos + new Vector2((float)iconSize / 1.5f), new Vector2(0.6818182f, 0.21538462f), new Vector2(1, 0.4f));
            }
        }

        return clicked;
    }

    public void DrawTooltip(uint iconId, string title, string? category = null, string? description = null)
    {
        if (!TextureProvider.TryGetFromGameIcon(iconId, out var tex) || !tex.TryGetWrap(out var texture, out _))
            return;

        DrawTooltip(texture, title, category, description);
    }

    public void DrawTooltip(IDalamudTextureWrap icon, string title, string? category = null, string? description = null)
    {
        using var tooltip = ImRaii.Tooltip();
        if (!tooltip) return;

        using var popuptable = ImRaii.Table("PopupTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.NoKeepColumnsVisible);
        if (!popuptable) return;

        var itemInnerSpacing = ImGui.GetStyle().ItemInnerSpacing * ImGuiHelpers.GlobalScale;

        ImGui.TableSetupColumn("Icon", ImGuiTableColumnFlags.WidthFixed, 40 * ImGuiHelpers.GlobalScale + itemInnerSpacing.X);
        ImGui.TableSetupColumn("Text", ImGuiTableColumnFlags.WidthFixed, Math.Max(ImGui.CalcTextSize(title).X + itemInnerSpacing.X, 300 * ImGuiHelpers.GlobalScale));

        ImGui.TableNextColumn(); // Icon
        ImGui.Image(icon.ImGuiHandle, ImGuiHelpers.ScaledVector2(40));

        ImGui.TableNextColumn(); // Text
        using var indentSpacing = ImRaii.PushStyle(ImGuiStyleVar.IndentSpacing, itemInnerSpacing.X);
        using var indent = ImRaii.PushIndent(1);

        ImGui.TextUnformatted(title);

        if (!string.IsNullOrEmpty(category))
        {
            ImGuiUtils.PushCursorY(-3 * ImGuiHelpers.GlobalScale);
            using (ImRaii.PushColor(ImGuiCol.Text, (uint)Color.Grey))
                ImGui.TextUnformatted(category);
        }

        if (!string.IsNullOrEmpty(description))
        {
            ImGuiUtils.PushCursorY(1 * ImGuiHelpers.GlobalScale);

            // separator
            var pos = ImGui.GetCursorScreenPos();
            ImGui.GetWindowDrawList().AddLine(pos, pos + new Vector2(ImGui.GetContentRegionAvail().X, 0), ImGui.GetColorU32(ImGuiCol.Separator));
            ImGuiUtils.PushCursorY(4 * ImGuiHelpers.GlobalScale);

            ImGuiHelpers.SafeTextWrapped(description);
        }
    }

    public void DrawItemTooltip(RowRef rowRef)
    {
        if (rowRef.TryGetValue<Item>(out var item))
            DrawItemTooltip(item);
        else if (rowRef.TryGetValue<EventItem>(out var eventItem))
            DrawEventItemTooltip(eventItem);
    }

    public void DrawItemTooltip(Item item, string? descriptionOverride = null)
    {
        if (!TextureProvider.TryGetFromGameIcon((uint)item.Icon, out var tex) || !tex.TryGetWrap(out var icon, out _))
            return;

        using var id = ImRaii.PushId($"ItemTooltip{item.RowId}");

        using var tooltip = ImRaii.Tooltip();
        if (!tooltip) return;

        using var popuptable = ImRaii.Table("PopupTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.NoKeepColumnsVisible);
        if (!popuptable) return;

        var itemInnerSpacing = ImGui.GetStyle().ItemInnerSpacing * ImGuiHelpers.GlobalScale;
        var title = TextService.GetItemName(item.RowId);

        ImGui.TableSetupColumn("Icon", ImGuiTableColumnFlags.WidthFixed, 40 * ImGuiHelpers.GlobalScale + itemInnerSpacing.X);
        ImGui.TableSetupColumn("Text", ImGuiTableColumnFlags.WidthFixed, Math.Max(ImGui.CalcTextSize(title).X + itemInnerSpacing.X, 300 * ImGuiHelpers.GlobalScale));

        ImGui.TableNextColumn(); // Icon
        ImGui.Image(icon.ImGuiHandle, ImGuiHelpers.ScaledVector2(40));

        var isUnlocked = ItemService.IsUnlockable(item) && ItemService.IsUnlocked(item);
        if (isUnlocked)
        {
            ImGui.SameLine(1 + ImGui.GetStyle().CellPadding.X + itemInnerSpacing.X, 0);

            if (TextureProvider.GetFromGame("ui/uld/RecipeNoteBook_hr1.tex").TryGetWrap(out var checkTex, out _))
            {
                var pos = ImGui.GetCursorScreenPos() + new Vector2(40 * ImGuiHelpers.GlobalScale / 2f);
                ImGui.GetWindowDrawList().AddImage(checkTex.ImGuiHandle, pos, pos + new Vector2(40 * ImGuiHelpers.GlobalScale / 1.5f), new Vector2(0.6818182f, 0.21538462f), new Vector2(1, 0.4f));
            }
        }

        ImGui.TableNextColumn(); // Text
        using var indentSpacing = ImRaii.PushStyle(ImGuiStyleVar.IndentSpacing, itemInnerSpacing.X);
        using var indent = ImRaii.PushIndent(1);

        ImGui.TextUnformatted(title);

        if (isUnlocked)
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 40 * ImGuiHelpers.GlobalScale / 2f - 3); // wtf

        var category = item.ItemUICategory.IsValid ? item.ItemUICategory.Value.Name.ExtractText().StripSoftHyphen() : null;
        if (!string.IsNullOrEmpty(category))
        {
            ImGuiUtils.PushCursorY(-3 * ImGuiHelpers.GlobalScale);
            using (ImRaii.PushColor(ImGuiCol.Text, (uint)Color.Grey))
                ImGui.TextUnformatted(category);
        }

        var description = descriptionOverride ?? (!item.Description.IsEmpty ? item.Description.ExtractText().StripSoftHyphen() : null);
        if (!string.IsNullOrEmpty(description))
        {
            DrawSeparator(marginTop: 1, marginBottom: 4);

            ImGuiHelpers.SafeTextWrapped(description);
        }

        switch ((ItemActionType)item.ItemAction.Value.Type)
        {
            case ItemActionType.Mount when ExcelService.TryGetRow<Mount>(item.ItemAction.Value.Data[0], out var mount):
                TextureService.DrawIcon(64000 + mount.Icon, new DrawInfo() { Scale = 0.5f * ImGuiHelpers.GlobalScale });
                break;

            case ItemActionType.Companion when ExcelService.TryGetRow<Companion>(item.ItemAction.Value.Data[0], out var companion):
                TextureService.DrawIcon(64000 + companion.Icon, new DrawInfo() { Scale = 0.5f * ImGuiHelpers.GlobalScale });
                break;

            case ItemActionType.Ornament when ExcelService.TryGetRow<Ornament>(item.ItemAction.Value.Data[0], out var ornament):
                TextureService.DrawIcon(59000 + ornament.Icon, new DrawInfo() { Scale = 0.5f * ImGuiHelpers.GlobalScale });
                break;

            case ItemActionType.UnlockLink when item.ItemAction.Value.Data[1] == 5211 && ExcelService.TryGetRow<Emote>(item.ItemAction.Value.Data[2], out var emote):
                TextureService.DrawIcon(emote.Icon, new DrawInfo() { Scale = 0.5f * ImGuiHelpers.GlobalScale });
                break;

            case ItemActionType.UnlockLink when item.ItemAction.Value.Data[1] == 4659 && ItemService.GetHairstyleIconId(item.RowId) is { } hairStyleIconId && hairStyleIconId != 0:
                TextureService.DrawIcon(hairStyleIconId, new DrawInfo() { Scale = ImGuiHelpers.GlobalScale });
                break;

            case ItemActionType.UnlockLink when item.ItemAction.Value.Data[1] == 9390 && TryGetFacePaintIconId(item.ItemAction.Value.Data[0], out var facePaintIconId):
                TextureService.DrawIcon(facePaintIconId, new DrawInfo() { Scale = ImGuiHelpers.GlobalScale });
                break;

            case ItemActionType.TripleTriadCard:
                if (ExcelService.TryGetRow<TripleTriadCardResident>(item.ItemAction.Value.Data[0], out var residentRow) &&
                    ExcelService.TryGetRow<TripleTriadCardObtain>(residentRow.AcquisitionType, out var obtainRow) &&
                    obtainRow.Unknown1 != 0)
                {
                    DrawSeparator();
                    TextureService.DrawIcon(obtainRow.Unknown0, 40 * ImGuiHelpers.GlobalScale);
                    ImGui.SameLine();
                    ImGuiHelpers.SafeTextWrapped(SeStringEvaluator.EvaluateFromAddon(obtainRow.Unknown1, [
                        residentRow.Acquisition.RowId,
                    residentRow.Location.RowId
                    ]).ExtractText().StripSoftHyphen());
                }

                DrawTripleTriadCard(item);
                break;

            default:
                if (item.ItemUICategory.RowId == 95 && ExcelService.TryGetRow<Picture>(item.AdditionalData.RowId, out var picture)) // Paintings
                {
                    TextureService.DrawIcon(picture.Image, ResizeToFit(GetIconSize((uint)picture.Image), ImGui.GetContentRegionAvail().X));
                }
                break;
        }
    }

    private void DrawTripleTriadCard(Item item)
    {
        if (item.ItemAction.IsValid)
            DrawTripleTriadCard(item.ItemAction.Value.Data[0]);
    }

    private void DrawTripleTriadCard(uint cardId)
    {
        if (!ExcelService.TryGetRow<TripleTriadCard>(cardId, out var card))
            return;

        if (!ExcelService.TryGetRow<TripleTriadCardResident>(cardId, out var cardResident))
            return;

        DrawSeparator(marginTop: 3);

        var isEx = cardResident.UIPriority == 5;
        var order = (uint)cardResident.Order;
        var addonRowId = isEx ? 9773u : 9772;

        var infoText = $"{SeStringEvaluator.EvaluateFromAddon(addonRowId, [order]).ExtractText()} - {card.Name}";
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - ImGui.GetStyle().IndentSpacing + ImGui.GetContentRegionAvail().X / 2f - ImGui.CalcTextSize(infoText).X / 2f);
        ImGui.TextUnformatted(infoText);

        var cardSizeScaled = ImGuiHelpers.ScaledVector2(208, 256);
        var cardStartPosX = ImGui.GetCursorPosX() - ImGui.GetStyle().IndentSpacing + ImGui.GetContentRegionAvail().X / 2f - cardSizeScaled.X / 2f;
        var cardStartPos = new Vector2(cardStartPosX, ImGui.GetCursorPosY());

        // draw background
        ImGui.SetCursorPosX(cardStartPosX);
        TextureService.DrawPart("CardTripleTriad", 1, 0, cardSizeScaled);

        // draw card
        ImGui.SetCursorPos(cardStartPos);
        TextureService.DrawIcon(87000 + cardId, cardSizeScaled);

        // draw numbers
        using var font = TripleTriadNumberFont.Push();

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
            TextureService.DrawPart("CardTripleTriad", 1, partIndex, typeSize);
        }

        // functions

        void DrawStar(StarPosition pos)
        {
            var angleIncrement = 2 * MathF.PI / 5; // 5 = amount of stars
            var angle = (int)pos * angleIncrement - MathF.PI / 2;

            ImGui.SetCursorPos(starCenter + new Vector2(starRadius * MathF.Cos(angle), starRadius * MathF.Sin(angle)));
            TextureService.DrawPart("CardTripleTriad", 1, 1, starSize);
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
            ImGui.TextUnformatted(text);

        ImGui.SetCursorPos(position + ImGuiHelpers.ScaledVector2(1));
        using (outlineColor.Push(ImGuiCol.Text))
            ImGui.TextUnformatted(text);

        ImGui.SetCursorPos(position + ImGuiHelpers.ScaledVector2(1, -1));
        using (outlineColor.Push(ImGuiCol.Text))
            ImGui.TextUnformatted(text);

        ImGui.SetCursorPos(position + ImGuiHelpers.ScaledVector2(-1, 1));
        using (outlineColor.Push(ImGuiCol.Text))
            ImGui.TextUnformatted(text);

        // text
        ImGui.SetCursorPos(position);
        ImGui.TextUnformatted(text);
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
        if (playerState == null || playerState->IsLoaded == 0)
        {
            _facePaintIconCache.Add(dataId, iconId = 0);
            return false;
        }

        if (!ExcelService.TryFindRow<CustomHairMakeType>(t => t.Tribe.RowId == playerState->Tribe && t.Gender == playerState->Sex, out var hairMakeType))
        {
            _facePaintIconCache.Add(dataId, iconId = 0);
            return false;
        }

        if (!ExcelService.TryFindRow<CharaMakeCustomize>(row => row.IsPurchasable && row.Data == dataId && hairMakeType.CharaMakeStruct[7].SubMenuParam.Any(id => id == row.RowId), out var charaMakeCustomize))
        {
            _facePaintIconCache.Add(dataId, iconId = 0);
            return false;
        }

        _facePaintIconCache.Add(dataId, iconId = charaMakeCustomize.Icon);
        return true;
    }

    // TODO: move to TextureService?
    private Vector2 GetIconSize(uint iconId)
    {
        if (_iconSizeCache.TryGetValue(iconId, out var size))
            return size;

        var iconPath = TextureProvider.GetIconPath(iconId);
        if (string.IsNullOrEmpty(iconPath))
        {
            _iconSizeCache.Add(iconId, size = Vector2.Zero);
            return size;
        }

        var file = DataManager.GetFile<TexFile>(iconPath);
        _iconSizeCache.Add(iconId, size = file != null ? new Vector2(file.Header.Width, file.Header.Height) : Vector2.Zero);
        return size;
    }

    private static Vector2 ResizeToFit(Vector2 imageSize, float outerWidth)
    {
        if (imageSize.X <= outerWidth)
            return new Vector2(imageSize.X, imageSize.Y);

        var aspectRatio = imageSize.Y / imageSize.X;
        return new Vector2(outerWidth, outerWidth * aspectRatio);
    }

    public void DrawEventItemTooltip(EventItem item)
    {
        if (!TextureProvider.TryGetFromGameIcon((uint)item.Icon, out var tex) || !tex.TryGetWrap(out var icon, out _))
            return;

        using var id = ImRaii.PushId($"ItemTooltip{item.RowId}");

        using var tooltip = ImRaii.Tooltip();
        if (!tooltip) return;

        using var popuptable = ImRaii.Table("PopupTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.NoKeepColumnsVisible);
        if (!popuptable) return;

        var itemInnerSpacing = ImGui.GetStyle().ItemInnerSpacing * ImGuiHelpers.GlobalScale;
        var title = TextService.GetItemName(item.RowId);

        ImGui.TableSetupColumn("Icon", ImGuiTableColumnFlags.WidthFixed, 40 * ImGuiHelpers.GlobalScale + itemInnerSpacing.X);
        ImGui.TableSetupColumn("Text", ImGuiTableColumnFlags.WidthFixed, Math.Max(ImGui.CalcTextSize(title).X + itemInnerSpacing.X, 300 * ImGuiHelpers.GlobalScale));

        ImGui.TableNextColumn(); // Icon
        ImGui.Image(icon.ImGuiHandle, ImGuiHelpers.ScaledVector2(40));

        ImGui.TableNextColumn(); // Text
        using var indentSpacing = ImRaii.PushStyle(ImGuiStyleVar.IndentSpacing, itemInnerSpacing.X);
        using var indent = ImRaii.PushIndent(1);

        ImGui.TextUnformatted(title);

        if (item.Unknown2 != 0 && ExcelService.TryGetRow<EventItemCategory>(item.Unknown2, out var itemCategoy) && !itemCategoy.Unknown0.IsEmpty)
        {
            var text = itemCategoy.RowId switch
            {
                1 when item.Quest.IsValid && !item.Quest.Value.Name.IsEmpty => SeStringEvaluator.Evaluate(itemCategoy.Unknown0, [TextService.GetQuestName(item.Quest.RowId)]),
                _ => itemCategoy.Unknown0
            };

            if (!text.IsEmpty)
            {
                ImGuiUtils.PushCursorY(-3 * ImGuiHelpers.GlobalScale);
                using (ImRaii.PushColor(ImGuiCol.Text, (uint)Color.Grey))
                    ImGui.TextUnformatted(text.ExtractText());
            }
        }

        if (ExcelService.TryGetRow<EventItemHelp>(item.RowId, out var itemHelp) && !itemHelp.Description.IsEmpty)
        {
            DrawSeparator(marginTop: 1, marginBottom: 4);

            ImGuiHelpers.SafeTextWrapped(itemHelp.Description.ExtractText().StripSoftHyphen());
        }
    }

    public void DrawQuestTooltip(Quest quest)
    {
        using var id = ImRaii.PushId($"QuestTooltip{quest.RowId}");

        using var tooltip = ImRaii.Tooltip();
        if (!tooltip) return;

        using var popuptable = ImRaii.Table("PopupTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.NoKeepColumnsVisible);
        if (!popuptable) return;

        var itemInnerSpacing = ImGui.GetStyle().ItemInnerSpacing * ImGuiHelpers.GlobalScale;
        var title = TextService.GetQuestName(quest.RowId);

        ImGui.TableSetupColumn("Icon", ImGuiTableColumnFlags.WidthFixed, 40 * ImGuiHelpers.GlobalScale + itemInnerSpacing.X);
        ImGui.TableSetupColumn("Text", ImGuiTableColumnFlags.WidthFixed, Math.Max(ImGui.CalcTextSize(title).X + itemInnerSpacing.X, 300 * ImGuiHelpers.GlobalScale));

        ImGui.TableNextColumn(); // Icon

        var eventIconType = quest.EventIconType.IsValid
            ? quest.EventIconType.Value
            : ExcelService.GetSheet<EventIconType>().GetRow(1);

        var iconOffset = 1u;
        if (QuestManager.IsQuestComplete(quest.RowId))
            iconOffset = 5u;
        else if (quest.IsRepeatable)
            iconOffset = 2u;

        if (eventIconType.MapIconAvailable != 0 &&
            TextureProvider.TryGetFromGameIcon(eventIconType.MapIconAvailable + iconOffset, out var tex) &&
            tex.TryGetWrap(out var icon, out _))
        {
            ImGui.Image(icon.ImGuiHandle, ImGuiHelpers.ScaledVector2(40));
        }

        ImGui.TableNextColumn(); // Text
        using var indentSpacing = ImRaii.PushStyle(ImGuiStyleVar.IndentSpacing, itemInnerSpacing.X);
        using var indent = ImRaii.PushIndent(1);

        ImGui.TextUnformatted(title);

        var text = quest.JournalGenre.IsValid ? quest.JournalGenre.Value.Name.ExtractText() : null;
        if (!string.IsNullOrWhiteSpace(text))
        {
            ImGuiUtils.PushCursorY(-3 * ImGuiHelpers.GlobalScale);
            using (ImRaii.PushColor(ImGuiCol.Text, (uint)Color.Grey))
                ImGui.TextUnformatted(text);
        }

        var iconId = quest.Icon;

        var currentQuest = quest;
        while (iconId == 0 && currentQuest.PreviousQuest[0].RowId != 0)
        {
            currentQuest = currentQuest.PreviousQuest[0].Value;
            iconId = currentQuest.Icon;
        }

        if (iconId != 0 && TextureProvider.TryGetFromGameIcon(iconId, out var imageTex) && imageTex.TryGetWrap(out var image, out _))
        {
            DrawSeparator(marginTop: 1, marginBottom: 5);
            var newWidth = ImGui.GetContentRegionAvail().X;
            var ratio = newWidth / image.Width;
            var newHeight = image.Height * ratio;
            ImGui.Image(image.ImGuiHandle, new Vector2(newWidth, newHeight));
        }

        var questText = ExcelService.GetSheet<QuestText>($"quest/{(quest.RowId - 0x10000) / 100:000}/{quest.Id.ExtractText()}");
        var questSequence = QuestManager.GetQuestSequence((ushort)(quest.RowId - 0x10000));
        if (questSequence == 0xFF) questSequence = 1;
        for (var seq = questSequence == 0 ? 0 : 1; seq <= questSequence; seq++)
        {
            if (questText.TryGetFirst(kvRow => kvRow.LuaKey.ExtractText() == $"TEXT_{quest.Id.ExtractText().ToUpper()}_SEQ_{seq:00}", out var seqText) && !seqText.Text.IsEmpty)
            {
                DrawSeparator(marginTop: 1, marginBottom: 4);
                ImGuiHelpers.SeStringWrapped(SeStringEvaluator.Evaluate(seqText.Text));
            }
        }
    }

    // kinda meh, lol
    public void DrawAdventureTooltip(int index, Adventure adventure)
    {
        using var id = ImRaii.PushId($"AdventureTooltip{adventure.RowId}");

        using var tooltip = ImRaii.Tooltip();
        if (!tooltip) return;

        using var outerpopuptable = ImRaii.Table("OuterPopupTable", 1, ImGuiTableFlags.NoPadOuterX | ImGuiTableFlags.NoPadInnerX | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.NoKeepColumnsVisible);
        if (!outerpopuptable) return;

        var itemInnerSpacing = ImGui.GetStyle().ItemInnerSpacing * ImGuiHelpers.GlobalScale;
        var indexStr = $"#{index:000}";
        var title = adventure.Name.ExtractText();

        var leftColumnWidth = ImGui.CalcTextSize(indexStr).X + itemInnerSpacing.X;
        var rightColumnWidth = Math.Max(ImGui.CalcTextSize(title).X + itemInnerSpacing.X, 300 * ImGuiHelpers.GlobalScale);

        ImGui.TableSetupColumn("Table", ImGuiTableColumnFlags.WidthFixed, leftColumnWidth + rightColumnWidth + ImGui.GetStyle().CellPadding.X * 2); // ???

        ImGui.TableNextColumn(); // Table

        using var popuptable = ImRaii.Table("PopupTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.NoKeepColumnsVisible);
        if (!popuptable) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, leftColumnWidth);
        ImGui.TableSetupColumn("Title", ImGuiTableColumnFlags.WidthFixed, rightColumnWidth);

        ImGui.TableNextColumn(); // Index
        ImGui.TextUnformatted(indexStr);

        ImGui.TableNextColumn(); // Title
        using var indentSpacing = ImRaii.PushStyle(ImGuiStyleVar.IndentSpacing, itemInnerSpacing.X);
        using var indent = ImRaii.PushIndent(1);
        ImGui.TextUnformatted(title);

        var text = TextService.GetPlaceName(adventure.PlaceName.RowId);
        ImGuiUtils.PushCursorY(-3 * ImGuiHelpers.GlobalScale);
        using (ImRaii.PushColor(ImGuiCol.Text, (uint)Color.Grey))
            ImGui.TextUnformatted(text);

        indent.Dispose();
        indentSpacing.Dispose();
        popuptable.Dispose();

        var iconId = adventure.IconDiscovered;
        var iconDrawn = false;
        if (iconId != 0 && TextureProvider.TryGetFromGameIcon(iconId, out var imageTex) && imageTex.TryGetWrap(out var image, out _))
        {
            ImGuiUtils.PushCursorY(5 * ImGuiHelpers.GlobalScale);
            var newWidth = ImGui.GetContentRegionAvail().X;
            var ratio = newWidth / image.Width;
            var newHeight = image.Height * ratio;
            ImGui.Image(image.ImGuiHandle, new Vector2(newWidth, newHeight));
            iconDrawn = true;
        }

        ImGuiUtils.PushCursorY((iconDrawn ? -10 : 1) * ImGuiHelpers.GlobalScale);
        using var indentSpacing2 = ImRaii.PushStyle(ImGuiStyleVar.IndentSpacing, itemInnerSpacing.X);
        using var indent2 = ImRaii.PushIndent(1);
        ImGuiHelpers.SeStringWrapped(SeStringEvaluator.Evaluate(adventure.Description));
    }

    private static void DrawSeparator(float marginTop = 2, float marginBottom = 5)
    {
        ImGuiUtils.PushCursorY(marginTop * ImGuiHelpers.GlobalScale);
        var pos = ImGui.GetCursorScreenPos();
        ImGui.GetWindowDrawList().AddLine(pos, pos + new Vector2(ImGui.GetContentRegionAvail().X, 0), ImGui.GetColorU32(ImGuiCol.Separator));
        ImGuiUtils.PushCursorY(marginBottom * ImGuiHelpers.GlobalScale);
    }
}
