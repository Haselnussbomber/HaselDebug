using System.Collections.Generic;
using System.Reflection;
using FFXIVClientStructs.Attributes;
using HaselDebug.Abstracts;
using HaselDebug.Extensions;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;

namespace HaselDebug.Tabs;

public unsafe class InstancesTab : DebugTab
{
    private readonly DebugRenderer DebugRenderer;
    private readonly (nint Address, Type Type)[] Instances;

    public InstancesTab(DebugRenderer debugRenderer)
    {
        DebugRenderer = debugRenderer;

        var list = new List<(nint, Type)>();

        foreach (var type in typeof(AgentAttribute).Assembly.GetTypes())
        {
            if (!type.IsStruct() || type.GetCustomAttribute<AgentAttribute>() != null)
                continue;

            var method = type.GetMethod("Instance", BindingFlags.Static | BindingFlags.Public);
            if (method == null || method.GetParameters().Length != 0 || !method.ReturnType.IsPointer)
                continue;

            var pointer = method?.Invoke(null, null);
            if (pointer == null)
                continue;

            var address = (nint)Pointer.Unbox(pointer);
            list.Add((address, type));
        }

        Instances = [.. list];
    }

    public override void Draw()
    {
        var i = 0;
        foreach (var (ptr, type) in Instances)
        {
            DebugRenderer.DrawAddress(ptr);
            ImGui.SameLine(120);
            DebugRenderer.DrawPointerType(ptr, type, new NodeOptions() { AddressPath = new AddressPath(i++) });
        }
    }
}
