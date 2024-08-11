using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using HaselDebug.Abstracts;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;

namespace HaselDebug.Tabs;

public unsafe class LocalPlayerTab(DebugRenderer DebugRenderer) : DebugTab
{
    public override void Draw()
    {
        var localPlayer = Control.GetLocalPlayer();
        if (localPlayer == null)
        {
            ImGui.TextUnformatted("LocalPlayer unavailable");
            return;
        }

        DebugRenderer.DrawPointerType(localPlayer, typeof(BattleChara), new NodeOptions() { DefaultOpen = true });
    }
}
