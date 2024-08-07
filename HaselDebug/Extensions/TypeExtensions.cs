using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace HaselDebug.Extensions;

public static class TypeExtensions
{
    public static bool IsVoid(this Type type)
    {
        if (type == null)
            return false;

        if (type == typeof(void))
            return true;

        if (!type.IsPointer)
            return false;

        return type.GetElementType()?.IsVoid() ?? false;
    }

    public static bool IsNumericType(this Type type)
        => type == typeof(nint)
        || type == typeof(Half)
        || Type.GetTypeCode(type)
            is TypeCode.Byte
            or TypeCode.SByte
            or TypeCode.Int16
            or TypeCode.UInt16
            or TypeCode.Int32
            or TypeCode.UInt32
            or TypeCode.Int64
            or TypeCode.UInt64
            or TypeCode.Decimal
            or TypeCode.Double
            or TypeCode.Single;

    public static bool IsStruct(this Type type)
        => type.IsValueType
        && !type.IsEnum
        && !type.IsEquivalentTo(typeof(decimal))
        && !type.IsPrimitive;

    public static string ReadableTypeName(this Type type, bool fullName = false)
    {
        var stars = string.Empty;

        var i = 0;
        while (type.IsPointer)
        {
            stars += "*";
            type = type.GetElementType()!;
            if (i++ > 10) break; // not yet encountered, but better be safe!
        }

        if (type.IsVoid())
            return "void" + stars;

        if (type == typeof(nint) || type.GetElementType() == typeof(nint))
            return "nint" + stars;

        if (!type.IsEnum)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    return "bool" + stars;
                case TypeCode.Char:
                    return "char" + stars;
                case TypeCode.SByte:
                    return "sbyte" + stars;
                case TypeCode.Byte:
                    return "byte" + stars;
                case TypeCode.Int16:
                    return "short" + stars;
                case TypeCode.UInt16:
                    return "ushort" + stars;
                case TypeCode.Int32:
                    return "int" + stars;
                case TypeCode.UInt32:
                    return "uint" + stars;
                case TypeCode.Int64:
                    return "long" + stars;
                case TypeCode.UInt64:
                    return "ulong" + stars;
                case TypeCode.Single:
                    return "float" + stars;
                case TypeCode.Double:
                    return "double" + stars;
                case TypeCode.Decimal:
                    return "decimal" + stars;
                case TypeCode.String:
                    return "string" + stars;
            }
        }

        if (type.IsGenericType)
            return $"{type.Name[..type.Name.IndexOf('`')]}<{string.Join(",", type.GetGenericArguments().Select((t) => t.ReadableTypeName(fullName)))}>{stars}";

        if (type.IsUnmanagedFunctionPointer)
        {
            var argTypes = type.GetFunctionPointerParameterTypes();
            var argTypeStr = argTypes.Length > 0
                ? string.Join(", ", argTypes.Select(argType => argType.ReadableTypeName(fullName)))
                : string.Empty;
            var retType = type.GetFunctionPointerReturnType().ReadableTypeName(fullName);
            return $"delegate* unmanaged<{(string.IsNullOrEmpty(argTypeStr) ? string.Empty : argTypeStr + ", ")}{retType}>";
        }

        return (fullName ? type.FullName ?? type.Name : type.Name) + stars;
    }

    public static int SizeOf(this Type type)
        => type switch
        {
            _ when type == typeof(sbyte)
                || type == typeof(byte)
                || type == typeof(bool)
                => 1,

            _ when type == typeof(char)
                || type == typeof(short)
                || type == typeof(ushort)
                || type == typeof(Half)
                => 2,

            _ when type == typeof(int)
                || type == typeof(uint)
                || type == typeof(float)
                => 4,

            _ when type == typeof(long)
                || type == typeof(ulong)
                || type == typeof(double)
                || type.IsPointer
                || type.IsFunctionPointer
                || type.IsUnmanagedFunctionPointer
                // || (type.Name == "Pointer`1" && type.Namespace == ExporterStatics.InteropNamespacePrefix[..^1])
                => 8,

            _ when type.Name.StartsWith("FixedSizeArray")
                => type.GetGenericArguments()[0].SizeOf() * int.Parse(type.Name[14..type.Name.IndexOf('`')]),

            _ when type.GetCustomAttribute<InlineArrayAttribute>() is { Length: var length }
                => type.GetGenericArguments()[0].SizeOf() * length,

            _ when type.IsStruct()
                && !type.IsGenericType
                && (type.StructLayoutAttribute?.Value ?? LayoutKind.Sequential) != LayoutKind.Sequential
                => type.StructLayoutAttribute?.Size
                    ?? (int?)typeof(Unsafe).GetMethod("SizeOf")?.MakeGenericMethod(type).Invoke(null, null)
                    ?? 0,

            _ when type.IsEnum
                => Enum.GetUnderlyingType(type).SizeOf(),

            _ when type.IsGenericType
                => Marshal.SizeOf(Activator.CreateInstance(type)!),

            _ => type.GetSizeOf()
        };

    private static int GetSizeOf(this Type type)
    {
        try
        {
            return Marshal.SizeOf(Activator.CreateInstance(type)!);
        }
        catch
        {
            return 0;
        }
    }
}
