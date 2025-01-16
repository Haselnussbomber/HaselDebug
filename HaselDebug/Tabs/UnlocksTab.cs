using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Gui.Yoga;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Windows;
using ImGuiNET;
using YogaSharp;
using static HaselCommon.Globals.ColorHelpers;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append)]
public class UnlocksTab : DebugTab, IDisposable
{
    private readonly UnlocksTabSummaryWrapper _summaryNode;

    public override bool IsPinnable => false;
    public override bool CanPopOut => false;

    public UnlocksTab(IEnumerable<IUnlockTab> subTabs)
    {
        var unlockTabs = subTabs.OrderBy(t => t.Title).ToArray();
        _summaryNode = new UnlocksTabSummaryWrapper(unlockTabs);
        SubTabs = unlockTabs.Cast<ISubTab<UnlocksTab>>().ToImmutableArray();
    }

    public void Dispose()
    {
        _summaryNode.Dispose();
        GC.SuppressFinalize(this);
    }

    public override void Draw()
    {
        _summaryNode.CalculateLayout(ImGui.GetContentRegionAvail());
        _summaryNode.Update();
        _summaryNode.Draw();
    }
}

public class UnlocksTabSummaryWrapper : Node
{
    public UnlocksTabSummaryWrapper(IUnlockTab[] unlockTabs)
    {
        FlexGrow = 1;
        JustifyContent = YGJustify.Center;
        AlignItems = YGAlign.Center;

        Add(new UnlocksTabSummary(unlockTabs));
        Add(new TextNode() { MarginTop = 20, Text = "Note: This is completely inaccurate and totally useless. :)" });
    }

    public override void ApplyLayout()
    {
        JustifyContent = ComputedWidth < 320
            ? YGJustify.FlexStart
            : YGJustify.Center;
    }
}

public class UnlocksTabSummary : Node
{
    public UnlocksTabSummary(IUnlockTab[] unlockTabs)
    {
        // flex box? in my imgui? kinda. though i'm struggling with it. but maybe i'm just using it wrong.
        
        MarginTop = 20;
        MaxWidth = 650;
        FlexWrap = YGWrap.Wrap;
        FlexDirection = YGFlexDirection.Row;
        JustifyContent = YGJustify.Center;
        AlignContent = YGAlign.Center;
        Gap = 5;

        foreach (var tab in unlockTabs)
        {
            Add(new UnlocksTabCard(tab));
        }
    }
}

public class UnlocksTabCard : Node
{
    private readonly IUnlockTab _tab;

    public UnlocksTabCard(IUnlockTab tab)
    {
        _tab = tab;

        Width = 200;
        Height = 52;
    }

    public override unsafe void DrawContent()
    {
        using var color = ImRaii
            .PushColor(ImGuiCol.Button, (uint)hsla(0, 0, 0.25f, 0.9f))
            .Push(ImGuiCol.ButtonHovered, (uint)hsla(0, 0, 0.33f, 0.9f))
            .Push(ImGuiCol.ButtonActive, (uint)hsla(0, 0, 0.5f, 0.9f));

        if (ImGui.Button($"##{_tab.InternalName}Button", ComputedSize))
        {
            Service.Get<PluginWindow>().SelectTab(_tab.InternalName);
        }

        var style = ImGui.GetStyle();
        var innerStartPos = ImGui.GetWindowPos() + style.FramePadding * 2f;

        // label
        ImGui.GetWindowDrawList().AddText(
            innerStartPos,
            0xDDFFFFFF,
            _tab.Title);

        if (!AgentLobby.Instance()->IsLoggedIn)
            return;

        var progress = _tab.GetUnlockProgress();
        var canShowProgress = !progress.NeedsExtraData || (progress.NeedsExtraData && progress.HasExtraData);
        var percentage = progress.NumUnlocked / (float)progress.TotalUnlocks;

        var buttonSize = ImGui.GetItemRectSize();
        var progressBarOffset = new Vector2(0, ImGui.GetTextLineHeightWithSpacing() + style.ItemInnerSpacing.Y / 2f);
        var progressBarMin = innerStartPos + progressBarOffset;
        var progressBarMax = progressBarMin + (buttonSize - style.FramePadding * 4f) - progressBarOffset;

        // percentage text right
        if (canShowProgress)
        {
            var text = $"{progress.NumUnlocked / (float)progress.TotalUnlocks * 100f:0.00}%";
            ImGui.GetWindowDrawList().AddText(
                innerStartPos + new Vector2(progressBarMax.X - progressBarMin.X - ImGui.CalcTextSize(text).X, 0),
                0xDDFFFFFF,
                text);

            // progress bar background
            ImGui.GetWindowDrawList().AddRectFilled(
            progressBarMin,
            progressBarMax,
            (uint)hsla(0, 0, 0.4f, 0.9f));

            // progress bar
            ImGui.GetWindowDrawList().AddRectFilled(
            progressBarMin,
            progressBarMax - new Vector2((progressBarMax.X - progressBarMin.X) * (1 - percentage), 0),
            (uint)hsla(40, 10, 0.15f, 0.9f));

            // progress bar text
            text = $"{progress.NumUnlocked} / {progress.TotalUnlocks}";
            ImGui.GetWindowDrawList().AddText(
                progressBarMin + new Vector2((progressBarMax.X - progressBarMin.X) * 0.5f - ImGui.CalcTextSize(text).X / 2f + style.ItemInnerSpacing.X - 1, -1), // idk
                0xFFFFFFFF,
                text);
        }
        else
        {
            ImGui.GetWindowDrawList().AddText(
                innerStartPos + new Vector2(0, ImGui.GetTextLineHeightWithSpacing()),
                0xDDFFFFFF,
                "Missing data");
        }
    }
}
