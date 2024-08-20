using System.Numerics;
using HaselCommon.Services;
using HaselCommon.Windowing;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;

namespace HaselDebug.Windows;

public class PointerTypeWindow(WindowManager WindowManager, DebugRenderer DebugRenderer, nint Address, Type Type) : SimpleWindow(WindowManager, Type.Name)
{
    private NodeOptions? NodeOptions;

    public override void OnOpen()
    {
        base.OnOpen();

        Size = new Vector2(1140, 880);
        SizeCondition = ImGuiCond.Appearing;
        SizeConstraints = new()
        {
            MinimumSize = new Vector2(250, 250),
            MaximumSize = new Vector2(4096, 2160)
        };
    }

    public override void Draw()
    {
        DebugRenderer.DrawPointerType(Address, Type, NodeOptions ??= new NodeOptions()
        {
            AddressPath = new AddressPath(Address),
            DefaultOpen = true,
        });
    }
}
