using HaselDebug.Utils;

namespace HaselDebug.Services;

public unsafe partial class DebugRenderer
{
    public void DrawArray<T>(Span<T> span, NodeOptions nodeOptions) where T : unmanaged
    {
        if (span.Length == 0)
        {
            ImGui.Text("No values"u8);
            return;
        }

        nodeOptions = nodeOptions.WithAddress((nint)span.GetPointer(0));

        using var node = DrawTreeNode(nodeOptions.WithTitle($"{span.Length} value{(span.Length != 1 ? "s" : "")}") with { DrawSeStringTreeNode = false });
        if (!node) return;

        nodeOptions = nodeOptions.ConsumeTreeNodeOptions();

        using var table = ImRaii.Table(nodeOptions.GetKey("Array"), 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Index"u8, ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Value");
        ImGui.TableSetupScrollFreeze(2, 1);
        ImGui.TableHeadersRow();

        var type = typeof(T);
        for (var i = 0; i < span.Length; i++)
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // Index
            ImGui.Text(i.ToString());

            ImGui.TableNextColumn(); // Value
            var ptr = span.GetPointer(i);
            DrawPointerType(ptr, type, nodeOptions);
        }
    }
}
