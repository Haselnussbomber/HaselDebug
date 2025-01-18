using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Extensions.Sheets;
using HaselCommon.Extensions.Strings;
using HaselCommon.Game.Enums;
using HaselCommon.Graphics;
using HaselCommon.Gui;
using HaselCommon.Services;
using HaselCommon.Services.SeStringEvaluation;
using HaselCommon.Sheets;
using HaselDebug.Services;
using HaselDebug.Windows.ItemTooltips;
using ImGuiNET;
using Lumina.Data.Files;
using Lumina.Excel;
using Lumina.Excel.Sheets;
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
    IDalamudPluginInterface PluginInterface,
    TripleTriadNumberFontManager TripleTriadNumberFontManager,
    SeStringEvaluatorService SeStringEvaluator) : IDisposable
{
    private readonly Dictionary<uint, Vector2?> _iconSizeCache = [];
    private readonly Lazy<IFontHandle> _tripleTriadNumberFont = new(() => PluginInterface.UiBuilder.FontAtlas.NewGameFontHandle(new GameFontStyle(GameFontFamily.MiedingerMid, 208f / 10f)));

    private TripleTriadCardTooltip? TripleTriadCardTooltip;

    public void Dispose()
    {
        if (_tripleTriadNumberFont.IsValueCreated)
            _tripleTriadNumberFont.Value.Dispose();

        TripleTriadCardTooltip?.Dispose();
        TripleTriadCardTooltip = null;

        GC.SuppressFinalize(this);
    }

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
            builder.AddTryOn(item.AsRef());
            builder.AddItemFinder(item.RowId);
            builder.AddCopyItemName(item.RowId);
            builder.AddItemSearch(item.AsRef());
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

        var category = item.ItemUICategory.IsValid ? item.ItemUICategory.Value.Name.ExtractText().StripSoftHypen() : null;
        if (!string.IsNullOrEmpty(category))
        {
            ImGuiUtils.PushCursorY(-3 * ImGuiHelpers.GlobalScale);
            using (ImRaii.PushColor(ImGuiCol.Text, (uint)Color.Grey))
                ImGui.TextUnformatted(category);
        }

        var description = descriptionOverride ?? (!item.Description.IsEmpty ? item.Description.ExtractText().StripSoftHypen() : null);
        if (!string.IsNullOrEmpty(description))
        {
            DrawSeparator(marginTop: 1, marginBottom: 4);

            ImGuiHelpers.SafeTextWrapped(description);
        }

        if (item.ItemAction.Value.Type == (uint)ItemActionType.Mount)
        {
            if (ExcelService.TryGetRow<Mount>(item.ItemAction.Value!.Data[0], out var mount))
            {
                TextureService.DrawIcon(64000 + mount.Icon, 192);
            }
        }
        else if (item.ItemAction.Value.Type == (uint)ItemActionType.Companion)
        {
            if (ExcelService.TryGetRow<Companion>(item.ItemAction.Value!.Data[0], out var companion))
            {
                TextureService.DrawIcon(64000 + companion.Icon, 192);
            }
        }
        else if (item.ItemAction.Value.Type == (uint)ItemActionType.Ornament)
        {
            if (ExcelService.TryGetRow<Ornament>(item.ItemAction.Value!.Data[0], out var ornament))
            {
                TextureService.DrawIcon(59000 + ornament.Icon, 192);
            }
        }
        else if (item.ItemAction.Value.Type == (uint)ItemActionType.UnlockLink && item.ItemAction.Value.Data[1] == 5211) // Emotes
        {
            if (ExcelService.TryGetRow<Emote>(item.ItemAction.Value!.Data[2], out var emote))
            {
                TextureService.DrawIcon(emote.Icon, 80);
            }
        }
        else if (item.ItemAction.Value.Type == (uint)ItemActionType.UnlockLink && item.ItemAction.Value.Data[1] == 4659) // Hairstyles
        {
            var playerState = PlayerState.Instance();
            if (playerState->IsLoaded == 1 &&
                ExcelService.TryFindRow<CustomHairMakeType>(t => t.Tribe.RowId == playerState->Tribe && t.Gender == playerState->Sex, out var hairMakeType) &&
                ExcelService.TryFindRow<CharaMakeCustomize>(row => row.IsPurchasable && row.Data == item.ItemAction.Value.Data[0] && hairMakeType.CharaMakeStruct[0].SubMenuParam.Any(id => id == row.RowId), out var charaMakeCustomize))
            {
                TextureService.DrawIcon(charaMakeCustomize.Icon, 80);
            }
        }
        else if (item.ItemAction.Value.Type == (uint)ItemActionType.UnlockLink && item.ItemAction.Value.Data[1] == 9390) // Face Paints
        {
            var playerState = PlayerState.Instance();
            if (playerState->IsLoaded == 1 &&
                ExcelService.TryFindRow<CustomHairMakeType>(t => t.Tribe.RowId == playerState->Tribe && t.Gender == playerState->Sex, out var hairMakeType) &&
                ExcelService.TryFindRow<CharaMakeCustomize>(row => row.IsPurchasable && row.Data == item.ItemAction.Value.Data[0] && hairMakeType.CharaMakeStruct[7].SubMenuParam.Any(id => id == row.RowId), out var charaMakeCustomize))
            {
                TextureService.DrawIcon(charaMakeCustomize.Icon, 80);
            }
        }
        else if (item.ItemAction.Value.Type == (uint)ItemActionType.TripleTriadCard)
        {
            if (ExcelService.TryGetRow<TripleTriadCardResident>(item.ItemAction.Value.Data[0], out var residentRow) &&
                ExcelService.TryGetRow<TripleTriadCardObtain>(residentRow.AcquisitionType, out var obtainRow) &&
                obtainRow.Unknown1 != 0)
            {
                DrawSeparator();
                TextureService.DrawIcon(obtainRow.Unknown0, 40 * ImGuiHelpers.GlobalScale);
                ImGui.SameLine();
                ImGuiHelpers.SafeTextWrapped(SeStringEvaluator.EvaluateFromAddon(obtainRow.Unknown1, new SeStringContext()
                {
                    LocalParameters = [
                        residentRow.Acquisition.RowId,
                        residentRow.Location.RowId
                    ]
                }).ExtractText().StripSoftHypen());
            }

            DrawSeparator(marginTop: 3);

            TripleTriadCardTooltip ??= new TripleTriadCardTooltip(TextureService, ExcelService, SeStringEvaluator, TripleTriadNumberFontManager);
            TripleTriadCardTooltip.MarginTop = ImGui.GetCursorPosY();
            TripleTriadCardTooltip.MarginLeft = ImGui.GetContentRegionAvail().X / 2f - 208 * ImGuiHelpers.GlobalScale / 2f + ImGui.GetCursorPosX() - itemInnerSpacing.X;
            TripleTriadCardTooltip?.SetItem(item);
            TripleTriadCardTooltip?.CalculateLayout();
            TripleTriadCardTooltip?.Update();
            TripleTriadCardTooltip?.Draw();
        }
        else if (item.ItemUICategory.RowId == 95) // Paintings
        {
            if (ExcelService.TryGetRow<Picture>(item.AdditionalData.RowId, out var picture))
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
                    TextureService.DrawIcon(pictureId, (Vector2)size * 0.5f);
                }
            }
        }
        else if (item.ItemAction.Value.Type == (uint)ItemActionType.UnlockLink && ExcelService.TryFindRow<CharaMakeCustomize>(row => row.HintItem.RowId == item.RowId, out _)) // Hairstyles etc.
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

            var hairStyleIconId = ItemService.GetHairstyleIconId(item.RowId, tribeId, sex);
            if (hairStyleIconId != 0)
                TextureService.DrawIcon(hairStyleIconId, 192);
        }
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
                1 when item.Quest.IsValid && !item.Quest.Value.Name.IsEmpty => SeStringEvaluator.Evaluate(itemCategoy.Unknown0, new()
                {
                    LocalParameters = [TextService.GetQuestName(item.Quest.RowId)]
                }),
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

            ImGuiHelpers.SafeTextWrapped(itemHelp.Description.ExtractText().StripSoftHypen());
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
