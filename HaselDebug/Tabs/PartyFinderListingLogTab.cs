using Dalamud.Game.Gui.PartyFinder.Types;
using FFXIVClientStructs.FFXIV.Client.Game.Network;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class PartyFinderListingLogTab : DebugTab, IDisposable
{
    private readonly ILogger<PartyFinderListingLogTab> _logger;
    private readonly DebugRenderer _debugRenderer;
    private readonly IGameInteropProvider _gameInteropProvider;

    private readonly List<(DateTime Time, Pointer<PartyFinderListingPacket> Packet)> _records = [];
    private Hook<InfoProxyCrossRealm.Delegates.ReceiveListing>? _receiveListingHook;
    private Hook<AgentLookingForGroup.Delegates.PopulateListingData>? _populateListingHook;

    private bool _enabled = false;

    public void Dispose()
    {
        _receiveListingHook?.Dispose();
        _populateListingHook?.Dispose();
        Clear();
    }

    private void Clear()
    {
        foreach (var (_, Packet) in _records)
            Marshal.FreeHGlobal((nint)Packet.Value);

        _records.Clear();
    }

    private void ReceiveListingDetour(InfoProxyCrossRealm* thisPtr, nint packet)
    {
        var ptr = (PartyFinderListingPacket*)Marshal.AllocHGlobal(sizeof(PartyFinderListingPacket));
        *ptr = *(PartyFinderListingPacket*)packet;
        _records.Add((DateTime.Now, (Pointer<PartyFinderListingPacket>)ptr));
        _receiveListingHook!.Original(thisPtr, packet);
    }

    private void PopulateListingDataDetour(AgentLookingForGroup* thisPtr, AgentLookingForGroup.Detailed* listingData)
    {
        _logger.LogDebug("Listing: {Id} | {_memberContentIds} | {_slotFlags}", listingData->ListingId, string.Join(',', listingData->MemberContentIds.ToArray().Select(x => x.ToString("X"))), string.Join(',', listingData->MemberContentIds.ToArray().Select(x => x.ToString("X"))));
        _populateListingHook!.Original(thisPtr, listingData);
    }

    public override void Draw()
    {
        _receiveListingHook ??= _gameInteropProvider.HookFromAddress<InfoProxyCrossRealm.Delegates.ReceiveListing>((nint)InfoProxyCrossRealm.MemberFunctionPointers.ReceiveListing, ReceiveListingDetour);
        _populateListingHook ??= _gameInteropProvider.HookFromAddress<AgentLookingForGroup.Delegates.PopulateListingData>((nint)AgentLookingForGroup.MemberFunctionPointers.PopulateListingData, PopulateListingDataDetour);

        if (ImGui.Checkbox("Enabled", ref _enabled))
        {
            if (_enabled && !_receiveListingHook.IsEnabled)
            {
                _receiveListingHook.Enable();
                _populateListingHook.Enable();
            }
            else if (!_enabled && _receiveListingHook.IsEnabled)
            {
                _receiveListingHook.Disable();
                _populateListingHook.Disable();
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Clear"))
            Clear();

        using var table = ImRaii.Table("PartyFinderListingLogTable"u8, 2, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable);
        if (!table) return;

        ImGui.TableSetupColumn("Time"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Packet"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        for (var i = _records.Count - 1; i >= 0; i--)
        {
            var (time, packet) = _records[i];

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(time.ToLongTimeString());

            ImGui.TableNextColumn();
            _debugRenderer.DrawPointerType(packet);
        }
    }
}
