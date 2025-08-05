using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Services;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.AozActions.Columns;

[RegisterTransient, AutoConstruct]
public partial class LocationColumn : ColumnString<AozEntry>
{
    private readonly TextureService _textureService;
    private readonly DebugRenderer _debugRenderer;
    private readonly ExcelService _excelService;
    private readonly TextService _textService;
    private readonly ISeStringEvaluator _seStringEvaluator;

    public override string ToName(AozEntry entry)
    {
        return entry.AozActionTransient.LocationKey switch
        {
            1 when entry.AozActionTransient.Location.TryGetValue<PlaceName>(out var placeName) => placeName.Name.ExtractText().StripSoftHyphen(),
            4 when entry.AozActionTransient.Location.TryGetValue<ContentFinderCondition>(out var cfc) => cfc.Name.ExtractText().StripSoftHyphen(),
            _ => string.Empty,
        };
    }

    public override unsafe void DrawColumn(AozEntry entry)
    {
        switch (entry.AozActionTransient.LocationKey)
        {
            case 1 when entry.AozActionTransient.Location.TryGetValue<PlaceName>(out var placeName):
                _textureService.DrawPart("AozNoteBook", 7, 2, ImGui.GetTextLineHeight());
                ImGui.SameLine();
                ImGui.TextUnformatted(placeName.Name.ExtractText().StripSoftHyphen());
                break;

            case 2:
                _debugRenderer.DrawIcon(26543);
                ImGui.TextUnformatted(_textService.GetAddonText(12269)); // Learned via Whalaqee Totem
                break;

            case 3:
                _textureService.DrawPart("AozNoteBook", 7, 2, ImGui.GetTextLineHeight());
                ImGui.SameLine();
                ImGui.TextUnformatted(_textService.GetAddonText(12270)); // Learned First
                break;

            case 4 when entry.AozActionTransient.Location.TryGetValue<ContentFinderCondition>(out var cfc):
                _debugRenderer.DrawIcon(cfc.ContentType.Value.Icon);
                if (ImGui.Selectable(_seStringEvaluator.EvaluateFromAddon(12277, [cfc.Name]).ExtractText().StripSoftHyphen()))
                {
                    AgentContentsFinder.Instance()->OpenRegularDuty(cfc.RowId);
                }
                break;
        }
    }
}
