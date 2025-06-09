using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Graphics;
using HaselCommon.Services;
using HaselDebug.Extensions;
using HaselDebug.Utils;
using HaselDebug.Windows;
using ImGuiNET;

namespace HaselDebug.Services;

[RegisterSingleton, AutoConstruct]
public unsafe partial class AtkDebugRenderer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DebugRenderer _debugRenderer;
    private readonly TextService _textService;
    private readonly WindowManager _windowManager;

    public void DrawAddon(string addonName, bool border = true)
    {
        if (string.IsNullOrEmpty(addonName))
            return;

        using var hostchild = ImRaii.Child("AddonChild", new Vector2(-1), border, ImGuiWindowFlags.NoSavedSettings);

        var unitManager = RaptureAtkUnitManager.Instance();
        var unitBase = unitManager->GetAddonByName(addonName);
        if (unitBase == null)
        {
            ImGui.TextUnformatted($"Could not find addon {addonName}");
            return;
        }

        var nodeOptions = new NodeOptions() { AddressPath = new((nint)unitBase), DefaultOpen = true };

        if (!_debugRenderer.AddonTypes.TryGetValue(addonName, out var type))
            type = typeof(AtkUnitBase);

        _debugRenderer.DrawCopyableText(addonName);

        ImGui.SameLine();

        var isVisible = unitBase->IsVisible;
        using (ImRaii.PushColor(ImGuiCol.Text, isVisible ? 0xFF00FF00 : Color.From(ImGuiCol.TextDisabled).ToUInt()))
        {
            ImGui.TextUnformatted(isVisible ? "Visible" : "Not Visible");
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

            ImGui.TextUnformatted($"Used by Agent{agentId}");
            ImGui.SameLine();

            if (!_debugRenderer.AgentTypes.TryGetValue(agentId, out var agentType))
                agentType = typeof(AgentInterface);

            _debugRenderer.DrawPointerType(agent, agentType, nodeOptions.WithAddress((nint)agent) with
            {
                DefaultOpen = false,
                DrawContextMenu = (nodeOptions, builder) =>
                {
                    var pinnedInstances = Service.Get<PinnedInstancesService>();
                    var isPinned = pinnedInstances.Contains(agentType);

                    builder.AddCopyName(_textService, agentId.ToString());
                    builder.AddCopyAddress(_textService, (nint)agent);

                    builder.AddSeparator();

                    builder.Add(new ImGuiContextMenuEntry()
                    {
                        Visible = !_windowManager.Contains(win => win.WindowName == agentType.Name),
                        Label = _textService.Translate("ContextMenu.TabPopout"),
                        ClickCallback = () => _windowManager.Open(new PointerTypeWindow(_serviceProvider, (nint)agent, agentType, string.Empty))
                    });

                    builder.Add(new ImGuiContextMenuEntry()
                    {
                        Visible = !isPinned,
                        Label = _textService.Translate("ContextMenu.PinnedInstances.Pin"),
                        ClickCallback = () => pinnedInstances.Add((nint)agent, agentType)
                    });

                    builder.Add(new ImGuiContextMenuEntry()
                    {
                        Visible = isPinned,
                        Label = _textService.Translate("ContextMenu.PinnedInstances.Unpin"),
                        ClickCallback = () => pinnedInstances.Remove(agentType)
                    });
                }
            });
        }

        // Host
        if (unitBase->HostId != 0)
        {
            var host = unitManager->GetAddonById(unitBase->HostId);
            if (host != null)
            {
                ImGui.TextUnformatted($"Embedded by Addon{host->NameString}");
                ImGui.SameLine();

                if (!_debugRenderer.AddonTypes.TryGetValue(host->NameString, out var hostType))
                    hostType = typeof(AgentInterface);

                _debugRenderer.DrawPointerType((nint)host, hostType, nodeOptions.WithAddress((nint)host) with
                {
                    DefaultOpen = false,
                    DrawContextMenu = (nodeOptions, builder) =>
                    {
                        var pinnedInstances = Service.Get<PinnedInstancesService>();
                        var isPinned = pinnedInstances.Contains(hostType);

                        builder.AddCopyName(_textService, host->NameString);
                        builder.AddCopyAddress(_textService, (nint)host);

                        builder.AddSeparator();

                        builder.Add(new ImGuiContextMenuEntry()
                        {
                            Visible = !_windowManager.Contains(win => win.WindowName == hostType.Name),
                            Label = _textService.Translate("ContextMenu.TabPopout"),
                            ClickCallback = () => _windowManager.Open(new PointerTypeWindow(_serviceProvider, (nint)host, hostType, string.Empty))
                        });
                    }
                });
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

        ImGuiUtilsEx.PaddedSeparator();

        if (unitBase->RootNode != null)
        {
            PrintNode(unitBase->RootNode, true, string.Empty, nodeOptions with { DefaultOpen = true });
        }

        if (unitBase->UldManager.NodeListCount > 0)
        {
            ImGui.Dummy(ImGuiHelpers.ScaledVector2(25));
            ImGui.Separator();
            ImGui.PushStyleColor(ImGuiCol.Text, 0xFFFFAAAA);
            if (ImGui.TreeNodeEx($"Node List##{(ulong)unitBase:X}", ImGuiTreeNodeFlags.SpanAvailWidth))
            {
                ImGui.PopStyleColor();

                var j = 0;
                foreach (var node in unitBase->UldManager.Nodes)
                {
                    PrintNode(node, false, $"[{j++}] ", nodeOptions with { DefaultOpen = false });
                }

                ImGui.TreePop();
            }
            else
            {
                ImGui.PopStyleColor();
            }
        }
    }

    public void DrawNode(AtkResNode* node)
    {
        var unitManager = RaptureAtkUnitManager.Instance();
        var unitBase = unitManager->GetAddonByNode(node);
        if (unitBase == null)
        {
            ImGui.TextUnformatted($"Could not find addon with node {(nint)node:X}");
            return;
        }

        PrintNode(node, false, string.Empty, new() { DefaultOpen = true });
    }

    private void PrintNode(AtkResNode* node, bool printSiblings, string treePrefix, NodeOptions nodeOptions)
    {
        if (node == null)
            return;

        nodeOptions = nodeOptions.WithAddress((nint)node);

        if ((int)node->Type < 1000)
            PrintSimpleNode(node, treePrefix, nodeOptions);
        else
            PrintComponentNode(node, treePrefix, nodeOptions);

        if (printSiblings)
        {
            var prevNode = node;
            while ((prevNode = prevNode->PrevSiblingNode) != null)
                PrintNode(prevNode, false, string.Empty, nodeOptions);
        }
    }

    private void PrintSimpleNode(AtkResNode* node, string treePrefix, NodeOptions nodeOptions)
    {
        nodeOptions = nodeOptions.WithHighlightNode((nint)node, typeof(AtkResNode));

        using var treeNode = _debugRenderer.DrawTreeNode(nodeOptions with
        {
            Title = $"{treePrefix}[Id: {node->NodeId}] {node->Type} Node (0x{(nint)node:X})",
            TitleColor = node->IsVisible() ? Color.Green : Color.Grey,
            DrawContextMenu = (nodeOptions, builder) =>
            {
                builder.Add(new ImGuiContextMenuEntry()
                {
                    Visible = !_windowManager.Contains(win => win.WindowName == nodeOptions.Title),
                    Label = _textService.Translate("ContextMenu.TabPopout"),
                    ClickCallback = () =>
                    {
                        _windowManager.Open(new NodeInspectorWindow(_serviceProvider, this)
                        {
                            WindowName = nodeOptions.Title!,
                            NodeAddress = (nint)node
                        });
                    }
                });
            }
        });

        nodeOptions = nodeOptions.ConsumeTreeNodeOptions();

        if (!treeNode) return;

        ImGui.TextUnformatted("Node: ");
        ImGui.SameLine();
        _debugRenderer.DrawAddress(node);
        ImGui.SameLine();
        _debugRenderer.DrawPointerType((nint)node, typeof(AtkResNode), nodeOptions);

        PrintResNodeInfo(node);

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

        var isVisible = node->NodeFlags.HasFlag(NodeFlags.Visible);

        nodeOptions = nodeOptions.WithHighlightNode((nint)node, typeof(AtkComponentNode));

        using var treeNode = _debugRenderer.DrawTreeNode(nodeOptions with
        {
            Title = $"{treePrefix}[Id: {node->NodeId}] {objectInfo->ComponentType} Component Node (Node: 0x{(nint)node:X}, Component: 0x{(nint)component:X})###{nodeOptions.GetKey("ComponentNode")}",
            TitleColor = node->IsVisible() ? Color.Green : Color.Grey,
            DrawContextMenu = (nodeOptions, builder) =>
            {
                builder.Add(new ImGuiContextMenuEntry()
                {
                    Visible = !_windowManager.Contains(win => win.WindowName == nodeOptions.Title),
                    Label = _textService.Translate("ContextMenu.TabPopout"),
                    ClickCallback = () =>
                    {
                        _windowManager.Open(new NodeInspectorWindow(_serviceProvider, this)
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

        ImGui.TextUnformatted("Node: ");
        ImGui.SameLine();
        _debugRenderer.DrawAddress(node);
        ImGui.SameLine();
        _debugRenderer.DrawPointerType((nint)node, typeof(AtkComponentNode), nodeOptions.WithAddress((nint)node));

        ImGui.TextUnformatted("Component: ");
        ImGui.SameLine();
        _debugRenderer.DrawAddress(component);
        ImGui.SameLine();
        _debugRenderer.DrawPointerType((nint)component, typeof(AtkComponentBase), nodeOptions.WithAddress(2));

        PrintResNodeInfo(resNode);
        PrintNode(component->UldManager.RootNode, true, string.Empty, nodeOptions);

        ImGui.PushStyleColor(ImGuiCol.Text, 0xFFFFAAAA);
        if (ImGui.TreeNodeEx($"Node List##{(ulong)node:X}", ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            ImGui.PopStyleColor();

            for (var i = 0; i < component->UldManager.NodeListCount; i++)
            {
                PrintNode(component->UldManager.NodeList[i], false, $"[{i}] ", nodeOptions);
            }

            ImGui.TreePop();
        }
        else
        {
            ImGui.PopStyleColor();
        }
    }

    private void PrintResNodeInfo(AtkResNode* node)
    {
        ImGui.TextUnformatted("NodeId:");
        ImGui.SameLine();
        _debugRenderer.DrawNumeric(node->NodeId, typeof(uint), new NodeOptions() { HexOnShift = true });

        using var treeNode = ImRaii.TreeNode("Properties", ImGuiTreeNodeFlags.SpanAvailWidth);
        if (!treeNode) return;

        using var infoTable = ImRaii.Table("NodeInfoTable", 2, ImGuiTableFlags.NoSavedSettings);
        if (!infoTable) return;

        ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

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
                if (ImGuiUtilsEx.PartListSelector(imageNode->PartsList, ref partId))
                {
                    imageNode->PartId = (ushort)partId;
                    imageNode->DrawFlags |= 1;
                }
                break;

            case NodeType.Text:
                var textNode = (AtkTextNode*)node;
                StartRow("Text");
                _debugRenderer.DrawSeString(textNode->NodeText.AsSpan(), true, new NodeOptions() { AddressPath = new((nint)node) }); // TODO: make editable

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
                if (ImGui.DragInt("##FontSize", ref fontSize, 1, 0, int.MaxValue))
                {
                    textNode->FontSize = (byte)fontSize;
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
                var textFlags = (TextFlags)textNode->TextFlags;
                if (ImGuiUtilsEx.EnumCombo("##TextFlags", ref textFlags, true))
                {
                    textNode->TextFlags = (byte)textFlags;
                }

                StartRow("Text Flags 2");
                var textFlags2 = (TextFlags2)textNode->TextFlags2;
                if (ImGuiUtilsEx.EnumCombo("##TextFlags2", ref textFlags2, true))
                {
                    textNode->TextFlags2 = (byte)textFlags2;
                }

                break;

            case NodeType.Counter:
                var counterNode = (AtkCounterNode*)node;
                StartRow("Text");
                _debugRenderer.DrawSeString(counterNode->NodeText.AsSpan(), true, new NodeOptions() { AddressPath = new((nint)node) }); // TODO: make editable
                break;

            case NodeType.NineGrid:
                var ngNode = (AtkNineGridNode*)node;
                StartRow("Asset");
                partId = ngNode->PartId;
                if (ImGuiUtilsEx.PartListSelector(ngNode->PartsList, ref partId))
                {
                    ngNode->PartId = partId;
                    ngNode->DrawFlags |= 1;
                }
                break;

            case NodeType.ClippingMask:
                var cmNode = (AtkClippingMaskNode*)node;
                StartRow("Asset");
                partId = cmNode->PartId;
                if (ImGuiUtilsEx.PartListSelector(cmNode->PartsList, ref partId))
                {
                    cmNode->PartId = (ushort)partId;
                    cmNode->DrawFlags |= 1;
                }
                break;
        }
    }

    private static void StartRow(string label)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(label);
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(200);
    }
}
