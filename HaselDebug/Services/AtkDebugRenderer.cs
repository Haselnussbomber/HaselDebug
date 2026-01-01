using System.Globalization;
using System.Text;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselDebug.Config;
using HaselDebug.Extensions;
using HaselDebug.Service;
using HaselDebug.Utils;
using HaselDebug.Windows;

namespace HaselDebug.Services;

[RegisterSingleton, AutoConstruct]
public unsafe partial class AtkDebugRenderer
{
    private const float MinWidth = 50f;
    private const float SplitterWidth = 4f;

    private readonly IServiceProvider _serviceProvider;
    private readonly PluginConfig _pluginConfig;
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly TypeService _typeService;
    private readonly DebugRenderer _debugRenderer;
    private readonly TextService _textService;
    private readonly WindowManager _windowManager;
    private readonly LanguageProvider _languageProvider;
    private readonly AddonObserver _addonObserver;
    private readonly PinnedInstancesService _pinnedInstancesService;
    private readonly NavigationService _navigationService;
    private readonly ProcessInfoService _processInfoService;

    private InspectorContext _ctx;
    private string _nodeQuery = string.Empty;
    private List<SearchToken>? _searchTokens;
    private float _sidebarWidth = 350f;
    private AtkResNode* _selectedNode;

    public void DrawInspector(InspectorContext ctx)
    {
        _ctx = ctx;

        if (ctx.Addon == null)
            return;

        var unitBase = ctx.Addon;
        if (!_processInfoService.IsPointerValid(unitBase))
        {
            ImGui.Text($"Could not find addon with id {ctx.AddonId} or name {ctx.AddonName}");
            return;
        }

        // reset selected node if it isn't from this addon
        if (RaptureAtkUnitManager.Instance()->GetAddonByNode(_selectedNode) != _ctx.Addon)
            _selectedNode = null;

        var windowSize = ImGui.GetContentRegionAvail();
        var mainContentWidth = windowSize.X - _sidebarWidth - ImGui.GetStyle().ItemSpacing.X;
        var sidebarWidth = _sidebarWidth;

        using (var child = ImRaii.Child("MainContent"u8, new Vector2(mainContentWidth, 0), true))
        {
            DrawMainContent();
        }

        ImGui.SameLine();
        var splitterPos = ImGui.GetCursorPos() - new Vector2(SplitterWidth / 2f + ImGui.GetStyle().ItemSpacing.X / 2f, 0);

        using (var child = ImRaii.Child("Sidebar"u8, new Vector2(sidebarWidth, 0), true))
        {
            DrawSidebar();
        }

        ImGui.SetCursorPos(splitterPos);

        using (ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 0))
        using (ImRaii.PushColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.Border)))
        {
            ImGui.Button("##splitter"u8, new Vector2(SplitterWidth, -1));
        }

        if (ImGui.IsItemHovered())
            ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEw);

        if (ImGui.IsItemActive())
        {
            _sidebarWidth -= ImGui.GetIO().MouseDelta.X;
            if (_sidebarWidth < MinWidth) _sidebarWidth = MinWidth;
            if (_sidebarWidth > windowSize.X - MinWidth) _sidebarWidth = windowSize.X - MinWidth;
        }
    }

    private void DrawMainContent()
    {
        var nodeOptions = new NodeOptions()
        {
            AddressPath = new([(nint)_ctx.Addon, 1]),
            DefaultOpen = true,
            UnitBase = _ctx.Addon,
        };

        DrawAddonInfo(nodeOptions);
        ImGuiUtilsEx.PaddedSeparator();

        using var tabBar = ImRaii.TabBar(nodeOptions.GetKey("TabBar"));
        if (!tabBar) return;

        DrawAddonNodes(nodeOptions);
        DrawAddonStruct(nodeOptions);
        DrawAddonAgent(nodeOptions);
        DrawAddonAtkValues(nodeOptions);
    }

    private void DrawSidebar()
    {
        var nodeOptions = new NodeOptions()
        {
            AddressPath = new([(nint)_ctx.Addon, 2]),
            DefaultOpen = true,
            UnitBase = _ctx.Addon,
        };

        PrintInfoTable(_selectedNode, nodeOptions);
    }

    private void DrawAddonInfo(NodeOptions nodeOptions)
    {
        var addon = _ctx.Addon;

        ImGuiUtils.DrawCopyableText(addon->NameString);

        ImGui.SameLine();

        var isVisible = addon->IsVisible;
        using (ImRaii.PushColor(ImGuiCol.Text, isVisible ? 0xFF00FF00 : Color.From(ImGuiCol.TextDisabled).ToUInt()))
        {
            ImGui.Text(isVisible ? "Visible" : "Not Visible");
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            ImGui.SetTooltip("Toggle visibility"u8);
        }
        if (ImGui.IsItemClicked())
        {
            addon->IsVisible = !isVisible;
        }

        ImGuiUtilsEx.PaddedSeparator();

        var pos = addon->Position;
        var size = addon->Size;
        var scaledSize = addon->ScaledSize;

        ImGuiUtilsEx.PrintFieldValuePairs(
            ("Address", ((nint)addon).ToString("X")),
            ("Position", $"{pos.X}x{pos.Y}"),
            ("Size", $"{size.X}x{size.Y}"),
            ("Scale", $"{addon->Scale * 100}%"),
            ("Size (scaled)", $"{scaledSize.X}x{scaledSize.Y}"),
            ("Widget Count", $"{addon->UldManager.ObjectCount}"));
    }

    public void DrawAddonNodes(NodeOptions nodeOptions)
    {
        using var tabItem = ImRaii.TabItem("Nodes"u8);
        if (!tabItem) return;

        using var tabChild = ImRaii.Child("TabChild"u8, new Vector2(-1));
        if (!tabChild) return;

        var unitBase = _ctx.Addon;

        if (unitBase->RootNode != null)
        {
            PrintNode(unitBase->RootNode, true, string.Empty, nodeOptions with { DefaultOpen = true });
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

            if (ImGui.InputTextWithHint("##NodeSearch"u8, _textService.Translate("SearchBar.Hint"), ref _nodeQuery, 256, ImGuiInputTextFlags.AutoSelectAll))
            {
                UpdateSearchTokens();
            }

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

                PrintNode(node, false, $"[{j++}] ", nodeOptions with { DefaultOpen = false });
            }
        }
    }

    private void DrawAddonStruct(NodeOptions nodeOptions)
    {
        using var tabItem = ImRaii.TabItem(_ctx.AddonType.Name);
        if (!tabItem) return;

        _debugRenderer.DrawPointerType((nint)_ctx.Addon, _ctx.AddonType, nodeOptions with { DefaultOpen = true });
    }

    private void DrawAddonAgent(NodeOptions nodeOptions)
    {
        if (_ctx.AgentId is not AgentId agentId)
            return;

        var agentType = _typeService.GetAgentType(agentId);

        using var tabItem = ImRaii.TabItem("Agent" + agentId.ToString());
        if (!tabItem) return;

        var addon = _ctx.Addon;
        var agent = _ctx.Agent;

        // Callback
        var atkModule = RaptureAtkModule.Instance();
        if (atkModule->AddonCallbackMapping.TryGetValue(addon->Id, out var addonCallbackEntry, false))
        {
            var agentFound = false;

            if (addonCallbackEntry.AgentInterface != null)
            {
                var agentModule = AgentModule.Instance();
                foreach (var cbAgentId in Enum.GetValues<AgentId>())
                {
                    var cbAgent = agentModule->GetAgentByInternalId(cbAgentId);
                    if (cbAgent != addonCallbackEntry.AgentInterface)
                        continue;

                    agentFound = true;

                    ImGui.Text("Callback handler is"u8);
                    ImGuiUtils.SameLineSpace();
                    _navigationService.DrawAgentLink(cbAgentId);
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
        if (addon->HostId != 0)
        {
            var host = RaptureAtkUnitManager.Instance()->GetAddonById(addon->HostId);
            if (host != null)
            {
                ImGui.Text("Embedded by"u8);
                ImGuiUtils.SameLineSpace();
                _navigationService.DrawAddonLink(host->Id, host->NameString);
            }
        }

        _debugRenderer.DrawPointerType(agent, agentType, nodeOptions.WithAddress((nint)agent) with
        {
            DefaultOpen = true,
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

    private void DrawAddonAtkValues(NodeOptions nodeOptions)
    {
        var count = _ctx.Addon->AtkValuesCount;

        if (_ctx.Addon->AtkValues == null || count == 0)
            return;

        using var tabItem = ImRaii.TabItem($"AtkValues ({count})##AtkValues");
        if (!tabItem) return;

        using var tabChild = ImRaii.Child("TabChild"u8, new Vector2(-1));
        if (!tabChild) return;

        if (ImGui.Button("Observe AtkValues"u8))
        {
            var addonName = _ctx.Addon->NameString;
            _windowManager.CreateOrOpen(
                addonName + " - AtkValues Observer",
                () => new AddonAtkValuesObserverWindow(_windowManager, _textService, _addonObserver, _addonLifecycle, _debugRenderer) { AddonName = addonName });
        }

        _debugRenderer.DrawAtkValues(_ctx.Addon->AtkValues, count, nodeOptions, false);
    }

    public void DrawNode(AtkResNode* node)
    {
        var unitManager = RaptureAtkUnitManager.Instance();
        var unitBase = unitManager->AtkUnitManager.GetAddonByNodeSafe(node);
        if (unitBase == null)
        {
            ImGui.Text($"Could not find addon with node {(nint)node:X}");
            return;
        }

        PrintNode(node, false, string.Empty, new() { DefaultOpen = true });
    }

    private void PrintNode(AtkResNode* node, bool printSiblings, string treePrefix, NodeOptions nodeOptions)
    {
        if (node == null)
            return;

        // set default selected node to the first one that's rendered
        if (_selectedNode == null)
            _selectedNode = node;

        nodeOptions = nodeOptions.WithAddress((nint)node);

        if (_ctx.NodePath != null)
            ImGui.SetNextItemOpen(_ctx.NodePath.Contains(node), ImGuiCond.Always);

        switch (node->GetNodeType())
        {
            case NodeType.Component:
                PrintComponentNode(node, treePrefix, nodeOptions);
                break;
            default:
                PrintSimpleNode(node, treePrefix, nodeOptions);
                break;
        }

        if (printSiblings)
        {
            var prevNode = node;
            while ((prevNode = prevNode->PrevSiblingNode) != null)
                PrintNode(prevNode, false, string.Empty, nodeOptions);
        }
    }

    private void PrintSimpleNode(AtkResNode* node, string treePrefix, NodeOptions nodeOptions)
    {
        using var rssb = new RentedSeStringBuilder();

        SeStringBuilder titleBuilder;
        if (_typeService.CustomNodeTypes?.TryGetValue((nint)node, out var type) ?? false)
        {
            var name = type.ReadableTypeName();
            if (_pluginConfig.SpacesInKTKNames)
                name = name.SplitCamelCase();
            titleBuilder = rssb.Builder
                               .PushColorRgba(node->IsVisible() ? Color.Green : Color.Grey)
                               .Append($"{treePrefix}[#{node->NodeId}] {name} ({(nint)node:X})")
                               .PopColor();
        }
        else
        {
            titleBuilder = rssb.Builder
                               .PushColorRgba(node->IsVisible() ? Color.Green : Color.Grey)
                               .Append($"{treePrefix}[#{node->NodeId}] {node->Type} ({(nint)node:X})")
                               .PopColor();
        }

        AddNodeFieldSuffix(titleBuilder, node, nodeOptions);

        using var treeNode = _debugRenderer.DrawTreeNode(nodeOptions with
        {
            SeStringTitle = titleBuilder.ToReadOnlySeString(),
            DrawSeStringTreeNode = true,
            TitleColor = node->IsVisible() ? Color.Green : Color.Grey, // needed for the tree node arrow
            HighlightAddress = (nint)node,
            HighlightType = typeof(AtkResNode),
            OnClicked = () => _selectedNode = node,
            IsLeafNode = node->ChildNode == null,
            IsSelected = _selectedNode == node,
            /*
            DrawContextMenu = (nodeOptions, builder) =>
            {
                builder.Add(new ImGuiContextMenuEntry()
                {
                    Visible = !_windowManager.Contains(win => win.WindowName == nodeOptions.SeStringTitle.ToString()),
                    Label = _textService.Translate("ContextMenu.TabPopout"),
                    ClickCallback = () =>
                    {
                        _windowManager.Open(new NodeInspectorWindow(_windowManager, _textService, this)
                        {
                            WindowName = nodeOptions.SeStringTitle?.ToString() ?? $"Node at 0x{(nint)node:X}",
                            NodeAddress = (nint)node
                        });
                    }
                });
            }
            */
        });

        nodeOptions = nodeOptions.ConsumeTreeNodeOptions();

        if (!treeNode)
            return;

        if (_ctx.NodePath != null && _ctx.NodePath.Count > 0 && node == _ctx.NodePath.Last())
            ImGui.SetScrollHereY();

        if (node->ChildNode != null)
            PrintNode(node->ChildNode, true, string.Empty, nodeOptions);
    }

    private void PrintComponentNode(AtkResNode* resNode, string treePrefix, NodeOptions nodeOptions)
    {
        var node = (AtkComponentNode*)resNode;
        var component = node->Component;

        var objectInfo = (AtkUldComponentInfo*)component->UldManager.Objects;
        if (objectInfo == null)
            return;

        using var rssb = new RentedSeStringBuilder();

        SeStringBuilder titleBuilder;
        if (_typeService.CustomNodeTypes?.TryGetValue((nint)node, out var type) ?? false)
        {
            var name = type.ReadableTypeName();
            if (_pluginConfig.SpacesInKTKNames)
                name = name.SplitCamelCase();
            titleBuilder = rssb.Builder
                               .PushColorRgba(node->IsVisible() ? Color.Green : Color.Grey)
                               .Append($"{treePrefix}[#{node->NodeId}] {name} (Node: {(nint)node:X}, Component: {(nint)component:X})")
                               .PopColor();
        }
        else
        {
            titleBuilder = rssb.Builder
                               .PushColorRgba(node->IsVisible() ? Color.Green : Color.Grey)
                               .Append($"{treePrefix}[#{node->NodeId}] {objectInfo->ComponentType} Component Node (Node: {(nint)node:X}, Component: {(nint)component:X})")
                               .PopColor();
        }

        AddNodeFieldSuffix(titleBuilder, (AtkResNode*)node, nodeOptions);

        using var treeNode = _debugRenderer.DrawTreeNode(nodeOptions with
        {
            SeStringTitle = titleBuilder.ToReadOnlySeString(),
            DrawSeStringTreeNode = true,
            TitleColor = node->IsVisible() ? Color.Green : Color.Grey, // needed for the tree node arrow
            HighlightAddress = (nint)node,
            HighlightType = typeof(AtkComponentNode),
            OnClicked = () => _selectedNode = resNode,
            IsLeafNode = component->UldManager.NodeListCount == 0,
            IsSelected = _selectedNode == node,
            /*
            DrawContextMenu = (nodeOptions, builder) =>
            {
                builder.Add(new ImGuiContextMenuEntry()
                {
                    Visible = !_windowManager.Contains(win => win.WindowName == nodeOptions.SeStringTitle.ToString()),
                    Label = _textService.Translate("ContextMenu.TabPopout"),
                    ClickCallback = () =>
                    {
                        _windowManager.Open(new NodeInspectorWindow(_windowManager, _textService, this)
                        {
                            WindowName = nodeOptions.SeStringTitle?.ToString() ?? $"Node at 0x{(nint)node:X}",
                            NodeAddress = (nint)node
                        });
                    }
                });
            }
            */
        });

        nodeOptions = nodeOptions.ConsumeTreeNodeOptions();

        if (!treeNode)
            return;

        if (_ctx.NodePath != null && _ctx.NodePath.Count > 0 && node == _ctx.NodePath.Last())
            ImGui.SetScrollHereY();

        PrintNode(component->UldManager.RootNode, true, string.Empty, nodeOptions);

        using var nodeTree = _debugRenderer.DrawTreeNode(new NodeOptions()
        {
            AddressPath = nodeOptions.AddressPath,
            Title = "Node List",
            TitleColor = Color.FromUInt(0xFFFFAAAA),
        });
        if (!nodeTree) return;

        for (var i = 0; i < component->UldManager.NodeListCount; i++)
        {
            PrintNode(component->UldManager.NodeList[i], false, $"[{i}] ", nodeOptions);
        }
    }

    private void AddNodeFieldSuffix(SeStringBuilder titleBuilder, AtkResNode* node, NodeOptions nodeOptions)
    {
        if (nodeOptions.UnitBase.HasValue && _ctx.FieldMapping != null)
        {
            var unitBaseAddress = (nint)nodeOptions.UnitBase.Value.Value;
            foreach (var (offset, (name, type)) in _ctx.FieldMapping)
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

    private void PrintInfoTable(AtkResNode* node, NodeOptions nodeOptions)
    {
        if (node == null)
            return;

        using var tabBar = ImRaii.TabBar(nodeOptions.GetKey("TabBar"));
        if (!tabBar) return;

        PrintProperties(node);
        PrintNodeInfo(node, nodeOptions);
        PrintComponentInfo(node, nodeOptions);
        PrintEvents(node, nodeOptions);
        PrintLabelSets(node);
        PrintAnimations(node);
    }

    private void PrintNodeInfo(AtkResNode* node, NodeOptions nodeOptions)
    {
        using var tabItem = ImRaii.TabItem("Node"u8);
        if (!tabItem) return;

        ImGui.Text("Address: "u8);
        ImGui.SameLine();
        _debugRenderer.DrawAddress(node);

        ImGui.Text("NodeType:"u8);
        ImGui.SameLine();
        ImGui.Text(node->Type.ToString());

        _debugRenderer.DrawPointerType((nint)node, typeof(AtkResNode), nodeOptions with { DefaultOpen = true });
    }

    private void PrintComponentInfo(AtkResNode* node, NodeOptions nodeOptions)
    {
        if (node->GetNodeType() != NodeType.Component)
            return;

        using var tabItem = ImRaii.TabItem("Component"u8);
        if (!tabItem) return;

        var componentNode = (AtkComponentNode*)node;
        var component = componentNode->Component;

        ImGui.Text("Address:"u8);
        ImGui.SameLine();
        _debugRenderer.DrawAddress(component);

        _debugRenderer.DrawPointerType((nint)component, typeof(AtkComponentBase), nodeOptions.WithAddress(2) with { DefaultOpen = true });
    }

    private void PrintProperties(AtkResNode* node)
    {
        using var tabItem = ImRaii.TabItem("Properties"u8);
        if (!tabItem) return;

        using var infoTable = ImRaii.Table("NodeInfoTable"u8, 2, ImGuiTableFlags.NoSavedSettings);
        if (!infoTable) return;

        ImGui.TableSetupColumn("Label"u8, ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Value"u8, ImGuiTableColumnFlags.WidthStretch);

        StartRow("Visible");
        var visible = node->NodeFlags.HasFlag(NodeFlags.Visible);
        if (ImGui.Checkbox("##Visible"u8, ref visible))
        {
            if (visible)
                node->NodeFlags |= NodeFlags.Visible;
            else
                node->NodeFlags &= ~NodeFlags.Visible;
        }

        StartRow("Position");
        var position = new Vector2(node->X, node->Y);
        if (ImGui.DragFloat2("##Position"u8, ref position, 1, 0, float.MaxValue, "%.0f"))
        {
            node->SetPositionFloat(position.X, position.Y);
        }

        StartRow("Size");
        var size = new Vector2(node->Width, node->Height);
        if (ImGui.DragFloat2("##Size"u8, ref size, 1, 0, float.MaxValue, "%.0f"))
        {
            node->SetWidth((ushort)size.X);
            node->SetHeight((ushort)size.Y);
        }

        StartRow("Scale");
        var scale = new Vector2(node->ScaleX, node->ScaleY);
        if (ImGui.DragFloat2("##Scale"u8, ref scale, 0.01f, 0, float.MaxValue, "%.2f"))
        {
            node->SetScale(scale.X, scale.Y);
        }

        StartRow("Origin");
        var origin = new Vector2(node->OriginX, node->OriginY);
        if (ImGui.DragFloat2("##Origin"u8, ref origin, 1, 0, float.MaxValue, "%.0f"))
        {
            node->OriginX = origin.X;
            node->OriginY = origin.Y;
        }

        StartRow("Color");
        var color = Color.FromRGBA(node->Color.RGBA).ToVector();
        if (ImGui.ColorEdit4("##Color"u8, ref color))
        {
            node->Color.RGBA = Color.FromVector4(color).ToUInt();
        }

        StartRow("Add Color");
        var addColor = new Vector3(node->AddRed / 255f, node->AddGreen / 255f, node->AddBlue / 255f);
        if (ImGui.ColorEdit3("##AddColor"u8, ref addColor))
        {
            node->AddRed = (short)(addColor.X * 255f);
            node->AddGreen = (short)(addColor.Y * 255f);
            node->AddBlue = (short)(addColor.Z * 255f);
        }

        StartRow("Multiply Color");
        var multiplyColor = new Vector3(node->MultiplyRed, node->MultiplyGreen, node->MultiplyBlue);
        if (ImGui.DragFloat3("##MultiplyColor"u8, ref multiplyColor, 1, 0, 100, "%.0f"))
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
                if (ImGui.InputInt("##FontSize"u8, ref fontSize))
                {
                    textNode->FontSize = (byte)(fontSize < 0 ? 0 : fontSize > byte.MaxValue ? byte.MaxValue : fontSize);
                }

                StartRow("Text Color");
                var textColor = Color.FromRGBA(textNode->TextColor.RGBA).ToVector();
                if (ImGui.ColorEdit4("##TextColor"u8, ref textColor))
                {
                    textNode->TextColor.RGBA = Color.FromVector4(textColor).ToUInt();
                }

                StartRow("Edge Color");
                var edgeColor = Color.FromRGBA(textNode->EdgeColor.RGBA).ToVector();
                if (ImGui.ColorEdit4("##EdgeColor"u8, ref edgeColor))
                {
                    textNode->EdgeColor.RGBA = Color.FromVector4(edgeColor).ToUInt();
                }

                StartRow("Background Color");
                var backgroundColor = Color.FromRGBA(textNode->BackgroundColor.RGBA).ToVector();
                if (ImGui.ColorEdit4("##BackgroundColor"u8, ref backgroundColor))
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
                if (ImGui.InputInt("##Uses"u8, ref uses))
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
                            if (ImGui.InputInt("##IconId"u8, ref iconId))
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

    private void PrintEvents(AtkResNode* node, NodeOptions nodeOptions)
    {
        if (node == null || node->AtkEventManager.Event == null)
            return;

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

        using var tabItem = ImRaii.TabItem("Events"u8);
        if (!tabItem) return;

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

        using var tabItem = ImRaii.TabItem("Label Sets"u8);
        if (!tabItem) return;

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

        using var tabItem = ImRaii.TabItem("Animation Groups"u8);
        if (!tabItem) return;

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

    private static void StartRow(string label)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.Text(label);
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(200);
    }

    private void UpdateSearchTokens()
    {
        if (string.IsNullOrWhiteSpace(_nodeQuery))
        {
            _searchTokens = null;
            return;
        }

        _searchTokens = SearchTokenParser.Parse(_nodeQuery);
    }

    private bool IsNodeMatchingSearch(AtkResNode* node)
    {
        if (_searchTokens == null || _searchTokens.Count == 0)
            return true;

        return _searchTokens.All(token =>
        {
            var match = false;

            if (string.IsNullOrEmpty(token.Key))
            {
                match |= MatchesNodeId(node, token.Value.StartsWith('#') ? token.Value[1..] : token.Value);
                match |= MatchesNodeType(node, token.Value);
                match |= MatchesNodeAddress(node, token.Value);
            }
            else
            {
                switch (token.Key)
                {
                    case "id":
                        match = MatchesNodeId(node, token.Value);
                        break;

                    case "type":
                        match = MatchesNodeType(node, token.Value);
                        break;

                    case "addr":
                    case "address":
                        match = MatchesNodeAddress(node, token.Value);
                        break;
                }
            }

            return token.IsExclude ? !match : match;
        });

        static bool MatchesNodeId(AtkResNode* node, string value)
        {
            return uint.TryParse(value, out var nodeId) && node->NodeId == nodeId;
        }

        static bool MatchesNodeType(AtkResNode* node, string value)
        {
            if (node->GetNodeType().ToString().Contains(value, StringComparison.InvariantCultureIgnoreCase))
                return true;

            if (node->GetNodeType() == NodeType.Component)
            {
                var componentNode = (AtkComponentNode*)node;
                var component = componentNode->Component;
                if (component != null
                    && component->UldManager.ResourceFlags.HasFlag(AtkUldManagerResourceFlag.Initialized)
                    && component->UldManager.BaseType == AtkUldManagerBaseType.Component
                    && ((AtkUldComponentInfo*)component->UldManager.Objects)->ComponentType.ToString().Contains(value, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        static bool MatchesNodeAddress(AtkResNode* node, string value)
        {
            nint address;

            if (value.StartsWith("0x"))
            {
                if (nint.TryParse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out address))
                    return (nint)node == address;
            }

            return nint.TryParse(value, out address) && (nint)node == address;
        }
    }
}

public unsafe struct InspectorContext
{
    public ushort AddonId { get; }
    public string? AddonName { get; }

    public AtkUnitBase* Addon { get; }
    public Type AddonType { get; }
    public AgentId? AgentId { get; }
    public Type AgentType { get; }
    public AgentInterface* Agent { get; }

    public List<Pointer<AtkResNode>>? NodePath { get; set; }
    public bool Border { get; set; } = true;
    public bool UseNavigationService { get; set; } = true;

    public InspectorContext(string addonName, ushort addonId)
    {
        AddonId = addonId;
        AddonName = addonName;
        Addon = GetAtkUnitBase();
        AddonType = GetAddonType();
        AgentId = GetAgentId();
        AgentType = GetAgentType();
        Agent = GetAgentInterface();
    }

    private unsafe AtkUnitBase* GetAtkUnitBase()
    {
        AtkUnitBase* firstMatch = null;
        var matchCount = 0;

        Span<byte> name = stackalloc byte[32];
        var written = Encoding.UTF8.GetBytes(AddonName, name);
        name[written] = 0; // Null terminator
        var nameSpan = name[..(written + 1)];

        foreach (AtkUnitBase* unitBase in RaptureAtkUnitManager.Instance()->AllLoadedUnitsList.Entries)
        {
            if (unitBase == null || !unitBase->Name.StartsWith(nameSpan))
                continue;

            if (AddonId != 0 && unitBase->Id == AddonId)
                return unitBase;

            if (firstMatch == null)
                firstMatch = unitBase;

            matchCount++;
        }

        return matchCount == 1 ? firstMatch : null;
    }

    private Type GetAddonType()
    {
        if (Addon == null)
            return typeof(AtkUnitBase);

        if (!ServiceLocator.TryGetService<TypeService>(out var typeService))
            return typeof(AtkUnitBase);

        return typeService.GetAddonType(Addon->NameString);
    }

    private unsafe AgentId? GetAgentId()
    {
        if (Addon == null)
            return null;

        var agentModule = AgentModule.Instance();

        foreach (var agentId in Enum.GetValues<AgentId>())
        {
            var agent = agentModule->GetAgentByInternalId(agentId);
            if (agent != null && agent->AddonId == Addon->Id)
                return agentId;
        }

        return null;
    }

    private Type GetAgentType()
    {
        if (AgentId == null)
            return typeof(AgentInterface);

        if (!ServiceLocator.TryGetService<TypeService>(out var typeService))
            return typeof(AgentInterface);

        return typeService.GetAgentType(AgentId.Value);
    }

    private unsafe AgentInterface* GetAgentInterface()
    {
        var agentId = AgentId;
        return agentId == null
            ? null
            : AgentModule.Instance()->GetAgentByInternalId(agentId.Value);
    }

    public OrderedDictionary<int, (string, Type?)> FieldMapping
    {
        get
        {
            if (field != null)
                return field;

            var addonType = AddonType;
            if (addonType == null)
                return [];

            if (!ServiceLocator.TryGetService<TypeService>(out var typeService))
                return [];

            return field = typeService.GetTypeFields(addonType);
        }

        static bool MatchesNodeImage(AtkResNode* node, string value)
        {
            if (node->GetNodeType() != NodeType.Image)
                return false;

            var imageNode = (AtkImageNode*)node;
            if (imageNode->PartsList == null || imageNode->PartId >= imageNode->PartsList->PartCount)
                return false;

            var asset = imageNode->PartsList->Parts[imageNode->PartId].UldAsset;
            if (asset == null || asset->AtkTexture.TextureType != TextureType.Resource)
                return false;

            var resource = asset->AtkTexture.Resource;
            if (resource == null)
                return false;

            if (asset->AtkTexture.Resource->IconId.ToString() == value)
                return true;

            if (asset->AtkTexture.Resource->TexFileResourceHandle->FileName.ToString().Contains(value, StringComparison.InvariantCultureIgnoreCase))
                return true;

            return false;
        }

        static bool MatchesNodeImagePart(AtkResNode* node, string value)
        {
            if (node->GetNodeType() != NodeType.Image)
                return false;

            var imageNode = (AtkImageNode*)node;
            return imageNode->PartId.ToString() == value;
        }

        static bool MatchesNodeText(AtkResNode* node, string value)
        {
            if (node->GetNodeType() != NodeType.Text)
                return false;

            var textNode = (AtkTextNode*)node;
            return textNode->NodeText.StringPtr.AsReadOnlySeStringSpan().ToString().Contains(value, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
