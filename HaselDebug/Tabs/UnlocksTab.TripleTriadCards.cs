using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Graphics;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Windows.ItemTooltips;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

public unsafe class UnlocksTabTripleTriadCards(
    DebugRenderer DebugRenderer,
    ExcelService ExcelService,
    MapService MapService,
    TextureService TextureService,
    TripleTriadNumberFontManager TripleTriadNumberFontManager) : DebugTab, ISubTab<UnlocksTab>, IDisposable
{
    private TripleTriadCardTooltip? TripleTriadCardTooltip;

    public override string Title => "Triple Triad Cards";
    public override bool DrawInChild => false;

    public void Dispose()
    {
        TripleTriadCardTooltip?.Dispose();
    }

    public override void Draw()
    {
        var uiState = UIState.Instance();

        using var table = ImRaii.Table("TripleTriadCardsTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("RowId", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Collected", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Name");
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        foreach (var row in ExcelService.GetSheet<TripleTriadCard>())
        {
            if (row.RowId == 0 || !ExcelService.TryGetRow<TripleTriadCardResident>(row.RowId, out var resident))
                continue;

            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // RowId
            ImGui.TextUnformatted(row.RowId.ToString());

            ImGui.TableNextColumn(); // Collected
            var isCollected = uiState->IsTripleTriadCardUnlocked((ushort)row.RowId);
            using (ImRaii.PushColor(ImGuiCol.Text, (uint)(isCollected ? Color.Green : Color.Red)))
                ImGui.TextUnformatted(isCollected.ToString());

            ImGui.TableNextColumn(); // Name
            DebugRenderer.DrawIcon(88000 + row.RowId);
            var hasLevel = resident.Location.TryGetValue<Level>(out var level);
            using (Color.Transparent.Push(ImGuiCol.HeaderActive, !hasLevel))
            using (Color.Transparent.Push(ImGuiCol.HeaderHovered, !hasLevel))
            {
                if (ImGui.Selectable(row.Name.ExtractText()))
                {
                    if (hasLevel)
                    {
                        MapService.OpenMap(level);
                    }
                }
            }

            if (ImGui.IsItemHovered())
            {
                using var tooltip = ImRaii.Tooltip();
                TripleTriadCardTooltip ??= new TripleTriadCardTooltip(TextureService, ExcelService, TripleTriadNumberFontManager);
                TripleTriadCardTooltip?.SetCard(row.RowId);
                TripleTriadCardTooltip?.CalculateLayout();
                TripleTriadCardTooltip?.Update();
                TripleTriadCardTooltip?.Draw();
            }
        }
    }
}
