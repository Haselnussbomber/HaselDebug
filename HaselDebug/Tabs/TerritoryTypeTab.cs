using Dalamud.Plugin.Services;
using ExdSheets.Sheets;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Utils;

namespace HaselDebug.Tabs;

public unsafe class TerritoryTypeTab(IClientState ClientState, ExdSheets.Module ExdModule, TranslationManager TranslationManager) : DebugTab
{
    public override void Draw()
    {
        DebugUtils.DrawExdSheet(ExdModule, typeof(TerritoryType), ClientState.TerritoryType, 0, new NodeOptions()
        {
            DefaultOpen = true,
            Language = TranslationManager.ClientLanguage
        });
    }
}
