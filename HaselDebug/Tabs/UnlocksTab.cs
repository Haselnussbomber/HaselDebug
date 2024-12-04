using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using HaselCommon.Extensions.Sheets;
using HaselCommon.Game.Enums;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Services;
using ImGuiNET;
using Lumina.Data.Files;
using Lumina.Excel.Sheets;
using Companion = Lumina.Excel.Sheets.Companion;
using Ornament = Lumina.Excel.Sheets.Ornament;

namespace HaselDebug.Tabs;

public unsafe partial class UnlocksTab : DebugTab, IDisposable
{
    private readonly Dictionary<uint, Vector2?> _iconSizeCache = [];
    private readonly Lazy<IFontHandle> _tripleTriadNumberFont;

    public override bool DrawInChild => false;

    private readonly IDalamudPluginInterface PluginInterface;
    private readonly DebugRenderer DebugRenderer;
    private readonly ExcelService ExcelService;
    private readonly TextService TextService;
    private readonly MapService MapService;
    private readonly IDataManager DataManager;
    private readonly ItemService ItemService;
    private readonly TextureService TextureService;
    private readonly ITextureProvider TextureProvider;
    private readonly ImGuiContextMenuService ImGuiContextMenuService;

    public UnlocksTab(
        IDalamudPluginInterface pluginInterface,
        DebugRenderer debugRenderer,
        ExcelService excelService,
        TextService textService,
        MapService mapService,
        IDataManager dataManager,
        ItemService itemService,
        TextureService textureService,
        ITextureProvider textureProvider,
        ImGuiContextMenuService imGuiContextMenuService)
    {
        PluginInterface = pluginInterface;
        DebugRenderer = debugRenderer;
        ExcelService = excelService;
        TextService = textService;
        MapService = mapService;
        DataManager = dataManager;
        ItemService = itemService;
        TextureService = textureService;
        TextureProvider = textureProvider;
        ImGuiContextMenuService = imGuiContextMenuService;

        _tripleTriadNumberFont = new(() => PluginInterface.UiBuilder.FontAtlas.NewGameFontHandle(new GameFontStyle(GameFontFamily.MiedingerMid, 208f / 10f)));

        TextService.LanguageChanged += OnLanguageChanged;

        Update();
    }

    public void Dispose()
    {
        TextService.LanguageChanged -= OnLanguageChanged;

        if (_tripleTriadNumberFont.IsValueCreated)
            _tripleTriadNumberFont.Value.Dispose();

        GC.SuppressFinalize(this);
    }

    private void OnLanguageChanged(string langCode)
    {
        Update();
    }

    private void Update()
    {
        UpdateUnlockLinks();
        UpdateCutscenes();
        //UpdateStoreItems();
    }

    public override void Draw()
    {
        using var hostchild = ImRaii.Child("UnlocksTabChild", new Vector2(-1), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
        if (!hostchild) return;

        using var tabs = ImRaii.TabBar("UnlocksTabBar");
        if (!tabs) return;

        DrawAchievements();
        DrawAetherCurrents();
        DrawBardings();
        DrawCutscenes();
        DrawEmotes();
        DrawFashionAccessories();
        DrawGlasses();
        DrawMinions();
        DrawMounts();
        DrawOrchestrion();
        DrawRecipes();
        DrawSightseeingLog();
        //DrawStoreItems();
        DrawTitles();
        DrawUnlockLinks();
    }

    public unsafe bool DrawSelectableItem(Item item, ImGuiId id, bool drawIcon = true, bool isHq = false, float? iconSize = null)
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
            if (item.ItemAction.Value.Type == (uint)ItemActionType.Mount)
            {
                if (ExcelService.TryGetRow<Mount>(item.ItemAction.Value!.Data[0], out var mount))
                {
                    using var tooltip = ImRaii.Tooltip();
                    TextureService.DrawIcon(64000 + mount.Icon, 192);
                }
            }
            else if (item.ItemAction.Value.Type == (uint)ItemActionType.Companion)
            {
                if (ExcelService.TryGetRow<Companion>(item.ItemAction.Value!.Data[0], out var companion))
                {
                    using var tooltip = ImRaii.Tooltip();
                    TextureService.DrawIcon(64000 + companion.Icon, 192);
                }
            }
            else if (item.ItemAction.Value.Type == (uint)ItemActionType.Ornament)
            {
                if (ExcelService.TryGetRow<Ornament>(item.ItemAction.Value!.Data[0], out var ornament))
                {
                    using var tooltip = ImRaii.Tooltip();
                    TextureService.DrawIcon(59000 + ornament.Icon, 192);
                }
            }
            else if (item.ItemAction.Value.Type == (uint)ItemActionType.UnlockLink && item.ItemAction.Value.Data[1] == 5211) // Emotes
            {
                if (ExcelService.TryGetRow<Emote>(item.ItemAction.Value!.Data[2], out var emote))
                {
                    using var tooltip = ImRaii.Tooltip();
                    TextureService.DrawIcon(emote.Icon, 80);
                }
            }
            else if (item.ItemAction.Value.Type == (uint)ItemActionType.TripleTriadCard)
            {
                var cardId = item.ItemAction.Value!.Data[0];
                if (ExcelService.TryGetRow<TripleTriadCard>(cardId, out var cardRow) && ExcelService.TryGetRow<TripleTriadCardResident>(cardId, out var cardResident))
                {
                    var cardRarity = cardResident.TripleTriadCardRarity.Value!;

                    var cardSize = new Vector2(208, 256);
                    var cardSizeScaled = ImGuiHelpers.ScaledVector2(cardSize.X, cardSize.Y);

                    using var tooltip = ImRaii.Tooltip();
                    ImGui.TextUnformatted($"{(cardResident.TripleTriadCardRarity.RowId == 5 ? "Ex" : "No")}. {cardResident.Order} - {cardRow.Name}");
                    var pos = ImGui.GetCursorPos();
                    TextureService.DrawPart("CardTripleTriad", 1, 0, cardSizeScaled);
                    ImGui.SetCursorPos(pos);
                    TextureService.DrawIcon(87000 + cardRow.RowId, cardSizeScaled);

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
                        TextureService.DrawPart("CardTripleTriad", 1, 1, starSize);

                        if (cardRarity.Stars >= 2)
                        {
                            ImGui.SetCursorPos(starCenter + GetPosOnCircle(starRadius, 4, 5)); // left
                            TextureService.DrawPart("CardTripleTriad", 1, 1, starSize);
                        }
                        if (cardRarity.Stars >= 3)
                        {
                            ImGui.SetCursorPos(starCenter + GetPosOnCircle(starRadius, 1, 5)); // right
                            TextureService.DrawPart("CardTripleTriad", 1, 1, starSize);
                        }
                        if (cardRarity.Stars >= 4)
                        {
                            ImGui.SetCursorPos(starCenter + GetPosOnCircle(starRadius, 3, 5)); // bottom right
                            TextureService.DrawPart("CardTripleTriad", 1, 1, starSize);
                        }
                        if (cardRarity.Stars >= 5)
                        {
                            ImGui.SetCursorPos(starCenter + GetPosOnCircle(starRadius, 2, 5)); // bottom left
                            TextureService.DrawPart("CardTripleTriad", 1, 1, starSize);
                        }
                    }

                    // type
                    if (cardResident.TripleTriadCardType.RowId != 0)
                    {
                        ImGui.SetCursorPos(pos + new Vector2(cardSize.X, 0) - new Vector2(starSize * 1.5f, -starSize / 2f));
                        TextureService.DrawPart("CardTripleTriad", 1, cardResident.TripleTriadCardType.RowId + 2, starSize);
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
                        using var tooltip = ImRaii.Tooltip();
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
                {
                    using var tooltip = ImRaii.Tooltip();
                    TextureService.DrawIcon(hairStyleIconId, 192);
                }
            }
            else
            {
                using var tooltip = ImRaii.Tooltip();
                TextureService.DrawIcon(item.Icon, 64);
            }
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
}
