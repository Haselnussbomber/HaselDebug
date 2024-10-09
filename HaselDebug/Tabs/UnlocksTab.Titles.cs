using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselCommon.Extensions.Strings;
using HaselCommon.Graphics;
using HaselDebug.Abstracts;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace HaselDebug.Tabs;

public unsafe partial class UnlocksTab : DebugTab, IDisposable
{
    public void DrawTitles()
    {
        using var tab = ImRaii.TabItem("Titles");
        if (!tab) return;

        var localPlayer = Control.GetLocalPlayer();
        if (localPlayer == null)
        {
            ImGui.TextUnformatted("LocalPlayer unavailable");
            return;
        }

        var uiState = UIState.Instance();
        if (!uiState->TitleList.DataReceived)
        {
            using (ImRaii.Disabled(uiState->TitleList.DataPending))
            {
                if (ImGui.Button("Request Title List"))
                {
                    uiState->TitleList.RequestTitleList();
                }
            }

            return;
        }

        using var table = ImRaii.Table("TitlesTable", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings);
        if (!table) return;

        ImGui.TableSetupColumn("RowId", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Unlocked", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("IsPrefix", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Feminine");
        ImGui.TableSetupColumn("Masculine");
        ImGui.TableSetupScrollFreeze(5, 1);
        ImGui.TableHeadersRow();

        foreach (var row in ExcelService.GetSheet<Title>())
        {
            if (row.RowId == 0)
                continue;

            ImGui.TableNextRow();

            ImGui.TableNextColumn(); // RowId
            ImGui.TextUnformatted(row.RowId.ToString());

            ImGui.TableNextColumn(); // Unlocked
            var isUnlocked = uiState->TitleList.IsTitleUnlocked((ushort)row.RowId);
            using (ImRaii.PushColor(ImGuiCol.Text, (uint)(isUnlocked ? Color.Green : Color.Red)))
                ImGui.TextUnformatted(isUnlocked.ToString());

            ImGui.TableNextColumn(); // IsPrefix
            ImGui.TextUnformatted(row.IsPrefix.ToString());

            ImGui.TableNextColumn(); // Feminine
            if (uiState->PlayerState.Sex == 1 && isUnlocked)
            {
                var clicked = ImGui.Selectable($"{row.Feminine.ExtractText()}##Title_Feminine_{row.RowId}", localPlayer->TitleId == row.RowId);
                if (ImGui.IsItemHovered())
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                if (clicked)
                {
                    var titleIdToSend = (ushort)(localPlayer->TitleId == row.RowId ? 0 : row.RowId);
                    Service.Get<IPluginLog>().Debug($"Sending Title Update {titleIdToSend}");
                    uiState->TitleController.SendTitleIdUpdate(titleIdToSend);
                    if (titleIdToSend != 0)
                    {
                        using var title = new Utf8String(row.Feminine.RawData);
                        RaptureLogModule.Instance()->ShowLogMessageString(3846u, &title);
                    }
                }
            }
            else
            {
                ImGui.TextUnformatted(row.Feminine.ExtractText());
            }

            ImGui.TableNextColumn(); // Masculine
            if (uiState->PlayerState.Sex == 0 && isUnlocked)
            {
                var clicked = ImGui.Selectable($"{row.Masculine.ExtractText()}##Title_Masculine_{row.RowId}", localPlayer->TitleId == row.RowId);
                if (ImGui.IsItemHovered())
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                if (clicked)
                {
                    var titleIdToSend = (ushort)(localPlayer->TitleId == row.RowId ? 0 : row.RowId);
                    Service.Get<IPluginLog>().Debug($"Sending Title Update {titleIdToSend}");
                    uiState->TitleController.SendTitleIdUpdate(titleIdToSend);
                    if (titleIdToSend != 0)
                    {
                        using var title = new Utf8String(row.Masculine.RawData);
                        RaptureLogModule.Instance()->ShowLogMessageString(3846u, &title);
                    }
                }
            }
            else
            {
                ImGui.TextUnformatted(row.Masculine.ExtractText());
            }
        }
    }
}
