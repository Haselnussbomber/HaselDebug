using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Dalamud.Interface.Textures;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using FFXIVClientStructs.FFXIV.Client.Game.MassivePcContent;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Group;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.STD;
using HaselDebug.Config;
using HaselDebug.Extensions;
using HaselDebug.Service;
using HaselDebug.Services.Data;
using HaselDebug.Utils;
using static Dalamud.Utility.StringExtensions;
using static FFXIVClientStructs.FFXIV.Component.GUI.AtkUldManager;
using EventHandler = FFXIVClientStructs.FFXIV.Client.Game.Event.EventHandler;
using InstanceContentType = FFXIVClientStructs.FFXIV.Client.Game.InstanceContent.InstanceContentType;
using KernelTexture = FFXIVClientStructs.FFXIV.Client.Graphics.Kernel.Texture;

namespace HaselDebug.Services;

[RegisterSingleton, AutoConstruct]
public unsafe partial class DebugRenderer
{
    public static Color ColorModifier { get; } = new(0.5f, 0.5f, 0.75f, 1);
    public static Color ColorType { get; } = new(0.2f, 0.9f, 0.9f, 1);
    public static Color ColorBitField { get; } = new(1.0f, 0.6f, 0.2f, 1);
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
    private readonly ISeStringEvaluator _seStringEvaluator;
    private readonly TextService _textService;
    private readonly GfdService _gfdService;
    private readonly UldService _uldService;
    private readonly IDataManager _dataManager;
    private readonly ISigScanner _sigScanner;
    private readonly IGameGui _gameGui;
    private readonly LanguageProvider _languageProvider;
    private readonly AddonObserver _addonObserver;
    private readonly ExcelService _excelService;
    private readonly NavigationService _navigationService;
    private readonly DataYmlService _dataYml;
    private readonly ProcessInfoService _processInfoService;
    private readonly PluginConfig _pluginConfig;
    private readonly IAddonLifecycle _addonLifecycle;

    public void DrawPointerType(void* obj, Type? type, NodeOptions nodeOptions)
        => DrawPointerType((nint)obj, type, nodeOptions);

    public void DrawPointerType(nint address, Type? type, NodeOptions nodeOptions)
    {
        if (type == null)
        {
            ImGui.Text(""u8);
            return;
        }

        if (address == 0)
        {
            ImGui.Text("null"u8);
            return;
        }

        if (!_processInfoService.IsPointerValid(address))
        {
            ImGui.Text("invalid"u8);
            return;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Pointer<>))
        {
            address = *(nint*)address;
            type = type.GenericTypeArguments[0];
        }

        if (type == null)
        {
            ImGui.Text(""u8);
            return;
        }

        if (address == 0)
        {
            ImGui.Text("null"u8);
            return;
        }

        if (!_processInfoService.IsPointerValid(address))
        {
            ImGui.Text("invalid"u8);
            return;
        }

        // Get the original VTable address for addons from IAddonLifecycle, if it replaced it
        if (_pluginConfig.ResolveAddonLifecycleVTables)
        {
            var originalAddress = _addonLifecycle.GetOriginalVirtualTable(address);
            if (originalAddress != 0 && _processInfoService.IsPointerValid(originalAddress))
                address = originalAddress;
        }

        if (type.IsPointer && type.GetElementType() == typeof(void))
        {
            DrawAddress(*(nint*)address);
            return;
        }

        nodeOptions = nodeOptions.WithAddress(address) with
        {
            HighlightAddress = address,
            HighlightType = type,
        };

        if (type.IsVoid())
        {
            ImGui.Text(""u8);
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
                    case ObjectKind.ReactionEventObject:
                        type = typeof(FFXIVClientStructs.FFXIV.Client.Game.Object.ReactionEventObject);
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
                        additionalName = new ReadOnlySeStringSpan(((ShopEventHandler*)address)->ShopName.AsSpan()).ToString();
                        break;

                    case EventHandlerContent.Aetheryte:
                        type = typeof(AetheryteEventHandler);
                        break;

                    case EventHandlerContent.Craft:
                        type = typeof(CraftEventHandler);
                        break;

                    case EventHandlerContent.CustomTalk:
                        type = typeof(CustomTalkEventHandler);
                        additionalName = new ReadOnlySeStringSpan(((LuaEventHandler*)address)->LuaClass.AsSpan()).ToString();
                        break;

                    case EventHandlerContent.Fishing:
                        type = typeof(FishingEventHandler);
                        break;

                    case EventHandlerContent.FateDirector:
                        type = typeof(FateDirector);
                        break;

                    case EventHandlerContent.BattleLeveDirector:
                        type = typeof(BattleLeveDirector);
                        additionalName = new ReadOnlySeStringSpan(((LuaEventHandler*)address)->LuaClass.AsSpan()).ToString();
                        break;

                    case EventHandlerContent.CompanyLeveDirector:
                    case EventHandlerContent.CompanyLeveOfficer:
                    case EventHandlerContent.GatheringLeveDirector:
                        type = typeof(LeveDirector);
                        additionalName = new ReadOnlySeStringSpan(((LuaEventHandler*)address)->LuaClass.AsSpan()).ToString();
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

                    case EventHandlerContent.MassivePcContentDirector:
                        type = typeof(MassivePcContentDirector);
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
            else if (type == typeof(AtkUnitBase))
            {
                nodeOptions = nodeOptions.WithTitle($"{type.FullName} ({((AtkUnitBase*)address)->NameString})");
            }
            else if (type == typeof(AgentInterface))
            {
                var agent = (AgentInterface*)address;
                var agentModule = AgentModule.Instance();
                for (var i = 0; i < agentModule->Agents.Length; i++)
                {
                    var ptr = agentModule->Agents.GetPointer(i);
                    if (ptr->Value != null && ptr->Value == agent)
                    {
                        nodeOptions = nodeOptions.WithTitle($"{type.FullName} ({(AgentId)i})");
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
            ImGui.Text($"{*(bool*)address}");
            return;
        }
        else if (type == typeof(BitVector32))
        {
            ImGui.Text($"{*(BitVector32*)address}");
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
        else if (type == typeof(StdString))
        {
            ImGuiUtils.DrawCopyableText(((StdString*)address)->ToString());
            return;
        }
        else if (type == typeof(StdString))
        {
            ImGuiUtils.DrawCopyableText(((StdString*)address)->ToString());
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
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(StdLinkedList<>))
        {
            DrawStdLinkedList(address, type.GenericTypeArguments[0], nodeOptions);
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

        ImGui.Text("Unsupported Type"u8);
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
                    var gameObjectExists = GameObjectManager.Instance()->Objects.IndexSorted.Contains(gameObject);
                    if (gameObjectExists && gameObject->VirtualTable != null)
                    {
                        var pos = gameObject->GetPosition();
                        if (pos != null)
                            DrawLine((Vector3)(*pos));
                    }
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
                    else if (component != null && component->OwnerNode != null)
                        HighlightNode((AtkResNode*)component->OwnerNode);
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
            ImGuiContextMenu.Draw(nodeOptions.GetKey("ContextMenu"), builder => nodeOptions.DrawContextMenu(nodeOptions, builder));

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
                            DrawPointerType(*(nint**)fieldAddress, typeof(AtkUldComponentInfo), fieldNodeOptions);
                        else
                            DrawArray(new Span<AtkUldComponentInfo>(*(nint**)fieldAddress, objectCount), fieldNodeOptions);
                        break;

                    case AtkUldManagerBaseType.Widget:
                        if (objectCount == 1)
                            DrawPointerType(*(nint**)fieldAddress, typeof(AtkUldWidgetInfo), fieldNodeOptions);
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
                DrawArray(new Span<AtkTimeline>(*(nint**)fieldAddress, (int)((AtkTimelineManager*)address)->TimelineCount), fieldNodeOptions);
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

    private void DrawBitFields(Type structType, nint fieldAddress, int fieldOffset, Type fieldType, FieldInfo fieldInfo)
    {
        foreach (var structAttr in structType.GetCustomAttributes())
        {
            var structAttrType = structAttr.GetType();
            if (!structAttrType.IsGenericType)
                continue;

            if (structAttrType.GetGenericTypeDefinition() != typeof(InheritsAttribute<>))
                continue;

            var parentStructType = structAttrType.GenericTypeArguments[0];
            var parentFieldInfo = parentStructType.GetField(fieldInfo.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (parentFieldInfo != null)
            {
                DrawBitFields(parentStructType, fieldAddress, fieldOffset, fieldType, parentFieldInfo);
            }
        }

        foreach (var attr in fieldInfo.GetCustomAttributes())
        {
            var attrType = attr.GetType();
            if (!attrType.IsGenericType)
                continue;

            if (attrType.GetGenericTypeDefinition() != typeof(BitFieldAttribute<>))
                continue;

            DrawBitField(structType, fieldAddress, fieldOffset, fieldType, attr, attrType);
        }
    }

    private void DrawBitField(Type structType, nint fieldAddress, int fieldOffset, Type fieldType, Attribute attr, Type attrType)
    {
        var bitfieldType = attrType.GenericTypeArguments[0];
        var name = (string)attrType.GetProperty("Name")!.GetValue(attr)!;
        var index = (int)attrType.GetProperty("Index")!.GetValue(attr)!;
        var length = (int)attrType.GetProperty("Length")!.GetValue(attr)!;

        ImGuiUtils.DrawCopyableText($"[0x{fieldOffset:X}]", new()
        {
            CopyText = ImGui.IsKeyDown(ImGuiKey.LeftShift) ? $"{fieldAddress + fieldOffset:X}" : $"0x{fieldOffset:X}",
            TextColor = Color.Grey3
        });

        ImGuiUtils.SameLineSpace();

        ImGui.TextColored(ColorBitField, $"[{index}:{length}]");
        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            using var tooltip = ImRaii.Tooltip();
            ImGui.TextColored(ColorBitField, "BitField"u8);
            ImGui.Text($"Index: {index} \u2022 Length: {length}");
        }
        if (ImGui.IsItemClicked())
        {
            ImGui.SetClipboardText($"[{index}:{length}]");
        }

        ImGui.SameLine();

        var fieldValue = 0UL;

        switch (fieldType)
        {
            case Type tu when tu == typeof(byte):
                fieldValue = *(byte*)fieldAddress;
                break;

            case Type tu when tu == typeof(ushort):
                fieldValue = *(ushort*)fieldAddress;
                break;

            case Type tu when tu == typeof(uint):
                fieldValue = *(uint*)fieldAddress;
                break;

            case Type tu when tu == typeof(ulong):
                fieldValue = *(ulong*)fieldAddress;
                break;

            default:
                ImGui.Text($"FieldType {fieldType.Name} not supported");
                return;
        }

        ImGuiUtils.DrawCopyableText(bitfieldType.ReadableTypeName(), new()
        {
            CopyText = bitfieldType.ReadableTypeName(ImGui.IsKeyDown(ImGuiKey.LeftShift)),
            TextColor = ColorType
        });

        ImGui.SameLine();

        var fullName = $"{structType.FullName}.{name}";
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

        switch (bitfieldType)
        {
            case Type t when t == typeof(bool):
                {
                    var value = BitOps.GetBit(fieldValue, index);
                    ImGuiUtils.DrawCopyableText($"{value}");
                    break;
                }

            case Type t when t == typeof(sbyte) || t == typeof(byte) || t == typeof(short) || t == typeof(ushort) || t == typeof(int) || t == typeof(uint) || t == typeof(long) || t == typeof(ulong):
                {
                    var value = BitOps.GetBits(fieldValue, index, BitOps.CreateLowBitMask<ulong>(length));

                    if (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift))
                        ImGuiUtils.DrawCopyableText(ToHexString(value, bitfieldType));
                    else
                        ImGuiUtils.DrawCopyableText(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);

                    break;
                }

            case Type t when t.IsEnum:
                {
                    var value = BitOps.GetBits(fieldValue, index, BitOps.CreateLowBitMask<ulong>(length));
                    ImGuiUtils.DrawCopyableText(value.ToString());

                    if (t.GetCustomAttribute<FlagsAttribute>() != null)
                    {
                        ImGui.SameLine();
                        ImGui.Text(" - "u8);
                        var bitwidth = t.GetEnumUnderlyingType().SizeOf() * 8;
                        for (var i = 0; i < bitwidth; i++)
                        {
                            if (!BitOps.GetBit(value, i))
                                continue;

                            ImGui.SameLine();
                            var bitValue = 1ul << i;
                            ImGuiUtils.DrawCopyableText(Enum.GetName(t, bitValue)?.ToString() ?? $"{bitValue}", new()
                            {
                                CopyText = $"{bitValue}"
                            });
                        }
                    }
                    else
                    {
                        ImGui.SameLine();
                        ImGuiUtils.DrawCopyableText(Enum.GetName(t, value)?.ToString() ?? "");
                    }

                    break;
                }

            default:
                ImGui.Text($"BitField Type {bitfieldType.Name} not supported");
                break;
        }
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
            ImGui.Text(" - "u8);
            var bits = Marshal.SizeOf(underlyingType) * 8;
            for (var i = 0u; i < bits; i++)
            {
                var bitValue = 1u << (int)i;
                if ((Convert.ToUInt64(value) & bitValue) != 0)
                {
                    ImGui.SameLine();
                    ImGuiUtils.DrawCopyableText(Enum.GetName(type, bitValue)?.ToString() ?? $"{bitValue}", new() { CopyText = $"{bitValue}" });
                }
            }
        }
        else
        {
            ImGui.SameLine();
            ImGui.Text(Enum.GetName(type, value)?.ToString() ?? "");
        }
    }

    public void DrawAddress(void* obj)
        => DrawAddress((nint)obj);

    public void DrawAddress(nint address)
    {
        if (address == 0)
        {
            ImGui.Text("null");
            return;
        }

        var displayText = ImGui.IsKeyDown(ImGuiKey.LeftShift)
            ? $"0x{address:X}"
            : _processInfoService.GetAddressName(address);

        ImGuiUtils.DrawCopyableText(displayText);
    }

    public object? DrawNumeric(nint address, Type type, NodeOptions nodeOptions)
    {
        if (address == 0)
        {
            ImGui.Text("null"u8);
            return 0;
        }

        object? value = null;

        switch (type)
        {
            case Type t when t == typeof(nint):
                value = *(nint*)address;
                break;

            case Type t when t == typeof(Half):
                value = *(Half*)address;
                break;

            case Type t when t == typeof(sbyte):
                value = *(sbyte*)address;
                break;

            case Type t when t == typeof(byte):
                value = *(byte*)address;
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
                ImGui.Text("null"u8);
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
            ImGuiUtils.DrawCopyableText(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
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

        ImGui.Text($"Unhandled NumericType {type.FullName}");
    }

    private void DrawNumericWithHex(object value, Type type, NodeOptions nodeOptions)
    {
        if (nodeOptions.IsIconIdField)
        {
            DrawIcon(value, type);
        }

        if (nodeOptions.IsTimestampField)
        {
            ImGuiUtils.DrawCopyableText(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);

            switch (value)
            {
                case int intTime when intTime != 0:
                    ImGui.SameLine();
                    ImGuiUtils.DrawCopyableText(DateTimeOffset.FromUnixTimeSeconds(intTime).ToLocalTime().ToString());
                    break;

                case long longTime when longTime != 0:
                    ImGui.SameLine();
                    ImGuiUtils.DrawCopyableText(DateTimeOffset.FromUnixTimeSeconds(longTime).ToLocalTime().ToString());
                    break;
            }

            return;
        }
        else if (nodeOptions.HexOnShift)
        {
            if (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift))
            {
                ImGuiUtils.DrawCopyableText(ToHexString(value, type));
            }
            else
            {
                ImGuiUtils.DrawCopyableText(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
            }

            return;
        }

        ImGuiUtils.DrawCopyableText(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
        ImGui.SameLine();
        ImGuiUtils.DrawCopyableText(ToHexString(value, type));
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
            ImGui.Image(texture.Handle, drawInfo.DrawSize.Value);

            if (ImGui.IsItemHovered())
            {
                if (canCopy)
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                if (!noTooltip)
                {
                    ImGui.BeginTooltip();
                    if (canCopy)
                        ImGui.Text("Click to copy IconId"u8);
                    ImGui.Text($"ID: {iconId} – Size: {texture.Width}x{texture.Height}");
                    ImGui.Image(texture.Handle, new(texture.Width, texture.Height));
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

    public void DrawTexture(string path, bool sameLine = true, DrawInfo drawInfo = default, bool canCopy = true, bool noTooltip = false)
    {
        drawInfo.DrawSize ??= new Vector2(ImGui.GetTextLineHeight());

        if (string.IsNullOrEmpty(path))
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

        if (_textureProvider.GetFromGame(path).TryGetWrap(out var texture, out _))
        {
            ImGui.Image(texture.Handle, drawInfo.DrawSize.Value);

            if (ImGui.IsItemHovered())
            {
                if (canCopy)
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                if (!noTooltip)
                {
                    ImGui.BeginTooltip();
                    if (canCopy)
                        ImGui.Text("Click to copy IconId"u8);
                    ImGui.Text($"Path: {path} – Size: {texture.Width}x{texture.Height}");
                    ImGui.Image(texture.Handle, new(texture.Width, texture.Height));
                    ImGui.EndTooltip();
                }
            }

            if (canCopy && ImGui.IsItemClicked())
                ImGui.SetClipboardText(path.ToString());
        }
        else
        {
            ImGui.Dummy(drawInfo.DrawSize.Value);
        }

        if (sameLine)
            ImGui.SameLine();
    }

    public void DrawArray<T>(Span<T> span, NodeOptions nodeOptions) where T : unmanaged
    {
        if (span.Length == 0)
        {
            ImGui.Text("No values"u8);
            return;
        }

        nodeOptions = nodeOptions.WithAddress((nint)span.GetPointer(0));

        using var node = DrawTreeNode(nodeOptions.WithTitle($"{span.Length} value{(span.Length != 1 ? "s" : "")}") with { DrawSeStringTreeNode = false });
        if (!node) return;

        nodeOptions = nodeOptions.ConsumeTreeNodeOptions();

        using var table = ImRaii.Table(nodeOptions.GetKey("Array"), 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("Index"u8, ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Value");
        ImGui.TableSetupScrollFreeze(2, 1);
        ImGui.TableHeadersRow();

        var type = typeof(T);
        for (var i = 0; i < span.Length; i++)
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // Index
            ImGui.Text(i.ToString());

            ImGui.TableNextColumn(); // Value
            var ptr = span.GetPointer(i);
            DrawPointerType(ptr, type, nodeOptions);
        }
    }

    public void HighlightNode(AtkResNode* node)
    {
        if (!_processInfoService.IsPointerValid(node))
            return;

        var scale = 1f;
        var addon = RaptureAtkUnitManager.Instance()->AtkUnitManager.GetAddonByNodeSafe(node);
        if (_processInfoService.IsPointerValid(addon))
            scale *= addon->Scale;

        var pos = ImGui.GetMainViewport().Pos + new Vector2(node->ScreenX, node->ScreenY);
        var size = new Vector2(node->Width, node->Height) * scale;
        ImGui.GetForegroundDrawList().AddRect(pos, pos + size, Color.Gold.ToUInt());
    }
}
