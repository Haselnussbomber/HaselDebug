using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game;
using Dalamud.Interface.ImGuiSeStringRenderer;
using Dalamud.Interface.Utility;
using HaselCommon.Graphics;
using HaselCommon.Gui;
using HaselCommon.Services;
using HaselCommon.Services.Evaluator;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Text.Expressions;
using Lumina.Text.ReadOnly;

namespace HaselDebug.Windows;

public class SeStringInspectorWindow : SimpleWindow
{
    private readonly WindowManager _windowManager;
    private readonly DebugRenderer _debugRenderer;
    private readonly SeStringEvaluator _seStringEvaluator;

    private SeStringParameter[]? _localParameters = null;
    private ReadOnlySeString _string;

    public ReadOnlySeString String
    {
        get => _string;
        set
        {
            _string = value;
            _localParameters = null;
        }
    }

    public ClientLanguage Language { get; set; }

    public SeStringInspectorWindow(
        WindowManager windowManager,
        TextService textService,
        LanguageProvider languageProvider,
        DebugRenderer debugRenderer,
        SeStringEvaluator seStringEvaluator,
        ReadOnlySeString str,
        ClientLanguage language,
        string windowName = "SeString") : base(windowManager, textService, languageProvider)
    {
        _windowManager = windowManager;
        _debugRenderer = debugRenderer;
        _seStringEvaluator = seStringEvaluator;
        Language = language;
        String = str;
        WindowName = $"{windowName.Replace("\n", "")}##{GetType().Name}";
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
            _windowManager.Close<SeStringInspectorWindow>();
    }

    public override bool DrawConditions()
    {
        return true;
    }

    public override void Draw()
    {
        _localParameters ??= GetLocalParameters(String.AsSpan(), []);

        var evaluated = _seStringEvaluator.Evaluate(String.AsSpan(), _localParameters, Language);

        DrawPreview(evaluated);

        if (_localParameters!.Length != 0)
        {
            ImGui.Spacing();
            DrawParameters();
        }

        ImGui.Spacing();
        DrawPayloads(evaluated);
    }

    private void DrawPreview(ReadOnlySeString evaluated)
    {
        using var node = _debugRenderer.DrawTreeNode(new NodeOptions() { AddressPath = new(1), Title = "Preview", TitleColor = Color.Green, DefaultOpen = true });
        if (!node) return;

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

        for (var i = 0; i < _localParameters!.Length; i++)
        {
            if (_localParameters[i].IsString)
            {
                var str = _localParameters[i].StringValue.ExtractText();
                if (ImGui.InputText($"lstr({i + 1})", ref str, 255))
                {
                    _localParameters[i] = new(str);
                }
            }
            else
            {
                var num = (int)_localParameters[i].UIntValue;
                if (ImGui.InputInt($"lnum({i + 1})", ref num))
                {
                    _localParameters[i] = new((uint)num);
                }
            }
        }
    }

    private void DrawPayloads(ReadOnlySeString evaluated)
    {
        using var node = _debugRenderer.DrawTreeNode(new NodeOptions() { AddressPath = new(3), Title = "Payloads", TitleColor = Color.Green, DefaultOpen = true });
        if (node)
        {
            _debugRenderer.DrawSeString(String.AsSpan(), false, new NodeOptions()
            {
                RenderSeString = false,
                DefaultOpen = true
            });
        }
        node.Dispose();

        if (String.Equals(evaluated))
            return;

        using var node2 = _debugRenderer.DrawTreeNode(new NodeOptions() { AddressPath = new(4), Title = "Payloads (Evaluated)", TitleColor = Color.Green, DefaultOpen = true });
        if (!node2) return;

        _debugRenderer.DrawSeString(evaluated.AsSpan(), false, new NodeOptions()
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
