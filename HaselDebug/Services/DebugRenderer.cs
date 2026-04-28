using System.Collections.Immutable;
using System.Collections.Specialized;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.Sound;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.STD;
using HaselDebug.Config;
using HaselDebug.Extensions;
using HaselDebug.Service;
using HaselDebug.Services.Data;
using HaselDebug.Utils;
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
    private readonly IAgentLifecycle _agentLifecycle;

    public void DrawPointerType<T>(T* obj, NodeOptions? nodeOptions = null) where T : unmanaged
        => DrawPointerType((nint)obj, typeof(T), nodeOptions);

    public void DrawPointerType<T>(Pointer<T> obj, NodeOptions? nodeOptions = null) where T : unmanaged
        => DrawPointerType((nint)obj.Value, typeof(T), nodeOptions);

    public void DrawPointerType(void* obj, Type type, NodeOptions? nodeOptions = null)
        => DrawPointerType((nint)obj, type, nodeOptions);

    public void DrawPointerType(nint address, Type type, NodeOptions? nodeOptions = null)
    {
        var options = nodeOptions ?? new();

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

        // Get the original VTable address for addons from IAgentLifecycle, if it replaced it
        if (_pluginConfig.ResolveAgentLifecycleVTables)
        {
            var originalAddress = _agentLifecycle.GetOriginalVirtualTable(address);
            if (originalAddress != 0 && _processInfoService.IsPointerValid(originalAddress))
                address = originalAddress;
        }

        if (type.IsPointer && type.GetElementType() == typeof(void))
        {
            DrawAddress(*(nint*)address);
            return;
        }

        options = options.WithAddress(address) with
        {
            HighlightAddress = address,
            HighlightType = type,
        };

        if (type.IsVoid())
        {
            ImGui.Text(""u8);
            return;
        }

        TypeResolver.Resolve(address, ref type, ref options);

        if (type.IsPointer)
        {
            type = type.GetElementType() ?? type;
            address = *(nint*)address;
            DrawPointerType(address, type, options);
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
            DrawUtf8String(address, options);
            return;
        }
        else if (type == typeof(KernelTexture))
        {
            DrawTexture(address, options);
            return;
        }
        else if (type == typeof(AtkValue))
        {
            DrawAtkValue(address, options);
            return;
        }
        else if (type == typeof(CStringPointer))
        {
            DrawSeString(*(byte**)address, options);
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
            DrawStdVector(address, type.GenericTypeArguments[0], options);
            return;
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(StdMap<,>))
        {
            DrawStdMap(address, type.GenericTypeArguments[0], type.GenericTypeArguments[1], options);
            return;
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(StdSet<>))
        {
            DrawStdSet(address, type.GenericTypeArguments[0], options);
            return;
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(StdList<>))
        {
            DrawStdList(address, type.GenericTypeArguments[0], options);
            return;
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(StdLinkedList<>))
        {
            DrawStdLinkedList(address, type.GenericTypeArguments[0], options);
            return;
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(StdDeque<>))
        {
            DrawStdDeque(address, type.GenericTypeArguments[0], options);
            return;
        }
        else if (type.IsEnum)
        {
            DrawEnum(address, type, options);
            return;
        }
        else if (type.IsNumericType())
        {
            DrawNumeric(address, type, options);
            return;
        }
        else if (type.IsStruct() || type.IsClass)
        {
            DrawStruct(address, type, options);
            return;
        }

        ImGui.Text("Unsupported Type"u8);
    }

    public ImRaii.TreeNodeDisposable DrawTreeNode(NodeOptions nodeOptions)
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
                HighlightPointerType(nodeOptions.HighlightAddress, nodeOptions.HighlightType);
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

    private void HighlightPointerType(nint address, Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Pointer<>))
        {
            type = type.GenericTypeArguments[0];
            address = *(nint*)address;
        }

        if (type.IsPointer)
        {
            type = type.GetElementType()!;
            address = *(nint*)address;
        }

        if (Inherits<ILayoutInstance>(type))
        {
            var inst = (ILayoutInstance*)address;
            if (inst != null)
            {
                var transform = inst->GetTransformImpl();
                if (transform != null)
                    DrawLineToGamePos(transform->Translation);
            }
        }
        else if (Inherits<GameObject>(type))
        {
            var gameObject = (GameObject*)address;
            var gameObjectExists = GameObjectManager.Instance()->Objects.IndexSorted.Contains(gameObject);
            if (gameObjectExists && gameObject->VirtualTable != null)
            {
                var pos = gameObject->GetPosition();
                if (pos != null)
                    DrawLineToGamePos((Vector3)(*pos));
            }
        }
        else if (Inherits<FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Object>(type))
        {
            var obj = (FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Object*)address;
            DrawLineToGamePos(obj->Position);
        }
        else if (Inherits<AtkUnitBase>(type))
        {
            var unitBase = (AtkUnitBase*)address;
            if (unitBase->WindowNode != null)
                HighlightNode((AtkResNode*)unitBase->WindowNode);
            else if (unitBase->RootNode != null)
                HighlightNode(unitBase->RootNode);
        }
        else if (Inherits<AtkResNode>(type))
        {
            HighlightNode((AtkResNode*)address);
        }
        else if (Inherits<AtkComponentBase>(type))
        {
            var component = (AtkComponentBase*)address;
            if (component != null && component->AtkResNode != null)
                HighlightNode(component->AtkResNode);
            else if (component != null && component->OwnerNode != null)
                HighlightNode((AtkResNode*)component->OwnerNode);
        }
        else if (Inherits<ISoundData>(type))
        {
            var soundData = (ISoundData*)address;
            if (soundData->GetIsPositional())
            {
                var pos = new Vector3(soundData->GetPositionX(), soundData->GetPositionY(), soundData->GetPositionZ());
                if (pos.LengthSquared() > 0.001f)
                    DrawLineToGamePos(pos);
            }
        }
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
        var size = node->Size * scale;
        ImGui.GetForegroundDrawList().AddRect(pos, pos + size, Color.Gold.ToUInt());
    }

    private void DrawLineToGamePos(Vector3 pos)
    {
        if (_gameGui.WorldToScreen(pos, out var screenPos))
        {
            var drawList = ImGui.GetForegroundDrawList();
            drawList.AddLine(ImGui.GetMousePos(), screenPos, Color.Orange.ToUInt());
            drawList.AddCircleFilled(screenPos, 3f, Color.Orange.ToUInt());
        }
    }
}
