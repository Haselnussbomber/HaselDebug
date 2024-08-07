using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using HaselDebug.Abstracts;
using HaselDebug.Utils;

namespace HaselDebug.Tabs;

public unsafe class LocalPlayerTab : DebugTab
{
    public override void Draw()
    {
        var localPlayer = Control.GetLocalPlayer();
        if (localPlayer == null)
            return;

        DebugUtils.DrawPointerType((nint)localPlayer, typeof(BattleChara), new NodeOptions() { DefaultOpen = true });
    }
}
