using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselDebug.Abstracts;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Text.ReadOnly;

namespace HaselDebug.Tabs;

public class AddonInspector(IGameGui GameGui) : DebugTab
{
    private UiDebug? addonInspector;

    public override void Draw()
    {
        addonInspector ??= new UiDebug(GameGui);
        addonInspector?.Draw();
    }
}

#pragma warning disable SeStringRenderer
internal unsafe class UiDebug
{
    private const int UnitListCount = 18;

    private readonly bool[] selectedInList = new bool[UnitListCount];
    private readonly string[] listNames =
    [
        "Depth Layer 1",
        "Depth Layer 2",
        "Depth Layer 3",
        "Depth Layer 4",
        "Depth Layer 5",
        "Depth Layer 6",
        "Depth Layer 7",
        "Depth Layer 8",
        "Depth Layer 9",
        "Depth Layer 10",
        "Depth Layer 11",
        "Depth Layer 12",
        "Depth Layer 13",
        "Loaded Units",
        "Focused Units",
        "Units 16",
        "Units 17",
        "Units 18",
    ];

    private bool doingSearch;
    private string searchInput = string.Empty;
    private AtkUnitBase* selectedUnitBase = null;

    public IGameGui GameGui { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UiDebug"/> class.
    /// </summary>
    public UiDebug(IGameGui gameGui)
    {
        GameGui = gameGui;
    }

    /// <summary>
    /// Renders this window.
    /// </summary>
    public void Draw()
    {
        using (var child = ImRaii.Child("uiDebug_unitBaseSelect", new Vector2(250, -1), true))
        {
            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextWithHint("###atkUnitBaseSearch", "Search", ref searchInput, 0x20, ImGuiInputTextFlags.AutoSelectAll);

            DrawUnitBaseList();
        }

        if (selectedUnitBase != null)
        {
            ImGui.SameLine();
            using (var child = ImRaii.Child("uiDebug_selectedUnitBase", new Vector2(-1), true))
                DrawUnitBase(selectedUnitBase);
        }
    }

    private void DrawUnitBase(AtkUnitBase* atkUnitBase)
    {
        var isVisible = atkUnitBase->IsVisible;
        var addonName = atkUnitBase->NameString;
        var agent = GameGui.FindAgentInterface(atkUnitBase); //Service<GameGui>.Get().FindAgentInterface(atkUnitBase);

        ImGui.TextUnformatted(addonName);
        ImGui.SameLine();
        using (ImRaii.PushColor(ImGuiCol.Text, isVisible ? 0xFF00FF00 : 0xFF0000FF))
            ImGui.Text(isVisible ? "Visible" : "Not Visible");

        ImGui.SameLine(0, 0);
        ImGui.SetCursorPosX(ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X - ImGui.CalcTextSize("V").X);
        if (ImGui.Button("V"))
        {
            atkUnitBase->IsVisible = !atkUnitBase->IsVisible;
        }

        ImGui.Separator();
        ImGuiHelpers.ClickToCopyText($"Address: {(nint)atkUnitBase:X}", $"{(nint)atkUnitBase:X}");
        ImGuiHelpers.ClickToCopyText($"Agent: {agent:X}", $"{agent:X}");
        ImGui.Separator();

        ImGui.TextUnformatted($"Position: [ {atkUnitBase->X} , {atkUnitBase->Y} ]");
        ImGui.TextUnformatted($"Scale: {atkUnitBase->Scale * 100}%%");
        ImGui.TextUnformatted($"Widget Count {atkUnitBase->UldManager.ObjectCount}");

        ImGui.Separator();

        DebugUtils.DrawPointerType(atkUnitBase, typeof(AtkUnitBase), new NodeOptions());

        ImGui.Dummy(ImGuiHelpers.ScaledVector2(25));
        ImGui.Separator();
        if (atkUnitBase->RootNode != null)
            PrintNode(atkUnitBase->RootNode);

        if (atkUnitBase->UldManager.NodeListCount > 0)
        {
            ImGui.Dummy(ImGuiHelpers.ScaledVector2(25));
            ImGui.Separator();
            ImGui.PushStyleColor(ImGuiCol.Text, 0xFFFFAAAA);
            if (ImGui.TreeNode($"Node List##{(ulong)atkUnitBase:X}"))
            {
                ImGui.PopStyleColor();

                for (var j = 0; j < atkUnitBase->UldManager.NodeListCount; j++)
                {
                    PrintNode(atkUnitBase->UldManager.NodeList[j], false, $"[{j}] ");
                }

                ImGui.TreePop();
            }
            else
            {
                ImGui.PopStyleColor();
            }
        }
    }

    private void PrintNode(AtkResNode* node, bool printSiblings = true, string treePrefix = "")
    {
        if (node == null)
            return;

        if ((int)node->Type < 1000)
            PrintSimpleNode(node, treePrefix);
        else
            PrintComponentNode(node, treePrefix);

        if (printSiblings)
        {
            var prevNode = node;
            while ((prevNode = prevNode->PrevSiblingNode) != null)
                PrintNode(prevNode, false, "prev ");

            var nextNode = node;
            while ((nextNode = nextNode->NextSiblingNode) != null)
                PrintNode(nextNode, false, "next ");
        }
    }

    private void PrintSimpleNode(AtkResNode* node, string treePrefix)
    {
        var popped = false;
        var isVisible = node->NodeFlags.HasFlag(NodeFlags.Visible);

        if (isVisible)
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 255, 0, 255));

        if (ImGui.TreeNode($"{treePrefix}{node->Type} Node (ptr = {(long)node:X})###{(long)node}"))
        {
            if (ImGui.IsItemHovered())
                DrawOutline(node);

            if (isVisible)
            {
                ImGui.PopStyleColor();
                popped = true;
            }

            ImGui.TextUnformatted("Node: ");
            ImGui.SameLine();
            ImGuiHelpers.ClickToCopyText($"{(ulong)node:X}");
            ImGui.SameLine();
            switch (node->Type)
            {
                case NodeType.Text: Util.ShowStruct(*(AtkTextNode*)node, (ulong)node); break;
                case NodeType.Image: Util.ShowStruct(*(AtkImageNode*)node, (ulong)node); break;
                case NodeType.Collision: Util.ShowStruct(*(AtkCollisionNode*)node, (ulong)node); break;
                case NodeType.NineGrid: Util.ShowStruct(*(AtkNineGridNode*)node, (ulong)node); break;
                case NodeType.ClippingMask: Util.ShowStruct(*(AtkClippingMaskNode*)node, (ulong)node); break;
                case NodeType.Counter: Util.ShowStruct(*(AtkCounterNode*)node, (ulong)node); break;
                default: Util.ShowStruct(*node, (ulong)node); break;
            }

            PrintResNode(node);

            if (node->ChildNode != null)
                PrintNode(node->ChildNode);

            switch (node->Type)
            {
                case NodeType.Text:
                    var textNode = (AtkTextNode*)node;
                    ImGui.TextUnformatted("text: ");
                    ImGui.SameLine();
                    ImGuiHelpers.SeStringWrapped(textNode->NodeText.AsSpan()); // Service<SeStringRenderer>.Get().Draw(textNode->NodeText);

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

            ImGui.TreePop();
        }
        else if (ImGui.IsItemHovered())
        {
            DrawOutline(node);
        }

        if (isVisible && !popped)
            ImGui.PopStyleColor();

        static void PrintTextureInfo(AtkUldPartsList* partsList, uint partId)
        {
            if (partsList != null)
            {
                if (partId > partsList->PartCount)
                {
                    ImGui.TextUnformatted("part id > part count?");
                }
                else
                {
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
            }
            else
            {
                ImGui.TextUnformatted("no texture loaded");
            }
        }
    }

    private void PrintComponentNode(AtkResNode* node, string treePrefix)
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

        if (ImGui.TreeNode($"{treePrefix}{objectInfo->ComponentType} Component Node (ptr = {(long)node:X}, component ptr = {(long)compNode->Component:X}) child count = {childCount}  ###{(long)node}"))
        {
            if (ImGui.IsItemHovered())
                DrawOutline(node);

            if (isVisible)
            {
                ImGui.PopStyleColor();
                popped = true;
            }

            ImGui.TextUnformatted("Node: ");
            ImGui.SameLine();
            ImGuiHelpers.ClickToCopyText($"{(ulong)node:X}");
            ImGui.SameLine();
            Util.ShowStruct(*compNode, (ulong)compNode);
            ImGui.TextUnformatted("Component: ");
            ImGui.SameLine();
            ImGuiHelpers.ClickToCopyText($"{(ulong)compNode->Component:X}");
            ImGui.SameLine();

            switch (objectInfo->ComponentType)
            {
                case ComponentType.Button: Util.ShowStruct(*(AtkComponentButton*)compNode->Component, (ulong)compNode->Component); break;
                case ComponentType.Slider: Util.ShowStruct(*(AtkComponentSlider*)compNode->Component, (ulong)compNode->Component); break;
                case ComponentType.Window: Util.ShowStruct(*(AtkComponentWindow*)compNode->Component, (ulong)compNode->Component); break;
                case ComponentType.CheckBox: Util.ShowStruct(*(AtkComponentCheckBox*)compNode->Component, (ulong)compNode->Component); break;
                case ComponentType.GaugeBar: Util.ShowStruct(*(AtkComponentGaugeBar*)compNode->Component, (ulong)compNode->Component); break;
                case ComponentType.RadioButton: Util.ShowStruct(*(AtkComponentRadioButton*)compNode->Component, (ulong)compNode->Component); break;
                case ComponentType.TextInput: Util.ShowStruct(*(AtkComponentTextInput*)compNode->Component, (ulong)compNode->Component); break;
                case ComponentType.Icon: Util.ShowStruct(*(AtkComponentIcon*)compNode->Component, (ulong)compNode->Component); break;
                default: Util.ShowStruct(*compNode->Component, (ulong)compNode->Component); break;
            }

            PrintResNode(node);
            PrintNode(componentInfo.RootNode);

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
                    PrintNode(compNode->Component->UldManager.NodeList[i], false, $"[{i}] ");
                }

                ImGui.TreePop();
            }
            else
            {
                ImGui.PopStyleColor();
            }

            ImGui.TreePop();
        }
        else if (ImGui.IsItemHovered())
        {
            DrawOutline(node);
        }

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

    private bool DrawUnitListHeader(int index, ushort count, ulong ptr, bool highlight)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, highlight ? 0xFFAAAA00 : 0xFFFFFFFF);
        if (!string.IsNullOrEmpty(searchInput) && !doingSearch)
        {
            ImGui.SetNextItemOpen(true, ImGuiCond.Always);
        }
        else if (doingSearch && string.IsNullOrEmpty(searchInput))
        {
            ImGui.SetNextItemOpen(false, ImGuiCond.Always);
        }

        var treeNode = ImGui.TreeNode($"{listNames[index]}##unitList_{index}");
        ImGui.PopStyleColor();

        ImGui.SameLine();
        ImGui.TextDisabled($"C:{count}  {ptr:X}");
        return treeNode;
    }

    private void DrawUnitBaseList()
    {
        var foundSelected = false;
        var noResults = true;
        var stage = AtkStage.Instance();

        var unitManagers = &stage->RaptureAtkUnitManager->AtkUnitManager.DepthLayerOneList;

        var searchStr = searchInput;
        var searching = !string.IsNullOrEmpty(searchStr);

        for (var i = 0; i < UnitListCount; i++)
        {
            var headerDrawn = false;

            var highlight = selectedUnitBase != null && selectedInList[i];
            selectedInList[i] = false;
            var unitManager = &unitManagers[i];

            var headerOpen = true;

            if (!searching)
            {
                headerOpen = DrawUnitListHeader(i, unitManager->Count, (ulong)unitManager, highlight);
                headerDrawn = true;
                noResults = false;
            }

            for (var j = 0; j < unitManager->Count && headerOpen; j++)
            {
                AtkUnitBase* unitBase = unitManager->Entries[j];
                if (selectedUnitBase != null && unitBase == selectedUnitBase)
                {
                    selectedInList[i] = true;
                    foundSelected = true;
                }

                var name = unitBase->NameString;
                if (searching)
                {
                    if (name == null || !name.ToLowerInvariant().Contains(searchStr.ToLowerInvariant())) continue;
                }

                noResults = false;
                if (!headerDrawn)
                {
                    headerOpen = DrawUnitListHeader(i, unitManager->Count, (ulong)unitManager, highlight);
                    headerDrawn = true;
                }

                if (headerOpen)
                {
                    var visible = unitBase->IsVisible;
                    ImGui.PushStyleColor(ImGuiCol.Text, visible ? 0xFF00FF00 : 0xFF999999);

                    if (ImGui.Selectable($"{name}##list{i}-{(ulong)unitBase:X}_{j}", selectedUnitBase == unitBase))
                    {
                        selectedUnitBase = unitBase;
                        foundSelected = true;
                        selectedInList[i] = true;
                    }

                    ImGui.PopStyleColor();
                }
            }

            if (headerDrawn && headerOpen)
            {
                ImGui.TreePop();
            }

            if (selectedInList[i] == false && selectedUnitBase != null)
            {
                for (var j = 0; j < unitManager->Count; j++)
                {
                    AtkUnitBase* unitBase = unitManager->Entries[j];
                    if (selectedUnitBase == null || unitBase != selectedUnitBase) continue;
                    selectedInList[i] = true;
                    foundSelected = true;
                }
            }
        }

        if (noResults)
        {
            ImGui.TextDisabled("No Results");
        }

        if (!foundSelected)
        {
            selectedUnitBase = null;
        }

        if (doingSearch && string.IsNullOrEmpty(searchInput))
        {
            doingSearch = false;
        }
        else if (!doingSearch && !string.IsNullOrEmpty(searchInput))
        {
            doingSearch = true;
        }
    }

    private Vector2 GetNodePosition(AtkResNode* node)
    {
        var pos = new Vector2(node->X, node->Y);
        pos -= new Vector2(node->OriginX * (node->ScaleX - 1), node->OriginY * (node->ScaleY - 1));
        var par = node->ParentNode;
        while (par != null)
        {
            pos *= new Vector2(par->ScaleX, par->ScaleY);
            pos += new Vector2(par->X, par->Y);
            pos -= new Vector2(par->OriginX * (par->ScaleX - 1), par->OriginY * (par->ScaleY - 1));
            par = par->ParentNode;
        }

        return pos;
    }

    private Vector2 GetNodeScale(AtkResNode* node)
    {
        if (node == null) return new Vector2(1, 1);
        var scale = new Vector2(node->ScaleX, node->ScaleY);
        while (node->ParentNode != null)
        {
            node = node->ParentNode;
            scale *= new Vector2(node->ScaleX, node->ScaleY);
        }

        return scale;
    }

    private bool GetNodeVisible(AtkResNode* node)
    {
        if (node == null) return false;
        while (node != null)
        {
            if (!node->NodeFlags.HasFlag(NodeFlags.Visible)) return false;
            node = node->ParentNode;
        }

        return true;
    }

    private void DrawOutline(AtkResNode* node)
    {
        var position = GetNodePosition(node);
        var scale = GetNodeScale(node);
        var size = new Vector2(node->Width, node->Height) * scale;

        var nodeVisible = GetNodeVisible(node);

        position += ImGuiHelpers.MainViewport.Pos;

        ImGui.GetForegroundDrawList(ImGuiHelpers.MainViewport).AddRect(position, position + size, nodeVisible ? 0xFF00FF00 : 0xFF0000FF);
    }
}
