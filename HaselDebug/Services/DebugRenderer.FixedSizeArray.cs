using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Dalamud.Interface.Utility.Raii;
using HaselCommon.Extensions.Reflection;
using HaselCommon.Graphics;
using HaselDebug.Utils;
using ImGuiNET;

namespace HaselDebug.Services;

public unsafe partial class DebugRenderer
{
    public void DrawFixedSizeArray(nint address, Type type, bool isString, NodeOptions nodeOptions)
    {
        if (type.GetCustomAttribute<InlineArrayAttribute>() is not InlineArrayAttribute inlineArrayAttribute)
            return;

        var elementCount = inlineArrayAttribute.Length;
        if (elementCount == 0)
        {
            ImGui.Dummy(Vector2.Zero);
            return;
        }

        nodeOptions = nodeOptions.WithAddress(address);

        var fieldType = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)[0].FieldType;
        if (isString)
        {
            if (fieldType == typeof(char))
                ImGui.TextUnformatted(new string((char*)address));
            else
                DrawSeString((byte*)address, nodeOptions);

            return;
        }

        var typeSize = type.SizeOf();
        if (typeSize == 0)
        {
            ImGui.TextColored(Color.Red, $"Can't get size of {type.Name}");
            return;
        }

        using var node = DrawTreeNode(nodeOptions.WithSeStringTitle($"{elementCount} value{(elementCount != 1 ? "s" : "")}"));
        if (!node) return;

        nodeOptions = nodeOptions.ConsumeTreeNodeOptions();

        using var table = ImRaii.Table(nodeOptions.GetKey("FixedSizeArrayTable"), 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Value");
        ImGui.TableSetupScrollFreeze(2, 1);
        ImGui.TableHeadersRow();

        var fieldSize = typeSize / elementCount;

        using var indent = ImRaii.PushIndent(1, nodeOptions.Indent);
        for (var i = 0u; i < elementCount; i++)
        {
            var entryAddress = (nint)(address + i * fieldSize);
            var entryAddressPath = nodeOptions.AddressPath.With(entryAddress);

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Index
            ImGui.TextUnformatted(i.ToString());

            ImGui.TableNextColumn(); // Value
            DrawPointerType(entryAddress, fieldType, new NodeOptions() { AddressPath = entryAddressPath });
        }
    }
}
