using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;

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

        using var table = ImRaii.Table("SatisfactionSupply"u8, 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Index"u8, ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Satisfaction"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Rank"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Used Allowance"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableHeadersRow();

        foreach (var row in _excelService.GetSheet<SatisfactionNpc>())
        {
            if (row.RowId == 0)
                continue;

            var index = (int)row.RowId - 1;
            var rank = satisfactionSupply->SatisfactionRanks[index];

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Index
            ImGui.Text(row.RowId.ToString());

            ImGui.TableNextColumn(); // Name
            _debugRenderer.DrawIcon((uint)row.RankParams[rank].ImageId);

            if (_excelService.TryGetRow<Level>(row.Level.RowId, out var level) &&
                _teleportService.TryGetClosestAetheryte(level, out var aetheryte))
            {
                var clicked = ImGui.Selectable(_textService.GetENpcResidentName(row.Npc.RowId));
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
                ImGui.Text(_textService.GetENpcResidentName(row.Npc.RowId));
            }

            ImGui.TableNextColumn(); // Satisfaction
            ImGui.Text($"{satisfactionSupply->Satisfaction[index]}/{row.SatisfactionNpcParams[rank].SatisfactionRequired}");

            ImGui.TableNextColumn(); // Rank
            ImGui.Text(rank.ToString());

            ImGui.TableNextColumn(); // UsedAllowance
            ImGui.Text(satisfactionSupply->UsedAllowances[index].ToString());
        }
    }
}
