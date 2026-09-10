namespace HaselDebug.Tabs.UnlocksTabs.AozActions;

public record struct AozEntry(AozAction AozAction, AozActionTransient AozActionTransient)
{
    public HaselDebug.Excel.Sheets.Action Action => AozAction.Action.Value;
}
