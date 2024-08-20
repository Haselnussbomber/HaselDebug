using System.Numerics;
using HaselCommon.Services;
using HaselCommon.Windowing;
using HaselDebug.Abstracts;
using HaselDebug.Services;
using HaselDebug.Tabs;
using ImGuiNET;

namespace HaselDebug.Windows;

public class TabPopoutWindow(WindowManager wm, IDrawableTab tab) : SimpleWindow(wm, tab.Title)
{
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

    public static TabPopoutWindow Create(WindowManager wm, DebugRenderer dr, nint ptr, Type type)
    {
        return new TabPopoutWindow(wm, new PinnedInstanceTab(dr, ptr, type));
    }

    public override void Draw()
    {
        tab.Draw();
    }
}