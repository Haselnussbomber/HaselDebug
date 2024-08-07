using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HaselDebug.Extensions;

public static class FieldInfoExtensions
{
    public static bool IsFixed(this FieldInfo info)
    {
        return info.GetCustomAttributes(typeof(FixedBufferAttribute), false).Cast<FixedBufferAttribute>().FirstOrDefault() != null;
    }

    public static Type GetFixedType(this FieldInfo info)
    {
        return info.GetCustomAttributes(typeof(FixedBufferAttribute), false).Cast<FixedBufferAttribute>().Single().ElementType;
    }

    public static int GetFixedSize(this FieldInfo info)
    {
        return info.GetCustomAttributes(typeof(FixedBufferAttribute), false).Cast<FixedBufferAttribute>().Single().Length;
    }

    public static int GetFieldOffset(this FieldInfo info)
    {
        var attrs = info.GetCustomAttributes(typeof(FieldOffsetAttribute), false);

        return attrs.Length != 0
            ? attrs.Cast<FieldOffsetAttribute>().Single().Value
            : info.GetFieldOffsetSequential();
    }

    public static int GetFieldOffsetSequential(this FieldInfo info)
    {
        if (info.DeclaringType == null)
            throw new Exception($"Unable to access declaring type of field {info.Name}");

        var fields = info.DeclaringType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var offset = 0;

        foreach (var field in fields)
        {
            if (field == info)
                return offset;

            offset += field.FieldType.SizeOf();
        }

        Service.Provider?.GetRequiredService<IPluginLog>().Debug($"{info.DeclaringType.Name} - {info.Name}");
        throw new Exception("Field not found");
    }
}
