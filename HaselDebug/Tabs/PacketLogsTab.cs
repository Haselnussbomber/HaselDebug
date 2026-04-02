using System.Collections.Immutable;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append)]
public class PacketLogsTab : DebugTab
{
    public override unsafe bool DrawInChild => !AgentLobby.Instance()->IsLoggedIn;
    public override bool IsPinnable => false;
    public override bool CanPopOut => false;

    public PacketLogsTab(IEnumerable<IPacketLogTab> subTabs)
    {
        SubTabs = subTabs
            .OrderBy(t => t.Title, StringComparer.Ordinal)
            .Cast<IDebugTab>()
            .ToImmutableArray();
    }
}
