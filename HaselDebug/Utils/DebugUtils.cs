using System.Reflection;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselDebug.Utils;

// Note: these are globals!

public static unsafe class DebugUtils
{
    private static readonly Dictionary<Type, FieldInfo[]> FieldCache = [];
    private static readonly Dictionary<(Type, Type), bool> InheritsCache = [];

    public static bool Inherits<T>(Type pointerType) where T : struct
    {
        var targetType = typeof(T);
        var currentType = pointerType;

        if (currentType == targetType)
            return true;

        if (InheritsCache.TryGetValue((pointerType, targetType), out var result))
            return result;

        do
        {
            var attributes = currentType.GetCustomAttributes();
            var inheritedTypeFound = false;

            foreach (var attr in attributes)
            {
                var attrType = attr.GetType();

                if (!attrType.IsGenericType)
                    continue;

                if (attrType.GetGenericTypeDefinition() != typeof(InheritsAttribute<>))
                    continue;

                var parentOffsetProperty = attrType.GetProperty("ParentOffset", BindingFlags.Instance | BindingFlags.Public);
                if (parentOffsetProperty == null || parentOffsetProperty.GetValue(attr) is null or (not 0))
                    continue;

                var attrStructType = attrType.GenericTypeArguments[0]!;
                if (attrStructType == currentType)
                    continue;

                currentType = attrStructType;
                inheritedTypeFound = true;
                break;
            }

            if (!inheritedTypeFound)
                break;

        } while (currentType != targetType);

        result = currentType == targetType;
        InheritsCache.TryAdd((pointerType, targetType), result);
        return result;
    }

    public static FieldInfo[] GetAllInheritedFields(Type type)
    {
        if (FieldCache.TryGetValue(type, out var fields))
            return fields;

        var fieldsByOffsetAndName = new SortedDictionary<(int, string), FieldInfo>();

        void CollectFieldsRecursive(Type currentType)
        {
            var fields = currentType
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(fieldInfo => !fieldInfo.IsLiteral);

            foreach (var field in fields)
            {
                if (field.GetCustomAttribute<FieldOffsetAttribute>() is not FieldOffsetAttribute fieldOffsetAttr)
                    continue;

                fieldsByOffsetAndName.TryAdd((fieldOffsetAttr.Value, field.Name), field);
            }

            var inheritAttrs = currentType.GetCustomAttributes()
                .Where(attr => attr.GetType() is var attrType &&
                               attrType.IsGenericType &&
                               attrType.GetGenericTypeDefinition() == typeof(InheritsAttribute<>));

            foreach (var attr in inheritAttrs)
            {
                var parentType = attr.GetType().GenericTypeArguments[0];
                CollectFieldsRecursive(parentType);
            }
        }

        CollectFieldsRecursive(type);

        return FieldCache[type] = [.. fieldsByOffsetAndName.Values];
    }

    public static Type GetAtkEventDataType(AtkEventType eventType)
    {
        var type = typeof(AtkEventData);

        if ((int)eventType is >= (int)AtkEventType.MouseDown and <= (int)AtkEventType.MouseDoubleClick)
        {
            type = typeof(AtkEventData.AtkMouseData);
        }
        else if ((int)eventType is >= (int)AtkEventType.InputReceived and <= (int)AtkEventType.InputNavigation)
        {
            type = typeof(AtkEventData.AtkInputData);
        }
        else if ((int)eventType is >= (int)AtkEventType.ListItemRollOver and <= (int)AtkEventType.ListItemSelect)
        {
            type = typeof(AtkEventData.AtkListItemData);
        }
        else if ((int)eventType is >= (int)AtkEventType.DragDropBegin and <= (int)AtkEventType.DragDropClick)
        {
            type = typeof(AtkEventData.AtkDragDropData);
        }
        else if (eventType == AtkEventType.ChildAddonAttached)
        {
            type = typeof(AtkEventData.AtkAddonControlData);
        }
        else if (eventType == AtkEventType.ValueUpdate)
        {
            type = typeof(AtkEventData.AtkValueData);
        }
        else if (eventType == AtkEventType.TimelineActiveLabelChanged)
        {
            type = typeof(AtkEventData.AtkTimelineData);
        }
        else if ((int)eventType is >= (int)AtkEventType.LinkMouseClick and <= (int)AtkEventType.LinkMouseOut)
        {
            type = typeof(LinkData);
        }

        return type;
    }
}
