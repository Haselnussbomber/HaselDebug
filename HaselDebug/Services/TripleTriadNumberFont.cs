using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Plugin;

namespace HaselDebug.Services;

[RegisterSingleton]
public class TripleTriadNumberFont(IDalamudPluginInterface pluginInterface) : IDisposable
{
    private IFontHandle? _fontHandle;

    public IFontHandle FontHandle => _fontHandle ??= pluginInterface.UiBuilder.FontAtlas.NewGameFontHandle(new GameFontStyle(GameFontFamily.MiedingerMid, 208f / 10f));
    public IDisposable Push() => FontHandle.Push();

    public void Dispose()
    {
        _fontHandle?.Dispose();
        GC.SuppressFinalize(this);
    }
}
