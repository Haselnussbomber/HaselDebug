using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Gui.ImGuiTable;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Emotes.Columns;

[RegisterTransient]
public class CanUseColumn : ColumnBool<Emote>
{
    public CanUseColumn()
    {
        SetFixedWidth(75);
    }

    public override unsafe bool ToBool(Emote row)
        => AgentEmote.Instance()->CanUseEmote((ushort)row.RowId);
}
