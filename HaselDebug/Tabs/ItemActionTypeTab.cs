using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Interface.Utility.Raii;
using HaselCommon.Game.Enums;
using HaselCommon.Gui;
using HaselCommon.Services;
using HaselCommon.Utils;
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

    private ImmutableSortedDictionary<ushort, ItemHandle[]> _dict;
    private Task? _loadTask;

    private void LoadData()
    {
        _dict = _excelService.GetSheet<ItemAction>()
            .GroupBy(row => row.Type)
            .ToDictionary(
                g => g.Key,
                g => _excelService.FindRows<Item>(item => g.Any(itemAction => itemAction.RowId == item.ItemAction.RowId)).Select(row => (ItemHandle)row).ToArray()
            )
            .ToImmutableSortedDictionary();
    }

    public override void Draw()
    {
        _loadTask ??= Task.Run(LoadData);

        if (!_loadTask.IsCompleted)
        {
            ImGui.Text("Loading...");
            return;
        }

        if (_loadTask.IsFaulted)
        {
            ImGuiUtilsEx.DrawAlertError("TaskError", _loadTask.Exception?.ToString() ?? "Error loading data :(");
            return;
        }

        foreach (var (type, items) in _dict)
        {
            using var node = ImRaii.TreeNode($"[{type}] {(ItemActionType)type} ({items.Length})", ImGuiTreeNodeFlags.SpanAvailWidth);
            if (!node) continue;

            foreach (var item in items)
            {
                _debugRenderer.DrawExdRow(typeof(Item), item.ItemId, 0, new NodeOptions()
                {
                    AddressPath = new AddressPath([type]),
                    Title = $"[Item#{item.ItemId}] {item.Name}"
                });
            }
        }
    }
}
