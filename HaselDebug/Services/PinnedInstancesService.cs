using System.Collections;
using System.Threading.Tasks;
using HaselDebug.Config;
using HaselDebug.Utils;

namespace HaselDebug.Services;

[RegisterSingleton, AutoConstruct]
public partial class PinnedInstancesService : IReadOnlyCollection<PinnedInstanceTab>
{
    private readonly PluginConfig _pluginConfig;
    private readonly DebugRenderer _debugRenderer;
    private readonly TypeService _typeService;
    private readonly List<PinnedInstanceTab> _tabs = [];

    public event Action? Loaded;

    [AutoPostConstruct]
    public void Initialize()
    {
        Task.Run(InitAsync);
    }

    private async Task InitAsync()
    {
        await _typeService.Loaded;

        // make sure the types of pinned instances exist
        _pluginConfig.PinnedInstances = [.. _pluginConfig.PinnedInstances.Where(name => _typeService.Instances.Any(type => type.FullName == name))];

        // restore saved pinned instances
        foreach (var name in _pluginConfig.PinnedInstances)
        {
            var type = _typeService.Instances.FirstOrDefault(type => type.FullName == name);
            if (type != null)
                _tabs.Add(new PinnedInstanceTab(_debugRenderer, type));
        }

        Sort();

        Loaded?.Invoke();
    }

    private void Sort()
    {
        _tabs.Sort((a, b) => a.InternalName.CompareTo(b.InternalName));
    }

    public void Add(Type type)
    {
        _tabs.Add(new PinnedInstanceTab(_debugRenderer, type));
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

    public bool Contains(string fullName) => _tabs.Any(tab => tab.InternalName == fullName);
    public bool Contains(Type type) => _tabs.Any(tab => tab.Type == type);

    public int Count => _tabs.Count;

    public IEnumerator<PinnedInstanceTab> GetEnumerator() => _tabs.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _tabs.GetEnumerator();
}
