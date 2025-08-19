using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class TargetTab : DebugTab
{
    private readonly DebugRenderer _debugRenderer;

    public override void Draw()
    {
        var target = TargetSystem.Instance()->GetTargetObject();
        if (target == null)
        {
            ImGui.Text("No Target"u8);
            return;
        }

        _debugRenderer.DrawPointerType(target, typeof(GameObject), new NodeOptions() { DefaultOpen = true });
    }
}
