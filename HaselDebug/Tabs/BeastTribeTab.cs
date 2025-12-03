using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using Lumina.Excel;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class BeastTribeTab : DebugTab
{
    private readonly DebugRenderer _debugRenderer;
    private readonly ExcelService _excelService;
    private readonly TextService _textService;
    private readonly ISeStringEvaluator _seStringEvaluator;
    private readonly TeleportService _teleportService;

    public override bool DrawInChild => false;

    public override void Draw()
    {
        var playerState = PlayerState.Instance();

        using var table = ImRaii.Table("BeastTribe"u8, 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("RowId"u8, ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Rank"u8, ImGuiTableColumnFlags.WidthFixed, 150);
        ImGui.TableSetupColumn("Reputation"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableHeadersRow();

        foreach (var row in _excelService.GetSheet<BeastTribe>())
        {
            if (row.RowId == 0)
                continue;

            var index = (byte)row.RowId;
            var rank = playerState->GetBeastTribeRank(index);
            _excelService.TryGetRow<BeastReputationRank>(rank, out var rankRow);

            var currentRep = playerState->GetBeastTribeCurrentReputation(index);
            var neededRep = playerState->GetBeastTribeNeededReputation(index);
            var isAllied = row.Expansion.RowId != 0 && row.Unknown1 != 0 && QuestManager.IsQuestComplete(row.Unknown1);
            var maxRank = isAllied ? 8 : row.MaxRank;
            var rankName = rankRow.AlliedNames;

            if (row.Expansion.RowId != 0
                && row.Unknown1 != 0
                && QuestManager.IsQuestComplete(row.Unknown1))
            {
                rank++;
                rankName = rankRow.Name;
                neededRep = 0;
            }
            else if (row.Expansion.RowId == 0)
            {
                rankName = rankRow.Name;
            }

            if (rank > maxRank)
                maxRank = rank;

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // RowId
            ImGui.Text(row.RowId.ToString());

            ImGui.TableNextColumn(); // Name
            _debugRenderer.DrawIcon(row.Icon);
            if (_excelService.TryGetRow<Level>(row.Unknown2, out var level) && _teleportService.TryGetClosestAetheryte(level, out var aetheryte))
            {
                var clicked = ImGui.Selectable(row.Name.ToString());
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                    using var tooltip = ImRaii.Tooltip();
                    ImGui.Text($"Teleport to: {_textService.GetPlaceName(aetheryte.PlaceName.RowId)}");
                }
                if (clicked)
                {
                    Telepo.Instance()->Teleport(aetheryte.RowId, 0);
                }
            }
            else
            {
                ImGui.Text(row.Name.ToString());
            }

            ImGui.TableNextColumn(); // Rank
            if (rank > 0)
            {
                ImGui.Text($"{rank} {rankName}");
            }

            ImGui.TableNextColumn(); // Reputation
            if (rank > 0 && rank != maxRank)
            {
                ImGui.Text($"{currentRep}/{neededRep}");
            }
        }
    }
}
