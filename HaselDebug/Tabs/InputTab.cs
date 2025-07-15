using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class InputTab : DebugTab
{
    public override bool DrawInChild => true;

    public override void Draw()
    {
        using var hostchild = ImRaii.Child("InputTabChild", new Vector2(-1), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
        if (!hostchild) return;

        using var tabbar = ImRaii.TabBar("InputTabTabBar");
        if (!tabbar) return;

        DrawInputs();
        DrawInputIdStates();
        DrawKeyState();
    }

    private void DrawKeyState()
    {
        using var tab = ImRaii.TabItem("KeyState states");
        if (!tab) return;

        using var table = ImRaii.Table("KeyStateTable", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("SeVirtualKey", ImGuiTableColumnFlags.WidthFixed, 300);
        ImGui.TableSetupColumn("Press", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Down", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Held", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Released", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        var uiInputData = UIInputData.Instance();

        foreach (var seVirtualKey in Enum.GetValues<SeVirtualKey>())
        {
            var isPress = uiInputData->IsKeyPressed(seVirtualKey);
            var isDown = uiInputData->IsKeyDown(seVirtualKey);
            var isReleased = uiInputData->IsKeyReleased(seVirtualKey);
            var isHeld = uiInputData->IsKeyHeld(seVirtualKey);

            if (!isPress && !isDown && !isReleased && !isHeld)
                continue;

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{seVirtualKey}");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{isPress}");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{isDown}");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{isHeld}");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{isReleased}");
        }
    }

    private void DrawInputIdStates()
    {
        using var tab = ImRaii.TabItem("InputId states");
        if (!tab) return;

        using var table = ImRaii.Table("FnStateTable", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.ScrollY);
        if (!table) return;

        ImGui.TableSetupColumn("InputId", ImGuiTableColumnFlags.WidthFixed, 300);
        ImGui.TableSetupColumn("Press", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Down", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Held", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Released", ImGuiTableColumnFlags.WidthFixed, 100);
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
            ImGui.TextUnformatted($"{inputId}");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{isPress}");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{isDown}");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{isHeld}");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{isReleased}");
        }
    }

    private void DrawInputs()
    {
        using var tab = ImRaii.TabItem("Inputs");
        if (!tab) return;

        using var table = ImRaii.Table("InputsTable", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.ScrollY);
        if (!table) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("InputId", ImGuiTableColumnFlags.WidthFixed, 300);
        ImGui.TableSetupColumn("Press", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Down", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Held", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Released", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        var uiInputData = UIInputData.Instance();

        for (var i = 0; i < 675; i++)
        {
            var isPress = uiInputData->IsInputIdPressed((InputId)i);
            var isDown = uiInputData->IsInputIdDown((InputId)i);
            var isHeld = uiInputData->IsInputIdHeld((InputId)i);
            var isReleased = uiInputData->IsInputIdReleased((InputId)i);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{i}");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{(InputId)i}");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{isPress}");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{isDown}");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{isHeld}");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{isReleased}");
        }
    }
}
