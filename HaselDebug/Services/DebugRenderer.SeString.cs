using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Dalamud.Interface.ImGuiSeStringRenderer;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.System.String;
using HaselCommon.Extensions;
using HaselCommon.Services;
using HaselDebug.Utils;
using HaselDebug.Windows;
using ImGuiNET;
using Lumina.Text.Expressions;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

namespace HaselDebug.Services;

#pragma warning disable SeStringRenderer
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
        { MacroCode.JaNoun, ["SheetName", "Person", "RowId", "Amount", "Case", "UnkInt5"] },
        { MacroCode.EnNoun, ["SheetName", "Person", "RowId", "Amount", "Case", "UnkInt5"] },
        { MacroCode.DeNoun, ["SheetName", "Person", "RowId", "Amount", "Case", "UnkInt5"] },
        { MacroCode.FrNoun, ["SheetName", "Person", "RowId", "Amount", "Case", "UnkInt5"] },
        { MacroCode.ChNoun, ["SheetName", "Person", "RowId", "Amount", "Case", "UnkInt5"] },
        { MacroCode.LowerHead, ["String"] },
        { MacroCode.ColorType, ["ColorType"] },
        { MacroCode.EdgeColorType, ["ColorType"] },
        { MacroCode.Digit, ["Value", "TargetLength"] },
        { MacroCode.Ordinal, ["Value"] },
        { MacroCode.Sound, ["IsJingle", "SoundId"] },
        // { MacroCode.LevelPos, [] },
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
        DrawSeStringSelectable(new ReadOnlySeStringSpan(ptr), nodeOptions);
    }

    public void DrawSeStringSelectable(ReadOnlySeStringSpan rosss, NodeOptions nodeOptions)
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

            ImGuiContextMenuService.Draw(nodeOptions.GetKey("SeStringSelectableContextMenu"), (builder) =>
            {
                builder.Add(new ImGuiContextMenuEntry()
                {
                    Label = "Copy text",
                    ClickCallback = () => ImGui.SetClipboardText(text)
                });
            });
        }

        if (clicked)
        {
            var str = new ReadOnlySeString(rosss.Data.ToArray());
            WindowManager.CreateOrOpen(
                (nodeOptions.Title ?? str).ToString(),
                (wm, windowName) => new SeStringInspectorWindow(wm, this, SeStringEvaluator, str, windowName));
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

        using var node = asTreeNode ? DrawTreeNode(nodeOptions.WithTitle(rosss.ToReadOnlySeString())) : null;
        if (asTreeNode && !node!) return;

        if (!asTreeNode && nodeOptions.RenderSeString)
        {
            ImGuiHelpers.SeStringWrapped(rosss, new()
            {
                ForceEdgeColor = true,
            });
        }

        nodeOptions = nodeOptions with
        {
            DrawSeStringTreeNode = false,
            DefaultOpen = true
        };

        var payloadIdx = -1;
        foreach (var payload in rosss)
        {
            payloadIdx++;

            var preview = payload.Type.ToString();
            if (payload.Type == ReadOnlySePayloadType.Macro)
                preview += $": {payload.MacroCode}";

            var payloadNodeOptions = nodeOptions
                .WithAddress(payloadIdx)
                .WithTitle($"[{payloadIdx}] {preview}");

            using var payloadNode = DrawTreeNode(payloadNodeOptions);

            // consume option
            if (payloadNodeOptions.DrawSeStringTreeNode)
                payloadNodeOptions.DrawSeStringTreeNode = false;

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

                foreach (var expr in payload)
                {
                    DrawExpression(payload.MacroCode, exprIdx++, expr, payloadNodeOptions with
                    {
                        Title = null,
                        DefaultOpen = true,
                        AddressPath = nodeOptions.AddressPath.With([payloadIdx, exprIdx]),
                    });
                }
            }

            /*
            switch (payload.MacroCode)
            {
                case MacroCode.String:
                    //DrawExpressionRow("Parameter", parameterPayload.Parameter, localParameters);
                    break;

                // ---

                case LinkPayload linkPayload:

                    if (linkPayload.Type is IntegerExpression integerType)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted("Type");
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted($"0x{(byte)integerType.Value:X02}");
                        ImGui.SameLine();
                        var linkType = (LinkType)integerType.Value;
                        ImGui.TextUnformatted($"{linkType}");

                        DrawExpressionRow("Arg2", linkPayload.Arg2, localParameters);
                        DrawExpressionRow("Arg3", linkPayload.Arg3, localParameters);
                        DrawExpressionRow("Arg4", linkPayload.Arg4, localParameters);
                        DrawExpressionRow("Arg5", linkPayload.Arg5, localParameters);

                        switch (linkPayload)
                        {
                            case PlayerLinkPayload playerLinkPayload:
                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("Flags");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{playerLinkPayload.Flags}");

                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("WorldId");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{playerLinkPayload.WorldId} ({GetRow<World>(playerLinkPayload.WorldId)?.Name ?? "Unknown"})");

                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("PlayerName");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{playerLinkPayload.PlayerName}");
                                break;

                            case ItemLinkPayload itemLinkPayload:
                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("ItemId");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{itemLinkPayload.ItemId} ({GetRow<Item>(itemLinkPayload.ItemId)?.Name ?? ""})");
                                break;

                            case MapPositionLinkPayload mapPositionLinkPayload:
                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("TerritoryTypeId");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{mapPositionLinkPayload.TerritoryTypeId} ({GetRow<TerritoryType>(mapPositionLinkPayload.TerritoryTypeId)?.PlaceName.Value?.Name ?? ""})");

                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("MapId");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{mapPositionLinkPayload.MapId}");

                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("X");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{mapPositionLinkPayload.X} ({mapPositionLinkPayload.MapPosX:0.0})");

                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("Y");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{mapPositionLinkPayload.Y} ({mapPositionLinkPayload.MapPosY:0.0})");
                                break;

                            case QuestLinkPayload questLinkPayload:
                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("QuestId");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{questLinkPayload.QuestId} ({GetRow<Quest>(questLinkPayload.QuestId)?.Name ?? ""})");
                                break;

                            case AchievementLinkPayload achievementLinkPayload:
                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("AchievementId");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{achievementLinkPayload.AchievementId} ({GetRow<Achievement>(achievementLinkPayload.AchievementId)?.Name ?? ""})");
                                break;

                            case HowToLinkPayload howToLinkPayload:
                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("HowToId");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{howToLinkPayload.HowToId} ({GetRow<HowTo>(howToLinkPayload.HowToId)?.Name ?? ""})");
                                break;

                            case StatusLinkPayload statusLinkPayload:
                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("StatusId");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{statusLinkPayload.StatusId} ({GetRow<Status>(statusLinkPayload.StatusId)?.Name ?? ""})");
                                break;

                            case PartyFinderLinkPayload partyFinderLinkPayload:
                                DrawExpressionRow("ListingId", linkPayload.Arg2, localParameters);

                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("Flags");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{partyFinderLinkPayload.Flags} ({(byte)partyFinderLinkPayload.Flags:X02})");
                                break;

                            case AkatsukiNoteLinkPayload akatsukiNoteLinkPayload:
                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("AkatsukiNoteId");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{akatsukiNoteLinkPayload.AkatsukiNoteId} ({GetRow<AkatsukiNoteString>((uint)GetRow<AkatsukiNote>(akatsukiNoteLinkPayload.AkatsukiNoteId, 0)!.Unknown5)!.Unknown0 ?? ""})");
                                break;

                            case DalamudLinkPayload dalamudLinkPayload:
                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("PluginName");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{dalamudLinkPayload.PluginName}");

                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted("CommandId");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{dalamudLinkPayload.CommandId}");
                                break;

                            case PartyFinderNotificationLinkPayload:
                            case LinkTerminatorPayload:
                                break;
                        }
                    }
                    else
                    {
                        DrawExpressionRow("Type", linkPayload.Type, localParameters);
                        DrawExpressionRow("Arg2", linkPayload.Arg2, localParameters);
                        DrawExpressionRow("Arg3", linkPayload.Arg3, localParameters);
                        DrawExpressionRow("Arg4", linkPayload.Arg4, localParameters);
                        DrawExpressionRow("Arg5", linkPayload.Arg5, localParameters);
                    }
                    break;

                case RawPayload:
                case CharacterPayload:
                    // ignored
                    break;

                default:
                    var props = payload.GetType()
                        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .Where(propInfo => propInfo.PropertyType.IsAssignableTo(typeof(Expression)));

                    foreach (var propInfo in props)
                    {
                        DrawExpressionRow(propInfo.Name, (Expression?)propInfo.GetValue(payload), localParameters);
                    }

                    // ImGui.TextUnformatted($"Unhandled Payload: {payload.GetType().Name}");
                    break;
            }

            var encoded = payload.Encode();
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted("Data [");
            ImGui.SameLine(0, 0);
            ImGui.TextUnformatted(ImGui.IsKeyDown(ImGuiKey.LeftShift) ? $"0x{encoded.Length:X02}" : $"{encoded.Length}");
            ImGui.SameLine(0, 0);
            ImGui.TextUnformatted("]");
            ImGui.TableNextColumn();
            DrawCopyableText(BitConverter.ToString(encoded).Replace("-", " "));
            */
        }
    }

    private void DrawExpression(MacroCode macroCode, int idx, ReadOnlySeExpressionSpan expr, NodeOptions nodeOptions)
    {
        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"[{idx}] " + GetExpressionName(macroCode, idx));

        ImGui.TableNextColumn();

        if (expr.Body.IsEmpty)
        {
            ImGui.TextUnformatted("(?)");
            return;
        }

        if (expr.TryGetUInt(out var u32))
        {
            ImGui.TextUnformatted(u32.ToString());
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

    private string GetExpressionName(MacroCode macroCode, int idx)
    {
        if (ExpressionNames.TryGetValue(macroCode, out var names) && idx < names.Length)
            return names[idx];

        if (macroCode == MacroCode.Switch)
            return $"Case {idx - 1}";

        return string.Empty;
    }
}
