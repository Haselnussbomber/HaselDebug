using FFXIVClientStructs.FFXIV.Client.Game;
using HaselDebug.Abstracts;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;

namespace HaselDebug.Tabs;

public unsafe class HousingTab(DebugRenderer DebugRenderer) : DebugTab
{
    public override void Draw()
    {
        var housingManager = HousingManager.Instance();
        if (housingManager == null)
        {
            ImGui.TextUnformatted("HousingManager unavailable");
            return;
        }

        ImGui.TextUnformatted($"TerritoryType: {housingManager->CurrentTerritory->GetTerritoryType()}");

        DebugRenderer.DrawPointerType(housingManager, typeof(HousingManager), new NodeOptions() { DefaultOpen = true });
    }
}
