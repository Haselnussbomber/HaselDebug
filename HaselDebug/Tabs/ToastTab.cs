using Dalamud.Game.Gui.Toast;
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
    private readonly IToastGui _toastGui;
    private readonly ISeStringEvaluator _seStringEvaluator;

    private Hook<RaptureAtkModule.Delegates.ShowTextGimmickHint>? _showTextGimmickHintHook;

    private bool _enabled;

    private enum ToastType
    {
        Normal,
        Quest,
        Error,
        Gimmick,
    }
    private record struct Toast(DateTime Timestamp, ToastType Type, ReadOnlySeString Text, bool? IsHandled = null, RaptureAtkModule.TextGimmickHintStyle? Style = null);
    private readonly List<Toast> _toasts = [];

    public void Dispose()
    {
        _toastGui.Toast -= OnToast;
        _toastGui.QuestToast -= OnQuestToast;
        _toastGui.ErrorToast -= OnErrorToast;
        _showTextGimmickHintHook?.Dispose();
    }

    public override void Draw()
    {
        _showTextGimmickHintHook ??= _gameInteropProvider.HookFromAddress<RaptureAtkModule.Delegates.ShowTextGimmickHint>((nint)RaptureAtkModule.MemberFunctionPointers.ShowTextGimmickHint, OnShowTextGimmickHint);

        if (ImGui.Checkbox("Enable", ref _enabled))
        {
            if (_enabled)
            {
                _toastGui.Toast += OnToast;
                _toastGui.QuestToast += OnQuestToast;
                _toastGui.ErrorToast += OnErrorToast;
                _showTextGimmickHintHook.Enable();
            }
            else
            {
                _toastGui.Toast -= OnToast;
                _toastGui.QuestToast -= OnQuestToast;
                _toastGui.ErrorToast -= OnErrorToast;
                _showTextGimmickHintHook.Disable();
            }
        }

        ImGui.SameLine();

        if (ImGui.Button("Clear"))
        {
            _toasts.Clear();
        }

        ImGui.SameLine();
        ImGui.Text($"{_toasts.Count} Toasts");

        using var table = ImRaii.Table("ToastTabTable"u8, 5, ImGuiTableFlags.Resizable | ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table)
            return;

        ImGui.TableSetupColumn("Timestamp"u8, ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableSetupColumn("Type"u8, ImGuiTableColumnFlags.WidthFixed, 80);
        ImGui.TableSetupColumn("IsHandled"u8, ImGuiTableColumnFlags.WidthFixed, 80);
        ImGui.TableSetupColumn("Style"u8, ImGuiTableColumnFlags.WidthFixed, 80);
        ImGui.TableSetupColumn("Formatted Message"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(5, 1);
        ImGui.TableHeadersRow();

        using var clipper = new ImRaiiListClipper(_toasts.Count, ImGui.GetTextLineHeightWithSpacing());

        foreach (var i in clipper)
        {
            var toast = _toasts[i];
            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // timestamp
            ImGui.Text(toast.Timestamp.ToString("HH:mm:ss.fff"));

            ImGui.TableNextColumn(); // type
            ImGui.Text(toast.Type.ToString());

            ImGui.TableNextColumn();
            ImGui.Text(toast.IsHandled?.ToString() ?? string.Empty);

            ImGui.TableNextColumn();
            ImGui.Text(toast.Style?.ToString() ?? string.Empty);

            ImGui.TableNextColumn(); // formatted message
            if (!toast.Text.IsEmpty)
            {
                _debugRenderer.DrawSeString(toast.Text.AsSpan(), new NodeOptions()
                {
                    AddressPath = new AddressPath(i),
                    Indent = false,
                    Title = $"Toast Line {i}"
                });
            }
        }
    }

    private void OnToast(ref Dalamud.Game.Text.SeStringHandling.SeString message, ref ToastOptions options, ref bool isHandled)
    {
        _toasts.Add(new(DateTime.Now, ToastType.Normal, new(message.Encode()), IsHandled: isHandled));
    }

    private void OnQuestToast(ref Dalamud.Game.Text.SeStringHandling.SeString message, ref QuestToastOptions options, ref bool isHandled)
    {
        _toasts.Add(new(DateTime.Now, ToastType.Quest, new(message.Encode()), IsHandled: isHandled));
    }

    private void OnErrorToast(ref Dalamud.Game.Text.SeStringHandling.SeString message, ref bool isHandled)
    {
        _toasts.Add(new(DateTime.Now, ToastType.Error, new(message.Encode()), IsHandled: isHandled));
    }

    private void OnShowTextGimmickHint(RaptureAtkModule* thisPtr, CStringPointer text, RaptureAtkModule.TextGimmickHintStyle style, int duration)
    {
        _showTextGimmickHintHook!.Original(thisPtr, text, style, duration);
        var formatted = _seStringEvaluator.Evaluate(text.AsReadOnlySeString());
        if (!formatted.IsEmpty)
            _toasts.Add(new(DateTime.Now, ToastType.Gimmick, formatted, Style: style));
    }
}
