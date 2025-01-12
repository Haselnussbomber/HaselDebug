using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append)]
public unsafe class TargetTab(DebugRenderer DebugRenderer) : DebugTab
{
    public override void Draw()
    {
        var target = TargetSystem.Instance()->GetTargetObject();
        if (target == null)
        {
            ImGui.TextUnformatted("No Target");
            return;
        }

        DebugRenderer.DrawPointerType(target, typeof(GameObject), new NodeOptions() { DefaultOpen = true });
    }
}
