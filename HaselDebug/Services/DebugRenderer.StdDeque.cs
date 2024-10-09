using Dalamud.Interface.Utility.Raii;
using HaselCommon.Extensions.Reflection;
using HaselDebug.Utils;
using ImGuiNET;

namespace HaselDebug.Services;

public unsafe partial class DebugRenderer
{
    public void DrawStdDeque(nint address, Type type, NodeOptions nodeOptions)
    {
        var elementCount = *(ulong*)(address + 0x20); // MySize
        if (elementCount == 0)
        {
            ImGui.TextUnformatted("No values");
            return;
        }

        nodeOptions = nodeOptions.WithAddress(address);

        using var node = DrawTreeNode(nodeOptions.WithSeStringTitle($"{elementCount} value{(elementCount != 1 ? "s" : "")}"));
        if (!node) return;

        nodeOptions = nodeOptions.ConsumeTreeNodeOptions();

        var typeSize = type.SizeOf();
        var blockSize =
            typeSize <= 1 ? 16 :
            typeSize <= 2 ? 8 :
            typeSize <= 4 ? 4 :
            typeSize <= 8 ? 2 :
            1;

        var map = *(nint**)(address + 0x8);
        var mapSize = *(ulong*)(address + 0x10);
        var myOff = *(ulong*)(address + 0x18);

        using var indent = ImRaii.PushIndent(1, nodeOptions.Indent);
        using var table = ImRaii.Table(nodeOptions.GetKey("StdDequeTable"), 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Value");
        ImGui.TableSetupScrollFreeze(2, 1);
        ImGui.TableHeadersRow();

        for (var i = 0ul; i < elementCount; i++)
        {
            var actualIndex = myOff + i;
            var block = actualIndex / (ulong)blockSize & mapSize - 1;
            var offset = actualIndex % (ulong)blockSize;
            var valueAddress = map[block] + (nint)offset * typeSize; // TODO: check this again. this was originally map[block][offset], so doing pointer arithmetic

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Index
            ImGui.TextUnformatted(i.ToString());

            ImGui.TableNextColumn(); // Value
            DrawPointerType(valueAddress, type, new NodeOptions() { AddressPath = nodeOptions.AddressPath.With(valueAddress) });
        }
    }
}
