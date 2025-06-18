using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Graphics;
using HaselCommon.Gui;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using ImGuiNET;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class DragDropTypeTab : DebugTab
{
    private int _selected;

    public override bool DrawInChild => false;

    public override void Draw()
    {
        const int count = 88;
        using var table = ImRaii.Table("DragDropTypeTable", count, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table)
            return;

        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 100);
        for (var i = 1; i < count; i++)
            ImGui.TableSetupColumn($"[{i}] {(DragDropType)i}", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupScrollFreeze(1, 1);
        ImGui.TableHeadersRow();

        for (var i = 1; i < count; i++)
        {
            ImGui.TableNextRow();
            if (_selected == i)
            {
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, Color.FromVector4(new(1, 1, 1, 0.15f)).ToUInt());
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, Color.FromVector4(new(1, 1, 1, 0.15f)).ToUInt());
            }
            ImGui.TableNextColumn();
            if (ImGui.Selectable($"[{i}] {(DragDropType)i}"))
                _selected = i;

            for (var j = 1; j < count; j++)
            {
                ImGui.TableNextColumn();
                if (i == j)
                    continue;

                var accepted = ((DragDropType)i).Accepts((DragDropType)j);
                ImGuiUtils.TextUnformattedColored(accepted ? Color.Green : Color.Red, $"{accepted}");
            }
        }
    }
}
