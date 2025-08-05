using Dalamud.Plugin.Services;
using HaselCommon.Gui.ImGuiTable;

namespace HaselDebug.Tabs.UnlocksTabs.TripleTriadCards.Columns;

[RegisterTransient, AutoConstruct]
public partial class NumberColumn : ColumnString<TripleTriadCardEntry>
{
    private readonly ISeStringEvaluator _seStringEvaluator;

    [AutoPostConstruct]
    public void Initialize()
    {
        SetFixedWidth(75);
        Flags |= ImGuiTableColumnFlags.DefaultSort;
    }

    public override string ToName(TripleTriadCardEntry entry)
    {
        var isEx = entry.ResidentRow.UIPriority == 5;
        var order = (uint)entry.ResidentRow.Order;
        var addonRowId = isEx ? 9773u : 9772;
        return _seStringEvaluator.EvaluateFromAddon(addonRowId, [order]).ExtractText();
    }

    public override int Compare(TripleTriadCardEntry lhs, TripleTriadCardEntry rhs)
    {
        var result = lhs.ResidentRow.UIPriority.CompareTo(rhs.ResidentRow.UIPriority);
        if (result == 0)
            return lhs.ResidentRow.Order.CompareTo(rhs.ResidentRow.Order);
        return result;
    }
}
