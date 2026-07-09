using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using FFXIVClientStructs.FFXIV.Client.Game.MassivePcContent;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Group;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Layer;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselDebug.Extensions;
using DrawObjectType = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.ObjectType;
using EventHandler = FFXIVClientStructs.FFXIV.Client.Game.Event.EventHandler;
using InstanceContentType = FFXIVClientStructs.FFXIV.Client.Game.InstanceContent.InstanceContentType;

namespace HaselDebug.Utils;

public static unsafe class TypeResolver
{
    public static void Resolve(nint address, ref Type type, ref NodeOptions nodeOptions)
    {
        if (nodeOptions.ResolvedInheritedTypeAddresses.Path.Contains(address))
            return;

        if (Inherits<ResourceHandle>(type))
        {
            switch (((ResourceHandle*)address)->FileTypeString)
            {
                case "a": type = typeof(DefaultResourceHandle); break;
                case "aet": type = typeof(AnimationExchangeTableResourceHandle); break;
                case "amb": type = typeof(AmbientSetResourceHandle); break;
                case "asik": type = typeof(AutoShakeIkResourceHandle); break;
                case "atch": type = typeof(AttachOffsetResourceHandle); break;
                case "atex": type = typeof(ApricotTextureResourceHandle); break;
                case "avfx": type = typeof(ApricotResourceHandle); break;
                case "awt": type = typeof(AnimationWorkTableResourceHandle); break;
                case "bklb": type = typeof(BonamikLoadResourceHandle); break;
                case "bnmb": type = typeof(BonamikResourceHandle); break;
                case "cldb": type = typeof(CloudAssetResourceHandle); break;
                case "cmp": type = typeof(CharaMakeParameterResourceHandle); break;
                case "cmt": type = typeof(CameraShakeResourceHandle); break;
                case "ctb": type = typeof(ControlPointResourceHandle); break;
                case "cur": type = typeof(CursorResourceHandle); break;
                case "cutb": type = typeof(CutSceneResourceHandle); break;
                case "dic": type = typeof(DefaultResourceHandle); break;
                case "eanb": type = typeof(EyeAnimationResourceHandle); break;
                case "eid": type = typeof(ElementIdResourceHandle); break;
                case "envb": type = typeof(EnvSetResourceHandle); break;
                case "eqdp": type = typeof(EquipmentDeformerParameterResourceHandle); break;
                case "eqp": type = typeof(EquipmentParameterResourceHandle); break;
                case "eslb": type = typeof(ExtraSkeletonLoadResourceHandle); break;
                case "essb": type = typeof(SoundSetResourceHandle); break;
                case "est": type = typeof(ExSkeletonTableResourceHandle); break;
                case "evp": type = typeof(EquipmentVfxParameterResourceHandle); break;
                case "exd": type = typeof(ExdResourceHandle); break;
                case "exh": type = typeof(ExhResourceHandle); break;
                case "exl": type = typeof(ExlResourceHandle); break;
                case "fdt": type = typeof(FontdataResourceHandle); break;
                case "fpeb": type = typeof(FacialParameterEditResourceHandle); break;
                case "gfd": type = typeof(GaijiFontdataResourceHandle); break;
                case "ggd": type = typeof(GrassGridDataResourceHandle); break;
                case "gmp": type = typeof(GimmickParameterResourceHandle); break;
                case "gzd": type = typeof(GrassZoneDataResourceHandle); break;
                case "hwc": type = typeof(HardwareCursorResourceHandle); break;
                case "imc": type = typeof(ImageChangeDataResourceHandle); break;
                case "kdb": type = typeof(KineDriverResourceHandle); break;
                case "kdlb": type = typeof(KineDriverLoadResourceHandle); break;
                case "laik": type = typeof(LookAtIkResourceHandle); break;
                case "lcb": type = typeof(ClipAABBResourceHandle); break;
                case "lgb": type = typeof(LayerGroupResourceHandle); break;
                case "lua": type = typeof(LuaResourceHandle); break;
                case "luab": type = typeof(LuabResourceHandle); break;
                case "lvb": type = typeof(LevelSceneResourceHandle); break;
                case "mdl": type = typeof(ModelResourceHandle); break;
                case "mlt": type = typeof(MotionLineTableResourceHandle); break;
                case "msb": type = typeof(MsbResourceHandle); break;
                case "mtrl": type = typeof(MaterialResourceHandle); break;
                case "nvm": type = typeof(NaviMeshResourceHandle); break;
                case "obsb": type = typeof(ObjectBehaviorSetResourceHandle); break;
                case "pap": type = typeof(PartialAnimationPackResourceHandle); break;
                case "pbd": type = typeof(PreBoneDeformerResourceHandle); break;
                case "pcb": type = typeof(CollisionMeshResourceHandle); break;
                case "phyb": type = typeof(BonePhysicsResourceHandle); break;
                case "plt": type = typeof(PapLoadTableResourceHandle); break;
                case "png": type = typeof(PNGResourceHandle); break;
                case "scd": type = typeof(SoundResourceHandle); break;
                case "sgb": type = typeof(SharedGroupResourceHandle); break;
                case "shcd": type = typeof(ShaderCodeResourceHandle); break;
                case "shpk": type = typeof(ShaderPackageResourceHandle); break;
                case "sklb": type = typeof(SkeletonResourceHandle); break;
                case "skp": type = typeof(SkeletonParamResourceHandle); break;
                case "spm": type = typeof(SpmResourceHandle); break;
                case "stm": type = typeof(StainingTemplateResourceHandle); break;
                case "svb": type = typeof(SkyVisibilityResourceHandle); break;
                case "tera": type = typeof(TerrainResourceHandle); break;
                case "tex": type = typeof(TextureResourceHandle); break;
                case "tmb": type = typeof(TimeLineResourceHandle); break;
                case "ugd": type = typeof(UgdResourceHandle); break;
                case "uld": type = typeof(UldResourceHandle); break;
                case "uwb": type = typeof(UdwResourceHandle); break;
                case "waoe": type = typeof(WeaponAttachOffsetExistResourceHandle); break;
                case "wtd": type = typeof(WeaponTypeDataResourceHandle); break;
            }
        }
        else if (Inherits<ILayoutInstance>(type))
        {
            switch (((ILayoutInstance*)address)->Id.Type)
            {
                case InstanceType.BgPart:
                    type = typeof(BgPartsLayoutInstance);
                    break;

                case InstanceType.SharedGroup:
                    type = typeof(SharedGroupLayoutInstance);
                    break;

                case InstanceType.Sound:
                    type = typeof(SoundLayoutInstance);
                    break;

                case InstanceType.ExitRange:
                    type = typeof(ExitRangeLayoutInstance);
                    break;

                case InstanceType.MapRange:
                    type = typeof(MapRangeLayoutInstance);
                    break;

                case InstanceType.EnvLocation:
                    type = typeof(EnvLocationLayoutInstance);
                    break;

                case InstanceType.Timeline:
                    type = typeof(TimeLineLayoutInstance);
                    break;

                case InstanceType.CollisionBox:
                    type = typeof(CollisionBoxLayoutInstance);
                    break;

                case InstanceType.DoorRange:
                    type = typeof(DoorRangeLayoutInstance);
                    break;

                case InstanceType.LineVfx:
                    type = typeof(LineVfxLayoutInstance);
                    break;

                case InstanceType.PrefetchRange:
                    type = typeof(PrefetchRangeLayoutInstance);
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
        else if (Inherits<FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Object>(type))
        {
            switch (((DrawObject*)address)->GetObjectType())
            {
                case DrawObjectType.Terrain:
                    type = typeof(Terrain);
                    break;

                case DrawObjectType.BgObject:
                    type = typeof(BgObject);
                    break;

                case DrawObjectType.CharacterBase:
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

                case DrawObjectType.VfxObject:
                    type = typeof(VfxObject);
                    break;

                case DrawObjectType.Light:
                    type = typeof(Light);
                    break;

                // UnkType6

                case DrawObjectType.EnvSpace:
                    type = typeof(EnvSpace);
                    break;

                case DrawObjectType.EnvLocation:
                    type = typeof(EnvLocation);
                    break;

                case DrawObjectType.Decal:
                    type = typeof(Decal);
                    break;

                case DrawObjectType.UnkType10:
                    type = typeof(DrawObject);
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
                    additionalName = ServiceLocator.GetService<TextService>()?.GetQuestName(eventId.Id);
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
}
