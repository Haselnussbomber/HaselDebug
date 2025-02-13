using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HaselCommon.Extensions.Strings;
using HaselCommon.Graphics;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Services;
using HaselDebug.Utils;
using ImGuiNET;
using GlassesSheet = Lumina.Excel.Sheets.Glasses;

namespace HaselDebug.Tabs.UnlocksTabs.Glasses;

[RegisterSingleton]
public unsafe class GlassesTable : Table<GlassesSheet>
{
    internal readonly ExcelService _excelService;

    public GlassesTable(
        ExcelService excelService,
        DebugRenderer debugRenderer,
        TextService textService,
        UnlocksTabUtils unlocksTabUtils,
        LanguageProvider languageProvider) : base(languageProvider)
    {
        _excelService = excelService;

        Columns = [
            RowIdColumn<GlassesSheet>.Create(),
            new UnlockedColumn() {
                Label = "Unlocked",
                Flags = ImGuiTableColumnFlags.WidthFixed,
                Width = 75,
            },
            new NameColumn(debugRenderer, textService, unlocksTabUtils) {
                Label = "Name",
            }
        ];
    }

    public override void LoadRows()
    {
        Rows = _excelService.GetSheet<GlassesSheet>()
            .Skip(1)
            .ToList();
    }

    private class UnlockedColumn : ColumnBool<GlassesSheet>
    {
        public override unsafe bool ToBool(GlassesSheet row)
            => PlayerState.Instance()->IsGlassesUnlocked((ushort)row.RowId);
    }

    private class NameColumn(
        DebugRenderer debugRenderer,
        TextService textService,
        UnlocksTabUtils unlocksTabUtils) : ColumnString<GlassesSheet>
    {
        public override string ToName(GlassesSheet row)
            => textService.GetGlassesName(row.RowId);

        public override unsafe void DrawColumn(GlassesSheet row)
        {
            debugRenderer.DrawIcon((uint)row.Icon);

            var name = ToName(row);

            using (Color.Transparent.Push(ImGuiCol.HeaderActive))
            using (Color.Transparent.Push(ImGuiCol.HeaderHovered))
                ImGui.Selectable(name);

            if (ImGui.IsItemHovered())
            {
                unlocksTabUtils.DrawTooltip(
                    (uint)row.Icon,
                    name,
                    null,
                    !row.Description.IsEmpty
                        ? row.Description.ExtractText().StripSoftHypen()
                        : null);
            }
        }
    }
}
