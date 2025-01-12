using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using ImGuiNET;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append)]
public class UnlocksTab : DebugTab
{
    public override bool IsPinnable => false;
    public override bool CanPopOut => false;
    public override bool DrawInChild => false;

    public UnlocksTab(IEnumerable<ISubTab<UnlocksTab>> subTabs)
    {
        SubTabs = subTabs.ToArray().OrderBy(t => t.Title).ToImmutableArray();
    }

    public override void Draw()
    {
        ImGui.TextUnformatted("Please select a sub-category.");
    }
}
