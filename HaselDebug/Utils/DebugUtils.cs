using System.Numerics;
using System.Reflection;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Graphics;
using ImGuiNET;
using InteropGenerator.Runtime.Attributes;

namespace HaselDebug.Utils;

public static unsafe class DebugUtils
{
    public static bool Inherits<T>(Type pointerType) where T : struct
    {
        var targetType = typeof(T);
        var currentType = pointerType;

        if (currentType == targetType)
            return true;

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

        return currentType == targetType;
    }

    public static void HighlightNode(AtkResNode* node)
    {
        if (node == null)
            return;

        var pos = new Vector2(node->ScreenX, node->ScreenY);
        var size = new Vector2(node->Width, node->Height);
        ImGui.GetForegroundDrawList().AddRect(pos, pos + size, Color.Gold.ToUInt());
    }
}
