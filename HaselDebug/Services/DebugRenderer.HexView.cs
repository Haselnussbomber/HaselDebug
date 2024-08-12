using System.Text;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Memory;
using HaselCommon.Utils;
using HaselDebug.Utils;
using ImGuiNET;

namespace HaselDebug.Services;

public unsafe partial class DebugRenderer
{
    public void DrawHexView(nint address, int length, NodeOptions nodeOptions)
    {
        if (ImGui.Button($"Copy Hex"))
            ImGui.SetClipboardText(BitConverter.ToString(MemoryHelper.ReadRaw(address, length)).Replace("-", ""));
        ImGui.SameLine();
        if (ImGui.Button($"Copy Text"))
            ImGui.SetClipboardText(MemoryHelper.ReadStringNullTerminated(address));

        var numColumns = 16;

        nodeOptions = nodeOptions.WithAddress(address);

        using var indent = ImRaii.PushIndent(1, nodeOptions.Indent);
        using var table = ImRaii.Table(nodeOptions.GetKey("HexView"), 1 + numColumns + 1, ImGuiTableFlags.NoKeepColumnsVisible | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

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
}
