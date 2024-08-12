using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Dalamud.Game;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.STD;
using HaselCommon.Extensions;
using HaselCommon.Services;
using HaselCommon.Utils;
using HaselDebug.Extensions;
using HaselDebug.Utils;
using ImGuiNET;
using InteropGenerator.Runtime.Attributes;
using KernelTexture = FFXIVClientStructs.FFXIV.Client.Graphics.Kernel.Texture;

namespace HaselDebug.Services;

#pragma warning disable SeStringRenderer
public unsafe partial class DebugRenderer(
    WindowManager WindowManager,
    ITextureProvider TextureProvider,
    ImGuiContextMenuService ImGuiContextMenuService)
{
    private MethodInfo? GetSheetGeneric;

    public HaselColor ColorModifier { get; } = new(0.5f, 0.5f, 0.75f, 1);
    public HaselColor ColorType { get; } = new(0.2f, 0.9f, 0.9f, 1);
    public HaselColor ColorFieldName { get; } = new(0.2f, 0.9f, 0.4f, 1);
    public HaselColor ColorTreeNode { get; } = new(1, 1, 0, 1);
    public HaselColor ColorObsolete { get; } = new(1, 1, 0, 1);
    public HaselColor ColorObsoleteError { get; } = new(1, 0, 0, 1);

    private readonly Dictionary<Type, string[]> KnownStringPointers = new() {
        { typeof(FFXIVClientStructs.FFXIV.Client.UI.Agent.MapMarkerBase), ["Subtext"] },
        { typeof(FFXIVClientStructs.FFXIV.Common.Component.Excel.ExcelSheet), ["SheetName"] }
    };

    public void DrawPointerType(void* obj, Type? type, NodeOptions nodeOptions)
        => DrawPointerType((nint)obj, type, nodeOptions);

    public void DrawPointerType(nint address, Type? type, NodeOptions nodeOptions)
    {
        if (type == null)
        {
            ImGui.TextUnformatted("");
            return;
        }

        if (address == 0 || address < 0x140000000)
        {
            ImGui.TextUnformatted("null");
            return;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Pointer<>))
        {
            address = *(nint*)address;
            type = type.GenericTypeArguments[0];
        }

        if (type == null)
        {
            ImGui.TextUnformatted("");
            return;
        }

        if (address == 0 || address < 0x140000000)
        {
            ImGui.TextUnformatted("null");
            return;
        }

        nodeOptions = nodeOptions.WithAddress(address);

        if (type.IsVoid())
        {
            ImGui.TextUnformatted($"0x{address:X}"); // TODO: what did I do here?
            return;
        }

        if (type.IsPointer)
        {
            type = type.GetElementType();
            address = *(nint*)address;
            DrawPointerType(address, type, nodeOptions);
            return;
        }
        else if (type == typeof(bool))
        {
            ImGui.TextUnformatted($"{*(bool*)address}");
            return;
        }
        else if (type == typeof(BitVector32))
        {
            ImGui.TextUnformatted($"{*(BitVector32*)address}");
            return;
        }
        else if (type == typeof(Utf8String))
        {
            DrawUtf8String(address, nodeOptions);
            return;
        }
        else if (type == typeof(KernelTexture))
        {
            DrawTexture(address, nodeOptions);
            return;
        }
        else if (type == typeof(AtkTexture))
        {
            DrawAtkTexture(address, nodeOptions);
            return;
        }
        else if (type == typeof(AtkValue))
        {
            DrawAtkValue(address, nodeOptions);
            return;
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(StdVector<>))
        {
            DrawStdVector(address, type.GenericTypeArguments[0], nodeOptions);
            return;
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(StdMap<,>))
        {
            DrawStdMap(address, type, nodeOptions);
            return;
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(StdList<>))
        {
            DrawStdList(address, type, nodeOptions);
            return;
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(StdDeque<>))
        {
            DrawStdDeque(address, type, nodeOptions);
            return;
        }
        else if (type.IsEnum)
        {
            DrawEnum(address, type, nodeOptions);
            return;
        }
        else if (type.IsNumericType())
        {
            DrawNumeric(address, type, nodeOptions);
            return;
        }
        else if (type.IsStruct() || type.IsClass)
        {
            DrawStruct(address, type, nodeOptions);
            return;
        }

        ImGui.TextUnformatted("Unsupported Type");
    }

    private ImRaii.IEndObject DrawTreeNode(NodeOptions nodeOptions)
    {
        using var titleColor = ImRaii.PushColor(ImGuiCol.Text, (uint)ColorTreeNode);
        var node = ImRaii.TreeNode((nodeOptions.Title?.ToString() ?? string.Empty) + nodeOptions.GetKey("Node"), nodeOptions.GetTreeNodeFlags());
        titleColor?.Dispose();

        /*
        ImGui.SameLine();
         
        using (ImRaii.PushColor(ImGuiCol.Text, (uint)ColorTreeNode))
        {
            ImGuiHelpers.SeStringWrapped(nodeOptions.Title.AsSpan(), new()
            {
                ForceEdgeColor = true,
                WrapWidth = 9999
            });
        }
        */

        if (nodeOptions.OnHovered != null && ImGui.IsItemHovered())
            nodeOptions.OnHovered();

        nodeOptions.DrawContextMenu?.Invoke(nodeOptions);

        return node;
    }

    private void DrawStruct(nint address, Type type, NodeOptions nodeOptions)
    {
        nodeOptions = nodeOptions.WithAddress(address);

        using var node = DrawTreeNode(nodeOptions.WithTitleIfNull(type.FullName ?? "Unknown Type Name"));
        if (!node) return;

        var fields = type
            .GetFields(BindingFlags.Default | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(fieldInfo => !fieldInfo.IsLiteral) // no constants
            .Where(fieldInfo => !fieldInfo.IsStatic)
            .OrderBy(fieldInfo => fieldInfo.GetFieldOffset())
            .Select(fieldInfo => (
                Info: fieldInfo,
                Offset: fieldInfo.GetFieldOffset(),
                Size: fieldInfo.IsFixed() ? fieldInfo.GetFixedType().SizeOf() * fieldInfo.GetFixedSize() : fieldInfo.FieldType.SizeOf()));

        var i = 0;
        foreach (var (fieldInfo, offset, size) in fields)
        {
            i++;
            DrawCopyableText($"[0x{offset:X}]", $"{address + offset:X}", textColor: Colors.Grey3);
            ImGui.SameLine();

            var indexedAddressPath = nodeOptions.AddressPath.With(i);

            var fieldAddress = address + offset;
            var fieldType = fieldInfo.FieldType;

            if (fieldInfo.GetCustomAttribute<ObsoleteAttribute>() is ObsoleteAttribute obsoleteAttribute)
            {
                using (ImRaii.PushColor(ImGuiCol.Text, (uint)(obsoleteAttribute.IsError ? ColorObsoleteError : ColorObsolete)))
                    ImGui.TextUnformatted("[Obsolete]");

                if (!string.IsNullOrEmpty(obsoleteAttribute.Message) && ImGui.IsItemHovered())
                    ImGui.SetTooltip(obsoleteAttribute.Message);

                ImGui.SameLine();
            }

            if (fieldInfo.IsStatic)
            {
                ImGui.TextUnformatted("static");
                ImGui.SameLine();
            }

            DrawCopyableText(fieldType.ReadableTypeName(), fieldType.ReadableTypeName(ImGui.IsKeyDown(ImGuiKey.LeftShift)), textColor: ColorType);
            ImGui.SameLine();

            // TODO: is this all even necessary

            // delegate*
            if (fieldType.IsFunctionPointer || fieldType.IsUnmanagedFunctionPointer)
            {
                ImGui.TextColored(ColorFieldName, fieldInfo.Name);
                ImGui.SameLine();
                DrawAddress(*(nint*)fieldAddress);
                continue;
            }

            // internal FixedSizeArrays
            if (fieldInfo.IsAssembly
                && fieldInfo.GetCustomAttribute<FixedSizeArrayAttribute>() is FixedSizeArrayAttribute fixedSizeArrayAttribute
                && fieldType.GetCustomAttribute<InlineArrayAttribute>() is InlineArrayAttribute inlineArrayAttribute)
            {
                ImGui.TextColored(ColorFieldName, $"{fieldInfo.Name[1..].FirstCharToUpper()}");
                ImGui.SameLine();
                DrawFixedSizeArray(fieldAddress, fieldType, fixedSizeArrayAttribute.IsString, new());
                continue;
            }

            // StdVector<>
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(StdVector<>))
            {
                var underlyingType = fieldType.GenericTypeArguments[0];
                var underlyingTypeSize = underlyingType.SizeOf();
                if (underlyingTypeSize == 0)
                {
                    ImGui.TextColored(Colors.Red, $"Can't get size of {underlyingType.Name}");
                    continue;
                }

                ImGui.TextColored(ColorFieldName, fieldInfo.Name);
                ImGui.SameLine();
                DrawStdVector(fieldAddress, underlyingType, new NodeOptions() { AddressPath = indexedAddressPath });
                continue;
            }

            // StdDeque<>
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(StdDeque<>))
            {
                var underlyingType = fieldType.GenericTypeArguments[0];
                var underlyingTypeSize = underlyingType.SizeOf();
                if (underlyingTypeSize == 0)
                {
                    ImGui.TextColored(Colors.Red, $"Can't get size of {underlyingType.Name}");
                    continue;
                }

                ImGui.TextColored(ColorFieldName, fieldInfo.Name);
                ImGui.SameLine();
                DrawStdDeque(fieldAddress, underlyingType, new NodeOptions() { AddressPath = indexedAddressPath });
                continue;
            }

            // StdList<>
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(StdList<>))
            {
                var underlyingType = fieldType.GenericTypeArguments[0];
                var underlyingTypeSize = underlyingType.SizeOf();
                if (underlyingTypeSize == 0)
                {
                    ImGui.TextColored(Colors.Red, $"Can't get size of {underlyingType.Name}");
                    continue;
                }

                ImGui.TextColored(ColorFieldName, fieldInfo.Name);
                ImGui.SameLine();
                DrawStdList(fieldAddress, underlyingType, new NodeOptions() { AddressPath = indexedAddressPath });
                continue;
            }

            // AtkUnitBase.AtkValues
            if ((type == typeof(AtkUnitBase) || type.GetCustomAttribute<InheritsAttribute<AtkUnitBase>>() != null) && fieldType == typeof(AtkValue*) && fieldInfo.Name == "AtkValues")
            {
                ImGui.TextColored(ColorFieldName, fieldInfo.Name);
                ImGui.SameLine();
                DrawAtkValues(*(AtkValue**)fieldAddress, ((AtkUnitBase*)address)->AtkValuesCount, new NodeOptions() { AddressPath = nodeOptions.AddressPath.With(fieldAddress) });
                continue;
            }

            // byte* that are strings
            if (fieldType.IsPointer && KnownStringPointers.TryGetValue(type, out var fieldNames) && fieldNames.Contains(fieldInfo.Name))
            {
                ImGui.TextColored(ColorFieldName, fieldInfo.Name);
                ImGui.SameLine();
                DrawSeString(*(byte**)fieldAddress, new NodeOptions() { AddressPath = nodeOptions.AddressPath.With(fieldAddress) });
                continue;
            }

            // TODO: vector preview
            // TODO: enum values table

            ImGui.TextColored(ColorFieldName, fieldInfo.Name);
            ImGui.SameLine();

            if (fieldType == typeof(uint) && fieldInfo.Name == "IconId")
                DrawIcon(*(uint*)fieldAddress);

            DrawPointerType(fieldAddress, fieldType, new NodeOptions() { AddressPath = indexedAddressPath });
        }
    }

    private void DrawEnum(nint address, Type type, NodeOptions nodeOptions)
    {
        nodeOptions = nodeOptions.WithAddress(address);

        var underlyingType = type.GetEnumUnderlyingType();
        var value = DrawNumeric(address, underlyingType, nodeOptions);
        if (value == null)
            return;

        if (type.GetCustomAttribute<FlagsAttribute>() != null)
        {
            ImGui.SameLine();
            ImGui.TextUnformatted(" - ");
            var bits = (uint)Math.Pow(2, Marshal.SizeOf(underlyingType) + 1);
            for (var i = 0u; i < bits; i++)
            {
                var bitValue = (uint)Math.Pow(2, i);
                if ((Convert.ToUInt64(value) & bitValue) != 0)
                {
                    ImGui.SameLine();
                    DrawCopyableText(type.GetEnumName(bitValue)?.ToString() ?? $"{bitValue}", $"{bitValue}");
                }
            }
        }
        else
        {
            ImGui.SameLine();
            ImGui.TextUnformatted(type.GetEnumName(value)?.ToString() ?? "");
        }
    }

    public void DrawCopyableText(string text, string? textCopy = null, string? tooltipText = null, bool asSelectable = false, Vector4? textColor = null)
    {
        textCopy ??= text;
        textColor ??= (Vector4)Colors.White;

        using (ImRaii.PushColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32((Vector4)textColor)))
        {
            if (asSelectable)
                ImGui.Selectable(text);
            else
                ImGui.TextUnformatted(text);
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            ImGui.SetTooltip(tooltipText ?? textCopy);
        }

        if (ImGui.IsItemClicked())
            ImGui.SetClipboardText(textCopy);
    }

    public void DrawAddress(void* obj)
        => DrawAddress((nint)obj);

    public void DrawAddress(nint address)
    {
        if (address == 0)
        {
            ImGui.TextUnformatted("");
            return;
        }

        var sigScanner = Service.Get<ISigScanner>();

        if (address > sigScanner.Module.BaseAddress && !ImGui.IsKeyDown(ImGuiKey.LeftShift))
        {
            DrawCopyableText($"+0x{address - sigScanner.Module.BaseAddress:X}");
            return;
        }
        else
        {
            DrawCopyableText($"0x{address:X}");
        }
    }


    public object? DrawNumeric(nint address, Type type, NodeOptions nodeOptions)
    {
        object? value = null;

        switch (type)
        {
            case Type t when t == typeof(nint):
                DrawAddress(*(nint*)address);
                break;

            case Type t when t == typeof(Half):
                value = *(Half*)address;
                DrawCopyableText($"{value}");
                break;

            case Type t when t == typeof(byte):
                value = *(byte*)address;
                DrawCopyableText($"{value}");
                ImGui.SameLine();
                DrawCopyableText($"0x{value:X}");
                break;

            case Type t when t == typeof(sbyte):
                value = *(sbyte*)address;
                DrawCopyableText($"{value}");
                ImGui.SameLine();
                DrawCopyableText($"0x{value:X}");
                break;

            case Type t when t == typeof(short):
                value = *(short*)address;
                DrawCopyableText($"{value}");
                ImGui.SameLine();
                DrawCopyableText($"0x{value:X}");
                break;

            case Type t when t == typeof(ushort):
                value = *(ushort*)address;
                DrawCopyableText($"{value}");
                ImGui.SameLine();
                DrawCopyableText($"0x{value:X}");
                break;

            case Type t when t == typeof(int):
                value = *(int*)address;
                DrawCopyableText($"{value}");
                ImGui.SameLine();
                DrawCopyableText($"0x{value:X}");
                break;

            case Type t when t == typeof(uint):
                value = *(uint*)address;
                DrawCopyableText($"{value}");
                ImGui.SameLine();
                DrawCopyableText($"0x{value:X}");
                break;

            case Type t when t == typeof(long):
                value = *(long*)address;
                DrawCopyableText($"{value}");
                ImGui.SameLine();
                DrawCopyableText($"0x{value:X}");
                break;

            case Type t when t == typeof(ulong):
                value = *(ulong*)address;
                DrawCopyableText($"{value}");
                ImGui.SameLine();
                DrawCopyableText($"0x{value:X}");
                break;

            case Type t when t == typeof(decimal):
                value = *(decimal*)address;
                DrawCopyableText($"{value}");
                break;

            case Type t when t == typeof(double):
                value = *(double*)address;
                DrawCopyableText($"{value}");
                break;

            case Type t when t == typeof(float):
                value = *(float*)address;
                DrawCopyableText($"{value}");
                break;

            default:
                ImGui.TextUnformatted($"Unhandled NumericType {type.FullName}");
                break;
        }

        return value;
    }

    /*
    public string ToBitsString(ulong value, int bits)
    {
        var bitsString = new StringBuilder();

        for (var i = bits - 1; i >= 0; i--)
        {
            if ((i + 1) % 4 == 0)
                bitsString.Append(' ');
            bitsString.Append(Convert.ToString(value / (ulong)(1 << i) % 2));
        }

        return bitsString.ToString();
    }

    public string ToBitsString(byte byteIn)
        => ToBitsString(byteIn, 8);

    public string ToBitsString(ushort byteIn)
        => ToBitsString(byteIn, 16);

    public string ToBitsString(uint byteIn)
        => ToBitsString(byteIn, 32);
    */

    public void DrawIcon(uint iconId, bool isHq = false, bool sameLine = true, DrawInfo drawInfo = default)
    {
        drawInfo.DrawSize ??= new Vector2(ImGui.GetTextLineHeight());

        if (iconId == 0)
        {
            ImGui.Dummy(drawInfo.DrawSize.Value);
            if (sameLine)
                ImGui.SameLine();
            return;
        }

        if (!ImGuiUtils.IsInViewport(drawInfo.DrawSize.Value))
        {
            ImGui.Dummy(drawInfo.DrawSize.Value);
            if (sameLine)
                ImGui.SameLine();
            return;
        }

        if (TextureProvider.TryGetFromGameIcon(new GameIconLookup(iconId, isHq), out var tex) && tex.TryGetWrap(out var texture, out _))
        {
            ImGui.Image(texture.ImGuiHandle, drawInfo.DrawSize.Value);

            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                ImGui.BeginTooltip();
                ImGui.TextUnformatted("Click to copy IconId");
                ImGui.TextUnformatted($"ID: {iconId} – Size: {texture.Width}x{texture.Height}");
                ImGui.Image(texture.ImGuiHandle, new(texture.Width, texture.Height));
                ImGui.EndTooltip();
            }

            if (ImGui.IsItemClicked())
                ImGui.SetClipboardText(iconId.ToString());
        }
        else
        {
            ImGui.Dummy(drawInfo.DrawSize.Value);
        }

        if (sameLine)
            ImGui.SameLine();
    }
}