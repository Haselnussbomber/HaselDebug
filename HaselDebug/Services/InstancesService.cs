using System.Reflection;
using FFXIVClientStructs.Attributes;

namespace HaselDebug.Services;

[RegisterSingleton]
public class InstancesService
{
    public Instance[] Instances { get; init; }

    public unsafe InstancesService()
    {
        var list = new List<Instance>();

        foreach (var type in typeof(AgentAttribute).Assembly.GetTypes())
        {
            if (!type.IsStruct())
                continue;

            var method = type.GetMethod("Instance", BindingFlags.Static | BindingFlags.Public);
            if (method == null || method.GetParameters().Length != 0 || !method.ReturnType.IsPointer)
                continue;

            var pointer = method?.Invoke(null, null);
            if (pointer == null)
                continue;

            var address = (nint)Pointer.Unbox(pointer);
            list.Add(new Instance(address, type));
        }

        Instances = [.. list];
    }

    public record Instance(nint Address, Type Type);
}
