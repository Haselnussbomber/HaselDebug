using System.Reflection;
using System.Threading.Tasks;
using FFXIVClientStructs.Attributes;

namespace HaselDebug.Services;

[RegisterSingleton, AutoConstruct]
public partial class InstancesService
{
    public Instance[] Instances { get; private set; } = [];

    public event Action? Loaded;

    public InstancesService()
    {
        Task.Run(Load);
    }

    public async Task Load()
    {
        var csAssembly = typeof(AddonAttribute).Assembly;
        var list = new List<Instance>();

        foreach (var type in csAssembly.GetTypes())
        {
            if (!type.IsStruct())
                continue;

            var method = type.GetMethod("Instance", BindingFlags.Static | BindingFlags.Public);
            if (method == null || method.GetParameters().Length != 0 || !method.ReturnType.IsPointer)
                continue;

            var pointer = method?.Invoke(null, null);
            if (pointer == null)
                continue;

            unsafe
            {
                var address = (nint)Pointer.Unbox(pointer);
                list.Add(new Instance(address, type));
            }
        }

        Instances = [.. list];
        Loaded?.Invoke();
    }

    public record Instance(nint Address, Type Type);
}
