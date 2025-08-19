namespace HaselDebug.Tabs;

/*
public unsafe class FashionCheckManagerTab : DebugTab
{
    public string Title => "FashionCheckManager";

    public unsafe void Draw()
    {
        var manager = FashionCheckManager.Instance();

        if (manager == null)
        {
            ImGui.Text("Manager not available"u8);
            return;
        }

        Debug.DrawPointerType((nint)manager, typeof(FashionCheckManager));

        ImGui.Separator();

        ImGui.Text($"RemainingTries: {manager->RemainingTries}");
        ImGui.Text($"Highscore: {manager->Highscore}");

        ImGui.Separator();

        ImGui.Text($"Weekly Theme: {GetSheet<FashionCheckWeeklyTheme>()!.GetRow(manager->WeeklyTheme)?.Name ?? ""}");

        using var table = ImRaii.Table("FashionCheckManagerTable"u8, 3, ImGuiTableFlags.RowBg);
        if (!table)
            return;

        ImGui.TableSetupColumn("Slot");
        ImGui.TableSetupColumn("Theme");
        ImGui.TableSetupColumn("Evaluation");
        ImGui.TableHeadersRow();

        for (var i = 0; i < manager->EquipmentEvaluationsSpan.Length; i++)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(((FashioCheckEquipSlotName)i).ToString());
            ImGui.TableNextColumn();
            ImGui.Text(GetSheet<FashionCheckThemeCategory>()!.GetRow(manager->EquipmentThemes[i])?.Name ?? "");
            ImGui.TableNextColumn();
            ImGui.Text(((FashionCheckEquipEvaluation)manager->EquipmentEvaluations[i]).ToString());
        }
    }
}

public enum FashioCheckEquipSlotName {
    Weapon,
    Head,
    Body,
    Hand,
    Leg,
    Foot,
    Ear,
    Neck,
    Wrist,
    RingRight,
    RingLeft
}
*/
