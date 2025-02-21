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
    public void DrawStdSet(nint address, Type valueType, NodeOptions nodeOptions)
    {
        var elementCount = *(ulong*)(address + 0x8);
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
                    ClickCallback = () => _windowManager.Open(new PointerTypeWindow(_windowManager, _textService, _languageProvider, this, address, typeof(StdSet<>).MakeGenericType(valueType), "0x" + address.ToString("X")))
                });
            }
        });

        if (!node) return;

        nodeOptions = nodeOptions.ConsumeTreeNodeOptions();

        var mapType = typeof(StdSetNode<>).MakeGenericType(valueType);
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
        using var table = ImRaii.Table(nodeOptions.GetKey("StdMapTable"), 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableSetupColumn("Value");
        ImGui.TableSetupScrollFreeze(3, 1);
        ImGui.TableHeadersRow();

        while (MoveNext())
        {
            var keyAddress = _current + myvalOffset;

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Address
            DrawAddress(_current);

            ImGui.TableNextColumn(); // Value

            // since key equals value, we just use the key here for simplicity
            DrawPointerType(keyAddress, valueType, new NodeOptions() { AddressPath = nodeOptions.AddressPath.With(keyAddress) });
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct StdSetNode<TValue>
        where TValue : unmanaged
    {
        public nint _Left; // StdMapNode<TKey, TValue>*
        public nint _Parent; // StdMapNode<TKey, TValue>*
        public nint _Right; // StdMapNode<TKey, TValue>*
        public byte _Color; // RedBlackTreeNodeColor
        public byte _Isnil; // bool
        public byte _18;
        public byte _19;
        public TValue _Myval;
    }
}
