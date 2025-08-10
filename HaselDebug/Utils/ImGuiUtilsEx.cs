using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Graphics;
using HaselCommon.Gui;

namespace HaselDebug.Utils;

public static unsafe class ImGuiUtilsEx
{
    public static bool EnumCombo<T>(string label, ref T refValue, bool flagCombo = false) where T : Enum
    {
        if (flagCombo)
        {
            var size = typeof(T).GetEnumUnderlyingType().SizeOf();
            var names = new List<string>();

            for (var bit = 0; bit < size * 8; bit++)
            {
                var intRefValue = Convert.ToInt32(refValue);
                var intFlagValue = 1 << bit;
                var hasFlag = (intRefValue & intFlagValue) != 0;
                if (hasFlag)
                    names.Add(Enum.GetName(refValue.GetType(), intFlagValue) ?? $"Unk{bit}");
            }

            using var combo = ImRaii.Combo(label, string.Join(", ", names), ImGuiComboFlags.HeightLarge);
            if (!combo) return false;

            for (var bit = 0; bit < size * 8; bit++)
            {
                var intRefValue = Convert.ToInt32(refValue);
                var intFlagValue = 1 << bit;
                var hasFlag = (intRefValue & intFlagValue) != 0;

                if (ImGui.Selectable($"[{bit}] {Enum.GetName(refValue.GetType(), intFlagValue) ?? $"Unk{bit}"}", hasFlag))
                {
                    if (!hasFlag)
                    {
                        var result = intRefValue | intFlagValue;
                        refValue = (T)Enum.ToObject(refValue.GetType(), result);
                    }
                    else
                    {
                        var result = intRefValue & ~intFlagValue;
                        refValue = (T)Enum.ToObject(refValue.GetType(), result);
                    }

                    return true;
                }
            }
        }
        else
        {
            using var combo = ImRaii.Combo(label, refValue.ToString(), ImGuiComboFlags.HeightLarge);
            if (!combo) return false;

            foreach (Enum enumValue in Enum.GetValues(refValue.GetType()))
            {
                if (!ImGui.Selectable(enumValue.ToString(), enumValue.Equals(refValue))) continue;
                refValue = (T)enumValue;
                return true;
            }
        }

        return false;
    }

    private static string GetPartLabel(in AtkUldPart part)
    {
        var texPath = part.UldAsset->AtkTexture.TextureType switch
        {
            TextureType.Resource => part.UldAsset->AtkTexture.Resource->TexFileResourceHandle->ResourceHandle.FileName.ToString(),
            TextureType.KernelTexture => "KernelTexture",
            _ => "N/A"
        };

        return $"{texPath} | U: {part.U} V: {part.V} | W: {part.Width} H: {part.Height}";
    }

    public static bool PartListSelector(IServiceProvider serviceProvider, AtkUldPartsList* partsList, ref uint partId)
    {
        if (partsList == null || partId > partsList->PartCount)
            return false;

        ref var currentPart = ref partsList->Parts[partId];
        var changed = false;

        /* not sure yet
        using (var combo = ImRaii.Combo("##PartId", $"[{partId}] {GetPartLabel(currentPart)}"))
        {
            if (combo)
            {
                for (var i = 0u; i < partsList->PartCount; i++)
                {
                    ref var part = ref partsList->Parts[i];
                    var isSelected = i == partId;

                    if (ImGui.Selectable($"[{i}] {GetPartLabel(part)}", isSelected))
                    {
                        partId = i;
                        changed = true;
                    }

                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }
            }
        }
        */

        PrintFieldValuePairs(
            ("PartId", partId.ToString()),
            ("Position", $"({currentPart.U}, {currentPart.V})"),
            ("Size", $"{currentPart.Width}x{currentPart.Height}")
        );

        var textureInfo = partsList->Parts[partId].UldAsset;
        var texType = textureInfo->AtkTexture.TextureType;

        if (texType == TextureType.Resource)
        {
            DrawCopyableText(textureInfo->AtkTexture.Resource->TexFileResourceHandle->ResourceHandle.FileName.ToString());

            /* explodes
            if (textureInfo->AtkTexture.Resource->IconId != 0)
            {
                var iconId = (int)textureInfo->AtkTexture.Resource->IconId;
                if (ImGui.InputInt("IconId", ref iconId))
                {
                    textureInfo->AtkTexture.LoadIconTexture((uint)iconId);
                }
            }
            */

            ref var kernelTexture = ref textureInfo->AtkTexture.Resource->KernelTextureObject;

            using var treeNode = ImRaii.TreeNode(
                $"Texture##{(ulong)kernelTexture->D3D11ShaderResourceView:X}",
                ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.DefaultOpen);

            if (treeNode)
            {
                ImGui.Image(
                    new ImTextureID(kernelTexture->D3D11ShaderResourceView),
                    new Vector2(
                        kernelTexture->ActualWidth,
                        kernelTexture->ActualHeight));
            }
        }
        else if (texType == TextureType.KernelTexture)
        {
            using var treeNode = ImRaii.TreeNode(
                $"Texture##{(ulong)textureInfo->AtkTexture.KernelTexture->D3D11ShaderResourceView:X}",
                ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.DefaultOpen);

            if (treeNode)
            {
                ImGui.Image(
                    new ImTextureID(textureInfo->AtkTexture.KernelTexture->D3D11ShaderResourceView),
                    new Vector2(
                        textureInfo->AtkTexture.KernelTexture->ActualWidth,
                        textureInfo->AtkTexture.KernelTexture->ActualHeight));
            }
        }

        return changed;
    }

    public static void PrintFieldValuePair(string fieldName, string value, bool copy = true)
    {
        ImGui.TextUnformatted(fieldName + ":");
        ImGuiUtils.SameLineSpace();
        if (copy)
        {
            DrawCopyableText(value);
        }
        else
        {
            ImGui.TextUnformatted(value);
        }
    }

    public static void PrintFieldValuePairs(params (string FieldName, string Value)[] pairs)
    {
        for (var i = 0; i < pairs.Length; i++)
        {
            if (i != 0)
            {
                ImGui.SameLine();
                ImGui.TextUnformatted("\u2022");
                ImGui.SameLine();
            }

            PrintFieldValuePair(pairs[i].FieldName, pairs[i].Value);
        }
    }

    public static void PaddedSeparator(uint mask = 0b11, float padding = 5f)
    {
        if ((mask & 0b10) > 0)
        {
            ImGui.Dummy(new(padding * ImGui.GetIO().FontGlobalScale));
        }

        ImGui.Separator();

        if ((mask & 0b01) > 0)
        {
            ImGui.Dummy(new(padding * ImGui.GetIO().FontGlobalScale));
        }
    }

    public static void DrawCopyableText(string text, string? textCopy = null, string? tooltipText = null, bool asSelectable = false, Color? textColor = null, string? highligtedText = null, bool noTooltip = false)
    {
        textCopy ??= text;

        using var color = textColor?.Push(ImGuiCol.Text);

        if (asSelectable)
        {
            ImGui.Selectable(text);
        }
        else if (!string.IsNullOrEmpty(highligtedText))
        {
            var pos = text.IndexOf(highligtedText, StringComparison.InvariantCultureIgnoreCase);
            if (pos != -1)
            {
                ImGui.TextUnformatted(text[..pos]);
                ImGui.SameLine(0, 0);

                using (Color.Yellow.Push(ImGuiCol.Text))
                    ImGui.TextUnformatted(text[pos..(pos + highligtedText.Length)]);

                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted(text[(pos + highligtedText.Length)..]);
            }
            else
            {
                ImGui.TextUnformatted(text);
            }
        }
        else
        {
            ImGui.TextUnformatted(text);
        }

        color?.Pop();

        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            if (!noTooltip)
                ImGui.SetTooltip(tooltipText ?? textCopy);
        }

        if (ImGui.IsItemClicked())
            ImGui.SetClipboardText(textCopy);
    }
}
