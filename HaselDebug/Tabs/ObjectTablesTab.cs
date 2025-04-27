using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append)]
public class ObjectTablesTab : DebugTab
{
    public override unsafe bool DrawInChild => !AgentLobby.Instance()->IsLoggedIn;
    public override bool IsPinnable => false;
    public override bool CanPopOut => false;

    public ObjectTablesTab(IEnumerable<IObjectTableTab> subTabs)
    {
        SubTabs = subTabs
            .OrderBy(t => t.Title).ToArray()
            .Cast<IDebugTab>().ToImmutableArray();
    }
}
