using System.Globalization;
using System.Runtime.CompilerServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Abstracts;
using HaselDebug.Extensions;
using HaselDebug.Interfaces;
using HaselDebug.Utils;
using HaselDebug.Windows;
using HousingFurniture = Lumina.Excel.Sheets.HousingFurniture;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class FurnitureCatalogTab : DebugTab
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ITextureProvider _textureProvider;
    private readonly TextService _textService;
    private bool _isInitialized;
    private FurnitureCatalogTable? _indoorTable;
    private FurnitureCatalogTable? _outdoorTable;
    private bool _wasInside;
    private bool _wasOutside;

    private void Initialize()
    {
        _indoorTable = ActivatorUtilities.CreateInstance<FurnitureCatalogTable>(_serviceProvider, HousingTerritoryType.Indoor);
        _outdoorTable = ActivatorUtilities.CreateInstance<FurnitureCatalogTable>(_serviceProvider, HousingTerritoryType.Outdoor);
    }

    public override void Draw()
    {
        if (!_isInitialized)
        {
            Initialize();
            _isInitialized = true;
        }

        var housingManager = HousingManager.Instance();
        if (housingManager == null)
            return;

        var isInside = housingManager->IsInside();
        var isOutside = housingManager->IsOutside();

        var indoorTabFlags = ImGuiTabItemFlags.None;
        var outdoorTabFlags = ImGuiTabItemFlags.None;

        if (_wasOutside && isInside)
        {
            indoorTabFlags |= ImGuiTabItemFlags.SetSelected;
        }

        if (_wasInside && isOutside)
        {
            outdoorTabFlags |= ImGuiTabItemFlags.SetSelected;
        }

        _wasInside = isInside;
        _wasOutside = isOutside;

        if (!AgentHousingCatalogPreview.Instance()->IsAgentActive())
        {
            var showButton = isOutside || isInside;
            var text = _textService.Translate("FurnitureCatalogTab.AlertText");
            var buttonText = _textService.Translate("FurnitureCatalogTab.OpenButton.Label", _textService.GetAddonText(isInside ? 6263u : 6264));
            var style = ImGui.GetStyle();
            var iconSize = ImGui.GetTextLineHeight();
            var outerSize = new Vector2(ImGui.GetContentRegionMax().X - style.FramePadding.X * 2, 0);
            var innerWidth = outerSize.X
                - iconSize
                - style.ItemInnerSpacing.X * 2 // padding of icon
                - style.ItemSpacing.X // spacing between icon and text
                - style.ItemInnerSpacing.X * 2; // padding of text
            outerSize.Y = style.FramePadding.Y * 2
                + Math.Max(iconSize, ImGui.CalcTextSize(text, wrapWidth: innerWidth).Y) + style.ItemInnerSpacing.Y * 2 // line 1: icon/text
                + (showButton // line 2: button
                    ? (style.ItemSpacing.Y + ImGuiHelpers.GetButtonSize(buttonText).Y) // spacing between text and button + button
                    : 0);

            using (ImGuiUtilsEx.AlertBox("InfoBox", Color.FromHSL(190, 1f, 0.5f), outerSize))
            {
                if (ServiceLocator.TryGetService<ITextureProvider>(out var textureProvider))
                    textureProvider.DrawIcon(60071, iconSize);
                else
                    ImGui.Dummy(new Vector2(iconSize));
                ImGui.SameLine();
                ImGui.TextWrapped(text);

                if (showButton && ImGui.Button(buttonText))
                {
                    var agent = AgentModule.Instance()->GetAgentByInternalId(AgentId.Housing);
                    var returnValue = stackalloc AtkValue[1];
                    var values = stackalloc AtkValue[1];
                    values->SetInt(4);
                    agent->ReceiveEvent(returnValue, values, 1, 1);
                }
            }
        }

        using var tabBar = ImRaii.TabBar("FurnitureCatalogTabBar");
        if (!tabBar)
            return;

        using (var tab = ImRaii.TabItem(_textService.GetAddonText(6263), indoorTabFlags)) // Preview Indoor Furnishings
        {
            if (tab)
            {
                _indoorTable?.Draw();
            }
        }

        using (var tab = ImRaii.TabItem(_textService.GetAddonText(6264), outdoorTabFlags)) // Preview Outdoor Furnishings
        {
            if (tab)
            {
                _outdoorTable?.Draw();
            }
        }
    }

    public readonly record struct FurnitureCatalogItem
    {
        public Type SheetType { get; init; }
        public uint RowId { get; init; }
        public ItemHandle Item { get; init; }
        public ushort ModelKey { get; init; }
        public byte HousingItemCategory { get; init; }
        public ushort Patch { get; init; }
        public uint? FurnitureCatalogCategoryRowId { get; init; }

        public FurnitureCatalogItem(HousingFurniture row)
        {
            SheetType = typeof(HousingFurniture);
            RowId = row.RowId;
            Item = row.Item;
            ModelKey = row.ModelKey;
            HousingItemCategory = row.HousingItemCategory;

            if (ServiceLocator.GetService<ExcelService>()?.TryFindRow<FurnitureCatalogItemList>(cRow => cRow.Item.RowId == row.Item.RowId, out var catalogItem) == true)
            {
                Patch = catalogItem.Patch;
                FurnitureCatalogCategoryRowId = catalogItem.Category.RowId;
            }
        }

        public FurnitureCatalogItem(HousingYardObject row)
        {
            SheetType = typeof(HousingYardObject);
            RowId = row.RowId;
            Item = row.Item;
            ModelKey = row.ModelKey;
            HousingItemCategory = row.HousingItemCategory;

            if (ServiceLocator.GetService<ExcelService>()?.TryFindRow<YardCatalogItemList>(cRow => cRow.Item.RowId == row.Item.RowId, out var catalogItem) == true)
            {
                Patch = catalogItem.Patch;
                FurnitureCatalogCategoryRowId = catalogItem.Category.RowId;
            }
        }
    }

    [RegisterTransient, AutoConstruct]
    public unsafe partial class FurnitureCatalogTable : Table<FurnitureCatalogItem>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ExcelService _excelService;
        private HousingTerritoryType _houseTerritoryType;

        [AutoPostConstruct]
        public void Initialize(HousingTerritoryType housingTerritoryType)
        {
            _houseTerritoryType = housingTerritoryType;

            Columns = [
                ActivatorUtilities.CreateInstance<RowIdColumn>(_serviceProvider),
                ActivatorUtilities.CreateInstance<PatchColumn>(_serviceProvider),
                ActivatorUtilities.CreateInstance<CategoryColumn>(_serviceProvider),
                ActivatorUtilities.CreateInstance<ItemColumn>(_serviceProvider, housingTerritoryType),
            ];
        }

        public override void LoadRows()
        {
            switch (_houseTerritoryType)
            {
                case HousingTerritoryType.Indoor:
                    Rows = [
                        .. _excelService.GetSheet<HousingFurniture>()
                        .Where(row => row.Item.RowId != 0 && row.Item.IsValid && row.Item.Value.Icon!= 0)
                        .Select(row => new FurnitureCatalogItem(row))
                    ];
                    break;

                case HousingTerritoryType.Outdoor:
                    Rows = [
                        .. _excelService.GetSheet<HousingYardObject>()
                        .Where(row => row.Item.RowId != 0 && row.Item.IsValid && row.Item.Value.Icon!= 0)
                        .Select(row => new FurnitureCatalogItem(row))
                    ];
                    break;
            }
        }

        [RegisterTransient, AutoConstruct]
        public partial class RowIdColumn : ColumnNumber<FurnitureCatalogItem>
        {
            private readonly IServiceProvider _serviceProvider;
            private readonly WindowManager _windowManager;
            private readonly TextService _textService;
            private readonly LanguageProvider _languageProvider;

            [AutoPostConstruct]
            private void Initialize()
            {
                LabelKey = "RowIdColumn.Label";
                SetFixedWidth(60);
                Flags |= ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.PreferSortDescending;
            }

            public override int ToValue(FurnitureCatalogItem row)
            {
                return (int)row.RowId;
            }

            public override void DrawColumn(FurnitureCatalogItem row)
            {
                if (ImGui.Selectable(ToName(row)))
                {
                    var title = $"{row.SheetType.Name}#{row.RowId} ({_languageProvider.ClientLanguage})";
                    _windowManager.CreateOrOpen(title, () => ActivatorUtilities.CreateInstance<ExcelRowTab>(_serviceProvider, row.SheetType, row.RowId, _languageProvider.ClientLanguage, title));
                }

                ImGuiContextMenu.Draw($"{row.SheetType.Name}{row.RowId}RowIdContextMenu", builder =>
                {
                    builder.AddCopyRowId(row.RowId);
                });
            }
        }

        [RegisterTransient, AutoConstruct]
        public partial class PatchColumn : ColumnNumber<FurnitureCatalogItem>
        {
            [AutoPostConstruct]
            private void Initialize()
            {
                LabelKey = "PatchColumn.Label";
                SetFixedWidth(50);
            }

            public override int ToValue(FurnitureCatalogItem row)
            {
                return row.Patch;
            }

            public override string ToName(FurnitureCatalogItem row)
            {
                if (row.Patch == 0)
                    return string.Empty;

                return (row.Patch / 100.0).ToString("F2", CultureInfo.InvariantCulture);
            }
        }

        [RegisterTransient, AutoConstruct]
        public partial class CategoryColumn : ColumnString<FurnitureCatalogItem>
        {
            private readonly ExcelService _excelService;
            private readonly TextService _textService;

            [AutoPostConstruct]
            private void Initialize()
            {
                LabelKey = "CategoryColumn.Label";
                SetStretchWidth(1);
            }

            public override string ToName(FurnitureCatalogItem row)
            {
                return row.FurnitureCatalogCategoryRowId.HasValue && _excelService.TryGetRow<FurnitureCatalogCategory>(row.FurnitureCatalogCategoryRowId.Value, out var category)
                    ? category.Category.ToString()
                    : _textService.GetAddonText(1562); // None
            }
        }

        [RegisterTransient, AutoConstruct]
        public partial class ItemColumn : ColumnString<FurnitureCatalogItem>
        {
            private readonly ItemService _itemService;
            private readonly UnlocksTabUtils _unlocksTabUtils;
            private HousingTerritoryType _housingTerritoryType;

            [AutoPostConstruct]
            private void Initialize(HousingTerritoryType housingTerritoryType)
            {
                _housingTerritoryType = housingTerritoryType;
                LabelKey = "ItemColumn.Label";
                SetStretchWidth(2);
            }

            public override string ToName(FurnitureCatalogItem row)
            {
                return _itemService.GetItemName(row.Item).ToString();
            }

            public override void DrawColumn(FurnitureCatalogItem row)
            {
                var housingManager = HousingManager.Instance();
                var agent = AgentHousingCatalogPreview.Instance();
                if (housingManager == null || agent == null)
                    return;

                ref var temporaryObject = ref Unsafe.NullRef<HousingTemporaryObject>();
                var tabName = string.Empty;
                var isInTerritory = housingManager->GetCurrentHousingTerritoryType() == _housingTerritoryType;
                byte kind = 0;

                switch (_housingTerritoryType)
                {
                    case HousingTerritoryType.Indoor:
                        kind = 76;
                        if (housingManager->IndoorTerritory != null)
                            temporaryObject = ref housingManager->IndoorTerritory->TemporaryObject;

                        break;
                    case HousingTerritoryType.Outdoor:
                        kind = 77;
                        if (housingManager->OutdoorTerritory != null)
                            temporaryObject = ref housingManager->OutdoorTerritory->TemporaryObject;

                        break;
                    default:
                        return;
                }

                var disabled = !agent->IsAgentActive() || !isInTerritory || kind == 0 || Unsafe.IsNullRef(in temporaryObject);
                if (_unlocksTabUtils.DrawSelectableItem(row.Item, row.Item.ItemId.ToString()) && !disabled)
                {
                    temporaryObject.SetData(
                        kind,
                        row.ModelKey,
                        row.HousingItemCategory,
                        AgentHousingCatalogPreview.Instance()->SelectedStainId,
                        UIModule.Instance()->IsPadModeEnabled());
                }
            }
        }
    }
}
