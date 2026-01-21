using Dalamud.Game.Text.Evaluator;
using Dalamud.Interface.ImGuiSeStringRenderer;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselDebug.Extensions;
using HaselDebug.Services;
using HaselDebug.Utils;
using Lumina.Text.Parse;

namespace HaselDebug.Windows;

[AutoConstruct]
public unsafe partial class SeStringInspectorWindow : SimpleWindow
{
    private readonly IServiceProvider _serviceProvider;

    private WindowManager _windowManager;
    private DebugRenderer _debugRenderer;
    private ISeStringEvaluator _seStringEvaluator;

    private SeStringParameter[]? _localParameters = null;
    private string _macroString = string.Empty;
    private ReadOnlySeString _string;
    private Utf8String* _utf8string;

    public ReadOnlySeString String
    {
        get => _string;
        set
        {
            _string = value;
            _localParameters = null;
        }
    }

    public ReadOnlySeStringSpan StringSpan => IsValidUtf8String ? Utf8String->AsSpan() : String.AsSpan();

    public ClientLanguage Language { get; set; }

    public AtkResNode* Node { get; set; }
    public Utf8String* Utf8String
    {
        get => _utf8string;
        set
        {
            _utf8string = value;
            _macroString = value != null ? new ReadOnlySeStringSpan(value->AsSpan()).ToString() : string.Empty;
        }
    }

    public bool IsValidUtf8String
        => Node != null &&
            Utf8String != null &&
            RaptureAtkUnitManager.Instance()->AtkUnitManager.GetAddonByNodeSafe(Node) != null;

    [AutoPostConstruct]
    private void Initialize()
    {
        _windowManager = _serviceProvider.GetRequiredService<WindowManager>();
        _debugRenderer = _serviceProvider.GetRequiredService<DebugRenderer>();
        _seStringEvaluator = _serviceProvider.GetRequiredService<ISeStringEvaluator>();
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
        if (IsValidUtf8String)
        {
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputText("MacroString", ref _macroString, 1024, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                using var rssb = new RentedSeStringBuilder();

                rssb.Builder.Append(ReadOnlySeString.FromMacroString(_macroString, new MacroStringParseOptions { ExceptionMode = MacroStringParseExceptionMode.Ignore }));

                if (!rssb.Builder.ToReadOnlySeString().IsEmpty)
                    Utf8String->SetString(rssb.Builder.GetViewAsSpan());
                else
                    Utf8String->Clear();
            }
        }

        _localParameters ??= GetLocalParameters(StringSpan, []);

        var evaluated = _seStringEvaluator.Evaluate(StringSpan, _localParameters, Language);

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
        if (ImGui.IsKeyDown(ImGuiKey.LeftShift))
        {
            ImGuiUtils.DrawCopyableText(evaluated.ToMacroString());
        }
        else
        {
            ImGuiHelpers.SeStringWrapped(evaluated, new SeStringDrawParams()
            {
                ForceEdgeColor = true,
            });

            if (ImGui.IsItemClicked())
                ImGui.SetClipboardText(evaluated.ToString());
        }

        if (ImGui.IsItemHovered())
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
    }

    private void DrawParameters()
    {
        using var node = _debugRenderer.DrawTreeNode(new NodeOptions() { AddressPath = new(2), Title = "Parameters", TitleColor = Color.Green, DefaultOpen = true });
        if (!node) return;

        for (var i = 0; i < _localParameters!.Length; i++)
        {
            if (_localParameters[i].IsString)
            {
                var str = _localParameters[i].StringValue.ToString();
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
            _debugRenderer.DrawSeString(StringSpan, false, new NodeOptions()
            {
                RenderSeString = false,
                DefaultOpen = true
            });
        }
        node.Dispose();

        if (IsValidUtf8String || String.Equals(evaluated))
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
