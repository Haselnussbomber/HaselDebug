using FFXIVClientStructs.FFXIV.Client.Game.Event;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Utils;
using static HaselDebug.Tabs.PacketLogs.DirectorUpdateLogTab;
using EventId = FFXIVClientStructs.FFXIV.Client.Game.Event.EventId;

namespace HaselDebug.Tabs.PacketLogs;

[RegisterSingleton<IPacketLogTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class DirectorUpdateLogTab : PacketLogTab<DirectorUpdateEntry>, IDisposable
{
    private Hook<EventFramework.Delegates.ProcessDirectorUpdate>? _hook;

    [StructLayout(LayoutKind.Sequential)]
    public struct DirectorUpdateEntry
    {
        public EventId EventId;
        public uint Category;
        public uint Arg1;
        public uint Arg2;
        public uint Arg3;
        public uint Arg4;
        public uint Arg5;
        public uint Arg6;
    }

    public void Dispose()
    {
        _hook?.Dispose();
        Clear();
    }

    private void ProcessDirectorUpdateDetour(EventFramework* thisPtr, EventId eventId, uint category, uint arg1, uint arg2, uint arg3, uint arg4, uint arg5, uint arg6)
    {
        AddRecord(new DirectorUpdateEntry()
        {
            EventId = eventId,
            Category = category,
            Arg1 = arg1,
            Arg2 = arg2,
            Arg3 = arg3,
            Arg4 = arg4,
            Arg5 = arg5,
            Arg6 = arg6,
        });

        _hook!.Original(thisPtr, eventId, category, arg1, arg2, arg3, arg4, arg5, arg6);
    }

    public override void Draw()
    {
        _hook ??= _gameInteropProvider.HookFromAddress<EventFramework.Delegates.ProcessDirectorUpdate>(EventFramework.MemberFunctionPointers.ProcessDirectorUpdate, ProcessDirectorUpdateDetour);

        var enabled = IsPacketLogEnabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
            TogglePacketLog();

        ImGui.SameLine();
        if (ImGui.Button("Clear"))
            Clear();

        using var table = ImRaii.Table("ActorControlLogTable"u8, 10, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable);
        if (!table) return;

        ImGui.TableSetupColumn("Time"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("ContentId"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("EntryId"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Category"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Arg2"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Arg3"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Arg4"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Arg5"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Arg6"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Arg7"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        foreach (var (index, time, payload) in Records)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(time.ToLongTimeString());

            ImGui.TableNextColumn();
            ImGuiUtils.DrawCopyableText(payload.Value->EventId.ContentId.ToString());

            ImGui.TableNextColumn();
            ImGuiUtils.DrawCopyableText(payload.Value->EventId.EntryId.ToString());

            ImGui.TableNextColumn();
            ImGuiUtils.DrawCopyableText(payload.Value->Category.ToString("X"));

            ImGui.TableNextColumn();
            _debugRenderer.DrawNumber(payload.Value->Arg1, new NodeOptions() { HexOnShift = true });

            ImGui.TableNextColumn();
            _debugRenderer.DrawNumber(payload.Value->Arg2, new NodeOptions() { HexOnShift = true });

            ImGui.TableNextColumn();
            _debugRenderer.DrawNumber(payload.Value->Arg3, new NodeOptions() { HexOnShift = true });

            ImGui.TableNextColumn();
            _debugRenderer.DrawNumber(payload.Value->Arg4, new NodeOptions() { HexOnShift = true });

            ImGui.TableNextColumn();
            _debugRenderer.DrawNumber(payload.Value->Arg5, new NodeOptions() { HexOnShift = true });

            ImGui.TableNextColumn();
            _debugRenderer.DrawNumber(payload.Value->Arg6, new NodeOptions() { HexOnShift = true });
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
