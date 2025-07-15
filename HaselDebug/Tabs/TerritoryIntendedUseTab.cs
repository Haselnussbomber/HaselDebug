using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using Lumina.Excel.Sheets;
using TerritoryIntendedUseEnum = HaselCommon.Game.Enums.TerritoryIntendedUse;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class TerritoryIntendedUseTab : DebugTab
{
    private readonly ExcelService _excelService;
    private readonly TextService _textService;
    private readonly DebugRenderer _debugRenderer;

    private ImmutableSortedDictionary<uint, List<(TerritoryType, ContentFinderCondition[])>> _dict;
    private bool _isInitialized;

    private void Initialize()
    {
        var dict = new Dictionary<uint, List<(TerritoryType, ContentFinderCondition[])>>();

        foreach (var territoryTypes in _excelService.GetSheet<TerritoryType>().GroupBy(row => row.TerritoryIntendedUse.RowId))
        {
            var list = new List<(TerritoryType, ContentFinderCondition[])>();

            foreach (var territoryType in territoryTypes)
            {
                list.Add((
                    territoryType,
                    _excelService.FindRows<ContentFinderCondition>(cfcRow => cfcRow.TerritoryType.RowId == territoryType.RowId)));
            }

            dict[territoryTypes.Key] = list;
        }

        _dict = dict.ToImmutableSortedDictionary();
    }

    public override void Draw()
    {
        if (!_isInitialized)
        {
            Initialize();
            _isInitialized = true;
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

                if (kv2.Item2.Length == 0)
                    continue;

                using var indent = ImRaii.PushIndent();

                foreach (var cfc in kv2.Item2)
                {
                    _debugRenderer.DrawExdRow(typeof(ContentFinderCondition), cfc.RowId, 0, new NodeOptions()
                    {
                        AddressPath = new AddressPath([(nint)territoryIntendedUse]),
                        Title = $"[ContentFinderCondition#{cfc.RowId}] {cfc.Name.ExtractText().FirstCharToUpper().StripSoftHyphen()}"
                    });
                }
            }
        }
    }
}
