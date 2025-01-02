using System.Linq;
using System.Reflection;
using HaselDebug.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace HaselDebug.Utils;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddSubTabs(this IServiceCollection collection)
    {
        var iType = typeof(ISubTab<>);
        foreach (var type in Assembly.GetExecutingAssembly().ExportedTypes.Where(t => t is { IsInterface: false, IsAbstract: false }))
        {
            var subTabInterfaceType = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == iType);
            if (subTabInterfaceType != null && collection.All(t => t.ServiceType != type))
                collection.AddSingleton(subTabInterfaceType, type);
        }
        return collection;
    }
}
