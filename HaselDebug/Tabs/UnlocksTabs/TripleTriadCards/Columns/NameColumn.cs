using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselCommon.Graphics;
using HaselCommon.Gui.ImGuiTable;
using HaselCommon.Services;
using HaselDebug.Services;
using HaselDebug.Utils;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs.UnlocksTabs.TripleTriadCards.Columns;

[RegisterTransient, AutoConstruct]
public partial class NameColumn : ColumnString<TripleTriadCardEntry>
{
    private readonly DebugRenderer _debugRenderer;
    private readonly ExcelService _excelService;
    private readonly ISeStringEvaluator _seStringEvaluator;
    private readonly MapService _mapService;
    private readonly UnlocksTabUtils _unlocksTabUtils;
    private readonly ITextureProvider _textureProvider;

    [AutoPostConstruct]
    public void Initialize()
    {
        LabelKey = "NameColumn.Label";
    }

    public override string ToName(TripleTriadCardEntry entry)
        => entry.Row.Name.ToString();

    public string ToSearchName(TripleTriadCardEntry entry)
    {
        var str = ToName(entry);

        if (entry.Item.HasValue &&
            _excelService.TryGetRow<TripleTriadCardResident>(entry.Item.Value.ItemAction.Value.Data[0], out var residentRow) &&
            _excelService.TryGetRow<TripleTriadCardObtain>(residentRow.AcquisitionType.RowId, out var obtainRow) &&
            obtainRow.Text.RowId != 0)
        {
            str += "\n" + _seStringEvaluator.EvaluateFromAddon(obtainRow.Text.RowId, [
                residentRow.Acquisition.RowId,
                    residentRow.Location.RowId
            ]).ToString();
        }

        return str;
    }

    public override bool ShouldShow(TripleTriadCardEntry row)
    {
        var name = ToSearchName(row);
        if (FilterValue.Length == 0)
            return true;

        return FilterRegex?.IsMatch(name) ?? name.Contains(FilterValue, StringComparison.OrdinalIgnoreCase);
    }

    public override unsafe void DrawColumn(TripleTriadCardEntry entry)
    {
        _debugRenderer.DrawIcon(88000 + entry.Row.RowId);

        if (AgentLobby.Instance()->IsLoggedIn)
        {
            var hasLevel = entry.ResidentRow.Location.TryGetValue<Level>(out var level);
            var hasCfcEntry = entry.ResidentRow.Acquisition.Is<ContentFinderCondition>();

            using (Color.Transparent.Push(ImGuiCol.HeaderActive, !hasLevel && !hasCfcEntry))
            using (Color.Transparent.Push(ImGuiCol.HeaderHovered, !hasLevel && !hasCfcEntry))
            {
                if (ImGui.Selectable(ToName(entry)))
                {
                    if (hasCfcEntry)
                    {
                        if (entry.ResidentRow.Acquisition.TryGetValue<ContentFinderCondition>(out var cfc))
                        {
                            if (cfc.ContentType.RowId == 30)
                            {
                                UIModule.Instance()->ExecuteMainCommand(94); // can't open VVDFinder with the right instance :/
                            }
                            else
                            {
                                AgentContentsFinder.Instance()->OpenRegularDuty(entry.ResidentRow.Acquisition.RowId);
                            }
                        }
                    }
                    else if (hasLevel)
                    {
                        _mapService.OpenMap(level);
                    }
                }
            }

            if (entry.Item.HasValue && ImGui.IsItemHovered())
                _unlocksTabUtils.DrawItemTooltip(entry.Item.Value);
        }
        else
        {
            ImGui.Text(ToName(entry));
        }

        if (_textureProvider.TryGetFromGameIcon(entry.UnlockIcon, out var iconTex) && iconTex.TryGetWrap(out _, out _))
        {
            // cool, icon preloaded! now the tooltips don't flicker...
        }

        if (_textureProvider.TryGetFromGameIcon(87000 + entry.RowId, out var imageTex) && imageTex.TryGetWrap(out _, out _))
        {
            // cool, image preloaded! now the tooltips don't flicker...
        }
    }
}
