using Dalamud.Interface.Utility.Raii;
using HaselDebug.Extensions;
using HaselDebug.Utils;
using ImGuiNET;

namespace HaselDebug.Services;

public unsafe partial class DebugRenderer
{
    public void DrawStdVector(nint address, Type type, NodeOptions nodeOptions)
    {
        var size = type.SizeOf();
        if (size == 0)
        {
            ImGui.TextUnformatted($"Can't get size of {type.Name}");
            return;
        }

        var firstElement = *(nint*)address;
        var lastElement = *(nint*)(address + 8);
        var elementCount = (lastElement - firstElement) / size;
        if (elementCount == 0)
        {
            ImGui.TextUnformatted("No values");
            return;
        }

        nodeOptions = nodeOptions.WithAddress(address);

        using var node = DrawTreeNode(nodeOptions.WithTitle($"{elementCount} value{(elementCount != 1 ? "s" : "")}"));
        if (!node) return;

        using var indent = ImRaii.PushIndent(1, nodeOptions.Indent);
        using var table = ImRaii.Table(nodeOptions.GetKey("StdVectorTable"), 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg);
        if (!table) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Value");
        ImGui.TableSetupScrollFreeze(2, 1);
        ImGui.TableHeadersRow();

        for (var i = 0u; i < elementCount; i++)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Index
            ImGui.TextUnformatted(i.ToString());

            ImGui.TableNextColumn(); // Value
            DrawPointerType((nint)(firstElement + i * size), type, new NodeOptions() { AddressPath = nodeOptions.AddressPath });
        }
    }
}
