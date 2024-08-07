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
            ImGui.TextUnformatted("Manager not available");
            return;
        }

        Debug.DrawPointerType((nint)manager, typeof(FashionCheckManager));

        ImGui.Separator();

        ImGui.TextUnformatted($"RemainingTries: {manager->RemainingTries}");
        ImGui.TextUnformatted($"Highscore: {manager->Highscore}");

        ImGui.Separator();

        ImGui.TextUnformatted($"Weekly Theme: {GetSheet<FashionCheckWeeklyTheme>()!.GetRow(manager->WeeklyTheme)?.Name ?? ""}");

        using var table = ImRaii.Table("FashionCheckManagerTable", 3, ImGuiTableFlags.RowBg);
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
            ImGui.TextUnformatted(((FashioCheckEquipSlotName)i).ToString());
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(GetSheet<FashionCheckThemeCategory>()!.GetRow(manager->EquipmentThemes[i])?.Name ?? "");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(((FashionCheckEquipEvaluation)manager->EquipmentEvaluations[i]).ToString());
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
