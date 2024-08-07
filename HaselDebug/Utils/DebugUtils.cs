using System.Collections.Specialized;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Dalamud.Game;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiSeStringRenderer;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.STD;
using HaselCommon.Extensions;
using HaselCommon.Utils;
using HaselDebug.Extensions;
using ImGuiNET;
using InteropGenerator.Runtime.Attributes;
using Lumina.Text.ReadOnly;
using KernelTexture = FFXIVClientStructs.FFXIV.Client.Graphics.Kernel.Texture;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace HaselDebug.Utils;

#pragma warning disable SeStringRenderer
public static unsafe class DebugUtils
{
    public static HaselColor ColorModifier { get; } = new(0.5f, 0.5f, 0.75f, 1f);
    public static HaselColor ColorType { get; } = new(0.2f, 0.9f, 0.9f, 1);
    public static HaselColor ColorName { get; } = new(0.2f, 0.9f, 0.4f, 1);

    public static void DrawPointerType(void* obj, Type? type, NodeOptions nodeOptions)
        => DrawPointerType((nint)obj, type, nodeOptions);

    public static void DrawPointerType(nint address, Type? type, NodeOptions nodeOptions)
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

        nodeOptions.EnsureAddressInPath(address);

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

    private static void DrawStruct(nint address, Type type, NodeOptions nodeOptions)
    {
        nodeOptions.EnsureAddressInPath(address);

        using var titleColor = ImRaii.PushColor(ImGuiCol.Text, 0xFF00FFFF);

        var flags = ImGuiTreeNodeFlags.SpanAvailWidth;
        if (nodeOptions.DefaultOpen)
            flags |= ImGuiTreeNodeFlags.DefaultOpen;

        using var node = ImRaii.TreeNode($"##Node{nodeOptions.AddressPath}", flags);

        if (nodeOptions.OnHovered != null && ImGui.IsItemHovered())
            nodeOptions.OnHovered();

        if (nodeOptions.TextOffsetX > 0)
            ImGui.SameLine(nodeOptions.TextOffsetX + ImGui.GetStyle().FramePadding.X * 3f + ImGui.GetFontSize(), 0);
        else
            ImGui.SameLine();

        ImGuiHelpers.SeStringWrapped((nodeOptions.TitleOverride ?? new(Encoding.UTF8.GetBytes(type.FullName ?? "Unknown Type Name"))).AsSpan(), new()
        {
            ForceEdgeColor = true,
            WrapWidth = 9999
        });

        if (!node)
            return;

        titleColor?.Dispose();

        var fields = type
            .GetFields(BindingFlags.Default | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            //.Where(fieldInfo => !Attribute.IsDefined(fieldInfo, typeof(ObsoleteAttribute)))
            //.Where(fieldInfo => !Attribute.IsDefined(fieldInfo, typeof(CExportIgnoreAttribute)))
            .Where(fieldInfo => !fieldInfo.IsLiteral) // not constants
            .Where(fieldInfo => !fieldInfo.IsStatic) // not static
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

            var indexedAddressPath = nodeOptions.AddressPath!.Value.With(i);

            var fieldAddress = address + offset;
            var fieldType = fieldInfo.FieldType;

            if (fieldInfo.GetCustomAttribute<ObsoleteAttribute>() is ObsoleteAttribute obsoleteAttribute)
            {
                using (ImRaii.PushColor(ImGuiCol.Text, obsoleteAttribute.IsError ? 0xFF0000FF : 0xFF00FFFF))
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

            if (fieldInfo.FieldType.IsFunctionPointer || fieldInfo.FieldType.IsUnmanagedFunctionPointer)
            {
                DrawCopyableText(fieldInfo.FieldType.ReadableTypeName(), fieldInfo.FieldType.ReadableTypeName(ImGui.IsKeyDown(ImGuiKey.LeftShift)), textColor: ColorType);
                ImGui.SameLine();
                ImGui.TextColored(ColorName, fieldInfo.Name);
                ImGui.SameLine();
                DrawAddress(*(nint*)fieldAddress);
                continue;
            }

            if (fieldInfo.IsAssembly
                && fieldInfo.GetCustomAttribute<FixedSizeArrayAttribute>() is FixedSizeArrayAttribute fixedSizeArrayAttribute
                && fieldInfo.FieldType.GetCustomAttribute<InlineArrayAttribute>() is InlineArrayAttribute inlineArrayAttribute)
            {
                DrawCopyableText(fieldInfo.FieldType.ReadableTypeName(), fieldInfo.FieldType.ReadableTypeName(ImGui.IsKeyDown(ImGuiKey.LeftShift)), textColor: ColorType);
                ImGui.SameLine();
                ImGui.TextColored(ColorName, $"{fieldInfo.Name[1..].FirstCharToUpper()}");
                ImGui.SameLine();
                DrawFixedSizeArray(fieldAddress, fieldInfo.FieldType, fixedSizeArrayAttribute.IsString, new NodeOptions());
                continue;
            }

            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(StdVector<>))
            {
                var underlyingType = fieldType.GenericTypeArguments[0];
                var underlyingTypeSize = underlyingType.SizeOf();
                if (underlyingTypeSize == 0)
                {
                    ImGui.TextColored(Colors.Red, $"Can't get size of {underlyingType.Name}");
                    continue;
                }

                DrawCopyableText(fieldType.ReadableTypeName(), fieldType.ReadableTypeName(ImGui.IsKeyDown(ImGuiKey.LeftShift)), textColor: ColorType);
                ImGui.SameLine();
                ImGui.TextColored(ColorName, fieldInfo.Name);
                ImGui.SameLine();
                DrawStdVector(fieldAddress, underlyingType, new NodeOptions() { AddressPath = indexedAddressPath });
                continue;
            }

            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(StdDeque<>))
            {
                var underlyingType = fieldType.GenericTypeArguments[0];
                var underlyingTypeSize = underlyingType.SizeOf();
                if (underlyingTypeSize == 0)
                {
                    ImGui.TextColored(Colors.Red, $"Can't get size of {underlyingType.Name}");
                    continue;
                }

                DrawCopyableText(fieldType.ReadableTypeName(), fieldType.ReadableTypeName(ImGui.IsKeyDown(ImGuiKey.LeftShift)), textColor: ColorType);
                ImGui.SameLine();
                ImGui.TextColored(ColorName, fieldInfo.Name);
                ImGui.SameLine();
                DrawStdDeque(fieldAddress, underlyingType, new NodeOptions() { AddressPath = indexedAddressPath });
                continue;
            }


            if (type == typeof(AtkUnitBase) && fieldType == typeof(AtkValue*) && fieldInfo.Name == "AtkValues")
            {
                DrawCopyableText(fieldType.ReadableTypeName(), fieldType.ReadableTypeName(ImGui.IsKeyDown(ImGuiKey.LeftShift)), textColor: ColorType);
                ImGui.SameLine();
                ImGui.TextColored(ColorName, fieldInfo.Name);
                ImGui.SameLine();
                DrawAtkValues((AtkValue*)fieldAddress, ((AtkUnitBase*)address)->AtkValuesCount, new NodeOptions() { AddressPath = nodeOptions.AddressPath.Value.With(fieldAddress) });
                continue;
            }

            // TODO: vector preview
            // TODO: enum values table

            DrawCopyableText(fieldType.ReadableTypeName(), fieldType.ReadableTypeName(ImGui.IsKeyDown(ImGuiKey.LeftShift)), textColor: ColorType);
            ImGui.SameLine();
            ImGui.TextColored(ColorName, fieldInfo.Name);
            ImGui.SameLine();
            DrawPointerType(fieldAddress, fieldType, new NodeOptions() { AddressPath = indexedAddressPath });
        }
    }

    private static void DrawEnum(nint address, Type type, NodeOptions nodeOptions)
    {
        nodeOptions.EnsureAddressInPath(address);

        var underlyingType = type.GetEnumUnderlyingType();
        var value = DrawNumeric(address, underlyingType, new NodeOptions());
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

    public static void DrawCopyableText(string text, string? textCopy = null, string? tooltipText = null, bool asSelectable = false, Vector4? textColor = null)
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

    public static void DrawAddress(void* obj)
        => DrawAddress((nint)obj);

    public static void DrawAddress(nint address)
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

    public static void DrawHexView(nint address, int length)
    {
        using var id = ImRaii.PushId($"HexView_{address}");

        if (ImGui.Button($"Copy Hex"))
        {
            ImGui.SetClipboardText(BitConverter.ToString(MemoryHelper.ReadRaw(address, length)).Replace("-", ""));
        }
        ImGui.SameLine();
        if (ImGui.Button($"Copy Text"))
        {
            ImGui.SetClipboardText(MemoryHelper.ReadStringNullTerminated(address));
        }

        var numColumns = 16;

        using var table = ImRaii.Table($"##HaselDebugDebugWindow_HexView{address}", 1 + numColumns + 1, ImGuiTableFlags.NoKeepColumnsVisible | ImGuiTableFlags.NoSavedSettings);
        if (!table)
            return;

        using var font = ImRaii.PushFont(UiBuilder.MonoFont);

        ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize(address.ToString("X")).X);

        for (var column = 0; column < numColumns; column++)
            ImGui.TableSetupColumn(column.ToString("X"), ImGuiTableColumnFlags.WidthFixed, 14 + (column % 8 == 7 ? 3 : 0));

        ImGui.TableSetupColumn("Data", ImGuiTableColumnFlags.WidthFixed);

        ImGui.TableHeadersRow();

        var pos = 0;
        for (var line = 0; line < Math.Ceiling(length / (float)numColumns); line++)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            using (ImRaii.PushColor(ImGuiCol.Text, (uint)Colors.Grey3))
                DrawCopyableText($"{address + line * numColumns:X}", asSelectable: true);

            var colpos = pos;
            for (var column = 0; column < numColumns; column++)
            {
                ImGui.TableNextColumn();
                if (colpos++ < length)
                {
                    DrawCopyableText(
                        $"{*(byte*)(address + line * numColumns + column):X2}",
                        $"{address + line * numColumns + column:X}",
                        asSelectable: true);
                }
            }

            colpos = pos;
            var sb = new StringBuilder();
            for (var column = 0; column < numColumns; column++)
            {
                if (colpos++ < length)
                {
                    var c = (char)*(byte*)(address + line * numColumns + column);
                    sb.Append(char.IsAsciiLetterOrDigit(c) || char.IsPunctuation(c) ? c : ".");
                }
            }

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(sb.ToString());

            pos += numColumns;
            if (pos > length)
                break;
        }
    }

    public static void DrawStdVector(nint address, Type type, NodeOptions nodeOptions)
    {
        nodeOptions.EnsureAddressInPath(address);

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

        using var titleColor = ImRaii.PushColor(ImGuiCol.Text, 0xFF00FFFF);
        using var node = ImRaii.TreeNode($"{elementCount} Value{(elementCount != 1 ? "s" : "")}##Node{nodeOptions.AddressPath}", ImGuiTreeNodeFlags.SpanAvailWidth);
        if (!node)
            return;
        titleColor?.Dispose();

        using var table = ImRaii.Table($"StdVectorTable{nodeOptions.AddressPath}", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg/* | ImGuiTableFlags.ScrollY*/);
        if (!table)
            return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Value");
        ImGui.TableSetupScrollFreeze(2, 1);
        ImGui.TableHeadersRow();

        using var indent = ImRaii.PushIndent(1, nodeOptions.Indent);
        for (var i = 0u; i < elementCount; i++)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Index
            ImGui.TextUnformatted(i.ToString());

            ImGui.TableNextColumn(); // Value
            DrawPointerType((nint)(firstElement + i * size), type, new NodeOptions() { AddressPath = nodeOptions.AddressPath });
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StdMapNode<TKey, TValue>
        where TKey : unmanaged
        where TValue : unmanaged
    {
        public nint _Left; // StdMapNode<TKey, TValue>*
        public nint _Parent; // StdMapNode<TKey, TValue>*
        public nint _Right; // StdMapNode<TKey, TValue>*
        public byte _Color; // RedBlackTreeNodeColor
        public byte _Isnil; // bool
        public byte _18;
        public byte _19;
        public StdPair<TKey, TValue> _Myval;
    }

    public static void DrawStdMap(nint address, Type type, NodeOptions nodeOptions)
    {
        nodeOptions.EnsureAddressInPath(address);
        var elementCount = *(ulong*)(address + 0x8);

        using var titleColor = ImRaii.PushColor(ImGuiCol.Text, 0xFF00FFFF);
        using var node = ImRaii.TreeNode($"{elementCount} Value{(elementCount != 1 ? "s" : "")}##Node{nodeOptions.AddressPath}", ImGuiTreeNodeFlags.SpanAvailWidth);
        if (!node)
            return;
        titleColor?.Dispose();

        var size = type.SizeOf();
        if (size == 0)
        {
            ImGui.TextUnformatted($"Can't get size of {type.Name}");
            return;
        }

        if (type.GenericTypeArguments.Length != 2)
        {
            ImGui.TextUnformatted($"Invalid GenericTypeArguments.Length of {type.GenericTypeArguments.Length}");
            return;
        }

        var keyType = type.GenericTypeArguments[0];
        var valueType = type.GenericTypeArguments[1];
        var mapType = typeof(StdMapNode<,>).MakeGenericType(keyType, valueType);
        var myvalOffset = Marshal.OffsetOf(mapType, "_Myval");

        var _head = *(nint*)address;
        var _current = _head;

        static nint GetLeft(nint node) => *(nint*)node;
        static nint GetParent(nint node) => *(nint*)(node + 0x08);
        static nint GetRight(nint node) => *(nint*)(node + 0x10);
        static bool IsNil(nint node) => *(byte*)(node + 0x19) == 1;

        nint Next(nint node)
        {
            if (IsNil(node))
                throw new Exception("Tried to increment a head node.");

            if (IsNil(GetRight(node))) // if (Right->IsNil)
            {
                var ptr = node;
                while (!IsNil(node = GetParent(ptr)) && ptr == GetRight(node)) // while (!(node = ptr->Parent)->IsNil && ptr == node->Right)
                    ptr = node;
                return node;
            }

            var ret = GetRight(node); // var ret = Right;
            while (!IsNil(GetLeft(ret))) // while (!ret->Left->IsNil)
                ret = GetLeft(ret); // ret = ret->Left;
            return ret;
        }

        bool MoveNext()
        {
            if (_current == 0 || _current == GetRight(_head))
                return false;
            _current = _current == _head ? GetLeft(_head) : Next(_current);
            return _current != 0;
        }

        using var indent = ImRaii.PushIndent(1, nodeOptions.Indent);
        using var table = ImRaii.Table($"StdMapTable{nodeOptions.AddressPath}", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg);
        if (!table)
            return;

        ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableSetupColumn("Key", ImGuiTableColumnFlags.WidthFixed, 150);
        ImGui.TableSetupColumn("Value");
        ImGui.TableSetupScrollFreeze(3, 1);
        ImGui.TableHeadersRow();

        while (MoveNext())
        {
            var keyAddress = _current + myvalOffset;
            var valueAddress = keyAddress + keyType.SizeOf();

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Address
            DrawAddress(_current);

            ImGui.TableNextColumn(); // Key
            DrawPointerType(keyAddress, keyType, new NodeOptions() { AddressPath = nodeOptions.AddressPath!.Value.With(keyAddress) });

            ImGui.TableNextColumn(); // Value
            DrawPointerType(valueAddress, valueType, new NodeOptions() { AddressPath = nodeOptions.AddressPath.Value.With(valueAddress) });
        }
    }

    public static void DrawStdDeque(nint address, Type type, NodeOptions nodeOptions)
    {
        nodeOptions.EnsureAddressInPath(address);

        var mySize = *(ulong*)(address + 0x20);
        if (mySize == 0)
        {
            ImGui.TextUnformatted("No values");
            return;
        }

        using var titleColor = ImRaii.PushColor(ImGuiCol.Text, 0xFF00FFFF);
        using var node = ImRaii.TreeNode($"{mySize} Value{(mySize != 1 ? "s" : "")}##Node{nodeOptions.AddressPath}", ImGuiTreeNodeFlags.SpanAvailWidth);
        if (!node)
            return;
        titleColor?.Dispose();

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
        using var table = ImRaii.Table($"StdDequeTable{nodeOptions.AddressPath}", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg);
        if (!table)
            return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Value");
        ImGui.TableSetupScrollFreeze(2, 1);
        ImGui.TableHeadersRow();

        for (var i = 0ul; i < mySize; i++)
        {
            var actualIndex = myOff + i;
            var block = (actualIndex / (ulong)blockSize) & (mapSize - 1);
            var offset = actualIndex % (ulong)blockSize;
            var valueAddress = map[block] + (nint)offset * typeSize; // TODO: check this again. this was originally map[block][offset], so doing pointer arithmetic

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Index
            ImGui.TextUnformatted(i.ToString());

            ImGui.TableNextColumn(); // Value
            DrawPointerType(valueAddress, type, new NodeOptions() { AddressPath = nodeOptions.AddressPath!.Value.With(valueAddress) });
        }
    }

    public static void DrawFixedSizeArray(nint address, Type type, bool isString, NodeOptions nodeOptions)
    {
        nodeOptions.EnsureAddressInPath(address);

        if (type.GetCustomAttribute<InlineArrayAttribute>() is not InlineArrayAttribute inlineArrayAttribute)
            return;

        var length = inlineArrayAttribute.Length;
        if (length == 0)
        {
            ImGui.Dummy(Vector2.Zero);
            return;
        }

        var fieldType = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)[0].FieldType;
        if (isString)
        {
            if (fieldType == typeof(char))
                ImGui.TextUnformatted(new string((char*)address));
            else
                ImGuiHelpers.SeStringWrapped(MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)address));

            return;
        }

        var typeSize = type.SizeOf();
        if (typeSize == 0)
        {
            ImGui.TextColored(Colors.Red, $"Can't get size of {type.Name}");
            return;
        }

        using var titleColor = ImRaii.PushColor(ImGuiCol.Text, 0xFF00FFFF);
        using var node = ImRaii.TreeNode($"{length} Value{(length != 1 ? "s" : "")}##Node{nodeOptions.AddressPath}", ImGuiTreeNodeFlags.SpanAvailWidth);
        if (!node) return;
        titleColor?.Dispose();

        using var table = ImRaii.Table($"FixedSizeArrayTable{nodeOptions.AddressPath}", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg);
        if (!table)
            return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Value");
        ImGui.TableSetupScrollFreeze(2, 1);
        ImGui.TableHeadersRow();

        var fieldSize = typeSize / length;

        using var indent = ImRaii.PushIndent(1, nodeOptions.Indent);
        for (var i = 0u; i < length; i++)
        {
            var entryAddress = (nint)(address + i * fieldSize);
            var entryAddressPath = nodeOptions.AddressPath!.Value.With(entryAddress);

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Index
            ImGui.TextUnformatted(i.ToString());

            ImGui.TableNextColumn(); // Value
            DrawPointerType(entryAddress, fieldType, new NodeOptions() { AddressPath = entryAddressPath });
        }
    }

    public static void DrawAtkValues(AtkValue* values, ushort valueCount, NodeOptions nodeOptions)
    {
        if (valueCount == 0)
        {
            ImGui.TextUnformatted("No values");
            return;
        }

        nodeOptions.EnsureAddressInPath((nint)values);

        using var titleColor = ImRaii.PushColor(ImGuiCol.Text, 0xFF00FFFF);
        using var node = ImRaii.TreeNode($"{valueCount} value{(valueCount != 1 ? "s" : "")}##Node{nodeOptions.AddressPath}", ImGuiTreeNodeFlags.SpanAvailWidth);
        if (!node)
            return;
        titleColor?.Dispose();

        using var table = ImRaii.Table($"AtkValuesTable{nodeOptions.AddressPath}", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY);
        if (!table)
            return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Value");
        ImGui.TableSetupScrollFreeze(3, 1);
        ImGui.TableHeadersRow();

        for (var i = 0; i < valueCount; i++)
        {
            var value = values[i];
            using var disabled = ImRaii.Disabled(value.Type is ValueType.Undefined or ValueType.Null);
            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // Index
            ImGui.TextUnformatted(i.ToString());

            ImGui.TableNextColumn(); // Type
            ImGui.TextUnformatted(value.Type.ToString());

            ImGui.TableNextColumn(); // Value
            if (value.Type is ValueType.String or ValueType.String8 or ValueType.ManagedString)
                DrawSeString(value.String, new NodeOptions { AddressPath = nodeOptions.AddressPath!.Value.With(i), Indent = false });
            else
                ImGui.TextUnformatted(value.GetValueAsString());
        }
    }

    public static void DrawAtkValue(nint address, NodeOptions nodeOptions)
    {
        nodeOptions.EnsureAddressInPath(address);

        var value = (AtkValue*)address;
        switch (value->Type)
        {
            case ValueType.Int:
                DrawCopyableText($"{value->Int}");
                break;
            case ValueType.Bool:
                DrawCopyableText($"{value->Byte == 0x01}");
                break;
            case ValueType.UInt:
                DrawCopyableText($"{value->UInt}");
                break;
            case ValueType.Float:
                DrawCopyableText($"{value->Float}");
                break;
            case ValueType.String:
            case ValueType.String8:
            case ValueType.ManagedString:
                DrawCopyableText($"{MemoryHelper.ReadStringNullTerminated((nint)value->String)}");
                break;
            case ValueType.Vector:
            case ValueType.ManagedVector:
                DrawStdVector((nint)value->Vector, typeof(AtkValue), nodeOptions);
                break;
            case ValueType.Texture:
                DrawTexture((nint)value->Texture, new NodeOptions());
                break;
        }
    }

    public static void DrawAtkTexture(nint address, NodeOptions nodeOptions)
    {
        nodeOptions.EnsureAddressInPath(address);

        var tex = (AtkTexture*)address;
        if (!tex->IsTextureReady())
        {
            ImGui.TextUnformatted("Texture not ready");
            return;
        }

        var title = "AtkTexture";
        if (tex->TextureType == TextureType.Resource)
        {
            title = tex->Resource->TexFileResourceHandle->ResourceHandle.FileName.ToString();
        }

        using var titleColor = ImRaii.PushColor(ImGuiCol.Text, 0xFF00FFFF);
        using var node = ImRaii.TreeNode($"{title}##Node{nodeOptions.AddressPath}", ImGuiTreeNodeFlags.SpanAvailWidth);
        if (!node)
            return;
        titleColor?.Dispose();

        var kernelTexture = tex->GetKernelTexture();
        if (kernelTexture == null)
        {
            ImGui.TextUnformatted("No KernelTexture");
            return;
        }

        DrawTexture((nint)kernelTexture, new NodeOptions());
    }

    public static void DrawTexture(nint address, NodeOptions nodeOptions)
    {
        var tex = (KernelTexture*)address;
        ImGui.Image((nint)tex->D3D11ShaderResourceView, new Vector2(tex->Width, tex->Height));
        DrawCopyableText($"{tex->Width}x{tex->Height}");
        ImGui.SameLine();
        DrawCopyableText($"{tex->TextureFormat}");
        // TODO: context menu to copy/save image
    }

    public static void DrawUtf8String(nint address, NodeOptions nodeOptions)
    {
        if (address == 0)
        {
            ImGui.Dummy(Vector2.Zero);
            return;
        }

        nodeOptions.EnsureAddressInPath(address);

        var str = (Utf8String*)address;
        if (str->StringPtr == null)
        {
            ImGui.Dummy(Vector2.Zero);
            return;
        }

        DrawSeString(str->StringPtr, nodeOptions);
    }

    public static void DrawSeString(byte* ptr, NodeOptions nodeOptions)
    {
        if (ptr == null)
        {
            ImGui.TextUnformatted("null");
            return;
        }

        DrawSeString(new ReadOnlySeStringSpan(ptr), nodeOptions);
    }

    public static void DrawSeString(ReadOnlySeStringSpan rosss, NodeOptions nodeOptions)
    {
        if (rosss.PayloadCount == 0)
        {
            ImGui.Dummy(Vector2.Zero);
            return;
        }

        nodeOptions.EnsureAddressInPath(rosss.GetHashCode());

        using var nodeTitleColor = ImRaii.PushColor(ImGuiCol.Text, 0xFF00FFFF, nodeOptions.RenderSeString);
        using var stringNode = ImRaii.TreeNode($"{(!nodeOptions.RenderSeString ? rosss.ToString() : string.Empty)}##SeStringPayloads{nodeOptions.AddressPath}", ImGuiTreeNodeFlags.SpanAvailWidth);
        nodeTitleColor?.Dispose();

        using (var contextMenu = ImRaii.ContextPopupItem($"SeStringPayloadsContextMenu{nodeOptions.AddressPath}"))
        {
            if (contextMenu)
            {
                if (ImGui.MenuItem("Copy text"))
                {
                    ImGui.SetClipboardText(rosss.ToString());
                }
            }
        }

        if (nodeOptions.RenderSeString)
        {
            ImGui.SameLine(0, ImGui.GetStyle().FramePadding.X * 2f);
            ImGuiHelpers.SeStringWrapped(rosss, new()
            {
                GetEntity = (scoped in SeStringDrawState state, int byteOffset) =>
                {
                    var span = state.Span[byteOffset..];
                    if (span.Length != 0 && span[0] == '\n')
                        return new SeStringReplacementEntity(1, new Vector2(3, state.FontSize), (scoped in SeStringDrawState state, int byteOffset, Vector2 offset) => { });

                    return default;
                },
                ForceEdgeColor = true,
                WrapWidth = 9999
            });
        }

        if (!stringNode) return;

        var i = -1;
        foreach (var payload in rosss)
        {
            i++;

            using var titleColor = ImRaii.PushColor(ImGuiCol.Text, 0xFF00FFFF);
            var preview = payload.Type.ToString();
            if (payload.Type == ReadOnlySePayloadType.Macro)
                preview += $": {payload.MacroCode}";
            using var node = ImRaii.TreeNode($"[{i}] {preview}##Payload{i}_{payload.GetHashCode()}", ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.DefaultOpen);
            if (!node) continue;
            titleColor?.Dispose();

            using var table = ImRaii.Table($"##Payload{i}_{payload.GetHashCode()}Table", 2);
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 100);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted("String");
            ImGui.TableNextColumn();
            var text = payload.ToString();
            DrawCopyableText($"\"{text}\"", text);

            if (payload.Type != ReadOnlySePayloadType.Macro)
                continue;

            if (payload.ExpressionCount > 0)
            {
                var j = 0;
                foreach (var expr in payload)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted($"Expr {j}");
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(expr.ToString());
                    j++;
                }
            }
            /*
            switch (payload.MacroCode)
            {
                /// <summary>Sets the reset time to the contextual time storage.</summary>
                /// <remarks>Parameters: weekday, hour, terminator.</remarks>
                case MacroCode.SetResetTime: // "n N x"
                    break;

                /// <summary>Sets the specified time to the contextual time storage.</summary>
                /// <remarks>Parameters: unix timestamp in seconds, terminator.</remarks>
                case MacroCode.SetTime: // "n x"
                    break;

                /// <summary>Tests an expression and uses a corresponding subexpression.</summary>
                /// <remarks>Parameters: condition, expression to use if condition is true, expression to use if condition is false.</remarks>
                case MacroCode.If: // ". . * x"
                    break;

                /// <summary>Tests an expression and uses a corresponding subexpression.</summary>
                /// <remarks>Parameters: condition, expression to use if condition is 1, expression to use if condition is 2, and so on.</remarks>
                case MacroCode.Switch: // ". . ."
                    break;

                /// <summary>Adds a characters name.</summary>
                /// <remarks>Parameters: ObjectId, terminator.</remarks>
                case MacroCode.PcName: // "n x"
                    break;

                /// <summary>Tests a characters gender.</summary>
                /// <remarks>Parameters: ObjectId, expression to use if the character is male, expression to use if the character is female, terminator.</remarks>
                case MacroCode.IfPcGender: // "n . . x"
                    break;

                /// <summary>Tests a characters name.</summary>
                /// <remarks>Parameters: ObjectId, the name to test against, expression to use if the name matches, expression to use if the name doesn't match, terminator.</remarks>
                case MacroCode.IfPcName: // "n . . . x"
                    break;

                /// <summary>Determines the type of josa required from the last character of the first expression.</summary>
                /// <remarks>Parameters: test string, eun/i/eul suffix, neun/ga/reul suffix, terminator.</remarks>
                case MacroCode.Josa: // "s s s x"
                    break;

                /// <summary>Determines the type of josa, ro in particular, required from the last character of the first expression.</summary>
                /// <remarks>Parameters: test string, ro suffix, euro suffix, terminator.</remarks>
                case MacroCode.Josaro: // "s s s x"
                    break;

                /// <summary>Tests if the character is the local player.</summary>
                /// <remarks>Parameters: ObjectId, expression to use if the character is the local player, expression to use if the character is not the local player, terminator.</remarks>
                case MacroCode.IfSelf: // "n . . x"
                    break;

                /// <summary>Adds a line break.</summary>
                case MacroCode.NewLine:
                    break;

                /// <summary>Waits for a specified duration.</summary>
                /// <remarks>Parameters: delay in seconds, terminator.</remarks>
                case MacroCode.Wait: // "n x"
                    break;

                /// <summary>Adds an icon from common/font/gfdata.gfd.</summary>
                /// <remarks>Parameters: icon ID, terminator.</remarks>
                case MacroCode.Icon: // "n x"
                    break;

                /// <summary>Pushes the text foreground color.</summary>
                /// <remarks>Parameters: something that resolves to B8G8R8A8 or stackcolor(, ?), terminator</remarks>
                case MacroCode.Color: // "n N x"
                    break;

                /// <summary>Pushes the text border color.</summary>
                /// <remarks>Parameters: something that resolves to B8G8R8A8 or stackcolor(, ?), terminator</remarks>
                case MacroCode.EdgeColor: // "n N x"
                    break;

                /// <summary>Pushes the text shadow color.</summary>
                /// <remarks>Parameters: something that resolves to B8G8R8A8 or stackcolor(, ?), terminator</remarks>
                case MacroCode.ShadowColor: // "n N x"
                    break;

                /// <summary>Adds a soft hyphen.</summary>
                case MacroCode.SoftHyphen:
                    break;

                case MacroCode.Key:
                    break;

                case MacroCode.Scale: // "n"
                    break;

                /// <summary>Sets whether to use bold text effect.</summary>
                /// <remarks>Parameters: bool enabled.</remarks>
                case MacroCode.Bold: // "n"
                    break;

                /// <summary>Sets whether to use italic text effect.</summary>
                /// <remarks>Parameters: bool enabled.</remarks>
                case MacroCode.Italic: // "n"
                    break;

                case MacroCode.Edge: // "n n"
                    break;

                case MacroCode.Shadow: // "n n"
                    break;

                /// <summary>Adds a non-breaking space.</summary>
                case MacroCode.NonBreakingSpace:
                    break;

                case MacroCode.Icon2:
                    break;

                /// <summary>Adds a hyphen.</summary>
                case MacroCode.Hyphen:
                    break;

                /// <summary>Adds a decimal representation of an integer expression.</summary>
                /// <remarks>Parameters: integer expression, terminator.</remarks>
                case MacroCode.Num: // "n x"
                    break;

                /// <summary>Adds a hexadecimal representation of an integer expression.</summary>
                /// <remarks>Parameters: integer expression, terminator.</remarks>
                case MacroCode.Hex: // "n x"
                    break;

                /// <summary>Adds a decimal representation of an integer expression, separating by thousands.</summary>
                /// <remarks>Parameters: integer expression, separator (usually a comma or a dot), terminator.</remarks>
                case MacroCode.Kilo: // ". s x"
                    break;

                /// <summary>Adds a human-readable byte string (possible suffixes: omitted, K, M, G, T).</summary>
                /// <remarks>Parameters: integer expression, terminator.</remarks>
                case MacroCode.Byte: // "n x"
                    break;

                /// <summary>Adds a zero-padded-to-two-digits decimal representation of an integer expression.</summary>
                /// <remarks>Parameters: integer expression, terminator.</remarks>
                case MacroCode.Sec: // "n x"
                    break;

                case MacroCode.Time: // "n x"
                    break;

                /// <summary>Adds a floating point number as text.</summary>
                /// <remarks>Parameters: integer expression, radix, separator, terminator.</remarks>
                case MacroCode.Float: // "n n s x"
                    break;

                /// <summary>Begins or ends a region of link.</summary>
                /// <remarks>Parameters: <see cref="LinkMacroPayloadType"/>, numeric argument 1, numeric argument 2, numeric argument 3, display string.<br />
                /// See comments in <see cref="LinkMacroPayloadType"/> for the argument usages.</remarks>
                case MacroCode.Link: // "n n n n s"
                    break;

                /// <summary>Adds a column from a sheet.</summary>
                /// <remarks>Parameters: sheet name, row ID, column index, expression passed as first local parameter to the columns text.</remarks>
                case MacroCode.Sheet: // "s . . ."
                    break;

                /// <summary>Adds a string expression as-is.</summary>
                /// <remarks>Parameters: string expression, terminator.</remarks>
                case MacroCode.String: // "s x"
                    break;

                /// <summary>Adds a string, fully upper cased.</summary>
                /// <remarks>Parameters: string expression, terminator.</remarks>
                case MacroCode.Caps: // "s x"
                    break;

                /// <summary>Adds a string, first character upper cased.</summary>
                /// <remarks>Parameters: string expression, terminator.</remarks>
                case MacroCode.Head: // "s x"
                    break;

                case MacroCode.Split: // "s s n x"
                    break;

                /// <summary>Adds a string, every words first character upper cased.</summary>
                /// <remarks>Parameters: string expression, terminator.</remarks>
                case MacroCode.HeadAll: // "s x"
                    break;

                case MacroCode.Fixed: // "n n . . ."
                    break;

                /// <summary>Adds a string, fully lower cased.</summary>
                /// <remarks>Parameters: string expression, terminator.</remarks>
                case MacroCode.Lower: // "s x"
                    break;

                /// <summary>Adds sheet text with proper declension in Japanese.</summary>
                /// <remarks>Parameters: sheet name, person, row id, amount, unused, unknown offset.</remarks>
                case MacroCode.JaNoun: // "s . ."
                    break;

                /// <summary>Adds sheet text with proper declension in English.</summary>
                /// <remarks>Parameters: sheet name, person, row id, amount, unused, unused.</remarks>
                case MacroCode.EnNoun: // "s . ."
                    break;

                /// <summary>Adds sheet text with proper declension in German.</summary>
                /// <remarks>Parameters: sheet name, person, row id, amount, case, unused.</remarks>
                case MacroCode.DeNoun: // "s . ."
                    break;

                /// <summary>Adds sheet text with proper declension in French.</summary>
                /// <remarks>Parameters: sheet name, person, row id, amount, unused, unknown offset.</remarks>
                case MacroCode.FrNoun: // "s . ."
                    break;

                /// <summary>Adds sheet text with proper declension in Chinese.</summary>
                /// <remarks>Parameters: sheet name, unused, row id, amount, unused, unknown offset.</remarks>
                case MacroCode.ChNoun: // "s . ."
                    break;

                /// <summary>Adds a string, first character lower cased.</summary>
                /// <remarks>Parameters: string expression, terminator.</remarks>
                case MacroCode.LowerHead: // "s x"
                    break;

                /// <summary>Pushes the text foreground color, referring to a color defined in UIColor sheet.</summary>
                /// <remarks>Parameters: row ID in UIColor sheet or 0 to pop(or reset?) the pushed color, terminator.</remarks>
                case MacroCode.ColorType: // "n x"
                    break;

                /// <summary>Pushes the text border color, referring to a color defined in UIColor sheet.</summary>
                /// <remarks>Parameters: row ID in UIColor sheet or 0 to pop(or reset?) the pushed color, terminator.</remarks>
                case MacroCode.EdgeColorType: // "n x"
                    break;

                /// <summary>Adds a zero-padded number as text.</summary>
                /// <remarks>Parameters: integer expression, target length, terminator.</remarks>
                case MacroCode.Digit: // "n n x"
                    break;

                /// <summary>Adds an ordinal number as text (English only).</summary>
                case MacroCode.Ordinal: // "n x"
                    break;

                /// <summary>Adds an invisible sound payload.</summary>
                /// <remarks>Parameters: bool whether this sound is a Jingle (see sheet), the id.</remarks>
                case MacroCode.Sound: // "n n"
                    break;

                /// <summary>Adds a formatted map name and corresponding coordinates, in the format of <c>Map Name\n( X  , Y )</c>.</summary>
                /// <remarks>Parameters: row ID in Level sheet, terminator.</remarks>
                case MacroCode.LevelPos: // "n x"
                    break;
            }
            */
            /*
            switch (payload.MacroCode)
            {
                case MacroCode.String:
                    //DrawExpressionRow("Parameter", parameterPayload.Parameter, localParameters);
                    break;

                case NounPayload nounPayload:
                    DrawExpressionRow("SheetName", nounPayload.SheetName, localParameters);
                    DrawExpressionRow("Person", nounPayload.Person, localParameters);
                    DrawExpressionRow("RowId", nounPayload.RowId, localParameters);
                    DrawExpressionRow("Amount", nounPayload.Amount, localParameters);
                    DrawExpressionRow("Case", nounPayload.Case, localParameters);
                    DrawExpressionRow("UnkInt5", nounPayload.UnkInt5, localParameters);
                    break;

                // ---

                case BytePayload bytePayload:
                    DrawExpressionRow("Value", bytePayload.Value, localParameters);
                    break;

                case ColorPayload colorPayload:
                    DrawExpressionRow("Color", colorPayload.Color, localParameters, true);
                    break;

                case ColorTypePayload colorTypePayload:
                    {
                        DrawExpressionRow("ColorType", colorTypePayload.ColorType, localParameters);
                        var color = colorTypePayload.ColorType?.ResolveNumber(localParameters);
                        if (color is not (null or 0))
                        {
                            //ImGui.SameLine();
                            //ImGui.ColorButton(color.ToString(), HaselColor.FromUiForeground((uint)color));
                        }
                    }
                    break;

                case DigitPayload digitPayload:
                    DrawExpressionRow("Value", digitPayload.Value);
                    DrawExpressionRow("TargetLength", digitPayload.TargetLength, localParameters);
                    break;

                case EdgeColorPayload edgeColorPayload:
                    DrawExpressionRow("Color", edgeColorPayload.Color, localParameters, true);
                    break;

                case EdgeColorTypePayload edgeColorTypePayload:
                    {
                        DrawExpressionRow("ColorType", edgeColorTypePayload.ColorType, localParameters);
                        var color = edgeColorTypePayload.ColorType?.ResolveNumber(localParameters);
                        if (color is not (null or 0))
                        {
                            //ImGui.SameLine();
                            //ImGui.ColorButton(color.ToString(), HaselColor.FromUiGlow((uint)color));
                        }
                    }
                    break;

                case FloatPayload floatPayload:
                    DrawExpressionRow("Value", floatPayload.Value, localParameters);
                    DrawExpressionRow("Radix", floatPayload.Radix, localParameters);
                    DrawExpressionRow("Separator", floatPayload.Separator, localParameters);
                    break;

                case HeadAllPayload headAllPayload:
                    DrawExpressionRow("String", headAllPayload.String, localParameters);
                    break;

                case HeadPayload headPayload:
                    DrawExpressionRow("String", headPayload.String, localParameters);
                    break;

                case HexPayload hexPayload:
                    DrawExpressionRow("Value", hexPayload.Value, localParameters);
                    break;

                case IconPayload iconPayload:
                    DrawExpressionRow("IconId", iconPayload.IconId, localParameters);
                    /*
                    fontIconDictionary ??= Service.DataManager.GetFile<GraphicFontDictionary>("common/font/gfdata.gfd");
                    if (fontIconDictionary != null)
                    {
                        var iconId = iconPayload.IconId?.ResolveNumber() ?? 0;
                        if (fontIconDictionary.Icons.TryGetValue(iconId, out var icon))
                        {
                            ImGui.SameLine();
                            Service.TextureManager.Get("common/font/fontIcon_Xinput.tex", 1, icon.Position, icon.Position + icon.Size).Draw();
                        }
                    }
                    * /
                    break;

                case Icon2Payload icon2Payload:
                    DrawExpressionRow("IconId", icon2Payload.IconId, localParameters);
                    /*
                    fontIconDictionary ??= Service.DataManager.GetFile<GraphicFontDictionary>("common/font/gfdata.gfd");
                    if (fontIconDictionary != null)
                    {
                        var iconId = icon2Payload.IconId?.ResolveNumber() ?? 0;
                        if (fontIconDictionary.Icons.TryGetValue(iconId, out var icon))
                        {
                            ImGui.SameLine();
                            Service.TextureManager.Get("common/font/fontIcon_Xinput.tex", 1, icon.Position, icon.Position + icon.Size).Draw();
                        }
                    }
                    * /
                    DrawExpressionRow("UnkNumber2", icon2Payload.UnkNumber2, localParameters);
                    break;

                case IfPayload ifPayload:
                    DrawExpressionRow("Condition", ifPayload.Condition, localParameters);
                    DrawExpressionRow("StatementTrue", ifPayload.StatementTrue, localParameters);
                    DrawExpressionRow("StatementFalse", ifPayload.StatementFalse, localParameters);
                    break;

                case IfPcGenderPayload ifPcGenderPayload:
                    DrawExpressionRow("EntityId", ifPcGenderPayload.EntityId, localParameters, true);
                    DrawExpressionRow("CaseMale", ifPcGenderPayload.CaseMale, localParameters);
                    DrawExpressionRow("CaseFemale", ifPcGenderPayload.CaseFemale, localParameters);
                    break;

                case IfPcNamePayload ifPcNamePayload:
                    DrawExpressionRow("EntityId", ifPcNamePayload.EntityId, localParameters, true);
                    DrawExpressionRow("CaseTrue", ifPcNamePayload.CaseTrue, localParameters);
                    DrawExpressionRow("CaseFalse", ifPcNamePayload.CaseFalse, localParameters);
                    break;

                case IfSelfPayload ifSelfPayload:
                    DrawExpressionRow("EntityId", ifSelfPayload.EntityId, localParameters, true);
                    DrawExpressionRow("CaseTrue", ifSelfPayload.CaseTrue, localParameters);
                    DrawExpressionRow("CaseFalse", ifSelfPayload.CaseFalse, localParameters);
                    break;

                case KiloPayload kiloPayload:
                    DrawExpressionRow("Value", kiloPayload.Value, localParameters);
                    DrawExpressionRow("Separator", kiloPayload.Separator, localParameters);
                    break;

                case LinkPayload linkPayload:

                    if (linkPayload.Type is IntegerExpression integerType)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted("Type");
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"0x{(byte)integerType.Value:X02}");
                        ImGui.SameLine();
                        var linkType = (LinkType)integerType.Value;
                        ImGui.TextUnformatted($"{linkType}");

                        DrawExpressionRow("Arg2", linkPayload.Arg2, localParameters);
                        DrawExpressionRow("Arg3", linkPayload.Arg3, localParameters);
                        DrawExpressionRow("Arg4", linkPayload.Arg4, localParameters);
                        DrawExpressionRow("Arg5", linkPayload.Arg5, localParameters);

                        switch (linkPayload)
                        {
                            case PlayerLinkPayload playerLinkPayload:
                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("Flags");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{playerLinkPayload.Flags}");

                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("WorldId");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{playerLinkPayload.WorldId} ({GetRow<World>(playerLinkPayload.WorldId)?.Name ?? "Unknown"})");

                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("PlayerName");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{playerLinkPayload.PlayerName}");
                                break;

                            case ItemLinkPayload itemLinkPayload:
                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("ItemId");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{itemLinkPayload.ItemId} ({GetRow<Item>(itemLinkPayload.ItemId)?.Name ?? ""})");
                                break;

                            case MapPositionLinkPayload mapPositionLinkPayload:
                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("TerritoryTypeId");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{mapPositionLinkPayload.TerritoryTypeId} ({GetRow<TerritoryType>(mapPositionLinkPayload.TerritoryTypeId)?.PlaceName.Value?.Name ?? ""})");

                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("MapId");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{mapPositionLinkPayload.MapId}");

                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("X");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{mapPositionLinkPayload.X} ({mapPositionLinkPayload.MapPosX:0.0})");

                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("Y");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{mapPositionLinkPayload.Y} ({mapPositionLinkPayload.MapPosY:0.0})");
                                break;

                            case QuestLinkPayload questLinkPayload:
                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("QuestId");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{questLinkPayload.QuestId} ({GetRow<Quest>(questLinkPayload.QuestId)?.Name ?? ""})");
                                break;

                            case AchievementLinkPayload achievementLinkPayload:
                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("AchievementId");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{achievementLinkPayload.AchievementId} ({GetRow<Achievement>(achievementLinkPayload.AchievementId)?.Name ?? ""})");
                                break;

                            case HowToLinkPayload howToLinkPayload:
                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("HowToId");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{howToLinkPayload.HowToId} ({GetRow<HowTo>(howToLinkPayload.HowToId)?.Name ?? ""})");
                                break;

                            case StatusLinkPayload statusLinkPayload:
                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("StatusId");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{statusLinkPayload.StatusId} ({GetRow<Status>(statusLinkPayload.StatusId)?.Name ?? ""})");
                                break;

                            case PartyFinderLinkPayload partyFinderLinkPayload:
                                DrawExpressionRow("ListingId", linkPayload.Arg2, localParameters);

                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("Flags");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{partyFinderLinkPayload.Flags} ({(byte)partyFinderLinkPayload.Flags:X02})");
                                break;

                            case AkatsukiNoteLinkPayload akatsukiNoteLinkPayload:
                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("AkatsukiNoteId");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{akatsukiNoteLinkPayload.AkatsukiNoteId} ({GetRow<AkatsukiNoteString>((uint)GetRow<AkatsukiNote>(akatsukiNoteLinkPayload.AkatsukiNoteId, 0)!.Unknown5)!.Unknown0 ?? ""})");
                                break;

                            case DalamudLinkPayload dalamudLinkPayload:
                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("PluginName");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{dalamudLinkPayload.PluginName}");

                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("CommandId");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{dalamudLinkPayload.CommandId}");
                                break;

                            case PartyFinderNotificationLinkPayload:
                            case LinkTerminatorPayload:
                                break;
                        }
                    }
                    else
                    {
                        DrawExpressionRow("Type", linkPayload.Type, localParameters);
                        DrawExpressionRow("Arg2", linkPayload.Arg2, localParameters);
                        DrawExpressionRow("Arg3", linkPayload.Arg3, localParameters);
                        DrawExpressionRow("Arg4", linkPayload.Arg4, localParameters);
                        DrawExpressionRow("Arg5", linkPayload.Arg5, localParameters);
                    }
                    break;

                case LowerHeadPayload lowerHeadPayload:
                    DrawExpressionRow("String", lowerHeadPayload.String, localParameters);
                    break;

                case LowerPayload lowerPayload:
                    DrawExpressionRow("String", lowerPayload.String, localParameters);
                    break;

                case OrdinalPayload ordinalPayload:
                    DrawExpressionRow("Value", ordinalPayload.Value, localParameters);
                    break;

                case PcNamePayload pcNamePayload:
                    DrawExpressionRow("EntityId", pcNamePayload.EntityId, localParameters, true);
                    break;

                case SoundPayload soundPayload:
                    DrawExpressionRow("IsJingle", soundPayload.IsJingle, localParameters);
                    DrawExpressionRow("SoundId", soundPayload.SoundId, localParameters);

                    var isJingle = soundPayload.IsJingle?.ResolveNumber(localParameters) ?? 0;
                    var soundId = soundPayload.SoundId?.ResolveNumber(localParameters) ?? 0;
                    if (soundId != 0)
                    {
                        // Client::UI::Misc::RaptureTextModule_vf18
                        if (isJingle != 0)
                            soundId += 1000000;

                        ImGui.SameLine();
                        if (ImGuiUtils.IconButton($"PlaySound{soundId}", FontAwesomeIcon.Play, $"Play sound {soundId}"))
                        {
                            UIModule.PlaySound((uint)soundId);
                        }
                    }
                    break;

                case SecPayload secPayload:
                    DrawExpressionRow("Time", secPayload.Value, localParameters);
                    break;

                case SetResetTimePayload setResetTimePayload:
                    DrawExpressionRow("Hour", setResetTimePayload.Hour, localParameters);
                    DrawExpressionRow("WeekDay", setResetTimePayload.WeekDay, localParameters);
                    break;

                case SetTimePayload setTimePayload:
                    DrawExpressionRow("Time", setTimePayload.Time, localParameters);
                    break;

                case SheetPayload sheetPayload:
                    DrawExpressionRow("SheetName", sheetPayload.SheetName, localParameters);
                    DrawExpressionRow("RowId", sheetPayload.RowId, localParameters);
                    DrawExpressionRow("ColumnIndex", sheetPayload.ColumnIndex, localParameters);
                    DrawExpressionRow("ColumnParam", sheetPayload.ColumnParam, localParameters);
                    break;

                case SwitchPayload switchPayload:
                    DrawExpressionRow("Condition", switchPayload.Condition, localParameters);

                    for (var paramIdx = 0; paramIdx < switchPayload.Cases.Count; paramIdx++)
                        DrawExpressionRow($"Cases[{paramIdx}]", switchPayload.Cases[paramIdx], localParameters);
                    break;

                case TimePayload timePayload:
                    DrawExpressionRow("Value", timePayload.Value, localParameters);
                    break;

                case WaitPayload waitPayload:
                    DrawExpressionRow("Seconds", waitPayload.Seconds, localParameters);
                    break;

                case RawPayload:
                case CharacterPayload:
                    // ignored
                    break;

                default:
                    var props = payload.GetType()
                        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .Where(propInfo => propInfo.PropertyType.IsAssignableTo(typeof(Expression)));

                    foreach (var propInfo in props)
                    {
                        DrawExpressionRow(propInfo.Name, (Expression?)propInfo.GetValue(payload), localParameters);
                    }

                    // ImGui.TextUnformatted($"Unhandled Payload: {payload.GetType().Name}");
                    break;
            }

            var encoded = payload.Encode();
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted("Data [");
            ImGui.SameLine(0, 0);
            ImGui.TextUnformatted(ImGui.IsKeyDown(ImGuiKey.LeftShift) ? $"0x{encoded.Length:X02}" : $"{encoded.Length}");
            ImGui.SameLine(0, 0);
            ImGui.TextUnformatted("]");
            ImGui.TableNextColumn();
            DrawCopyableText(BitConverter.ToString(encoded).Replace("-", " "));
            */
        }
    }

    public static object? DrawNumeric(nint address, Type type, NodeOptions nodeOptions)
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
    public static string ToBitsString(ulong value, int bits)
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

    public static string ToBitsString(byte byteIn)
        => ToBitsString(byteIn, 8);

    public static string ToBitsString(ushort byteIn)
        => ToBitsString(byteIn, 16);

    public static string ToBitsString(uint byteIn)
        => ToBitsString(byteIn, 32);
    */

    public static void DrawIcon(ITextureProvider textureProvider, uint iconId, bool isHq = false, bool sameLine = true)
    {
        if (iconId == 0)
        {
            ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight()));
            if (sameLine)
                ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
            return;
        }

        if (!ImGuiUtils.IsInViewport(new Vector2(ImGui.GetTextLineHeight())))
        {
            ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight()));
            if (sameLine)
                ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
            return;
        }

        if (textureProvider.TryGetFromGameIcon(iconId, out var tex) && tex.TryGetWrap(out var texture, out _))
        {
            ImGui.Image(texture.ImGuiHandle, new(ImGui.GetTextLineHeight()));

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
            {
                ImGui.SetClipboardText(iconId.ToString());
            }
        }
        else
        {
            ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight()));
        }

        if (sameLine)
            ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
    }
}
