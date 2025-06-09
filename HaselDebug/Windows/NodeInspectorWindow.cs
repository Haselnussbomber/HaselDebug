using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Gui;
using HaselDebug.Services;
using ImGuiNET;

namespace HaselDebug.Windows;

[AutoConstruct]
public partial class NodeInspectorWindow : SimpleWindow
{
    private readonly AtkDebugRenderer _atkDebugRenderer;

    public nint NodeAddress { get; set; }

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

        Flags |= ImGuiWindowFlags.NoSavedSettings;

        RespectCloseHotkey = true;
        DisableWindowSounds = true;
    }

    public override unsafe void Draw()
    {
        _atkDebugRenderer.DrawNode((AtkResNode*)NodeAddress);
    }
}
