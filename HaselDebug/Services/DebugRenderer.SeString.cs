using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.ImGuiSeStringRenderer;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.System.String;
using HaselCommon.Services;
using HaselDebug.Utils;
using HaselDebug.Windows;
using ImGuiNET;
using Lumina.Data;
using Lumina.Text.Expressions;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

namespace HaselDebug.Services;

public unsafe partial class DebugRenderer
{
    private readonly Dictionary<MacroCode, string[]> ExpressionNames = new()
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
        // { MacroCode.Num, [] },
        { MacroCode.Hex, ["Value"] },
        { MacroCode.Kilo, ["Value", "Separator"] },
        { MacroCode.Byte, ["Value"] },
        { MacroCode.Sec, ["Time"] },
        { MacroCode.Time, ["Value"] },
        { MacroCode.Float, ["Value", "Radix", "Separator"] },
        { MacroCode.Link, ["Type"] },
        { MacroCode.Sheet, ["SheetName", "RowId", "ColumnIndex", "ColumnParam"] },
        // { MacroCode.String, [] },
        // { MacroCode.Caps, [] },
        { MacroCode.Head, ["String"] },
        // { MacroCode.Split, [] },
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
        // { MacroCode.LevelPos, [] },
    };

    private const LinkMacroPayloadType DalamudLinkType = (LinkMacroPayloadType)Payload.EmbeddedInfoType.DalamudLink - 1;

    private readonly Dictionary<LinkMacroPayloadType, string[]> LinkExpressionNames = new()
    {
        { LinkMacroPayloadType.Character, ["Flags", "WorldId"] },
        { LinkMacroPayloadType.Item, ["ItemId", "Rarity"] },
        { LinkMacroPayloadType.MapPosition, ["TerritoryType/MapId", "Raw X", "Raw Y"] },
        { LinkMacroPayloadType.Quest, ["QuestId"] },
        { LinkMacroPayloadType.Achievement, ["AchievementId"] },
        { LinkMacroPayloadType.HowTo, ["HowToId"] },
        // PartyFinderNotification
        { LinkMacroPayloadType.Status, ["StatusId"] },
        { LinkMacroPayloadType.PartyFinder, ["ListingId", string.Empty, "WorldId"] },
        { LinkMacroPayloadType.AkatsukiNote, ["AkatsukiNoteId"] },
        { DalamudLinkType, ["CommandId", "Extra1", "Extra2", "ExtraString"] }
    };

    public void DrawUtf8String(nint address, NodeOptions nodeOptions)
    {
        if (address == 0)
        {
            ImGui.TextUnformatted("null");
            return;
        }

        nodeOptions = nodeOptions.WithAddress(address);

        var str = (Utf8String*)address;
        if (str->StringPtr == null)
        {
            ImGui.TextUnformatted("null");
            return;
        }

        DrawSeString(str->StringPtr, nodeOptions);
    }

    public void DrawSeString(byte* ptr, NodeOptions nodeOptions)
    {
        if (ptr == null)
        {
            ImGui.TextUnformatted("null");
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
            var text = rosss.ToString();

            using (ImRaii.PushColor(ImGuiCol.Text, (uint)ColorTreeNode, nodeOptions.RenderSeString))
                clicked = ImGui.Selectable(text + nodeOptions.GetKey("SeStringSelectable"));

            ImGuiContextMenu.Draw(nodeOptions.GetKey("SeStringSelectableContextMenu"), (builder) =>
            {
                builder.Add(new ImGuiContextMenuEntry()
                {
                    Label = TextService.Translate("ContextMenu.CopyText"),
                    ClickCallback = () => ImGui.SetClipboardText(text)
                });
            });
        }

        if (clicked)
        {
            var str = new ReadOnlySeString(rosss.Data.ToArray());
            var windowTitle = nodeOptions.Title ?? (nodeOptions.SeStringTitle ?? str).ToString();
            WindowManager.CreateOrOpen(windowTitle, () => new SeStringInspectorWindow(WindowManager, this, SeStringEvaluator, str, nodeOptions.Language, windowTitle));
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

            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 120);
            ImGui.TableSetupColumn("Tree", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(payload.Type == ReadOnlySePayloadType.Text ? "Text" : "ToString()");
            ImGui.TableNextColumn();
            var text = payload.ToString();
            DrawCopyableText($"\"{text}\"", text);

            if (payload.Type != ReadOnlySePayloadType.Macro)
                continue;

            if (payload.ExpressionCount > 0)
            {
                var exprIdx = 0;
                LinkMacroPayloadType? linkType = null;

                if (payload.MacroCode == MacroCode.Link && payload.TryGetExpression(out var linkExpr1) && linkExpr1.TryGetUInt(out var linkExpr1Val))
                    linkType = (LinkMacroPayloadType)linkExpr1Val;

                foreach (var expr in payload)
                {
                    DrawExpression(payload.MacroCode, linkType, exprIdx++, expr, payloadNodeOptions with
                    {
                        SeStringTitle = null,
                        DefaultOpen = true,
                        AddressPath = nodeOptions.AddressPath.With([payloadIdx, exprIdx]),
                    });
                }
            }
        }
    }

    private void DrawExpression(MacroCode macroCode, LinkMacroPayloadType? linkType, int idx, ReadOnlySeExpressionSpan expr, NodeOptions nodeOptions)
    {
        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        var expressionName = GetExpressionName(macroCode, linkType, idx, expr);
        ImGui.TextUnformatted($"[{idx}] " + (string.IsNullOrEmpty(expressionName) ? $"Expr {idx}" : expressionName));

        ImGui.TableNextColumn();

        if (expr.Body.IsEmpty)
        {
            ImGui.TextUnformatted("(?)");
            return;
        }

        if (expr.TryGetUInt(out var u32))
        {
            if (macroCode is MacroCode.Icon or MacroCode.Icon2 && idx == 0)
            {
                TextureService.DrawGfd(u32, ImGui.GetTextLineHeight());
                ImGui.SameLine();
            }

            DrawCopyableText(u32.ToString());
            ImGui.SameLine();
            DrawCopyableText($"0x{u32:X}");

            if (macroCode == MacroCode.Link && idx == 0)
            {
                var name = linkType == DalamudLinkType ? "Dalamud" : Enum.GetName((LinkMacroPayloadType)u32);
                if (!string.IsNullOrEmpty(name))
                {
                    ImGui.SameLine();
                    ImGui.TextUnformatted(name);
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
                ImGui.TextUnformatted(Enum.GetName(articleTypeEnumType, u32));
            }

            // TODO: clickable link to open row in new window :O

            return;
        }

        if (expr.TryGetString(out var s))
        {
            DrawSeString(s, false, nodeOptions with { DefaultOpen = true });
            // ImGui.TextUnformatted($"\"{s.ToString().Replace("\\", "\\\\").Replace("\"", "\\\"")}\"");
            return;
        }

        if (expr.TryGetPlaceholderExpression(out var exprType))
        {
            if (((ExpressionType)exprType).GetNativeName() is { } nativeName)
            {
                ImGui.TextUnformatted(nativeName);
                return;
            }

            ImGui.TextUnformatted($"?x{exprType:X02}");
            return;
        }

        if (expr.TryGetParameterExpression(out exprType, out var e1))
        {
            if (((ExpressionType)exprType).GetNativeName() is { } nativeName)
            {
                ImGui.TextUnformatted($"{nativeName}({e1.ToString()})");
                return;
            }

            throw new InvalidOperationException("All native names must be defined for unary expressions.");
        }

        if (expr.TryGetBinaryExpression(out exprType, out e1, out var e2))
        {
            if (((ExpressionType)exprType).GetNativeName() is { } nativeName)
            {
                ImGui.TextUnformatted($"{e1.ToString()} {nativeName} {e2.ToString()}");
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
        ImGui.TextUnformatted(sb.ToString());
    }

    private string GetExpressionName(MacroCode macroCode, LinkMacroPayloadType? linkType, int idx, ReadOnlySeExpressionSpan expr)
    {
        if (ExpressionNames.TryGetValue(macroCode, out var names) && idx < names.Length)
            return names[idx];

        if (macroCode == MacroCode.Switch)
            return $"Case {idx - 1}";

        if (macroCode == MacroCode.Link && linkType != null && LinkExpressionNames.TryGetValue((LinkMacroPayloadType)linkType, out var linkNames) && idx - 1 < linkNames.Length)
            return linkNames[idx - 1];

        if (macroCode == MacroCode.Link && idx == 4)
            return "Copy String";

        return string.Empty;
    }
}
