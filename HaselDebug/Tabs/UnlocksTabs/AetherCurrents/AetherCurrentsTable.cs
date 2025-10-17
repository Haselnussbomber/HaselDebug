using HaselCommon.Gui.ImGuiTable;
using HaselDebug.Tabs.UnlocksTabs.AetherCurrents.Columns;

namespace HaselDebug.Tabs.UnlocksTabs.AetherCurrents;

[RegisterSingleton, AutoConstruct]
public unsafe partial class AetherCurrentsTable : Table<AetherCurrentEntry>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ExcelService _excelService;
    private readonly CompletedColumn _completedColumn;
    private readonly ZoneColumn _zoneColumn;
    private readonly LocationColumn _locationColumn;

    internal readonly Dictionary<uint, uint> AetherCurrentEObjCache = [];
    internal readonly Dictionary<uint, uint> EObjLevelCache = [];

    [AutoPostConstruct]
    public void Initialize()
    {
        _locationColumn.SetTable(this);

        Columns = [
            EntryRowIdColumn<AetherCurrentEntry, AetherCurrent>.Create(_serviceProvider),
            _completedColumn,
            _zoneColumn,
            _locationColumn,
        ];
    }

    public override void LoadRows()
    {
        Rows.Clear();

        foreach (var row in _excelService.GetSheet<AetherCurrentCompFlgSet>())
        {
            var currentNumber = 1;
            var lastWasQuest = false;
            foreach (var aetherCurrent in row.AetherCurrents)
            {
                if (!aetherCurrent.IsValid) continue;

                var isQuest = aetherCurrent.Value.Quest.IsValid;
                if (isQuest)
                {
                    lastWasQuest = true;
                }
                else if (lastWasQuest)
                {
                    currentNumber = 1;
                    lastWasQuest = false;
                }

                Rows.Add(new AetherCurrentEntry(row, aetherCurrent.Value, currentNumber++));
            }
        }
    }
}
