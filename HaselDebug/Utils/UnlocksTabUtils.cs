using System.Collections.Generic;
using System.Numerics;
using System.Xml.Linq;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using HaselCommon.Extensions.Sheets;
using HaselCommon.Game.Enums;
using HaselCommon.Graphics;
using HaselCommon.Gui;
using HaselCommon.Services;
using ImGuiNET;
using Lumina.Data.Files;
using Lumina.Excel.Sheets;
using Companion = Lumina.Excel.Sheets.Companion;
using Ornament = Lumina.Excel.Sheets.Ornament;

namespace HaselDebug.Utils;

public unsafe class UnlocksTabUtils(
    ExcelService ExcelService,
    TextService TextService,
    TextureService TextureService,
    ItemService ItemService,
    ImGuiContextMenuService ImGuiContextMenuService,
    IDataManager DataManager,
    ITextureProvider TextureProvider,
    IDalamudPluginInterface pluginInterface) : IDisposable
{
    private readonly ExcelService excelService = ExcelService;
    private readonly TextService textService = TextService;
    private readonly TextureService textureService = TextureService;
    private readonly ItemService itemService = ItemService;
    private readonly ImGuiContextMenuService imGuiContextMenuService = ImGuiContextMenuService;

    private readonly Dictionary<uint, Vector2?> _iconSizeCache = [];
    private readonly Lazy<IFontHandle> _tripleTriadNumberFont = new(() => pluginInterface.UiBuilder.FontAtlas.NewGameFontHandle(new GameFontStyle(GameFontFamily.MiedingerMid, 208f / 10f)));

    public void Dispose()
    {
        if (_tripleTriadNumberFont.IsValueCreated)
            _tripleTriadNumberFont.Value.Dispose();

        GC.SuppressFinalize(this);
    }

    public bool DrawSelectableItem(Item item, ImGuiId id, bool drawIcon = true, bool isHq = false, float? iconSize = null)
    {
        var itemName = textService.GetItemName(item.RowId);
        var isHovered = false;
        iconSize ??= ImGui.GetTextLineHeight();

        if (drawIcon)
        {
            textureService.DrawIcon(item.Icon, isHq, (float)iconSize);
            isHovered |= ImGui.IsItemHovered();
            ImGui.SameLine();
        }
        var clicked = ImGui.Selectable(itemName);
        isHovered |= ImGui.IsItemHovered();

        if (isHovered && !ImGui.IsKeyDown(ImGuiKey.LeftAlt))
        {
            if (item.ItemAction.Value.Type == (uint)ItemActionType.Mount)
            {
                if (excelService.TryGetRow<Mount>(item.ItemAction.Value!.Data[0], out var mount))
                {
                    using var tooltip = ImRaii.Tooltip();
                    textureService.DrawIcon(64000 + mount.Icon, 192);
                }
            }
            else if (item.ItemAction.Value.Type == (uint)ItemActionType.Companion)
            {
                if (excelService.TryGetRow<Companion>(item.ItemAction.Value!.Data[0], out var companion))
                {
                    using var tooltip = ImRaii.Tooltip();
                    textureService.DrawIcon(64000 + companion.Icon, 192);
                }
            }
            else if (item.ItemAction.Value.Type == (uint)ItemActionType.Ornament)
            {
                if (excelService.TryGetRow<Ornament>(item.ItemAction.Value!.Data[0], out var ornament))
                {
                    using var tooltip = ImRaii.Tooltip();
                    textureService.DrawIcon(59000 + ornament.Icon, 192);
                }
            }
            else if (item.ItemAction.Value.Type == (uint)ItemActionType.UnlockLink && item.ItemAction.Value.Data[1] == 5211) // Emotes
            {
                if (excelService.TryGetRow<Emote>(item.ItemAction.Value!.Data[2], out var emote))
                {
                    using var tooltip = ImRaii.Tooltip();
                    textureService.DrawIcon(emote.Icon, 80);
                }
            }
            else if (item.ItemAction.Value.Type == (uint)ItemActionType.TripleTriadCard)
            {
                var cardId = item.ItemAction.Value!.Data[0];
                if (excelService.TryGetRow<TripleTriadCard>(cardId, out var cardRow) && excelService.TryGetRow<TripleTriadCardResident>(cardId, out var cardResident))
                {
                    var cardRarity = cardResident.TripleTriadCardRarity.Value!;

                    var cardSize = new Vector2(208, 256);
                    var cardSizeScaled = ImGuiHelpers.ScaledVector2(cardSize.X, cardSize.Y);

                    using var tooltip = ImRaii.Tooltip();
                    ImGui.TextUnformatted($"{(cardResident.TripleTriadCardRarity.RowId == 5 ? "Ex" : "No")}. {cardResident.Order} - {cardRow.Name}");
                    var pos = ImGui.GetCursorPos();
                    textureService.DrawPart("CardTripleTriad", 1, 0, cardSizeScaled);
                    ImGui.SetCursorPos(pos);
                    textureService.DrawIcon(87000 + cardRow.RowId, cardSizeScaled);

                    var starSize = cardSizeScaled.Y / 10f;
                    var starCenter = pos + new Vector2(starSize);
                    var starRadius = starSize / 1.666f;

                    static Vector2 GetPosOnCircle(float radius, int index, int numberOfPoints)
                    {
                        var angleIncrement = 2 * MathF.PI / numberOfPoints;
                        var angle = index * angleIncrement - MathF.PI / 2;
                        return new Vector2(
                            radius * MathF.Cos(angle),
                            radius * MathF.Sin(angle)
                        );
                    }

                    if (cardRarity.Stars >= 1)
                    {
                        ImGui.SetCursorPos(starCenter + GetPosOnCircle(starRadius, 0, 5)); // top
                        textureService.DrawPart("CardTripleTriad", 1, 1, starSize);

                        if (cardRarity.Stars >= 2)
                        {
                            ImGui.SetCursorPos(starCenter + GetPosOnCircle(starRadius, 4, 5)); // left
                            textureService.DrawPart("CardTripleTriad", 1, 1, starSize);
                        }
                        if (cardRarity.Stars >= 3)
                        {
                            ImGui.SetCursorPos(starCenter + GetPosOnCircle(starRadius, 1, 5)); // right
                            textureService.DrawPart("CardTripleTriad", 1, 1, starSize);
                        }
                        if (cardRarity.Stars >= 4)
                        {
                            ImGui.SetCursorPos(starCenter + GetPosOnCircle(starRadius, 3, 5)); // bottom right
                            textureService.DrawPart("CardTripleTriad", 1, 1, starSize);
                        }
                        if (cardRarity.Stars >= 5)
                        {
                            ImGui.SetCursorPos(starCenter + GetPosOnCircle(starRadius, 2, 5)); // bottom left
                            textureService.DrawPart("CardTripleTriad", 1, 1, starSize);
                        }
                    }

                    // type
                    if (cardResident.TripleTriadCardType.RowId != 0)
                    {
                        ImGui.SetCursorPos(pos + new Vector2(cardSize.X, 0) - new Vector2(starSize * 1.5f, -starSize / 2f));
                        textureService.DrawPart("CardTripleTriad", 1, cardResident.TripleTriadCardType.RowId + 2, starSize);
                    }

                    // numbers
                    using var font = _tripleTriadNumberFont.Value.Push();

                    var numberText = $"{cardResident.Top:X}";
                    var numberTextSize = ImGui.CalcTextSize(numberText);
                    var numberTextWidth = numberTextSize.X / 1.333f;
                    var numberCenter = pos + new Vector2(cardSizeScaled.X / 2f - numberTextWidth, cardSizeScaled.Y - numberTextSize.Y * 2f);

                    static void DrawNumberText(Vector2 numberCenter, float numberTextWidth, int posIndex, string numberText)
                    {
                        // shadow
                        ImGui.SetCursorPos(numberCenter + GetPosOnCircle(numberTextWidth, posIndex, 4) + ImGuiHelpers.ScaledVector2(2));
                        using (ImRaii.PushColor(ImGuiCol.Text, 0xFF000000))
                            ImGui.TextUnformatted(numberText);

                        // text
                        ImGui.SetCursorPos(numberCenter + GetPosOnCircle(numberTextWidth, posIndex, 4));
                        ImGui.TextUnformatted(numberText);
                    }

                    DrawNumberText(numberCenter, numberTextWidth, 0, numberText); // top
                    DrawNumberText(numberCenter, numberTextWidth, 1, $"{cardResident.Right:X}"); // right
                    DrawNumberText(numberCenter, numberTextWidth, 2, $"{cardResident.Left:X}"); // left
                    DrawNumberText(numberCenter, numberTextWidth, 3, $"{cardResident.Bottom:X}"); // bottom
                }
            }
            else if (item.ItemUICategory.RowId == 95) // Paintings
            {
                if (excelService.TryGetRow<Picture>(item.AdditionalData.RowId, out var picture))
                {
                    var pictureId = (uint)picture.Image;

                    if (!_iconSizeCache.TryGetValue(pictureId, out var size))
                    {
                        var iconPath = TextureProvider.GetIconPath(pictureId);
                        if (string.IsNullOrEmpty(iconPath))
                        {
                            _iconSizeCache.Add(pictureId, null);
                        }
                        else
                        {
                            var file = DataManager.GetFile<TexFile>(iconPath);
                            _iconSizeCache.Add(pictureId, size = file != null ? new(file.Header.Width, file.Header.Height) : null);
                        }
                    }

                    if (size != null)
                    {
                        using var tooltip = ImRaii.Tooltip();
                        textureService.DrawIcon(pictureId, (Vector2)size * 0.5f);
                    }
                }
            }
            else if (item.ItemAction.Value.Type == (uint)ItemActionType.UnlockLink && excelService.TryFindRow<CharaMakeCustomize>(row => row.HintItem.RowId == item.RowId, out _)) // Hairstyles etc.
            {
                byte tribeId = 1;
                byte sex = 1;
                unsafe
                {
                    var character = Control.GetLocalPlayer();
                    if (character != null)
                    {
                        tribeId = character->DrawData.CustomizeData.Tribe;
                        sex = character->DrawData.CustomizeData.Sex;
                    }
                }

                var hairStyleIconId = itemService.GetHairstyleIconId(item.RowId, tribeId, sex);
                if (hairStyleIconId != 0)
                {
                    using var tooltip = ImRaii.Tooltip();
                    textureService.DrawIcon(hairStyleIconId, 192);
                }
            }
            else
            {
                using var tooltip = ImRaii.Tooltip();
                textureService.DrawIcon(item.Icon, 64);
            }
        }

        imGuiContextMenuService.Draw($"##{id}_ItemContextMenu{item.RowId}_IconTooltip", builder =>
        {
            builder.AddTryOn(item.AsRef());
            builder.AddItemFinder(item.RowId);
            builder.AddCopyItemName(item.RowId);
            builder.AddItemSearch(item.AsRef());
            builder.AddOpenOnGarlandTools("item", item.RowId);
        });

        if (itemService.IsUnlockable(item) && itemService.IsUnlocked(item))
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

        var itemInnerSpacing = ImGui.GetStyle().ItemInnerSpacing;

        ImGui.TableSetupColumn("Icon", ImGuiTableColumnFlags.WidthFixed, 40 + itemInnerSpacing.X);
        ImGui.TableSetupColumn("Text", ImGuiTableColumnFlags.WidthFixed, Math.Max(ImGui.CalcTextSize(title).X + itemInnerSpacing.X, 300));

        ImGui.TableNextColumn(); // Icon
        ImGui.Image(icon.ImGuiHandle, new Vector2(40));

        ImGui.TableNextColumn(); // Text
        using var indentSpacing = ImRaii.PushStyle(ImGuiStyleVar.IndentSpacing, itemInnerSpacing.X);
        using var indent = ImRaii.PushIndent(1);

        ImGui.TextUnformatted(title);

        if (!string.IsNullOrEmpty(category))
        {
            ImGuiUtils.PushCursorY(-3);
            using (ImRaii.PushColor(ImGuiCol.Text, (uint)Color.Grey))
                ImGui.TextUnformatted(category);
        }

        if (!string.IsNullOrEmpty(description))
        {
            ImGuiUtils.PushCursorY(1);

            // separator
            var pos = ImGui.GetCursorScreenPos();
            ImGui.GetWindowDrawList().AddLine(pos, pos + new Vector2(ImGui.GetContentRegionAvail().X, 0), ImGui.GetColorU32(ImGuiCol.Separator));
            ImGuiUtils.PushCursorY(4);

            ImGuiHelpers.SafeTextWrapped(description);
        }
    }
}
