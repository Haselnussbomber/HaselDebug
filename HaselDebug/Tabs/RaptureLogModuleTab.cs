using System.Collections.Generic;
using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselDebug.Abstracts;
using ImGuiNET;

namespace HaselDebug.Tabs;

public unsafe class RaptureLogModuleTab : DebugTab
{
    private string input = "";
    private readonly List<string> messages = [];

    public override void Draw()
    {
        var raptureLogModule = RaptureLogModule.Instance();

        ImGui.TextUnformatted($"CurrentLogIndex: {raptureLogModule->LogModule.LogMessageIndex}");
        ImGui.TextUnformatted($"LogMessageCount: {raptureLogModule->LogModule.LogMessageCount}");

        if (ImGui.Button("Clear"))
        {
            messages.Clear();
        }

        if (ImGui.Button("Read Messages"))
        {
            messages.Clear();
            for (var i = 0; i < raptureLogModule->LogModule.LogMessageCount; i++)
            {
                raptureLogModule->GetLogMessage(i, out var message);
                messages.Add(SeString.Parse(message).ToString());
            }
        }

        if (ImGui.InputText("PrintString", ref input, 255, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            raptureLogModule->PrintString(input);
        }

        if (ImGui.InputText("PrintMessage", ref input, 255, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            using var sender = new Utf8String("me");
            using var message = new Utf8String(input);
            raptureLogModule->PrintMessage(27, &sender, &message, (int)DateTimeOffset.Now.ToUnixTimeMilliseconds());
        }

        var index = 0;
        foreach (var message in messages)
        {
            ImGui.TextUnformatted($"[{index++}] {message}");
        }
    }
}
