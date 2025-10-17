using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append)]
public unsafe class RaptureLogModuleTab : DebugTab
{
    private string _input = "";
    private readonly List<string> _messages = [];

    public override void Draw()
    {
        var raptureLogModule = RaptureLogModule.Instance();

        ImGui.Text($"CurrentLogIndex: {raptureLogModule->LogModule.LogMessageIndex}");
        ImGui.Text($"LogMessageCount: {raptureLogModule->LogModule.LogMessageCount}");

        if (ImGui.Button("Clear"))
        {
            _messages.Clear();
        }

        if (ImGui.Button("Read Messages"))
        {
            _messages.Clear();
            for (var i = 0; i < raptureLogModule->LogModule.LogMessageCount; i++)
            {
                raptureLogModule->GetLogMessage(i, out var message);
                _messages.Add(((ReadOnlySeStringSpan)message).ToMacroString());
            }
        }

        if (ImGui.InputText("PrintString", ref _input, 255, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            raptureLogModule->PrintString(_input);
        }

        if (ImGui.InputText("PrintMessage", ref _input, 255, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            using var sender = new Utf8String("me");
            using var message = new Utf8String(_input);
            raptureLogModule->PrintMessage(27, &sender, &message, (int)DateTimeOffset.Now.ToUnixTimeMilliseconds());
        }

        var index = 0;
        foreach (var message in _messages)
        {
            ImGui.Text($"[{index++}] {message}");
        }
    }
}
