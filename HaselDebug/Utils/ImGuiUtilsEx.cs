using Dalamud.Interface.Textures;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselDebug.Services;

namespace HaselDebug.Utils;

public static unsafe class ImGuiUtilsEx
{
    private static int TexDisplayStyle = 0;

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

        PrintFieldValuePairs(
            ("PartId", partId.ToString()),
            ("Position", $"({currentPart.U}, {currentPart.V})"),
            ("Size", $"{currentPart.Width}x{currentPart.Height}")
        );

        var asset = currentPart.UldAsset;
        var texType = asset->AtkTexture.TextureType;

        if (!asset->AtkTexture.IsTextureReady())
        {
            ImGui.Text("Texture not ready"u8);
            return changed;
        }

        var path = string.Empty;
        var version = 1;
        if (asset->AtkTexture.TextureType == TextureType.Resource)
        {
            path = asset->AtkTexture.Resource->TexFileResourceHandle->ResourceHandle.FileName.ToString();
            version = asset->AtkTexture.Resource->Version;
        }

        var kernelTexture = asset->AtkTexture.GetKernelTexture();
        if (kernelTexture == null)
        {
            ImGui.Text("No KernelTexture"u8);
            return changed;
        }

        if (!ServiceLocator.TryGetService<DebugRenderer>(out var debugRenderer))
            return changed;

        var tex = asset->AtkTexture.GetKernelTexture();
        var nodeOptions = new NodeOptions
        {
            AddressPath = new AddressPath((nint)asset),
            DefaultOpen = true,
            DrawContextMenu = (nodeOptions, builder) =>
            {
                builder.Add(new ImGuiContextMenuEntry()
                {
                    Visible = !string.IsNullOrEmpty(path),
                    Label = "Copy Path",
                    ClickCallback = () => ImGui.SetClipboardText(path)
                });

                builder.Add(new ImGuiContextMenuEntry()
                {
                    Label = "Copy Size",
                    ClickCallback = () => ImGui.SetClipboardText(ImGui.IsKeyDown(ImGuiKey.LeftShift)
                        ? $"new Vector2({tex->ActualWidth}, {tex->ActualHeight})"
                        : $"{tex->ActualWidth}x{tex->ActualHeight}")
                });

                builder.Add(new ImGuiContextMenuEntry()
                {
                    Label = "Copy Format",
                    ClickCallback = () => ImGui.SetClipboardText($"{tex->TextureFormat}")
                });
            }
        };

        var texInfo = $"{tex->ActualWidth}x{tex->ActualHeight}, {tex->TextureFormat}";
        nodeOptions.Title = string.IsNullOrEmpty(path)
            ? texInfo
            : $"{path} ({texInfo})";

        using var node = debugRenderer.DrawTreeNode(nodeOptions);
        if (!node)
            return changed;

        if (ImGui.RadioButton("Parts List##TextureDisplayStyle0"u8, TexDisplayStyle == 0))
        {
            TexDisplayStyle = 0;
        }

        ImGui.SameLine();

        if (ImGui.RadioButton("Full Image##TextureDisplayStyle1"u8, TexDisplayStyle == 1))
        {
            TexDisplayStyle = 1;
        }

        var size = new Vector2(tex->ActualWidth, tex->ActualHeight);

        if (TexDisplayStyle == 1)
        {
            var pos = ImGui.GetCursorPos();
            var screenPos = ImGui.GetCursorScreenPos();

            ImGui.Image(new ImTextureID(tex->D3D11ShaderResourceView), size);

            var posAfter = ImGui.GetCursorPos();
            var drawList = ImGui.GetWindowDrawList();

            for (var i = 0u; i < partsList->PartCount; i++)
            {
                var part = partsList->Parts[i];
                var partPos = new Vector2(part.U, part.V) * version;
                var partSize = new Vector2(part.Width, part.Height) * version;
                var partSelected = i == partId;

                drawList.AddRect(
                    screenPos + partPos,
                    screenPos + partPos + partSize,
                    partSelected
                        ? Color.Gold.ToUInt()
                        : Color.Grey4.ToUInt());

                ImGui.SetCursorPos(pos + partPos);
                ImGui.Dummy(partSize);
                var popupKey = $"##Asset{(nint)asset}_Part{i}";

                if (ImGui.IsItemHovered() || ImGui.IsPopupOpen(popupKey))
                {
                    var text = $"#{i}";
                    var textSize = ImGui.CalcTextSize(text);
                    var textPos = screenPos + partPos + partSize / 2f - textSize / 2f;

                    drawList.AddRectFilled(
                        screenPos + partPos + Vector2.One,
                        screenPos + partPos + partSize - Vector2.One,
                        (Color.Black with { A = 0.5f }).ToUInt());

                    drawList.AddText(
                        textPos - Vector2.One,
                        Color.Black.ToUInt(),
                        text);

                    drawList.AddText(
                        textPos + Vector2.One,
                        Color.Black.ToUInt(),
                        text);

                    drawList.AddText(
                        textPos,
                        Color.Gold.ToUInt(),
                        text);
                }

                if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                {
                    partId = i;
                    changed |= true;
                }

                if (ServiceLocator.TryGetService<ImGuiContextMenuService>(out var contextMenuService))
                {
                    contextMenuService.Draw(popupKey, builder =>
                    {
                        builder.Add(new ImGuiContextMenuEntry()
                        {
                            Label = "Copy PartId",
                            ClickCallback = () => ImGui.SetClipboardText(i.ToString())
                        });
                        builder.Add(new ImGuiContextMenuEntry()
                        {
                            Label = "Copy Position",
                            ClickCallback = () => ImGui.SetClipboardText(ImGui.IsKeyDown(ImGuiKey.LeftShift)
                                ? $"new Vector2({part.U}, {part.V})"
                                : $"({part.U}, {part.V})")
                        });
                        builder.Add(new ImGuiContextMenuEntry()
                        {
                            Label = "Copy Size",
                            ClickCallback = () => ImGui.SetClipboardText(ImGui.IsKeyDown(ImGuiKey.LeftShift)
                                ? $"new Vector2({part.Width}, {part.Height})"
                                : $"{part.Width}x{part.Height}")
                        });
                    });
                }
            }

            ImGui.SetCursorPos(posAfter);
        }
        else
        {
            using var tbl = ImRaii.Table($"partsTable##{(nint)asset:X}", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Reorderable);
            if (!tbl)
                return changed;

            var maxPartWidth = 60;

            for (var i = 0u; i < partsList->PartCount; i++)
            {
                ref var part = ref partsList->Parts[i];
                if (maxPartWidth < part.Width * version)
                    maxPartWidth = part.Width * version;
            }

            ImGui.TableSetupColumn("PartId"u8, ImGuiTableColumnFlags.WidthFixed, 40);
            ImGui.TableSetupColumn("Texture"u8, ImGuiTableColumnFlags.WidthFixed, maxPartWidth);
            ImGui.TableSetupColumn("Info"u8, ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            for (var i = 0u; i < partsList->PartCount; i++)
            {
                ref var part = ref partsList->Parts[i];
                var selected = i == partId;

                ImGui.TableNextRow();

                ImGui.TableNextColumn(); // PartId
                if (selected) ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, (Color.Gold with { A = 0.5f }).ToUInt());

                ImGuiUtils.DrawCopyableText(i.ToString());

                ImGui.TableNextColumn(); // Texture
                if (selected) ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, (Color.Gold with { A = 0.5f }).ToUInt());

                ImGui.Image(
                    new ImTextureID(tex->D3D11ShaderResourceView),
                    new Vector2(part.Width, part.Height) * version,
                    new Vector2(part.U, part.V) * version / size,
                    new Vector2(part.U + part.Width, part.V + part.Height) * version / size);

                if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                {
                    partId = i;
                    changed |= true;
                }

                ImGui.TableNextColumn(); // Info
                if (selected) ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, (Color.Gold with { A = 0.5f }).ToUInt());

                ImGui.Text("Position:");
                ImGui.SameLine();
                var x = ImGui.GetCursorPosX();
                ImGuiUtils.DrawCopyableText($"({part.U}, {part.V})", new CopyableTextOptions()
                {
                    CopyText = ImGui.IsKeyDown(ImGuiKey.LeftShift)
                        ? $"new Vector2({part.U}, {part.V})"
                        : null
                });

                ImGui.Text("Size:    ");
                ImGui.SameLine();
                ImGui.SetCursorPosX(x);
                ImGuiUtils.DrawCopyableText($"{part.Width}x{part.Height}", new CopyableTextOptions()
                {
                    CopyText = ImGui.IsKeyDown(ImGuiKey.LeftShift)
                        ? $"new Vector2({part.Width}, {part.Height})"
                        : null
                });
            }
        }

        return changed;
    }

    public static void PrintFieldValuePair(string fieldName, string value, bool copy = true)
    {
        ImGui.Text(fieldName + ":");
        ImGuiUtils.SameLineSpace();
        if (copy)
        {
            ImGuiUtils.DrawCopyableText(value);
        }
        else
        {
            ImGui.Text(value);
        }
    }

    public static void PrintFieldValuePairs(params (string FieldName, string Value)[] pairs)
    {
        for (var i = 0; i < pairs.Length; i++)
        {
            if (i != 0)
            {
                ImGui.SameLine();
                ImGui.Text("\u2022"u8);
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

    public static ImRaii.EndUnconditionally AlertBox(string id, Color color, Vector2 size)
    {
        var colors = ImRaii
            .PushColor(ImGuiCol.ChildBg, (color with { A = 0.1f }).ToUInt())
            .Push(ImGuiCol.Border, (color with { A = 0.4f }).ToUInt());
        var style = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 3);
        var child = ImRaii.Child(id, size, true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        style.Dispose();
        colors.Dispose();
        return new ImRaii.EndUnconditionally(child.Dispose, true);
    }

    private static void DrawAlert(string id, string text, GameIconLookup icon, Color color)
    {
        var maxWidth = ImGui.GetContentRegionAvail().X;
        var innerTextWidth = maxWidth - ImGui.GetStyle().FramePadding.X * 2 - ImGui.GetStyle().ItemInnerSpacing.X * 2 - ImGui.GetTextLineHeight();
        var textSize = ImGui.CalcTextSize(text, wrapWidth: innerTextWidth);
        var size = new Vector2(maxWidth, textSize.Y + ImGui.GetStyle().FramePadding.Y * 2 + ImGui.GetStyle().ItemInnerSpacing.Y * 2);

        using (AlertBox(id, color, size))
        {
            if (ServiceLocator.TryGetService<ITextureProvider>(out var textureProvider))
                textureProvider.DrawIcon(icon, ImGui.GetTextLineHeight());
            else
                ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight()));
            ImGui.SameLine();
            ImGui.TextWrapped(text);
        }
    }

    public static void DrawAlertInfo(string id, string text)
    {
        DrawAlert(id, text, 60071, Color.FromHSL(190, 1f, 0.5f));
    }

    public static void DrawAlertWarning(string id, string text)
    {
        DrawAlert(id, text, 60073, Color.FromHSL(50, 1f, 0.5f));
    }

    public static void DrawAlertError(string id, string text)
    {
        DrawAlert(id, text, 60074, Color.FromHSL(0, 1f, 0.5f));
    }
}
