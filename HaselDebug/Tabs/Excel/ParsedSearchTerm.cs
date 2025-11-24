using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using Lumina.Data.Structs.Excel;

namespace HaselDebug.Tabs.Excel;

public readonly struct ParsedSearchTerm
{
    private readonly bool _searchMacroString;

    public ParsedSearchTerm(string searchTerm, bool searchMacroString)
    {
        String = searchTerm;
        _searchMacroString = searchMacroString;

        // --- Constructor Body (Parsing Logic) ---
        IsBool = bool.TryParse(searchTerm, out Bool);
        IsSByte = sbyte.TryParse(searchTerm, out SByte);
        IsByte = byte.TryParse(searchTerm, out Byte);
        IsShort = short.TryParse(searchTerm, out Short);
        IsUShort = ushort.TryParse(searchTerm, out UShort);
        IsInt = int.TryParse(searchTerm, out Int);
        IsUInt = uint.TryParse(searchTerm, out UInt);
        IsLong = long.TryParse(searchTerm, out Long);
        IsULong = ulong.TryParse(searchTerm, out ULong);
        IsFloat = float.TryParse(searchTerm, out Float);
    }

    public readonly string String;

    // --- Field Definitions (Flag and Value) ---

    // Boolean
    public readonly bool IsBool;
    public readonly bool Bool;

    // Signed Integers
    public readonly bool IsSByte;
    public readonly sbyte SByte;
    public readonly bool IsShort;
    public readonly short Short;
    public readonly bool IsInt;
    public readonly int Int;
    public readonly bool IsLong;
    public readonly long Long;

    // Unsigned Integers
    public readonly bool IsByte;
    public readonly byte Byte;
    public readonly bool IsUShort;
    public readonly ushort UShort;
    public readonly bool IsUInt;
    public readonly uint UInt;
    public readonly bool IsULong;
    public readonly ulong ULong;

    // Floating Point Numbers
    public readonly bool IsFloat;
    public readonly float Float;

    public bool IsMatch(PropertyInfo prop, object? value, [NotNullWhen(returnValue: true)] out string? columnValue)
    {
        if (prop.PropertyType == typeof(ReadOnlySeString)
            && value is ReadOnlySeString stringValue
            && stringValue.ToString(_searchMacroString ? "m" : "t") is { } str
            && str.Contains(String, StringComparison.InvariantCultureIgnoreCase))
        {
            columnValue = str;
            return true;
        }

        if (prop.PropertyType == typeof(bool)
            && IsBool
            && value is bool boolValue
            && boolValue == Bool)
        {
            columnValue = boolValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (prop.PropertyType == typeof(sbyte)
            && IsSByte
            && value is sbyte sbyteValue
            && sbyteValue == SByte)
        {
            columnValue = sbyteValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (prop.PropertyType == typeof(short)
            && IsShort
            && value is short shortValue
            && shortValue == Short)
        {
            columnValue = shortValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (prop.PropertyType == typeof(int)
            && IsInt
            && value is int intValue
            && intValue == Int)
        {
            columnValue = intValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (prop.PropertyType == typeof(long)
            && IsLong
            && value is long longValue
            && longValue == Long)
        {
            columnValue = longValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (prop.PropertyType == typeof(byte)
            && IsByte
            && value is byte byteValue
            && byteValue == Byte)
        {
            columnValue = byteValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (prop.PropertyType == typeof(ushort)
            && IsUShort
            && value is ushort ushortValue
            && ushortValue == UShort)
        {
            columnValue = ushortValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (prop.PropertyType == typeof(uint)
            && IsUInt
            && value is uint uintValue
            && uintValue == UInt)
        {
            columnValue = uintValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (prop.PropertyType == typeof(ulong)
            && IsULong
            && value is ulong ulongValue
            && ulongValue == ULong)
        {
            columnValue = ulongValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (prop.PropertyType == typeof(float)
            && IsFloat
            && value is float floatValue
            && floatValue == Float)
        {
            columnValue = floatValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }


        if (prop.PropertyType.IsGenericType
            && IsUInt
            && prop.PropertyType.GetGenericTypeDefinition() is { } genericTypeDefinition
            && genericTypeDefinition == typeof(RowRef<>)
            && prop.PropertyType.GetProperty("RowId", BindingFlags.Public | BindingFlags.Instance) is { } rowIdProp
            && rowIdProp.GetValue(value) is uint rowIdValue
            && rowIdValue == UInt)
        {
            columnValue = rowIdValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        columnValue = null;
        return false;
    }

    public bool IsMatch(RawRow row, int columnIndex, [NotNullWhen(returnValue: true)] out string? columnValue)
    {
        var column = row.Columns[columnIndex];

        if (column.Type == ExcelColumnDataType.String
            && row.ReadStringColumn(columnIndex) is { } stringValue
            && stringValue.ToString(_searchMacroString ? "m" : "t") is { } str
            && str.Contains(String, StringComparison.InvariantCultureIgnoreCase))
        {
            columnValue = str;
            return true;
        }

        if (column.Type == ExcelColumnDataType.Bool
            && IsBool
            && row.ReadBoolColumn(columnIndex) is { } boolValue
            && boolValue == Bool)
        {
            columnValue = boolValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (column.Type == ExcelColumnDataType.Int8
            && IsSByte
            && row.ReadInt8Column(columnIndex) is { } sbyteValue
            && sbyteValue == SByte)
        {
            columnValue = sbyteValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (column.Type == ExcelColumnDataType.Int16
            && IsShort
            && row.ReadInt16Column(columnIndex) is { } shortValue
            && shortValue == Short)
        {
            columnValue = shortValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (column.Type == ExcelColumnDataType.Int32
            && IsInt
            && row.ReadInt32Column(columnIndex) is { } intValue
            && intValue == Int)
        {
            columnValue = intValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (column.Type == ExcelColumnDataType.Int64
            && IsLong
            && row.ReadInt64Column(columnIndex) is { } longValue
            && longValue == Long)
        {
            columnValue = longValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (column.Type == ExcelColumnDataType.UInt8
            && IsByte
            && row.ReadUInt8Column(columnIndex) is { } byteValue
            && byteValue == Byte)
        {
            columnValue = byteValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (column.Type == ExcelColumnDataType.UInt16
            && IsUShort
            && row.ReadUInt16Column(columnIndex) is { } ushortValue
            && ushortValue == UShort)
        {
            columnValue = ushortValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (column.Type == ExcelColumnDataType.UInt32
            && IsUInt
            && row.ReadUInt32Column(columnIndex) is { } uintValue
            && uintValue == UInt)
        {
            columnValue = uintValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (column.Type == ExcelColumnDataType.UInt64
            && IsULong
            && row.ReadUInt64Column(columnIndex) is { } ulongValue
            && ulongValue == ULong)
        {
            columnValue = ulongValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (column.Type == ExcelColumnDataType.Float32
            && IsFloat
            && row.ReadFloat32Column(columnIndex) is { } floatValue
            && floatValue == Float)
        {
            columnValue = floatValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (column.Type
            is ExcelColumnDataType.PackedBool0
            or ExcelColumnDataType.PackedBool1
            or ExcelColumnDataType.PackedBool2
            or ExcelColumnDataType.PackedBool3
            or ExcelColumnDataType.PackedBool4
            or ExcelColumnDataType.PackedBool5
            or ExcelColumnDataType.PackedBool6
            or ExcelColumnDataType.PackedBool7
            && IsBool
            && row.ReadPackedBoolColumn(columnIndex) is { } packedBoolValue
            && packedBoolValue == Bool)
        {
            columnValue = packedBoolValue.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        columnValue = null;
        return false;
    }
}
