using Dalamud.Game.Chat;
using Dalamud.Game.Text.Evaluator;
using Dalamud.Interface.ImGuiSeStringRenderer;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.Text;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Windows;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class LogMessageLogTab : DebugTab, IDisposable
{
    private record LogMessageEntry(
        DateTime Time,
        uint LogMessageId,
        ReadOnlySeString Text,
        ReadOnlySeString Preview,
        ReadOnlySeString SourceEntityName,
        ReadOnlySeString TargetEntityName,
        bool IsSourceEntityPlayer,
        bool IsTargetEntityPlayer,
        SeStringParameter[] Parameters);

    private readonly IChatGui _chatGui;
    private readonly IClientState _clientState;
    private readonly ISeStringEvaluator _seStringEvaluator;
    private readonly ExcelService _excelService;
    private readonly DebugRenderer _debugRenderer;
    private readonly WindowManager _windowManager;
    private readonly TextService _textService;
    private readonly IServiceProvider _serviceProvider;

    private readonly List<LogMessageEntry> _logMessages = [];
    private bool _logEnabled;

    public override string Title => "LogMessage Log";

    public void Dispose()
    {
        _chatGui.LogMessage -= OnLogMessage;
    }

    private void OnLogMessage(ILogMessage message)
    {
        var ptr = (LogMessageQueueItem*)message.Address;
        var parameterCount = ptr->Parameters.Count;
        var parameters = new SeStringParameter[parameterCount];

        for (var i = 0; i < parameterCount; i++)
        {
            ref var param = ref ptr->Parameters[i];

            parameters[i] = param.Type switch
            {
                TextParameterType.Integer => new SeStringParameter((uint)param.IntValue),
                TextParameterType.String => new SeStringParameter(param.StringValue.AsSpan()),
                TextParameterType.ReferencedUtf8String => new SeStringParameter(param.ReferencedUtf8StringValue->Utf8String.AsSpan()),
                _ => new SeStringParameter(),
            };
        }

        var text = _excelService.TryGetRow<LogMessage>(message.LogMessageId, out var row) ? row.Text : new ReadOnlySeString();

        _logMessages.Add(new LogMessageEntry(
            DateTime.Now,
            message.LogMessageId,
            text,
            _seStringEvaluator.Evaluate(text, parameters, _clientState.ClientLanguage),
            message.SourceEntity?.Name ?? new(),
            message.TargetEntity?.Name ?? new(),
            message.SourceEntity?.IsPlayer ?? false,
            message.TargetEntity?.IsPlayer ?? false,
            parameters));
    }

    public override void Draw()
    {
        if (ImGui.Checkbox("Enable", ref _logEnabled))
        {
            if (_logEnabled)
                _chatGui.LogMessage += OnLogMessage;
            else
                _chatGui.LogMessage -= OnLogMessage;
        }

        ImGui.SameLine();

        if (ImGui.Button("Clear"))
        {
            _logMessages.Clear();
        }

        using var table = ImRaii.Table("LogMessageTable3"u8, 6, ImGuiTableFlags.Resizable | ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table)
            return;

        ImGui.TableSetupColumn("Time"u8, ImGuiTableColumnFlags.WidthFixed, 80);
        ImGui.TableSetupColumn("LogMessageId"u8, ImGuiTableColumnFlags.WidthFixed, 80);
        ImGui.TableSetupColumn("Preview"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("ParameterCount"u8, ImGuiTableColumnFlags.WidthFixed, 70);
        ImGui.TableSetupColumn("SourceEntity"u8, ImGuiTableColumnFlags.WidthFixed, 150);
        ImGui.TableSetupColumn("TargetEntity"u8, ImGuiTableColumnFlags.WidthFixed, 150);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        for (var i = _logMessages.Count - 1; i >= 0; i--)
        {
            var message = _logMessages[i];
            using var id = ImRaii.PushId(i);

            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // Time
            ImGui.Text(message.Time.ToLongTimeString());

            ImGui.TableNextColumn(); // LogMessageId
            ImGuiUtils.DrawCopyableText($"{message.LogMessageId}");

            ImGui.TableNextColumn(); // Preview

            var clicked = ImGui.Selectable("##SeStringSelectable");
            ImGui.SameLine(0, 0);
            ImGuiHelpers.SeStringWrapped(message.Preview, new()
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

            if (clicked)
            {
                var windowTitle = $"Idx {i} | LogMessage {message.LogMessageId}";
                _windowManager.CreateOrOpen(windowTitle, () => new SeStringInspectorWindow(_windowManager, _textService, _serviceProvider)
                {
                    String = message.Text,
                    Language = _clientState.ClientLanguage,
                    WindowName = windowTitle,
                    LocalParameters = message.Parameters,
                });
            }

            ImGui.TableNextColumn(); // ParameterCount
            ImGui.Text(message.Parameters.Length.ToString());

            ImGui.TableNextColumn(); // SourceEntity
            ImGuiUtils.DrawCopyableText($"{(message.IsSourceEntityPlayer == true ? "Self" : message.SourceEntityName)}");

            ImGui.TableNextColumn(); // TargetEntity
            ImGuiUtils.DrawCopyableText($"{(message.IsTargetEntityPlayer == true ? "Self" : message.TargetEntityName)}");
        }
    }
}
