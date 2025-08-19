using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class ChatTab : DebugTab
{
    private readonly DebugRenderer _debugRenderer;
    private readonly ExcelService _excelService;
    private readonly TextService _textService;
    private readonly ISeStringEvaluator _seStringEvaluator;

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

        ImGui.Text($"{count} Message");

        using var table = ImRaii.Table("ChatTabTable"u8, 5, ImGuiTableFlags.Resizable | ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table)
            return;

        ImGui.TableSetupColumn("Timestamp"u8, ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableSetupColumn("LogKind"u8, ImGuiTableColumnFlags.WidthFixed, 50);
        ImGui.TableSetupColumn("Caster"u8, ImGuiTableColumnFlags.WidthFixed, 150);
        ImGui.TableSetupColumn("Target"u8, ImGuiTableColumnFlags.WidthFixed, 150);
        ImGui.TableSetupColumn("Formatted Message"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(5, 1);
        ImGui.TableHeadersRow();

        var clipper = ImGui.ImGuiListClipper();
        clipper.Begin(count, ImGui.GetTextLineHeightWithSpacing());
        while (clipper.Step())
        {
            for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
            {
                if (i >= count)
                    return;

                if (i >= 0 && raptureLogModule->GetLogMessageDetail(i + start, out var sender, out var message, out var logKind, out var casterKind, out var targetKind, out var time))
                {
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn(); // Timestamp
                    ImGui.Text(DateTimeOffset.FromUnixTimeSeconds(time).LocalDateTime.ToString());

                    ImGui.TableNextColumn(); // LogKind
                    ImGui.Text(logKind.ToString());

                    ImGui.TableNextColumn(); // Caster
                    ImGui.Text(GetLabel(casterKind));

                    ImGui.TableNextColumn(); // Target
                    ImGui.Text(GetLabel(targetKind));

                    ImGui.TableNextColumn(); // Formatted Message
                    var senderEvaluated = _seStringEvaluator.Evaluate((ReadOnlySeStringSpan)sender);
                    var messageEvaluated = _seStringEvaluator.Evaluate((ReadOnlySeStringSpan)message);
                    var format = _excelService.TryGetRow<LogKind>((uint)logKind, out var logKindRow) ? logKindRow.Format : new();
                    var formatted = _seStringEvaluator.Evaluate(format, [senderEvaluated, messageEvaluated]).AsSpan();

                    if (!formatted.IsEmpty)
                    {
                        _debugRenderer.DrawSeString(formatted, new NodeOptions()
                        {
                            AddressPath = new AddressPath(i),
                            Indent = false,
                            Title = $"Chat Line {i}"
                        });
                    }
                }
            }
        }

        clipper.End();
        clipper.Destroy();
    }

    private string GetLabel(int index)
    {
        return index switch
        {
            0 => _textService.GetAddonText(1227), // You
            1 => _textService.GetAddonText(1228), // Party Member
            2 => _textService.GetAddonText(1229), // Alliance Member
            3 => _textService.GetAddonText(1230), // Other PC
            4 => _textService.GetAddonText(1231), // Engaged Enemy
            5 => _textService.GetAddonText(1232), // Unengaged Enemy
            6 => _textService.GetAddonText(1283), // Friendly NPCs
            7 => _textService.GetAddonText(1276), // Pets/Companions
            8 => _textService.GetAddonText(1277), // Pets/Companions (Party)
            9 => _textService.GetAddonText(1278), // Pets/Companions (Alliance)
            10 => _textService.GetAddonText(1279), // Pets/Companions (Other PC)
            _ => string.Empty,
        };
    }
}
