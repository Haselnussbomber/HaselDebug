using FFXIVClientStructs.FFXIV.Client.Game;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using static HaselDebug.Tabs.PacketLogs.SetRestoreLogTab;

namespace HaselDebug.Tabs.PacketLogs;

[RegisterSingleton<IPacketLogTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class SetRestoreLogTab : PacketLogTab<SetRestoreEntry>, IDisposable
{
    private Hook<MirageManager.Delegates.RestorePrismBoxSetItem>? _hook;

    [StructLayout(LayoutKind.Sequential)]
    public struct SetRestoreEntry
    {
        public uint ItemIndex;
        public byte Bits0;
        public byte Bits1;
    }

    public void Dispose()
    {
        _hook?.Dispose();
        Clear();
    }

    private bool RestorePrismBoxSetItemDetour(MirageManager* thisPtr, uint itemIndex, byte* restoreBits)
    {
        AddRecord(new SetRestoreEntry()
        {
            ItemIndex = itemIndex,
            Bits0 = *restoreBits,
            Bits1 = *(restoreBits + 1),
        });

        return _hook!.Original(thisPtr, itemIndex, restoreBits);
    }

    public override void Draw()
    {
        _hook ??= _gameInteropProvider.HookFromAddress<MirageManager.Delegates.RestorePrismBoxSetItem>(MirageManager.MemberFunctionPointers.RestorePrismBoxSetItem, RestorePrismBoxSetItemDetour);

        var enabled = IsPacketLogEnabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
            TogglePacketLog();

        ImGui.SameLine();
        if (ImGui.Button("Clear"))
            Clear();

        using var table = ImRaii.Table("SetRestoreLogTable"u8, 4, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable);
        if (!table) return;

        ImGui.TableSetupColumn("Time"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("ItemIndex"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Bits0"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Bits1"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        foreach (var (index, time, payload) in Records)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(time.ToLongTimeString());

            ImGui.TableNextColumn();
            ImGuiUtils.DrawCopyableText(payload.Value->ItemIndex.ToString());

            ImGui.TableNextColumn();
            ImGuiUtils.DrawCopyableText(Convert.ToString(payload.Value->Bits0, 2));

            ImGui.TableNextColumn();
            ImGuiUtils.DrawCopyableText(Convert.ToString(payload.Value->Bits1, 2));
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


// 

[RegisterSingleton<IPacketLogTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class CabinetStoreLogTab : PacketLogTab<uint>, IDisposable
{
    private Hook<StoreCabinetItemDelegate>? _hook;

    public void Dispose()
    {
        _hook?.Dispose();
        Clear();
    }

    private bool StoreCabinetItemDetour(FFXIVClientStructs.FFXIV.Client.Game.UI.Cabinet* thisPtr, uint cabinetId)
    {
        AddRecord(cabinetId);
        return _hook!.Original(thisPtr, cabinetId);
    }

    public delegate bool StoreCabinetItemDelegate(FFXIVClientStructs.FFXIV.Client.Game.UI.Cabinet* thisPtr, uint cabinetId);

    public override void Draw()
    {
        _hook ??= _gameInteropProvider.HookFromSignature<StoreCabinetItemDelegate>("E8 ?? ?? ?? ?? 45 33 E4 41 C6 46 ?? ?? 4D 89 66", StoreCabinetItemDetour);

        var enabled = IsPacketLogEnabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
            TogglePacketLog();

        ImGui.SameLine();
        if (ImGui.Button("Clear"))
            Clear();

        using var table = ImRaii.Table("CabinetStoreLogTable"u8, 2, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable);
        if (!table) return;

        ImGui.TableSetupColumn("Time"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("CabinetId"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        foreach (var (index, time, payload) in Records)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(time.ToLongTimeString());

            ImGui.TableNextColumn();
            ImGuiUtils.DrawCopyableText(payload.Value->ToString());
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
