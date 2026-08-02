using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Client.Game.Network;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs.PacketLogs;

[RegisterSingleton<IPacketLogTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class FateRewardLogTab : PacketLogTab<FateRewardPacket>, IDisposable
{
    private Hook<FateManager.Delegates.HandleFateRewardPacket>? _hook;

    public override void Dispose()
    {
        _hook?.Dispose();
        base.Dispose();
    }

    private void HandleFateRewardPacketDetour(FateManager* thisPtr, FateRewardPacket* packet)
    {
        AddRecord(*packet);
        _hook!.Original(thisPtr, packet);
    }

    public override void Draw()
    {
        _hook ??= _gameInteropProvider.HookFromAddress<FateManager.Delegates.HandleFateRewardPacket>(FateManager.MemberFunctionPointers.HandleFateRewardPacket, HandleFateRewardPacketDetour);

        if (ImGui.Button("Test"u8))
        {
            var packet = new FateRewardPacket()
            {
                GilAmount = 0,
                FateTokenTypeAmount = 0,
                FateId = 1924,
                FateTokenTypeId = 0,
                ItemId = 23,
                ItemAmount = 99,
                Flags = FateRewardFlag.Success | FateRewardFlag.Bonus,
                Medal = FateRewardMedal.Gold,
            };
            packet.ItemRewards[0].ItemId = 4802;
            packet.ItemRewards[0].Amount = 8;
            packet.ItemRewards[1].ItemId = 4803;
            packet.ItemRewards[1].Amount = 6;
            packet.ItemRewards[2].ItemId = 4804;
            packet.ItemRewards[2].Amount = 4;
            packet.ItemRewards[3].ItemId = 4805;
            packet.ItemRewards[3].Amount = 2;
            HandleFateRewardPacketDetour(FateManager.Instance(), &packet);
        }

        var enabled = IsPacketLogEnabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
            TogglePacketLog();

        ImGui.SameLine();
        if (ImGui.Button("Clear"))
            Clear();

        using var table = ImRaii.Table("FateRewardTable"u8, 2, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable);
        if (!table) return;

        ImGui.TableSetupColumn("Time"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Packet"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        foreach (var (i, time, packet) in Records)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(time.ToLongTimeString());

            ImGui.TableNextColumn();
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
