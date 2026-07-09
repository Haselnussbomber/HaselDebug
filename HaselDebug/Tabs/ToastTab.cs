using FFXIVClientStructs.FFXIV.Client.UI;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class ToastTab : DebugTab, IDisposable
{
    private readonly DebugRenderer _debugRenderer;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly ISeStringEvaluator _seStringEvaluator;

    private bool _enabled;
    private Hook<UIModule.Delegates.ShowWideText> _showWideTextHook;
    private Hook<UIModule.Delegates.ShowText> _showTextHook;
    private Hook<UIModule.Delegates.ShowPoisonText> _showPoisonTextHook;
    private Hook<UIModule.Delegates.ShowErrorText> _showErrorTextHook;
    private Hook<RaptureAtkModule.Delegates.ShowTextGimmickHint>? _showTextGimmickHintHook;

    private readonly List<Toast> _toasts = [];

    public void Dispose()
    {
        _showWideTextHook?.Dispose();
        _showTextHook?.Dispose();
        _showPoisonTextHook?.Dispose();
        _showErrorTextHook?.Dispose();
        _showTextGimmickHintHook?.Dispose();
    }

    public override void Draw()
    {
        _showWideTextHook ??= _gameInteropProvider.HookFromAddress<UIModule.Delegates.ShowWideText>((nint)UIModule.StaticVirtualTablePointer->ShowWideText, ShowWideTextDetour);
        _showTextHook ??= _gameInteropProvider.HookFromAddress<UIModule.Delegates.ShowText>((nint)UIModule.StaticVirtualTablePointer->ShowText, ShowTextDetour);
        _showPoisonTextHook ??= _gameInteropProvider.HookFromAddress<UIModule.Delegates.ShowPoisonText>((nint)UIModule.StaticVirtualTablePointer->ShowPoisonText, ShowPoisonTextDetour);
        _showErrorTextHook ??= _gameInteropProvider.HookFromAddress<UIModule.Delegates.ShowErrorText>((nint)UIModule.StaticVirtualTablePointer->ShowErrorText, ShowErrorTextDetour);
        _showTextGimmickHintHook ??= _gameInteropProvider.HookFromAddress<RaptureAtkModule.Delegates.ShowTextGimmickHint>((nint)RaptureAtkModule.MemberFunctionPointers.ShowTextGimmickHint, ShowTextGimmickHintDetour);

        if (ImGui.Checkbox("Enable", ref _enabled))
        {
            if (_enabled)
            {
                _showWideTextHook?.Enable();
                _showTextHook?.Enable();
                _showPoisonTextHook?.Enable();
                _showErrorTextHook?.Enable();
                _showTextGimmickHintHook?.Enable();
            }
            else
            {
                _showWideTextHook?.Disable();
                _showTextHook?.Disable();
                _showPoisonTextHook?.Disable();
                _showErrorTextHook?.Disable();
                _showTextGimmickHintHook?.Disable();
            }
        }

        ImGui.SameLine();

        if (ImGui.Button("Clear"))
        {
            _toasts.Clear();
        }

        ImGui.SameLine();
        ImGui.Text($"{_toasts.Count} Toasts");

        using var table = ImRaii.Table("ToastTable"u8, 4, ImGuiTableFlags.Resizable | ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table)
            return;

        ImGui.TableSetupColumn("Time"u8, ImGuiTableColumnFlags.WidthFixed, 80);
        ImGui.TableSetupColumn("Type"u8, ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Formatted Message"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Parameters"u8, ImGuiTableColumnFlags.WidthFixed, 400);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        using var clipper = new ImRaiiListClipper(_toasts.Count, ImGui.GetTextLineHeightWithSpacing());

        foreach (var i in clipper)
        {
            var toast = _toasts[i];
            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // Timestamp
            ImGui.Text(toast.Timestamp.ToLongTimeString());

            ImGui.TableNextColumn(); // Type
            switch (toast)
            {
                case WideToast:
                    ImGui.Text("Wide"u8);
                    break;
                case NormalToast:
                    ImGui.Text("Normal"u8);
                    break;
                case PoisonToast:
                    ImGui.Text("Poison"u8);
                    break;
                case ErrorToast:
                    ImGui.Text("Error"u8);
                    break;
                case GimmickHintToast:
                    ImGui.Text("Gimmick"u8);
                    break;
            }

            ImGui.TableNextColumn(); // Formatted Message
            if (!toast.Text.IsEmpty)
            {
                _debugRenderer.DrawSeString(toast.Text.AsSpan(), new NodeOptions()
                {
                    AddressPath = new AddressPath(i),
                    Indent = false,
                    Title = $"Toast Line {i}"
                });
            }

            ImGui.TableNextColumn(); // Parameters
            ImGuiUtils.DrawCopyableText(toast.GetParameters());
        }
    }

    private void ShowWideTextDetour(UIModule* thisPtr, CStringPointer text, int layer, bool isTop, bool isFast, uint logMessageId)
    {
        _showWideTextHook!.Original(thisPtr, text, layer, isTop, isFast, logMessageId);
        var message = _seStringEvaluator.Evaluate(text.AsReadOnlySeStringSpan());
        if (!message.IsEmpty)
            _toasts.Add(new WideToast(DateTime.Now, message, layer, isTop, isFast, logMessageId));
    }

    private void ShowTextDetour(UIModule* thisPtr, int position, CStringPointer text, uint iconOrCheck1, bool playSound, uint iconOrCheck2, bool alsoPlaySound)
    {
        _showTextHook!.Original(thisPtr, position, text, iconOrCheck1, playSound, iconOrCheck2, alsoPlaySound);
        var formatted = _seStringEvaluator.Evaluate(text.AsReadOnlySeStringSpan());
        if (!formatted.IsEmpty)
            _toasts.Add(new NormalToast(DateTime.Now, formatted, position, iconOrCheck1, playSound, iconOrCheck2, alsoPlaySound));
    }

    private void ShowPoisonTextDetour(UIModule* thisPtr, CStringPointer text, int layer)
    {
        _showPoisonTextHook!.Original(thisPtr, text, layer);
        var formatted = _seStringEvaluator.Evaluate(text.AsReadOnlySeStringSpan());
        if (!formatted.IsEmpty)
            _toasts.Add(new PoisonToast(DateTime.Now, formatted, layer));
    }

    private void ShowErrorTextDetour(UIModule* thisPtr, CStringPointer text, bool forceVisible)
    {
        _showErrorTextHook!.Original(thisPtr, text, forceVisible);
        var formatted = _seStringEvaluator.Evaluate(text.AsReadOnlySeStringSpan());
        if (!formatted.IsEmpty)
            _toasts.Add(new ErrorToast(DateTime.Now, formatted, forceVisible));
    }

    private void ShowTextGimmickHintDetour(RaptureAtkModule* thisPtr, CStringPointer text, RaptureAtkModule.TextGimmickHintStyle style, int duration)
    {
        _showTextGimmickHintHook!.Original(thisPtr, text, style, duration);
        var formatted = _seStringEvaluator.Evaluate(text.AsReadOnlySeStringSpan());
        if (!formatted.IsEmpty)
            _toasts.Add(new GimmickHintToast(DateTime.Now, formatted, style, duration));
    }

    private class Toast(DateTime timestamp, ReadOnlySeString text)
    {
        public DateTime Timestamp { get; init; } = timestamp;
        public ReadOnlySeString Text { get; init; } = text;

        public virtual string GetParameters()
        {
            return string.Empty;
        }
    }

    private class WideToast(DateTime timestamp, ReadOnlySeString text, int layer, bool isTop, bool isFast, uint logMessageId) : Toast(timestamp, text)
    {
        public int Layer { get; init; } = layer;
        public bool IsTop { get; init; } = isTop;
        public bool IsFast { get; init; } = isFast;
        public uint LogMessageId { get; init; } = logMessageId;

        public override string GetParameters()
        {
            return $"Layer = {Layer}, IsTop = {IsTop}, IsFast = {IsFast}, LogMessageId = {LogMessageId}";
        }
    }

    private class NormalToast(DateTime timestamp, ReadOnlySeString text, int position, uint iconOrCheck1, bool playSound1, uint iconOrCheck2, bool playSound2) : Toast(timestamp, text)
    {
        public int Position { get; init; } = position;
        public uint IconOrCheck1 { get; init; } = iconOrCheck1;
        public bool PlaySound1 { get; init; } = playSound1;
        public uint IconOrCheck2 { get; init; } = iconOrCheck2;
        public bool PlaySound2 { get; init; } = playSound2;

        public override string GetParameters()
        {
            return $"Position = {Position}, IconOrCheck1 = {IconOrCheck1}, PlaySound1 = {PlaySound1}, IconOrCheck2 = {IconOrCheck2}, PlaySound2 = {PlaySound2}";
        }
    }

    private class PoisonToast(DateTime timestamp, ReadOnlySeString text, int layer) : Toast(timestamp, text)
    {
        public int Layer { get; init; } = layer;

        public override string GetParameters()
        {
            return $"Layer = {Layer}";
        }
    }

    private class ErrorToast(DateTime timestamp, ReadOnlySeString text, bool forceVisible) : Toast(timestamp, text)
    {
        public bool ForceVisible { get; init; } = forceVisible;

        public override string GetParameters()
        {
            return $"ForceVisible = {ForceVisible}";
        }
    }

    private class GimmickHintToast(DateTime timestamp, ReadOnlySeString text, RaptureAtkModule.TextGimmickHintStyle style, int duration) : Toast(timestamp, text)
    {
        public RaptureAtkModule.TextGimmickHintStyle Style { get; init; } = style;
        public int Duration { get; init; } = duration;

        public override string GetParameters()
        {
            return $"Style = {Style}, Duration = {Duration}";
        }
    }
}
