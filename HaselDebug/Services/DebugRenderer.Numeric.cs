using System.Globalization;
using HaselDebug.Utils;

namespace HaselDebug.Services;

public unsafe partial class DebugRenderer
{
    public object? DrawPointerNumber(nint address, Type type, NodeOptions nodeOptions)
    {
        if (address == 0)
        {
            ImGui.Text("null"u8);
            return 0;
        }

        object? value = null;

        switch (type)
        {
            case Type t when t == typeof(nint):
                value = *(nint*)address;
                break;

            case Type t when t == typeof(Half):
                value = *(Half*)address;
                break;

            case Type t when t == typeof(sbyte):
                value = *(sbyte*)address;
                break;

            case Type t when t == typeof(byte):
                value = *(byte*)address;
                break;

            case Type t when t == typeof(short):
                value = *(short*)address;
                break;

            case Type t when t == typeof(ushort):
                value = *(ushort*)address;
                break;

            case Type t when t == typeof(int):
                value = *(int*)address;
                break;

            case Type t when t == typeof(uint):
                value = *(uint*)address;
                break;

            case Type t when t == typeof(long):
                value = *(long*)address;
                break;

            case Type t when t == typeof(ulong):
                value = *(ulong*)address;
                break;

            case Type t when t == typeof(decimal):
                value = *(decimal*)address;
                break;

            case Type t when t == typeof(double):
                value = *(double*)address;
                break;

            case Type t when t == typeof(float):
                value = *(float*)address;
                break;

            default:
                ImGui.Text("null"u8);
                return value;
        }

        DrawNumber(value, type, nodeOptions);

        return value;
    }

    public void DrawNumber<T>(T obj, NodeOptions nodeOptions) where T : notnull
        => DrawNumber(obj, typeof(T), nodeOptions);

    public void DrawNumber(object value, Type type, NodeOptions nodeOptions)
    {
        if (type == typeof(nint))
        {
            DrawAddress((nint)value);
            return;
        }

        if (type == typeof(Half) || type == typeof(decimal) || type == typeof(double) || type == typeof(float))
        {
            ImGuiUtils.DrawCopyableText(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
            return;
        }

        if (type == typeof(byte) || type == typeof(sbyte) ||
            type == typeof(short) || type == typeof(ushort) ||
            type == typeof(int) || type == typeof(uint) ||
            type == typeof(long) || type == typeof(ulong))
        {
            DrawNumericWithHex(value, type, nodeOptions);
            return;
        }

        ImGui.Text($"Unhandled NumericType {type.FullName}");
    }

    private void DrawNumericWithHex(object value, Type type, NodeOptions nodeOptions)
    {
        if (nodeOptions.IsIconIdField)
        {
            DrawIcon(value, type);
        }

        if (nodeOptions.IsTimestampField)
        {
            ImGuiUtils.DrawCopyableText(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);

            switch (value)
            {
                case int intTime when intTime != 0:
                    ImGui.SameLine();
                    ImGuiUtils.DrawCopyableText(DateTimeOffset.FromUnixTimeSeconds(intTime).ToLocalTime().ToString());
                    break;

                case long longTime when longTime != 0:
                    ImGui.SameLine();
                    ImGuiUtils.DrawCopyableText(DateTimeOffset.FromUnixTimeSeconds(longTime).ToLocalTime().ToString());
                    break;
            }

            return;
        }
        else if (nodeOptions.IsWorldIdField)
        {
            ImGuiUtils.DrawCopyableText(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);

            switch (value)
            {
                case short shortWorldId when shortWorldId != 0 && _excelService.TryGetRow<World>((ushort)shortWorldId, out var worldRow):
                    ImGui.SameLine();
                    ImGuiUtils.DrawCopyableText(worldRow.Name.ToString());
                    break;

                case int intWorldId when intWorldId != 0 && _excelService.TryGetRow<World>((uint)intWorldId, out var worldRow):
                    ImGui.SameLine();
                    ImGuiUtils.DrawCopyableText(worldRow.Name.ToString());
                    break;

                case ushort ushortWorldId when ushortWorldId != 0 && _excelService.TryGetRow<World>(ushortWorldId, out var worldRow):
                    ImGui.SameLine();
                    ImGuiUtils.DrawCopyableText(worldRow.Name.ToString());
                    break;

                case uint uintWorldId when uintWorldId != 0 && _excelService.TryGetRow<World>(uintWorldId, out var worldRow):
                    ImGui.SameLine();
                    ImGuiUtils.DrawCopyableText(worldRow.Name.ToString());
                    break;
            }

            return;
        }
        else if (nodeOptions.HexOnShift)
        {
            if (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift))
            {
                ImGuiUtils.DrawCopyableText(ToHexString(value, type));
            }
            else
            {
                ImGuiUtils.DrawCopyableText(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
            }

            return;
        }

        ImGuiUtils.DrawCopyableText(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
        ImGui.SameLine();
        ImGuiUtils.DrawCopyableText(ToHexString(value, type));
    }

    private static string ToHexString(object value, Type type)
    {
        return type switch
        {
            _ when type == typeof(byte) => $"0x{(byte)value:X}",
            _ when type == typeof(sbyte) => $"0x{(sbyte)value:X}",
            _ when type == typeof(short) => $"0x{(short)value:X}",
            _ when type == typeof(ushort) => $"0x{(ushort)value:X}",
            _ when type == typeof(int) => $"0x{(int)value:X}",
            _ when type == typeof(uint) => $"0x{(uint)value:X}",
            _ when type == typeof(long) => $"0x{(long)value:X}",
            _ when type == typeof(ulong) => $"0x{(ulong)value:X}",
            _ => throw new InvalidOperationException($"Unsupported type {type.FullName}")
        };
    }
}
