using System.Collections.Immutable;
using System.Linq;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using HaselCommon.Game.Enums;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class ItemActionTypeTab : DebugTab
{
    private readonly ExcelService _excelService;
    private readonly DebugRenderer _debugRenderer;

    private ImmutableSortedDictionary<ushort, Item[]> _dict;

    [AutoPostConstruct]
    public void Initialize()
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
        foreach (var (type, items) in _dict)
        {
            using var node = ImRaii.TreeNode($"[{type}] {(ItemActionType)type} ({items.Length})", ImGuiTreeNodeFlags.SpanAvailWidth);
            if (!node) continue;

            foreach (var item in items)
            {
                _debugRenderer.DrawExdRow(typeof(Item), item.RowId, 0, new NodeOptions()
                {
                    AddressPath = new AddressPath([(nint)type]),
                    Title = $"[Item#{item.RowId}] {item.Name.ExtractText().StripSoftHyphen()}"
                });
            }
        }
    }
}
