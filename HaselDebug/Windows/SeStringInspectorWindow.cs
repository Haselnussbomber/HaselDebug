using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game;
using Dalamud.Interface.ImGuiSeStringRenderer;
using Dalamud.Interface.Utility;
using HaselCommon.Graphics;
using HaselCommon.Gui;
using HaselCommon.Services;
using HaselCommon.Services.SeStringEvaluation;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Text.Expressions;
using Lumina.Text.ReadOnly;

namespace HaselDebug.Windows;

public class SeStringInspectorWindow : SimpleWindow
{
    private SeStringParameter[]? LocalParameters = null;
    private ReadOnlySeString _string;
    private readonly DebugRenderer _debugRenderer;
    private readonly SeStringEvaluatorService _seStringEvaluator;

    public ReadOnlySeString String
    {
        get => _string;
        set
        {
            _string = value;
            LocalParameters = null;
        }
    }

    public ClientLanguage Language { get; set; }

    public SeStringInspectorWindow(
        WindowManager windowManager,
        DebugRenderer debugRenderer,
        SeStringEvaluatorService seStringEvaluator,
        ReadOnlySeString str,
        ClientLanguage language,
        string windowName = "SeString") : base(windowManager, windowName)
    {
        _debugRenderer = debugRenderer;
        _seStringEvaluator = seStringEvaluator;
        Language = language;
        String = str;
    }

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

    public override bool DrawConditions()
    {
        return true;
    }

    public override void Draw()
    {
        LocalParameters ??= GetLocalParameters(String.AsSpan(), []);

        DrawPreview();

        if (LocalParameters.Length != 0)
        {
            ImGui.Spacing();
            DrawParameters();
        }

        ImGui.Spacing();
        DrawPayloads();
    }

    private void DrawPreview()
    {
        using var node = _debugRenderer.DrawTreeNode(new NodeOptions() { AddressPath = new(1), Title = "Preview", TitleColor = Color.Green, DefaultOpen = true });
        if (!node) return;

        var evaluated = _seStringEvaluator.Evaluate(String.AsSpan(), new()
        {
            LocalParameters = LocalParameters ?? [],
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
        using var node = _debugRenderer.DrawTreeNode(new NodeOptions() { AddressPath = new(2), Title = "Parameters", TitleColor = Color.Green, DefaultOpen = true });
        if (!node) return;

        for (var i = 0; i < LocalParameters!.Length; i++)
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
        }
    }

    private void DrawPayloads()
    {
        using var node = _debugRenderer.DrawTreeNode(new NodeOptions() { AddressPath = new(3), Title = "Payloads", TitleColor = Color.Green, DefaultOpen = true });
        if (!node) return;

        _debugRenderer.DrawSeString(String.AsSpan(), false, new NodeOptions()
        {
            RenderSeString = false,
            DefaultOpen = true
        });
    }

    private static SeStringParameter[] GetLocalParameters(ReadOnlySeStringSpan rosss, Dictionary<uint, SeStringParameter>? parameters)
    {
        parameters ??= [];

        void ProcessString(ReadOnlySeStringSpan rosss)
        {
            foreach (var payload in rosss)
            {
                foreach (var expression in payload)
                {
                    ProcessExpression(expression);
                }
            }
        }

        void ProcessExpression(ReadOnlySeExpressionSpan expression)
        {
            if (expression.TryGetString(out var exprString))
            {
                ProcessString(exprString);
                return;
            }

            if (expression.TryGetBinaryExpression(out var expressionType, out var operand1, out var operand2))
            {
                ProcessExpression(operand1);
                ProcessExpression(operand2);
                return;
            }

            if (expression.TryGetParameterExpression(out expressionType, out var operand))
            {
                if (!operand.TryGetUInt(out var index))
                    return;

                if (parameters.ContainsKey(index))
                    return;

                if (expressionType == (int)ExpressionType.LocalNumber)
                {
                    parameters[index] = new SeStringParameter(0);
                    return;
                }
                else if (expressionType == (int)ExpressionType.LocalString)
                {
                    parameters[index] = new SeStringParameter("");
                    return;
                }
            }
        }

        ProcessString(rosss);

        if (parameters.Count > 0)
        {
            var last = parameters.OrderBy(x => x.Key).Last();

            if (parameters.Count != last.Key)
            {
                // fill missing local parameter slots, so we can go off the array index in SeStringContext

                for (var i = 1u; i <= last.Key; i++)
                {
                    if (!parameters.ContainsKey(i))
                        parameters[i] = new SeStringParameter(0);
                }
            }
        }

        return parameters.OrderBy(x => x.Key).Select(x => x.Value).ToArray();
    }
}
