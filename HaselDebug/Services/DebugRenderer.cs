using System.Collections.Immutable;
using System.Collections.Specialized;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.Sound;
using FFXIVClientStructs.FFXIV.Client.System.File;
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
        { typeof(AtkTextNode), ["OriginalTextPointer"] },
        { typeof(RaptureAtkModule.NamePlateInfo), ["NameOverride"] },
        { typeof(WarpInfo), ["TerritoryTypeBg"] }
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
        else if (type == typeof(FileAccessPath))
        {
            ImGuiUtils.DrawCopyableText(((FileAccessPath*)address)->ToString());
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
            DrawPointerNumber(address, type, options);
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
                var drawObject = gameObject->GetDrawObject();
                if (drawObject != null)
                {
                    var bounds = new FFXIVClientStructs.FFXIV.Common.Math.OrientedBounds();
                    drawObject->ComputeOrientedBounds(&bounds);
                    DrawOrientedBounds(bounds);
                }
                else if (gameObject->SharedGroupLayoutInstance != null)
                {
                    var bounds = new FFXIVClientStructs.FFXIV.Common.Math.OrientedBounds();
                    gameObject->SharedGroupLayoutInstance->GetOrientedBoundsImpl(&bounds);
                    DrawOrientedBounds(bounds);
                }
                else
                {
                    var pos = gameObject->GetPosition();
                    if (pos != null)
                        DrawLineToGamePos((Vector3)(*pos));
                }
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

        var origin = ImGui.GetMainViewport().Pos + new Vector2(node->ScreenX, node->ScreenY);

        var width = node->Width * scale;
        var height = node->Height * scale;

        // Define the original rectangle that we will then transform below
        Span<Vector2> localCorners =
        [
            new(0, 0),
            new(width, 0),
            new(width, height),
            new(0, height),
        ];

        var transform = node->Transform;
        Span<Vector2> screenCorners = stackalloc Vector2[4];

        // Calculate transform using #math
        for (var i = 0; i < 4; i++)
        {
            var local = localCorners[i];
            var transformedX = local.X * transform.M11 - local.Y * transform.M12;
            var transformedY = -local.X * transform.M21 + local.Y * transform.M22;

            screenCorners[i] = origin + new Vector2(transformedX, transformedY);
        }

        // Draw transformed bounds via Polyline
        var drawList = ImGui.GetForegroundDrawList();
        drawList.AddPolyline(ref screenCorners[0], 4, Color.Gold.ToUInt(), ImDrawFlags.Closed, 1.5f);
    }

    private void DrawLineToGamePos(Vector3 pos)
    {
        var drawList = ImGui.GetForegroundDrawList();
        var color = Color.Orange.ToUInt();
        var mousePos = ImGui.GetMousePos();

        // On-screen: Draw line to position with the center dot
        if (_gameGui.WorldToScreen(pos, out var screenPos))
        {
            drawList.AddLine(mousePos, screenPos, color);
            drawList.AddCircleFilled(screenPos, 3f, color);
            return;
        }

        // Off-screen: Project line to the screen edge (no dot)
        var displaySize = ImGui.GetIO().DisplaySize;
        var screenCenter = displaySize * 0.5f;

        // Calculate direction from screen center toward projected target
        var dir = Vector2.Normalize(screenPos - screenCenter);
        if (dir == Vector2.Zero || float.IsNaN(dir.X))
        {
            dir = new Vector2(0, 1); // Fallback
        }

        var scaleX = (dir.X > 0 ? (displaySize.X - screenCenter.X) : screenCenter.X) / dir.X;
        var scaleY = (dir.Y > 0 ? (displaySize.Y - screenCenter.Y) : screenCenter.Y) / dir.Y;
        var scale = MathF.Min(MathF.Abs(scaleX), MathF.Abs(scaleY));

        var edgePos = screenCenter + dir * scale;
        drawList.AddLine(mousePos, edgePos, color);
    }

    private void DrawOrientedBounds(FFXIVClientStructs.FFXIV.Common.Math.OrientedBounds bounds)
    {
        var extents = bounds.HalfExtents;

        Vector3[] localCorners = [
            new(-extents.X, -extents.Y, -extents.Z), // 0: Bottom-left-back
            new( extents.X, -extents.Y, -extents.Z), // 1: Bottom-right-back
            new( extents.X, -extents.Y,  extents.Z), // 2: Bottom-right-front
            new(-extents.X, -extents.Y,  extents.Z), // 3: Bottom-left-front
            new(-extents.X,  extents.Y, -extents.Z), // 4: Top-left-back
            new( extents.X,  extents.Y, -extents.Z), // 5: Top-right-back
            new( extents.X,  extents.Y,  extents.Z), // 6: Top-right-front
            new(-extents.X,  extents.Y,  extents.Z), // 7: Top-left-front
        ];

        // Transform local corners into world space
        var worldCorners = new Vector3[8];
        for (var i = 0; i < 8; i++)
        {
            worldCorners[i] = Vector3.Transform(localCorners[i], bounds.Transform);
        }

        // Draw line to center
        Vector3 worldCenter = bounds.Transform.Translation;
        DrawLineToGamePos(worldCenter);

        // Render clipped 3D edges
        var drawList = ImGui.GetForegroundDrawList();
        var color = Color.Orange.ToUInt();
        const float thickness = 1.0f;

        void DrawClippedLine(Vector3 p1, Vector3 p2)
        {
            var p1Valid = _gameGui.WorldToScreen(p1, out var s1);
            var p2Valid = _gameGui.WorldToScreen(p2, out var s2);

            if (p1Valid && p2Valid)
            {
                drawList.AddLine(s1, s2, color, thickness);
                return;
            }

            if (!p1Valid && !p2Valid) return;

            var validPoint = p1Valid ? p1 : p2;
            var invalidPoint = p1Valid ? p2 : p1;

            float low = 0.0f, high = 1.0f;
            Vector2 clippedScreen = default;

            for (var i = 0; i < 10; i++)
            {
                var mid = (low + high) * 0.5f;
                var testPoint = Vector3.Lerp(validPoint, invalidPoint, mid);

                if (_gameGui.WorldToScreen(testPoint, out var testScreen))
                {
                    clippedScreen = testScreen;
                    low = mid;
                }
                else
                {
                    high = mid;
                }
            }

            if (p1Valid)
            {
                drawList.AddLine(s1, clippedScreen, color, thickness);
            }
            else
            {
                drawList.AddLine(clippedScreen, s2, color, thickness);
            }
        }

        // Bottom face
        DrawClippedLine(worldCorners[0], worldCorners[1]);
        DrawClippedLine(worldCorners[1], worldCorners[2]);
        DrawClippedLine(worldCorners[2], worldCorners[3]);
        DrawClippedLine(worldCorners[3], worldCorners[0]);

        // Top face
        DrawClippedLine(worldCorners[4], worldCorners[5]);
        DrawClippedLine(worldCorners[5], worldCorners[6]);
        DrawClippedLine(worldCorners[6], worldCorners[7]);
        DrawClippedLine(worldCorners[7], worldCorners[4]);

        // Vertical pillars
        DrawClippedLine(worldCorners[0], worldCorners[4]);
        DrawClippedLine(worldCorners[1], worldCorners[5]);
        DrawClippedLine(worldCorners[2], worldCorners[6]);
        DrawClippedLine(worldCorners[3], worldCorners[7]);
    }
}
