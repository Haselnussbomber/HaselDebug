using System.Globalization;
using System.Reflection;

namespace HaselDebug.Services;

public unsafe partial class DebugRenderer
{
    private void DrawBitFields(Type structType, nint fieldAddress, int fieldOffset, Type fieldType, FieldInfo fieldInfo)
    {
        foreach (var structAttr in structType.GetCustomAttributes())
        {
            var structAttrType = structAttr.GetType();
            if (!structAttrType.IsGenericType)
                continue;

            if (structAttrType.GetGenericTypeDefinition() != typeof(InheritsAttribute<>))
                continue;

            var parentStructType = structAttrType.GenericTypeArguments[0];
            var parentFieldInfo = parentStructType.GetField(fieldInfo.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (parentFieldInfo != null)
            {
                DrawBitFields(parentStructType, fieldAddress, fieldOffset, fieldType, parentFieldInfo);
            }
        }

        foreach (var attr in fieldInfo.GetCustomAttributes())
        {
            var attrType = attr.GetType();
            if (!attrType.IsGenericType)
                continue;

            if (attrType.GetGenericTypeDefinition() != typeof(BitFieldAttribute<>))
                continue;

            DrawBitField(structType, fieldAddress, fieldOffset, fieldType, attr, attrType);
        }
    }

    private void DrawBitField(Type structType, nint fieldAddress, int fieldOffset, Type fieldType, Attribute attr, Type attrType)
    {
        var bitfieldType = attrType.GenericTypeArguments[0];
        var name = (string)attrType.GetProperty("Name")!.GetValue(attr)!;
        var index = (int)attrType.GetProperty("Index")!.GetValue(attr)!;
        var length = (int)attrType.GetProperty("Length")!.GetValue(attr)!;

        ImGuiUtils.DrawCopyableText($"[0x{fieldOffset:X}]", new()
        {
            CopyText = ImGui.IsKeyDown(ImGuiKey.LeftShift) ? $"{fieldAddress + fieldOffset:X}" : $"0x{fieldOffset:X}",
            TextColor = Color.Text600
        });

        ImCursor.SameLineSpace();

        ImGui.TextColored(ColorBitField, $"[{index}:{length}]");
        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            using var tooltip = ImRaii.Tooltip();
            ImGui.TextColored(ColorBitField, "BitField"u8);
            ImGui.Text($"Index: {index} \u2022 Length: {length}");
        }
        if (ImGui.IsItemClicked())
        {
            ImGui.SetClipboardText($"[{index}:{length}]");
        }

        ImGui.SameLine();

        var fieldValue = 0UL;

        switch (fieldType)
        {
            case Type tu when tu == typeof(byte):
                fieldValue = *(byte*)fieldAddress;
                break;

            case Type tu when tu == typeof(ushort):
                fieldValue = *(ushort*)fieldAddress;
                break;

            case Type tu when tu == typeof(uint):
                fieldValue = *(uint*)fieldAddress;
                break;

            case Type tu when tu == typeof(ulong):
                fieldValue = *(ulong*)fieldAddress;
                break;

            default:
                ImGui.Text($"FieldType {fieldType.Name} not supported");
                return;
        }

        ImGuiUtils.DrawCopyableText(bitfieldType.ReadableTypeName(), new()
        {
            CopyText = bitfieldType.ReadableTypeName(ImGui.IsKeyDown(ImGuiKey.LeftShift)),
            TextColor = ColorType
        });

        ImGui.SameLine();

        var fullName = $"{structType.FullName}.{name}";
        var hasDoc = HasDocumentation(fullName);
        var startPos = ImCursor.ScreenPosition;

        ImGuiUtils.DrawCopyableText(name, new CopyableTextOptions() { NoTooltip = true, TextColor = ColorFieldName });

        if (hasDoc)
        {
            var textSize = ImGui.CalcTextSize(name);
            ImGui.GetWindowDrawList().AddLine(startPos + new Vector2(0, textSize.Y), startPos + textSize, ColorFieldName.ToUInt());
        }

        if (ImGui.IsItemHovered())
        {
            using var tooltip = ImRaii.Tooltip();
            ImGui.TextColored(ColorFieldName, name);

            if (hasDoc)
            {
                using var font = _pluginInterface.UiBuilder.MonoFontHandle.Push();
                var doc = GetDocumentation(fullName);
                if (doc != null)
                {
                    ImGui.Separator();

                    if (!string.IsNullOrEmpty(doc.Sumamry))
                        ImGui.Text(doc.Sumamry);

                    if (!string.IsNullOrEmpty(doc.Remarks))
                        ImGui.Text(doc.Remarks);

                    if (doc.Parameters.Length > 0)
                    {
                        foreach (var param in doc.Parameters)
                        {
                            ImGui.Text($"{param.Key}: {param.Value}");
                        }
                    }

                    if (!string.IsNullOrEmpty(doc.Returns))
                        ImGui.Text(doc.Returns);
                }
            }
        }

        ImGui.SameLine();

        switch (bitfieldType)
        {
            case Type t when t == typeof(bool):
                {
                    var value = BitOps.GetBit(fieldValue, index);
                    ImGuiUtils.DrawCopyableText($"{value}");
                    break;
                }

            case Type t when t == typeof(sbyte) || t == typeof(byte) || t == typeof(short) || t == typeof(ushort) || t == typeof(int) || t == typeof(uint) || t == typeof(long) || t == typeof(ulong):
                {
                    var value = BitOps.GetBits(fieldValue, index, BitOps.CreateLowBitMask<ulong>(length));

                    if (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift))
                        ImGuiUtils.DrawCopyableText(ToHexString(value, typeof(ulong)));
                    else
                        ImGuiUtils.DrawCopyableText(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);

                    break;
                }

            case Type t when t.IsEnum:
                {
                    var value = BitOps.GetBits(fieldValue, index, BitOps.CreateLowBitMask<ulong>(length));
                    ImGuiUtils.DrawCopyableText(value.ToString());

                    if (t.GetCustomAttribute<FlagsAttribute>() != null)
                    {
                        ImGui.SameLine();
                        ImGui.Text(" - "u8);
                        var bitwidth = t.GetEnumUnderlyingType().SizeOf() * 8;
                        for (var i = 0; i < bitwidth; i++)
                        {
                            if (!BitOps.GetBit(value, i))
                                continue;

                            ImGui.SameLine();
                            var bitValue = 1ul << i;
                            ImGuiUtils.DrawCopyableText(Enum.GetName(t, bitValue)?.ToString() ?? $"{bitValue}", new()
                            {
                                CopyText = $"{bitValue}"
                            });
                        }
                    }
                    else
                    {
                        ImGui.SameLine();
                        ImGuiUtils.DrawCopyableText(Enum.GetName(t, value)?.ToString() ?? "");
                    }

                    break;
                }

            default:
                ImGui.Text($"BitField Type {bitfieldType.Name} not supported");
                break;
        }
    }
}
