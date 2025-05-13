using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.AozActions;

public record struct AozEntry(AozAction AozAction, AozActionTransient AozActionTransient)
{
    public Lumina.Excel.Sheets.Action Action => AozAction.Action.Value;
}
