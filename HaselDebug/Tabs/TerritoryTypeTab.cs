using Dalamud.Plugin.Services;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Services;
using HaselDebug.Utils;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

public unsafe class TerritoryTypeTab(
    DebugRenderer DebugRenderer,
    IClientState ClientState,
    LanguageProvider LanguageProvider) : DebugTab
{
    public override void Draw()
    {
        DebugRenderer.DrawExdSheet(typeof(TerritoryType), ClientState.TerritoryType, 0, new NodeOptions()
        {
            DefaultOpen = true,
            Language = LanguageProvider.ClientLanguage
        });
    }
}
