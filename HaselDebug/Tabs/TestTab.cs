using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class TestTab : DebugTab
{
    private readonly DebugRenderer _debugRenderer;

    public override void Draw()
    {
        if (ImGui.Button("Toggle 3D rendering"))
        {
            Manager.Instance()->Is3DRenderingDisabled = !Manager.Instance()->Is3DRenderingDisabled;
        }

        if (ImGui.Button("LocalPlayer Enable Draw"))
        {
            Control.GetLocalPlayer()->EnableDraw();
        }

        if (ImGui.Button("LocalPlayer Diable Draw"))
        {
            Control.GetLocalPlayer()->DisableDraw();
        }
    }
}
