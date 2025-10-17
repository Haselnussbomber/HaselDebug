using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class TerritoryTypeTab : DebugTab
{
    private readonly DebugRenderer _debugRenderer;
    private readonly IClientState _clientState;
    private readonly LanguageProvider _languageProvider;

    public override void Draw()
    {
        _debugRenderer.DrawExdRow(typeof(TerritoryType), _clientState.TerritoryType, 0, new NodeOptions()
        {
            DefaultOpen = true,
            Language = _languageProvider.ClientLanguage
        });
    }
}
