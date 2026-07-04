using System.Globalization;
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

    private static string GetPartLabel(AtkUldPart part)
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
            var pos = ImCursor.Position;
            var screenPos = ImCursor.ScreenPosition;

            ImGui.Image(new ImTextureID(tex->D3D11ShaderResourceView), size);

            var posAfter = ImCursor.Position;
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
                        : Color.Text200.ToUInt());

                ImCursor.Position = pos + partPos;
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

                ImGuiContextMenu.Draw(popupKey, builder =>
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

            ImCursor.Position = posAfter;
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
                var x = ImCursor.X;
                ImGuiUtils.DrawCopyableText($"({part.U}, {part.V})", new CopyableTextOptions()
                {
                    CopyText = ImGui.IsKeyDown(ImGuiKey.LeftShift)
                        ? $"new Vector2({part.U}, {part.V})"
                        : null
                });

                ImGui.Text("Size:    ");
                ImGui.SameLine();
                ImCursor.X = x;
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
        ImCursor.SameLineSpace();
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

    public static ImRaii.ChildDisposable AlertBox(string id, Color color, Vector2 size)
    {
        var colors = ImRaii
            .PushColor(ImGuiCol.ChildBg, (color with { A = 0.1f }).ToUInt())
            .Push(ImGuiCol.Border, (color with { A = 0.4f }).ToUInt());
        var style = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 3);
        var child = ImRaii.Child(id, size, true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        style.Dispose();
        colors.Dispose();
        return child;
    }

    private static void DrawAlert(string id, string text, GameIconLookup icon, Color color)
    {
        var maxWidth = ImStyle.ContentRegionAvail.X;
        var innerTextWidth = maxWidth - ImStyle.FramePadding.X * 2 - ImStyle.ItemInnerSpacing.X * 2 - ImStyle.TextLineHeight;
        var textSize = ImGui.CalcTextSize(text, wrapWidth: innerTextWidth);
        var size = new Vector2(maxWidth, textSize.Y + ImStyle.FramePadding.Y * 2 + ImStyle.ItemInnerSpacing.Y * 2);

        using (AlertBox(id, color, size))
        {
            if (ServiceLocator.TryGetService<ITextureProvider>(out var textureProvider))
                textureProvider.DrawIcon(icon, ImStyle.TextLineHeight);
            else
                ImGui.Dummy(new Vector2(ImStyle.TextLineHeight));
            ImGui.SameLine();
            ImGui.TextWrapped(text);
        }
    }

    public static void DrawAlertInfo(string id, string text)
    {
        DrawAlert(id, text, 60071, Color.FromHSV(0.527f, 1, 1));
    }

    public static void DrawAlertWarning(string id, string text)
    {
        DrawAlert(id, text, 60073, Color.FromHSV(0.138f, 1, 1));
    }

    public static void DrawAlertError(string id, string text)
    {
        DrawAlert(id, text, 60074, Color.FromHSV(0, 1, 1));
    }

    // https://github.com/ocornut/imgui/commit/c895e987
    // size_arg (for each axis) < 0.0f: align to end, 0.0f: auto, > 0.0f: specified size
    public static void ProgressBar(float fraction, Vector2 size_arg, string? overlay = null)
    {
        var window = ImGuiP.GetCurrentWindow();
        if (window.SkipItems)
            return;

        var g = ImGui.GetCurrentContext();
        var style = g.Style;

        var pos = window.DC.CursorPos;
        var size = ImGuiP.CalcItemSize(size_arg, ImGui.CalcItemWidth(), g.FontSize + style.FramePadding.Y * 2.0f);
        ImRect bb = new(pos, pos + size);
        ImGuiP.ItemSize(size, style.FramePadding.Y);
        if (!ImGuiP.ItemAdd(bb, 0))
            return;

        // Fraction < 0.0f will display an indeterminate progress bar animation
        // The value must be animated along with time, so e.g. passing '-1.0f * ImGui::GetTime()' as fraction works.
        var is_indeterminate = fraction < 0.0f;
        if (!is_indeterminate)
            fraction = ImGuiP.ImSaturate(fraction);

        // Out of courtesy we accept a NaN fraction without crashing
        var fill_n0 = 0.0f;
        var fill_n1 = !float.IsNaN(fraction) ? fraction : 0.0f;

        if (is_indeterminate)
        {
            const float fill_width_n = 0.2f;
            fill_n0 = -fraction % 1.0f * (1.0f + fill_width_n) - fill_width_n;
            fill_n1 = ImGuiP.ImSaturate(fill_n0 + fill_width_n);
            fill_n0 = ImGuiP.ImSaturate(fill_n0);
        }

        // Render
        ImGuiP.RenderFrame(bb.Min, bb.Max, ImGui.GetColorU32(ImGuiCol.FrameBg), true, style.FrameRounding);
        bb.Expand(new Vector2(-style.FrameBorderSize, -style.FrameBorderSize));
        ImGuiP.RenderRectFilledRangeH(window.DrawList, bb, ImGui.GetColorU32(ImGuiCol.PlotHistogram), fill_n0, fill_n1, style.FrameRounding);

        // Default displaying the fraction as percentage string, but user can override it
        // Don't display text for indeterminate bars by default
        if (!is_indeterminate || !string.IsNullOrEmpty(overlay))
        {
            if (string.IsNullOrEmpty(overlay))
            {
                overlay = string.Format(CultureInfo.InvariantCulture, "{0:P1}", fraction + 0.01f);
            }

            var overlay_size = ImGui.CalcTextSize(overlay);
            if (overlay_size.X > 0.0f)
            {
                var text_x = is_indeterminate ? (bb.Min.X + bb.Max.X - overlay_size.X) * 0.5f : MathUtils.Lerp(bb.Min.X, bb.Max.X, fill_n1) + style.ItemSpacing.X;
                ImGuiP.RenderTextClipped(new Vector2(Math.Clamp(text_x, bb.Min.X, bb.Max.X - overlay_size.X - style.ItemInnerSpacing.X), bb.Min.Y), bb.Max, overlay, overlay_size, new Vector2(0.0f, 0.5f), bb);
            }
        }
    }
}
