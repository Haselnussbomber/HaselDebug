using System.Linq;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselCommon.Extensions.Strings;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.Titles;

[RegisterSingleton]
public class TitlesTable : Table<Title>
{
    private readonly ExcelService _excelService;

    public TitlesTable(ExcelService excelService, LanguageProvider languageProvider) : base("TitlesTable", languageProvider)
    {
        _excelService = excelService;

        Columns = [
            RowIdColumn<Title>.Create(),
            new UnlockedColumn() {
                Label = "Unlocked",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 75,
            },
            new PrefixColumn() {
                Label = "IsPrefix",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 75,
            },
            new TitleColumn(true),
            new TitleColumn(false),
        ];
    }

    public override void LoadRows()
    {
        Rows = _excelService.GetSheet<Title>()
            .Where(row => row.RowId != 0 && !row.Feminine.IsEmpty && !row.Masculine.IsEmpty)
            .ToList();
    }

    private class RowIdColumn : ColumnNumber<Title>
    {
        public override string ToName(Title row)
            => row.RowId.ToString();

        public override int ToValue(Title row)
            => (int)row.RowId;
    }

    private class UnlockedColumn : ColumnBool<Title>
    {
        public override unsafe bool ToBool(Title row)
            => UIState.Instance()->TitleList.IsTitleUnlocked((ushort)row.RowId);
    }

    private class PrefixColumn : ColumnBool<Title>
    {
        public override bool ToBool(Title row)
            => row.IsPrefix;
    }

    private class TitleColumn : ColumnString<Title>
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
}
