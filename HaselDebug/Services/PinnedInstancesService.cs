using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HaselDebug.Config;
using HaselDebug.Utils;

namespace HaselDebug.Services;

[RegisterSingleton]
public class PinnedInstancesService : IReadOnlyCollection<PinnedInstanceTab>
{
    private readonly PluginConfig PluginConfig;
    private readonly DebugRenderer DebugRenderer;
    private readonly List<PinnedInstanceTab> Tabs = [];

    public PinnedInstancesService(PluginConfig pluginConfig, InstancesService InstancesService, DebugRenderer debugRenderer)
    {
        PluginConfig = pluginConfig;
        DebugRenderer = debugRenderer;

        // make sure the types of pinned instances exist
        PluginConfig.PinnedInstances = PluginConfig.PinnedInstances
            .Where(name => InstancesService.Instances.Any(inst => inst.Type.FullName == name))
            .ToArray();

        // restore saved pinned instances
        foreach (var name in PluginConfig.PinnedInstances)
        {
            var inst = InstancesService.Instances.FirstOrDefault(inst => inst.Type.FullName == name);
            if (inst == null) continue;
            Tabs.Add(new PinnedInstanceTab(DebugRenderer, inst.Address, inst.Type));
        }

        Sort();
    }

    private void Sort()
    {
        Tabs.Sort((a, b) => a.InternalName.CompareTo(b.InternalName));
    }

    public void Add(nint address, Type type)
    {
        Tabs.Add(new PinnedInstanceTab(DebugRenderer, address, type));
        Sort();

        var nameList = new List<string>(PluginConfig.PinnedInstances)
        {
            type.FullName!
        };

        nameList.Sort();

        PluginConfig.PinnedInstances = [.. nameList];
        PluginConfig.Save();
    }

    public void Remove(PinnedInstanceTab tab)
    {
        Tabs.Remove(tab);
        Sort();

        PluginConfig.PinnedInstances = PluginConfig.PinnedInstances
            .Where(name => name != tab.Type.FullName!)
            .Order()
            .ToArray();

        PluginConfig.Save();
    }

    public void Remove(Type type)
    {
        var tab = Tabs.FirstOrDefault(tab => tab.Type == type);
        if (tab == null)
            return;

        Remove(tab);
    }

    public void Remove(Type type, nint address)
    {
        var tab = Tabs.FirstOrDefault(tab => tab.Type == type && tab.Address == address);
        if (tab == null)
            return;

        Remove(tab);
    }

    public bool Contains(string fullName) => Tabs.Any(tab => tab.InternalName == fullName);
    public bool Contains(Type type) => Tabs.Any(tab => tab.Type == type);

    public int Count => Tabs.Count;

    public IEnumerator<PinnedInstanceTab> GetEnumerator() => Tabs.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Tabs.GetEnumerator();
}
