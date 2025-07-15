using Dalamud.Game;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using Lumina.Excel.Sheets;
using Microsoft.Extensions.Logging;

namespace HaselDebug.Tabs.UnlocksTabs.Titles.Columns;

[RegisterTransient, AutoConstruct]
public partial class TitleColumn : ColumnString<Title>
{
    private readonly ILogger<TitleColumn> _logger;
    private readonly ExcelService _excelService;

    private bool _isFeminine;

    public void SetSex(bool isFeminine)
    {
        _isFeminine = isFeminine;
        LabelKey = isFeminine ? "FeminineTitle.Label" : "MasculineTitle.Label";
    }

    public override unsafe string ToName(Title row)
    {
        if (UIModule.Instance()->GetUIInputData()->IsKeyDown(SeVirtualKey.SHIFT))
        {
            _excelService.TryGetRow(row.RowId, ClientLanguage.English, out row);
        }

        return (_isFeminine ? row.Feminine : row.Masculine).ExtractText().StripSoftHyphen();
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
                _logger.LogDebug($"Sending Title Update {titleIdToSend}");
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
