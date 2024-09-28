using Dalamud.Plugin.Services;
using ExdSheets.Sheets;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Tabs;

public unsafe class TerritoryTypeTab(DebugRenderer DebugRenderer, IClientState ClientState, ExdSheets.Module ExdModule, TextService TextService) : DebugTab
{
    public override void Draw()
    {
        DebugRenderer.DrawExdSheet(ExdModule, typeof(TerritoryType), ClientState.TerritoryType, 0, new NodeOptions()
        {
            DefaultOpen = true,
            Language = TextService.ClientLanguage
        });
    }
}
