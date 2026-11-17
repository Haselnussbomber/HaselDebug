using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.Game;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Utils;
using static HaselDebug.Tabs.PacketLogs.ExecuteCommandLogTab;

namespace HaselDebug.Tabs.PacketLogs;

[RegisterSingleton<IPacketLogTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class ExecuteCommandLogTab : PacketLogTab<ExecuteCommandEntry>
{
    private Hook<GameMain.Delegates.ExecuteCommand>? _hook;

    [StructLayout(LayoutKind.Sequential)]
    public struct ExecuteCommandEntry
    {
        public int Command;
        public int Param1;
        public int Param2;
        public int Param3;
        public int Param4;
    }

    public override ValueTask DisposeAsync()
    {
        Clear();
        return new ValueTask(_framework.Run(() => DisposeAndNull(ref _hook)));
    }

    private bool ExecuteCommandDetour(int command, int param1, int param2, int param3, int param4)
    {
        AddRecord(new ExecuteCommandEntry()
        {
            Command = command,
            Param1 = param1,
            Param2 = param2,
            Param3 = param3,
            Param4 = param4,
        });

        return _hook!.Original(command, param1, param2, param3, param4);
    }

    public override void Draw()
    {
        _hook ??= _gameInteropProvider.HookFromAddress<GameMain.Delegates.ExecuteCommand>(GameMain.MemberFunctionPointers.ExecuteCommand, ExecuteCommandDetour);

        var enabled = IsPacketLogEnabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
            TogglePacketLog();

        ImGui.SameLine();
        if (ImGui.Button("Clear"))
            Clear();

        using var table = ImRaii.Table("ExecuteCommandLogTable"u8, 6, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable);
        if (!table) return;

        ImGui.TableSetupColumn("Time"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Command"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Param1"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Param2"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Param3"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Param4"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        foreach (var (index, time, payload) in Records)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(time.ToLongTimeString());

            ImGui.TableNextColumn();
            _debugRenderer.DrawNumber(payload.Value->Command, new NodeOptions() { HexOnShift = true });

            ImGui.TableNextColumn();
            _debugRenderer.DrawNumber(payload.Value->Param1, new NodeOptions() { HexOnShift = true });

            ImGui.TableNextColumn();
            _debugRenderer.DrawNumber(payload.Value->Param2, new NodeOptions() { HexOnShift = true });

            ImGui.TableNextColumn();
            _debugRenderer.DrawNumber(payload.Value->Param3, new NodeOptions() { HexOnShift = true });

            ImGui.TableNextColumn();
            _debugRenderer.DrawNumber(payload.Value->Param4, new NodeOptions() { HexOnShift = true });
        }
    }

    public override void EnablePacketLog()
    {
        _hook!.Enable();
        IsPacketLogEnabled = _hook.IsEnabled;
    }

    public override void DisablePacketLog()
    {
        _hook!.Disable();
        IsPacketLogEnabled = _hook.IsEnabled;
    }
}
