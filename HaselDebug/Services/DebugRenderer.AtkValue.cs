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
            case ValueType.Int:
                DrawNumeric((nint)(&value->Int), typeof(int), nodeOptions);
                break;
            case ValueType.Bool:
                DrawCopyableText($"{value->Byte == 0x01}");
                break;
            case ValueType.UInt:
                DrawNumeric((nint)(&value->UInt), typeof(uint), nodeOptions);
                break;
            case ValueType.Float:
                DrawNumeric((nint)(&value->Float), typeof(float), nodeOptions);
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
            case ValueType.Texture:
                DrawTexture((nint)value->Texture, nodeOptions);
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

        using var node = DrawTreeNode(nodeOptions.WithSeStringTitle($"{elementCount} value{(elementCount != 1 ? "s" : "")}"));
        if (!node) return;

        nodeOptions = nodeOptions.ConsumeTreeNodeOptions();

        using var table = ImRaii.Table(nodeOptions.GetKey("AtkValuesTable"), 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Value");
        ImGui.TableSetupScrollFreeze(3, 1);
        ImGui.TableHeadersRow();

        for (var i = 0; i < elementCount; i++)
        {
            var value = values[i];
            using var disabled = ImRaii.Disabled(value.Type is ValueType.Undefined or ValueType.Null);
            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // Index
            ImGui.TextUnformatted(i.ToString());

            ImGui.TableNextColumn(); // Type
            ImGui.TextUnformatted(value.Type.ToString());

            ImGui.TableNextColumn(); // Value
            DrawAtkValue((nint)(&value), nodeOptions);
        }
    }
}
