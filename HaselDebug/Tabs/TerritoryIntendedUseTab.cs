using System.Collections.Immutable;
using System.Threading.Tasks;
using Dalamud.Utility;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using TerritoryIntendedUseEnum = HaselCommon.Game.Enums.TerritoryIntendedUse;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class TerritoryIntendedUseTab : DebugTab
{
    private readonly ExcelService _excelService;
    private readonly TextService _textService;
    private readonly DebugRenderer _debugRenderer;

    private ImmutableSortedDictionary<uint, List<(TerritoryType, IReadOnlyList<uint>)>> _dict;
    private Task? _loadTask;

    private void LoadData()
    {
        var dict = new Dictionary<uint, List<(TerritoryType, IReadOnlyList<uint>)>>();

        foreach (var territoryTypes in _excelService.GetSheet<TerritoryType>().GroupBy(row => row.TerritoryIntendedUse.RowId))
        {
            var list = new List<(TerritoryType, IReadOnlyList<uint>)>();

            foreach (var territoryType in territoryTypes)
            {
                list.Add((
                    territoryType,
                    [.. _excelService.FindRows<ContentFinderCondition>(cfcRow => cfcRow.TerritoryType.RowId == territoryType.RowId).Select(row => row.RowId)]));
            }

            dict[territoryTypes.Key] = list;
        }

        _dict = dict.ToImmutableSortedDictionary();
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

        foreach (var territoryIntendedUse in Enum.GetValues<TerritoryIntendedUseEnum>())
        {
            if (!_dict.TryGetValue((uint)territoryIntendedUse, out var entries))
            {
                using (ImRaii.Disabled())
                {
                    using var _ = ImRaii.TreeNode($"[{(uint)territoryIntendedUse}] {territoryIntendedUse}", ImGuiTreeNodeFlags.SpanAvailWidth);
                }
                continue;
            }

            using var node = ImRaii.TreeNode($"[{(uint)territoryIntendedUse}] {territoryIntendedUse} ({entries.Count})", ImGuiTreeNodeFlags.SpanAvailWidth);
            if (!node) continue;

            foreach (var kv2 in entries)
            {
                var placeName = kv2.Item1.PlaceName.RowId != 0 && kv2.Item1.PlaceName.IsValid && !kv2.Item1.PlaceName.Value.Name.IsEmpty
                    ? $" {_textService.GetPlaceName(kv2.Item1.PlaceName.RowId)}"
                    : string.Empty;
                var zoneName = kv2.Item1.PlaceNameZone.RowId != 0 && kv2.Item1.PlaceNameZone.IsValid && !kv2.Item1.PlaceNameZone.Value.Name.IsEmpty
                    ? $" ({_textService.GetPlaceName(kv2.Item1.PlaceNameZone.RowId)})"
                    : string.Empty;

                _debugRenderer.DrawExdRow(typeof(TerritoryType), kv2.Item1.RowId, 0, new NodeOptions()
                {
                    AddressPath = new AddressPath([(nint)territoryIntendedUse]),
                    Title = $"[TerritoryType#{kv2.Item1.RowId}]{placeName}{zoneName}"
                });

                if (kv2.Item2.Count == 0)
                    continue;

                using var indent = ImRaii.PushIndent();

                foreach (var cfcRowId in kv2.Item2)
                {
                    if (!_excelService.TryGetRow<ContentFinderCondition>(cfcRowId, out var cfcRow))
                        continue;

                    _debugRenderer.DrawExdRow(typeof(ContentFinderCondition), cfcRowId, 0, new NodeOptions()
                    {
                        AddressPath = new AddressPath([(nint)territoryIntendedUse]),
                        Title = $"[ContentFinderCondition#{cfcRowId}] {cfcRow.Name.ToString().FirstCharToUpper()}"
                    });
                }
            }
        }
    }
}
