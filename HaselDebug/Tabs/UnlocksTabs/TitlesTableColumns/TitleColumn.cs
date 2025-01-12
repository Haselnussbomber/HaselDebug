using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselCommon.Extensions.Strings;
using HaselCommon.Gui.ImGuiTable;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.TitlesTableColumns;

public class TitleColumn : ColumnString<Title>
{
    private readonly bool _isFeminine;

    public TitleColumn(bool isFeminine)
    {
        _isFeminine = isFeminine;

        Label = isFeminine ? "Feminine" : "Masculine";
    }

    public override string ToName(Title row)
    {
        return (_isFeminine ? row.Feminine : row.Masculine).ExtractText().StripSoftHypen();
    }

    public override unsafe void DrawColumn(Title row)
    {
        var uiState = UIState.Instance();
        var isUnlocked = uiState->TitleList.IsTitleUnlocked((ushort)row.RowId);
        var localPlayer = Control.GetLocalPlayer();

        if (localPlayer != null && uiState->PlayerState.IsLoaded == 1 && uiState->PlayerState.Sex == (_isFeminine ? 1 : 0) && isUnlocked)
        {
            var clicked = ImGui.Selectable($"{ToName(row)}##Selectable", localPlayer->TitleId == row.RowId);

            if (ImGui.IsItemHovered())
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

            if (clicked)
            {
                var titleIdToSend = (ushort)(localPlayer->TitleId == row.RowId ? 0 : row.RowId);
                Service.Get<IPluginLog>().Debug($"Sending Title Update {titleIdToSend}");
                uiState->TitleController.SendTitleIdUpdate(titleIdToSend);
                if (titleIdToSend != 0)
                {
                    using var title = new Utf8String(ToName(row));
                    RaptureLogModule.Instance()->ShowLogMessageString(3846u, &title);
                }
            }
        }
        else
        {
            ImGui.TextUnformatted(ToName(row));
        }
    }
}
