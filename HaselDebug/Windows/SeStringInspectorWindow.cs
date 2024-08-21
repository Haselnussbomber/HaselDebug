using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiSeStringRenderer;
using Dalamud.Interface.Utility;
using HaselCommon.Services;
using HaselCommon.Services.SeStringEvaluation;
using HaselCommon.Utils;
using HaselCommon.Windowing;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Text.ReadOnly;

namespace HaselDebug.Windows;

#pragma warning disable SeStringRenderer
public class SeStringInspectorWindow(
    WindowManager windowManager,
    DebugRenderer DebugRenderer,
    SeStringEvaluatorService SeStringEvaluator,
    ReadOnlySeString SeString,
    ClientLanguage Language,
    string windowName = "SeString") : SimpleWindow(windowManager, windowName)
{
    private readonly List<SeStringParameter> LocalParameters = [];

    public override void OnOpen()
    {
        base.OnOpen();

        Size = new Vector2(800, 600);
        SizeConstraints = new()
        {
            MinimumSize = new Vector2(250, 250),
            MaximumSize = new Vector2(4096, 2160)
        };

        SizeCondition = ImGuiCond.Appearing;

        Flags |= ImGuiWindowFlags.NoSavedSettings;

        RespectCloseHotkey = true;
        DisableWindowSounds = true;
    }

    public override void OnClose()
    {
        base.OnClose();

        if (ImGui.IsKeyDown(ImGuiKey.LeftShift))
            WindowManager.Close<SeStringInspectorWindow>();
    }

    public override void Draw()
    {
        DrawPreview();
        ImGui.Spacing();
        DrawParameters();
        ImGui.Spacing();
        DrawPayloads();
    }

    private void DrawPreview()
    {
        using var node = DebugRenderer.DrawTreeNode(new NodeOptions() { Title = "Preview", TitleColor = Colors.Green, DefaultOpen = true });
        if (!node) return;

        var evaluated = SeStringEvaluator.Evaluate(SeString.AsSpan(), new()
        {
            LocalParameters = LocalParameters.ToArray(),
            Language = Language
        });

        ImGui.Dummy(new Vector2(0, ImGui.GetTextLineHeight()));
        ImGui.SameLine(0, 0);
        ImGuiHelpers.SeStringWrapped(evaluated, new SeStringDrawParams()
        {
            ForceEdgeColor = true,
        });
    }

    private void DrawParameters()
    {
        using var node = DebugRenderer.DrawTreeNode(new NodeOptions() { Title = "Parameters", TitleColor = Colors.Green, DefaultOpen = true });
        if (!node) return;

        // TODO: nice table

        var elementToDelete = -1;

        for (var i = 0; i < LocalParameters.Count; i++)
        {
            if (LocalParameters[i].IsString)
            {
                var str = LocalParameters[i].StringValue.ExtractText();
                if (ImGui.InputText($"lstr({i + 1})", ref str, 255))
                {
                    LocalParameters[i] = new(str);
                }
            }
            else
            {
                var num = (int)LocalParameters[i].UIntValue;
                if (ImGui.InputInt($"lnum({i + 1})", ref num))
                {
                    LocalParameters[i] = new((uint)num);
                }
            }

            ImGui.SameLine();
            if (ImGuiUtils.IconButton($"DeleteLocalParam{i}", FontAwesomeIcon.Trash, "Delete"))
            {
                elementToDelete = i;
            }
        }

        if (elementToDelete != -1)
        {
            LocalParameters.RemoveAt(elementToDelete);
        }

        if (ImGui.Button("Add UInt"))
        {
            LocalParameters.Add(new(0));
        }
        ImGui.SameLine();
        if (ImGui.Button("Add String"))
        {
            LocalParameters.Add(new(""));
        }
    }

    private void DrawPayloads()
    {
        using var node = DebugRenderer.DrawTreeNode(new NodeOptions() { Title = "Payloads", TitleColor = Colors.Green, DefaultOpen = true });
        if (!node) return;

        DebugRenderer.DrawSeString(SeString.AsSpan(), false, new NodeOptions()
        {
            RenderSeString = false,
            DefaultOpen = true
        });
    }
}
