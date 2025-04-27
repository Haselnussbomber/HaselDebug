using System.Numerics;
using HaselCommon.Gui;
using HaselCommon.Services;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;

namespace HaselDebug.Windows;

public class PointerTypeWindow : SimpleWindow
{
    private NodeOptions? _nodeOptions;
    private readonly DebugRenderer _debugRenderer;
    private readonly nint _address;
    private readonly Type _type;

    public PointerTypeWindow(
        WindowManager windowManager,
        TextService textService,
        LanguageProvider languageProvider,
        DebugRenderer debugRenderer,
        nint address,
        Type type,
        string? name = null) : base(windowManager, textService, languageProvider)
    {
        _debugRenderer = debugRenderer;
        _address = address;
        _type = type;
        WindowName = $"{name ?? string.Empty}##{type.Name}";
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
