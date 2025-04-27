using System.Numerics;
using HaselCommon.Gui;
using HaselCommon.Services;
using HaselDebug.Interfaces;
using ImGuiNET;

namespace HaselDebug.Windows;

public class TabPopoutWindow : SimpleWindow
{
    private readonly IDebugTab _tab;

    public TabPopoutWindow(WindowManager wm, TextService textService, LanguageProvider languageProvider, IDebugTab tab) : base(wm, textService, languageProvider)
    {
        _tab = tab;
        WindowName = $"{tab.Title}##{GetType().Name}";
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
