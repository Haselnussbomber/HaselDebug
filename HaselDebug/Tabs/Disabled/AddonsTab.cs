/*
using System.Text;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Utils;
using HaselDebug.Abstracts;
using HaselDebug.Utils;
using ImGuiNET;

namespace HaselDebug.Tabs;

public unsafe class AddonsTab : DebugTab
{
    public override void Draw()
    {
        ImGui.Text($"RaptureAtkModule.IsUiVisible: {RaptureAtkModule.Instance()->IsUiVisible}");

        using var tabs = ImRaii.TabBar("Addons");
        if (!tabs) return;

        var addonNames = RaptureAtkModule.Instance()->AddonNames;

        using (var tab = ImRaii.TabItem("Names"))
        {
            if (tab)
            {
                var addonNamesCount = addonNames.LongCount;
                ImGui.Text($"Num Addons: {addonNamesCount}");

                if (ImGui.Button("Copy py list"))
                {
                    var sb = new StringBuilder();
                    for (var i = 0u; i < addonNamesCount; i++)
                    {
                        sb.AppendLine($"\"{i}\": \"{addonNames[i]}\",");
                    }
                    ImGui.SetClipboardText(sb.ToString());
                }

                using var table = ImRaii.Table("AddonsTable", 3);
                if (!table) return;

                for (var i = 0u; i < addonNamesCount; i++)
                {
                    var name = addonNames[i].ToString();
                    /*
                    if (!name.Contains("config", StringComparison.InvariantCultureIgnoreCase))
                        continue;
                    * /
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    DebugUtils.DrawCopyableText($"{i}");

                    ImGui.TableNextColumn();
                    DebugUtils.DrawCopyableText(name);

                    ImGui.TableNextColumn();
                    if (ImGui.Button($"Open##OpenAddon{i}"))
                    {
                        RaptureAtkModule.Instance()->OpenAddon(i, 0, null, null, 0, 0, 0);
                    }
                }
            }
        }

        using (var tab = ImRaii.TabItem("Open Addons"))
        {
            if (tab)
            {
                using var table = ImRaii.Table("OpenAddonsTable", 6);
                if (!table) return;

                AtkUnitBase* hoveredAddon = null;

                ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, 50);
                ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn("DepthLayer", ImGuiTableColumnFlags.WidthFixed, 40);
                ImGui.TableSetupColumn("DrawOrderIndex", ImGuiTableColumnFlags.WidthFixed, 40);
                ImGui.TableSetupColumn("Name");
                ImGui.TableSetupColumn("Actions");
                ImGui.TableHeadersRow();

                var i = 0;
                var windowBounds = stackalloc Bounds[1];
                var values = stackalloc SimpleTweenValue[1];

                var entriesSpan = RaptureAtkUnitManager.Instance()->AtkUnitManager.AllLoadedUnitsList.Entries;
                //var backing = new Pointer<AtkUnitBase>[entriesSpan.Length];
                //var addons = entriesSpan.Where(ptr => ptr.Value != null).OrderBy(backing.AsSpan(), ptr => ptr.Value->DrawOrderIndex);
                foreach (var ptr in entriesSpan)
                {
                    if (ptr == null)
                        continue;

                    var addon = ptr.Value;

                    if (addon->UldManager.LoadedState != AtkLoadState.Loaded)
                        continue;

                    var name = addon->NameString;

                    //if (name is not ("Character" or "CharacterStatus" or "GearSetList" or "InventoryExpansion" or "InventoryGrid0E"))
                    //    continue;

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    DebugUtils.DrawCopyableText($"{i}");

                    ImGui.TableNextColumn();
                    DebugUtils.DrawCopyableText($"{(nint)addon:X}");

                    ImGui.TableNextColumn();
                    DebugUtils.DrawCopyableText($"{addon->DepthLayer}");

                    ImGui.TableNextColumn();
                    DebugUtils.DrawCopyableText($"{addon->DrawOrderIndex}");

                    ImGui.TableNextColumn();
                    ImGui.Text(name);
                    if (!Framework.Instance()->WindowInactive && ImGui.IsItemHovered())
                    {
                        addon->GetWindowBounds(windowBounds);
                        ImGui.SetTooltip(windowBounds[0].ToString());
                        hoveredAddon = addon;
                    }

                    ImGui.TableNextColumn();
                    if (ImGui.Button($"Focus##FocusAddon{i}"))
                    {
                        addon->Focus();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button($"FireCloseCallback##FireCloseCallback{i}"))
                    {
                        addon->FireCloseCallback();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button($"TweenTest##TweenTestAddon{i}"))
                    {
                        //haddon->RootNodeTween.Clear();
                        var currentAlpha = addon->RootNodeTween.GetNodeValue(SimpleTweenValueType.Alpha);
                        if (currentAlpha == 0f)
                            addon->RootNodeTween.Node->ToggleVisibility(true);

                        values[0].Type = SimpleTweenValueType.Alpha;
                        values[0].Value = currentAlpha == 1f ? 0f : 1f;
                        addon->RootNodeTween.Prepare(2000, addon->RootNode, values, 1);
                        //haddon->RootNodeTween.RegisterEvent((AtkEventType)64, 0, (AtkEventListener*)addon, null, false);
                        //haddon->RootNodeTween.RegisterEvent((AtkEventType)65, 0, (AtkEventListener*)addon, null, false);
                        addon->RootNodeTween.Execute();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button($"TweenClear##TweenClear{i}"))
                    {
                        addon->RootNodeTween.Clear();
                    }
                    i++;
                }

                if (hoveredAddon != null)
                {
                    // ((HAtkUnitBase*)hoveredAddon)->CalculateWindowBounds(windowBounds);

                    var pos = new Vector2(windowBounds[0].Pos1.X, windowBounds[0].Pos1.Y);
                    var pos2 = new Vector2(windowBounds[0].Pos2.X, windowBounds[0].Pos2.Y);
                    ImGui.SetNextWindowPos(pos);
                    ImGui.SetNextWindowSize(windowBounds[0].Size);

                    using var windowBorderSize = ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 1.0f);
                    using var borderColor = ImRaii.PushColor(ImGuiCol.Border, (uint)Colors.Gold);
                    using var windowBgColor = ImRaii.PushColor(ImGuiCol.WindowBg, new Vector4(0.847f, 0.733f, 0.49f, 0.33f));

                    if (ImGui.Begin("Windows Picker", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoFocusOnAppearing))
                    {
                        var drawList = ImGui.GetForegroundDrawList();
                        var textPos = pos + new Vector2(0, -ImGui.GetTextLineHeight());
                        var addonName = hoveredAddon->NameString;
                        drawList.AddText(textPos + Vector2.One, Colors.Black, addonName);
                        drawList.AddText(textPos, Colors.Gold, addonName);
                        drawList.AddCircleFilled(pos, 3f, 0xFFFFFF00); // cyan
                        drawList.AddCircleFilled(pos2, 3f, 0xFFFF00FF); // magenta
                        drawList.AddCircleFilled(windowBounds[0].Center, 3f, 0xFF00FFFF); // yellow
                        ImGui.End();
                    }
                }
            }
        }
    }
}
*/
