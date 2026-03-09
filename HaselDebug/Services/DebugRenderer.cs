using System.Collections.Immutable;
using System.Collections.Specialized;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using FFXIVClientStructs.FFXIV.Client.Game.MassivePcContent;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Group;
using FFXIVClientStructs.FFXIV.Client.Sound;
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
                else if (Inherits<ISoundData>(highlightType))
                {
                    var soundData = (ISoundData*)highlightAddress;
                    if (soundData->GetIsPositional())
                    {
                        var pos = new Vector3(soundData->GetPositionX(), soundData->GetPositionY(), soundData->GetPositionZ());
                        if (pos.LengthSquared() > 0.001f)
                            DrawLine(pos);
                    }
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

    private void HighlightNode(AtkResNode* node)
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
