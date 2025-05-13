using System.Numerics;
using HaselCommon.Gui;
using HaselDebug.Interfaces;
using ImGuiNET;

namespace HaselDebug.Windows;

[AutoConstruct]
public partial class TabPopoutWindow : SimpleWindow
{
    private readonly IDebugTab _tab;

    [AutoPostConstruct]
    private void Initialize()
    {
        WindowName = $"{_tab.Title}##{GetType().Name}";
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

    public override bool DrawConditions()
    {
        return true;
    }

    public override void Draw()
    {
        _tab.Draw();
    }
}
