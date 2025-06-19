using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Dalamud.Game;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Group;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.STD;
using HaselCommon.Extensions;
using HaselCommon.Graphics;
using HaselCommon.Services;
using HaselDebug.Utils;
using ImGuiNET;
using InteropGenerator.Runtime;
using InteropGenerator.Runtime.Attributes;
using Lumina.Text.ReadOnly;
using Microsoft.Extensions.Logging;
using EventHandler = FFXIVClientStructs.FFXIV.Client.Game.Event.EventHandler;
using KernelTexture = FFXIVClientStructs.FFXIV.Client.Graphics.Kernel.Texture;

namespace HaselDebug.Services;

[RegisterSingleton, AutoConstruct]
public unsafe partial class DebugRenderer
{
    public static Color ColorModifier { get; } = new(0.5f, 0.5f, 0.75f, 1);
    public static Color ColorType { get; } = new(0.2f, 0.9f, 0.9f, 1);
    public static Color ColorFieldName { get; } = new(0.2f, 0.9f, 0.4f, 1);
    public static Color ColorTreeNode { get; } = new(1, 1, 0, 1);
    public static Color ColorObsolete { get; } = new(1, 1, 0, 1);
    public static Color ColorObsoleteError { get; } = new(1, 0, 0, 1);

    private readonly Dictionary<Type, string[]> _knownStringPointers = new() {
        { typeof(FFXIVClientStructs.FFXIV.Client.UI.Agent.MapMarkerBase), ["Subtext"] },
        { typeof(FFXIVClientStructs.FFXIV.Common.Component.Excel.ExcelSheet), ["SheetName"] },
        { typeof(WorldHelper.World), ["Name"] },
        { typeof(AtkTextNode), ["OriginalTextPointer"] }
    };

    private readonly ILogger<DebugRenderer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly WindowManager _windowManager;
    private readonly ITextureProvider _textureProvider;
    private readonly ImGuiContextMenuService _imGuiContextMenu;
    private readonly SeStringEvaluator _seStringEvaluator;
    private readonly TextService _textService;
    private readonly TextureService _textureService;
    private readonly IDataManager _dataManager;
    private readonly IGameGui _gameGui;
    private readonly LanguageProvider _languageProvider;

    public ImmutableSortedDictionary<string, Type> AddonTypes { get; private set; }
    public ImmutableSortedDictionary<AgentId, Type> AgentTypes { get; private set; }

    [AutoPostConstruct]
    public void Initialize()
    {
        var csAssembly = typeof(AddonAttribute).Assembly;

        AddonTypes = csAssembly.GetTypes()
            .Where(type => type.GetCustomAttribute<AddonAttribute>() != null)
            .SelectMany(type => type.GetCustomAttribute<AddonAttribute>()!.AddonIdentifiers, (type, addonName) => (type, addonName))
            .ToImmutableSortedDictionary(
                tuple => tuple.addonName,
                tuple => tuple.type);

        AgentTypes = csAssembly.GetTypes()
            .Where(type => type.GetCustomAttribute<AgentAttribute>() != null)
            .Select(type => (type, agentId: type.GetCustomAttribute<AgentAttribute>()!.Id))
            .ToImmutableSortedDictionary(
                tuple => tuple.agentId,
                tuple => tuple.type);
    }

    public void DrawPointerType(void* obj, Type? type, NodeOptions nodeOptions)
        => DrawPointerType((nint)obj, type, nodeOptions);

    public void DrawPointerType(nint address, Type? type, NodeOptions nodeOptions)
    {
        if (type == null)
        {
            ImGui.TextUnformatted("");
            return;
        }

        if (address == 0 || address < 0x140000000)
        {
            ImGui.TextUnformatted("null");
            return;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Pointer<>))
        {
            address = *(nint*)address;
            type = type.GenericTypeArguments[0];
        }

        if (type == null)
        {
            ImGui.TextUnformatted("");
            return;
        }

        if (address == 0 || address < 0x140000000)
        {
            ImGui.TextUnformatted("null");
            return;
        }

        nodeOptions = nodeOptions.WithAddress(address) with
        {
            HighlightAddress = address,
            HighlightType = type,
        };

        if (type.IsVoid())
        {
            ImGui.TextUnformatted($"0x{address:X}"); // TODO: what did I do here?
            return;
        }

        if (!nodeOptions.ResolvedInheritedTypeAddresses.Path.Contains(address))
        {
            if (Inherits<ILayoutInstance>(type))
            {
                switch (((ILayoutInstance*)address)->Id.Type)
                {
                    case InstanceType.SharedGroup:
                        type = typeof(SharedGroupLayoutInstance);
                        break;
                }
            }
            else if (Inherits<GameObject>(type))
            {
                switch (((GameObject*)address)->ObjectKind)
                {
                    case ObjectKind.Pc:
                    case ObjectKind.BattleNpc:
                        type = typeof(FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara);
                        break;
                    case ObjectKind.EventNpc:
                        type = typeof(FFXIVClientStructs.FFXIV.Client.Game.Character.Character);
                        break;
                    case ObjectKind.Treasure:
                        type = typeof(FFXIVClientStructs.FFXIV.Client.Game.Object.Treasure);
                        break;
                    case ObjectKind.Aetheryte:
                        type = typeof(FFXIVClientStructs.FFXIV.Client.Game.Object.Aetheryte);
                        break;
                    case ObjectKind.GatheringPoint:
                        type = typeof(FFXIVClientStructs.FFXIV.Client.Game.Object.GatheringPointObject);
                        break;
                    case ObjectKind.EventObj:
                        type = typeof(FFXIVClientStructs.FFXIV.Client.Game.Object.EventObject);
                        break;
                    case ObjectKind.Companion:
                        type = typeof(FFXIVClientStructs.FFXIV.Client.Game.Character.Companion);
                        break;
                    case ObjectKind.HousingEventObject:
                        type = typeof(FFXIVClientStructs.FFXIV.Client.Game.Object.HousingObject);
                        break;
                    case ObjectKind.MjiObject:
                        type = typeof(FFXIVClientStructs.FFXIV.Client.Game.Object.MJIObject);
                        break;
                    case ObjectKind.Ornament:
                        type = typeof(FFXIVClientStructs.FFXIV.Client.Game.Character.Ornament);
                        break;
                }
            }
            else if (Inherits<DrawObject>(type))
            {
                switch (((DrawObject*)address)->GetObjectType())
                {
                    case ObjectType.CharacterBase:
                        type = typeof(CharacterBase);
                        switch (((CharacterBase*)address)->GetModelType())
                        {
                            case CharacterBase.ModelType.Human:
                                type = typeof(Human);
                                break;
                            case CharacterBase.ModelType.DemiHuman:
                                type = typeof(Demihuman);
                                break;
                            case CharacterBase.ModelType.Monster:
                                type = typeof(Monster);
                                break;
                            case CharacterBase.ModelType.Weapon:
                                type = typeof(Weapon);
                                break;
                        }
                        break;
                }
            }
            else if (Inherits<EventHandler>(type))
            {
                var eventId = ((EventHandler*)address)->Info.EventId;
                string? additionalName = null;

                switch (eventId.ContentId)
                {
                    case EventHandlerContent.Quest:
                        type = typeof(QuestEventHandler);
                        additionalName = _textService.GetQuestName(eventId.Id);
                        break;

                    case EventHandlerContent.GatheringPoint:
                        type = typeof(GatheringPointEventHandler);
                        break;

                    case EventHandlerContent.Shop:
                        type = typeof(ShopEventHandler);
                        additionalName = new ReadOnlySeStringSpan(((ShopEventHandler*)address)->ShopName.AsSpan()).ExtractText();
                        break;

                    case EventHandlerContent.Aetheryte:
                        type = typeof(AetheryteEventHandler);
                        break;

                    case EventHandlerContent.Craft:
                        type = typeof(CraftEventHandler);
                        break;

                    case EventHandlerContent.CustomTalk:
                        type = typeof(CustomTalkEventHandler);
                        additionalName = new ReadOnlySeStringSpan(((LuaEventHandler*)address)->LuaClass.AsSpan()).ExtractText();
                        break;

                    case EventHandlerContent.InstanceContentDirector:
                        type = ((InstanceContentDirector*)address)->InstanceContentType switch
                        {
                            InstanceContentType.DeepDungeon => typeof(InstanceContentDeepDungeon),
                            InstanceContentType.OceanFishing => typeof(InstanceContentOceanFishing),
                            _ => typeof(InstanceContentDirector)
                        };
                        additionalName = ((InstanceContentDirector*)address)->InstanceContentType.ToString();
                        break;

                    case EventHandlerContent.PublicContentDirector:
                        type = ((PublicContentDirector*)address)->Type switch
                        {
                            PublicContentDirectorType.Bozja => typeof(PublicContentBozja),
                            PublicContentDirectorType.Eureka => typeof(PublicContentEureka),
                            PublicContentDirectorType.OccultCrescent => typeof(PublicContentOccultCrescent),
                            _ => typeof(PublicContentDirector)
                        };
                        additionalName = ((PublicContentDirector*)address)->Type.ToString();
                        break;

                    case EventHandlerContent.GoldSaucerDirector:
                        type = typeof(GoldSaucerDirector);
                        break;
                }

                if (nodeOptions.UseSimpleEventHandlerName && string.IsNullOrEmpty(nodeOptions.Title))
                {
                    nodeOptions = nodeOptions with
                    {
                        UseSimpleEventHandlerName = false,
                        Title = string.IsNullOrEmpty(additionalName)
                        ? $"{eventId.ContentId} {eventId.Id}"
                        : $"{eventId.ContentId} {eventId.Id} ({additionalName})"
                    };
                }
            }
            else if (Inherits<AtkResNode>(type))
            {
                switch (((AtkResNode*)address)->GetNodeType())
                {
                    case NodeType.Image:
                        type = typeof(AtkImageNode);
                        break;
                    case NodeType.Text:
                        type = typeof(AtkTextNode);
                        break;
                    case NodeType.NineGrid:
                        type = typeof(AtkNineGridNode);
                        break;
                    case NodeType.Counter:
                        type = typeof(AtkCounterNode);
                        break;
                    case NodeType.Collision:
                        type = typeof(AtkCollisionNode);
                        break;
                    case NodeType.ClippingMask:
                        type = typeof(AtkClippingMaskNode);
                        break;
                    case NodeType.Component:
                        type = typeof(AtkComponentNode);
                        break;
                }
            }
            else if (Inherits<AtkComponentBase>(type))
            {
                var compBase = (AtkComponentBase*)address;
                if (compBase->UldManager.ResourceFlags.HasFlag(AtkUldManagerResourceFlag.Initialized) &&
                    compBase->UldManager.BaseType == AtkUldManagerBaseType.Component)
                {
                    switch (((AtkUldComponentInfo*)compBase->UldManager.Objects)->ComponentType)
                    {
                        case ComponentType.Base:
                            type = typeof(AtkComponentBase);
                            break;
                        case ComponentType.Button:
                            type = typeof(AtkComponentButton);
                            break;
                        case ComponentType.Window:
                            type = typeof(AtkComponentWindow);
                            break;
                        case ComponentType.CheckBox:
                            type = typeof(AtkComponentCheckBox);
                            break;
                        case ComponentType.RadioButton:
                            type = typeof(AtkComponentRadioButton);
                            break;
                        case ComponentType.GaugeBar:
                            type = typeof(AtkComponentGaugeBar);
                            break;
                        case ComponentType.Slider:
                            type = typeof(AtkComponentSlider);
                            break;
                        case ComponentType.TextInput:
                            type = typeof(AtkComponentTextInput);
                            break;
                        case ComponentType.NumericInput:
                            type = typeof(AtkComponentNumericInput);
                            break;
                        case ComponentType.List:
                            type = typeof(AtkComponentList);
                            break;
                        case ComponentType.DropDownList:
                            type = typeof(AtkComponentDropDownList);
                            break;
                        case ComponentType.Tab:
                            type = typeof(AtkComponentTab);
                            break;
                        case ComponentType.TreeList:
                            type = typeof(AtkComponentTreeList);
                            break;
                        case ComponentType.ScrollBar:
                            type = typeof(AtkComponentScrollBar);
                            break;
                        case ComponentType.ListItemRenderer:
                            type = typeof(AtkComponentListItemRenderer);
                            break;
                        case ComponentType.Icon:
                            type = typeof(AtkComponentIcon);
                            break;
                        case ComponentType.IconText:
                            type = typeof(AtkComponentIconText);
                            break;
                        case ComponentType.DragDrop:
                            type = typeof(AtkComponentDragDrop);
                            break;
                        case ComponentType.GuildLeveCard:
                            type = typeof(AtkComponentGuildLeveCard);
                            break;
                        case ComponentType.TextNineGrid:
                            type = typeof(AtkComponentTextNineGrid);
                            break;
                        case ComponentType.JournalCanvas:
                            type = typeof(AtkComponentJournalCanvas);
                            break;
                        case ComponentType.Multipurpose:
                            type = typeof(AtkComponentMultipurpose);
                            break;
                        case ComponentType.Map:
                            type = typeof(AtkComponentMap);
                            break;
                        case ComponentType.Preview:
                            type = typeof(AtkComponentPreview);
                            break;
                        case ComponentType.HoldButton:
                            type = typeof(AtkComponentHoldButton);
                            break;
                        case ComponentType.Portrait:
                            type = typeof(AtkComponentPortrait);
                            break;
                    }
                }
            }

            nodeOptions = nodeOptions with
            {
                ResolvedInheritedTypeAddresses = nodeOptions.ResolvedInheritedTypeAddresses.With(address)
            };
        }

        if (type.IsPointer)
        {
            type = type.GetElementType();
            address = *(nint*)address;
            DrawPointerType(address, type, nodeOptions);
            return;
        }
        else if (type == typeof(bool))
        {
            ImGui.TextUnformatted($"{*(bool*)address}");
            return;
        }
        else if (type == typeof(BitVector32))
        {
            ImGui.TextUnformatted($"{*(BitVector32*)address}");
            return;
        }
        else if (type == typeof(Utf8String))
        {
            DrawUtf8String(address, nodeOptions);
            return;
        }
        else if (type == typeof(KernelTexture))
        {
            DrawTexture(address, nodeOptions);
            return;
        }
        else if (type == typeof(AtkTexture))
        {
            DrawAtkTexture(address, nodeOptions);
            return;
        }
        else if (type == typeof(AtkValue))
        {
            DrawAtkValue(address, nodeOptions);
            return;
        }
        else if (type == typeof(CStringPointer))
        {
            DrawSeString(*(byte**)address, nodeOptions);
            return;
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(StdVector<>))
        {
            DrawStdVector(address, type.GenericTypeArguments[0], nodeOptions);
            return;
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(StdMap<,>))
        {
            DrawStdMap(address, type.GenericTypeArguments[0], type.GenericTypeArguments[1], nodeOptions);
            return;
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(StdSet<>))
        {
            DrawStdSet(address, type.GenericTypeArguments[0], nodeOptions);
            return;
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(StdList<>))
        {
            DrawStdList(address, type.GenericTypeArguments[0], nodeOptions);
            return;
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(StdDeque<>))
        {
            DrawStdDeque(address, type.GenericTypeArguments[0], nodeOptions);
            return;
        }
        else if (type.IsEnum)
        {
            DrawEnum(address, type, nodeOptions);
            return;
        }
        else if (type.IsNumericType())
        {
            DrawNumeric(address, type, nodeOptions);
            return;
        }
        else if (type.IsStruct() || type.IsClass)
        {
            DrawStruct(address, type, nodeOptions);
            return;
        }

        ImGui.TextUnformatted("Unsupported Type");
    }

    public ImRaii.IEndObject DrawTreeNode(NodeOptions nodeOptions)
    {
        using var titleColor = ImRaii.PushColor(ImGuiCol.Text, (nodeOptions.TitleColor ?? ColorTreeNode).ToUInt());
        var previewText = string.Empty;

        if (!nodeOptions.DrawSeStringTreeNode && nodeOptions.SeStringTitle != null)
            previewText = nodeOptions.SeStringTitle?.ToString();
        else if (nodeOptions.Title != null)
            previewText = nodeOptions.Title;

        var node = ImRaii.TreeNode(previewText + nodeOptions.GetKey("Node"), nodeOptions.GetTreeNodeFlags());
        titleColor?.Dispose();

        if (ImGui.IsItemHovered())
        {
            nodeOptions.OnHovered?.Invoke();

            if (nodeOptions.HighlightType != null && nodeOptions.HighlightAddress != 0)
            {
                var highlightType = nodeOptions.HighlightType;
                var highlightAddress = nodeOptions.HighlightAddress;

                if (highlightType.IsGenericType && highlightType.GetGenericTypeDefinition() == typeof(Pointer<>))
                {
                    highlightType = highlightType.GenericTypeArguments[0];
                    highlightAddress = *(nint*)highlightAddress;
                }

                if (highlightType.IsPointer)
                {
                    highlightType = highlightType.GetElementType()!;
                    highlightAddress = *(nint*)highlightAddress;
                }

                if (Inherits<ILayoutInstance>(highlightType))
                {
                    var inst = (ILayoutInstance*)highlightAddress;
                    if (inst != null)
                    {
                        var transform = inst->GetTransformImpl();
                        if (transform != null)
                            DrawLine(transform->Translation);
                    }
                }
                else if (Inherits<GameObject>(highlightType))
                {
                    var gameObject = (GameObject*)highlightAddress;
                    if (gameObject != null)
                        DrawLine(gameObject->Position);
                }
                else if (Inherits<AtkUnitBase>(highlightType))
                {
                    var unitBase = (AtkUnitBase*)highlightAddress;
                    if (unitBase->WindowNode != null)
                        HighlightNode((AtkResNode*)unitBase->WindowNode);
                    else if (unitBase->RootNode != null)
                        HighlightNode(unitBase->RootNode);
                }
                else if (Inherits<AtkResNode>(highlightType))
                {
                    HighlightNode((AtkResNode*)highlightAddress);
                }
                else if (Inherits<AtkComponentBase>(highlightType))
                {
                    var component = (AtkComponentBase*)highlightAddress;
                    if (component != null && component->AtkResNode != null)
                        HighlightNode(component->AtkResNode);
                }

                void DrawLine(Vector3 pos)
                {
                    if (_gameGui.WorldToScreen(pos, out var screenPos))
                    {
                        ImGui.GetForegroundDrawList().AddLine(ImGui.GetMousePos(), screenPos, Color.Orange.ToUInt());
                        ImGui.GetForegroundDrawList().AddCircleFilled(screenPos, 3f, Color.Orange.ToUInt());
                    }
                }
            }
        }

        if (nodeOptions.DrawContextMenu != null)
            _imGuiContextMenu.Draw(nodeOptions.GetKey("ContextMenu"), builder => nodeOptions.DrawContextMenu(nodeOptions, builder));

        if (nodeOptions.DrawSeStringTreeNode && nodeOptions.SeStringTitle != null)
        {
            ImGui.SameLine();

            using (ImRaii.PushColor(ImGuiCol.Text, (nodeOptions.TitleColor ?? ColorTreeNode).ToUInt()))
            {
                ImGuiHelpers.SeStringWrapped(nodeOptions.SeStringTitle.Value.AsSpan(), new()
                {
                    ForceEdgeColor = true,
                    WrapWidth = 9999
                });
            }
        }

        return node;
    }

    private void DrawStruct(nint address, Type type, NodeOptions nodeOptions)
    {
        nodeOptions = nodeOptions.WithAddress(address);

        var fields = type
            .GetFields(BindingFlags.Default | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(fieldInfo => !fieldInfo.IsLiteral) // no constants
            .Where(fieldInfo => !fieldInfo.IsStatic);

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
            DrawCopyableText($"[0x{offset:X}]", ImGui.IsKeyDown(ImGuiKey.LeftShift) ? $"0x{offset:X}" : $"{address + offset:X}", textColor: Color.Grey3);
            ImGui.SameLine();

            var fieldNodeOptions = nodeOptions.WithAddress(i);

            var fieldAddress = address + offset;
            var fieldType = fieldInfo.FieldType;

            if (fieldType == typeof(uint) && fieldInfo.Name.Contains("IconId"))
                fieldNodeOptions = fieldNodeOptions with { IsIconIdField = true };

            if ((fieldType == typeof(int) || fieldType == typeof(long)) && fieldInfo.Name.Contains("Timestamp"))
                fieldNodeOptions = fieldNodeOptions with { IsTimestampField = true };

            if (fieldInfo.GetCustomAttribute<ObsoleteAttribute>() is ObsoleteAttribute obsoleteAttribute)
            {
                using (ImRaii.PushColor(ImGuiCol.Text, (obsoleteAttribute.IsError ? ColorObsoleteError : ColorObsolete).ToUInt()))
                    ImGui.TextUnformatted("[Obsolete]");

                if (!string.IsNullOrEmpty(obsoleteAttribute.Message) && ImGui.IsItemHovered())
                    ImGui.SetTooltip(obsoleteAttribute.Message);

                ImGui.SameLine();
            }

            if (fieldInfo.IsStatic)
            {
                ImGui.TextUnformatted("static");
                ImGui.SameLine();
            }

            DrawCopyableText(fieldType.ReadableTypeName(), fieldType.ReadableTypeName(ImGui.IsKeyDown(ImGuiKey.LeftShift)), textColor: ColorType);
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

            // AtkUnitBase.AtkValues
            if (Inherits<AtkUnitBase>(type) && fieldType == typeof(AtkValue*) && fieldInfo.Name == "AtkValues")
            {
                DrawFieldName(fieldInfo);
                DrawAtkValues(*(AtkValue**)fieldAddress, ((AtkUnitBase*)address)->AtkValuesCount, fieldNodeOptions);
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
        }
    }

    private void DrawFieldName(FieldInfo fieldInfo, string? fieldNameOverride = null)
    {
        var fullName = (fieldInfo.DeclaringType != null ? fieldInfo.DeclaringType.FullName + "." : string.Empty) + fieldInfo.Name;
        var hasDoc = HasDocumentation(fullName);

        using (ImRaii.PushColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32((Vector4)ColorFieldName)))
        {
            var startPos = ImGui.GetWindowPos() + ImGui.GetCursorPos() - new Vector2(ImGui.GetScrollX(), ImGui.GetScrollY());

            ImGui.TextUnformatted(fieldNameOverride ?? fieldInfo.Name);

            if (hasDoc)
            {
                var textSize = ImGui.CalcTextSize(fieldNameOverride ?? fieldInfo.Name);
                ImGui.GetWindowDrawList().AddLine(startPos + new Vector2(0, textSize.Y), startPos + textSize, ColorFieldName.ToUInt());
            }
        }

        if (ImGui.IsItemHovered())
        {
            using var tooltip = ImRaii.Tooltip();
            ImGui.TextUnformatted(fieldNameOverride ?? fieldInfo.Name);

            if (hasDoc)
            {
                using var font = _pluginInterface.UiBuilder.MonoFontHandle.Push();
                var doc = GetDocumentation(fullName);
                if (doc != null)
                {
                    ImGui.Separator();

                    if (!string.IsNullOrEmpty(doc.Sumamry))
                        ImGui.TextUnformatted(doc.Sumamry);

                    if (!string.IsNullOrEmpty(doc.Remarks))
                        ImGui.TextUnformatted(doc.Remarks);

                    if (doc.Parameters.Length > 0)
                    {
                        foreach (var param in doc.Parameters)
                        {
                            ImGui.TextUnformatted($"{param.Key}: {param.Value}");
                        }
                    }

                    if (!string.IsNullOrEmpty(doc.Returns))
                        ImGui.TextUnformatted(doc.Returns);
                }
            }
        }

        if (ImGui.IsItemClicked())
        {
            ImGui.SetClipboardText(fieldNameOverride ?? fieldInfo.Name);
        }

        ImGui.SameLine();
    }

    private void DrawEnum(nint address, Type type, NodeOptions nodeOptions)
    {
        nodeOptions = nodeOptions.WithAddress(address);

        var underlyingType = type.GetEnumUnderlyingType();
        var value = DrawNumeric(address, underlyingType, nodeOptions);
        if (value == null)
            return;

        if (type.GetCustomAttribute<FlagsAttribute>() != null)
        {
            ImGui.SameLine();
            ImGui.TextUnformatted(" - ");
            var bits = Marshal.SizeOf(underlyingType) * 8;
            for (var i = 0u; i < bits; i++)
            {
                var bitValue = 1u << (int)i;
                if ((Convert.ToUInt64(value) & bitValue) != 0)
                {
                    ImGui.SameLine();
                    DrawCopyableText(Enum.GetName(type, bitValue)?.ToString() ?? $"{bitValue}", $"{bitValue}");
                }
            }
        }
        else
        {
            ImGui.SameLine();
            ImGui.TextUnformatted(Enum.GetName(type, value)?.ToString() ?? "");
        }
    }

    public void DrawCopyableText(string text, string? textCopy = null, string? tooltipText = null, bool asSelectable = false, Color? textColor = null, string? highligtedText = null, bool noTooltip = false)
    {
        textCopy ??= text;

        using var color = textColor?.Push(ImGuiCol.Text);

        if (asSelectable)
        {
            ImGui.Selectable(text);
        }
        else if (!string.IsNullOrEmpty(highligtedText))
        {
            var pos = text.IndexOf(highligtedText, StringComparison.InvariantCultureIgnoreCase);
            if (pos != -1)
            {
                ImGui.TextUnformatted(text[..pos]);
                ImGui.SameLine(0, 0);

                using (Color.Yellow.Push(ImGuiCol.Text))
                    ImGui.TextUnformatted(text[pos..(pos + highligtedText.Length)]);

                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted(text[(pos + highligtedText.Length)..]);
            }
            else
            {
                ImGui.TextUnformatted(text);
            }
        }
        else
        {
            ImGui.TextUnformatted(text);
        }

        color?.Pop();

        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            if (!noTooltip)
                ImGui.SetTooltip(tooltipText ?? textCopy);
        }

        if (ImGui.IsItemClicked())
            ImGui.SetClipboardText(textCopy);
    }

    public void DrawAddress(void* obj)
        => DrawAddress((nint)obj);

    public void DrawAddress(nint address)
    {
        if (address == 0)
        {
            ImGui.TextUnformatted("");
            return;
        }

        var sigScanner = Service.Get<ISigScanner>();

        if (address > sigScanner.Module.BaseAddress && !ImGui.IsKeyDown(ImGuiKey.LeftShift))
        {
            DrawCopyableText($"+0x{address - sigScanner.Module.BaseAddress:X}");
            return;
        }
        else
        {
            DrawCopyableText($"0x{address:X}");
        }
    }

    public object? DrawNumeric(nint address, Type type, NodeOptions nodeOptions)
    {
        object? value = null;

        switch (type)
        {
            case Type t when t == typeof(nint):
                value = *(nint*)address;
                break;

            case Type t when t == typeof(Half):
                value = *(Half*)address;
                break;

            case Type t when t == typeof(byte):
                value = *(byte*)address;
                break;

            case Type t when t == typeof(sbyte):
                value = *(sbyte*)address;
                break;

            case Type t when t == typeof(short):
                value = *(short*)address;
                break;

            case Type t when t == typeof(ushort):
                value = *(ushort*)address;
                break;

            case Type t when t == typeof(int):
                value = *(int*)address;
                break;

            case Type t when t == typeof(uint):
                value = *(uint*)address;
                break;

            case Type t when t == typeof(long):
                value = *(long*)address;
                break;

            case Type t when t == typeof(ulong):
                value = *(ulong*)address;
                break;

            case Type t when t == typeof(decimal):
                value = *(decimal*)address;
                break;

            case Type t when t == typeof(double):
                value = *(double*)address;
                break;

            case Type t when t == typeof(float):
                value = *(float*)address;
                break;

            default:
                ImGui.TextUnformatted("null");
                return value;
        }

        DrawNumeric(value, type, nodeOptions);

        return value;
    }

    public void DrawNumeric(object value, Type type, NodeOptions nodeOptions)
    {
        if (type == typeof(nint))
        {
            DrawAddress((nint)value);
            return;
        }

        if (type == typeof(Half) || type == typeof(decimal) || type == typeof(double) || type == typeof(float))
        {
            DrawCopyableText(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
            return;
        }

        if (type == typeof(byte) || type == typeof(sbyte) ||
            type == typeof(short) || type == typeof(ushort) ||
            type == typeof(int) || type == typeof(uint) ||
            type == typeof(long) || type == typeof(ulong))
        {
            DrawNumericWithHex(value, type, nodeOptions);
            return;
        }

        ImGui.TextUnformatted($"Unhandled NumericType {type.FullName}");
    }

    private void DrawNumericWithHex(object value, Type type, NodeOptions nodeOptions)
    {
        if (nodeOptions.IsIconIdField)
        {
            DrawIcon(value, type);
        }

        if (nodeOptions.IsTimestampField)
        {
            DrawCopyableText(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);

            switch (value)
            {
                case int intTime when intTime != 0:
                    ImGui.SameLine();
                    DrawCopyableText(DateTimeOffset.FromUnixTimeSeconds(intTime).ToLocalTime().ToString());
                    break;

                case long longTime when longTime != 0:
                    ImGui.SameLine();
                    DrawCopyableText(DateTimeOffset.FromUnixTimeSeconds(longTime).ToLocalTime().ToString());
                    break;
            }

            return;
        }
        else if (nodeOptions.HexOnShift)
        {
            if (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift))
            {
                DrawCopyableText(ToHexString(value, type));
            }
            else
            {
                DrawCopyableText(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
            }

            return;
        }

        DrawCopyableText(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
        ImGui.SameLine();
        DrawCopyableText(ToHexString(value, type));
    }

    private static string ToHexString(object value, Type type)
    {
        return type switch
        {
            _ when type == typeof(byte) => $"0x{(byte)value:X}",
            _ when type == typeof(sbyte) => $"0x{(sbyte)value:X}",
            _ when type == typeof(short) => $"0x{(short)value:X}",
            _ when type == typeof(ushort) => $"0x{(ushort)value:X}",
            _ when type == typeof(int) => $"0x{(int)value:X}",
            _ when type == typeof(uint) => $"0x{(uint)value:X}",
            _ when type == typeof(long) => $"0x{(long)value:X}",
            _ when type == typeof(ulong) => $"0x{(ulong)value:X}",
            _ => throw new InvalidOperationException($"Unsupported type {type.FullName}")
        };
    }

    public string ToBitsString(ulong value, int bits)
    {
        var bitsString = new StringBuilder();

        for (var i = bits - 1; i >= 0; i--)
        {
            if ((i + 1) % 4 == 0)
                bitsString.Append(' ');
            bitsString.Append(Convert.ToString(value / (ulong)(1 << i) % 2));
        }

        return bitsString.ToString();
    }

    public string ToBitsString(byte byteIn)
        => ToBitsString(byteIn, 8);

    public string ToBitsString(ushort byteIn)
        => ToBitsString(byteIn, 16);

    public string ToBitsString(uint byteIn)
        => ToBitsString(byteIn, 32);

    public void DrawIcon(object value, Type? type = null, bool isHq = false, bool sameLine = true, DrawInfo drawInfo = default, bool canCopy = true, bool noTooltip = false)
    {
        if (value == null)
        {
            DrawIcon(0, isHq, sameLine, drawInfo, canCopy, noTooltip);
            return;
        }

        var iconId = (type ?? value.GetType()) switch
        {
            Type t when t == typeof(short) => (short)value > 0 ? (uint)(short)value : 0u,
            Type t when t == typeof(ushort) => (ushort)value,
            Type t when t == typeof(int) => (int)value > 0 ? (uint)(int)value : 0u,
            Type t when t == typeof(uint) => (uint)value,
            _ => 0u
        };

        DrawIcon(iconId, isHq, sameLine, drawInfo, canCopy, noTooltip);
    }

    public void DrawIcon(uint iconId, bool isHq = false, bool sameLine = true, DrawInfo drawInfo = default, bool canCopy = true, bool noTooltip = false)
    {
        drawInfo.DrawSize ??= new Vector2(ImGui.GetTextLineHeight());

        if (iconId == 0)
        {
            ImGui.Dummy(drawInfo.DrawSize.Value);
            if (sameLine)
                ImGui.SameLine();
            return;
        }

        if (!ImGui.IsRectVisible(drawInfo.DrawSize.Value))
        {
            ImGui.Dummy(drawInfo.DrawSize.Value);
            if (sameLine)
                ImGui.SameLine();
            return;
        }

        if (_textureProvider.TryGetFromGameIcon(new GameIconLookup(iconId, isHq), out var tex) && tex.TryGetWrap(out var texture, out _))
        {
            ImGui.Image(texture.ImGuiHandle, drawInfo.DrawSize.Value);

            if (ImGui.IsItemHovered())
            {
                if (canCopy)
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                if (!noTooltip)
                {
                    ImGui.BeginTooltip();
                    if (canCopy)
                        ImGui.TextUnformatted("Click to copy IconId");
                    ImGui.TextUnformatted($"ID: {iconId} – Size: {texture.Width}x{texture.Height}");
                    ImGui.Image(texture.ImGuiHandle, new(texture.Width, texture.Height));
                    ImGui.EndTooltip();
                }
            }

            if (canCopy && ImGui.IsItemClicked())
                ImGui.SetClipboardText(iconId.ToString());
        }
        else
        {
            ImGui.Dummy(drawInfo.DrawSize.Value);
        }

        if (sameLine)
            ImGui.SameLine();
    }
}
