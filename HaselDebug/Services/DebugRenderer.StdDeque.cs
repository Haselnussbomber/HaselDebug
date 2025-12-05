using FFXIVClientStructs.STD;
using HaselDebug.Extensions;
using HaselDebug.Utils;
using HaselDebug.Windows;

namespace HaselDebug.Services;

public unsafe partial class DebugRenderer
{
    public void DrawStdDeque(nint address, Type valueType, NodeOptions nodeOptions)
    {
        if (address == 0)
        {
            ImGui.Text("null"u8);
            return;
        }

        if (!address.IsValid())
        {
            ImGui.Text("invalid"u8);
            return;
        }

        var elementCount = *(ulong*)(address + 0x20); // MySize
        if (elementCount == 0)
        {
            ImGui.Text("No values"u8);
            return;
        }

        nodeOptions = nodeOptions.WithAddress(address);

        using var node = DrawTreeNode(nodeOptions.WithSeStringTitle($"{elementCount} value{(elementCount != 1 ? "s" : "")}") with
        {
            DrawContextMenu = (nodeOptions, builder) =>
            {
                builder.AddCopyAddress(address);
                builder.AddSeparator();
                builder.Add(new ImGuiContextMenuEntry()
                {
                    Visible = !_windowManager.Contains(win => win.WindowName == "0x" + address.ToString("X")),
                    Label = _textService.Translate("ContextMenu.TabPopout"),
                    ClickCallback = () => _windowManager.Open(new PointerTypeWindow(_windowManager, _textService, _addonObserver, _serviceProvider, address, typeof(StdDeque<>).MakeGenericType(valueType), "0x" + address.ToString("X")))
                });
            }
        });

        if (!node) return;

        nodeOptions = nodeOptions.ConsumeTreeNodeOptions();

        var typeSize = valueType.SizeOf();
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
        using var table = ImRaii.Table(nodeOptions.GetKey("StdDequeTable"), 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Index"u8, ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Value");
        ImGui.TableSetupScrollFreeze(2, 1);
        ImGui.TableHeadersRow();

        for (var i = 0ul; i < elementCount; i++)
        {
            var actualIndex = myOff + i;
            var block = actualIndex / (ulong)blockSize & mapSize - 1;
            var offset = actualIndex % (ulong)blockSize;
            var valueAddress = map[block] + (nint)offset * typeSize; // TODO: check this again. this was originally map[block][offset], so doing pointer arithmetic

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Index
            ImGui.Text(i.ToString());

            ImGui.TableNextColumn(); // Value
            DrawPointerType(valueAddress, valueType, new NodeOptions() { AddressPath = nodeOptions.AddressPath.With(valueAddress) });
        }
    }
}
