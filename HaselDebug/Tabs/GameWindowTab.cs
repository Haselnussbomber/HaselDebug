using FFXIVClientStructs.FFXIV.Client.System.Framework;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class GameWindowTab : DebugTab
{
    private readonly DebugRenderer _debugRenderer;

    public override void Draw()
    {
        var gameWindow = Framework.Instance()->GameWindow;
        if (gameWindow == null) return;

        _debugRenderer.DrawPointerType(gameWindow, typeof(GameWindow), new NodeOptions() { DefaultOpen = true });

        var i = 0;
        foreach (var arg in gameWindow->ArgumentsSpan)
        {
            ImGui.TextUnformatted($"[{i++}] {arg.ExtractText()}");
        }
    }
}
