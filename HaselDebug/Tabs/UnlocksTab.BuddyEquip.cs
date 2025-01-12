using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Graphics;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

[RegisterSingleton<ISubTab<UnlocksTab>>(Duplicate = DuplicateStrategy.Append)]
public unsafe class UnlocksTabBardings(DebugRenderer DebugRenderer, ExcelService ExcelService) : DebugTab, ISubTab<UnlocksTab>
{
    public override string Title => "Bardings";
    public override bool DrawInChild => false;

    public override void Draw()
    {
        ref var companionInfo = ref UIState.Instance()->Buddy.CompanionInfo;

        using var table = ImRaii.Table("BuddyEquipTable", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("RowId", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Unlocked", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Items", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        foreach (var row in ExcelService.GetSheet<BuddyEquip>())
        {
            if (row.RowId == 0) continue;

            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // RowId
            ImGui.TextUnformatted(row.RowId.ToString());

            ImGui.TableNextColumn(); // Unlocked
            var isUnlocked = companionInfo.IsBuddyEquipUnlocked(row.RowId);
            using (ImRaii.PushColor(ImGuiCol.Text, (uint)(isUnlocked ? Color.Green : Color.Red)))
                ImGui.TextUnformatted(isUnlocked.ToString());

            ImGui.TableNextColumn(); // Name
            DebugRenderer.DrawIcon(row.IconBody);
            ImGui.TextUnformatted(row.Name.ExtractText());
        }
    }
}
