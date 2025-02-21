using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class SatisfactionSupplyTab : DebugTab
{
    private readonly DebugRenderer _debugRenderer;
    private readonly ExcelService _excelService;
    private readonly TextService _textService;
    private readonly TeleportService _teleportService;

    public override bool DrawInChild => false;

    public override void Draw()
    {
        var satisfactionSupply = SatisfactionSupplyManager.Instance();

        using var table = ImRaii.Table("SatisfactionSupply", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Satisfaction", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Rank", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Used Allowance", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableHeadersRow();

        foreach (var row in _excelService.GetSheet<SatisfactionNpc>())
        {
            if (row.RowId == 0)
                continue;

            var index = (int)row.RowId - 1;
            var rank = satisfactionSupply->SatisfactionRanks[index];

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Index
            ImGui.TextUnformatted(row.RowId.ToString());

            ImGui.TableNextColumn(); // Name
            _debugRenderer.DrawIcon((uint)row.RankParams[rank].ImageId);

            if (_excelService.TryGetRow<Level>(row.Unknown0, out var level) &&
                _teleportService.TryGetClosestAetheryte(level, out var aetheryte) &&
                _excelService.TryGetRow<PlaceName>(aetheryte.Value.PlaceName.RowId, out var placeName))
            {
                var clicked = ImGui.Selectable(_textService.GetENpcResidentName(row.Npc.RowId));
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                    using var tooltip = ImRaii.Tooltip();
                    ImGui.TextUnformatted($"Teleport to: {placeName.Name.ExtractText()}");
                }
                if (clicked)
                {
                    Telepo.Instance()->Teleport(aetheryte.Value.RowId, 0);
                }
            }
            else
            {
                ImGui.TextUnformatted(_textService.GetENpcResidentName(row.Npc.RowId));
            }

            ImGui.TableNextColumn(); // Satisfaction
            ImGui.TextUnformatted($"{satisfactionSupply->Satisfaction[index]}/{row.SatisfactionNpcParams[rank].SatisfactionRequired}");

            ImGui.TableNextColumn(); // Rank
            ImGui.TextUnformatted(rank.ToString());

            ImGui.TableNextColumn(); // UsedAllowance
            ImGui.TextUnformatted(satisfactionSupply->UsedAllowances[index].ToString());
        }
    }
}
