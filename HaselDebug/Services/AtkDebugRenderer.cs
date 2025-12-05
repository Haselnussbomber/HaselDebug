using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselDebug.Extensions;
using HaselDebug.Utils;
using HaselDebug.Windows;

namespace HaselDebug.Services;

public struct DrawAddonParams()
{
    public ushort AddonId { get; set; }
    public string? AddonName { get; set; }
    public List<Pointer<AtkResNode>>? NodePath { get; set; }
    public bool Border { get; set; } = true;
    public bool UseNavigationService { get; set; } = true;
}

[RegisterSingleton, AutoConstruct]
public unsafe partial class AtkDebugRenderer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly TypeService _typeService;
    private readonly DebugRenderer _debugRenderer;
    private readonly TextService _textService;
    private readonly WindowManager _windowManager;
    private readonly LanguageProvider _languageProvider;
    private readonly AddonObserver _addonObserver;
    private readonly PinnedInstancesService _pinnedInstancesService;
    private readonly NavigationService _navigationService;
    private readonly Dictionary<string, OrderedDictionary<int, string>> _fieldMapping = [];
    private string _nodeQuery = string.Empty;

    public void DrawAddon(DrawAddonParams drawParams)
    {
        if (drawParams.AddonId == 0 && string.IsNullOrEmpty(drawParams.AddonName))
            return;

        using var hostchild = ImRaii.Child("AddonChild", new Vector2(-1), drawParams.Border, ImGuiWindowFlags.NoSavedSettings);

        var unitManager = RaptureAtkUnitManager.Instance();

        AtkUnitBase* unitBase = null;

        if (drawParams.AddonId != 0)
            unitBase = unitManager->GetAddonById(drawParams.AddonId);

        if ((unitBase == null && !string.IsNullOrEmpty(drawParams.AddonName)) || (unitBase != null && unitBase->NameString != drawParams.AddonName))
            unitBase = unitManager->GetAddonByName(drawParams.AddonName);

        if (!((nint)unitBase).IsValid())
        {
            ImGui.Text($"Could not find addon with id {drawParams.AddonId} or name {drawParams.AddonName}");
            return;
        }

        var nodeOptions = new NodeOptions()
        {
            AddressPath = new((nint)unitBase),
            DefaultOpen = true,
            UnitBase = unitBase,
        };

        var type = _typeService.GetAddonType(unitBase->NameString);

        if (!_fieldMapping.ContainsKey(unitBase->NameString))
        {
            var fields = _fieldMapping[unitBase->NameString] = [];

            LoadTypeMapping(fields, "", 0, type);

            for (var offset = 0; offset < type.SizeOf() - 8; offset += 8)
            {
                if (!fields.ContainsKey(offset))
                {
                    fields[offset] = $"+0x{offset:X}";
                }
            }
        }

        ImGuiUtils.DrawCopyableText(unitBase->NameString);

        ImGui.SameLine();

        var isVisible = unitBase->IsVisible;
        using (ImRaii.PushColor(ImGuiCol.Text, isVisible ? 0xFF00FF00 : Color.From(ImGuiCol.TextDisabled).ToUInt()))
        {
            ImGui.Text(isVisible ? "Visible" : "Not Visible");
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            ImGui.SetTooltip("Toggle visibility");
        }
        if (ImGui.IsItemClicked())
        {
            unitBase->IsVisible = !isVisible;
        }

        ImGuiUtilsEx.PaddedSeparator(1);

        ImGuiUtilsEx.PrintFieldValuePair("Address", ((nint)unitBase).ToString("X"));
        ImGui.SameLine();
        _debugRenderer.DrawPointerType((nint)unitBase, type, nodeOptions with { DefaultOpen = false });

        // Agent
        var agentModule = AgentModule.Instance();
        foreach (var agentId in Enum.GetValues<AgentId>())
        {
            var agent = agentModule->GetAgentByInternalId(agentId);
            if (agent == null || agent->AddonId != unitBase->Id)
                continue;

            ImGui.Text("Used by"u8);
            ImGuiUtils.SameLineSpace();
            _navigationService.DrawAgentLink(agentId);

            ImGui.SameLine();

            var agentType = _typeService.GetAgentType(agentId);

            _debugRenderer.DrawPointerType(agent, agentType, nodeOptions.WithAddress((nint)agent) with
            {
                DefaultOpen = false,
                DrawContextMenu = (nodeOptions, builder) =>
                {
                    var isPinned = _pinnedInstancesService.Contains(agentType);

                    builder.AddCopyName(agentId.ToString());
                    builder.AddCopyAddress((nint)agent);

                    builder.AddSeparator();

                    builder.Add(new ImGuiContextMenuEntry()
                    {
                        Visible = !_windowManager.Contains(win => win.WindowName == agentType.Name),
                        Label = _textService.Translate("ContextMenu.TabPopout"),
                        ClickCallback = () => _windowManager.Open(ActivatorUtilities.CreateInstance<PointerTypeWindow>(_serviceProvider, (nint)agent, agentType, string.Empty))
                    });

                    builder.Add(new ImGuiContextMenuEntry()
                    {
                        Visible = !isPinned,
                        Label = _textService.Translate("ContextMenu.PinnedInstances.Pin"),
                        ClickCallback = () => _pinnedInstancesService.Add((nint)agent, agentType)
                    });

                    builder.Add(new ImGuiContextMenuEntry()
                    {
                        Visible = isPinned,
                        Label = _textService.Translate("ContextMenu.PinnedInstances.Unpin"),
                        ClickCallback = () => _pinnedInstancesService.Remove(agentType)
                    });
                }
            });
        }

        // Callback
        var atkModule = RaptureAtkModule.Instance();
        if (atkModule->AddonCallbackMapping.TryGetValue(unitBase->Id, out var addonCallbackEntry, false))
        {
            var agentFound = false;

            if (addonCallbackEntry.AgentInterface != null)
            {
                foreach (var agentId in Enum.GetValues<AgentId>())
                {
                    var agent = agentModule->GetAgentByInternalId(agentId);
                    if (agent != addonCallbackEntry.AgentInterface)
                        continue;

                    agentFound = true;

                    ImGui.Text("Callback handler is"u8);
                    ImGuiUtils.SameLineSpace();
                    _navigationService.DrawAgentLink(agentId);
                    ImGuiUtils.SameLineSpace();
                    ImGui.Text($"with EventKind {addonCallbackEntry.EventKind}");

                    break;
                }
            }

            if (!agentFound && addonCallbackEntry.EventInterface != null)
            {
                ImGui.Text("Callback handler at"u8);
                ImGuiUtils.SameLineSpace();
                _debugRenderer.DrawAddress(addonCallbackEntry.EventInterface);
                ImGuiUtils.SameLineSpace();
                ImGui.Text($"with EventKind {addonCallbackEntry.EventKind}");
            }
        }

        // Host
        if (unitBase->HostId != 0)
        {
            var host = unitManager->GetAddonById(unitBase->HostId);
            if (host != null)
            {
                ImGui.Text("Embedded by"u8);
                ImGuiUtils.SameLineSpace();
                _navigationService.DrawAddonLink(host->Id, host->NameString);
            }
        }

        ImGuiUtilsEx.PaddedSeparator();

        short width;
        short height;
        unitBase->GetSize(&width, &height, false);

        short scaledWidth;
        short scaledHeight;
        unitBase->GetSize(&scaledWidth, &scaledHeight, true);

        ImGuiUtilsEx.PrintFieldValuePairs(
            ("Position", $"{unitBase->X}x{unitBase->Y}"),
            ("Size", $"{width}x{height}"),
            ("Scale", $"{unitBase->Scale * 100}%"),
            ("Size (scaled)", $"{scaledWidth}x{scaledHeight}"),
            ("Widget Count", $"{unitBase->UldManager.ObjectCount}"));

        if (ImGui.Button("Observe AtkValues"))
        {
            var addonName = unitBase->NameString;
            _windowManager.CreateOrOpen(
                addonName + " - AtkValues Observer",
                () => new AddonAtkValuesObserverWindow(_windowManager, _textService, _addonObserver, _addonLifecycle, _debugRenderer) { AddonName = addonName });
        }

        ImGuiUtilsEx.PaddedSeparator();

        if (unitBase->RootNode != null)
        {
            PrintNode(unitBase->RootNode, true, string.Empty, drawParams.NodePath, nodeOptions with { DefaultOpen = true });
        }

        if (unitBase->UldManager.NodeListCount > 0)
        {
            ImGui.Dummy(ImGuiHelpers.ScaledVector2(25));
            ImGui.Separator();

            using var nodeTree = _debugRenderer.DrawTreeNode(new NodeOptions()
            {
                AddressPath = nodeOptions.AddressPath,
                Title = "Node List",
                TitleColor = Color.FromUInt(0xFFFFAAAA),
            });

            if (!nodeTree)
                return;

            ImGui.InputTextWithHint("##NodeSearch", _textService.Translate("SearchBar.Hint"), ref _nodeQuery, 256, ImGuiInputTextFlags.AutoSelectAll);

            var j = 0;
            foreach (var node in unitBase->UldManager.Nodes)
            {
                if (node == null)
                {
                    j++;
                    continue;
                }

                if (!IsNodeMatchingSearch(node))
                {
                    j++;
                    continue;
                }

                PrintNode(node, false, $"[{j++}] ", drawParams.NodePath, nodeOptions with { DefaultOpen = false });
            }
        }
    }

    private static void LoadTypeMapping(OrderedDictionary<int, string> fields, string prefix, int offset, Type type)
    {
        foreach (var fieldInfo in type.GetFields(BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (fieldInfo.GetCustomAttribute<FieldOffsetAttribute>() is not { } fieldOffsetAttribute)
                continue;

            if (fieldInfo.IsAssembly
                && fieldInfo.GetCustomAttribute<FixedSizeArrayAttribute>() is FixedSizeArrayAttribute fixedSizeArrayAttribute
                && !fixedSizeArrayAttribute.IsString
                && !fixedSizeArrayAttribute.IsBitArray
                && fieldInfo.FieldType.GetCustomAttribute<InlineArrayAttribute>() is InlineArrayAttribute inlineArrayAttribute)
            {
                var innerType = fieldInfo.FieldType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)[0].FieldType;
                for (var i = 0; i < inlineArrayAttribute.Length; i++)
                {
                    LoadTypeMapping(fields, $"{prefix}{fieldInfo.Name[1..].FirstCharToUpper()}[{i}].", offset + fieldOffsetAttribute.Value + i * innerType.SizeOf(), innerType);
                }
            }
            else if (fieldInfo.FieldType.IsStruct())
            {
                LoadTypeMapping(fields, prefix + fieldInfo.Name + ".", offset + fieldOffsetAttribute.Value, fieldInfo.FieldType);
            }
            else
            {
                if (!fields.ContainsKey(offset + fieldOffsetAttribute.Value))
                {
                    fields[offset + fieldOffsetAttribute.Value] = prefix + fieldInfo.Name;
                }
            }
        }
    }

    private bool IsNodeMatchingSearch(AtkResNode* node)
    {
        if (string.IsNullOrEmpty(_nodeQuery))
            return true;

        if (("0x" + ((nint)node).ToString("X")).Contains(_nodeQuery, StringComparison.InvariantCultureIgnoreCase))
            return true;

        if (node->NodeId.ToString().Contains(_nodeQuery, StringComparison.InvariantCultureIgnoreCase))
            return true;

        if (node->GetNodeType().ToString().Contains(_nodeQuery, StringComparison.InvariantCultureIgnoreCase))
            return true;

        if (node->Type.ToString().Contains(_nodeQuery, StringComparison.InvariantCultureIgnoreCase))
            return true;

        if (node->GetNodeType() == NodeType.Component)
        {
            var componentNode = (AtkComponentNode*)node;
            var component = componentNode->Component;
            if (component != null &&
                component->UldManager.ResourceFlags.HasFlag(AtkUldManagerResourceFlag.Initialized) &&
                component->UldManager.BaseType == AtkUldManagerBaseType.Component &&
                ((AtkUldComponentInfo*)component->UldManager.Objects)->ComponentType.ToString().Contains(_nodeQuery, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public void DrawNode(AtkResNode* node)
    {
        var unitManager = RaptureAtkUnitManager.Instance();
        var unitBase = unitManager->GetAddonByNode(node);
        if (unitBase == null)
        {
            ImGui.Text($"Could not find addon with node {(nint)node:X}");
            return;
        }

        PrintNode(node, false, string.Empty, null, new() { DefaultOpen = true });
    }

    private void PrintNode(AtkResNode* node, bool printSiblings, string treePrefix, List<Pointer<AtkResNode>>? nodePath, NodeOptions nodeOptions)
    {
        if (node == null)
            return;

        nodeOptions = nodeOptions.WithAddress((nint)node);

        if (nodePath != null)
            ImGui.SetNextItemOpen(nodePath.Contains(node), ImGuiCond.Always);

        if ((int)node->Type < 1000)
            PrintSimpleNode(node, treePrefix, nodePath, nodeOptions);
        else
            PrintComponentNode(node, treePrefix, nodePath, nodeOptions);

        if (printSiblings)
        {
            var prevNode = node;
            while ((prevNode = prevNode->PrevSiblingNode) != null)
                PrintNode(prevNode, false, string.Empty, nodePath, nodeOptions);
        }
    }

    private void PrintSimpleNode(AtkResNode* node, string treePrefix, List<Pointer<AtkResNode>>? nodePath, NodeOptions nodeOptions)
    {
        using var rssb = new RentedSeStringBuilder();
        var titleBuilder = rssb.Builder
            .PushColorRgba(node->IsVisible() ? Color.Green : Color.Grey)
            .Append($"{treePrefix}[#{node->NodeId}] {node->Type} Node ({(nint)node:X})")
            .PopColor();

        AddNodeFieldSuffix(titleBuilder, node, nodeOptions);

        using var treeNode = _debugRenderer.DrawTreeNode(nodeOptions with
        {
            SeStringTitle = titleBuilder.ToReadOnlySeString(),
            DrawSeStringTreeNode = true,
            TitleColor = node->IsVisible() ? Color.Green : Color.Grey, // needed for the tree node arrow
            HighlightAddress = (nint)node,
            HighlightType = typeof(AtkResNode),
            DrawContextMenu = (nodeOptions, builder) =>
            {
                builder.Add(new ImGuiContextMenuEntry()
                {
                    Visible = !_windowManager.Contains(win => win.WindowName == nodeOptions.Title),
                    Label = _textService.Translate("ContextMenu.TabPopout"),
                    ClickCallback = () =>
                    {
                        _windowManager.Open(new NodeInspectorWindow(_windowManager, _textService, _addonObserver, this)
                        {
                            WindowName = nodeOptions.Title!,
                            NodeAddress = (nint)node
                        });
                    }
                });
            }
        });

        nodeOptions = nodeOptions.ConsumeTreeNodeOptions();

        if (!treeNode)
            return;

        if (nodePath != null && nodePath.Count > 0 && node == nodePath.Last())
            ImGui.SetScrollHereY();

        ImGui.Text("Node: "u8);
        ImGui.SameLine();
        _debugRenderer.DrawAddress(node);
        ImGui.SameLine();
        _debugRenderer.DrawPointerType((nint)node, typeof(AtkResNode), nodeOptions);

        ImGui.Text("NodeId:"u8);
        ImGui.SameLine();
        _debugRenderer.DrawNumeric(node->NodeId, typeof(uint), new NodeOptions() { HexOnShift = true });

        PrintProperties(node);
        PrintEvents(node, nodeOptions);
        PrintLabelSets(node);
        PrintAnimations(node);

        if (node->ChildNode != null)
            PrintNode(node->ChildNode, true, string.Empty, nodePath, nodeOptions);
    }

    private void PrintComponentNode(AtkResNode* resNode, string treePrefix, List<Pointer<AtkResNode>>? nodePath, NodeOptions nodeOptions)
    {
        var node = (AtkComponentNode*)resNode;
        var component = node->Component;

        var objectInfo = (AtkUldComponentInfo*)component->UldManager.Objects;
        if (objectInfo == null)
            return;

        using var rssb = new RentedSeStringBuilder();
        var titleBuilder = rssb.Builder
            .PushColorRgba(node->IsVisible() ? Color.Green : Color.Grey)
            .Append($"{treePrefix}[#{node->NodeId}] {objectInfo->ComponentType} Component Node (Node: {(nint)node:X}, Component: {(nint)component:X})")
            .PopColor();

        AddNodeFieldSuffix(titleBuilder, (AtkResNode*)node, nodeOptions);

        using var treeNode = _debugRenderer.DrawTreeNode(nodeOptions with
        {
            SeStringTitle = titleBuilder.ToReadOnlySeString(),
            DrawSeStringTreeNode = true,
            TitleColor = node->IsVisible() ? Color.Green : Color.Grey, // needed for the tree node arrow
            HighlightAddress = (nint)node,
            HighlightType = typeof(AtkComponentNode),
            DrawContextMenu = (nodeOptions, builder) =>
            {
                builder.Add(new ImGuiContextMenuEntry()
                {
                    Visible = !_windowManager.Contains(win => win.WindowName == nodeOptions.Title),
                    Label = _textService.Translate("ContextMenu.TabPopout"),
                    ClickCallback = () =>
                    {
                        _windowManager.Open(new NodeInspectorWindow(_windowManager, _textService, _addonObserver, this)
                        {
                            WindowName = nodeOptions.Title!,
                            NodeAddress = (nint)node
                        });
                    }
                });
            }
        });

        nodeOptions = nodeOptions.ConsumeTreeNodeOptions();

        if (!treeNode)
            return;

        if (nodePath != null && nodePath.Count > 0 && node == nodePath.Last())
            ImGui.SetScrollHereY();

        ImGui.Text("Node:"u8);
        ImGui.SameLine();
        _debugRenderer.DrawAddress(node);
        ImGui.SameLine();
        _debugRenderer.DrawPointerType((nint)node, typeof(AtkComponentNode), nodeOptions.WithAddress(1));

        ImGui.Text("Component:"u8);
        ImGui.SameLine();
        _debugRenderer.DrawAddress(component);
        ImGui.SameLine();
        _debugRenderer.DrawPointerType((nint)component, typeof(AtkComponentBase), nodeOptions.WithAddress(2));

        ImGuiUtilsEx.PrintFieldValuePairs(
            ("NodeId", node->NodeId.ToString()),
            ("NodeType", node->Type.ToString())
        );

        PrintProperties(resNode);
        PrintEvents(resNode, nodeOptions);
        PrintLabelSets(resNode);
        PrintAnimations(resNode);

        PrintNode(component->UldManager.RootNode, true, string.Empty, nodePath, nodeOptions);

        using var nodeTree = _debugRenderer.DrawTreeNode(new NodeOptions()
        {
            AddressPath = nodeOptions.AddressPath,
            Title = "Node List",
            TitleColor = Color.FromUInt(0xFFFFAAAA),
        });
        if (!nodeTree) return;

        for (var i = 0; i < component->UldManager.NodeListCount; i++)
        {
            PrintNode(component->UldManager.NodeList[i], false, $"[{i}] ", nodePath, nodeOptions);
        }
    }

    private void AddNodeFieldSuffix(SeStringBuilder titleBuilder, AtkResNode* node, NodeOptions nodeOptions)
    {
        if (nodeOptions.UnitBase.HasValue && _fieldMapping.TryGetValue(nodeOptions.UnitBase.Value.Value->NameString, out var fields))
        {
            var unitBaseAddress = (nint)nodeOptions.UnitBase.Value.Value;
            foreach (var (offset, name) in fields)
            {
                var fieldValue = *(nint*)(unitBaseAddress + offset);
                if (fieldValue != (nint)node)
                    continue;

                titleBuilder
                    .Append(' ')
                    .PushColorRgba(Color.Cyan)
                    .Append(name)
                    .PopColor();

                break;
            }
        }
    }

    private void PrintEvents(AtkResNode* node, NodeOptions nodeOptions)
    {
        if (node == null || node->AtkEventManager.Event == null)
        {
            return;
        }

        var unitBaseAddress = (nint)(nodeOptions.UnitBase.HasValue ? nodeOptions.UnitBase.Value.Value : null);
        var hasDifferentTarget = false;
        var hasDifferentListener = unitBaseAddress == 0;

        var evt = node->AtkEventManager.Event;
        while (evt != null)
        {
            if (evt->Target != node)
                hasDifferentTarget = true;

            if (unitBaseAddress != 0 && (nint)evt->Listener != unitBaseAddress)
                hasDifferentListener = true;

            evt = evt->NextEvent;
        }

        using var treeNode = ImRaii.TreeNode("Events", ImGuiTreeNodeFlags.SpanAvailWidth);
        if (!treeNode) return;

        var columns = 3;
        if (hasDifferentTarget) columns += 1;
        if (hasDifferentListener) columns += 1;

        using var table = ImRaii.Table("EventTable"u8, columns, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg);
        if (!table) return;

        ImGui.TableSetupColumn("EventType"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Param"u8, ImGuiTableColumnFlags.WidthFixed, 50);
        if (hasDifferentTarget) ImGui.TableSetupColumn("Target"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        if (hasDifferentListener) ImGui.TableSetupColumn("Listener"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Event"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        evt = node->AtkEventManager.Event;
        while (evt != null)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text($"{evt->State.EventType}");

            ImGui.TableNextColumn();
            ImGui.Text($"{evt->Param}");

            if (hasDifferentTarget)
            {
                ImGui.TableNextColumn();
                if (evt->Target == node)
                {
                    ImGui.Text("Node"u8);
                }
                else
                {
                    _debugRenderer.DrawAddress(evt->Target);
                }
            }

            if (hasDifferentListener)
            {
                ImGui.TableNextColumn();
                if ((nint)evt->Listener == unitBaseAddress)
                {
                    ImGui.Text("UnitBase"u8);
                }
                else
                {
                    _debugRenderer.DrawAddress(evt->Listener);
                }
            }

            ImGui.TableNextColumn();
            _debugRenderer.DrawPointerType(evt, typeof(AtkEvent), new() { AddressPath = new((nint)evt) });

            evt = evt->NextEvent;
        }
    }

    private void PrintLabelSets(AtkResNode* node)
    {
        if (node == null ||
            node->Timeline == null ||
            node->Timeline->Resource == null ||
            node->Timeline->Resource->LabelSetCount == 0 ||
            node->Timeline->Resource->LabelSets == null)
        {
            return;
        }

        using var treeNode = ImRaii.TreeNode("Label Sets", ImGuiTreeNodeFlags.SpanAvailWidth);
        if (!treeNode) return;

        if (ImGui.Button($"Export Timeline##{(nint)node:X}"))
        {
            ExportTimeline(node);
        }

        var labelSets = node->Timeline->Resource->LabelSets;

        ImGuiUtilsEx.PrintFieldValuePairs(
            ("StartFrameIdx", labelSets->StartFrameIdx.ToString()),
            ("EndFrameIdx", labelSets->EndFrameIdx.ToString()));

        var keyFrameGroup = labelSets->LabelKeyGroup;

        using var table = ImRaii.Table("LabelSetKeyFrameTable"u8, 7, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoHostExtendX);
        if (!table) return;

        ImGui.TableSetupColumn("Frame ID"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Speed Start"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Speed End"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Interpolation"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Label ID"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Jump Behavior"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Target Label ID"u8, ImGuiTableColumnFlags.WidthFixed);

        ImGui.TableHeadersRow();

        for (var i = 0; i < keyFrameGroup.KeyFrameCount; i++)
        {
            var keyFrame = keyFrameGroup.KeyFrames[i];

            ImGui.TableNextColumn();
            ImGui.Text($"{keyFrame.FrameIdx}");

            ImGui.TableNextColumn();
            ImGui.Text($"{keyFrame.SpeedCoefficient1:F2}");

            ImGui.TableNextColumn();
            ImGui.Text($"{keyFrame.SpeedCoefficient2:F2}");

            ImGui.TableNextColumn();
            ImGui.Text($"{keyFrame.Interpolation}");

            ImGui.TableNextColumn();
            ImGui.Text($"{keyFrame.Value.Label.LabelId}");

            ImGui.TableNextColumn();
            ImGui.Text($"{keyFrame.Value.Label.JumpBehavior}");

            ImGui.TableNextColumn();
            ImGui.Text($"{keyFrame.Value.Label.JumpLabelId}");
        }
    }

    private void PrintAnimations(AtkResNode* node)
    {
        if (node == null ||
            node->Timeline == null ||
            node->Timeline->Resource == null ||
            node->Timeline->Resource->AnimationCount == 0 ||
            node->Timeline->Resource->Animations == null)
        {
            return;
        }

        using var animationsTreeNode = ImRaii.TreeNode("Animation Groups", ImGuiTreeNodeFlags.SpanAvailWidth);
        if (!animationsTreeNode) return;

        if (ImGui.Button($"Export Timeline##{(nint)node:X}"))
        {
            ExportTimeline(node);
        }

        for (var i = 0; i < node->Timeline->Resource->AnimationCount; i++)
        {
            var animation = node->Timeline->Resource->Animations[i];

            using var keyGroupTreeNode = ImRaii.TreeNode($"[{i}] [Frames {animation.StartFrameIdx}-{animation.EndFrameIdx}]", ImGuiTreeNodeFlags.SpanAvailWidth);
            if (!keyGroupTreeNode) continue;

            var hasPosition = animation.KeyGroups[0].KeyFrameCount > 0;
            var hasRotation = animation.KeyGroups[1].KeyFrameCount > 0;
            var hasScale = animation.KeyGroups[2].KeyFrameCount > 0;
            var hasAlpha = animation.KeyGroups[3].KeyFrameCount > 0;
            var hasTint = animation.KeyGroups[4].KeyFrameCount > 0;
            var hasPartId = node->Type is NodeType.Image or NodeType.NineGrid or NodeType.ClippingMask && animation.KeyGroups[5].KeyFrameCount > 0;
            var hasTextColor = node->Type == NodeType.Text && animation.KeyGroups[5].KeyFrameCount > 0;
            var hasTextEdge = animation.KeyGroups[6].KeyFrameCount > 0;
            var hasTextLabel = animation.KeyGroups[7].KeyFrameCount > 0;

            var tableColumnCount = 1;
            if (hasPosition) tableColumnCount += 2;
            if (hasRotation) tableColumnCount += 1;
            if (hasScale) tableColumnCount += 2;
            if (hasAlpha) tableColumnCount += 1;
            if (hasTint) tableColumnCount += 2;
            if (hasPartId) tableColumnCount += 1;
            if (hasTextColor) tableColumnCount += 1;
            if (hasTextEdge) tableColumnCount += 1;
            if (hasTextLabel) tableColumnCount += 1;

            var groupHasAnyFrames = hasPosition || hasRotation || hasScale || hasAlpha || hasTint || hasPartId || hasTextColor || hasTextEdge || hasTextLabel;

            if (!groupHasAnyFrames)
            {
                ImGui.Text("Group has no keyframes"u8);
                continue;
            }

            using var keyFrameTable = ImRaii.Table("AnimationKeyFrameTable"u8, tableColumnCount, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoHostExtendX);
            if (!keyFrameTable) return;

            ImGui.TableSetupColumn("Frame ID"u8, ImGuiTableColumnFlags.WidthFixed);

            if (hasPosition)
            {
                ImGui.TableSetupColumn("X"u8, ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("Y"u8, ImGuiTableColumnFlags.WidthFixed);
            }

            if (hasRotation)
            {
                ImGui.TableSetupColumn("Rotation"u8, ImGuiTableColumnFlags.WidthFixed);
            }

            if (hasScale)
            {
                ImGui.TableSetupColumn("Scale"u8, ImGuiTableColumnFlags.WidthFixed);
            }

            if (hasAlpha)
            {
                ImGui.TableSetupColumn("Alpha"u8, ImGuiTableColumnFlags.WidthFixed);
            }

            if (hasTint)
            {
                ImGui.TableSetupColumn("Add Color"u8, ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("Multiply Color"u8, ImGuiTableColumnFlags.WidthFixed);
            }

            if (hasPartId)
            {
                ImGui.TableSetupColumn("Part ID"u8, ImGuiTableColumnFlags.WidthFixed);
            }

            if (hasTextColor)
            {
                ImGui.TableSetupColumn("Text Color"u8, ImGuiTableColumnFlags.WidthFixed);
            }

            if (hasTextEdge)
            {
                ImGui.TableSetupColumn("Text Edge"u8, ImGuiTableColumnFlags.WidthFixed);
            }

            if (hasTextLabel)
            {
                ImGui.TableSetupColumn("Text Label"u8, ImGuiTableColumnFlags.WidthFixed);
            }

            ImGui.TableHeadersRow();

            for (var frameIndex = animation.StartFrameIdx; frameIndex <= animation.EndFrameIdx; frameIndex++)
            {
                var groupHasFrame = false;
                foreach (var group in animation.KeyGroups)
                {
                    for (var keyFrameIndex = 0; keyFrameIndex < group.KeyFrameCount; keyFrameIndex++)
                    {
                        var keyFrame = group.KeyFrames[keyFrameIndex];

                        if (keyFrame.FrameIdx == frameIndex)
                        {
                            groupHasFrame = true;
                            break;
                        }
                    }

                    if (groupHasFrame) break;
                }

                if (!groupHasFrame) continue;

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.Text(frameIndex.ToString());

                for (var groupSelector = 0; groupSelector < 8; groupSelector++)
                {
                    var keyFrameGroup = animation.KeyGroups[groupSelector];

                    for (var keyFrameIndex = 0; keyFrameIndex < keyFrameGroup.KeyFrameCount; keyFrameIndex++)
                    {
                        var keyFrame = keyFrameGroup.KeyFrames[keyFrameIndex];
                        if (keyFrame.FrameIdx != frameIndex) continue;
                        var numericNodeOptions = new NodeOptions() { AddressPath = new([(nint)node, 1337, groupSelector, keyFrameIndex]) };
                        const int ColorEditWidth = 180;

                        switch (groupSelector)
                        {
                            case 0 when hasPosition: // Position
                                ImGui.TableNextColumn();
                                ImGui.AlignTextToFramePadding();
                                ImGuiUtils.DrawCopyableText(keyFrame.Value.Float2.Item1.ToString(CultureInfo.InvariantCulture));

                                ImGui.TableNextColumn();
                                ImGui.AlignTextToFramePadding();
                                ImGuiUtils.DrawCopyableText(keyFrame.Value.Float2.Item2.ToString(CultureInfo.InvariantCulture));
                                break;

                            case 1 when hasRotation: // Rotation
                                ImGui.TableNextColumn();
                                ImGui.AlignTextToFramePadding();
                                ImGuiUtils.DrawCopyableText(keyFrame.Value.Float.ToString(CultureInfo.InvariantCulture));
                                break;

                            case 2 when hasScale: // Scale
                                ImGui.TableNextColumn();
                                ImGui.AlignTextToFramePadding();
                                ImGuiUtils.DrawCopyableText(keyFrame.Value.Float2.Item1.ToString(CultureInfo.InvariantCulture));

                                ImGui.TableNextColumn();
                                ImGui.AlignTextToFramePadding();
                                ImGuiUtils.DrawCopyableText(keyFrame.Value.Float2.Item2.ToString(CultureInfo.InvariantCulture));
                                break;

                            case 3 when hasAlpha: // Alpha
                                ImGui.TableNextColumn();
                                ImGui.AlignTextToFramePadding();
                                ImGuiUtils.DrawCopyableText(keyFrame.Value.Byte.ToString(CultureInfo.InvariantCulture));
                                break;

                            case 4 when hasTint: // NodeTint
                                ImGui.TableNextColumn();
                                var addColor = new Vector3(keyFrame.Value.NodeTint.AddR, keyFrame.Value.NodeTint.AddG, keyFrame.Value.NodeTint.AddB) / 255f;
                                ImGui.SetNextItemWidth(ColorEditWidth);
                                ImGui.ColorEdit3(numericNodeOptions.GetKey("AddColor"), ref addColor);

                                ImGui.TableNextColumn();
                                var multiplyColor = new Vector3(keyFrame.Value.NodeTint.MultiplyRGB.R, keyFrame.Value.NodeTint.MultiplyRGB.G, keyFrame.Value.NodeTint.MultiplyRGB.B) / 255f;
                                ImGui.SetNextItemWidth(ColorEditWidth);
                                ImGui.ColorEdit3(numericNodeOptions.GetKey("MultiplyColor"), ref multiplyColor);
                                break;

                            case 5 when hasPartId: // PartId
                                ImGui.TableNextColumn();
                                ImGui.AlignTextToFramePadding();
                                ImGuiUtils.DrawCopyableText(keyFrame.Value.UShort.ToString(CultureInfo.InvariantCulture));
                                break;

                            case 5 when hasTextColor: // TextColor
                                ImGui.TableNextColumn();
                                var textColor = new Vector3(keyFrame.Value.RGB.R, keyFrame.Value.RGB.G, keyFrame.Value.RGB.B) / 255f;
                                ImGui.SetNextItemWidth(ColorEditWidth);
                                ImGui.ColorEdit3(numericNodeOptions.GetKey("TextColor"), ref textColor);
                                break;

                            case 6 when hasTextEdge: // TextEdge
                                ImGui.TableNextColumn();
                                var edgeColor = new Vector3(keyFrame.Value.RGB.R, keyFrame.Value.RGB.G, keyFrame.Value.RGB.B) / 255f;
                                ImGui.SetNextItemWidth(ColorEditWidth);
                                ImGui.ColorEdit3(numericNodeOptions.GetKey("TextEdgeColor"), ref edgeColor);
                                break;

                            case 7 when hasTextLabel: // TextLabel
                                ImGui.TableNextColumn();
                                ImGui.AlignTextToFramePadding();
                                ImGuiUtils.DrawCopyableText(keyFrame.Value.UShort.ToString(CultureInfo.InvariantCulture)); // Might not be the correct property UShort vs Short for this bucket
                                break;
                        }
                    }
                }
            }
        }
    }

    private void ExportTimeline(AtkResNode* node)
    {
        if (node == null)
            return;

        var timeline = node->Timeline;
        if (timeline == null || timeline->Resource == null)
            return;

        var timelineResource = timeline->Resource;
        var codeString = "new TimelineBuilder()\n";

        // Build Timeline LabelSets
        if (timelineResource->LabelSetCount > 0 && timelineResource->LabelSets is not null)
        {
            for (var i = 0; i < timelineResource->LabelSetCount; i++)
            {
                var labelSet = timelineResource->LabelSets[i];

                codeString += $"\t.BeginFrameSet({labelSet.StartFrameIdx}, {labelSet.EndFrameIdx})\n";

                for (var j = 0; j < labelSet.LabelKeyGroup.KeyFrameCount; j++)
                {
                    var keyFrame = labelSet.LabelKeyGroup.KeyFrames[j];

                    var label = keyFrame.Value.Label;
                    codeString += $"\t\t.AddLabel({keyFrame.FrameIdx}, {label.LabelId}, AtkTimelineJumpBehavior.{label.JumpBehavior}, {label.JumpLabelId})\n";
                }
            }

            codeString += $"\t.EndFrameSet()\n";
        }

        // Build Timeline Animations
        if (timeline->Resource->AnimationCount > 0 && timeline->Resource->Animations is not null)
        {
            for (var i = 0; i < timeline->Resource->AnimationCount; i++)
            {
                var animation = timeline->Resource->Animations[i];

                codeString += $"\t.BeginFrameSet({animation.StartFrameIdx}, {animation.EndFrameIdx})\n";
                var frameSetHasFrames = false;

                for (var groupSelector = 0; groupSelector < 8; groupSelector++)
                {
                    var keyGroup = animation.KeyGroups[groupSelector];

                    for (var j = 0; j < keyGroup.KeyFrameCount; j++)
                    {
                        var keyFrame = keyGroup.KeyFrames[j];
                        var keyFrameValue = keyFrame.Value;
                        frameSetHasFrames = true;

                        codeString += $"\t\t.AddFrame({keyFrame.FrameIdx}, ";

                        codeString += groupSelector switch
                        {
                            0 => $"position: new Vector2({keyFrameValue.Float2.Item1},{keyFrameValue.Float2.Item2}))\n",
                            1 => $"rotation: {keyFrameValue.Float})\n",
                            2 => $"scale: new Vector2({keyFrameValue.Float2.Item1}, {keyFrameValue.Float2.Item2}))\n",
                            3 => $"alpha: {keyFrameValue.Byte})\n",
                            4 => $"addColor: new Vector3({keyFrameValue.NodeTint.AddR}, {keyFrameValue.NodeTint.AddG}, {keyFrameValue.NodeTint.AddB}), multiplyColor: new Vector3({keyFrameValue.NodeTint.MultiplyRGB.R}, {keyFrameValue.NodeTint.MultiplyRGB.G}, {keyFrameValue.NodeTint.MultiplyRGB.B}))\n",
                            5 when node->Type is NodeType.Image or NodeType.NineGrid or NodeType.ClippingMask => $"partId: {keyFrameValue.UShort})\n",
                            5 when node->Type == NodeType.Text => $"textColor: new Vector3({keyFrameValue.RGB.R}, {keyFrameValue.RGB.G}, {keyFrameValue.RGB.B}))\n",
                            6 => $"textOutlineColor: new Vector3({keyFrameValue.RGB.R}, {keyFrameValue.RGB.G}, {keyFrameValue.RGB.B}))\n",
                            7 => string.Empty, // Not implemented yet
                            _ => string.Empty,
                        };
                    }
                }

                if (!frameSetHasFrames)
                {
                    codeString += $"\t\t.AddEmptyFrame({animation.StartFrameIdx})\n";
                }

                codeString += $"\t.EndFrameSet()\n";
            }
        }

        codeString += $"\t.Build();\n";

        ImGui.SetClipboardText(codeString);
    }

    private void PrintProperties(AtkResNode* node)
    {
        using var treeNode = ImRaii.TreeNode("Properties", ImGuiTreeNodeFlags.SpanAvailWidth);
        if (!treeNode) return;

        using var infoTable = ImRaii.Table("NodeInfoTable"u8, 2, ImGuiTableFlags.NoSavedSettings);
        if (!infoTable) return;

        ImGui.TableSetupColumn("Label"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Value"u8, ImGuiTableColumnFlags.WidthStretch);

        StartRow("Visible");
        var visible = node->NodeFlags.HasFlag(NodeFlags.Visible);
        if (ImGui.Checkbox("##Visible", ref visible))
        {
            if (visible)
                node->NodeFlags |= NodeFlags.Visible;
            else
                node->NodeFlags &= ~NodeFlags.Visible;
        }

        StartRow("Position");
        var position = new Vector2(node->X, node->Y);
        if (ImGui.DragFloat2("##Position", ref position, 1, 0, float.MaxValue, "%.0f"))
        {
            node->SetPositionFloat(position.X, position.Y);
        }

        StartRow("Size");
        var size = new Vector2(node->Width, node->Height);
        if (ImGui.DragFloat2("##Size", ref size, 1, 0, float.MaxValue, "%.0f"))
        {
            node->SetWidth((ushort)size.X);
            node->SetHeight((ushort)size.Y);
        }

        StartRow("Scale");
        var scale = new Vector2(node->ScaleX, node->ScaleY);
        if (ImGui.DragFloat2("##Scale", ref scale, 0.01f, 0, float.MaxValue, "%.2f"))
        {
            node->SetScale(scale.X, scale.Y);
        }

        StartRow("Origin");
        var origin = new Vector2(node->OriginX, node->OriginY);
        if (ImGui.DragFloat2("##Origin", ref origin, 1, 0, float.MaxValue, "%.0f"))
        {
            node->OriginX = origin.X;
            node->OriginY = origin.Y;
        }

        StartRow("Color");
        var color = Color.FromRGBA(node->Color.RGBA).ToVector();
        if (ImGui.ColorEdit4("##Color", ref color))
        {
            node->Color.RGBA = Color.FromVector4(color).ToUInt();
        }

        StartRow("Add Color");
        var addColor = new Vector3(node->AddRed / 255f, node->AddGreen / 255f, node->AddBlue / 255f);
        if (ImGui.ColorEdit3("##AddColor", ref addColor))
        {
            node->AddRed = (short)(addColor.X * 255f);
            node->AddGreen = (short)(addColor.Y * 255f);
            node->AddBlue = (short)(addColor.Z * 255f);
        }

        StartRow("Multiply Color");
        var multiplyColor = new Vector3(node->MultiplyRed, node->MultiplyGreen, node->MultiplyBlue);
        if (ImGui.DragFloat3("##MultiplyColor", ref multiplyColor, 1, 0, 100, "%.0f"))
        {
            node->MultiplyRed = (byte)multiplyColor.X;
            node->MultiplyGreen = (byte)multiplyColor.Y;
            node->MultiplyBlue = (byte)multiplyColor.Z;
        }

        var partId = 0u;
        switch (node->GetNodeType())
        {
            case NodeType.Image:
                var imageNode = (AtkImageNode*)node;
                StartRow("Asset");
                partId = imageNode->PartId;
                if (ImGuiUtilsEx.PartListSelector(_serviceProvider, imageNode->PartsList, ref partId))
                {
                    imageNode->PartId = (ushort)partId;
                    imageNode->DrawFlags |= 1;
                }
                break;

            case NodeType.Text:
                var textNode = (AtkTextNode*)node;
                StartRow("Text");
                var str = new ReadOnlySeString(textNode->NodeText.AsSpan());
                var macroCode = str.ToString();
                if (ImGui.Selectable(str.ToString() + $"##TextNodeText{(nint)node:X}"))
                {
                    var windowTitle = $"Text Node #{node->NodeId} (0x{(nint)node:X})";
                    _windowManager.CreateOrOpen(windowTitle, () => new SeStringInspectorWindow(_windowManager, _textService, _addonObserver, _serviceProvider)
                    {
                        String = str,
                        Language = _languageProvider.ClientLanguage,
                        WindowName = windowTitle,
                        Node = node,
                        Utf8String = &textNode->NodeText,
                    });
                }

                StartRow("Alignment");
                var alignmentType = textNode->AlignmentType;
                if (ImGuiUtilsEx.EnumCombo("##Alignment", ref alignmentType))
                {
                    textNode->AlignmentType = alignmentType;
                }

                StartRow("Font Type");
                var fontType = textNode->FontType;
                if (ImGuiUtilsEx.EnumCombo("##FontType", ref fontType))
                {
                    textNode->SetFont(fontType);
                }

                StartRow("Font Size");
                var fontSize = (int)textNode->FontSize;
                if (ImGui.InputInt("##FontSize", ref fontSize))
                {
                    textNode->FontSize = (byte)(fontSize < 0 ? 0 : fontSize > byte.MaxValue ? byte.MaxValue : fontSize);
                }

                StartRow("Text Color");
                var textColor = Color.FromRGBA(textNode->TextColor.RGBA).ToVector();
                if (ImGui.ColorEdit4("##TextColor", ref textColor))
                {
                    textNode->TextColor.RGBA = Color.FromVector4(textColor).ToUInt();
                }

                StartRow("Edge Color");
                var edgeColor = Color.FromRGBA(textNode->EdgeColor.RGBA).ToVector();
                if (ImGui.ColorEdit4("##EdgeColor", ref edgeColor))
                {
                    textNode->EdgeColor.RGBA = Color.FromVector4(edgeColor).ToUInt();
                }

                StartRow("Background Color");
                var backgroundColor = Color.FromRGBA(textNode->BackgroundColor.RGBA).ToVector();
                if (ImGui.ColorEdit4("##BackgroundColor", ref backgroundColor))
                {
                    textNode->BackgroundColor.RGBA = Color.FromVector4(backgroundColor).ToUInt();
                }

                StartRow("Text Flags");
                var textFlags = textNode->TextFlags;
                if (ImGuiUtilsEx.EnumCombo("##TextFlags", ref textFlags, true))
                {
                    textNode->TextFlags = textFlags;
                }

                break;

            case NodeType.Counter:
                var counterNode = (AtkCounterNode*)node;
                StartRow("Text");
                str = new ReadOnlySeString(counterNode->NodeText.AsSpan());
                macroCode = str.ToString();
                if (ImGui.Selectable(str.ToString() + $"##CounterNodeText{(nint)node:X}"))
                {
                    var windowTitle = $"Counter Node #{node->NodeId} (0x{(nint)node:X})";
                    _windowManager.CreateOrOpen(windowTitle, () => new SeStringInspectorWindow(_windowManager, _textService, _addonObserver, _serviceProvider)
                    {
                        String = str,
                        Language = _languageProvider.ClientLanguage,
                        WindowName = windowTitle,
                        Node = node,
                        Utf8String = &counterNode->NodeText,
                    });
                }
                break;

            case NodeType.NineGrid:
                var ngNode = (AtkNineGridNode*)node;
                StartRow("Asset");
                partId = ngNode->PartId;
                if (ImGuiUtilsEx.PartListSelector(_serviceProvider, ngNode->PartsList, ref partId))
                {
                    ngNode->PartId = partId;
                    ngNode->DrawFlags |= 1;
                }
                break;

            case NodeType.Collision:
                var collNode = (AtkCollisionNode*)node;
                StartRow("CollisionType");
                var collisionType = collNode->CollisionType;
                if (ImGuiUtilsEx.EnumCombo("##CollisionType", ref collisionType))
                {
                    collNode->CollisionType = collisionType;
                }

                StartRow("Uses");
                var uses = (int)collNode->Uses;
                if (ImGui.InputInt("##Uses", ref uses))
                {
                    collNode->Uses = (ushort)(uses < 0 ? 0 : uses > ushort.MaxValue ? ushort.MaxValue : uses);
                }
                break;

            case NodeType.ClippingMask:
                var cmNode = (AtkClippingMaskNode*)node;
                StartRow("Asset");
                partId = cmNode->PartId;
                if (ImGuiUtilsEx.PartListSelector(_serviceProvider, cmNode->PartsList, ref partId))
                {
                    cmNode->PartId = (ushort)partId;
                    cmNode->DrawFlags |= 1;
                }
                break;

            case NodeType.Component:
                var componentNode = (AtkComponentNode*)node;
                var component = componentNode->Component;
                if (component != null &&
                    component->UldManager.ResourceFlags.HasFlag(AtkUldManagerResourceFlag.Initialized) &&
                    component->UldManager.BaseType == AtkUldManagerBaseType.Component)
                {
                    switch (((AtkUldComponentInfo*)component->UldManager.Objects)->ComponentType)
                    {
                        case ComponentType.Icon:
                            var iconComp = (AtkComponentIcon*)component;
                            StartRow("IconId");
                            var iconId = (int)iconComp->IconId;
                            if (ImGui.InputInt("##IconId", ref iconId))
                            {
                                iconComp->LoadIcon((uint)(iconId < 0 ? 0 : iconId > ushort.MaxValue ? ushort.MaxValue : iconId));
                            }

                            StartRow("Flags");
                            var iconFlags = iconComp->Flags;
                            if (ImGuiUtilsEx.EnumCombo("##IconFlags", ref iconFlags, true))
                            {
                                iconComp->Flags = iconFlags;
                                iconComp->UpdateIndicator();
                            }
                            break;
                    }
                }
                break;
        }
    }

    private static void StartRow(string label)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.Text(label);
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(200);
    }
}
