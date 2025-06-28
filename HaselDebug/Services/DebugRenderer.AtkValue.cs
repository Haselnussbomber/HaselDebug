using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselDebug.Utils;
using ImGuiNET;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace HaselDebug.Services;

public unsafe partial class DebugRenderer
{
    public void DrawAtkValue(nint address, NodeOptions nodeOptions)
    {
        nodeOptions = nodeOptions.WithAddress(address);

        var value = (AtkValue*)address;
        switch (value->Type)
        {
            case ValueType.Undefined:
                ImGui.TextUnformatted("Undefined");
                break;
            case ValueType.Null:
                ImGui.TextUnformatted("Null");
                break;
            case ValueType.Bool:
                ImGuiUtilsEx.DrawCopyableText($"{value->Byte == 0x01}");
                break;
            case ValueType.Int:
                DrawNumeric((nint)(&value->Int), typeof(int), nodeOptions);
                break;
            case ValueType.Int64:
                DrawNumeric((nint)(&value->Int64), typeof(long), nodeOptions);
                break;
            case ValueType.UInt:
                DrawNumeric((nint)(&value->UInt), typeof(uint), nodeOptions);
                break;
            case ValueType.UInt64:
                DrawNumeric((nint)(&value->UInt64), typeof(ulong), nodeOptions);
                break;
            case ValueType.Float:
                DrawNumeric((nint)(&value->Float), typeof(float), nodeOptions);
                break;
            case ValueType.WideString:
                ImGui.TextUnformatted(value->ToString());
                break;
            case ValueType.String:
            case ValueType.String8:
            case ValueType.ManagedString:
                DrawSeString(value->String, nodeOptions);
                break;
            case ValueType.Vector:
            case ValueType.ManagedVector:
                DrawStdVector((nint)value->Vector, typeof(AtkValue), nodeOptions);
                break;
            case ValueType.Pointer:
                DrawNumeric((nint)(&value->Pointer), typeof(nint), nodeOptions);
                break;
            case ValueType.AtkValues:
                ImGui.TextUnformatted(value->ToString());
                break;
            default:
                ImGui.TextUnformatted(value->ToString());
                break;
        }
    }

    public void DrawAtkValues(AtkValue* values, ushort elementCount, NodeOptions nodeOptions)
    {
        if (elementCount == 0)
        {
            ImGui.TextUnformatted("No values");
            return;
        }

        nodeOptions = nodeOptions.WithAddress((nint)values);

        using var node = DrawTreeNode(nodeOptions.WithTitle($"{elementCount} value{(elementCount != 1 ? "s" : "")}") with { DrawSeStringTreeNode = false });
        if (!node) return;

        nodeOptions = nodeOptions.ConsumeTreeNodeOptions();

        using var table = ImRaii.Table(nodeOptions.GetKey("AtkValuesTable"), 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Value");
        ImGui.TableSetupScrollFreeze(3, 1);
        ImGui.TableHeadersRow();

        var valueCount = 0;
        var span = new Span<AtkValue>(values, elementCount);

        for (var i = 0; i < elementCount; i++)
        {
            var value = span.GetPointer(i);
            using var disabled = ImRaii.Disabled(value->Type is ValueType.Undefined or ValueType.Null);

            if (value->Type == ValueType.Int && i < elementCount - 1 && span.GetPointer(i + 1)->Type == ValueType.AtkValues)
                valueCount = value->Int;
            else if (value->Type != ValueType.AtkValues)
                valueCount = 0;

            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // Index
            ImGui.TextUnformatted(i.ToString());

            ImGui.TableNextColumn(); // Type
            ImGui.TextUnformatted(value->Type.ToString());

            ImGui.TableNextColumn(); // Value

            if (value->Type == ValueType.AtkValues && valueCount > 0)
            {
                DrawAtkValues(value->AtkValues, (ushort)valueCount, nodeOptions);
            }
            else
            {
                DrawAtkValue((nint)value, nodeOptions);
            }
        }
    }
}
