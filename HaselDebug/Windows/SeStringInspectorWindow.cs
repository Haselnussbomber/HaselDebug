using System.Numerics;
using HaselCommon.Services;
using HaselCommon.Windowing;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Text.ReadOnly;

namespace HaselDebug.Windows;

public class SeStringInspectorWindow(
    WindowManager windowManager,
    DebugRenderer DebugRenderer,
    ReadOnlySeString SeString,
    string windowName = "SeString") : SimpleWindow(windowManager, windowName)
{
    public override void OnOpen()
    {
        base.OnOpen();

        Size = new Vector2(800, 600);
        SizeConstraints = new()
        {
            MinimumSize = new Vector2(250, 250),
            MaximumSize = new Vector2(4096, 2160)
        };

        SizeCondition = ImGuiCond.Appearing;
        RespectCloseHotkey = true;
        DisableWindowSounds = true;
    }

    public override void Draw()
    {
        DebugRenderer.DrawSeString(SeString.AsSpan(), new NodeOptions()
        {
            DefaultOpen = true
        });
    }
}
