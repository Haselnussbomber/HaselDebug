using System.Numerics;
using HaselCommon.Gui;
using HaselCommon.Services;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;

namespace HaselDebug.Windows;

public class ExcelRowTab : SimpleWindow
{
    private readonly DebugRenderer _debugRenderer;
    private readonly Type _rowType;
    private readonly uint _rowId;

    public ExcelRowTab(
        WindowManager windowManager,
        TextService textService,
        LanguageProvider languageProvider,
        DebugRenderer debugRenderer,
        Type rowType,
        uint rowId,
        string windowName = "Excel Row") : base(windowManager, textService, languageProvider)
    {
        _debugRenderer = debugRenderer;
        _rowType = rowType;
        _rowId = rowId;
        WindowName = windowName;
    }

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
        _debugRenderer.DrawExdRow(_rowType, _rowId, 0, new NodeOptions() { DefaultOpen = true });
    }
}
