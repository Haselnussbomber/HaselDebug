using System.Reflection;
using HaselDebug.Utils;

namespace HaselDebug.Services;

public partial class DebugRenderer
{
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
            ImGui.Text(" - "u8);
            var bits = Marshal.SizeOf(underlyingType) * 8;
            for (var i = 0u; i < bits; i++)
            {
                var bitValue = 1u << (int)i;
                if ((Convert.ToUInt64(value) & bitValue) != 0)
                {
                    ImGui.SameLine();
                    ImGuiUtils.DrawCopyableText(Enum.GetName(type, bitValue)?.ToString() ?? $"{bitValue}", new() { CopyText = $"{bitValue}" });
                }
            }
        }
        else
        {
            ImGui.SameLine();
            ImGui.Text(Enum.GetName(type, value)?.ToString() ?? "");
        }
    }
}
