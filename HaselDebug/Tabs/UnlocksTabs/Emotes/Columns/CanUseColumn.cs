using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.Emotes.Columns;

[RegisterTransient]
public class CanUseColumn : ColumnYesNo<Emote>
{
    public CanUseColumn()
    {
        SetFixedWidth(75);
    }

    public override unsafe bool ToBool(Emote row)
        => AgentEmote.Instance()->CanUseEmote((ushort)row.RowId);
}
