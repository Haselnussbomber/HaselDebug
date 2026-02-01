using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Utility;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselDebug.Services;

[AutoConstruct]
[RegisterSingleton(Duplicate = DuplicateStrategy.Append, Registration = RegistrationStrategy.SelfWithProxyFactory)]
public partial class TypeService : IHostedService
{
    private readonly ILogger<TypeService> _logger;
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly Dictionary<Type, OrderedDictionary<int, (string, Type?)>> _offsetTypeStructs = [];
    private readonly TaskCompletionSource _loadedTcs = new();

    public Task Loaded => _loadedTcs.Task;
    public ImmutableSortedDictionary<string, Type>? CSTypes { get; private set; }
    public ImmutableSortedDictionary<string, Type>? AddonTypes { get; private set; }
    public ImmutableSortedDictionary<AgentId, Type>? AgentTypes { get; private set; }
    public ConcurrentDictionary<nint, Type>? CustomNodeTypes { get; private set; }
    public Type[] Instances { get; private set; } = [];

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var csAssembly = typeof(AddonAttribute).Assembly;

        LoadClientStructs(csAssembly);
        LoadAddons(csAssembly);
        LoadAgents(csAssembly);
        LoadInstances(csAssembly);

        CustomNodeTypes = _pluginInterface
            .GetOrCreateData("KamiToolKitAllocatedNodes", () => new ConcurrentDictionary<nint, Type>());

        _loadedTcs.SetResult();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _pluginInterface.RelinquishData("KamiToolKitAllocatedNodes");
        return Task.CompletedTask;
    }

    public Type GetAddonType(string addonName)
    {
        return AddonTypes != null && AddonTypes.TryGetValue(addonName, out var type) ? type : typeof(AtkUnitBase);
    }

    public Type GetAgentType(AgentId agentId)
    {
        return AgentTypes != null && AgentTypes.TryGetValue(agentId, out var type) ? type : typeof(AgentInterface);
    }

    public OrderedDictionary<int, (string, Type?)> GetTypeFields(Type type, string prefix = "", int offset = 0)
    {
        if (_offsetTypeStructs.TryGetValue(type, out var mapping))
            return mapping;

        _offsetTypeStructs[type] = mapping = [];

        LoadTypeMapping(mapping, prefix, offset, type);

        for (offset = 0; offset < type.SizeOf() - 8; offset += 8)
        {
            if (!mapping.ContainsKey(offset))
            {
                mapping[offset] = ($"+0x{offset:X}", null);
            }
        }

        return mapping;
    }

    private void LoadClientStructs(Assembly csAssembly)
    {
        CSTypes = csAssembly.GetTypes()
            .Where(type => type.FullName != null)
            .ToImmutableSortedDictionary(
                type => type.FullName!,
                type => type);
    }

    private void LoadAddons(Assembly csAssembly)
    {
        AddonTypes = csAssembly.GetTypes()
            .Where(type => type.GetCustomAttribute<AddonAttribute>() != null)
            .SelectMany(type => type.GetCustomAttribute<AddonAttribute>()!.AddonIdentifiers, (type, addonName) => (type, addonName))
            .ToImmutableSortedDictionary(
                tuple => tuple.addonName,
                tuple => tuple.type);
    }

    private void LoadAgents(Assembly csAssembly)
    {
        AgentTypes = csAssembly.GetTypes()
            .Where(type => type.GetCustomAttribute<AgentAttribute>() != null)
            .Select(type => (type, agentId: type.GetCustomAttribute<AgentAttribute>()!.Id))
            .ToImmutableSortedDictionary(
                tuple => tuple.agentId,
                tuple => tuple.type);
    }

    private void LoadInstances(Assembly csAssembly)
    {
        Instances = [.. csAssembly.GetTypes().Where(type => type.IsStruct() && type.GetMethod("Instance", BindingFlags.Static | BindingFlags.Public) is { } method && method.GetParameters().Length == 0 && method.ReturnType.IsPointer)];

        _logger.LogDebug("Loaded {count} instances", Instances.Length);
    }

    private static void LoadTypeMapping(OrderedDictionary<int, (string, Type?)> fields, string prefix, int offset, Type type)
    {
        foreach (var fieldInfo in type.GetFields(BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (fieldInfo.GetCustomAttribute<FieldOffsetAttribute>() is not { } fieldOffsetAttribute)
                continue;

            if (fieldInfo.IsAssembly
                && fieldInfo.GetCustomAttribute<FixedSizeArrayAttribute>() is FixedSizeArrayAttribute fixedSizeArrayAttribute
                && !fixedSizeArrayAttribute.IsString
                && !fixedSizeArrayAttribute.IsBitArray
                && fieldInfo.FieldType.GetCustomAttribute<InlineArrayAttribute>() is InlineArrayAttribute inlineArrayAttribute)
            {
                var innerType = fieldInfo.FieldType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)[0].FieldType;

                if (innerType.IsGenericType && innerType.Name.StartsWith("Pointer`1"))
                    innerType = innerType.GenericTypeArguments[0].MakePointerType();

                if (innerType.IsStruct())
                {
                    for (var i = 0; i < inlineArrayAttribute.Length; i++)
                    {
                        LoadTypeMapping(fields, $"{prefix}{fieldInfo.Name[1..].FirstCharToUpper()}[{i}].", offset + fieldOffsetAttribute.Value + i * innerType.SizeOf(), innerType);
                    }
                }
                else
                {
                    for (var i = 0; i < inlineArrayAttribute.Length; i++)
                    {
                        fields[offset + fieldOffsetAttribute.Value + i * innerType.SizeOf()] = ($"{prefix}{fieldInfo.Name[1..].FirstCharToUpper()}[{i}]", innerType);
                    }
                }
            }
            else if (fieldInfo.FieldType.IsStruct())
            {
                LoadTypeMapping(fields, prefix + fieldInfo.Name + ".", offset + fieldOffsetAttribute.Value, fieldInfo.FieldType);
            }
            else if (!fields.ContainsKey(offset + fieldOffsetAttribute.Value))
            {
                fields[offset + fieldOffsetAttribute.Value] = (prefix + fieldInfo.Name, fieldInfo.FieldType);
            }
        }
    }
}
