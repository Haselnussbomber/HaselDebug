using System.Collections;
using HaselDebug.Config;
using HaselDebug.Utils;

namespace HaselDebug.Services;

[RegisterSingleton, AutoConstruct]
public partial class PinnedInstancesService : IReadOnlyCollection<PinnedInstanceTab>
{
    private readonly PluginConfig _pluginConfig;
    private readonly DebugRenderer _debugRenderer;
    private readonly InstancesService _instancesService;
    private readonly List<PinnedInstanceTab> _tabs = [];

    [AutoPostConstruct]
    public void Initialize()
    {
        _instancesService.Loaded += OnInstancesLoaded;
    }

    private void OnInstancesLoaded()
    {
        _instancesService.Loaded -= OnInstancesLoaded;

        // make sure the types of pinned instances exist
        _pluginConfig.PinnedInstances = _pluginConfig.PinnedInstances
            .Where(name => _instancesService.Instances.Any(inst => inst.Type.FullName == name))
            .ToArray();

        // restore saved pinned instances
        foreach (var name in _pluginConfig.PinnedInstances)
        {
            var inst = _instancesService.Instances.FirstOrDefault(inst => inst.Type.FullName == name);
            if (inst == null) continue;
            _tabs.Add(new PinnedInstanceTab(_debugRenderer, inst.Address, inst.Type));
        }

        Sort();
    }

    private void Sort()
    {
        _tabs.Sort((a, b) => a.InternalName.CompareTo(b.InternalName));
    }

    public void Add(nint address, Type type)
    {
        _tabs.Add(new PinnedInstanceTab(_debugRenderer, address, type));
        Sort();

        var nameList = new List<string>(_pluginConfig.PinnedInstances)
        {
            type.FullName!
        };

        nameList.Sort();

        _pluginConfig.PinnedInstances = [.. nameList];
        _pluginConfig.Save();
    }

    public void Remove(PinnedInstanceTab tab)
    {
        _tabs.Remove(tab);
        Sort();

        _pluginConfig.PinnedInstances = _pluginConfig.PinnedInstances
            .Where(name => name != tab.Type.FullName!)
            .Order()
            .ToArray();

        _pluginConfig.Save();
    }

    public void Remove(Type type)
    {
        var tab = _tabs.FirstOrDefault(tab => tab.Type == type);
        if (tab == null)
            return;

        Remove(tab);
    }

    public void Remove(Type type, nint address)
    {
        var tab = _tabs.FirstOrDefault(tab => tab.Type == type && tab.Address == address);
        if (tab == null)
            return;

        Remove(tab);
    }

    public bool Contains(string fullName) => _tabs.Any(tab => tab.InternalName == fullName);
    public bool Contains(Type type) => _tabs.Any(tab => tab.Type == type);

    public int Count => _tabs.Count;

    public IEnumerator<PinnedInstanceTab> GetEnumerator() => _tabs.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _tabs.GetEnumerator();
}
