using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.STD;
using HaselCommon.Services;
using HaselDebug.Extensions;
using HaselDebug.Utils;
using HaselDebug.Windows;

namespace HaselDebug.Services;

public unsafe partial class DebugRenderer
{
    public void DrawStdVector(nint address, Type valueType, NodeOptions nodeOptions)
    {
        var size = valueType.SizeOf();
        if (size == 0)
        {
            ImGui.TextUnformatted($"Can't get size of {valueType.Name}");
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

        nodeOptions = nodeOptions.WithAddress(address);

        using var node = DrawTreeNode(nodeOptions.WithSeStringTitle($"{elementCount} value{(elementCount != 1 ? "s" : "")}") with
        {
            DrawContextMenu = (nodeOptions, builder) =>
            {
                builder.AddCopyAddress(_textService, address);
                builder.AddSeparator();
                builder.Add(new ImGuiContextMenuEntry()
                {
                    Visible = !_windowManager.Contains(win => win.WindowName == "0x" + address.ToString("X")),
                    Label = _textService.Translate("ContextMenu.TabPopout"),
                    ClickCallback = () => _windowManager.Open(new PointerTypeWindow(_windowManager, _textService, _addonObserver, _serviceProvider, address, typeof(StdVector<>).MakeGenericType(valueType), "0x" + address.ToString("X")))
                });
            }
        });

        if (!node) return;

        nodeOptions = nodeOptions.ConsumeTreeNodeOptions();

        using var indent = ImRaii.PushIndent(1, nodeOptions.Indent);
        using var table = ImRaii.Table(nodeOptions.GetKey("StdVectorTable"), 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Value");
        ImGui.TableSetupScrollFreeze(2, 1);
        ImGui.TableHeadersRow();

        for (var i = 0u; i < elementCount; i++)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Index
            ImGui.TextUnformatted(i.ToString());

            ImGui.TableNextColumn(); // Value
            DrawPointerType((nint)(firstElement + i * size), valueType, new NodeOptions() { AddressPath = nodeOptions.AddressPath, IsIconIdField = nodeOptions.IsIconIdField });
        }
    }
}
