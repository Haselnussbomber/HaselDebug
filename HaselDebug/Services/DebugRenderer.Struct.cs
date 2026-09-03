using System.Buffers.Binary;
using System.Reflection;
using System.Runtime.CompilerServices;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Common.Component.Excel;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.STD;
using HaselDebug.Extensions;
using HaselDebug.Utils;
using static FFXIVClientStructs.FFXIV.Component.GUI.AtkUldManager;

namespace HaselDebug.Services;

public unsafe partial class DebugRenderer
{
    private void DrawStruct(nint address, Type type, NodeOptions nodeOptions)
    {
        nodeOptions = nodeOptions.WithAddress(address);

        var fields = GetAllInheritedFields(type);

        if (type == typeof(AtkComponentTreeListItem) && nodeOptions.SeStringTitle == null && nodeOptions.Title == null)
        {
            var item = (AtkComponentTreeListItem*)address;
            if (item->StringValues.Count > 0)
            {
                var str = HaselCommon.Extensions.CStringExtensions.AsReadOnlySeString(item->StringValues[0]);
                if (!str.IsEmpty)
                    nodeOptions = nodeOptions with { SeStringTitle = str };
            }
        }

        using var disabled = ImRaii.Disabled(fields.Length == 0);
        using var node = DrawTreeNode(nodeOptions.WithSeStringTitleIfNull(type.FullName ?? "Unknown Type Name"));
        if (!node) return;

        var processedFields = fields
            .OrderBy(fieldInfo => fieldInfo.FieldOffset)
            .Select(fieldInfo => (
                Info: fieldInfo,
                Offset: fieldInfo.FieldOffset,
                Size: fieldInfo.IsFixed ? fieldInfo.FixedType.SizeOf() * fieldInfo.FixedSize : fieldInfo.FieldType.SizeOf()));

        nodeOptions = nodeOptions.ConsumeTreeNodeOptions();

        foreach (var (i, (fieldInfo, offset, size)) in processedFields.Index())
        {
            DrawField(address, offset, type, fieldInfo, nodeOptions.WithAddress(i));
        }
    }

    private void DrawField(nint address, int offset, Type parentType, FieldInfo fieldInfo, NodeOptions fieldNodeOptions)
    {
        var fieldAddress = address + offset;
        var fieldType = fieldInfo.FieldType;

        DrawBitFields(parentType, fieldAddress, offset, fieldType, fieldInfo);

        ImGuiUtils.DrawCopyableText($"[0x{offset:X}]", new()
        {
            CopyText = ImGui.IsKeyDown(ImGuiKey.LeftShift) ? $"{address + offset:X}" : $"0x{offset:X}",
            TextColor = Color.Text600
        });

        ImGui.SameLine();

        if (fieldType == typeof(uint) && fieldInfo.Name.Contains("IconId"))
            fieldNodeOptions = fieldNodeOptions with { IsIconIdField = true };

        if ((fieldType == typeof(int) || fieldType == typeof(long)) && fieldInfo.Name.Contains("Timestamp"))
            fieldNodeOptions = fieldNodeOptions with { IsTimestampField = true };

        if ((fieldType == typeof(short) || fieldType == typeof(int) || fieldType == typeof(ushort) || fieldType == typeof(uint)) && fieldInfo.Name.Contains("WorldId"))
            fieldNodeOptions = fieldNodeOptions with { IsWorldIdField = true };

        if (Attribute.IsDefined(fieldInfo, typeof(ObsoleteAttribute)) && fieldInfo.GetCustomAttribute<ObsoleteAttribute>() is ObsoleteAttribute obsoleteAttribute)
        {
            using (ImRaii.PushColor(ImGuiCol.Text, (obsoleteAttribute.IsError ? ColorObsoleteError : ColorObsolete).ToUInt()))
                ImGui.Text("[Obsolete]"u8);

            if (!string.IsNullOrEmpty(obsoleteAttribute.Message) && ImGui.IsItemHovered())
                ImGui.SetTooltip(obsoleteAttribute.Message);

            ImGui.SameLine();
        }

        if (fieldInfo.IsStatic)
        {
            ImGui.Text("static"u8);
            ImGui.SameLine();
        }

        ImGuiUtils.DrawCopyableText(fieldType.ReadableTypeName(), new()
        {
            CopyText = fieldType.ReadableTypeName(ImGui.IsKeyDown(ImGuiKey.LeftShift)),
            TextColor = ColorType
        });

        ImGui.SameLine();

        // delegate*
        if (fieldType.IsFunctionPointer || fieldType.IsUnmanagedFunctionPointer)
        {
            DrawFieldName(fieldInfo);
            DrawAddress(*(nint*)fieldAddress);
            return;
        }

        // internal FixedSizeArrays
        if (fieldInfo.IsAssembly
            && Attribute.IsDefined(fieldInfo, typeof(FixedSizeArrayAttribute))
            && Attribute.IsDefined(fieldType, typeof(InlineArrayAttribute))
            && fieldInfo.GetCustomAttribute<FixedSizeArrayAttribute>() is FixedSizeArrayAttribute fixedSizeArrayAttribute
            && fieldType.GetCustomAttribute<InlineArrayAttribute>() is InlineArrayAttribute inlineArrayAttribute)
        {
            DrawFieldName(fieldInfo, fieldInfo.Name[1..].FirstCharToUpper());
            DrawFixedSizeArray(fieldAddress, fieldType, fixedSizeArrayAttribute.IsString, fieldNodeOptions);
            return;
        }

        // StdVector<>
        if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(StdVector<>))
        {
            var underlyingType = fieldType.GenericTypeArguments[0];
            var underlyingTypeSize = underlyingType.SizeOf();
            if (underlyingTypeSize == 0)
            {
                ImGui.TextColored(Color.Red, $"Can't get size of {underlyingType.Name}");
                return;
            }

            DrawFieldName(fieldInfo);
            DrawStdVector(fieldAddress, underlyingType, fieldNodeOptions);
            return;
        }

        // StdDeque<>
        if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(StdDeque<>))
        {
            var underlyingType = fieldType.GenericTypeArguments[0];
            var underlyingTypeSize = underlyingType.SizeOf();
            if (underlyingTypeSize == 0)
            {
                ImGui.TextColored(Color.Red, $"Can't get size of {underlyingType.Name}");
                return;
            }

            DrawFieldName(fieldInfo);
            DrawStdDeque(fieldAddress, underlyingType, fieldNodeOptions);
            return;
        }

        // StdList<>
        if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(StdList<>))
        {
            var underlyingType = fieldType.GenericTypeArguments[0];
            var underlyingTypeSize = underlyingType.SizeOf();
            if (underlyingTypeSize == 0)
            {
                ImGui.TextColored(Color.Red, $"Can't get size of {underlyingType.Name}");
                return;
            }

            DrawFieldName(fieldInfo);
            DrawStdList(fieldAddress, underlyingType, fieldNodeOptions);
            return;
        }

        // AgentInterface.AddonId
        if (Inherits<AgentInterface>(parentType) && fieldType == typeof(uint) && fieldInfo.Name == nameof(AgentInterface.AddonId))
        {
            DrawFieldName(fieldInfo);
            DrawPointerType(fieldAddress, fieldType, fieldNodeOptions);
            var unitBase = RaptureAtkUnitManager.Instance()->GetAddonById(*(ushort*)fieldAddress);
            if (unitBase != null)
            {
                ImGui.SameLine();
                _navigationService.DrawAddonLink(unitBase->Id, unitBase->NameString);
            }
            return;
        }

        // AtkUnitBase.AtkValues
        if (Inherits<AtkUnitBase>(parentType) && fieldType == typeof(AtkValue*) && fieldInfo.Name == nameof(AtkUnitBase.AtkValues))
        {
            DrawFieldName(fieldInfo);
            DrawAtkValues(*(AtkValue**)fieldAddress, ((AtkUnitBase*)address)->AtkValuesCount, fieldNodeOptions);
            return;
        }

        // AtkUldManager.Assets
        if (Inherits<AtkUldManager>(parentType) && fieldType == typeof(AtkUldAsset*) && fieldInfo.Name == nameof(AtkUldManager.Assets))
        {
            DrawFieldName(fieldInfo);
            DrawArray(new Span<AtkUldAsset>(*(nint**)fieldAddress, ((AtkUldManager*)address)->AssetCount), fieldNodeOptions);
            return;
        }

        // AtkUldManager.PartsList
        if (Inherits<AtkUldManager>(parentType) && fieldType == typeof(AtkUldPartsList*) && fieldInfo.Name == nameof(AtkUldManager.PartsList))
        {
            DrawFieldName(fieldInfo);
            DrawArray(new Span<AtkUldPartsList>(*(nint**)fieldAddress, ((AtkUldManager*)address)->PartsListCount), fieldNodeOptions);
            return;
        }

        // AtkUldManager.NodeList
        if (Inherits<AtkUldManager>(parentType) && fieldType == typeof(AtkResNode**) && fieldInfo.Name == nameof(AtkUldManager.NodeList))
        {
            DrawFieldName(fieldInfo);
            DrawArray(new Span<Pointer<AtkResNode>>(*(nint**)fieldAddress, ((AtkUldManager*)address)->NodeListCount), fieldNodeOptions);
            return;
        }

        // AtkUldManager.Objects
        if (Inherits<AtkUldManager>(parentType) && fieldType == typeof(AtkUldObjectInfo*) && fieldInfo.Name == nameof(AtkUldManager.Objects))
        {
            DrawFieldName(fieldInfo);
            var uldManager = (AtkUldManager*)address;
            var objectCount = uldManager->ObjectCount;
            switch (uldManager->BaseType)
            {
                case AtkUldManagerBaseType.Component:
                    if (objectCount == 1)
                        DrawPointerType(*(AtkUldComponentInfo**)fieldAddress, fieldNodeOptions);
                    else
                        DrawArray(new Span<AtkUldComponentInfo>(*(nint**)fieldAddress, objectCount), fieldNodeOptions);
                    break;

                case AtkUldManagerBaseType.Widget:
                    if (objectCount == 1)
                        DrawPointerType(*(AtkUldWidgetInfo**)fieldAddress, fieldNodeOptions);
                    else
                        DrawArray(new Span<AtkUldWidgetInfo>(*(nint**)fieldAddress, objectCount), fieldNodeOptions);
                    break;
            }

            return;
        }

        // AtkUldWidgetInfo.NodeList
        if (parentType == typeof(AtkUldWidgetInfo) && fieldType == typeof(AtkResNode**) && fieldInfo.Name == nameof(AtkUldWidgetInfo.NodeList))
        {
            DrawFieldName(fieldInfo);
            DrawArray(new Span<Pointer<AtkResNode>>(*(nint**)fieldAddress, ((AtkUldWidgetInfo*)address)->NodeCount), fieldNodeOptions);
            return;
        }

        // DuplicateObjectList.NodeList
        if (parentType == typeof(DuplicateObjectList) && fieldType == typeof(AtkComponentNode*) && fieldInfo.Name == nameof(DuplicateObjectList.NodeList))
        {
            DrawFieldName(fieldInfo);
            DrawArray(new Span<AtkComponentNode>(*(nint**)fieldAddress, (int)((DuplicateObjectList*)address)->NodeCount), fieldNodeOptions);
            return;
        }

        // AtkTimelineManager.Timelines
        if (parentType == typeof(AtkTimelineManager) && fieldType == typeof(AtkTimeline*) && fieldInfo.Name == nameof(AtkTimelineManager.Timelines))
        {
            DrawFieldName(fieldInfo);
            DrawArray(new Span<AtkTimeline>(*(nint**)fieldAddress, ((AtkTimelineManager*)address)->TimelineCount), fieldNodeOptions);
            return;
        }

        // AtkTimelineManager.Animations
        if (parentType == typeof(AtkTimelineManager) && fieldType == typeof(AtkTimelineAnimation*) && fieldInfo.Name == nameof(AtkTimelineManager.Animations))
        {
            DrawFieldName(fieldInfo);
            DrawArray(new Span<AtkTimelineAnimation>(*(nint**)fieldAddress, (int)((AtkTimelineManager*)address)->AnimationCount), fieldNodeOptions);
            return;
        }

        // AtkTimelineManager.LabelSets
        if (parentType == typeof(AtkTimelineManager) && fieldType == typeof(AtkTimelineLabelSet*) && fieldInfo.Name == nameof(AtkTimelineManager.LabelSets))
        {
            DrawFieldName(fieldInfo);
            DrawArray(new Span<AtkTimelineLabelSet>(*(nint**)fieldAddress, (int)((AtkTimelineManager*)address)->LabelSetCount), fieldNodeOptions);
            return;
        }

        // AtkTimelineManager.KeyFrames
        if (parentType == typeof(AtkTimelineManager) && fieldType == typeof(AtkTimelineKeyFrame*) && fieldInfo.Name == nameof(AtkTimelineManager.KeyFrames))
        {
            DrawFieldName(fieldInfo);
            DrawArray(new Span<AtkTimelineKeyFrame>(*(nint**)fieldAddress, (int)((AtkTimelineManager*)address)->KeyFrameCount), fieldNodeOptions);
            return;
        }

        // ExcelSheet.ColumnDefinitions
        if (parentType == typeof(ExcelSheet) && fieldType == typeof(ExcelSheet.ColumnInfo*) && fieldInfo.Name == nameof(ExcelSheet.ColumnDefinitions))
        {
            DrawFieldName(fieldInfo);
            DrawArray(new Span<ExcelSheet.ColumnInfo>(*(nint**)fieldAddress, ((ExcelSheet*)address)->ColumnCount), fieldNodeOptions);
            return;
        }

        // AgentShop.ItemReceive
        if (parentType == typeof(AgentShop) && fieldType == typeof(AgentShop.ShopItem*) && fieldInfo.Name == nameof(AgentShop.ItemReceive))
        {
            DrawFieldName(fieldInfo);
            DrawArray(((AgentShop*)address)->ItemReceiveSpan, fieldNodeOptions);
            return;
        }

        // AgentShop.ItemCost
        if (parentType == typeof(AgentShop) && fieldType == typeof(AgentShop.ShopItem*) && fieldInfo.Name == nameof(AgentShop.ItemCost))
        {
            DrawFieldName(fieldInfo);
            DrawArray(((AgentShop*)address)->ItemCostSpan, fieldNodeOptions);
            return;
        }

        // AgentShop.ItemRetainerBuyback
        if (parentType == typeof(AgentShop) && fieldType == typeof(AgentShop.ShopItem*) && fieldInfo.Name == nameof(AgentShop.ItemRetainerBuyback))
        {
            DrawFieldName(fieldInfo);
            DrawArray(((AgentShop*)address)->ItemRetainerBuybackSpan, fieldNodeOptions);
            return;
        }

        // AddonListIcon.ItemData
        if (parentType == typeof(AddonListIcon) && fieldType == typeof(AddonListIcon.ItemData*) && fieldInfo.Name == nameof(AddonListIcon.ListData))
        {
            DrawFieldName(fieldInfo);
            DrawArray(new Span<AddonListIcon.ItemData>(((AddonListIcon*)address)->ListData, ((AddonListIcon*)address)->TotalItemCount), fieldNodeOptions);
            return;
        }

        // GameWindow.Arguments
        if (parentType == typeof(GameWindow) && fieldType == typeof(CStringPointer*) && fieldInfo.Name == nameof(GameWindow.Arguments))
        {
            DrawFieldName(fieldInfo);
            DrawArray(((GameWindow*)address)->ArgumentsSpan, fieldNodeOptions);
            return;
        }

        // ByteColor.RGBA
        if (parentType == typeof(ByteColor) && fieldType == typeof(uint) && fieldInfo.Name == nameof(ByteColor.RGBA))
        {
            var color = *(ByteColor*)fieldAddress;

            DrawFieldName(fieldInfo);
            DrawPointerNumber(fieldAddress, fieldType, fieldNodeOptions);

            ImGui.SameLine();
            ImGuiUtils.DrawCopyableText($"#{color.RGBA:X8}");

            var abgr = BinaryPrimitives.ReverseEndianness(color.RGBA);
            var currentTheme = RaptureAtkModule.Instance()->AtkUIColorHolder.ActiveColorThemeType;

            if (_excelService.TryFindRow<RawRow>("UIColor", row => row.ReadUInt32Column(currentTheme) == abgr, out var row))
            {
                ImGui.SameLine();
                ImGuiUtils.DrawCopyableText($"UIColor#{row.RowId}");
            }

            ImGui.SameLine();
            ImGui.Dummy(new Vector2(ImStyle.TextLineHeight));
            ImGui.GetWindowDrawList().AddRectFilled(
                ImGui.GetItemRectMin(),
                ImGui.GetItemRectMax(),
                color.RGBA,
                3);
            return;
        }

        // ResourceHandle.FileType
        if (Inherits<ResourceHandle>(parentType) && fieldType == typeof(uint) && fieldInfo.Name == nameof(ResourceHandle.FileType))
        {
            DrawFieldName(fieldInfo);
            DrawPointerNumber(fieldAddress, fieldType, fieldNodeOptions);
            ImGui.SameLine();
            ImGuiUtils.DrawCopyableText(((ResourceHandle*)address)->FileTypeString);
            return;
        }

        // InventoryItem.CrafterContentId
        if (Inherits<InventoryItem>(parentType) && fieldType == typeof(ulong) && fieldInfo.Name == nameof(InventoryItem.CrafterContentId))
        {
            DrawFieldName(fieldInfo);
            DrawPointerNumber(fieldAddress, fieldType, fieldNodeOptions);
            ImGui.SameLine();
            ImGuiUtils.DrawCopyableText(NameCache.Instance()->GetNameByContentId(*(ulong*)fieldAddress).ToString());
            return;
        }

        // byte* that are strings
        if (fieldType.IsPointer && _knownStringPointers.TryGetValue(parentType, out var fieldNames) && fieldNames.Contains(fieldInfo.Name))
        {
            DrawFieldName(fieldInfo);
            DrawSeString(*(byte**)fieldAddress, fieldNodeOptions);
            return;
        }

        // Vector2
        if (fieldType == typeof(Vector2))
        {
            DrawFieldName(fieldInfo);
            DrawPointerType(fieldAddress, fieldType, fieldNodeOptions with { Title = (*(Vector2*)fieldAddress).ToString() });
            return;
        }
        if (fieldType == typeof(FFXIVClientStructs.FFXIV.Common.Math.Vector2))
        {
            DrawFieldName(fieldInfo);
            DrawPointerType(fieldAddress, fieldType, fieldNodeOptions with
            {
                Title = (*(FFXIVClientStructs.FFXIV.Common.Math.Vector2*)fieldAddress).ToString()
            });
            return;
        }

        // Vector3
        if (fieldType == typeof(Vector3))
        {
            DrawFieldName(fieldInfo);
            DrawPointerType(fieldAddress, fieldType, fieldNodeOptions with { Title = (*(Vector3*)fieldAddress).ToString() });
            return;
        }
        if (fieldType == typeof(FFXIVClientStructs.FFXIV.Common.Math.Vector3))
        {
            DrawFieldName(fieldInfo);
            DrawPointerType(fieldAddress, fieldType, fieldNodeOptions with
            {
                Title = (*(FFXIVClientStructs.FFXIV.Common.Math.Vector3*)fieldAddress).ToString()
            });
            return;
        }

        // Vector4
        if (fieldType == typeof(Vector4))
        {
            DrawFieldName(fieldInfo);
            DrawPointerType(fieldAddress, fieldType, fieldNodeOptions with { Title = (*(Vector4*)fieldAddress).ToString() });
            return;
        }
        if (fieldType == typeof(FFXIVClientStructs.FFXIV.Common.Math.Vector4))
        {
            DrawFieldName(fieldInfo);
            DrawPointerType(fieldAddress, fieldType, fieldNodeOptions with
            {
                Title = (*(FFXIVClientStructs.FFXIV.Common.Math.Vector4*)fieldAddress).ToString()
            });
            return;
        }

        // Math.Size
        if (fieldType == typeof(FFXIVClientStructs.FFXIV.Common.Math.Size))
        {
            DrawFieldName(fieldInfo);
            DrawPointerType(fieldAddress, fieldType, fieldNodeOptions with { Title = (*(FFXIVClientStructs.FFXIV.Common.Math.Size*)fieldAddress).ToString() });
            return;
        }

        // TODO: enum values table

        DrawFieldName(fieldInfo);
        DrawPointerType(fieldAddress, fieldType, fieldNodeOptions);

        if (fieldType == typeof(AtkTexture))
        {
            DrawAtkTexture(fieldAddress, fieldNodeOptions);
        }
    }

    private void DrawFieldName(FieldInfo fieldInfo, string? fieldNameOverride = null)
    {
        var name = fieldNameOverride ?? fieldInfo.Name;
        var fullName = (fieldInfo.DeclaringType != null ? fieldInfo.DeclaringType.FullName + "." : string.Empty) + fieldInfo.Name;
        var hasDoc = HasDocumentation(fullName);
        var startPos = ImCursor.ScreenPosition;

        ImGuiUtils.DrawCopyableText(name, new CopyableTextOptions() { NoTooltip = true, TextColor = fieldInfo.IsPrivate ? ColorFieldName with { A = 0.67f } : ColorFieldName });

        if (hasDoc)
        {
            var textSize = ImGui.CalcTextSize(name);
            ImGui.GetWindowDrawList().AddLine(startPos + new Vector2(0, textSize.Y), startPos + textSize, ColorFieldName.ToUInt());
        }

        if (ImGui.IsItemHovered())
        {
            using var tooltip = ImRaii.Tooltip();
            ImGui.TextColored(ColorFieldName, name);

            if (hasDoc)
            {
                using var font = _pluginInterface.UiBuilder.MonoFontHandle.Push();
                var doc = GetDocumentation(fullName);
                if (doc != null)
                {
                    ImGui.Separator();

                    if (!string.IsNullOrEmpty(doc.Sumamry))
                        ImGui.Text(doc.Sumamry);

                    if (!string.IsNullOrEmpty(doc.Remarks))
                        ImGui.Text(doc.Remarks);

                    if (doc.Parameters.Length > 0)
                    {
                        foreach (var param in doc.Parameters)
                        {
                            ImGui.Text($"{param.Key}: {param.Value}");
                        }
                    }

                    if (!string.IsNullOrEmpty(doc.Returns))
                        ImGui.Text(doc.Returns);
                }
            }
        }

        ImGui.SameLine();
    }
}
