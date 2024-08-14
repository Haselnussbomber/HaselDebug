using Dalamud.Interface.Utility.Raii;
using HaselDebug.Utils;
using ImGuiNET;

namespace HaselDebug.Services;

public unsafe partial class DebugRenderer
{
    public void DrawStdList(nint address, Type type, NodeOptions nodeOptions)
    {
        if (*(nint*)address == 0)
        {
            ImGui.TextUnformatted("Not initialized");
            return;
        }

        var elementCount = *(ulong*)(address + 0x8);
        if (elementCount == 0)
        {
            ImGui.TextUnformatted("No values");
            return;
        }

        nodeOptions = nodeOptions.WithAddress(address);

        using var node = DrawTreeNode(nodeOptions.WithSeStringTitle($"{elementCount} value{(elementCount != 1 ? "s" : "")}"));
        if (!node) return;

        nodeOptions = nodeOptions.ConsumeTreeNodeOptions();

        var _head = **(nint**)address;
        var _current = _head;

        bool MoveNext()
        {
            if (_head == 0 || *(nint*)_current == _head)
                return false;
            _current = *(nint*)_current;
            return true;
        }

        using var indent = ImRaii.PushIndent(1, nodeOptions.Indent);
        using var table = ImRaii.Table(nodeOptions.GetKey("StdListTable"), 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Value");
        ImGui.TableSetupScrollFreeze(2, 1);
        ImGui.TableHeadersRow();

        var i = 0;
        while (MoveNext())
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Index
            ImGui.TextUnformatted(i.ToString());

            ImGui.TableNextColumn(); // Value
            DrawPointerType(_current + 0x10, type, new NodeOptions() { AddressPath = nodeOptions.AddressPath });
            i++;
        }
    }
}
