using System.Collections.Immutable;
using System.Reflection;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace HaselDebug.Services;

[RegisterSingleton]
public class TypeService
{
    public ImmutableSortedDictionary<string, Type> AddonTypes { get; private set; }
    public ImmutableSortedDictionary<AgentId, Type> AgentTypes { get; private set; }

    public TypeService()
    {
        var csAssembly = typeof(AddonAttribute).Assembly;

        AddonTypes = csAssembly.GetTypes()
            .Where(type => type.GetCustomAttribute<AddonAttribute>() != null)
            .SelectMany(type => type.GetCustomAttribute<AddonAttribute>()!.AddonIdentifiers, (type, addonName) => (type, addonName))
            .ToImmutableSortedDictionary(
                tuple => tuple.addonName,
                tuple => tuple.type);

        AgentTypes = csAssembly.GetTypes()
            .Where(type => type.GetCustomAttribute<AgentAttribute>() != null)
            .Select(type => (type, agentId: type.GetCustomAttribute<AgentAttribute>()!.Id))
            .ToImmutableSortedDictionary(
                tuple => tuple.agentId,
                tuple => tuple.type);
    }
}
