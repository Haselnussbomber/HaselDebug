namespace HaselDebug.Tabs;
/*
public unsafe partial class WindowsTab : DebugTab, IDisposable
{
    private bool _showPicker = false;
    private string _hoveredWindowName = "";
    private Vector2 _hoveredWindowPos;
    private Vector2 _hoveredWindowSize;
    private AtkUnitBase* _hoveredWindow = null;
    private AtkUnitBase* _pickedWindow = null;

    private static readonly string[] IgnoredAddons = new[] {
        "CharaCardEditMenu", // always opens docked to CharaCard (OnSetup)
    };

    public unsafe WindowsTab()
    {
        SetupVTableHooks();
        RaptureAtkUnitManager_Vf6Hook?.Enable();
    }

    public void Dispose()
    {
        RaptureAtkUnitManager_Vf6Hook?.Dispose();
    }

    /*
    public override void OnConfigWindowClose()
    {
        _hoveredWindowName = "";
        _hoveredWindowPos = default;
        _hoveredWindowSize = default;
        _hoveredWindow = null;
        _showPicker = false;
    }
    * /

    public override void Draw()
    {
        if (_showPicker)
        {
            if (ImGui.Button("Cancel"))
            {
                _showPicker = false;
            }
        }
        else
        {
            if (ImGui.Button("Pick Window"))
            {
                _hoveredWindowName = "";
                _hoveredWindowPos = default;
                _hoveredWindowSize = default;
                _hoveredWindow = null;
                _showPicker = true;
            }
        }

        if (_showPicker && _hoveredWindowPos != default)
        {
            ImGui.SetNextWindowPos(_hoveredWindowPos);
            ImGui.SetNextWindowSize(_hoveredWindowSize);

            using var windowBorderSize = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1.0f);
            using var borderColor = ImRaii.PushColor(ImGuiCol.Border, (uint)Colors.Gold);
            using var windowBgColor = ImRaii.PushColor(ImGuiCol.WindowBg, new Vector4(0.847f, 0.733f, 0.49f, 0.33f));

            if (ImGui.Begin("Lock Windows Picker", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize))
            {
                var drawList = ImGui.GetForegroundDrawList();
                var textPos = _hoveredWindowPos + new Vector2(0, -ImGui.GetTextLineHeight());
                drawList.AddText(textPos + Vector2.One, Colors.Black, _hoveredWindowName);
                drawList.AddText(textPos, Colors.Gold, _hoveredWindowName);

                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    _showPicker = false;
                    _pickedWindow = _hoveredWindow;
                }

                ImGui.End();
            }
        }

        if (_pickedWindow != null)
        {
            DebugUtils.DrawPointerType((nint)_pickedWindow, typeof(AtkUnitBase));
        }
    }

    [VTableHook<HaselRaptureAtkUnitManager>(6)]
    public bool RaptureAtkUnitManager_Vf6(HaselRaptureAtkUnitManager* self, nint a2)
    {
        if (_showPicker)
        {
            if (a2 != 0)
            {
                var atkUnitBase = *(AtkUnitBase**)(a2 + 8);
                if (atkUnitBase != null && atkUnitBase->WindowNode != null && atkUnitBase->WindowCollisionNode != null)
                {
                    var name = MemoryHelper.ReadStringNullTerminated((nint)atkUnitBase->Name);
                    if (!IgnoredAddons.Contains(name))
                    {
                        _hoveredWindowName = name;
                        _hoveredWindowPos = new(atkUnitBase->X, atkUnitBase->Y);
                        _hoveredWindowSize = new(atkUnitBase->WindowNode->AtkResNode.Width, atkUnitBase->WindowNode->AtkResNode.Height - 7);
                        _hoveredWindow = atkUnitBase;
                    }
                    else
                    {
                        _hoveredWindowName = "";
                        _hoveredWindowPos = default;
                        _hoveredWindowSize = default;
                        _hoveredWindow = null;
                    }
                }
                else
                {
                    _hoveredWindowName = "";
                    _hoveredWindowPos = default;
                    _hoveredWindowSize = default;
                    _hoveredWindow = null;
                }
            }
            else
            {
                _showPicker = false;
            }

            return false;
        }

        return RaptureAtkUnitManager_Vf6Hook.OriginalDisposeSafe(self, a2);
    }
}
*/
