using System.Reflection;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselDebug.Extensions;

namespace HaselDebug.Utils;

public static unsafe class DebugUtils
{
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

    public static void HighlightNode(AtkResNode* node)
    {
        if (!MemoryUtils.IsPointerValid(node))
            return;

        var scale = 1f;
        var addon = RaptureAtkUnitManager.Instance()->AtkUnitManager.GetAddonByNodeSafe(node);
        if (MemoryUtils.IsPointerValid(addon))
            scale *= addon->Scale;

        var pos = new Vector2(node->ScreenX, node->ScreenY);
        var size = new Vector2(node->Width, node->Height) * scale;
        ImGui.GetForegroundDrawList().AddRect(pos, pos + size, Color.Gold.ToUInt());
    }
}
