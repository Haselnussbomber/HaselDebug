using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselDebug.Services;

// TODO: pick GameObjects?

[RegisterSingleton, AutoConstruct]
public unsafe partial class AtkNodePicker : IDisposable
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly NavigationService _navigationService;

    private HashSet<Pointer<AtkResNode>> _lastHoveredNodePtrs = [];

    public bool ShowPicker { get; set; }
    public int NodePickerSelectionIndex { get; set; }


    [AutoPostConstruct]
    private void Initialize()
    {
        _pluginInterface.UiBuilder.Draw += Draw;
    }

    public void Dispose()
    {
        _pluginInterface.UiBuilder.Draw -= Draw;
    }

    public void Draw()
    {
        if (!ShowPicker)
            return;

        var raptureAtkUnitManager = RaptureAtkUnitManager.Instance();
        var allUnitsList = new List<Pointer<AtkUnitBase>>();

        for (var i = 0; i < raptureAtkUnitManager->AllLoadedUnitsList.Count; i++)
        {
            var unitBase = raptureAtkUnitManager->AllLoadedUnitsList.Entries[i].Value;
            if (unitBase == null || !unitBase->IsFullyLoaded() || !unitBase->IsVisible)
                continue;
            allUnitsList.Add(unitBase);
        }

        allUnitsList.Sort((a, b) =>
        {
            var aIsOverlay = a.Value->Name.StartsWith("KTK_Overlay"u8);
            var bIsOverlay = b.Value->Name.StartsWith("KTK_Overlay"u8);

            if (aIsOverlay && !bIsOverlay)
                return 1;
            if (!aIsOverlay && bIsOverlay)
                return -1;

            return (int)(b.Value->DepthLayer - a.Value->DepthLayer);
        });

        var hoveredDepthLayerAddonNodes = new Dictionary<uint, Dictionary<Pointer<AtkUnitBase>, List<Pointer<AtkResNode>>>>();
        var nodeCount = 0;
        var bounds = stackalloc FFXIVClientStructs.FFXIV.Common.Math.Bounds[1];

        var currentHoveredNodePtrs = new HashSet<Pointer<AtkResNode>>();

        foreach (AtkUnitBase* unitBase in allUnitsList)
        {
            unitBase->GetWindowBounds(bounds);
            var pos = new Vector2(bounds->Pos1.X, bounds->Pos1.Y);
            var size = new Vector2(bounds->Size.X, bounds->Size.Y);

            if (!bounds->ContainsPoint((int)ImGui.GetMousePos().X, (int)ImGui.GetMousePos().Y))
                continue;

            FindHoveredNodes(hoveredDepthLayerAddonNodes, ref nodeCount, currentHoveredNodePtrs, &unitBase->UldManager, unitBase);
        }

        // Only reset selection index if hovered nodes changed
        if (!currentHoveredNodePtrs.SetEquals(_lastHoveredNodePtrs))
        {
            NodePickerSelectionIndex = 0;
            _lastHoveredNodePtrs = currentHoveredNodePtrs;
        }

        if (nodeCount == 0)
        {
            ShowPicker = false;
            _lastHoveredNodePtrs.Clear();
            return;
        }

        ImGui.SetNextWindowPos(_pluginInterface.IsDevMenuOpen ? new Vector2(0, ImGui.GetFrameHeight()) : Vector2.Zero);
        ImGui.SetNextWindowSize(ImGui.GetMainViewport().Size);

        if (!ImGui.Begin("NodePicker", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            return;

        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        var nodeIndex = 0;
        foreach (var (depthLayer, addons) in hoveredDepthLayerAddonNodes)
        {
            DrawText($"Depth Layer {depthLayer}:");

            using var indent = ImRaii.PushIndent();
            foreach (var (unitBase, nodes) in addons)
            {
                DrawText($"{unitBase.Value->NameString}:");

                using var indent2 = ImRaii.PushIndent();

                for (var i = nodes.Count - 1; i >= 0; i--)
                {
                    var node = nodes[i].Value;
                    node->GetBounds(bounds);

                    if (NodePickerSelectionIndex == nodeIndex)
                    {
                        using (ImRaii.PushFont(UiBuilder.IconFont))
                            DrawText(FontAwesomeIcon.CaretRight.ToIconString());
                        ImGui.SameLine(0, 0);

                        ImGui.GetBackgroundDrawList().AddRectFilled(
                            new Vector2(bounds->Pos1.X, bounds->Pos1.Y),
                            new Vector2(bounds->Pos2.X, bounds->Pos2.Y),
                            new Color(1, 1, 0, 0.5f).ToUInt());

                        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                        {
                            var nodePath = new List<Pointer<AtkResNode>>();
                            var current = node;
                            while (current != null)
                            {
                                nodePath.Insert(0, current);
                                current = current->ParentNode;
                            }

                            NodePickerSelectionIndex = 0;
                            ShowPicker = false;
                            _lastHoveredNodePtrs.Clear();

                            _navigationService.NavigateTo(new AddonNavigation()
                            {
                                AddonId = unitBase.Value->Id,
                                AddonName = unitBase.Value->NameString,
                                NodePath = nodePath
                            });
                        }
                    }

                    if (node->GetNodeType() != NodeType.Component)
                    {
                        DrawText($"[0x{(nint)node:X}] [{node->NodeId}] {node->Type} Node");
                    }
                    else
                    {
                        var compNode = (AtkComponentNode*)node;
                        var componentInfo = compNode->Component->UldManager;
                        var objectInfo = (AtkUldComponentInfo*)componentInfo.Objects;
                        if (objectInfo == null) continue;
                        DrawText($"[0x{(nint)node:X}] [{node->NodeId}] {objectInfo->ComponentType} Component Node");
                    }

                    nodeIndex++;
                }
            }
        }

        NodePickerSelectionIndex -= (int)ImGui.GetIO().MouseWheel;
        if (NodePickerSelectionIndex < 0)
            NodePickerSelectionIndex = nodeCount - 1;
        if (NodePickerSelectionIndex > nodeCount - 1)
            NodePickerSelectionIndex = 0;

        ImGui.End();
    }

    private static void FindHoveredNodes(
        Dictionary<uint, Dictionary<Pointer<AtkUnitBase>, List<Pointer<AtkResNode>>>> hoveredDepthLayerAddonNodes,
        ref int nodeCount,
        HashSet<Pointer<AtkResNode>> currentHoveredNodePtrs,
        AtkUldManager* uldManager,
        AtkUnitBase* unitBase)
    {
        var bounds = stackalloc FFXIVClientStructs.FFXIV.Common.Math.Bounds[1];

        for (var i = 0; i < uldManager->NodeListCount; i++)
        {
            var node = uldManager->NodeList[i];
            node->GetBounds(bounds);

            if (!bounds->ContainsPoint((int)ImGui.GetMousePos().X, (int)ImGui.GetMousePos().Y))
                continue;

            currentHoveredNodePtrs.Add(node);

            if (!hoveredDepthLayerAddonNodes.TryGetValue(unitBase->DepthLayer, out var addonNodes))
                hoveredDepthLayerAddonNodes.Add(unitBase->DepthLayer, addonNodes = []);

            if (!addonNodes.TryGetValue(unitBase, out var nodes))
                addonNodes.Add(unitBase, [node]);
            else if (!nodes.Contains(node))
                nodes.Add(node);

            nodeCount++;

            if ((ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift)) && node->GetNodeType() == NodeType.Component)
            {
                var componentNode = (AtkComponentNode*)node;
                var component = node->GetComponent();
                FindHoveredNodes(hoveredDepthLayerAddonNodes, ref nodeCount, currentHoveredNodePtrs, &component->UldManager, unitBase);
            }
        }
    }

    private static void DrawText(string text)
    {
        var position = ImGui.GetCursorPos();
        var outlineColor = Color.Black with { A = 0.5f };

        // outline
        ImGui.SetCursorPos(position + ImGuiHelpers.ScaledVector2(-1));
        using (outlineColor.Push(ImGuiCol.Text))
            ImGui.Text(text);

        ImGui.SetCursorPos(position + ImGuiHelpers.ScaledVector2(1));
        using (outlineColor.Push(ImGuiCol.Text))
            ImGui.Text(text);

        ImGui.SetCursorPos(position + ImGuiHelpers.ScaledVector2(1, -1));
        using (outlineColor.Push(ImGuiCol.Text))
            ImGui.Text(text);

        ImGui.SetCursorPos(position + ImGuiHelpers.ScaledVector2(-1, 1));
        using (outlineColor.Push(ImGuiCol.Text))
            ImGui.Text(text);

        // text
        ImGui.SetCursorPos(position);
        ImGui.Text(text);
    }
}
