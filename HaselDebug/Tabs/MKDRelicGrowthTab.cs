using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class MKDRelicGrowthTab : DebugTab
{
    // Aether, Aether, Everywhere
    private const ushort InternalQuestId = 5381;
    private const uint QuestId = InternalQuestId | 0x10000u;

    private readonly TextService _textService;
    private readonly ExcelService _excelService;
    private readonly ISeStringEvaluator _evaluator;

    public override string Title => "MKDRelicGrowth";

    public override void Draw()
    {
        var questManager = QuestManager.Instance();
        if (!questManager->IsQuestAccepted(InternalQuestId))
        {
            ImGui.Text($"Quest \"{_textService.GetQuestName(QuestId)}\" not accepted");
            return;
        }

        if (!_excelService.TryGetRow<QuestCustomTodo>(QuestId, out var row))
        {
            ImGui.Text("QuestCustomTodo row not found"u8);
            return;
        }

        var manager = QuestCustomTodoManager.Instance();
        QuestCustomTodoProgress progress = default;

        foreach (var entry in row.Entries)
        {
            if (entry.Index == ushort.MaxValue)
                continue;

            manager->GetProgress(&progress, InternalQuestId, (byte)entry.Index);

            ImGui.Text($"[{entry.Index}] {GetRouletteName(entry.Index)} | {progress.CurValue}/{progress.MaxValue}");
        }
    }

    private string GetRouletteName(uint index)
    {
        // index to ContentRoulette RowId
        var rouletteId = index switch
        {
            0 => 2, // Duty Roulette: High-level Dungeons
            1 => 6, // Duty Roulette: Trials
            2 => 15, // Duty Roulette: Alliance Raids
            3 => 17, // Duty Roulette: Normal Raids
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
        return _evaluator.EvaluateFromAddon(16557, [rouletteId]).ToString().StripSoftHyphen();
    }
}

public static unsafe class QuestCustomTodoExtensions
{
    extension (QuestCustomTodo row)
    {
        public Collection<EntryStruct> Entries => new(row.ExcelPage, row.RowOffset, row.RowOffset, &EntryCtor, 7);
    }

    private static EntryStruct EntryCtor(ExcelPage page, uint _, uint offset, uint i) => new(page, offset, i);
    public readonly struct EntryStruct(ExcelPage page, uint offset, uint i)
    {
        public readonly ushort Index => page.ReadUInt16(offset + i * 2);
        public readonly ushort Value => page.ReadUInt16(offset + 14 + i * 2);
    }
}
