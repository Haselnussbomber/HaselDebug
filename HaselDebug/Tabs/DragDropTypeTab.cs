using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class DragDropTypeTab : DebugTab
{
    private int _count = 0;

    [AutoPostConstruct]
    private void Initialize()
    {
        _count = (int)Enum.GetValues<DragDropType>().Max();
    }

    public override void Draw()
    {
        for (var i = 1; i <= _count; i++)
        {
            using var treeNode = ImRaii.TreeNode($"[{i}] {(DragDropType)i}", ImGuiTreeNodeFlags.SpanAvailWidth);
            if (!treeNode) continue;

            using var table = ImRaii.Table($"DragDropTypeTable{i}", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings, new Vector2(-1, 600));
            if (!table)
                return;

            ImGui.TableSetupColumn("Type"u8, ImGuiTableColumnFlags.WidthFixed, 200);
            ImGui.TableSetupColumn("Accepted"u8);
            ImGui.TableSetupScrollFreeze(1, 1);
            ImGui.TableHeadersRow();

            for (var j = 1; j <= _count; j++)
            {
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.Text($"[{j}] {(DragDropType)j}");

                ImGui.TableNextColumn();

                if (i == j)
                    continue;

                var accepted = ((DragDropType)i).Accepts((DragDropType)j);
                ImGui.TextColored(accepted ? Color.Green : Color.Red, $"{accepted}");
            }
        }
    }
}
