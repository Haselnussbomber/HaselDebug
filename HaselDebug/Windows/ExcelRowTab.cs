using System.Numerics;
using Dalamud.Game;
using HaselCommon.Gui;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;

namespace HaselDebug.Windows;

[AutoConstruct]
public partial class ExcelRowTab : SimpleWindow
{
    private readonly DebugRenderer _debugRenderer;
    private readonly Type _rowType;
    private readonly uint _rowId;
    private readonly ClientLanguage _language;

    [AutoPostConstruct]
    private void Initialize(string windowName)
    {
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
        _debugRenderer.DrawExdRow(_rowType, _rowId, 0, new NodeOptions() { DefaultOpen = true, Language = _language });
    }
}
