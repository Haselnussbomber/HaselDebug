using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Graphics;
using HaselCommon.Gui;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Sheets;
using ImGuiNET;
using Lumina.Excel.Sheets;
using PlayerState = FFXIVClientStructs.FFXIV.Client.Game.UI.PlayerState;

namespace HaselDebug.Tabs;

public unsafe class UnlocksTabOutfits : DebugTab, ISubTab<UnlocksTab>, IDisposable
{
    private const float IconSize = 32;

    private readonly LanguageProvider _languageProvider;

    private readonly List<uint> _prismBoxItemIds = [];
    private bool _prismBoxBackedUp = false;
    private DateTime _prismBoxLastCheck = DateTime.MinValue;
    private readonly OutfitsTable _table;

    public UnlocksTabOutfits(
        ExcelService excelService,
        ImGuiContextMenuService imGuiContextMenuService,
        ItemService itemService,
        TextService textService,
        TextureService textureService,
        ITextureProvider textureProvider,
        LanguageProvider languageProvider,
        GlobalScaleObserver globalScaleObserver)
    {
        _languageProvider = languageProvider;

        _table = new(
            excelService,
            itemService,
            textService,
            languageProvider,
            textureService,
            textureProvider,
            imGuiContextMenuService,
            globalScaleObserver,
            _prismBoxItemIds);

        _languageProvider.LanguageChanged += OnLanguageChanged;
    }

    public void Dispose()
    {
        _table.Dispose();
        _languageProvider.LanguageChanged -= OnLanguageChanged;
        GC.SuppressFinalize(this);
    }

    private void OnLanguageChanged(string langCode)
    {
        _table.IsSortDirty = true;
    }

    public override string Title => "Outfits";

    public override void Draw()
    {
        var playerState = PlayerState.Instance();
        if (playerState->IsLoaded != 1)
        {
            ImGui.TextUnformatted("PlayerState not loaded.");

            // in case of logout
            if (_prismBoxBackedUp)
            {
                _prismBoxBackedUp = false;
            }

            return;
        }

        var mirageManager = MirageManager.Instance();
        if (!mirageManager->PrismBoxLoaded)
        {
            if (_prismBoxBackedUp)
            {
                using (Color.Yellow.Push(ImGuiCol.Text))
                    ImGui.TextUnformatted("PrismBox not loaded. Using cache.");
            }
            else
            {
                using (Color.Red.Push(ImGuiCol.Text))
                    ImGui.TextUnformatted("PrismBox not loaded.");
            }
        }
        else
        {
            var hasChanges = false;

            if (DateTime.Now - _prismBoxLastCheck > TimeSpan.FromSeconds(2))
            {
                hasChanges = !CollectionsMarshal.AsSpan(_prismBoxItemIds).SequenceEqual(mirageManager->PrismBoxItemIds);
                _prismBoxLastCheck = DateTime.Now;
            }

            if (!_prismBoxBackedUp || hasChanges)
            {
                _prismBoxItemIds.Clear();
                _prismBoxItemIds.AddRange(mirageManager->PrismBoxItemIds);
                _prismBoxBackedUp = true;
            }
        }

        var numCollectedSets = _table.Rows.Count(row => _prismBoxItemIds.Contains(row.RowId));
        ImGui.TextUnformatted($"{numCollectedSets} out of {_table.Rows.Count} filtered sets collected");

        _table.Draw();
    }
}

public class OutfitsTable : Table<CustomMirageStoreSetItem>, IDisposable
{
    private const float IconSize = 32;

    private readonly GlobalScaleObserver _globalScaleObserver;

    public OutfitsTable(
        ExcelService excelService,
        ItemService itemService,
        TextService textService,
        LanguageProvider languageProvider,
        TextureService textureService,
        ITextureProvider textureProvider,
        ImGuiContextMenuService imGuiContextMenuService,
        GlobalScaleObserver globalScaleObserver,
        List<uint> prismBoxItemIds) : base("OutfitsTable")
    {
        _globalScaleObserver = globalScaleObserver;

        var cabinetSheet = excelService.GetSheet<Cabinet>().Select(row => row.Item.RowId).ToArray();
        foreach (var row in excelService.GetSheet<CustomMirageStoreSetItem>())
        {
            // is valid set item
            if (row.RowId == 0 || !row.Set.IsValid)
                continue;

            // has items
            if (row.Items.All(i => i.RowId == 0))
                continue;

            // does not only consist of cabinet items
            if (row.Items.Where(i => i.RowId != 0).All(i => cabinetSheet.Contains(i.RowId)))
                continue;

            // does not only consist of items that can't be worn
            if (row.Items.Where(i => i.RowId != 0).All(i => !itemService.CanTryOn(i.Value)))
                continue;

            Rows.Add(row);
        }

        Columns = [
            new RowIdColumn(),
            new SetColumn(textService, languageProvider, textureService, textureProvider, imGuiContextMenuService, prismBoxItemIds),
            new ItemsColumn(textService, textureService, textureProvider, imGuiContextMenuService, prismBoxItemIds),
        ];

        Flags |= ImGuiTableFlags.Sortable | ImGuiTableFlags.SortTristate;
        LineHeight = IconSize * ImGuiHelpers.GlobalScale + ImGui.GetStyle().ItemSpacing.Y; // I honestly don't know why using ItemSpacing here works

        _globalScaleObserver.ScaleChange += OnScaleChange;
    }

    public void Dispose()
    {
        _globalScaleObserver.ScaleChange -= OnScaleChange;
        GC.SuppressFinalize(this);
    }

    private void OnScaleChange(float scale)
    {
        LineHeight = IconSize * scale + ImGui.GetStyle().ItemSpacing.Y;
    }

    protected override void SortTristate()
    {
        Rows.Sort((a, b) => a.Set.RowId.CompareTo(b.Set.RowId));
    }

    private class RowIdColumn : Column<CustomMirageStoreSetItem>
    {
        public RowIdColumn()
        {
            Label = "RowId";
            Flags = ImGuiTableColumnFlags.WidthFixed;
            IsSearchable = true;
            Width = 60;
        }

        public override bool ShouldShow(CustomMirageStoreSetItem row)
        {
            return row.RowId.ToString().Contains(SearchQuery, StringComparison.InvariantCultureIgnoreCase);
        }

        public override void DrawColumn(CustomMirageStoreSetItem row)
        {
            //ImGuiUtils.PushCursorX(ImGui.GetContentRegionAvail().X / 2f - ImGui.CalcTextSize(row.RowId.ToString()).X / 2f);
            ImGuiUtils.PushCursorY(ImGui.GetTextLineHeight() / 2f);
            ImGui.TextUnformatted(row.RowId.ToString());
        }

        public override int Compare(CustomMirageStoreSetItem a, CustomMirageStoreSetItem b)
        {
            return a.RowId.CompareTo(b.RowId);
        }
    }

    private class SetColumn : Column<CustomMirageStoreSetItem>
    {
        private readonly TextService _textService;
        private readonly LanguageProvider _languageProvider;
        private readonly TextureService _textureService;
        private readonly ITextureProvider _textureProvider;
        private readonly ImGuiContextMenuService _imGuiContextMenuService;
        private readonly List<uint> _prismBoxItemIds;

        public SetColumn(
            TextService textService,
            LanguageProvider languageProvider,
            TextureService textureService,
            ITextureProvider textureProvider,
            ImGuiContextMenuService imGuiContextMenuService,
            List<uint> prismBoxItemIds)
        {
            _textService = textService;
            _languageProvider = languageProvider;
            _textureService = textureService;
            _textureProvider = textureProvider;
            _imGuiContextMenuService = imGuiContextMenuService;
            _prismBoxItemIds = prismBoxItemIds;

            Label = "Set";
            Flags = ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultSort;
            IsSearchable = true;
            Width = 300;
        }

        public override bool ShouldShow(CustomMirageStoreSetItem row)
        {
            return _textService.GetItemName(row.RowId).Contains(SearchQuery, StringComparison.InvariantCultureIgnoreCase);
        }

        public override void DrawColumn(CustomMirageStoreSetItem row)
        {
            var isSetCollected = _prismBoxItemIds.Contains(row.RowId);
            /*
            ImGuiUtils.PushCursorY(ImGui.GetTextLineHeight() / 2f);
            */

            ImGui.Dummy(ImGuiHelpers.ScaledVector2(IconSize));
            ImGui.SameLine(0, 0);
            ImGuiUtils.PushCursorX(-IconSize * ImGuiHelpers.GlobalScale);
            _textureService.DrawIcon(
                row.Set.Value.Icon,
                false,
                new(IconSize * ImGuiHelpers.GlobalScale)
                {
                    TintColor = isSetCollected
                        ? Color.White
                        : (ImGui.IsItemHovered() || ImGui.IsPopupOpen($"###Set_{row.RowId}_Icon_ItemContextMenu")
                            ? Color.White : Color.Grey3)
                }
            );

            if (ImGui.IsItemHovered())
            {
                using var tooltip = ImRaii.Tooltip();
                if (_textureProvider.TryGetFromGameIcon(new(row.Set.Value.Icon), out var texture) && texture.TryGetWrap(out var textureWrap, out _))
                {
                    ImGui.Image(textureWrap.ImGuiHandle, new(textureWrap.Width, textureWrap.Height));
                    ImGui.SameLine();
                    ImGuiUtils.PushCursorY(textureWrap.Height / 2f - ImGui.GetTextLineHeight() / 2f);
                }
                ImGui.TextUnformatted(_textService.GetItemName(row.Set.RowId));
            }

            if (isSetCollected)
            {
                DrawCollectedCheckmark(_textureProvider);
            }

            _imGuiContextMenuService.Draw($"###Set_{row.RowId}_Icon_ItemContextMenu", builder =>
            {
                builder.AddTryOn(row.Set);
                builder.AddItemFinder(row.Set.RowId);
                builder.AddCopyItemName(row.Set.RowId);
                builder.AddItemSearch(row.Set);
                builder.AddOpenOnGarlandTools("item", row.Set.RowId);
            });

            ImGui.SameLine();
            ImGui.Selectable($"###SetName_{row.RowId}", false, ImGuiSelectableFlags.None, new Vector2(ImGui.GetContentRegionAvail().X, IconSize * ImGuiHelpers.GlobalScale));

            // TODO: preview whole set??

            _imGuiContextMenuService.Draw($"###Set_{row.RowId}_Name_ItemContextMenu", builder =>
            {
                builder.AddTryOn(row.Set);
                builder.AddItemFinder(row.Set.RowId);
                builder.AddCopyItemName(row.Set.RowId);
                builder.AddItemSearch(row.Set);
                builder.AddOpenOnGarlandTools("item", row.Set.RowId);
            });

            ImGui.SameLine(IconSize * ImGuiHelpers.GlobalScale + ImGui.GetStyle().ItemSpacing.X, 0);
            ImGuiUtils.PushCursorY(IconSize * ImGuiHelpers.GlobalScale / 2f - ImGui.GetTextLineHeight() / 2f);
            ImGui.TextUnformatted(_textService.GetItemName(row.RowId));
        }

        public override int Compare(CustomMirageStoreSetItem a, CustomMirageStoreSetItem b)
        {
            return string.Compare(_textService.GetItemName(a.Set.RowId), _textService.GetItemName(b.Set.RowId), false, _languageProvider.CultureInfo);
        }
    }

    private class ItemsColumn : Column<CustomMirageStoreSetItem>
    {
        private readonly TextService _textService;
        private readonly TextureService _textureService;
        private readonly ITextureProvider _textureProvider;
        private readonly ImGuiContextMenuService _imGuiContextMenuService;
        private readonly List<uint> _prismBoxItemIds;

        public ItemsColumn(
            TextService textService,
            TextureService textureService,
            ITextureProvider textureProvider,
            ImGuiContextMenuService imGuiContextMenuService,
            List<uint> prismBoxItemIds)
        {
            _textService = textService;
            _textureService = textureService;
            _textureProvider = textureProvider;
            _imGuiContextMenuService = imGuiContextMenuService;
            _prismBoxItemIds = prismBoxItemIds;

            Label = "Items";
            Flags = ImGuiTableColumnFlags.NoSort;
            IsSearchable = true;
        }

        public override bool ShouldShow(CustomMirageStoreSetItem row)
        {
            var ret = false;

            for (var i = 1; i < row.Items.Count; i++)
            {
                ret |= _textService.GetItemName(row.Items[i].RowId).Contains(SearchQuery, StringComparison.InvariantCultureIgnoreCase);
                if (ret) break;
            }

            return ret;
        }

        public override void DrawColumn(CustomMirageStoreSetItem row)
        {
            var isSetCollected = _prismBoxItemIds.Contains(row.RowId);

            for (var i = 1; i < row.Items.Count; i++)
            {
                var item = row.Items[i];
                if (item.RowId != 0 && item.IsValid)
                {
                    var isSetItemCollected = _prismBoxItemIds.Contains(item.RowId) || _prismBoxItemIds.Contains(item.RowId + 1_000_000);

                    ImGui.Dummy(ImGuiHelpers.ScaledVector2(IconSize));
                    ImGui.SameLine(0, 0);
                    ImGuiUtils.PushCursorX(-IconSize * ImGuiHelpers.GlobalScale);
                    _textureService.DrawIcon(
                        item.Value.Icon,
                        false,
                        new(IconSize * ImGuiHelpers.GlobalScale)
                        {
                            TintColor = isSetCollected || isSetItemCollected
                                ? Color.White
                                : (ImGui.IsItemHovered() || ImGui.IsPopupOpen($"###SetItem_{row.RowId}_{item.RowId}_ItemContextMenu")
                                    ? Color.White : Color.Grey3)
                        }
                    );

                    if (ImGui.IsItemClicked())
                    {
                        AgentTryon.TryOn(0, item.RowId);
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                        using var tooltip = ImRaii.Tooltip();
                        if (_textureProvider.TryGetFromGameIcon(new(item.Value.Icon), out var texture) && texture.TryGetWrap(out var textureWrap, out _))
                        {
                            ImGui.Image(textureWrap.ImGuiHandle, textureWrap.Size);
                            ImGui.SameLine();
                            ImGuiUtils.PushCursorY(textureWrap.Height / 2f - ImGui.GetTextLineHeight() / 2f);
                        }
                        ImGui.TextUnformatted(_textService.GetItemName(item.RowId));
                    }

                    _imGuiContextMenuService.Draw($"###SetItem_{row.RowId}_{item.RowId}_ItemContextMenu", builder =>
                    {
                        builder.AddTryOn(item);
                        builder.AddItemFinder(item.RowId);
                        builder.AddCopyItemName(item.RowId);
                        builder.AddItemSearch(item);
                        builder.AddSearchCraftingMethod(item);
                        builder.AddOpenOnGarlandTools("item", item.RowId);
                    });

                    if (isSetItemCollected)
                    {
                        DrawCollectedCheckmark(_textureProvider);
                    }

                    ImGui.SameLine();
                }
            }
            ImGui.NewLine();
        }
    }

    private static void DrawCollectedCheckmark(ITextureProvider textureProvider)
    {
        ImGui.SameLine(0, 0);
        ImGuiUtils.PushCursorX(-IconSize * ImGuiHelpers.GlobalScale);
        if (textureProvider.GetFromGame("ui/uld/RecipeNoteBook_hr1.tex").TryGetWrap(out var tex, out _))
        {
            var pos = ImGui.GetWindowPos() + ImGui.GetCursorPos() - new Vector2(ImGui.GetScrollX(), ImGui.GetScrollY()) + ImGuiHelpers.ScaledVector2(IconSize / 2.5f + 4);
            ImGui.GetWindowDrawList().AddImage(tex.ImGuiHandle, pos, pos + ImGuiHelpers.ScaledVector2(IconSize) / 1.5f, new Vector2(0.6818182f, 0.21538462f), new Vector2(1, 0.4f));
        }
    }
}
