using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselDebug.Utils;

namespace HaselDebug.Extensions;

public static unsafe class AtkUnitManagerExtensions
{
    public static AtkUnitBase* GetAddonByNodeSafe(ref this AtkUnitManager atkUnitManager, AtkResNode* needle)
    {
        if (!MemoryUtils.IsPointerValid(needle))
            return null;

        var count = atkUnitManager.AllLoadedUnitsList.Count;
        if (count == 0)
            return null;

        for (var i = 0; i < count; i++)
        {
            AtkUnitBase* unitBase = atkUnitManager.AllLoadedUnitsList.Entries[i];
            if (unitBase == null)
                continue;

            if (!unitBase->IsReady)
                continue;

            if (unitBase->UldManager.ContainsNode(needle))
                return unitBase;
        }

        return null;
    }
}
