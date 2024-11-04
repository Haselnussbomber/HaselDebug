using Dalamud.Plugin.Services;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Services;
using HaselDebug.Utils;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

public unsafe class TerritoryTypeTab(DebugRenderer DebugRenderer, IClientState ClientState, ExcelModule ExcelModule, TextService TextService) : DebugTab
{
    public override void Draw()
    {
        DebugRenderer.DrawExdSheet(ExcelModule, typeof(TerritoryType), ClientState.TerritoryType, 0, new NodeOptions()
        {
            DefaultOpen = true,
            Language = TextService.ClientLanguage
        });
    }
}
