using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselCommon.Services;
using HaselCommon.Services.SeStringEvaluation;
using HaselDebug.Abstracts;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace HaselDebug.Tabs;

public unsafe class ChatTab(DebugRenderer DebugRenderer, ExcelService ExcelService, TextService TextService, SeStringEvaluatorService SeStringEvaluator) : DebugTab
{
    public override void Draw()
    {
        var raptureLogModule = RaptureLogModule.Instance();

        for (var i = 0; i < 4; i++)
        {
            if (ImGui.Button($"Reload Tab #{i}"))
            {
                raptureLogModule->ChatTabIsPendingReload[i] = true;
            }

            ImGui.SameLine();
        }

        var start = *(int*)((nint)raptureLogModule + 0x18);
        var count = raptureLogModule->LogMessageCount - start;

        ImGui.TextUnformatted($"{count} Message");

        using var table = ImRaii.Table("ChatTabTable", 5, ImGuiTableFlags.Resizable | ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table)
            return;

        ImGui.TableSetupColumn("Timestamp", ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableSetupColumn("LogKind", ImGuiTableColumnFlags.WidthFixed, 50);
        ImGui.TableSetupColumn("Caster", ImGuiTableColumnFlags.WidthFixed, 150);
        ImGui.TableSetupColumn("Target", ImGuiTableColumnFlags.WidthFixed, 150);
        ImGui.TableSetupColumn("Formatted Message", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(5, 1);
        ImGui.TableHeadersRow();

        var imGuiListClipperPtr = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
        imGuiListClipperPtr.Begin(count, ImGui.GetTextLineHeightWithSpacing());
        while (imGuiListClipperPtr.Step())
        {
            for (var i = imGuiListClipperPtr.DisplayStart; i < imGuiListClipperPtr.DisplayEnd; i++)
            {
                if (i >= count)
                    return;

                if (i >= 0 && raptureLogModule->GetLogMessageDetail(i + start, out var sender, out var message, out var logInfo, out var time))
                {
                    var logKind = logInfo & 0x7F;
                    var caster = ((logInfo >> 11) & 0xF) - 1;
                    var target = ((logInfo >> 7) & 0xF) - 1;

                    ImGui.TableNextRow();

                    ImGui.TableNextColumn(); // Timestamp
                    ImGui.TextUnformatted(DateTimeOffset.FromUnixTimeSeconds(time).LocalDateTime.ToString());

                    ImGui.TableNextColumn(); // LogKind
                    ImGui.TextUnformatted(logKind.ToString());

                    ImGui.TableNextColumn(); // Caster
                    ImGui.TextUnformatted(GetLabel(caster));

                    ImGui.TableNextColumn(); // Target
                    ImGui.TextUnformatted(GetLabel(target));

                    ImGui.TableNextColumn(); // Formatted Message
                    var senderEvaluated = SeStringEvaluator.Evaluate(sender);
                    var messageEvaluated = SeStringEvaluator.Evaluate(message);
                    var format = ExcelService.GetRow<LogKind>((uint)logKind)?.Format.AsReadOnly() ?? new();
                    var formatted = SeStringEvaluator.Evaluate(format, new SeStringContext() { LocalParameters = [senderEvaluated, messageEvaluated] }).AsSpan();

                    if (!formatted.IsEmpty)
                    {
                        DebugRenderer.DrawSeStringSelectable(formatted, new NodeOptions()
                        {
                            AddressPath = new AddressPath(i),
                            Indent = false,
                            Title = $"Chat Line {i}"
                        });
                    }
                }
            }
        }

        imGuiListClipperPtr.End();
        imGuiListClipperPtr.Destroy();
    }

    private string GetLabel(int index)
    {
        return index switch
        {
            0 => TextService.GetAddonText(1227), // You
            1 => TextService.GetAddonText(1228), // Party Member
            2 => TextService.GetAddonText(1229), // Alliance Member
            3 => TextService.GetAddonText(1230), // Other PC
            4 => TextService.GetAddonText(1231), // Engaged Enemy
            5 => TextService.GetAddonText(1232), // Unengaged Enemy
            6 => TextService.GetAddonText(1283), // Friendly NPCs
            7 => TextService.GetAddonText(1276), // Pets/Companions
            8 => TextService.GetAddonText(1277), // Pets/Companions (Party)
            9 => TextService.GetAddonText(1278), // Pets/Companions (Alliance)
            10 => TextService.GetAddonText(1279), // Pets/Companions (Other PC)
            _ => string.Empty,
        };
    }
}
