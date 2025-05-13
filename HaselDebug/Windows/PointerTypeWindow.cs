using System.Numerics;
using HaselCommon.Gui;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using Microsoft.Extensions.DependencyInjection;

namespace HaselDebug.Windows;

[AutoConstruct]
public partial class PointerTypeWindow : SimpleWindow
{
    private DebugRenderer _debugRenderer;
    private NodeOptions? _nodeOptions;
    private nint _address;
    private Type _type;

    [AutoPostConstruct]
    private void Initialize(IServiceProvider serviceProvider, nint address, Type type, string name)
    {
        _debugRenderer = serviceProvider.GetRequiredService<DebugRenderer>();
        _address = address;
        _type = type;
        WindowName = $"{name}##{type.Name}";
    }

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
        _debugRenderer.DrawPointerType(_address, _type, _nodeOptions ??= new NodeOptions()
        {
            AddressPath = new AddressPath(_address),
            DefaultOpen = true,
        });
    }
}
