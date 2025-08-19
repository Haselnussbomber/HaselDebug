namespace HaselDebug.Tabs;
/*
public unsafe class RaptureGearsetModuleTab : DebugTab
{
    public override unsafe void Draw()
    {
        var raptureGearsetModule = RaptureGearsetModule.Instance();

        ImGui.Text($"CurrentGearsetIndex: {raptureGearsetModule->CurrentGearsetIndex}");

        if (ImGui.Button("Search for Dyeable BRD items"))
        {
            foreach (var item in GetSheet<Item>())
            {
                if (item.ClassJobCategory.Row == 50 && item.IsDyeable && item.ItemUICategory.Row == 35)
                {
                    Service.PluginLog.Debug($"{item.RowId}: {item.Name}");
                }
            }
        }

        for (var i = 0; i < 100; i++)
        {
            if (i < 16 || i > 17)
                continue;

            if (!raptureGearsetModule->IsValidGearset(i))
                continue;

            var entry = raptureGearsetModule->GetGearset(i);
            ImGui.Text($"Gearset #{entry->ID}: {MemoryHelper.ReadStringNullTerminated((nint)entry->Name)}");

            using var indent = ImRaii.PushIndent();
            for (var j = 0; j < 14; j++)
            {
                ref var item = ref entry->ItemsSpan[j];
                ImGui.Text($"Item #{j} - {Enum.GetName(typeof(RaptureGearsetModule.GearsetItemIndex), j)}");
                using var itemindent = ImRaii.PushIndent();
                ImGui.Text($"Flags: {item.Flags}");
                ImGui.Text($"Item: {item.ItemID} - {GetItemName(item.ItemID)}");
                ImGui.Text($"Glamour: {item.GlamourId} - {GetItemName(item.GlamourId)}");
            }
        }
    }
}
*/
