using System.Buffers.Binary;
using System.Reflection;
using System.Runtime.CompilerServices;
using Dalamud.Memory;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.STD;
using HaselDebug.Utils;
using static FFXIVClientStructs.FFXIV.Component.GUI.AtkUldManager;

namespace HaselDebug.Services;

public unsafe partial class DebugRenderer
{
    private void DrawStruct(nint address, Type type, NodeOptions nodeOptions)
    {
        nodeOptions = nodeOptions.WithAddress(address);

        var fields = GetAllInheritedFields(type);

        using var disabled = ImRaii.Disabled(!fields.Any());
        using var node = DrawTreeNode(nodeOptions.WithSeStringTitleIfNull(type.FullName ?? "Unknown Type Name"));
        if (!node) return;

        var processedFields = fields
            .OrderBy(fieldInfo => fieldInfo.GetFieldOffset())
            .Select(fieldInfo => (
                Info: fieldInfo,
                Offset: fieldInfo.GetFieldOffset(),
                Size: fieldInfo.IsFixed() ? fieldInfo.GetFixedType().SizeOf() * fieldInfo.GetFixedSize() : fieldInfo.FieldType.SizeOf()));

        nodeOptions = nodeOptions.ConsumeTreeNodeOptions();

        var i = 0;
        foreach (var (fieldInfo, offset, size) in processedFields)
        {
            i++;

            var fieldAddress = address + offset;
            var fieldType = fieldInfo.FieldType;

            DrawBitFields(type, fieldAddress, offset, fieldType, fieldInfo);

            ImGuiUtils.DrawCopyableText($"[0x{offset:X}]", new()
            {
                CopyText = ImGui.IsKeyDown(ImGuiKey.LeftShift) ? $"{address + offset:X}" : $"0x{offset:X}",
                TextColor = Color.Grey3
            });

            ImGui.SameLine();

            var fieldNodeOptions = nodeOptions.WithAddress(i);

            if (fieldType == typeof(uint) && fieldInfo.Name.Contains("IconId"))
                fieldNodeOptions = fieldNodeOptions with { IsIconIdField = true };

            if ((fieldType == typeof(int) || fieldType == typeof(long)) && fieldInfo.Name.Contains("Timestamp"))
                fieldNodeOptions = fieldNodeOptions with { IsTimestampField = true };
            
            if ((fieldType == typeof(short) || fieldType == typeof(int) || fieldType == typeof(ushort) || fieldType == typeof(uint)) && fieldInfo.Name.Contains("WorldId"))
                fieldNodeOptions = fieldNodeOptions with { IsWorldIdField = true };

            if (fieldInfo.GetCustomAttribute<ObsoleteAttribute>() is ObsoleteAttribute obsoleteAttribute)
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
                continue;
            }

            // internal FixedSizeArrays
            if (fieldInfo.IsAssembly
                && fieldInfo.GetCustomAttribute<FixedSizeArrayAttribute>() is FixedSizeArrayAttribute fixedSizeArrayAttribute
                && fieldType.GetCustomAttribute<InlineArrayAttribute>() is InlineArrayAttribute inlineArrayAttribute)
            {
                DrawFieldName(fieldInfo, fieldInfo.Name[1..].FirstCharToUpper());
                DrawFixedSizeArray(fieldAddress, fieldType, fixedSizeArrayAttribute.IsString, fieldNodeOptions);
                continue;
            }

            // StdVector<>
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(StdVector<>))
            {
                var underlyingType = fieldType.GenericTypeArguments[0];
                var underlyingTypeSize = underlyingType.SizeOf();
                if (underlyingTypeSize == 0)
                {
                    ImGui.TextColored(Color.Red, $"Can't get size of {underlyingType.Name}");
                    continue;
                }

                DrawFieldName(fieldInfo);
                DrawStdVector(fieldAddress, underlyingType, fieldNodeOptions);
                continue;
            }

            // StdDeque<>
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(StdDeque<>))
            {
                var underlyingType = fieldType.GenericTypeArguments[0];
                var underlyingTypeSize = underlyingType.SizeOf();
                if (underlyingTypeSize == 0)
                {
                    ImGui.TextColored(Color.Red, $"Can't get size of {underlyingType.Name}");
                    continue;
                }

                DrawFieldName(fieldInfo);
                DrawStdDeque(fieldAddress, underlyingType, fieldNodeOptions);
                continue;
            }

            // StdList<>
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(StdList<>))
            {
                var underlyingType = fieldType.GenericTypeArguments[0];
                var underlyingTypeSize = underlyingType.SizeOf();
                if (underlyingTypeSize == 0)
                {
                    ImGui.TextColored(Color.Red, $"Can't get size of {underlyingType.Name}");
                    continue;
                }

                DrawFieldName(fieldInfo);
                DrawStdList(fieldAddress, underlyingType, fieldNodeOptions);
                continue;
            }

            // AgentInterface.AddonId
            if (Inherits<AgentInterface>(type) && fieldType == typeof(uint) && fieldInfo.Name == nameof(AgentInterface.AddonId))
            {
                DrawFieldName(fieldInfo);
                DrawPointerType(fieldAddress, fieldType, fieldNodeOptions);
                var unitBase = RaptureAtkUnitManager.Instance()->GetAddonById(*(ushort*)fieldAddress);
                if (unitBase != null)
                {
                    ImGui.SameLine();
                    _navigationService.DrawAddonLink(unitBase->Id, unitBase->NameString);
                }
                continue;
            }

            // AtkUnitBase.AtkValues
            if (Inherits<AtkUnitBase>(type) && fieldType == typeof(AtkValue*) && fieldInfo.Name == nameof(AtkUnitBase.AtkValues))
            {
                DrawFieldName(fieldInfo);
                DrawAtkValues(*(AtkValue**)fieldAddress, ((AtkUnitBase*)address)->AtkValuesCount, fieldNodeOptions);
                continue;
            }

            // AtkUldManager.Assets
            if (Inherits<AtkUldManager>(type) && fieldType == typeof(AtkUldAsset*) && fieldInfo.Name == nameof(AtkUldManager.Assets))
            {
                DrawFieldName(fieldInfo);
                DrawArray(new Span<AtkUldAsset>(*(nint**)fieldAddress, ((AtkUldManager*)address)->AssetCount), fieldNodeOptions);
                continue;
            }

            // AtkUldManager.PartsList
            if (Inherits<AtkUldManager>(type) && fieldType == typeof(AtkUldPartsList*) && fieldInfo.Name == nameof(AtkUldManager.PartsList))
            {
                DrawFieldName(fieldInfo);
                DrawArray(new Span<AtkUldPartsList>(*(nint**)fieldAddress, ((AtkUldManager*)address)->PartsListCount), fieldNodeOptions);
                continue;
            }

            // AtkUldManager.NodeList
            if (Inherits<AtkUldManager>(type) && fieldType == typeof(AtkResNode**) && fieldInfo.Name == nameof(AtkUldManager.NodeList))
            {
                DrawFieldName(fieldInfo);
                DrawArray(new Span<Pointer<AtkResNode>>(*(nint**)fieldAddress, ((AtkUldManager*)address)->NodeListCount), fieldNodeOptions);
                continue;
            }

            // AtkUldManager.Objects
            if (Inherits<AtkUldManager>(type) && fieldType == typeof(AtkUldObjectInfo*) && fieldInfo.Name == nameof(AtkUldManager.Objects))
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

                continue;
            }

            // AtkUldWidgetInfo.NodeList
            if (type == typeof(AtkUldWidgetInfo) && fieldType == typeof(AtkResNode**) && fieldInfo.Name == nameof(AtkUldWidgetInfo.NodeList))
            {
                DrawFieldName(fieldInfo);
                DrawArray(new Span<Pointer<AtkResNode>>(*(nint**)fieldAddress, ((AtkUldWidgetInfo*)address)->NodeCount), fieldNodeOptions);
                continue;
            }

            // DuplicateObjectList.NodeList
            if (type == typeof(DuplicateObjectList) && fieldType == typeof(AtkComponentNode*) && fieldInfo.Name == nameof(DuplicateObjectList.NodeList))
            {
                DrawFieldName(fieldInfo);
                DrawArray(new Span<AtkComponentNode>(*(nint**)fieldAddress, (int)((DuplicateObjectList*)address)->NodeCount), fieldNodeOptions);
                continue;
            }

            // AtkTimelineManager.Timelines
            if (type == typeof(AtkTimelineManager) && fieldType == typeof(AtkTimeline*) && fieldInfo.Name == nameof(AtkTimelineManager.Timelines))
            {
                DrawFieldName(fieldInfo);
                DrawArray(new Span<AtkTimeline>(*(nint**)fieldAddress, ((AtkTimelineManager*)address)->TimelineCount), fieldNodeOptions);
                continue;
            }

            // AtkTimelineManager.Animations
            if (type == typeof(AtkTimelineManager) && fieldType == typeof(AtkTimelineAnimation*) && fieldInfo.Name == nameof(AtkTimelineManager.Animations))
            {
                DrawFieldName(fieldInfo);
                DrawArray(new Span<AtkTimelineAnimation>(*(nint**)fieldAddress, (int)((AtkTimelineManager*)address)->AnimationCount), fieldNodeOptions);
                continue;
            }

            // AtkTimelineManager.LabelSets
            if (type == typeof(AtkTimelineManager) && fieldType == typeof(AtkTimelineLabelSet*) && fieldInfo.Name == nameof(AtkTimelineManager.LabelSets))
            {
                DrawFieldName(fieldInfo);
                DrawArray(new Span<AtkTimelineLabelSet>(*(nint**)fieldAddress, (int)((AtkTimelineManager*)address)->LabelSetCount), fieldNodeOptions);
                continue;
            }

            // AtkTimelineManager.KeyFrames
            if (type == typeof(AtkTimelineManager) && fieldType == typeof(AtkTimelineKeyFrame*) && fieldInfo.Name == nameof(AtkTimelineManager.KeyFrames))
            {
                DrawFieldName(fieldInfo);
                DrawArray(new Span<AtkTimelineKeyFrame>(*(nint**)fieldAddress, (int)((AtkTimelineManager*)address)->KeyFrameCount), fieldNodeOptions);
                continue;
            }

            // ByteColor.RGBA
            if (type == typeof(ByteColor) && fieldType == typeof(uint) && fieldInfo.Name == nameof(ByteColor.RGBA))
            {
                var color = *(ByteColor*)fieldAddress;

                DrawFieldName(fieldInfo);
                DrawNumeric(fieldAddress, fieldType, fieldNodeOptions);

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
                ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight()));
                ImGui.GetWindowDrawList().AddRectFilled(
                    ImGui.GetItemRectMin(),
                    ImGui.GetItemRectMax(),
                    color.RGBA,
                    3);
                continue;
            }

            // ResourceHandle.FileType
            if (Inherits<ResourceHandle>(type) && fieldType == typeof(uint) && fieldInfo.Name == nameof(ResourceHandle.FileType))
            {
                DrawFieldName(fieldInfo);
                DrawNumeric(fieldAddress, fieldType, fieldNodeOptions);
                ImGui.SameLine();
                var chars = MemoryHelper.ReadString(fieldAddress, 4).ToCharArray();
                Array.Reverse(chars);
                ImGuiUtils.DrawCopyableText(new string(chars));
                continue;
            }

            // InventoryItem.CrafterContentId
            if (Inherits<InventoryItem>(type) && fieldType == typeof(ulong) && fieldInfo.Name == nameof(InventoryItem.CrafterContentId))
            {
                DrawFieldName(fieldInfo);
                DrawNumeric(fieldAddress, fieldType, fieldNodeOptions);
                ImGui.SameLine();
                ImGuiUtils.DrawCopyableText(NameCache.Instance()->GetNameByContentId(*(ulong*)fieldAddress).ToString());
                continue;
            }

            // byte* that are strings
            if (fieldType.IsPointer && _knownStringPointers.TryGetValue(type, out var fieldNames) && fieldNames.Contains(fieldInfo.Name))
            {
                DrawFieldName(fieldInfo);
                DrawSeString(*(byte**)fieldAddress, fieldNodeOptions);
                continue;
            }

            // Vector2
            if (fieldType == typeof(Vector2))
            {
                DrawFieldName(fieldInfo);
                DrawPointerType(fieldAddress, fieldType, fieldNodeOptions with { Title = (*(Vector2*)fieldAddress).ToString() });
                continue;
            }
            if (fieldType == typeof(FFXIVClientStructs.FFXIV.Common.Math.Vector2))
            {
                DrawFieldName(fieldInfo);
                DrawPointerType(fieldAddress, fieldType, fieldNodeOptions with
                {
                    Title = (*(FFXIVClientStructs.FFXIV.Common.Math.Vector2*)fieldAddress).ToString()
                });
                continue;
            }

            // Vector3
            if (fieldType == typeof(Vector3))
            {
                DrawFieldName(fieldInfo);
                DrawPointerType(fieldAddress, fieldType, fieldNodeOptions with { Title = (*(Vector3*)fieldAddress).ToString() });
                continue;
            }
            if (fieldType == typeof(FFXIVClientStructs.FFXIV.Common.Math.Vector3))
            {
                DrawFieldName(fieldInfo);
                DrawPointerType(fieldAddress, fieldType, fieldNodeOptions with
                {
                    Title = (*(FFXIVClientStructs.FFXIV.Common.Math.Vector3*)fieldAddress).ToString()
                });
                continue;
            }

            // Vector4
            if (fieldType == typeof(Vector4))
            {
                DrawFieldName(fieldInfo);
                DrawPointerType(fieldAddress, fieldType, fieldNodeOptions with { Title = (*(Vector4*)fieldAddress).ToString() });
                continue;
            }
            if (fieldType == typeof(FFXIVClientStructs.FFXIV.Common.Math.Vector4))
            {
                DrawFieldName(fieldInfo);
                DrawPointerType(fieldAddress, fieldType, fieldNodeOptions with
                {
                    Title = (*(FFXIVClientStructs.FFXIV.Common.Math.Vector4*)fieldAddress).ToString()
                });
                continue;
            }

            // TODO: enum values table

            DrawFieldName(fieldInfo);
            DrawPointerType(fieldAddress, fieldType, fieldNodeOptions);

            if (fieldType == typeof(AtkTexture))
            {
                DrawAtkTexture(fieldAddress, fieldNodeOptions);
            }
        }
    }

    private void DrawFieldName(FieldInfo fieldInfo, string? fieldNameOverride = null)
    {
        var name = fieldNameOverride ?? fieldInfo.Name;
        var fullName = (fieldInfo.DeclaringType != null ? fieldInfo.DeclaringType.FullName + "." : string.Empty) + fieldInfo.Name;
        var hasDoc = HasDocumentation(fullName);
        var startPos = ImGui.GetCursorScreenPos();

        ImGuiUtils.DrawCopyableText(name, new CopyableTextOptions() { NoTooltip = true, TextColor = ColorFieldName });

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
