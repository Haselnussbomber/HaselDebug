using System.Numerics;
using HaselCommon.Gui;
using HaselCommon.Services;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;

namespace HaselDebug.Windows;

public class PointerTypeWindow : SimpleWindow
{
    private NodeOptions? NodeOptions;
    private readonly DebugRenderer debugRenderer;
    private readonly nint address;
    private readonly Type type;

    public PointerTypeWindow(
        WindowManager windowManager,
        TextService textService,
        LanguageProvider languageProvider,
        DebugRenderer debugRenderer,
        nint address,
        Type type,
        string? name = null) : base(windowManager, textService, languageProvider)
    {
        this.debugRenderer = debugRenderer;
        this.address = address;
        this.type = type;
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
        debugRenderer.DrawPointerType(address, type, NodeOptions ??= new NodeOptions()
        {
            AddressPath = new AddressPath(address),
            DefaultOpen = true,
        });
    }
}
