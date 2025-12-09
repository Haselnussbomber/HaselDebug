using System.Text;
using Dalamud.Game.Text.Noun.Enums;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.ImGuiSeStringRenderer;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using HaselDebug.Utils;
using HaselDebug.Windows;
using Lumina.Data;
using Lumina.Text.Expressions;

namespace HaselDebug.Services;

public unsafe partial class DebugRenderer
{
    private readonly Dictionary<MacroCode, string[]> _expressionNames = new()
    {
        { MacroCode.SetResetTime, ["Hour", "WeekDay"] },
        { MacroCode.SetTime, ["Time"] },
        { MacroCode.If, ["Condition", "StatementTrue", "StatementFalse"] },
        { MacroCode.Switch, ["Condition"] },
        { MacroCode.PcName, ["EntityId"] },
        { MacroCode.IfPcGender, ["EntityId", "CaseMale", "CaseFemale"] },
        { MacroCode.IfPcName, ["EntityId", "CaseTrue", "CaseFalse"] },
        // { MacroCode.Josa, [] },
        // { MacroCode.Josaro, [] },
        { MacroCode.IfSelf, ["EntityId", "CaseTrue", "CaseFalse"] },
        // { MacroCode.NewLine, [] },
        { MacroCode.Wait, ["Seconds"] },
        { MacroCode.Icon, ["IconId"] },
        { MacroCode.Color, ["Color"] },
        { MacroCode.EdgeColor, ["Color"] },
        { MacroCode.ShadowColor, ["Color"] },
        // { MacroCode.SoftHyphen, [] },
        // { MacroCode.Key, [] },
        // { MacroCode.Scale, [] },
        { MacroCode.Bold, ["Enabled"] },
        { MacroCode.Italic, ["Enabled"] },
        // { MacroCode.Edge, [] },
        // { MacroCode.Shadow, [] },
        // { MacroCode.NonBreakingSpace, [] },
        { MacroCode.Icon2, ["IconId"] },
        // { MacroCode.Hyphen, [] },
        { MacroCode.Num, ["Value"] },
        { MacroCode.Hex, ["Value"] },
        { MacroCode.Kilo, ["Value", "Separator"] },
        { MacroCode.Byte, ["Value"] },
        { MacroCode.Sec, ["Time"] },
        { MacroCode.Time, ["Value"] },
        { MacroCode.Float, ["Value", "Radix", "Separator"] },
        { MacroCode.Link, ["Type"] },
        { MacroCode.Sheet, ["SheetName", "RowId", "ColumnIndex", "ColumnParam"] },
        { MacroCode.String, ["String"] },
        { MacroCode.Caps, ["String"] },
        { MacroCode.Head, ["String"] },
        { MacroCode.Split, ["String", "Separator"] },
        { MacroCode.HeadAll, ["String"] },
        // { MacroCode.Fixed, [] },
        { MacroCode.Lower, ["String"] },
        { MacroCode.JaNoun, ["SheetName", "ArticleType", "RowId", "Amount", "Case", "UnkInt5"] },
        { MacroCode.EnNoun, ["SheetName", "ArticleType", "RowId", "Amount", "Case", "UnkInt5"] },
        { MacroCode.DeNoun, ["SheetName", "ArticleType", "RowId", "Amount", "Case", "UnkInt5"] },
        { MacroCode.FrNoun, ["SheetName", "ArticleType", "RowId", "Amount", "Case", "UnkInt5"] },
        { MacroCode.ChNoun, ["SheetName", "ArticleType", "RowId", "Amount", "Case", "UnkInt5"] },
        { MacroCode.LowerHead, ["String"] },
        { MacroCode.ColorType, ["ColorType"] },
        { MacroCode.EdgeColorType, ["ColorType"] },
        { MacroCode.Digit, ["Value", "TargetLength"] },
        { MacroCode.Ordinal, ["Value"] },
        { MacroCode.Sound, ["IsJingle", "SoundId"] },
        { MacroCode.LevelPos, ["LevelId"] },
    };

    private const LinkMacroPayloadType DalamudLinkType = (LinkMacroPayloadType)Payload.EmbeddedInfoType.DalamudLink - 1;

    private readonly Dictionary<LinkMacroPayloadType, string[]> _linkExpressionNames = new()
    {
        { LinkMacroPayloadType.Character, ["Flags", "WorldId"] },
        { LinkMacroPayloadType.Item, ["ItemId", "Rarity"] },
        { LinkMacroPayloadType.MapPosition, ["TerritoryType/MapId", "RawX", "RawY"] },
        { LinkMacroPayloadType.Quest, ["RowId"] },
        { LinkMacroPayloadType.Achievement, ["RowId"] },
        { LinkMacroPayloadType.HowTo, ["RowId"] },
        // PartyFinderNotification
        { LinkMacroPayloadType.Status, ["StatusId"] },
        { LinkMacroPayloadType.PartyFinder, ["ListingId", string.Empty, "WorldId"] },
        { LinkMacroPayloadType.AkatsukiNote, ["RowId"] },
        { LinkMacroPayloadType.Description, ["RowId"] },
        { LinkMacroPayloadType.WKSPioneeringTrail, ["RowId", "SubrowId"] },
        { LinkMacroPayloadType.MKDLore, ["RowId"] },
        { DalamudLinkType, ["CommandId", "Extra1", "Extra2", "ExtraString"] },
    };

    private readonly Dictionary<uint, string[]> _fixedExpressionNames = new()
    {
        { 1, ["Type0", "Type1", "WorldId"] },
        { 2, ["Type0", "Type1", "ClassJobId", "Level"] },
        { 3, ["Type0", "Type1", "TerritoryTypeId", "Instance & MapId", "RawX", "RawY", "RawZ", "PlaceNameIdOverride"] },
        { 4, ["Type0", "Type1", "ItemId", "Rarity", string.Empty, string.Empty, "Item Name"] },
        { 5, ["Type0", "Type1", "Sound Effect Id"] },
        { 6, ["Type0", "Type1", "ObjStrId"] },
        { 7, ["Type0", "Type1", "Text"] },
        { 8, ["Type0", "Type1", "Seconds"] },
        { 9, ["Type0", "Type1", string.Empty] },
        { 10, ["Type0", "Type1", "StatusId", "HasOverride", "NameOverride", "DescriptionOverride"] },
        { 11, ["Type0", "Type1", "ListingId", string.Empty, "WorldId", "CrossWorldFlag"] },
        { 12, ["Type0", "Type1", "QuestId", string.Empty, string.Empty, string.Empty, "QuestName"] },
    };

    public void DrawUtf8String(nint address, NodeOptions nodeOptions)
    {
        if (address == 0)
        {
            ImGui.Text("null"u8);
            return;
        }

        if (!MemoryUtils.IsPointerValid(address))
        {
            ImGui.Text("invalid"u8);
            return;
        }

        nodeOptions = nodeOptions.WithAddress(address);

        var str = (Utf8String*)address;
        DrawSeString(str->StringPtr, nodeOptions);
    }

    public void DrawSeString(byte* ptr, NodeOptions nodeOptions)
    {
        if (ptr == null)
        {
            ImGui.Text("null"u8);
            return;
        }

        if (!MemoryUtils.IsPointerValid(ptr))
        {
            ImGui.Text("invalid"u8);
            return;
        }

        nodeOptions = nodeOptions.WithAddress((nint)ptr);
        DrawSeString(new ReadOnlySeStringSpan(ptr), nodeOptions);
    }

    public void DrawSeString(ReadOnlySeStringSpan rosss, NodeOptions nodeOptions)
    {
        if (rosss.PayloadCount == 0)
        {
            ImGui.Dummy(Vector2.Zero);
            return;
        }

        nodeOptions = nodeOptions.WithAddress(rosss.GetHashCode());

        var clicked = false;

        if (nodeOptions.RenderSeString)
        {
            clicked = ImGui.Selectable(nodeOptions.GetKey("SeStringSelectable"));
            ImGui.SameLine(0, 0);
            ImGuiHelpers.SeStringWrapped(rosss, new()
            {
                GetEntity = (scoped in SeStringDrawState state, int byteOffset) =>
                {
                    var span = state.Span[byteOffset..];
                    if (span.Length != 0 && span[0] == '\n')
                        return new SeStringReplacementEntity(1, new Vector2(3, state.FontSize), (scoped in SeStringDrawState state, int byteOffset, Vector2 offset) => { });
                    if (span.Length >= 4 && span[0] == 0x02 && span[1] == (byte)MacroCode.NewLine && span[2] == 0x01 && span[3] == 0x03)
                        return new SeStringReplacementEntity(4, new Vector2(3, state.FontSize), (scoped in SeStringDrawState state, int byteOffset, Vector2 offset) => { });

                    return default;
                },
                ForceEdgeColor = true,
                WrapWidth = 9999
            });
        }
        else
        {
            var text = rosss.ToMacroString();

            using (ImRaii.PushColor(ImGuiCol.Text, ColorTreeNode.ToVector(), nodeOptions.RenderSeString))
                clicked = ImGui.Selectable(text + nodeOptions.GetKey("SeStringSelectable"));

            _imGuiContextMenu.Draw(nodeOptions.GetKey("SeStringSelectableContextMenu"), (builder) =>
            {
                builder.Add(new ImGuiContextMenuEntry()
                {
                    Label = _textService.Translate("ContextMenu.CopyText"),
                    ClickCallback = () => ImGui.SetClipboardText(text)
                });
            });
        }

        if (clicked)
        {
            var str = new ReadOnlySeString(rosss.Data.ToArray());
            var windowTitle = nodeOptions.Title ?? (nodeOptions.SeStringTitle ?? str).ToMacroString();
            var language = nodeOptions.Language ?? _languageProvider.ClientLanguage;
            _windowManager.CreateOrOpen(windowTitle, () => new SeStringInspectorWindow(_windowManager, _textService, _addonObserver, _serviceProvider)
            {
                String = str,
                Language = language,
                WindowName = windowTitle,
            });
        }
    }

    public void DrawSeString(ReadOnlySeStringSpan rosss, bool asTreeNode, NodeOptions nodeOptions)
    {
        if (rosss.PayloadCount == 0)
        {
            ImGui.Dummy(Vector2.Zero);
            return;
        }

        nodeOptions = nodeOptions.WithAddress(rosss.GetHashCode());

        using var node = asTreeNode ? DrawTreeNode(nodeOptions.WithSeStringTitle(rosss)) : null;
        if (asTreeNode && !node!) return;

        if (!asTreeNode && nodeOptions.RenderSeString)
        {
            ImGuiHelpers.SeStringWrapped(rosss, new()
            {
                ForceEdgeColor = true,
            });
        }

        nodeOptions = nodeOptions.ConsumeTreeNodeOptions() with { DefaultOpen = true };

        var payloadIdx = -1;
        foreach (var payload in rosss)
        {
            payloadIdx++;

            var preview = payload.Type.ToString();
            if (payload.Type == ReadOnlySePayloadType.Macro)
                preview += $": {payload.MacroCode}";

            var payloadNodeOptions = nodeOptions
                .WithAddress(payloadIdx)
                .WithSeStringTitle($"[{payloadIdx}] {preview}");

            using var payloadNode = DrawTreeNode(payloadNodeOptions);
            if (!payloadNode) continue;

            nodeOptions = nodeOptions.ConsumeTreeNodeOptions() with { DefaultOpen = true, Indent = true };

            using var table = ImRaii.Table($"##Payload{payloadIdx}_{payload.GetHashCode()}Table", 2);
            if (!table) return;

            ImGui.TableSetupColumn("Label"u8, ImGuiTableColumnFlags.WidthFixed, 120);
            ImGui.TableSetupColumn("Tree"u8, ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(payload.Type == ReadOnlySePayloadType.Text ? "Text" : "ToString()");
            ImGui.TableNextColumn();
            var text = payload.ToString();
            ImGuiUtils.DrawCopyableText($"\"{text}\"", new() { CopyText = text });

            if (payload.Type != ReadOnlySePayloadType.Macro)
                continue;

            if (payload.ExpressionCount > 0)
            {
                var exprIdx = 0;
                uint? subType = null;
                uint? fixedType = null;

                if (payload.MacroCode == MacroCode.Link && payload.TryGetExpression(out var linkExpr1) && linkExpr1.TryGetUInt(out var linkExpr1Val))
                {
                    subType = linkExpr1Val;
                }
                else if (payload.MacroCode == MacroCode.Fixed && payload.TryGetExpression(out var fixedTypeExpr, out var linkExpr2) && fixedTypeExpr.TryGetUInt(out var fixedTypeVal) && linkExpr2.TryGetUInt(out var linkExpr2Val))
                {
                    subType = linkExpr2Val;
                    fixedType = fixedTypeVal;
                }

                foreach (var expr in payload)
                {
                    DrawExpression(payload.MacroCode, subType, fixedType, exprIdx++, expr, payloadNodeOptions with
                    {
                        SeStringTitle = null,
                        DefaultOpen = true,
                        AddressPath = nodeOptions.AddressPath.With([payloadIdx, exprIdx]),
                    });
                }
            }
        }
    }

    private void DrawExpression(MacroCode macroCode, uint? subType, uint? fixedType, int idx, ReadOnlySeExpressionSpan expr, NodeOptions nodeOptions)
    {
        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        var expressionName = GetExpressionName(macroCode, subType, idx, expr);
        ImGui.Text($"[{idx}] " + (string.IsNullOrEmpty(expressionName) ? $"Expr {idx}" : expressionName));

        ImGui.TableNextColumn();

        if (expr.Body.IsEmpty)
        {
            ImGui.Text("(?)"u8);
            return;
        }

        if (expr.TryGetUInt(out var u32))
        {
            if (macroCode is MacroCode.Icon or MacroCode.Icon2 && idx == 0)
            {
                _gfdService.Draw(u32, ImGui.GetTextLineHeight());
                ImGui.SameLine();
            }

            ImGuiUtils.DrawCopyableText(u32.ToString());
            ImGui.SameLine();
            ImGuiUtils.DrawCopyableText($"0x{u32:X}");

            if (macroCode == MacroCode.Link && idx == 0)
            {
                var name = subType != null && (LinkMacroPayloadType)subType == DalamudLinkType
                    ? "Dalamud"
                    : Enum.GetName((LinkMacroPayloadType)u32);

                if (!string.IsNullOrEmpty(name))
                {
                    ImGui.SameLine();
                    ImGui.Text(name);
                }
            }

            if (macroCode is MacroCode.JaNoun or MacroCode.EnNoun or MacroCode.DeNoun or MacroCode.FrNoun && idx == 1)
            {
                var language = macroCode switch
                {
                    MacroCode.JaNoun => Language.Japanese,
                    MacroCode.DeNoun => Language.German,
                    MacroCode.FrNoun => Language.French,
                    _ => Language.English,
                };
                var articleTypeEnumType = language switch
                {
                    Language.Japanese => typeof(JapaneseArticleType),
                    Language.German => typeof(GermanArticleType),
                    Language.French => typeof(FrenchArticleType),
                    _ => typeof(EnglishArticleType)
                };
                ImGui.SameLine();
                ImGui.Text(Enum.GetName(articleTypeEnumType, u32));
            }

            if (macroCode is MacroCode.Fixed && subType != null && fixedType != null && fixedType is 100 or 200 && subType == 5 && idx == 2)
            {
                ImGui.SameLine();
                if (ImGui.SmallButton("Play"))
                {
                    UIGlobals.PlayChatSoundEffect(u32 + 1);
                }
            }

            if (macroCode is MacroCode.Link && subType != null && idx == 1)
            {
                switch ((LinkMacroPayloadType)subType)
                {
                    case LinkMacroPayloadType.Item:
                        ImGui.SameLine();
                        ImGui.Text(_textService.GetItemName(u32).ToString());
                        break;

                    case LinkMacroPayloadType.Quest:
                        ImGui.SameLine();
                        ImGui.Text(_textService.GetQuestName(u32));
                        break;

                    case LinkMacroPayloadType.Achievement when _dataManager.GetExcelSheet<Achievement>(_languageProvider.ClientLanguage).TryGetRow(u32, out var achievementRow):
                        ImGui.SameLine();
                        ImGui.Text(achievementRow.Name.ToString());
                        break;

                    case LinkMacroPayloadType.HowTo when _dataManager.GetExcelSheet<HowTo>(_languageProvider.ClientLanguage).TryGetRow(u32, out var howToRow):
                        ImGui.SameLine();
                        ImGui.Text(howToRow.Name.ToString());
                        break;

                    case LinkMacroPayloadType.Status when _dataManager.GetExcelSheet<Status>(_languageProvider.ClientLanguage).TryGetRow(u32, out var statusRow):
                        ImGui.SameLine();
                        ImGui.Text(statusRow.Name.ToString());
                        break;

                    case LinkMacroPayloadType.AkatsukiNote when
                        _dataManager.GetSubrowExcelSheet<AkatsukiNote>(_languageProvider.ClientLanguage).TryGetRow(u32, out var akatsukiNoteRow) &&
                        akatsukiNoteRow[0].ListName.IsValid:
                        ImGui.SameLine();
                        ImGui.Text(akatsukiNoteRow[0].ListName.Value.Text.ToString());
                        break;
                }
            }

            // TODO: clickable link to open row in new window :O

            return;
        }

        if (expr.TryGetString(out var s))
        {
            DrawSeString(s, false, nodeOptions with { DefaultOpen = true });
            // ImGui.Text($"\"{s.ToString().Replace("\\", "\\\\").Replace("\"", "\\\"")}\"");
            return;
        }

        if (expr.TryGetPlaceholderExpression(out var exprType))
        {
            if (((ExpressionType)exprType).GetNativeName() is { } nativeName)
            {
                ImGui.Text(nativeName);
                return;
            }

            ImGui.Text($"?x{exprType:X02}");
            return;
        }

        if (expr.TryGetParameterExpression(out exprType, out var e1))
        {
            if (((ExpressionType)exprType).GetNativeName() is { } nativeName)
            {
                ImGui.Text($"{nativeName}({e1.ToString()})");
                return;
            }

            throw new InvalidOperationException("All native names must be defined for unary expressions.");
        }

        if (expr.TryGetBinaryExpression(out exprType, out e1, out var e2))
        {
            if (((ExpressionType)exprType).GetNativeName() is { } nativeName)
            {
                ImGui.Text($"{e1.ToString()} {nativeName} {e2.ToString()}");
                return;
            }

            throw new InvalidOperationException("All native names must be defined for binary expressions.");
        }

        var sb = new StringBuilder();
        sb.EnsureCapacity(1 + 3 * expr.Body.Length);
        sb.Append($"({expr.Body[0]:X02}");
        for (var i = 1; i < expr.Body.Length; i++)
            sb.Append($" {expr.Body[i]:X02}");
        sb.Append(')');
        ImGui.Text(sb.ToString());
    }

    private string GetExpressionName(MacroCode macroCode, uint? subType, int idx, ReadOnlySeExpressionSpan expr)
    {
        if (_expressionNames.TryGetValue(macroCode, out var names) && idx < names.Length)
            return names[idx];

        if (macroCode == MacroCode.Switch)
            return $"Case {idx - 1}";

        if (macroCode == MacroCode.Link && subType != null && _linkExpressionNames.TryGetValue((LinkMacroPayloadType)subType, out var linkNames) && idx - 1 < linkNames.Length)
            return linkNames[idx - 1];

        if (macroCode == MacroCode.Fixed && subType != null && _fixedExpressionNames.TryGetValue((uint)subType, out var fixedNames) && idx < fixedNames.Length)
            return fixedNames[idx];

        if (macroCode == MacroCode.Link && idx == 4)
            return "Copy String";

        return string.Empty;
    }
}
