using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using HaselDebug.Abstracts;
using ImGuiNET;

namespace HaselDebug.Tabs;

public unsafe class TerritoryTypeTab(IClientState ClientState) : DebugTab
{
    public override unsafe void Draw()
    {
        ImGui.TextUnformatted($"TerritoryType: {ClientState.TerritoryType}");

        var gameMain = GameMain.Instance();
        if (gameMain->CurrentTerritoryTypeRow != 0)
        {
            // DebugUtils.DrawPointerType(gameMain->CurrentTerritoryTypeRow, typeof(TerritoryType), new NodeOptions()); // we had sheet structs once...
        }
    }
}
