using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Services;
using HaselCommon.Utils;
using HaselDebug.Abstracts;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Text.ReadOnly;

namespace HaselDebug.Tabs;

#pragma warning disable SeStringRenderer
public unsafe class AddonInspectorTab(TextService TextService, DebugRenderer DebugRenderer) : DebugTab
{
    private ushort SelectedAddonId = 0;
    private string SelectedAddonName = string.Empty;
    private bool SortDirty = true;
    private short SortColumnIndex = 1;
    private ImGuiSortDirection SortDirection = ImGuiSortDirection.Ascending;
    private ImmutableSortedDictionary<string, Type>? AddonTypes;
    private string AddonNameSearchTerm = string.Empty;
    private bool ShowPicker;
    private int NodePickerSelectionIndex;
    private Vector2 LastMousePos;

    public override bool DrawInChild => false;

    public override void Draw()
    {
        AddonTypes ??= typeof(Addon).Assembly.GetTypes()
            .Where(type => type.GetCustomAttribute<Addon>() != null)
            .SelectMany(type => type.GetCustomAttribute<Addon>()!.AddonIdentifiers, (type, addonName) => (type, addonName))
            .ToImmutableSortedDictionary(
                tuple => tuple.addonName,
                tuple => tuple.type);

        using var hostchild = ImRaii.Child("AddonInspectorTabChild", new Vector2(-1), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
        if (!hostchild) return;

        DrawAddonList();
        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
        DrawAddon();
        DrawNodePicker();
    }

    private void DrawAddonList()
    {
        using var sidebarchild = ImRaii.Child("AddonListChild", new Vector2(300, -1), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
        if (!sidebarchild) return;

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGuiUtils.GetIconButtonSize(FontAwesomeIcon.ObjectUngroup).X - ImGui.GetStyle().ItemSpacing.X);
        var hasSearchTermChanged = ImGui.InputTextWithHint("##TextSearch", TextService.Translate("SearchBar.Hint"), ref AddonNameSearchTerm, 256, ImGuiInputTextFlags.AutoSelectAll);
        var hasSearchTerm = !string.IsNullOrWhiteSpace(AddonNameSearchTerm);
        var hasSearchTermAutoSelected = false;

        ImGui.SameLine();
        if (ImGuiUtils.IconButton("NodeSelectorToggleButton", FontAwesomeIcon.ObjectUngroup, "Pick Addon/Node", active: ShowPicker))
        {
            ShowPicker = !ShowPicker;
            NodePickerSelectionIndex = 0;
        }

        using var table = ImRaii.Table("AddonsTable", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Sortable | ImGuiTableFlags.NoSavedSettings, new Vector2(-1));
        if (!table) return;

        ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Name");
        ImGui.TableSetupScrollFreeze(2, 1);
        ImGui.TableHeadersRow();

        var raptureAtkUnitManager = RaptureAtkUnitManager.Instance();

        var allUnitsList = new List<Pointer<AtkUnitBase>>();
        var focusedList = new List<Pointer<AtkUnitBase>>();

        for (var i = 0; i < raptureAtkUnitManager->AllLoadedUnitsList.Count; i++)
        {
            var unitBase = raptureAtkUnitManager->AllLoadedUnitsList.Entries[i].Value;
            if (unitBase == null)
                continue;

            if (hasSearchTerm && !unitBase->NameString.Contains(AddonNameSearchTerm, StringComparison.InvariantCultureIgnoreCase))
                continue;

            allUnitsList.Add(unitBase);
        }

        for (var i = 0; i < raptureAtkUnitManager->FocusedUnitsList.Count; i++)
        {
            var unitBase = raptureAtkUnitManager->FocusedUnitsList.Entries[i].Value;
            if (unitBase == null)
                continue;

            if (hasSearchTerm && !unitBase->NameString.Contains(AddonNameSearchTerm, StringComparison.InvariantCultureIgnoreCase))
                continue;

            focusedList.Add(unitBase);
        }

        allUnitsList.Sort((a, b) => SortColumnIndex switch
        {
            0 when SortDirection == ImGuiSortDirection.Ascending => a.Value->Id - b.Value->Id,
            0 when SortDirection == ImGuiSortDirection.Descending => b.Value->Id - a.Value->Id,
            1 when SortDirection == ImGuiSortDirection.Ascending => a.Value->NameString.CompareTo(b.Value->NameString),
            1 when SortDirection == ImGuiSortDirection.Descending => b.Value->NameString.CompareTo(a.Value->NameString),
            _ => 0,
        });

        var bounds = stackalloc FFXIVClientStructs.FFXIV.Common.Math.Bounds[1];

        foreach (AtkUnitBase* unitBase in allUnitsList)
        {
            var addonId = unitBase->Id;
            var addonName = unitBase->NameString;

            if (hasSearchTermChanged && !hasSearchTermAutoSelected)
            {
                SelectedAddonId = addonId;
                SelectedAddonName = addonName;
                hasSearchTermAutoSelected = true;
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn(); // Id
            ImGui.TextUnformatted(addonId.ToString());

            ImGui.TableNextColumn(); // Name
            using (ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled), !unitBase->IsVisible))
            using (ImRaii.PushColor(ImGuiCol.Text, (uint)Colors.Gold, focusedList.Contains(unitBase)))
            {
                if (ImGui.Selectable(addonName + $"##Addon_{addonId}_{addonName}", addonId == SelectedAddonId && SelectedAddonName == addonName, ImGuiSelectableFlags.SpanAllColumns))
                {
                    SelectedAddonId = addonId;
                    SelectedAddonName = addonName;
                }
            }

            if (ImGui.IsItemHovered() && ImGui.IsKeyDown(ImGuiKey.LeftShift))
            {
                unitBase->GetWindowBounds(bounds);
                var pos = new Vector2(bounds->Pos1.X, bounds->Pos1.Y);
                var size = new Vector2(bounds->Size.X, bounds->Size.Y);

                ImGui.SetNextWindowPos(pos);
                ImGui.SetNextWindowSize(size);

                using var windowStyles = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1.0f);
                using var windowColors = Colors.Gold.Push(ImGuiCol.Border)
                                                    .Push(ImGuiCol.WindowBg, new Vector4(0.847f, 0.733f, 0.49f, 0.33f));

                if (ImGui.Begin("AddonHighligher", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoInputs))
                {
                    var drawList = ImGui.GetForegroundDrawList();
                    var textPos = pos + new Vector2(0, -ImGui.GetTextLineHeight());
                    drawList.AddText(textPos + Vector2.One, Colors.Black, addonName);
                    drawList.AddText(textPos, Colors.Gold, addonName);
                    ImGui.End();
                }
            }

            using var contextMenu = ImRaii.ContextPopupItem($"##Addon_{addonId}_{addonName}_Context");
            if (contextMenu)
            {
                if (!string.IsNullOrEmpty(addonName) && ImGui.MenuItem("Copy name"))
                {
                    ImGui.SetClipboardText(addonName);
                }

                if (ImGui.MenuItem("Copy address"))
                {
                    ImGui.SetClipboardText($"0x{(nint)unitBase:X}");
                }
            }
        }

        var sortSpecs = ImGui.TableGetSortSpecs();
        SortDirty |= sortSpecs.SpecsDirty;

        if (!SortDirty)
            return;

        SortColumnIndex = sortSpecs.Specs.ColumnIndex;
        SortDirection = sortSpecs.Specs.SortDirection;
        sortSpecs.SpecsDirty = SortDirty = false;
    }

    private void DrawAddon()
    {
        if (string.IsNullOrEmpty(SelectedAddonName))
            return;

        using var hostchild = ImRaii.Child("AddonChild", new Vector2(-1), true, ImGuiWindowFlags.NoSavedSettings);

        if (!AddonTypes!.TryGetValue(SelectedAddonName, out var type))
            type = typeof(AtkUnitBase);

        var unitBase = RaptureAtkUnitManager.Instance()->GetAddonById(SelectedAddonId);
        if (unitBase == null)
        {
            ImGui.TextUnformatted($"Could not find addon with id {SelectedAddonId}");
            return;
        }

        var nodeOptions = new NodeOptions() { AddressPath = new AddressPath((nint)unitBase) };

        var isVisible = unitBase->IsVisible;
        var addonName = unitBase->NameString;
        //var agent = 0; // GameGui.FindAgentInterface(atkUnitBase); //Service<GameGui>.Get().FindAgentInterface(atkUnitBase);

        DebugRenderer.DrawCopyableText(addonName);
        ImGui.SameLine();
        using (ImRaii.PushColor(ImGuiCol.Text, isVisible ? 0xFF00FF00 : 0xFF0000FF))
            ImGui.TextUnformatted(isVisible ? "Visible" : "Not Visible");

        ImGui.SameLine(0, 0);
        ImGui.SetCursorPosX(ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X - ImGui.CalcTextSize("V").X);
        if (ImGui.Button("V"))
        {
            unitBase->IsVisible = !unitBase->IsVisible;
        }

        ImGui.Separator();
        ImGuiHelpers.ClickToCopyText($"Address: {(nint)unitBase:X}", $"{(nint)unitBase:X}");
        //ImGuiHelpers.ClickToCopyText($"Agent: {agent:X}", $"{agent:X}");
        ImGui.Separator();

        ImGui.TextUnformatted($"Position: [ {unitBase->X} , {unitBase->Y} ]");
        ImGui.TextUnformatted($"Scale: {unitBase->Scale * 100}%");
        ImGui.TextUnformatted($"Widget Count {unitBase->UldManager.ObjectCount}");

        ImGui.Separator();

        DebugRenderer.DrawPointerType(unitBase, type, nodeOptions);

        ImGui.Dummy(ImGuiHelpers.ScaledVector2(25));
        ImGui.Separator();
        if (unitBase->RootNode != null)
            PrintNode(unitBase->RootNode, true, string.Empty, nodeOptions with { DefaultOpen = true });

        if (unitBase->UldManager.NodeListCount > 0)
        {
            ImGui.Dummy(ImGuiHelpers.ScaledVector2(25));
            ImGui.Separator();
            ImGui.PushStyleColor(ImGuiCol.Text, 0xFFFFAAAA);
            if (ImGui.TreeNodeEx($"Node List##{(ulong)unitBase:X}", ImGuiTreeNodeFlags.SpanAvailWidth))
            {
                ImGui.PopStyleColor();

                for (var j = 0; j < unitBase->UldManager.NodeListCount; j++)
                {
                    PrintNode(unitBase->UldManager.NodeList[j], false, $"[{j}] ", nodeOptions);
                }

                ImGui.TreePop();
            }
            else
            {
                ImGui.PopStyleColor();
            }
        }
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
                PrintNode(prevNode, false, "prev ", nodeOptions);

            var nextNode = node;
            while ((nextNode = nextNode->NextSiblingNode) != null)
                PrintNode(nextNode, false, "next ", nodeOptions);
        }
    }

    private void PrintSimpleNode(AtkResNode* node, string treePrefix, NodeOptions nodeOptions)
    {
        //var isVisible = node->NodeFlags.HasFlag(NodeFlags.Visible);

        using var treeNode = DebugRenderer.DrawTreeNode(nodeOptions with {
            Title = $"{treePrefix}{node->Type} Node (ptr = {(nint)node:X})",
            TitleColor = Colors.Green
            // TODO: OnHovered = () => DrawOutline(node)
        });

        nodeOptions = nodeOptions.ConsumeTreeNodeOptions();

        if (!treeNode) return;

        ImGui.TextUnformatted("Node: ");
        ImGui.SameLine();
        DebugRenderer.DrawAddress(node);
        ImGui.SameLine();
        var nodeType = node->Type switch
        {
            NodeType.Text => typeof(AtkTextNode),
            NodeType.Image => typeof(AtkImageNode),
            NodeType.Collision => typeof(AtkCollisionNode),
            NodeType.NineGrid => typeof(AtkNineGridNode),
            NodeType.ClippingMask => typeof(AtkClippingMaskNode),
            NodeType.Counter => typeof(AtkCounterNode),
            _ => typeof(AtkResNode)
        };
        DebugRenderer.DrawPointerType((nint)node, nodeType, nodeOptions);

        PrintResNode(node);

        if (node->ChildNode != null)
            PrintNode(node->ChildNode, true, string.Empty, nodeOptions);

        switch (node->Type)
        {
            case NodeType.Text:
                var textNode = (AtkTextNode*)node;
                ImGui.TextUnformatted("text: ");
                ImGui.SameLine();
                DebugRenderer.DrawUtf8String((nint)(&textNode->NodeText), nodeOptions);

                ImGui.InputText($"Replace Text##{(ulong)textNode:X}", new IntPtr(textNode->NodeText.StringPtr), (uint)textNode->NodeText.BufSize);

                ImGui.SameLine();
                if (ImGui.Button($"Encode##{(ulong)textNode:X}"))
                {
                    using var tmp = new Utf8String();
                    RaptureTextModule.Instance()->MacroEncoder.EncodeString(&tmp, textNode->NodeText.StringPtr);
                    textNode->NodeText.Copy(&tmp);
                }

                ImGui.SameLine();
                if (ImGui.Button($"Decode##{(ulong)textNode:X}"))
                    textNode->NodeText.SetString(new ReadOnlySeStringSpan(textNode->NodeText.StringPtr).ToString());

                ImGui.TextUnformatted($"AlignmentType: {(AlignmentType)textNode->AlignmentFontType}  FontSize: {textNode->FontSize}");
                int b = textNode->AlignmentFontType;
                if (ImGui.InputInt($"###setAlignment{(ulong)textNode:X}", ref b, 1))
                {
                    while (b > byte.MaxValue) b -= byte.MaxValue;
                    while (b < byte.MinValue) b += byte.MaxValue;
                    textNode->AlignmentFontType = (byte)b;
                    textNode->AtkResNode.DrawFlags |= 0x1;
                }

                ImGui.TextUnformatted($"Color: #{textNode->TextColor.R:X2}{textNode->TextColor.G:X2}{textNode->TextColor.B:X2}{textNode->TextColor.A:X2}");
                ImGui.SameLine();
                ImGui.TextUnformatted($"EdgeColor: #{textNode->EdgeColor.R:X2}{textNode->EdgeColor.G:X2}{textNode->EdgeColor.B:X2}{textNode->EdgeColor.A:X2}");
                ImGui.SameLine();
                ImGui.TextUnformatted($"BGColor: #{textNode->BackgroundColor.R:X2}{textNode->BackgroundColor.G:X2}{textNode->BackgroundColor.B:X2}{textNode->BackgroundColor.A:X2}");

                ImGui.TextUnformatted($"TextFlags: {textNode->TextFlags}");
                ImGui.SameLine();
                ImGui.TextUnformatted($"TextFlags2: {textNode->TextFlags2}");

                break;
            case NodeType.Counter:
                var counterNode = (AtkCounterNode*)node;
                ImGui.TextUnformatted("text: ");
                ImGui.SameLine();
                ImGuiHelpers.SeStringWrapped(counterNode->NodeText.AsSpan()); // Service<SeStringRenderer>.Get().Draw(counterNode->NodeText);
                break;
            case NodeType.Image:
                var imageNode = (AtkImageNode*)node;
                PrintTextureInfo(imageNode->PartsList, imageNode->PartId);
                break;
            case NodeType.NineGrid:
                var ngNode = (AtkNineGridNode*)node;
                PrintTextureInfo(ngNode->PartsList, ngNode->PartId);
                break;
            case NodeType.ClippingMask:
                var cmNode = (AtkClippingMaskNode*)node;
                PrintTextureInfo(cmNode->PartsList, cmNode->PartId);
                break;
        }
    }

    private static void PrintTextureInfo(AtkUldPartsList* partsList, uint partId)
    {
        if (partsList == null)
        {
            ImGui.TextUnformatted("No texture loaded");
            return;
        }

        if (partId > partsList->PartCount)
        {
            ImGui.TextUnformatted("part id > part count?");
            return;
        }

        var textureInfo = partsList->Parts[partId].UldAsset;
        var texType = textureInfo->AtkTexture.TextureType;
        ImGui.TextUnformatted(
            $"texture type: {texType} part_id={partId} part_id_count={partsList->PartCount}");
        if (texType == TextureType.Resource)
        {
            ImGui.TextUnformatted(
                $"texture path: {textureInfo->AtkTexture.Resource->TexFileResourceHandle->ResourceHandle.FileName}");
            var kernelTexture = textureInfo->AtkTexture.Resource->KernelTextureObject;

            if (ImGui.TreeNode($"Texture##{(ulong)kernelTexture->D3D11ShaderResourceView:X}"))
            {
                ImGui.Image(
                    new IntPtr(kernelTexture->D3D11ShaderResourceView),
                    new Vector2(kernelTexture->Width, kernelTexture->Height));
                ImGui.TreePop();
            }
        }
        else if (texType == TextureType.KernelTexture)
        {
            if (ImGui.TreeNode(
                    $"Texture##{(ulong)textureInfo->AtkTexture.KernelTexture->D3D11ShaderResourceView:X}"))
            {
                ImGui.Image(
                    new IntPtr(textureInfo->AtkTexture.KernelTexture->D3D11ShaderResourceView),
                    new Vector2(
                        textureInfo->AtkTexture.KernelTexture->Width,
                        textureInfo->AtkTexture.KernelTexture->Height));
                ImGui.TreePop();
            }
        }
    }

    private void PrintComponentNode(AtkResNode* node, string treePrefix, NodeOptions nodeOptions)
    {
        var compNode = (AtkComponentNode*)node;

        var popped = false;
        var isVisible = node->NodeFlags.HasFlag(NodeFlags.Visible);

        var componentInfo = compNode->Component->UldManager;

        var childCount = componentInfo.NodeListCount;

        var objectInfo = (AtkUldComponentInfo*)componentInfo.Objects;
        if (objectInfo == null)
        {
            return;
        }

        if (isVisible)
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 255, 0, 255));

        if (ImGui.TreeNodeEx($"{treePrefix}{objectInfo->ComponentType} Component Node (ptr = {(nint)node:X}, component ptr = {(nint)compNode->Component:X}) child count = {childCount}###{nodeOptions.GetKey("ComponentNode")}", ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            // if (ImGui.IsItemHovered())
            //     DrawOutline(node);

            if (isVisible)
            {
                ImGui.PopStyleColor();
                popped = true;
            }

            ImGui.TextUnformatted("Node: ");
            ImGui.SameLine();
            DebugRenderer.DrawAddress(node);
            ImGui.SameLine();
            DebugRenderer.DrawPointerType((nint)compNode, typeof(AtkComponentNode), nodeOptions.WithAddress(1));

            ImGui.TextUnformatted("Component: ");
            ImGui.SameLine();
            DebugRenderer.DrawAddress(compNode->Component);
            ImGui.SameLine();
            var componentType = objectInfo->ComponentType switch
            {
                ComponentType.Button => typeof(AtkComponentButton),
                ComponentType.Slider => typeof(AtkComponentSlider),
                ComponentType.Window => typeof(AtkComponentWindow),
                ComponentType.CheckBox => typeof(AtkComponentCheckBox),
                ComponentType.GaugeBar => typeof(AtkComponentGaugeBar),
                ComponentType.RadioButton => typeof(AtkComponentRadioButton),
                ComponentType.TextInput => typeof(AtkComponentTextInput),
                ComponentType.Icon => typeof(AtkComponentIcon),
                _ => typeof(AtkComponentBase)
            };
            DebugRenderer.DrawPointerType((nint)compNode->Component, componentType, nodeOptions.WithAddress(2));

            PrintResNode(node);
            PrintNode(componentInfo.RootNode, true, string.Empty, nodeOptions);

            switch (objectInfo->ComponentType)
            {
                case ComponentType.TextInput:
                    var textInputComponent = (AtkComponentTextInput*)compNode->Component;
                    ImGui.TextUnformatted("InputBase Text1: ");
                    ImGui.SameLine();
                    ImGuiHelpers.SeStringWrapped(textInputComponent->AtkComponentInputBase.UnkText1.AsSpan()); // Service<SeStringRenderer>.Get().Draw(textInputComponent->AtkComponentInputBase.UnkText1);

                    ImGui.TextUnformatted("InputBase Text2: ");
                    ImGui.SameLine();
                    ImGuiHelpers.SeStringWrapped(textInputComponent->AtkComponentInputBase.UnkText2.AsSpan()); // Service<SeStringRenderer>.Get().Draw(textInputComponent->AtkComponentInputBase.UnkText2);

                    ImGui.TextUnformatted("Text1: ");
                    ImGui.SameLine();
                    ImGuiHelpers.SeStringWrapped(textInputComponent->UnkText01.AsSpan()); // Service<SeStringRenderer>.Get().Draw(textInputComponent->UnkText01);

                    ImGui.TextUnformatted("Text2: ");
                    ImGui.SameLine();
                    ImGuiHelpers.SeStringWrapped(textInputComponent->UnkText02.AsSpan()); // Service<SeStringRenderer>.Get().Draw(textInputComponent->UnkText02);

                    ImGui.TextUnformatted("Text3: ");
                    ImGui.SameLine();
                    ImGuiHelpers.SeStringWrapped(textInputComponent->UnkText03.AsSpan()); // Service<SeStringRenderer>.Get().Draw(textInputComponent->UnkText03);

                    ImGui.TextUnformatted("Text4: ");
                    ImGui.SameLine();
                    ImGuiHelpers.SeStringWrapped(textInputComponent->UnkText04.AsSpan()); // Service<SeStringRenderer>.Get().Draw(textInputComponent->UnkText04);

                    ImGui.TextUnformatted("Text5: ");
                    ImGui.SameLine();
                    ImGuiHelpers.SeStringWrapped(textInputComponent->UnkText05.AsSpan()); // Service<SeStringRenderer>.Get().Draw(textInputComponent->UnkText05);
                    break;
            }

            ImGui.PushStyleColor(ImGuiCol.Text, 0xFFFFAAAA);
            if (ImGui.TreeNode($"Node List##{(ulong)node:X}"))
            {
                ImGui.PopStyleColor();

                for (var i = 0; i < compNode->Component->UldManager.NodeListCount; i++)
                {
                    PrintNode(compNode->Component->UldManager.NodeList[i], false, $"[{i}] ", nodeOptions);
                }

                ImGui.TreePop();
            }
            else
            {
                ImGui.PopStyleColor();
            }

            ImGui.TreePop();
        }
        //else if (ImGui.IsItemHovered())
        //{
        //    DrawOutline(node);
        //}

        if (isVisible && !popped)
            ImGui.PopStyleColor();
    }

    private void PrintResNode(AtkResNode* node)
    {
        ImGui.TextUnformatted($"NodeID: {node->NodeId}");
        ImGui.SameLine();
        if (ImGui.SmallButton($"T:Visible##{(ulong)node:X}"))
        {
            node->NodeFlags ^= NodeFlags.Visible;
        }

        ImGui.SameLine();
        if (ImGui.SmallButton($"C:Ptr##{(ulong)node:X}"))
        {
            ImGui.SetClipboardText($"{(ulong)node:X}");
        }

        ImGui.TextUnformatted(
            $"X: {node->X} Y: {node->Y} " +
            $"ScaleX: {node->ScaleX} ScaleY: {node->ScaleY} " +
            $"Rotation: {node->Rotation} " +
            $"Width: {node->Width} Height: {node->Height} " +
            $"OriginX: {node->OriginX} OriginY: {node->OriginY}");
        ImGui.TextUnformatted(
            $"RGBA: 0x{node->Color.R:X2}{node->Color.G:X2}{node->Color.B:X2}{node->Color.A:X2} " +
            $"AddRGB: {node->AddRed} {node->AddGreen} {node->AddBlue} " +
            $"MultiplyRGB: {node->MultiplyRed} {node->MultiplyGreen} {node->MultiplyBlue}");
    }


    private void DrawNodePicker()
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

        allUnitsList.Sort((a, b) => (int)(b.Value->DepthLayer - a.Value->DepthLayer));

        var hoveredDepthLayerAddonNodes = new Dictionary<uint, Dictionary<Pointer<AtkUnitBase>, List<Pointer<AtkResNode>>>>();
        var nodeCount = 0;
        var bounds = stackalloc FFXIVClientStructs.FFXIV.Common.Math.Bounds[1];

        foreach (AtkUnitBase* unitBase in allUnitsList)
        {
            unitBase->GetWindowBounds(bounds);
            var pos = new Vector2(bounds->Pos1.X, bounds->Pos1.Y);
            var size = new Vector2(bounds->Size.X, bounds->Size.Y);

            if (!bounds->ContainsPoint((int)ImGui.GetMousePos().X, (int)ImGui.GetMousePos().Y))
                continue;

            for (var i = 0; i < unitBase->UldManager.NodeListCount; i++)
            {
                var node = unitBase->UldManager.NodeList[i];
                node->GetBounds(bounds);

                if (!bounds->ContainsPoint((int)ImGui.GetMousePos().X, (int)ImGui.GetMousePos().Y))
                    continue;

                if (!hoveredDepthLayerAddonNodes.TryGetValue(unitBase->DepthLayer, out var addonNodes))
                    hoveredDepthLayerAddonNodes.Add(unitBase->DepthLayer, addonNodes = []);

                if (!addonNodes.TryGetValue(unitBase, out var nodes))
                    addonNodes.Add(unitBase, [node]);
                else if (!nodes.Contains(node))
                    nodes.Add(node);

                nodeCount++;
            }
        }

        if (nodeCount == 0)
        {
            ShowPicker = false;
            return;
        }

        ImGui.SetNextWindowPos(Vector2.Zero);
        ImGui.SetNextWindowSize(ImGui.GetMainViewport().Size);

        var mousePos = ImGui.GetMousePos();
        var mouseMoved = LastMousePos != mousePos;
        if (mouseMoved)
            NodePickerSelectionIndex = 0;

        if (!ImGui.Begin("NodePicker", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoBackground))
            return;

        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        var nodeIndex = 0;
        foreach (var (depthLayer, addons) in hoveredDepthLayerAddonNodes)
        {
            ImGui.TextUnformatted($"Depth Layer {depthLayer}:");

            using var indent = ImRaii.PushIndent();
            foreach (var (unitBase, nodes) in addons)
            {
                ImGui.TextUnformatted($"{unitBase.Value->NameString}:");

                using var indent2 = ImRaii.PushIndent();

                for (var i = nodes.Count - 1; i >= 0; i--)
                {
                    var node = nodes[i].Value;
                    node->GetBounds(bounds);

                    if (NodePickerSelectionIndex == nodeIndex)
                    {
                        using (ImRaii.PushFont(UiBuilder.IconFont))
                            ImGui.TextUnformatted(FontAwesomeIcon.CaretRight.ToIconString());
                        ImGui.SameLine(0, 0);

                        ImGui.GetForegroundDrawList().AddRectFilled(
                            new Vector2(bounds->Pos1.X, bounds->Pos1.Y),
                            new Vector2(bounds->Pos2.X, bounds->Pos2.Y),
                            HaselColor.From(1, 1, 0, 0.5f));

                        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                        {
                            SelectedAddonId = unitBase.Value->Id;
                            SelectedAddonName = unitBase.Value->NameString;

                            NodePickerSelectionIndex = 0;
                            ShowPicker = false;
                        }
                    }

                    if ((int)node->Type < 1000)
                    {
                        ImGui.TextUnformatted($"[0x{(nint)node:X}] [{node->NodeId}] {node->Type} Node");
                    }
                    else
                    {
                        var compNode = (AtkComponentNode*)node;
                        var componentInfo = compNode->Component->UldManager;
                        var objectInfo = (AtkUldComponentInfo*)componentInfo.Objects;
                        if (objectInfo == null) continue;
                        ImGui.TextUnformatted($"[0x{(nint)node:X}] [{node->NodeId}] {objectInfo->ComponentType} Component Node");
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

        if (mouseMoved)
            LastMousePos = mousePos;

        ImGui.End();
    }
}
