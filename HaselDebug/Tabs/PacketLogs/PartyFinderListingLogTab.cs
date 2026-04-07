using FFXIVClientStructs.FFXIV.Client.Game.Network;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.PacketLogs;

[RegisterSingleton<IPacketLogTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class PartyFinderListingLogTab : PacketLogTab<CrossRealmListingSegmentPacket>, IDisposable
{
    private Hook<InfoProxyCrossRealm.Delegates.ReceiveListing>? _hook;

    public void Dispose()
    {
        _hook?.Dispose();
        Clear();
    }

    private void ReceiveListingDetour(InfoProxyCrossRealm* thisPtr, nint packet)
    {
        AddRecord(*(CrossRealmListingSegmentPacket*)packet);
        _hook!.Original(thisPtr, packet);
    }

    public override void Draw()
    {
        _hook ??= _gameInteropProvider.HookFromAddress<InfoProxyCrossRealm.Delegates.ReceiveListing>((nint)InfoProxyCrossRealm.MemberFunctionPointers.ReceiveListing, ReceiveListingDetour);

        var enabled = IsPacketLogEnabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
            TogglePacketLog();

        ImGui.SameLine();
        if (ImGui.Button("Clear"))
            Clear();

        using var table = ImRaii.Table("PartyFinderListingLogTable"u8, 2, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable);
        if (!table) return;

        ImGui.TableSetupColumn("Time"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Packet"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        foreach (var (index, time, packet) in Records)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Time
            ImGui.Text(time.ToLongTimeString());

            ImGui.TableNextColumn(); // Packet
            _debugRenderer.DrawPointerType(packet);
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
