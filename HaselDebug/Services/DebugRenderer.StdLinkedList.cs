using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.STD;
using HaselCommon.Services;
using HaselDebug.Extensions;
using HaselDebug.Utils;
using HaselDebug.Windows;
using ImGuiNET;

namespace HaselDebug.Services;

public unsafe partial class DebugRenderer
{
    public void DrawStdLinkedList(nint address, Type valueType, NodeOptions nodeOptions)
    {
        if (*(nint*)address == 0)
        {
            ImGui.TextUnformatted("Not initialized");
            return;
        }

        var elementCount = *(uint*)(address + 0x10);
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
                    ClickCallback = () => _windowManager.Open(new PointerTypeWindow(_windowManager, _textService, _addonObserver, _serviceProvider, address, typeof(StdList<>).MakeGenericType(valueType), "0x" + address.ToString("X")))
                });
            }
        });

        if (!node) return;

        nodeOptions = nodeOptions.ConsumeTreeNodeOptions();

        var _start = *(nint*)(address + 0x8);
        var _current = _start;
        var nodeType = typeof(StdLinkedList<>.Node).MakeGenericType(valueType);
        var nextOffset = Marshal.OffsetOf(nodeType, "Next");

        bool MoveNext()
        {
            _current = *(nint*)(_current + nextOffset); // _current->Next
            return _current != 0;
        }

        using var indent = ImRaii.PushIndent(1, nodeOptions.Indent);
        using var table = ImRaii.Table(nodeOptions.GetKey("StdLinkedListTable"), 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Value");
        ImGui.TableSetupScrollFreeze(2, 1);
        ImGui.TableHeadersRow();

        var i = 0;
        do
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Index
            ImGui.TextUnformatted(i.ToString());

            ImGui.TableNextColumn(); // Value
            DrawPointerType(_current, valueType, new NodeOptions()
            {
                AddressPath = nodeOptions.AddressPath,
                IsIconIdField = nodeOptions.IsIconIdField
            });
            i++;
        } while (MoveNext());
    }
}
