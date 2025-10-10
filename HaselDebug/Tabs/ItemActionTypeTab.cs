using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Dalamud.Interface.Utility.Raii;
using HaselCommon.Game.Enums;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class ItemActionTypeTab : DebugTab
{
    private readonly ExcelService _excelService;
    private readonly DebugRenderer _debugRenderer;

    private ImmutableSortedDictionary<ushort, IReadOnlyList<Item>> _dict;
    private bool _isInitialized;

    private void Initialize()
    {
        _dict = _excelService.GetSheet<ItemAction>()
            .GroupBy(row => row.Type)
            .ToDictionary(
                g => g.Key,
                g => _excelService.FindRows<Item>(item => g.Any(itemAction => itemAction.RowId == item.ItemAction.RowId))
            )
            .ToImmutableSortedDictionary();
    }

    public override void Draw()
    {
        if (!_isInitialized)
        {
            Initialize();
            _isInitialized = true;
        }

        foreach (var (type, items) in _dict)
        {
            using var node = ImRaii.TreeNode($"[{type}] {(ItemActionType)type} ({items.Count})", ImGuiTreeNodeFlags.SpanAvailWidth);
            if (!node) continue;

            foreach (var item in items)
            {
                _debugRenderer.DrawExdRow(typeof(Item), item.RowId, 0, new NodeOptions()
                {
                    AddressPath = new AddressPath([(nint)type]),
                    Title = $"[Item#{item.RowId}] {item.Name.ToString()}"
                });
            }
        }
    }
}
