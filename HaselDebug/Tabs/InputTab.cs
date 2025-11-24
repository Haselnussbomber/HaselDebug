using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class InputTab : DebugTab
{
    private readonly ExcelService _excelService;
    private readonly Dictionary<InputId, ConfigKey> _inputKey2ConfigKey = [];
    private bool _initialized;

    public override bool DrawInChild => true;

    private void Initialize()
    {
        foreach (var inputId in Enum.GetValues<InputId>())
        {
            if (_excelService.TryFindRow<ConfigKey>(row => row.Label.ToString() == Enum.GetName(inputId)?.ToString(), out var configKeyRow))
                _inputKey2ConfigKey[inputId] = configKeyRow;
        }
    }

    public override void Draw()
    {
        if (!_initialized)
        {
            Initialize();
            _initialized = true;
        }

        using var hostchild = ImRaii.Child("InputTabChild", new Vector2(-1), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
        if (!hostchild) return;

        using var tabbar = ImRaii.TabBar("InputTabTabBar");
        if (!tabbar) return;

        DrawInputs();
        DrawInputIdStates();
        DrawKeyState();
    }

    private void DrawInputs()
    {
        using var tab = ImRaii.TabItem("Inputs");
        if (!tab) return;

        using var table = ImRaii.Table("InputsTable"u8, 11, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Hideable | ImGuiTableFlags.Reorderable);
        if (!table) return;

        ImGui.TableSetupColumn("Index"u8, ImGuiTableColumnFlags.WidthFixed, 30);
        ImGui.TableSetupColumn("InputId"u8, ImGuiTableColumnFlags.WidthFixed, 300);
        ImGui.TableSetupColumn("Display Name"u8, ImGuiTableColumnFlags.WidthFixed, 300);
        ImGui.TableSetupColumn("Binding 1"u8, ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableSetupColumn("Binding 2"u8, ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableSetupColumn("Controller Binding 1"u8, ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableSetupColumn("Controller Binding 2"u8, ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableSetupColumn("Press"u8, ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Down"u8, ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Held"u8, ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Released"u8, ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        var uiInputData = UIInputData.Instance();

        for (var i = 0; i < uiInputData->NumKeybinds; i++)
        {
            var keybind = uiInputData->GetKeybind(i);
            var isPress = uiInputData->IsInputIdPressed((InputId)i);
            var isDown = uiInputData->IsInputIdDown((InputId)i);
            var isHeld = uiInputData->IsInputIdHeld((InputId)i);
            var isReleased = uiInputData->IsInputIdReleased((InputId)i);

            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGuiUtils.DrawCopyableText($"{i}");

            ImGui.TableNextColumn();
            ImGuiUtils.DrawCopyableText($"{(InputId)i}");

            ImGui.TableNextColumn();
            if (_inputKey2ConfigKey.TryGetValue((InputId)i, out var configKeyRow))
                ImGuiUtils.DrawCopyableText(configKeyRow.Text.ToString());

            ImGui.TableNextColumn();
            DrawKeybind(ref keybind->KeySettings[0]);

            ImGui.TableNextColumn();
            DrawKeybind(ref keybind->KeySettings[1]);

            ImGui.TableNextColumn();
            DrawKeybind(ref keybind->GamepadSettings[0]);

            ImGui.TableNextColumn();
            DrawKeybind(ref keybind->GamepadSettings[1]);

            ImGui.TableNextColumn();
            ImGui.Text($"{isPress}");

            ImGui.TableNextColumn();
            ImGui.Text($"{isDown}");

            ImGui.TableNextColumn();
            ImGui.Text($"{isHeld}");

            ImGui.TableNextColumn();
            ImGui.Text($"{isReleased}");
        }
    }

    private void DrawInputIdStates()
    {
        using var tab = ImRaii.TabItem("InputId states");
        if (!tab) return;

        using var table = ImRaii.Table("FnStateTable"u8, 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.ScrollY);
        if (!table) return;

        ImGui.TableSetupColumn("InputId"u8, ImGuiTableColumnFlags.WidthFixed, 300);
        ImGui.TableSetupColumn("Press"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Down"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Held"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Released"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        var uiInputData = UIInputData.Instance();

        foreach (var inputId in Enum.GetValues<InputId>())
        {
            var isPress = uiInputData->IsInputIdPressed(inputId);
            var isDown = uiInputData->IsInputIdDown(inputId);
            var isHeld = uiInputData->IsInputIdHeld(inputId);
            var isReleased = uiInputData->IsInputIdReleased(inputId);

            if (!isPress && !isDown && !isHeld && !isReleased)
                continue;

            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGuiUtils.DrawCopyableText($"{inputId}");

            ImGui.TableNextColumn();
            ImGui.Text($"{isPress}");

            ImGui.TableNextColumn();
            ImGui.Text($"{isDown}");

            ImGui.TableNextColumn();
            ImGui.Text($"{isHeld}");

            ImGui.TableNextColumn();
            ImGui.Text($"{isReleased}");
        }
    }

    private void DrawKeyState()
    {
        using var tab = ImRaii.TabItem("KeyState states");
        if (!tab) return;

        using var table = ImRaii.Table("KeyStateTable"u8, 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.ScrollY);
        if (!table) return;

        ImGui.TableSetupColumn("SeVirtualKey"u8, ImGuiTableColumnFlags.WidthFixed, 300);
        ImGui.TableSetupColumn("Press"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Down"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Held"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Released"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        var uiInputData = UIInputData.Instance();

        foreach (var seVirtualKey in Enum.GetValues<SeVirtualKey>())
        {
            if ((int)seVirtualKey >= uiInputData->KeyboardInputs.KeyState.Length)
                break;

            var isPress = uiInputData->IsKeyPressed(seVirtualKey);
            var isDown = uiInputData->IsKeyDown(seVirtualKey);
            var isReleased = uiInputData->IsKeyReleased(seVirtualKey);
            var isHeld = uiInputData->IsKeyHeld(seVirtualKey);

            if (!isPress && !isDown && !isReleased && !isHeld)
                continue;

            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGuiUtils.DrawCopyableText($"{seVirtualKey}");

            ImGui.TableNextColumn();
            ImGui.Text($"{isPress}");

            ImGui.TableNextColumn();
            ImGui.Text($"{isDown}");

            ImGui.TableNextColumn();
            ImGui.Text($"{isHeld}");

            ImGui.TableNextColumn();
            ImGui.Text($"{isReleased}");
        }
    }

    private static void DrawKeybind(ref KeySetting keySetting)
    {
        if (keySetting.Key == SeVirtualKey.NO_KEY)
        {
            ImGui.Text("-"u8);
            return;
        }

        if ((int)keySetting.Key < 167)
        {
            if (keySetting.KeyModifier == KeyModifierFlag.None)
                ImGuiUtils.DrawCopyableText($"{keySetting.Key}");
            else
                ImGuiUtils.DrawCopyableText($"{keySetting.KeyModifier}+{keySetting.Key}");

            return;
        }

        if (keySetting.GamepadModifier == GamepadModifierFlag.None)
            ImGuiUtils.DrawCopyableText($"{keySetting.Key}");
        else
            ImGuiUtils.DrawCopyableText($"{keySetting.GamepadModifier}+{keySetting.Key}");
    }
}
