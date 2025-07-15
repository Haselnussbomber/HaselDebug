using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class LocalPlayerTab : DebugTab
{
    private readonly DebugRenderer _debugRenderer;

    public override void Draw()
    {
        var localPlayer = Control.GetLocalPlayer();
        if (localPlayer == null)
        {
            ImGui.TextUnformatted("LocalPlayer unavailable");
            return;
        }

        _debugRenderer.DrawPointerType(localPlayer, typeof(BattleChara), new NodeOptions() { DefaultOpen = true });
    }
}
