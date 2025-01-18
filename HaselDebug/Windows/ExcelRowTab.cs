using System.Numerics;
using HaselCommon.Gui;
using HaselCommon.Services;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;

namespace HaselDebug.Windows;

public class ExcelRowTab(
    WindowManager windowManager,
    DebugRenderer debugRenderer,
    Type rowType,
    uint rowId,
    string windowName = "Excel Row") : SimpleWindow(windowManager, windowName)
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

        Flags |= ImGuiWindowFlags.NoSavedSettings;

        RespectCloseHotkey = true;
        DisableWindowSounds = true;
    }

    public override bool DrawConditions()
    {
        return true;
    }

    public override void Draw()
    {
        debugRenderer.DrawExdSheet(rowType, rowId, 0, new NodeOptions() { DefaultOpen = true });
    }
}
